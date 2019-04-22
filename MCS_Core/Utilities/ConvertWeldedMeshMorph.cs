using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine;

using MCS;
using MCS.FOUNDATIONS;
using MCS.SERVICES;

namespace Internal
{
    /// <summary>
    /// Converts a streaming morph file (or sidecar) morph from a pre-unity fbx import into a morph that matches the correct vert count in unity
    /// </summary>
    class ConvertWeldedMeshMorph
    {
        //Converts a sidecar or streamable morph file from pre-uinty to post-unity import. This is necessary b/c
        // we use unity's FBX importer which will do things like add vertices
        public Dictionary<int,int> BuildMap(string objFile, Mesh dstMesh, float scale = 100.0f)
        {
            List<Vector3> srcVertices = new List<Vector3>();
            List<int> srcTriangles = new List<int>();

            Vector3[] dstVertices = dstMesh.vertices;
            int[] dstTriangles = dstMesh.triangles;

            Dictionary<int, int> srcEdges = new Dictionary<int, int>();
            Dictionary<int, int> dstEdges = new Dictionary<int, int>();


            Dictionary<int, int> dstToSrcVertIndex = new Dictionary<int, int>();

            float eps = 0.0001f; //let's be a little more forgiving then Mathf.Epsilon

            //let's start by building a map from our OBJ file
            string[] lines = File.ReadAllLines(objFile);

            Regex regexVert = new Regex(@"^v ([-0-9\.]+) ([-0-9\.]+) ([-0-9\.]+)");
            Regex regexFace = new Regex(@"^f ([0-9]+) ([0-9]+) ([0-9]+)");

            Match m;

            for (int i = 0; i < lines.Length; i++)
            {
                string cmd = lines[i].Substring(0, 1);

                if (cmd.Equals("v"))
                {
                    m = regexVert.Match(lines[i]);
                    if (!m.Success)
                    {
                        throw new Exception("Unable to parse obj file properly, failed converting a Vector3");
                    }

                    float x = float.Parse(m.Groups[1].Value) / scale;
                    float y = float.Parse(m.Groups[2].Value) / scale;
                    float z = float.Parse(m.Groups[3].Value) / scale;

                    Vector3 vert = new Vector3(x, y, z);

                    srcVertices.Add(vert);
                }
                else if (cmd.Equals("f"))
                {
                    m = regexFace.Match(lines[i]);
                    if (!m.Success)
                    {
                        throw new Exception("Unable to parse obj file properly, failed converting a Face");
                    }

                    int a = int.Parse(m.Groups[1].Value);
                    int b = int.Parse(m.Groups[2].Value);
                    int c = int.Parse(m.Groups[3].Value);

                    srcTriangles.Add(a);
                    srcTriangles.Add(b);
                    srcTriangles.Add(c);

                    //add all of our lookup edges
                    srcEdges.Add(a, b);
                    srcEdges.Add(b, a);
                    srcEdges.Add(a, c);
                    srcEdges.Add(c, a);
                    srcEdges.Add(b, c);
                    srcEdges.Add(c, b);
                }
            }

            for (int i = 0; i < dstTriangles.Length; i += 3)
            {
                int a = dstTriangles[i + 0];
                int b = dstTriangles[i + 1];
                int c = dstTriangles[i + 2];

                dstEdges.Add(a, b);
                dstEdges.Add(b, a);
                dstEdges.Add(a, c);
                dstEdges.Add(c, a);
                dstEdges.Add(b, c);
                dstEdges.Add(c, b);
            }

            //now we have a map of all faces and world space cooridnates and their associated indexes, let's build a second map that associates the src index to the dst index

            //let's create a candidates list, we'll then refine this by analyzing the triangles
            Dictionary<int, List<int>> candidates = new Dictionary<int, List<int>>();

            //O(n^2) this isn't really great, but it won't take too long in practice
            for (int i = 0; i < dstVertices.Length; i++)
            {
                Vector3 dstVert = dstVertices[i];

                for (int j = 0; j < srcVertices.Count; j++)
                {
                    Vector3 srcVert = srcVertices[j];

                    float delta = (dstVert - srcVert).magnitude;

                    if (delta < eps)
                    {
                        if (candidates[i] == null)
                        {
                            candidates[i] = new List<int>();
                        }

                        candidates[i].Add(j);
                    }
                }
            }


            //now we have a list of comparables, we'll loop again and compare them by finding the best guess when analyzing the triangle
            for (int i = 0; i < candidates.Count; i++)
            {

                int dstIndex = i;
                List<int> potentials = candidates[i];


                //only one candidate, let's use that one
                if (potentials.Count == 1)
                {
                    dstToSrcVertIndex[i] = potentials[0];
                    continue;
                }

                int srcIndex = FindBestMatchingVert(dstIndex, potentials, srcEdges, srcVertices, dstEdges, dstVertices);
                if (srcIndex == -1)
                {
                    UnityEngine.Debug.LogError("Failed to find matching edge at dstIndex: " + dstIndex + " | candidates length: " + potentials.Count);
                    throw new Exception("Unable to find matching edge");
                }

                dstToSrcVertIndex[dstIndex] = srcIndex;
            }

            //now we have our map

            return dstToSrcVertIndex;

        }

