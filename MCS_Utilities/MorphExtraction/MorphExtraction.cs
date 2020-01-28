using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
//using MCS.FOUNDATIONS;

using MCS_Utilities.Morph;

namespace MCS_Utilities.MorphExtraction
{
    public static class MorphExtraction
    {

        public static string extension = "morph"; //What is the file extension of our morph file, eg: FBMHeavy.morph

        /// <summary>
        /// Retrieves the total number of blendshapes in all meshes for the object
        /// </summary>
        /// <param name="smr"></param>
        public static int GetTotalBlendshapeCountsInObject(GameObject obj)
        {
            int count = 0;

            SkinnedMeshRenderer[] smrs = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer smr in smrs)
            {
                count += smr.sharedMesh.blendShapeCount;
            }

            return count;
        }

        public delegate string NameScrubCallback(string srcName);

        /// <summary>
        /// Rips out blendshapes from a skinned mesh renderer and figures out where to store it along with creating a manifest file
        /// </summary>
        /// <param name="smr"></param>
        /// <returns>A string path to the manifest file</returns>
        public static string ExtractBlendshapesFromMesh(SkinnedMeshRenderer smr, string dirPath, int totalProcessed = 0, int totalCount = 0, bool useProgressBar = true,bool generateManifest=true, NameScrubCallback smrScrub=null, NameScrubCallback morphScrub=null, List<string> nameWhitelist=null)
        {

            //Extracts all blendshapes out of the mesh, does not remove any of them from the mesh
            int blendshapeCount = smr.sharedMesh.blendShapeCount;

            string manifestPath = dirPath + "/manifest.json";

            MorphManifest manifest = new MorphManifest();
            manifest.name = smr.name;
            manifest.count = blendshapeCount;
            manifest.names = new string[blendshapeCount];

            if(smrScrub != null)
            {
                manifest.name = smrScrub(manifest.name);
            }


            if (!Directory.Exists(dirPath))
            {
                DirectoryInfo di = Directory.CreateDirectory(dirPath);
            }

            for (int i = 0; i < blendshapeCount; i++)
            {
                BlendshapeData bd = new BlendshapeData();
                bd.name = smr.sharedMesh.GetBlendShapeName(i);

                if (morphScrub != null)
                {
                    bd.name = morphScrub(bd.name);
                }

                bd.shapeIndex = i;
                int vertexCount = smr.sharedMesh.vertexCount;

                bd.deltaVertices = new Vector3[vertexCount];
                bd.deltaNormals = new Vector3[vertexCount];
                bd.deltaTangents = new Vector3[vertexCount];

                //loads the blendshape data from the blendshape into our blendshapestate struct
                smr.sharedMesh.GetBlendShapeFrameVertices(bd.shapeIndex, bd.frameIndex, bd.deltaVertices, bd.deltaNormals, bd.deltaTangents);

                //convert a blendshape name from something like Genesis2Male__FBMHeavy to FBMHeavy
                int bdIndex = bd.name.LastIndexOf("__");
                if (bdIndex > -1)
                {
                    bd.name = bd.name.Substring(bdIndex + 2);
                }

                if(nameWhitelist != null && nameWhitelist.Count>0)
                {
                    if (!nameWhitelist.Contains(bd.name))
                    {
                        continue;
                    } else
                    {
                        UnityEngine.Debug.Log("Matched: " + bd.name);
                    }
                }

                float percent = 0f;
                if (totalCount > 0)
                {
                    percent = (float)totalProcessed / (float)totalCount;
                }

                if (useProgressBar)
                {
                    //TODO: we need to move this back into editor land, as this function is ONLY used if you're using the production tool suite
                    //EditorUtility.DisplayProgressBar("Extracting Blends", "Blend: " + bd.name, percent);
                }

                string relativePath = bd.name + "." + extension;
                string filePath = dirPath + "/" + relativePath;

                MCS_Utilities.Morph.MorphData morphData = new MCS_Utilities.Morph.MorphData();

                morphData.name = bd.name;
                morphData.blendshapeData = bd;

                WriteMorphDataToFile(morphData, filePath, true, false);

                manifest.names[i] = bd.name;

                totalProcessed += 1;
            }

            if (generateManifest)
            {
                Stream fs = File.Create(manifestPath);
                string json = JsonUtility.ToJson(manifest);
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
                fs.Write(bytes, 0, bytes.Length);
                fs.Close();
            }

            return manifestPath;
        }

        /// <summary>
		/// Builds a manifest .json file ro the resources folder by scanning folders/files
		/// </summary>
		/// <returns>The manifest by crawling.</returns>
		/// <param name="baseName">This is a collection name, like MCS Female, Jersey Girl, etc</param>
		public static MorphManifest GenerateManifestByCrawling(string runtimePath, string baseName)
        {
            string baseDir = runtimePath;// + "/" + baseName;
            string manifestPath = runtimePath + "/" + "manifest.json";
            string[] fileNames;
            List<string> names = new List<string>();

            MorphManifest manifest = new MorphManifest();


            if (Application.isEditor)
            {
                //if we're in editor land, we can do this efficiently by getting a list of files
                fileNames = Directory.GetFiles(baseDir, "*.morph*", SearchOption.AllDirectories);
            }
            else
            {
                //if we're not in editor land, we have to ask for the system to load the resources for us, and look at the names, this means they have to be loaded into memory
                UnityEngine.Object[] objs = Resources.LoadAll(baseDir);
                fileNames = new string[objs.Length];
                int j = 0;
                foreach (GameObject obj in objs)
                {
                    fileNames[j++] = obj.name;
                }
                //we don't need these anymore
                for (j = 0; j < objs.Length; j++)
                {
                    UnityEngine.Object.Destroy(objs[j]);
                }
            }

            Regex pattern = new Regex(@"(\.morph\.gz|\.morph)$");

            for (int i = 0; i < fileNames.Length; i++)
            {
                //is this a file we care about?
                Match m = pattern.Match(fileNames[i]);
                if (!m.Success)
                {
                    continue;
                }

                //if we have a string like foo/bar/car.morph
                // we want "car"

                names.Add(Paths.GetFileNameFromFullPath(fileNames[i],true));
            }

            manifest.name = baseName;
            manifest.count = names.Count;
            manifest.names = names.ToArray();

            Stream fs = File.Create(manifestPath);
            string json = JsonUtility.ToJson(manifest);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            fs.Write(bytes, 0, bytes.Length);
            fs.Close();

            return manifest;
        }

