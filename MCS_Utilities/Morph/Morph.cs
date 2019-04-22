using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace MCS_Utilities.Morph
{

    //This is our human readable morph format, not for serialization directly
    [System.Serializable]
    public class MorphData
    {
        public string name; //The name of the morph, eg: FBMHeavy
        public string meshName; //The name of the mesh that this morph is to be applied to, eg: Genesis2Female.Shape_LOD0
        public BlendshapeData blendshapeData;
        public JCTData jctData;

        //used internally for packing and unpacking
        protected static int sizeOfFloat = sizeof(float);
        protected static int sizeOfInt = sizeof(int);
        protected static int magicHeader = 2112775168;
        protected static string jctKeySeperator = @"|"; //used for splitting the name later
        protected static char jctKeySeperatorChar = '|'; //used for splitting the name late


        /// <summary>
        /// Efficiently packs a morph into a hardcoded binary packed format
        ///  Using a dataset of about 30k verts...
        ///  C# native deserializer - 75ms
        ///  ProtoBuf deserializer - 30ms
        ///  Custom deserializer - 0ms
        /// </summary>
        public static byte[] ConvertMorphDataToBytes(MorphData morphData, int version = 1)
        {
            //Build the header, it's format is the following, note floats are all 4 bytes (single precision)
            // quick note, all lengths are of size "int" to keep things simple, saving a few extra bytes isn't worth worrying about it

            byte[] buffer;
            /*

            HEADER

            Magic Bytes - Verifies this is the correct file type (int)
            Version (float) - This is the DATA STRUCTURE version, if we update the structure, we need to update the version
            Length of Name (int)
            Length of Delta Verts (int)
            Length of Delta Norms (int)
            Length of Delta Tans (int)
            Length of JCT Keys (int) (about 170+ entries of variable length names)
            Length of JCTs (int)

            BODY

            Name (string)
            Verts (uint32+3floats)
            Norms (uint32+3floats)
            Tans (uint32+3floats)
            JCT Names (string split on "|")
            JCTs (6floats)

            */


            MorphDataHeaderV1 header = new MorphDataHeaderV1();
            header.version = version;

            byte[] morphNameAsBytes = System.Text.Encoding.UTF8.GetBytes(morphData.name);

            header.lenName = morphNameAsBytes.Length;

            Vector3Spatial[] deltaVertices = Vector3Spatial.ConvertVector3ArrayToSpatialArray(morphData.blendshapeData.deltaVertices);
            Vector3Spatial[] deltaNormals = Vector3Spatial.ConvertVector3ArrayToSpatialArray(morphData.blendshapeData.deltaNormals);
            Vector3Spatial[] deltaTangents = Vector3Spatial.ConvertVector3ArrayToSpatialArray(morphData.blendshapeData.deltaTangents);

            header.lenDeltaVertices = (morphData.blendshapeData.deltaVertices != null ? morphData.blendshapeData.deltaVertices.Length : 0);
            header.lenDeltaNormals = (morphData.blendshapeData.deltaNormals != null ? morphData.blendshapeData.deltaNormals.Length : 0);
            header.lenDeltaTangents = (morphData.blendshapeData.deltaTangents != null ? morphData.blendshapeData.deltaTangents.Length : 0);

            header.lenDeltaVerticesPacked = (deltaVertices != null ? deltaVertices.Length : 0);
            header.lenDeltaNormalsPacked = (deltaNormals != null ? deltaNormals.Length : 0);
            header.lenDeltaTangentsPacked = (deltaTangents != null ? deltaTangents.Length : 0);

            header.lenJCTKeys = 0;
            header.lenJCTs = 0;

            StringBuilder sbJCTKeys = new StringBuilder();
            byte[] jctKeys = null;

            //only do this if we have jct data in here
            if (morphData.jctData.boneNames != null && morphData.jctData.boneNames.Length > 0)
            {
                header.lenJCTs = (ushort)morphData.jctData.localPositions.Length;
                for (int i = 0; i < morphData.jctData.boneNames.Length; i++)
                {
                    sbJCTKeys.Append(morphData.jctData.boneNames[i]);
                    if (i + 1 < morphData.jctData.boneNames.Length)
                    {
                        sbJCTKeys.Append(jctKeySeperator);
                    }
                }
                jctKeys = System.Text.Encoding.UTF8.GetBytes(sbJCTKeys.ToString());
                header.lenJCTKeys = jctKeys.Length;
                //6 floats per item, key is the same as the name order
                header.lenJCTs = ((6 * sizeOfFloat) * morphData.jctData.localPositions.Length);
            }

            //now that we know the header, how big should our buffer be?
            int bufferSize = 0;
            bufferSize += sizeOfInt; //magic header
            bufferSize += sizeOfInt; //version
            bufferSize += sizeOfInt + header.lenName;
            bufferSize += sizeOfInt + (header.lenDeltaVerticesPacked * sizeOfInt) + (header.lenDeltaVerticesPacked * (3 * sizeOfFloat));
            bufferSize += sizeOfInt + (header.lenDeltaNormalsPacked * sizeOfInt) + (header.lenDeltaNormalsPacked * (3 * sizeOfFloat));
            bufferSize += sizeOfInt + (header.lenDeltaTangentsPacked * sizeOfInt) + (header.lenDeltaTangentsPacked * (3 * sizeOfFloat));
            bufferSize += (sizeOfInt * 3); //packed lengths
            bufferSize += sizeOfInt + header.lenJCTKeys;
            bufferSize += sizeOfInt + header.lenJCTs;
            //bufferSize += sizeOfInt + (6 * sizeOfFloat);


            buffer = new byte[bufferSize];

            byte[] itemBuffer;
            int pos = 0;

            //write the header first

            //magic
            itemBuffer = System.BitConverter.GetBytes(magicHeader);
            System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
            pos += itemBuffer.Length;

            //version
            itemBuffer = System.BitConverter.GetBytes(header.version);
            System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
            pos += itemBuffer.Length;

            //name
            itemBuffer = System.BitConverter.GetBytes(header.lenName);
            System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
            pos += itemBuffer.Length;

            //verts (unpacked)
            itemBuffer = System.BitConverter.GetBytes(header.lenDeltaVertices);
            System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
            pos += itemBuffer.Length;

            //normals
            itemBuffer = System.BitConverter.GetBytes(header.lenDeltaNormals);
            System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
            pos += itemBuffer.Length;

            //tangents
            itemBuffer = System.BitConverter.GetBytes(header.lenDeltaTangents);
            System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
            pos += itemBuffer.Length;

            //packed
            itemBuffer = System.BitConverter.GetBytes(header.lenDeltaVerticesPacked);
            System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
            pos += itemBuffer.Length;
            itemBuffer = System.BitConverter.GetBytes(header.lenDeltaNormalsPacked);
            System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
            pos += itemBuffer.Length;
            itemBuffer = System.BitConverter.GetBytes(header.lenDeltaTangentsPacked);
            System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
            pos += itemBuffer.Length;

            //JCT keys
            itemBuffer = System.BitConverter.GetBytes(header.lenJCTKeys);
            System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
            pos += itemBuffer.Length;

            //JCTs
            itemBuffer = System.BitConverter.GetBytes(header.lenJCTs);
            System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
            pos += itemBuffer.Length;

            //header is done, now let's write the body

            //morph name
            System.Buffer.BlockCopy(morphNameAsBytes, 0, buffer, pos, morphNameAsBytes.Length);
            pos += morphNameAsBytes.Length;

            //deltas

            if (deltaVertices != null)
            {
                for (int i = 0; i < deltaVertices.Length; i++)
                {
                    itemBuffer = System.BitConverter.GetBytes(deltaVertices[i].index);
                    System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                    pos += itemBuffer.Length;
                    itemBuffer = System.BitConverter.GetBytes(deltaVertices[i].x);
                    System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                    pos += itemBuffer.Length;
                    itemBuffer = System.BitConverter.GetBytes(deltaVertices[i].y);
                    System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                    pos += itemBuffer.Length;
                    itemBuffer = System.BitConverter.GetBytes(deltaVertices[i].z);
                    System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                    pos += itemBuffer.Length;
                }
            }
            if (deltaNormals != null)
            {
                for (int i = 0; i < deltaNormals.Length; i++)
                {
                    itemBuffer = System.BitConverter.GetBytes(deltaVertices[i].index);
                    System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                    pos += itemBuffer.Length;
                    itemBuffer = System.BitConverter.GetBytes(deltaNormals[i].x);
                    System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                    pos += itemBuffer.Length;
                    itemBuffer = System.BitConverter.GetBytes(deltaNormals[i].y);
                    System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                    pos += itemBuffer.Length;
                    itemBuffer = System.BitConverter.GetBytes(deltaNormals[i].z);
                    System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                    pos += itemBuffer.Length;
                }
            }
            if (deltaTangents != null)
            {
                for (int i = 0; i < deltaTangents.Length; i++)
                {
                    itemBuffer = System.BitConverter.GetBytes(deltaVertices[i].index);
                    System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                    pos += itemBuffer.Length;
                    itemBuffer = System.BitConverter.GetBytes(deltaTangents[i].x);
                    System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                    pos += itemBuffer.Length;
                    itemBuffer = System.BitConverter.GetBytes(deltaTangents[i].y);
                    System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                    pos += itemBuffer.Length;
                    itemBuffer = System.BitConverter.GetBytes(deltaTangents[i].z);
                    System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                    pos += itemBuffer.Length;
                }
            }

            //JCT
            if (jctKeys != null)
            {
                //JCT keys
                System.Buffer.BlockCopy(jctKeys, 0, buffer, pos, jctKeys.Length);
                pos += jctKeys.Length;


                //JCTs
                for (int i = 0; i < morphData.jctData.localPositions.Length; i++)
                {
                    itemBuffer = System.BitConverter.GetBytes(morphData.jctData.localPositions[i].x);
                    System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                    pos += itemBuffer.Length;
                    itemBuffer = System.BitConverter.GetBytes(morphData.jctData.localPositions[i].y);
                    System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                    pos += itemBuffer.Length;
                    itemBuffer = System.BitConverter.GetBytes(morphData.jctData.localPositions[i].z);
                    System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                    pos += itemBuffer.Length;


                    itemBuffer = System.BitConverter.GetBytes(morphData.jctData.worldPositions[i].x);
                    System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                    pos += itemBuffer.Length;
                    itemBuffer = System.BitConverter.GetBytes(morphData.jctData.worldPositions[i].y);
                    System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                    pos += itemBuffer.Length;
                    itemBuffer = System.BitConverter.GetBytes(morphData.jctData.worldPositions[i].z);
                    System.Buffer.BlockCopy(itemBuffer, 0, buffer, pos, itemBuffer.Length);
                    pos += itemBuffer.Length;
                }
            }


            return buffer;
        }

        public static MorphData ConvertStreamToMorphData(System.IO.Stream stream)
        {
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            return ConvertBytesToMorphData(buffer);
        }

        public static MorphData ConvertBytesToMorphData(byte[] buffer)
        {
            MorphData md = new MorphData();

            int pos = 0;

            //magic bytes
            int magicHeaderIn = System.BitConverter.ToInt32(buffer, pos);
            pos += sizeOfInt;

            if (magicHeader != magicHeaderIn)
            {
                throw new Exception("Magic morph header not found, invalid morph file");
            }

            int version = System.BitConverter.ToInt32(buffer, pos);
            pos += sizeOfInt;

            switch (version)
            {
                case 1: //Version 1
                    MorphDataHeaderV1 header = new MorphDataHeaderV1();
                    header.version = version;
                    header.lenName = System.BitConverter.ToInt32(buffer, pos);
                    pos += sizeOfInt;
                    header.lenDeltaVertices = System.BitConverter.ToInt32(buffer, pos);
                    pos += sizeOfInt;
                    header.lenDeltaNormals = System.BitConverter.ToInt32(buffer, pos);
                    pos += sizeOfInt;
                    header.lenDeltaTangents = System.BitConverter.ToInt32(buffer, pos);
                    pos += sizeOfInt;
                    header.lenDeltaVerticesPacked = System.BitConverter.ToInt32(buffer, pos);
                    pos += sizeOfInt;
                    header.lenDeltaNormalsPacked = System.BitConverter.ToInt32(buffer, pos);
                    pos += sizeOfInt;
                    header.lenDeltaTangentsPacked = System.BitConverter.ToInt32(buffer, pos);
                    pos += sizeOfInt;
                    header.lenJCTKeys = System.BitConverter.ToInt32(buffer, pos);
                    pos += sizeOfInt;
                    header.lenJCTs = System.BitConverter.ToInt32(buffer, pos);
                    pos += sizeOfInt;

                    //name
                    byte[] nameBytes = new byte[header.lenName];
                    System.Buffer.BlockCopy(buffer, pos, nameBytes, 0, header.lenName);
                    md.name = System.Text.Encoding.UTF8.GetString(nameBytes);
                    md.blendshapeData.name = md.name;
                    pos += header.lenName;

                    Vector3Spatial[] deltaVerticesCereal = new Vector3Spatial[header.lenDeltaVerticesPacked];
                    Vector3Spatial[] deltaNormalsCereal = new Vector3Spatial[header.lenDeltaNormalsPacked];
                    Vector3Spatial[] deltaTangentsCereal = new Vector3Spatial[header.lenDeltaTangentsPacked];

                    for (int i = 0; i < header.lenDeltaVerticesPacked; i++)
                    {
                        deltaVerticesCereal[i].index = System.BitConverter.ToInt32(buffer, pos);
                        pos += sizeOfInt;
                        deltaVerticesCereal[i].x = System.BitConverter.ToSingle(buffer, pos);
                        pos += sizeOfFloat;
                        deltaVerticesCereal[i].y = System.BitConverter.ToSingle(buffer, pos);
                        pos += sizeOfFloat;
                        deltaVerticesCereal[i].z = System.BitConverter.ToSingle(buffer, pos);
                        pos += sizeOfFloat;
                    }

                    for (int i = 0; i < header.lenDeltaNormalsPacked; i++)
                    {
                        deltaNormalsCereal[i].index = System.BitConverter.ToInt32(buffer, pos);
                        pos += sizeOfInt;
                        deltaNormalsCereal[i].x = System.BitConverter.ToSingle(buffer, pos);
                        pos += sizeOfFloat;
                        deltaNormalsCereal[i].y = System.BitConverter.ToSingle(buffer, pos);
                        pos += sizeOfFloat;
                        deltaNormalsCereal[i].z = System.BitConverter.ToSingle(buffer, pos);
                        pos += sizeOfFloat;
                    }

                    for (int i = 0; i < header.lenDeltaTangentsPacked; i++)
                    {
                        deltaTangentsCereal[i].index = System.BitConverter.ToInt32(buffer, pos);
                        pos += sizeOfInt;
                        deltaTangentsCereal[i].x = System.BitConverter.ToSingle(buffer, pos);
                        pos += sizeOfFloat;
                        deltaTangentsCereal[i].y = System.BitConverter.ToSingle(buffer, pos);
                        pos += sizeOfFloat;
                        deltaTangentsCereal[i].z = System.BitConverter.ToSingle(buffer, pos);
                        pos += sizeOfFloat;
                    }

                    //blendshape states allow nulls for normals and tangents, but not vertices, by doing this check we reduce the liklihood we need to expand the heap
                    md.blendshapeData.deltaVertices = new Vector3[header.lenDeltaVertices];
                    Vector3Spatial.ConvertVector3CerealToArrayNonAlloc(deltaVerticesCereal, md.blendshapeData.deltaVertices);
                    if (header.lenDeltaNormalsPacked > 0)
                    {
                        md.blendshapeData.deltaNormals = new Vector3[header.lenDeltaNormals];
                        Vector3Spatial.ConvertVector3CerealToArrayNonAlloc(deltaNormalsCereal, md.blendshapeData.deltaNormals);
                    }
                    if (header.lenDeltaTangentsPacked > 0)
                    {
                        md.blendshapeData.deltaTangents = new Vector3[header.lenDeltaTangents];
                        Vector3Spatial.ConvertVector3CerealToArrayNonAlloc(deltaTangentsCereal, md.blendshapeData.deltaTangents);
                    }


                    //finally let's read jcts if we have them
                    if (header.lenJCTKeys > 0)
                    {
                        byte[] JCTKeysBytes = new byte[header.lenJCTKeys];
                        System.Buffer.BlockCopy(buffer, pos, JCTKeysBytes, 0, header.lenJCTKeys);
                        string JCTKeysString = System.Text.Encoding.UTF8.GetString(JCTKeysBytes);
                        pos += header.lenJCTKeys;

                        md.jctData.boneNames = JCTKeysString.Split(jctKeySeperatorChar);

                        int totalJCTCount = header.lenJCTKeys / (6 * sizeOfFloat);

                        md.jctData.localPositions = new Vector3[totalJCTCount];
                        md.jctData.worldPositions = new Vector3[totalJCTCount];

                        if (header.lenJCTs > 0)
                        {
                            for (int i = 0; i < totalJCTCount; i++)
                            {
                                md.jctData.localPositions[i].x = System.BitConverter.ToSingle(buffer, pos);
                                pos += sizeOfFloat;
                                md.jctData.localPositions[i].y = System.BitConverter.ToSingle(buffer, pos);
                                pos += sizeOfFloat;
                                md.jctData.localPositions[i].z = System.BitConverter.ToSingle(buffer, pos);
                                pos += sizeOfFloat;
                            }
                            for (int i = 0; i < totalJCTCount; i++)
                            {
                                md.jctData.worldPositions[i].x = System.BitConverter.ToSingle(buffer, pos);
                                pos += sizeOfFloat;
                                md.jctData.worldPositions[i].y = System.BitConverter.ToSingle(buffer, pos);
                                pos += sizeOfFloat;
                                md.jctData.worldPositions[i].z = System.BitConverter.ToSingle(buffer, pos);
                                pos += sizeOfFloat;
                            }
                        }
                    }

                    break;
                default:
                    UnityEngine.Debug.LogError("Version: " + version + " is not supported, please verify you have the most up-to-date version of MCS");
                    throw new Exception("Unknown morph file version");
                    break;
            }

            return md;
        }

        /// <summary>
        /// Creates a negative or positive of a morph by flipping the sign of the blendshape
        /// </summary>
        public void InverseMorph()
        {
            if(blendshapeData.deltaNormals != null)
            {
                for(int i = 0; i < blendshapeData.deltaNormals.Length; i++)
                {
                    blendshapeData.deltaNormals[i] *= -1f;
                }
            }
            if(blendshapeData.deltaVertices != null)
            {
                for(int i = 0; i < blendshapeData.deltaVertices.Length; i++)
                {
                    blendshapeData.deltaVertices[i] *= -1f;
                }
            }
            if(blendshapeData.deltaTangents != null)
            {
                for(int i = 0; i < blendshapeData.deltaTangents.Length; i++)
                {
                    blendshapeData.deltaTangents[i] *= -1f;
                }
            }
        }

    }






    #region structs

    /// <summary>
    /// This is a helper struct that we use during packing and unpacking of a binary .morph file
    /// </summary>
    public struct MorphDataHeaderV1
    {
        public int version;
        public int lenName;
        public int lenDeltaVertices;
        public int lenDeltaNormals;
        public int lenDeltaTangents;
        public int lenDeltaVerticesPacked;
        public int lenDeltaNormalsPacked;
        public int lenDeltaTangentsPacked;
        public int lenJCTKeys;
        public int lenJCTs;
    }

    /// <summary>
    /// Used as a really basic way to serialize a vector3 that allows efficient packing for null/empty vectors
    /// </summary>
    [System.Serializable]
    public struct Vector3Spatial
    {
        public int index; //this is the vertex index, we use it similar to a dictionary for compression reasons so we can skip 0'd out vertices
        public float x;
        public float y;
        public float z;

        public void Fill(Vector3 v, int i)
        {
            x = v.x;
            y = v.y;
            z = v.z;
            index = i;
        }
        public Vector3 Get(ref int i)
        {
            i = index;
            return new Vector3(x, y, z);
        }


        //misc helper functions specific to the spatial vector handler

        public static Dictionary<int, Vector3> ConvertVectorArrayToCompressedDict(Vector3[] vectors, float deltaThreshold)
        {
            if(vectors == null)
            {
                return null;
            }
            Dictionary<int, Vector3> keep = new Dictionary<int, Vector3>();
            for (int i = 0; i < vectors.Length; i++)
            {
                Vector3 vertex = vectors[i];

                if (Mathf.Abs(vertex.x) > deltaThreshold || Mathf.Abs(vertex.y) > deltaThreshold || Mathf.Abs(vertex.z) > deltaThreshold)
                {
                    keep.Add(i, vertex);
                }
            }

            return keep;
        }

        public static Vector3Spatial[] ConvertVector3ArrayToSpatialArray(Vector3[] source)
        {
            if(source == null)
            {
                return null;
            }
            Dictionary<int, Vector3> verticesDict = ConvertVectorArrayToCompressedDict(source, 0.0001f);

            Vector3Spatial[] dest = new Vector3Spatial[verticesDict.Count];
            int i = 0;
            foreach (int key in verticesDict.Keys)
            {
                Vector3 vector = verticesDict[key];
                dest[i++].Fill(vector, key);
            }

            return dest;
        }

        public static Vector3[] ConvertVector3CerealToArray(ref Vector3Spatial[] source, int len)
        {
            Vector3[] dest = new Vector3[len];
            int j = 0;

            foreach (Vector3Spatial vc in source)
            {
                dest[vc.index].x = vc.x;
                dest[vc.index].y = vc.y;
                dest[vc.index].z = vc.z;
            }

            return dest;
        }

        public static void ConvertVector3CerealToArrayNonAlloc(Vector3Spatial[] source, Vector3[] dest)
        {
            foreach (Vector3Spatial vc in source)
            {
                dest[vc.index].x = vc.x;
                dest[vc.index].y = vc.y;
                dest[vc.index].z = vc.z;
            }

        }

    }


    /// <summary>
    /// Stores all of our jct info
    /// </summary>
    [System.Serializable]
    public struct JCTData
    {
        public string name;
        public string[] boneNames;
        public Vector3[] worldPositions;
        public Vector3[] localPositions;
    }

    /// <summary>
    /// This is a direct representation of a blendshape, this is pre formatting it for disk
    /// </summary>
    public struct BlendshapeData
    {
        public string name;
        public int shapeIndex; //not really used
        public int frameIndex; //not really used
        public Vector3[] deltaVertices;
        public Vector3[] deltaNormals;
        public Vector3[] deltaTangents; //will frequently be null/empty
    }
    
    
    /// <summary>
    /// This is the structure for our morph json file that tells you which morphs are available for a mesh, that way you don't need to scan the disk
    /// </summary>
    [System.Serializable]
    public struct MorphManifest
    {
        public string name;
        public int count;
        public string[] names;

		public void WriteToDisk(string manifestPath){
			System.IO.Stream fs = System.IO.File.Create(manifestPath);
			string json = JsonUtility.ToJson(this);
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
			fs.Write(bytes, 0, bytes.Length);
			fs.Close();
		}
    }


    #endregion

}
