using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.GZip;

namespace MCS_Utilities.MorphExtraction
{
    public static class Compression
    {
        public static string CompressFile(string filePath)
        {
            string filePathOut = filePath + ".gz";
            FileStream fsIn = File.OpenRead(filePath);
            FileStream fsOut = File.Create(filePathOut);
            GZipOutputStream compressionStream = new GZipOutputStream(fsOut);
            compressionStream.SetLevel(5); //good balance between speed of compression and actual packed bytes
            compressionStream.IsStreamOwner = true;

            byte[] buffer = new byte[4096];

            using (fsIn)
            {
                StreamUtils.Copy(fsIn, compressionStream, buffer);
            }

            compressionStream.Close();
            return filePathOut;
        }

        public static byte[] StreamToBytes(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                ms.Flush();
                return ms.ToArray();
            }
        }

        public static Stream CompressStream(Stream streamIn)
        {
            MemoryStream streamOut = new MemoryStream();
            byte[] buffer = new byte[4096];

            using (GZipOutputStream compressionStream = new GZipOutputStream(streamOut))
            {
                //GZipOutputStream compressionStream = new GZipOutputStream(streamOut);
                compressionStream.SetLevel(5); //good balance between speed of compression and actual packed bytes
                compressionStream.IsStreamOwner = false;
                StreamUtils.Copy(streamIn, compressionStream, buffer);
                compressionStream.Close();
            }
            streamOut.Flush();
            streamOut.Position = 0; //reset the pointer
            return streamOut;
        }

        public static string DecompressFile(string filePath, string outPath = null)
        {
            int pos = filePath.LastIndexOf(".gz");
            string filePathOut = filePath.Substring(0, pos);

            if (outPath != null)
            {
                int lastSlash = filePath.LastIndexOf("/");
                filePathOut = outPath + "/" + filePath.Substring(lastSlash, pos - lastSlash);
            }

            byte[] buffer = new byte[4096];

            using (System.IO.Stream fsIn = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (GZipInputStream compressionStream = new GZipInputStream(fsIn))
                {
                    using (FileStream fsOut = File.Create(filePathOut))
                    {
                        StreamUtils.Copy(compressionStream, fsOut, buffer);
                    }
                }
            }

            return filePathOut;
        }

        /// <summary>
        /// Decompresses a stream using gzip, make sure you manage your own memory for the returned stream
        /// </summary>
        /// <param name="streamIn"></param>
        /// <returns></returns>
        public static Stream DecompressStream(Stream streamIn)
        {
            MemoryStream streamOut = new MemoryStream();
            streamIn.Position = 0;

            byte[] buffer = new byte[4096];

            using (GZipInputStream compressionStream = new GZipInputStream(streamIn))
            {
                //we'll free our own memory later
                compressionStream.IsStreamOwner = false;
                StreamUtils.Copy(compressionStream, streamOut, buffer);
                compressionStream.Close();
            }

            streamOut.Flush();
            streamOut.Position = 0;

            return streamOut;
        }


        /*
        public static void ExtractGZipSample(string gzipFileName, string targetDir)
        {

            // Use a 4K buffer. Any larger is a waste.    
            byte[] dataBuffer = new byte[4096];

            using (System.IO.Stream fs = new FileStream(gzipFileName, FileMode.Open, FileAccess.Read))
            {
                using (GZipInputStream gzipStream = new GZipInputStream(fs))
                {

                    // Change this to your needs
                    string fnOut = Path.Combine(targetDir, Path.GetFileNameWithoutExtension(gzipFileName));

                    using (FileStream fsOut = File.Create(fnOut))
                    {
                        StreamUtils.Copy(gzipStream, fsOut, dataBuffer);
                    }
                }
            }
        }
        */

