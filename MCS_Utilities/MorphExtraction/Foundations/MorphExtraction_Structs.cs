using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;

namespace MCS.FOUNDATIONS
{
    public sealed class CurrentAssemblyDeserializationBinderLegacy : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            string newTypeName = typeName.Replace("MCS.FOUNDATIONS", "MCS_Utilities.MorphExtraction.Struct");
            UnityEngine.Debug.Log("Binding: " + assemblyName + " | " + typeName + " => " + newTypeName + " vs: " + Assembly.GetExecutingAssembly().FullName);


            return Type.GetType(String.Format("{0}, {1}", newTypeName, Assembly.GetExecutingAssembly().FullName));
        }
    }
}

namespace MCS_Utilities.MorphExtraction.Structs
{
    public sealed class CurrentAssemblyDeserializationBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            //return Type.GetType(String.Format("{0}, {1}", typeName, Assembly.GetExecutingAssembly().FullName));

            if (typeName.Contains("MCS.FOUNDATIONS"))
            {
                typeName = typeName.Replace("MCS.FOUNDATIONS", "MCS_Utilities.MorphExtraction.Structs");
                assemblyName = "MCS_Utilities";
            }


            return Type.GetType(String.Format("{0}, {1}", typeName, assemblyName));
        }
    }


    //This is the structure for our morph json file that tells you which morphs are available for a mesh, that way you don't need to scan the disk
    [System.Serializable]
    public struct MorphManifest
    {
        public string name;
        public int count;
        public string[] names;
    }

    //This is a direct representation of a blendshape, this is pre formatting it for disk
    public struct BlendshapeState
    {
        public string name;
        public int shapeIndex;
        public int frameIndex;
        public Vector3[] deltaVertices;
        public Vector3[] deltaNormals;
        public Vector3[] deltaTangents;

        /*
        public static implicit operator BlendshapeState(byte[] bytes)
        {
            UnityEngine.Debug.Log("Implicit conversion from bytes: " + bytes.Length);
            BlendshapeState bs = new BlendshapeState();
            bs.name = "Unpacked";

            return bs;
        }
        */

        public static explicit operator BlendshapeState(byte[] bytes)
        {
            UnityEngine.Debug.Log("explicit conversion from bytes: " + bytes.Length);
            BlendshapeState bs = new BlendshapeState();
            bs.name = "Unpacked";

            return bs;
        }
    }

    //This is used just for storing and loading from the filesystem, this is the internal morph data structure 
    [System.Serializable]
    public struct BlendshapeStateCereal
    {
        public string name;
        public int shapeIndex;
        public int frameIndex;
        public Vector3Cereal[] deltaVertices;
        public Vector3Cereal[] deltaNormals;
        public Vector3Cereal[] deltaTangents;
        public int deltaVerticesLength;
        public int deltaNormalsLength;
        public int deltaTangentsLength;

        //removes any vector from a list if the threshold for difference isn't met (strips out unneccessary vertices)
        public Dictionary<int, Vector3> ConvertVectorArrayToCompressedDict(Vector3[] vectors, float deltaThreshold)
        {
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

        //Converts the nice blendshape state into a serializable less human readable format
        public void Fill(BlendshapeState bs)
        {
            name = bs.name;
            shapeIndex = bs.shapeIndex;
            frameIndex = bs.frameIndex;

            Dictionary<int, Vector3> deltaVerticesDict = ConvertVectorArrayToCompressedDict(bs.deltaVertices, 0.0001f);
            Dictionary<int, Vector3> deltaNormalsDict = ConvertVectorArrayToCompressedDict(bs.deltaNormals, 0.0001f);
            Dictionary<int, Vector3> deltaTangentsDict = ConvertVectorArrayToCompressedDict(bs.deltaTangents, 0.0001f);

            deltaVertices = new Vector3Cereal[deltaVerticesDict.Count];
            deltaNormals = new Vector3Cereal[deltaNormalsDict.Count];
            deltaTangents = new Vector3Cereal[deltaTangentsDict.Count];

            deltaVerticesLength = bs.deltaVertices.Length;
            deltaNormalsLength = bs.deltaNormals.Length;
            deltaTangentsLength = bs.deltaTangents.Length;

            int i = 0;
            foreach (int key in deltaVerticesDict.Keys)
            {
                Vector3 vector = deltaVerticesDict[key];
                deltaVertices[i++].Fill(vector, key);
            }

            i = 0;
            foreach (int key in deltaNormalsDict.Keys)
            {
                Vector3 vector = deltaNormalsDict[key];
                deltaNormals[i++].Fill(vector, key);
            }

            i = 0;
            foreach (int key in deltaTangentsDict.Keys)
            {
                Vector3 vector = deltaTangentsDict[key];
                deltaTangents[i++].Fill(vector, key);
            }
        }

        //Converts from the disk format to the human readable format
        public BlendshapeState Extract()
        {
            BlendshapeState bs = new BlendshapeState();
            bs.name = name;
            bs.shapeIndex = shapeIndex;
            bs.frameIndex = frameIndex;
            bs.deltaVertices = new Vector3[deltaVerticesLength];
            bs.deltaNormals = new Vector3[deltaNormalsLength];
            bs.deltaTangents = new Vector3[deltaTangentsLength];

            int j = 0;
            foreach (Vector3Cereal v in deltaVertices)
            {
                Vector3 vector = v.Get(ref j);
                bs.deltaVertices[j] = vector;
            }
            foreach (Vector3Cereal v in deltaNormals)
            {
                Vector3 vector = v.Get(ref j);
                bs.deltaNormals[j] = vector;
            }
            foreach (Vector3Cereal v in deltaTangents)
            {
                Vector3 vector = v.Get(ref j);
                bs.deltaTangents[j] = vector;
            }

            return bs;
        }
    }

    //This is our vector structure that can be serialized directly to disk using binary formatters, also works for normal vectors too
    [System.Serializable]
    public struct Vector3Cereal
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
    }

    //This is our human readable morph format, not for serialization directly
    [System.Obsolete("Please switch to MCS_Utilities.Morph.MorphData")]
    [System.Serializable]
    public struct MorphData
    {
        public string name; //The name of the morph, eg: FBMHeavy
        public string meshName; //The name of the mesh that this morph is to be applied to, eg: Genesis2Female.Shape_LOD0
        public BlendshapeState blendshapeState;
        public JCTData jctData;
    }

    //This is our final stored state of a morph (which encapsulates both blendshapes and jcts currently)
    [System.Serializable]
    public struct MorphDataCereal
    {
        public string name;
        public BlendshapeStateCereal blendshapeStateCereal;
        public JCTDataCereal jctDataCereal;

        public void Fill(MorphData morphData)
        {
            name = morphData.name;
            blendshapeStateCereal = new BlendshapeStateCereal();
            jctDataCereal = new JCTDataCereal();

            blendshapeStateCereal.Fill(morphData.blendshapeState);
            jctDataCereal.Fill(morphData.jctData);
        }

        public MorphData Extract()
        {
            MorphData morphData = new MorphData();
            morphData.name = name;
            morphData.blendshapeState = blendshapeStateCereal.Extract();
            morphData.jctData = jctDataCereal.Extract();

            return morphData;
        }
    }

    //This contains a JCT (joint center transformation), not all morphs have these, but some do, they provide information about how a bone needs to scale/translate/rotate
    [System.Serializable]
    public struct JCTData
    {
        public string name;
        public string[] boneNames;
        public Vector3[] worldPositions;
        public Vector3[] localPositions;
    }

    //internal format just for serialization purposes
    [System.Serializable]
    public struct JCTDataCereal
    {
        public string name;
        public string[] boneNames;
        public Vector3Cereal[] worldPositions;
        public Vector3Cereal[] localPositions;

        //converts from the human readable to the disk format
        public void Fill(JCTData jctData)
        {
            name = jctData.name;
            boneNames = jctData.boneNames;
            worldPositions = new Vector3Cereal[jctData.worldPositions.Length];
            localPositions = new Vector3Cereal[jctData.localPositions.Length];

            int i = 0;
            foreach (Vector3 v in jctData.worldPositions)
            {
                worldPositions[i].Fill(v, i);
                i++;
            }
            i = 0;
            foreach (Vector3 v in jctData.localPositions)
            {
                localPositions[i].Fill(v, i);
                i++;
            }
        }

        //Converts from the disk format to the human readable format
        public JCTData Extract()
        {
            JCTData jctData = new JCTData();
            jctData.name = name;
            jctData.boneNames = boneNames;
            jctData.worldPositions = new Vector3[worldPositions.Length];
            jctData.localPositions = new Vector3[localPositions.Length];

            int j = 0;
            foreach (Vector3Cereal v in worldPositions)
            {
                Vector3 vector = v.Get(ref j);
                jctData.worldPositions[j] = vector;
            }
            foreach (Vector3Cereal v in localPositions)
            {
                Vector3 vector = v.Get(ref j);
                jctData.localPositions[j] = vector;
            }

            return jctData;
        }
    }
}
