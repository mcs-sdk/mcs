using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using MCS.SERVICES;
using MCS.FOUNDATIONS;
using System;

namespace M3DIMPORT
{
    class ImportUtilities
    {
        public static bool RemapMorphsIfRequired(GameObject go)
        {
            SkinnedMeshRenderer[] smrArray = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            StreamingMorphs sb = new StreamingMorphs();
            StreamingMorphs.LoadMainThreadAssetPaths();

            bool remapped = false;


            //we'll refresh this folder when we're done if we need to
            //string refreshDir = null;

            foreach (SkinnedMeshRenderer smr in smrArray)
            {
                Dictionary<int, int> tsMap = new Dictionary<int, int>();
                bool isTargetMapGenerated = false;
                //InferredMeta meta = new InferredMeta(assetPath, go, smr);
                CoreMesh coreMesh = smr.GetComponent<CoreMesh>();
                if (coreMesh == null)
                {
                    Debug.LogWarning("Skipping: " + smr.name + ", it does not contain a CoreMesh Component");
                    continue;
                }

                //ConvertBlendshapeFromMap
                #region Remapping
                ProjectionMeshMap pmm = new ProjectionMeshMap();
                string incompatMorphPath = coreMesh.runtimeMorphPath + "_incompat";
                string compatMorphPath = coreMesh.runtimeMorphPath;

                string incompatMorphPathAbsolute = Path.Combine(Application.streamingAssetsPath, incompatMorphPath);
                string compatMorphPathAbsolute = Path.Combine(Application.streamingAssetsPath, compatMorphPath);

                string incompatMorphPathAbsoluteMeta = incompatMorphPathAbsolute + ".meta";
                string compatMorphPathAbsoluteMeta = compatMorphPathAbsolute + ".meta";

                string mrPath = compatMorphPathAbsolute + ".morphs.mr";
                mrPath = mrPath.Replace(@"\", "/");

                bool incompatExists = Directory.Exists(incompatMorphPathAbsolute);
                bool compatExists = Directory.Exists(compatMorphPathAbsolute);

                if (compatExists)
                {
                    string compatProjectionFilePath = Path.Combine(compatMorphPathAbsolute, "projectionmap.bin");
                    string incompatProjectionFilePath = Path.Combine(incompatMorphPathAbsolute, "projectionmap.bin");
                    bool didFindProjectionFile = File.Exists(compatProjectionFilePath);

                    if (didFindProjectionFile)
                    {
                        var manifest = MCS_Utilities.MorphExtraction.MorphExtraction.GenerateManifestByCrawling(compatMorphPathAbsolute, compatMorphPathAbsolute.Substring(compatMorphPathAbsolute.LastIndexOf("/")));

                        if (manifest.names.Length <= 0)
                        {
                            UnityEngine.Debug.LogError("Unable to generate proper manifest in: " + compatMorphPathAbsolute + " => " + compatMorphPathAbsolute.Substring(compatMorphPathAbsolute.LastIndexOf("/")));
                        }

                        MCS_Utilities.Morph.MorphData sourceMD = sb.GetMorphDataFromResources(compatMorphPathAbsolute, manifest.names[0]);
                        pmm.Read(compatProjectionFilePath);

                        if (pmm.IsReMappingNeeded(smr, sourceMD, false))
                        {
                            try
                            {
                                int count = 0;
                                int total = manifest.names.Length;
                                if (incompatExists)
                                {
                                    //Delete the incompat directory.
                                    MCS_Utilities.Paths.TryDirectoryDelete(incompatMorphPathAbsolute);
                                }
                                //Move contents to incompat directory
                                Directory.Move(compatMorphPathAbsolute, incompatMorphPathAbsolute);
                                //Delete compat directory - clean up
                                //Directory.Delete(compatMorphPathAbsolute);
                                //Create compat directory.
                                Directory.CreateDirectory(compatMorphPathAbsolute);

                                foreach (string morph in manifest.names)
                                {

                                    EditorUtility.DisplayProgressBar("Processing Morphs...", morph, ((float)count / (float)total));
                                    try
                                    {
                                        MCS_Utilities.Morph.MorphData targetMD;
                                        //does this blendshape already exist, if so just continue and ignore it, and don't lookup from disk
                                        int blendshapeIndex = smr.sharedMesh.GetBlendShapeIndex(morph);
                                        if (blendshapeIndex >= 0)
                                        {
                                            continue;
                                        }

                                        sourceMD = sb.GetMorphDataFromResources(incompatMorphPathAbsolute, morph);

                                        //"Assets/MCS/Content/RRMale/Morph/Resources/LaidBack/LaidBackPants/projectionmap.bin");
                                        if (!isTargetMapGenerated)
                                        {
                                            tsMap = pmm.GenerateTargetToSourceMap(smr);
                                            isTargetMapGenerated = true;
                                        }
                                        targetMD = pmm.ConvertMorphDataFromMap(smr, sourceMD, tsMap);
                                        //MorphData mData = new MorphData { name = bsNew.name, meshName = smr.name, blendshapeState = bsNew };
                                        //var morphPath = meta.morphPathKey + "_NEW";

                                        MCS_Utilities.MorphExtraction.MorphExtraction.WriteMorphDataToFile(targetMD, compatMorphPathAbsolute + "/" + targetMD.name + ".morph", false, false);
                                        sourceMD = targetMD;
                                        //UnityEngine.Debug.Log("Converted");

                                    } catch (Exception e)
                                    {
                                        UnityEngine.Debug.LogException(e);

                                    }
                                    count++;

                                }
                                EditorUtility.ClearProgressBar();
                                //Copy the projectionmap.bin file to new folder.
                                if (isTargetMapGenerated)
                                {
                                    string saPath = Application.streamingAssetsPath;
                                    File.Copy(incompatProjectionFilePath, compatProjectionFilePath);
                                    //coreMesh.runtimeMorphPath = regeneratedMorphPath;
                                }

                                //generate an mr file
                                UnityEngine.Debug.Log("Creating MR: " + mrPath);
                                string baseMRPath = compatMorphPathAbsolute.Replace(@"\", @"/");
                                MCS_Utilities.MorphExtraction.MorphExtraction.MergeMorphsIntoMR(baseMRPath, mrPath);

                                if (Directory.Exists(compatMorphPathAbsolute))
                                {
                                    MCS_Utilities.Paths.TryDirectoryDelete(compatMorphPathAbsolute);
                                }
                                if (Directory.Exists(incompatMorphPathAbsolute))
                                {
                                    MCS_Utilities.Paths.TryDirectoryDelete(incompatMorphPathAbsolute);
                                }
                                if (File.Exists(incompatMorphPathAbsoluteMeta))
                                {
                                    File.Delete(incompatMorphPathAbsoluteMeta);
                                }
                                if (File.Exists(compatMorphPathAbsoluteMeta))
                                {
                                    File.Delete(compatMorphPathAbsoluteMeta);
                                }

                                remapped = true;

                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("Missing projection file. Can't remap!");
                    }
                }
                else
                {
                    //Debug.LogWarning("Directory containing morphs does not exist!");
                }
                #endregion
            }

            //NOTE: this does not work
            /*
            UnityEngine.Debug.LogWarning("Refresh Dir: " + refreshDir);

            if (!String.IsNullOrEmpty(refreshDir))
            {
                if (Directory.Exists(refreshDir))
                {
                    UnityEngine.Debug.Log("Refreshing: " + refreshDir);
                    AssetDatabase.ImportAsset(refreshDir);
                }
            }
            */
            return remapped;
        }
    }
}