        protected int FindBestMatchingVert(int dstIndex, List<int> potentials, Dictionary<int, int> srcEdges, List<Vector3> srcVertices, Dictionary<int, int> dstEdges, Vector3[] dstVertices)
        {
            float eps = 0.0001f; //let's be a little more forgiving then Mathf.Epsilon
            int result = -1;
            //there are a few candidates, let's try to find one that shares an edge from the original triangle
            for (int j = 0; j < potentials.Count; j++)
            {
                //let's find one that shares an edge if possible

                int srcIndex = potentials[j];

                foreach (int srcIndexB in srcEdges.Values)
                {
                    Vector3 srcVertB = srcVertices[srcIndexB];

                    //skip the same point
                    if (srcIndexB == srcIndex)
                    {
                        continue;
                    }

                    foreach (int dstIndexB in dstEdges.Values)
                    {
                        if (dstIndexB == dstIndex)
                        {
                            continue;
                        }

                        Vector3 dstVertB = dstVertices[dstIndexB];
                        float delta = (dstVertB - srcVertB).magnitude;
                        if (delta < eps)
                        {
                            //found it
                            return srcIndex;
                        }
                    }
                }
            }

            return result;
        }

        //public bool ConvertMorphFile(string srcMorphFilePath, string dstMorphFilePath, Dictionary<int,int>vertMap)
        //{

        //    bool compressed = srcMorphFilePath.Substring(-2, 2).Equals("gz");
        //    StreamingMorphs sm = new StreamingMorphs();
        //    //this is the "welded" or likely less vert count original
        //    MorphData srcMorphData = sm.GetMorphDataFromDisk(srcMorphFilePath, true, compressed, true);
        //    MorphData dstMorphData = ConvertMorph(srcMorphData, vertMap);

        //    sm.WriteMorphDataToFile(ref dstMorphData, dstMorphFilePath, true);

        //    return true;
        //}

        public MCS_Utilities.Morph.MorphData ConvertMorph(MCS_Utilities.Morph.MorphData srcMorphData, Dictionary<int,int> vertMap)
        {

            //this will be our "unwelded" or likely higher vert count new morph
            MCS_Utilities.Morph.MorphData dstMorphData = new MCS_Utilities.Morph.MorphData();

            dstMorphData.meshName = srcMorphData.meshName;
            dstMorphData.name = srcMorphData.name;
            dstMorphData.jctData = srcMorphData.jctData; //TODO: this is technically not supported as we don't do anything with this yet

            MCS_Utilities.Morph.BlendshapeData bd = new MCS_Utilities.Morph.BlendshapeData();

            bd.name = srcMorphData.blendshapeData.name;
            bd.shapeIndex = srcMorphData.blendshapeData.shapeIndex;
            bd.frameIndex = srcMorphData.blendshapeData.frameIndex;

            bd.deltaVertices = new Vector3[vertMap.Count];
            bd.deltaNormals = new Vector3[vertMap.Count]; //TODO: unsupported
            bd.deltaTangents = new Vector3[vertMap.Count]; //TODO: unsupported

            //rebuild this morph using the vertmap, we'll likely be adding duplicates
            foreach(int dstVertIndex in vertMap.Keys)
            {
                int srcVertIndex = vertMap[dstVertIndex];
                bd.deltaVertices[dstVertIndex] = srcMorphData.blendshapeData.deltaVertices[srcVertIndex];
            }

            dstMorphData.blendshapeData = bd;

            return dstMorphData;
        }
    }
}