        /// <summary>
        /// Generates all blendshape state manifests by recursing through the resources folder
        /// </summary>
        /// <returns>The number of morphs found</returns>
        /// <param name="basePath">If set, will filter to only generate from a parent directory</param>
        public static int GenerateManifestsRecursively(string basePath = "")
        {
            int count = 0;
            Dictionary<string, int> baseNamesMap = new Dictionary<string, int>();
            List<string> baseNames = new List<string>();
            string[] fileNames;

            //we only need to find directories that contain the morphs, which will look like:
            //MCS/Content/{COLLECTION_NAME}/morph/Resources/{ASSET_ID}/CBJacket__Aged_Body1.morph.gz
            //NOTE ^^ that is what it will eventually look like, once I have the right IDs, until then it's going to look a little different
            //TODO: the paths need to be refactored

            string baseDir = (basePath.Length > 0 ? "/" : "") + basePath;

            if (!Application.isEditor)
            {
                throw new UnityException("You can't run GenerateManifestsRecursively outside of the editor, try instead GenerateManifestByCrawling");
            }

            MorphManifest manifest;

            fileNames = Directory.GetFiles(baseDir, "*.morph*", SearchOption.AllDirectories);

            for (int i = 0; i < fileNames.Length; i++)
            {
                string fileName = fileNames[i];

                //get the parent directory of the filename
                int posEnd = fileName.LastIndexOf(@"\");

                if (posEnd <= -1)
                {
                    continue; //invalid file, there is no directory indicator
                }

                int posStart = fileName.Substring(0, posEnd).LastIndexOf(@"\");

                if (posStart <= -1)
                {
                    posStart = 0;
                }

                string directoryName = fileName.Substring(posStart + 1, posEnd - (posStart + 1));

                if (directoryName.Length <= 0)
                {
                    continue;
                }

                //don't regenerate the same
                if (baseNamesMap.ContainsKey(directoryName))
                {
                    continue;
                }

                manifest = GenerateManifestByCrawling(baseDir,directoryName);
                count += manifest.count;

                baseNamesMap[directoryName] = manifest.count;
            }

            return count;
        }

        public static void WriteMorphDataToFile(MCS_Utilities.Morph.MorphData morphData, string filePath, bool refresh = false, bool compress = false)
        {
            if (!File.Exists(filePath) || refresh)
            {
                //FYI, I first tried a JSON ascii format but the file size was way too big, thus we're using a binary struct format
                //The blendshape cereal structure takes care of very efficiently packing a blendshape (about 28x byte compression)

                byte[] bytes = MCS_Utilities.Morph.MorphData.ConvertMorphDataToBytes(morphData);

                if (compress)
                {
                    Stream ms = new MemoryStream(bytes);
                    ms.Position = 0;
                    Stream outStream = Compression.CompressStream(ms);
                    outStream.Position = 0;
                    bytes = Compression.StreamToBytes(outStream);
                    ms.Close();
                    outStream.Close();
                }

                System.IO.File.WriteAllBytes(filePath, bytes);
            }
        }

        /// <summary>
        /// Take all extracted morphs and pack them into a mr file
        /// </summary>
        /// <param name="rootFolderPath"></param>
        /// <returns>The path to the mr file</returns>
        public static string MergeMorphsIntoMR(string rootFolderPath, string outputFile = null)
        {
            rootFolderPath = rootFolderPath.TrimEnd('/');

            if (outputFile == null)
            {
                outputFile = rootFolderPath + "/morphs.mr";
            }

            UnityEngine.Debug.Log("Output file: " + outputFile);

            MCSResource.MergeFiles(rootFolderPath, outputFile, "*.morph");
            return outputFile;
        }

        /// <summary>
        /// Take all extracted morphs and pack them into a mr file
        /// </summary>
        /// <param name="rootFolderPath"></param>
        /// <param name="searchPattern"></param>
        /// <returns>The path to the mr file</returns>
        public static string MergeFilesIntoMR(string rootFolderPath, string searchPattern, string outputFile = null, bool isInstallFile = false)
        {
            rootFolderPath = rootFolderPath.TrimEnd('/');

            if (outputFile == null)
            {
                outputFile = rootFolderPath + "/morphs.mr";
            }

            UnityEngine.Debug.Log("Root Folder :" + rootFolderPath + "  Output file: " + outputFile + "Search Pattern : " + searchPattern);
             
            MCSResource.MergeFiles(rootFolderPath, outputFile, searchPattern, true, true);
            return outputFile;
        }

    }
}
