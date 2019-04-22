using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace MCS_Utilities
{

    public class MCSResource
    {
        protected static int magicHeader = MCSConstants.MR_MAGIC_NUMBER;
        
        public M3DResourceHeader header;

        protected string trueFilePath = null; //this is the location of where the mr currently sits
        protected Stream trueFileStream = null; //same thing as the file path but instead is a stream

        public byte[] body;

        protected int positionHeader = 0; //it's always 0
        protected int headerLength = -1;
        protected int positionBody = -1;
        protected int bodyLength = -1;

        protected byte[] buffer = null;

        public Dictionary<string, int> lookups = new Dictionary<string, int>();

        public MCSResource() : this(MCSConstants.LATEST_MAJOR_VERSION)
        {

        }

        public MCSResource(int version)
        {
            magicHeader = MCSConstants.MR_MAGIC_NUMBER;

            switch (version)
            {
                case 1:
                    header = new M3DResourceHeaderV1();
                    break;
                case 2:
                    header = new M3DResourceHeaderV2();
                    break;
                default:
                    break;
            }

        }
        public void Free()
        {
            //allow the buffer to be cleaned up if it was set
            buffer = null;
        }

        public void Read(Stream reader, bool loadBody = false, long totalByteLen=-1)
        {
            trueFileStream = reader;

            int markerLength = MCSConstants.HEAD_STOP.Length;
            int index = 0;
            byte[] compare = new byte[MCSConstants.HEAD_STOP.Length];


            if (totalByteLen < 0)
            {
                totalByteLen = reader.Length;
            }

			byte[] firstBytes = new byte[MCSConstants.SIZE_OF_INT * 3];

			//first int is magic, then version, then header size

			//verify the stream is a m3dresource
			reader.Read (firstBytes, 0, firstBytes.Length);

			int magicHeaderIn = System.BitConverter.ToInt32(firstBytes, 0);
			if (magicHeader != magicHeaderIn)
			{
				throw new Exception("Magic header not found, invalid M3DResource file");
			}

			//read the header length size from the stream

			int headerSize = System.BitConverter.ToInt32 (firstBytes, MCSConstants.SIZE_OF_INT*2);

            //if we haven't had to use the buffer yet, declare it now
			if (buffer == null || buffer.Length < headerSize)
            {
				buffer = new byte[headerSize];
            }

			Array.Copy (firstBytes, 0, buffer, 0, firstBytes.Length);

			//we've already read the first X bytes to get the header size, so just read the rest of the header
			reader.Read (buffer, firstBytes.Length, headerSize-firstBytes.Length);
			DeserializeHeader(buffer);

			//if they want to load the body, go ahead and do so by storing it in memory now
			if (loadBody) {
				int bodySize = System.BitConverter.ToInt32 (buffer, 3*MCSConstants.SIZE_OF_INT);
				body = new byte[bodySize];
				reader.Read (body, 0, bodySize);
			}

            reader.Close();

            IndexLookups();
        }

        //Read the header and optionally the body into our resource object
        public void Read(string filePath, bool loadBody = false)
        {
            trueFilePath = filePath;
            FileInfo info = new FileInfo(filePath);


            long totalByteLen = info.Length;

            //read the header

            //keep grabbing a few chunks at a time and proceed until we hit the body marker


            //byte[] buffer = new byte[MAX_HEADER_LENGTH];
            Stream reader = File.OpenRead(filePath);
            Read(reader, loadBody,totalByteLen);


            header.DirectoryPath = header.FilePath.Replace(info.Name, "");
            header.DirectoryPath = header.DirectoryPath.TrimEnd('/').TrimEnd('\\');
            //UnityEngine.Debug.Log("FILEPATH : " + filePath + "Directory Path :  " + header.DirectoryPath);
        }

        protected void DeserializeHeader(byte[] buffer)
        {
            int pos = 0;
            
            //magic bytes
            int magicHeaderIn = System.BitConverter.ToInt32(buffer, pos);
            pos += MCSConstants.SIZE_OF_INT;

            if (magicHeader != magicHeaderIn)
            {
                throw new Exception("Magic header not found, invalid M3DResource file");
            }

            //version
            int version = System.BitConverter.ToInt32(buffer, pos);
            pos += MCSConstants.SIZE_OF_INT;

            switch (version)
            {
				case 1: //Version 1

                    header = new M3DResourceHeaderV1();					
                    break;
                case 2:
                    header = new M3DResourceHeaderV2();                    
                    break; 
                default:
                    UnityEngine.Debug.LogError("Version: " + version + " is not supported, please verify you have the most up-to-date version of MCS");
                    throw new Exception("Unknown M3DResource file version");
                    break;
            }


            byte[] tmpBuf;

            //header size
            int headerBufferSize = System.BitConverter.ToInt32(buffer, pos);
            pos += MCSConstants.SIZE_OF_INT;

            positionBody = headerBufferSize;


            //body size
            int bodyBufferSize = System.BitConverter.ToInt32(buffer, pos);
            pos += MCSConstants.SIZE_OF_INT;

            //Utility Flag
            if(version > 1 && version <= MCSConstants.LATEST_MAJOR_VERSION)
            {
                header.UtilityFlag = System.BitConverter.ToInt32(buffer, pos);
                pos += MCSConstants.SIZE_OF_INT;
            }

            //Filepath
            int filePathLen = System.BitConverter.ToInt32(buffer, pos);
            pos += MCSConstants.SIZE_OF_INT;
            byte[] filePathBytes = new byte[filePathLen];
            System.Buffer.BlockCopy(buffer, pos, filePathBytes, 0, filePathLen);
            pos += filePathLen;
            header.FilePath = System.Text.Encoding.UTF8.GetString(filePathBytes);
            //UnityEngine.Debug.Log("filePath: " + header.filePath);

            //Total Entries
            int totalEntries = System.BitConverter.ToInt32(buffer, pos);
            pos += MCSConstants.SIZE_OF_INT;
            //UnityEngine.Debug.Log("total entries: " + totalEntries);

            header.Keys = new string[totalEntries];
            header.Positions = new int[totalEntries];
            header.Lengths = new int[totalEntries];

            int totalKeysAsBytesLen = System.BitConverter.ToInt32(buffer, pos);
            pos += MCSConstants.SIZE_OF_INT;
            //UnityEngine.Debug.Log("totalKeysAsBytesLen : " + totalKeysAsBytesLen);

            //read each variable length key name until we're done
            for (int i = 0; i < totalEntries; i++)
            {
                int keyLen = System.BitConverter.ToInt32(buffer, pos);
                pos += MCSConstants.SIZE_OF_INT;

                tmpBuf = new byte[keyLen];

                //UnityEngine.Debug.Log("KeyLen: " + i + " | " + keyLen + " | " + buffer.Length);

                System.Buffer.BlockCopy(buffer, pos, tmpBuf, 0, keyLen);
                pos += keyLen;

                header.Keys[i] = System.Text.Encoding.UTF8.GetString(tmpBuf);
            }

            //read the positions
            for (int i = 0; i < totalEntries; i++)
            {
                header.Positions[i] = System.BitConverter.ToInt32(buffer, pos);
                pos += MCSConstants.SIZE_OF_INT;
            }

            //read the lengths
            for (int i = 0; i < totalEntries; i++)
            {
                header.Lengths[i] = System.BitConverter.ToInt32(buffer, pos);
                pos += MCSConstants.SIZE_OF_INT;
            }

            /*
            BinaryFormatter serializer = new BinaryFormatter();
            MemoryStream stream = new MemoryStream(buffer);
            header = (M3DResourceHeader)serializer.Deserialize(stream);
            */
        }

        public void Serialize(string outputFile, int version=1)
        {
            //custom serializer for performance reasons

            byte[] filePathAsBytes = System.Text.Encoding.UTF8.GetBytes(header.FilePath);
            int filePathAsBytesLen = filePathAsBytes.Length;
            int totalEntries = header.Keys.Length;

			int bodySize = 0;

			if (totalEntries > 0) {
				bodySize = header.Positions [totalEntries - 1] + header.Lengths [totalEntries - 1];
			}

            int keysAsBytesLen = 0;
            for(int i = 0; i < totalEntries; i++)
            {
                byte[] tmp = System.Text.Encoding.UTF8.GetBytes(header.Keys[i]);
                keysAsBytesLen += MCSConstants.SIZE_OF_INT;
                keysAsBytesLen += tmp.Length;
            }

            byte[] headStopAsBytes = System.Text.Encoding.ASCII.GetBytes(MCSConstants.HEAD_STOP);
            int headStopAsBytesLen = headStopAsBytes.Length;


            int bufferSize = 0;
            bufferSize += MCSConstants.SIZE_OF_INT; //magic header
            bufferSize += MCSConstants.SIZE_OF_INT; //version
			bufferSize += MCSConstants.SIZE_OF_INT; //header size
			bufferSize += MCSConstants.SIZE_OF_INT; //body size
            if (version == MCSConstants.LATEST_MAJOR_VERSION)
                bufferSize += MCSConstants.SIZE_OF_INT; // Utility Flag
            bufferSize += MCSConstants.SIZE_OF_INT + filePathAsBytesLen;
            bufferSize += MCSConstants.SIZE_OF_INT; //total entries
            bufferSize += MCSConstants.SIZE_OF_INT + keysAsBytesLen;
            bufferSize += (MCSConstants.SIZE_OF_INT * totalEntries); //positions
            bufferSize += (MCSConstants.SIZE_OF_INT * totalEntries); //lengths
            bufferSize += headStopAsBytesLen; //HEAD_STOP

            byte[] buffer = new byte[bufferSize];

            byte[] itemBuffer;
            byte[] tmpBuffer;
            int pos = 0;

            //magic
            itemBuffer = System.BitConverter.GetBytes(magicHeader);
            System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
            pos += itemBuffer.Length;

            //version
            itemBuffer = System.BitConverter.GetBytes(version);
            System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
            pos += itemBuffer.Length;

			//header size
			itemBuffer = System.BitConverter.GetBytes(bufferSize);
			System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
			pos += itemBuffer.Length;

			//body size
			itemBuffer = System.BitConverter.GetBytes(bodySize);
			System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
			pos += itemBuffer.Length;

            if(version == MCSConstants.LATEST_MAJOR_VERSION)
            {
                itemBuffer = System.BitConverter.GetBytes(header.UtilityFlag);
                System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                pos += itemBuffer.Length;
            }
            //file path
            itemBuffer = System.BitConverter.GetBytes(filePathAsBytesLen);
            System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
            pos += itemBuffer.Length;
            System.Buffer.BlockCopy(filePathAsBytes, 0, buffer, pos, filePathAsBytes.Length);
            pos += filePathAsBytes.Length;

            //total entries
            itemBuffer = System.BitConverter.GetBytes(totalEntries);
            System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
            pos += itemBuffer.Length;

            //total keys as bytes len
            itemBuffer = System.BitConverter.GetBytes(keysAsBytesLen);
            System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
            pos += itemBuffer.Length;

            //keys
            for(int i = 0; i < totalEntries; i++)
            {
                itemBuffer = System.Text.Encoding.UTF8.GetBytes(header.Keys[i]);

                //store the key len
                tmpBuffer = System.BitConverter.GetBytes(itemBuffer.Length);
                System.Buffer.BlockCopy(tmpBuffer, 0, buffer, pos, tmpBuffer.Length);
                pos += MCSConstants.SIZE_OF_INT;
                //UnityEngine.Debug.Log("Len: " + i + " | " + itemBuffer.Length + " | " + tmpBuffer.Length);
                tmpBuffer = null;

                //store the key
                System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                pos += itemBuffer.Length;
            }
            for(int i = 0; i < totalEntries; i++)
            {
                itemBuffer = System.BitConverter.GetBytes(header.Positions[i]);
                System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                pos += itemBuffer.Length;
            }
            for(int i = 0; i < totalEntries; i++)
            {
                itemBuffer = System.BitConverter.GetBytes(header.Lengths[i]);
                System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                pos += itemBuffer.Length;
            }

            System.Buffer.BlockCopy(headStopAsBytes, 0, buffer, pos, headStopAsBytes.Length);
            pos += headStopAsBytes.Length;

            if(pos != buffer.Length)
            {
                throw new Exception("Failed to serialize m3dresource");
            }


            //head done

            FileStream fs = File.Create(outputFile);

            //write our header+head_stop
            fs.Write(buffer, 0, buffer.Length);
            fs.Flush();

            //write our body
            fs.Write(body, 0, body.Length);
            fs.Flush();

            //free descriptor
            fs.Close();


            /*

            BinaryFormatter serializer = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            serializer.Serialize(stream, header);
            stream.Flush();
            stream.Position = 0;
            byte[] bytes = stream.ToArray();

            //write our header, then marker, then body

            FileStream fs = File.Create(outputFile);

            fs.Write(bytes, 0, bytes.Length);
            fs.Flush();

            byte[] stop = System.Text.Encoding.ASCII.GetBytes(HEAD_STOP);
            fs.Write(stop, 0, HEAD_STOP.Length);
            fs.Flush();

            fs.Write(body, 0, body.Length);
            fs.Flush();

            fs.Close();
            stream.Close();
            */
        }


        //for fast querying we'll generate a dictionary lookup for the "files"
        public void IndexLookups()
        {
			if (header.Keys == null) {
				UnityEngine.Debug.LogWarning ("header keys is null, cannot index keys");
				return;
			}
            for (int i = 0; i < header.Keys.Length; i++)
            {
                lookups[header.Keys[i]] = i;
            }
        }

        //Retrieves a file from the resource file, returns as raw bytes
        public byte[] GetResource(string key, bool returnNullOnFailure = false)
        {

            bool found = false;


            if (lookups.ContainsKey(key))
            {
                found = true;
            }

            if (!found)
            {
                string tmpKey;
                //NOTE: these are hacks for bad morph names
                tmpKey = key.Replace("_", " ");
                if (lookups.ContainsKey(tmpKey))
                {
                    key = tmpKey;
                    found = true;
                }

                if (!found)
                {
                    if (key.Contains("_NEGATIVE_"))
                    {
                        tmpKey = key.Replace("_NEGATIVE_", "NEGATIVE");
                        if (lookups.ContainsKey(tmpKey))
                        {
                            key = tmpKey;
                            found = true;
                        }
                    }
                }
            }


            if (!found)
            {
                if (returnNullOnFailure)
                {
                    return null;
                }
                else
                {
                    throw new Exception("Invalid key: " + key);
                }
            }


            int slot = lookups[key];
            int position = header.Positions[slot];
            int length = header.Lengths[slot];

            byte[] buffer = new byte[length];

            if (body != null && body.Length > 0)
            {
                //read from the buffer, oh this makes me sad, if this was c or we could use unsafe, we could just return a pointer+length...
                Array.Copy(body, position, buffer, 0, length);
            }
            else
            {
                if (trueFilePath != null)
                {
                    //if we can pull it right off disk, try that first
                    FileStream fs = File.OpenRead(trueFilePath);
                    fs.Seek(positionBody + position, SeekOrigin.Begin);
                    fs.Read(buffer, 0, length);
                    fs.Close();
                } else if(trueFileStream != null)
                {
                    //what if we have a stream instead?
                    trueFileStream.Seek(positionBody + position, SeekOrigin.Begin);
                    trueFileStream.Read(buffer, 0, length);
                    //DO NOT CLOSE stream, it's not our responsibility
                }
                else
                {
                    throw new Exception("Body buffer does not exist and resource is not a true file");
                }
            }
            return buffer;
        }

        //Fetches and stores to disk the file being requested
        public bool UnpackResource(string key, string outputFile = null)
        {
            byte[] bytes = GetResource(key);

            string filePath = (outputFile == null ? header.DirectoryPath + "/" + key : outputFile);

            //verify the directory exists for the file
            //int pos = filePath.LastIndexOf("/");
            string dirPath = Path.GetDirectoryName(filePath);// filePath.Substring(0, pos);

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            FileStream fs = File.Create(filePath);
            fs.Write(bytes, 0, bytes.Length);
            fs.Flush();
            fs.Close();

            return true;
        }

        //Add file(s) to an existing resource (or new one)
        public bool Add(string key, List<string> paths)
        {
            int oldCount = header.Keys.Length;

            //we're adding a file...
            int count = oldCount + paths.Count;

            M3DResourceHeaderV1 newHeader = new M3DResourceHeaderV1();
            newHeader.FilePath = header.FilePath;
            newHeader.Keys = new string[count];
            newHeader.Positions = new int[count];
            newHeader.Lengths = new int[count];

            //copy the old one into the new if applicable
            if (oldCount > 0)
            {
                Array.Copy(header.Keys, newHeader.Keys, oldCount);
                Array.Copy(header.Positions, newHeader.Positions, oldCount);
                Array.Copy(header.Lengths, newHeader.Lengths, oldCount);
            }

            int index = 0;
            if (oldCount > 0)
            {
                //advance the index to the end of the file
                index = body.Length;
            }

            //re-index the header positions and lengths
            int totalLen = 0;
            for(int i = 0; i < count; i++)
            {
                int slot = i + oldCount;
                string path = paths[i];

                newHeader.Keys[slot] = path;
                newHeader.Positions[slot] = totalLen;
                newHeader.Lengths[slot] = (int)new FileInfo(paths[i]).Length;
                totalLen += newHeader.Lengths[slot];
            }

            //create a new body and copy the old one if we need to
            byte[] newBody = new byte[totalLen];
            if (oldCount > 0)
            {
                Array.Copy(body, newBody, body.Length);
            }

            for (int i = 0; i < count; i++)
            {
                int slot = i + oldCount;
                string path = paths[i];

                byte[] buffer = File.ReadAllBytes(paths[i]);
                int fileLen = buffer.Length;

                Array.Copy(buffer, 0, newBody, index, fileLen);
                index += fileLen;
            }

            header = newHeader;
            body = newBody;

            //if we have a current file path to save, save it
            if (header.FilePath != null)
            {
               Serialize(header.FilePath);
            }

            return true; //success
        }

        //overload for a single file
        public bool Add(string key, string pathToFile)
        {
            List<string> paths = new List<string>();
            paths.Add(pathToFile);
            return Add(key, paths);
        }

        //Adds or replaces an entry in the resource file
        public bool Upsert(string key, byte[] bytes, bool writeToDisk=true)
        {
            int count = header.Keys.Length;
            int oldCount = count;

            //how much should we resize our body buffer?
            int deltaBytes = bytes.Length;
            int upsertIndex = -1;

            //are we replacing or adding?
            bool replace = false;
            for(int i = 0; i < count; i++)
            {
                //since we're replacing it, recalculate the correct body size
                if (header.Keys[i].Equals(key))
                {
                    replace = true;
                    deltaBytes = bytes.Length - header.Lengths[i];
                    upsertIndex = i;
                    break;
                }
            }
            if (!replace)
            {
                upsertIndex = count;
                count++;
            }

            M3DResourceHeaderV2 newHeader = new M3DResourceHeaderV2();
            newHeader.FilePath = header.FilePath;
            newHeader.Keys = new string[count];
            newHeader.Positions = new int[count];
            newHeader.Lengths = new int[count];

            byte[] newBody = new byte[body.Length + deltaBytes];

            int bodyIndex = 0;

            //build a new header
            for(int i = 0; i < count; i++)
            {
                string currentKey = (i < oldCount ? header.Keys[i] : key);
                newHeader.Keys[i] = currentKey;

                newHeader.Positions[i] = bodyIndex;

                if (i != upsertIndex)
                {
                    //copy the old one into the body
                    Array.Copy(body, header.Positions[i], newBody, bodyIndex, header.Lengths[i]);
                    newHeader.Lengths[i] = header.Lengths[i];
                } else
                {
                    //copy the new one into the body
                    Array.Copy(bytes, 0, newBody, bodyIndex, bytes.Length);
                    newHeader.Lengths[i] = bytes.Length;
                }
                bodyIndex += newHeader.Lengths[i];
            }

            header = newHeader;
            body = newBody;

            if (writeToDisk && !String.IsNullOrEmpty(header.FilePath))
            {
                Serialize(header.FilePath);
            }

            return true;
        }


        //Combines all Morph_File.morph.gz.bytes files into a single file, these can be recursive or not, your choice
        // this can be done to improve performance of editing/development/ease of use
        public static string MergeFiles(string rootPath, string outputFile = null, string filter = null, bool recursive = true, bool isInstallFile = false)
        {
            SearchOption searchOption = (recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            string[] paths = Directory.GetFiles(rootPath, filter, searchOption);
            return BuildResourceFromListOfPaths(rootPath, outputFile, paths);
        }

        //Creates an M3DResource file by taking a list of strings as paths, does not do any file searching
        public static string BuildResourceFromListOfPaths(string rootPath, string outputFile, string[] paths, string[] incKeys=null, bool isInstallFile = false)
        {
            int count = paths.Length;

            string resourcePath = (outputFile == null ? rootPath + "/bin.mr" : outputFile);

            MCSResource resource = new MCSResource();
            resource.header.FilePath = resourcePath;
            if(isInstallFile)
                resource.header.UtilityFlag = (1 << 1); //set the first bit (?) to indicate install files. 
            // we should add code to set any other flags here. 
            resource.header.Keys = new string[count];
            resource.header.Positions = new int[count];
            resource.header.Lengths = new int[count];

            int index = 0;
            int totalLen = 0;

            for (int i = 0; i < count; i++)
            {
                string path = paths[i];
                if (path.Contains(".fbx") || path.Contains(".mon"))
                    UnityEngine.Debug.Log("FOUND FBX IN MR : " + path);
                path = path.Replace("\\", "/"); //TODO: i feel like this isn't correct, what about escape characters? 
                rootPath = rootPath.Replace("\\", "/");
                path = path.Replace(rootPath + "/", "");

                //if we specified our own keys...
                if (incKeys != null)
                {
                    resource.header.Keys[i] = incKeys[i];
                }
                else
                {
                    resource.header.Keys[i] = MCS_Utilities.Paths.ScrubPath(path);
                }
                resource.header.Positions[i] = totalLen;
                resource.header.Lengths[i] = (int)new FileInfo(paths[i]).Length;
                totalLen += resource.header.Lengths[i];
            }

            resource.body = new byte[totalLen];

            for (int i = 0; i < count; i++)
            {
                string path = paths[i].Replace(rootPath + "/", "");

                byte[] buffer = File.ReadAllBytes(paths[i]);
                int fileLen = buffer.Length;

                Array.Copy(buffer, 0, resource.body, index, fileLen);
                index += fileLen;
            }

            resource.Serialize(resourcePath);

            return resourcePath;
        }

    }


    public interface M3DResourceHeader
    {        
        string FilePath { get; set; }
        string[] Keys { get; set; }
        int[] Positions { get; set; }
        int[] Lengths { get; set; }
        int UtilityFlag { get; set; }
        string DirectoryPath { get; set; }
    }

    [System.Serializable]
    public struct M3DResourceHeaderV1 : M3DResourceHeader
    {
        string filePath; //Where is this file located?
        string[] keys; //the "file" we want to query relative to the root path
        int[] positions; //the first byte position we want to query, or where in this packed file does the first byte of the requested file live? the first file will have position 0 (does not include header offset)
        int[] lengths; //packed file lengths, used with positions to grab just a subset of files

        //These field(s) get populated during the Read call
        [NonSerialized]
        public string dirPath; //What is the parent folder name of the file (non-absolute)

        public string FilePath
        {
            get
            {
                return filePath;
            }

            set
            {
                filePath = value;
            }
        }

        public string[] Keys
        {
            get
            {
                return keys;
            }

            set
            {
                keys = value;
            }
        }

        public int[] Positions
        {
            get
            {
                return positions;
            }

            set
            {
                positions = value;
            }
        }

        public int[] Lengths
        {
            get
            {
                return lengths;
            }

            set
            {
                lengths = value;
            }
        }

        public int UtilityFlag
        {
            get { return 0; }
            set { }
        }

        public string DirectoryPath
        {
            get { return dirPath; }
            set { dirPath = value; }
        }

        public override string ToString()
        {
            return "ResourceHeaderV1: " + filePath + " => Keys: " + keys.Length + " positions: " + positions.Length + " lengths:" + lengths.Length + " First Key: " + (keys.Length > 0 ? keys[0] : "empty");
        }
    }
    public struct M3DResourceHeaderV2 : M3DResourceHeader
    {
        string filePath; //Where is this file located?
        string[] keys; //the "file" we want to query relative to the root path
        int[] positions; //the first byte position we want to query, or where in this packed file does the first byte of the requested file live? the first file will have position 0 (does not include header offset)
        int[] lengths; //packed file lengths, used with positions to grab just a subset of files
        int utilityFlag;
        //These field(s) get populated during the Read call
        [NonSerialized]
        public string dirPath; //What is the parent folder name of the file (non-absolute)

        public string FilePath
        {
            get
            {
                return filePath;
            }

            set
            {
                filePath = value;
            }
        }

        public string[] Keys
        {
            get
            {
                return keys;
            }

            set
            {
                keys = value;
            }
        }

        public int[] Positions
        {
            get
            {
                return positions;
            }

            set
            {
                positions = value;
            }
        }

        public int[] Lengths
        {
            get
            {
                return lengths;
            }

            set
            {
                lengths = value;
            }
        }

        public int UtilityFlag
        {
            get { return utilityFlag; }
            set { utilityFlag = value; }
        }

        public string DirectoryPath
        {
            get { return dirPath; }
            set { dirPath = value; }
        }

    }


}