        /*
        public static string CompressString(string strIn)
        {
            MemoryStream memoryStream = null;
            GZipOutputStream compressionStream = null;
            string strOut;

            try
            {
                memoryStream = new MemoryStream();
                compressionStream = new GZipOutputStream(memoryStream);
                using (BinaryWriter writer = new BinaryWriter(memoryStream, System.Text.Encoding.ASCII))
                {
                    compressionStream = new GZipOutputStream(memoryStream);
                    compressionStream.Write(Encoding.ASCII.GetBytes(strIn), 0, strIn.Length);
                    compressionStream.Close();
                    strOut = Convert.ToBase64String(memoryStream.ToArray());
                }

            }
            finally
            {
                //cleanup
                if(compressionStream != null)
                {
                    compressionStream.Dispose();
                }
                if (memoryStream != null)
                {
                    memoryStream.Dispose();
                }
            }

            return strOut;
        }

        public static string ZipString(string sBuffer)
        {
            MemoryStream m_msBZip2 = null;
            BZip2OutputStream m_osBZip2 = null;
            string result;
            try
            {
                m_msBZip2 = new MemoryStream();
                Int32 size = sBuffer.Length;
                // Prepend the compressed data with the length of the uncompressed data (firs 4 bytes)
                //
                using (BinaryWriter writer = new BinaryWriter(m_msBZip2, System.Text.Encoding.ASCII))
                {
                    writer.Write(size);

                    m_osBZip2 = new BZip2OutputStream(m_msBZip2);
                    m_osBZip2.Write(Encoding.ASCII.GetBytes(sBuffer), 0, sBuffer.Length);

                    m_osBZip2.Close();
                    result = Convert.ToBase64String(m_msBZip2.ToArray());
                    m_msBZip2.Close();

                    writer.Close();
                }
            }
            finally
            {
                if (m_osBZip2 != null)
                {
                    m_osBZip2.Dispose();
                }
                if (m_msBZip2 != null)
                {
                    m_msBZip2.Dispose();
                }
            }
            return result;
        }
        

        public static string UnzipString(string compbytes)
        {
            string result;
            MemoryStream m_msBZip2 = null;
            BZip2InputStream m_isBZip2 = null;
            try
            {
                m_msBZip2 = new MemoryStream(Convert.FromBase64String(compbytes));
                // read final uncompressed string size stored in first 4 bytes
                //
                using (BinaryReader reader = new BinaryReader(m_msBZip2, System.Text.Encoding.ASCII))
                {
                    Int32 size = reader.ReadInt32();

                    m_isBZip2 = new BZip2InputStream(m_msBZip2);
                    byte[] bytesUncompressed = new byte[size];
                    m_isBZip2.Read(bytesUncompressed, 0, bytesUncompressed.Length);
                    m_isBZip2.Close();
                    m_msBZip2.Close();

                    result = Encoding.ASCII.GetString(bytesUncompressed);

                    reader.Close();
                }
            }
            finally
            {
                if (m_isBZip2 != null)
                {
                    m_isBZip2.Dispose();
                }
                if (m_msBZip2 != null)
                {
                    m_msBZip2.Dispose();
                }
            }
            return result;
        }
        */





        /*

        //Left in here, as we could, in theory, use this if unity adds System.IO.Compression to their mono runtime

        /// <summary>
        /// Compresses a file and returns the new path
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static string CompressFile(string filepath)
        {
            string filepathOut = filepath + ".gz";

            try
            {
                //read the file into a a byte array using streams
                byte[] buffer = Compression.GetBytesFromFile(filepath);
                byte[] bufferOut = Compression.CompressBytes(ref buffer);
                File.WriteAllBytes(filepathOut, bufferOut);
            }
            catch(Exception e)
            {
                throw e;
            }

            return filepathOut;
        }

        public static Stream DecompressFile(string filepath)
        {
            MemoryStream s = new MemoryStream();
            try
            {
                //read the file into a a byte array using streams
                byte[] buffer = Compression.GetBytesFromFile(filepath);
                byte[] bufferOut = Compression.DecompressBytes(ref buffer);
                s.Write(bufferOut, 0, bufferOut.Length);
            }
            catch (Exception e)
            {
                throw e;
            }

            return s;
        }

        public static byte[] CompressBytes(ref byte[] buffer)
        {
            MemoryStream memoryStream = new MemoryStream();
            GZipStream zipStream = new GZipStream(memoryStream, CompressionMode.Compress, true);
            zipStream.Write(buffer, 0, buffer.Length);
            zipStream.Close();

            byte[] bufferOut = new byte[memoryStream.Length];
            ReadAllBytesFromStream(memoryStream, bufferOut);

            return bufferOut;
        }

        public static byte[] DecompressBytes(ref byte[] buffer)
        {
            MemoryStream memoryStream = new MemoryStream();
            byte[] decompressedBuffer = new byte[buffer.Length + 100];

            GZipStream zipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
            int totalCount = Compression.ReadAllBytesFromStream(zipStream, decompressedBuffer);

            zipStream.Write(buffer, 0, buffer.Length);
            zipStream.Close();

            return decompressedBuffer;
        }

        //taken directly from msdn
        public static int ReadAllBytesFromStream(Stream stream, byte[] buffer)
        {
            // Use this method is used to read all bytes from a stream.
            int offset = 0;
            int totalCount = 0;
            while (true)
            {
                int bytesRead = stream.Read(buffer, offset, 100);
                if (bytesRead == 0)
                {
                    break;
                }
                offset += bytesRead;
                totalCount += bytesRead;
            }
            return totalCount;
        }



        /// <summary>
        /// Reads a file using a stream into a byte array
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static byte[] GetBytesFromFile(string filepath)
        {
            //read the file into a a byte array using streams
            FileStream fileIn = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] buffer = new byte[fileIn.Length];
            int bytes = fileIn.Read(buffer, 0, buffer.Length);
            if (bytes != buffer.Length)
            {
                fileIn.Close();
                throw new Exception("Unable to read full file");
            }
            fileIn.Close();
            return buffer;
        }

        */
    }
}
