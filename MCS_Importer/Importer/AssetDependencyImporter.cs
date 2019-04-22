using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using UnityEngine;
using UnityEditor;

using MCS.CONTENTLIBRARY;
using MCS.Utility.Schematic;
using MCS.Utility.Schematic.Enumeration;
using M3D_DLL;

namespace M3DIMPORT
{
    //Adds extra helper support for depenedencies if run in importer
    public class AssetDependencyImporter : AssetDependency
    {
        public HashSet<string> unmetDependencies = new HashSet<string>();
        public bool HasAllDependencies()
        {
            string dirPath = MCS_Utilities.Paths.ConvertFileToDir(srcPath);

            unmetDependencies.Clear();
            foreach(string key in paths)
            {
                string path = MCS_Utilities.Paths.ConvertRelativeToAbsolute(dirPath, key);

                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (obj == null)
                {
                    unmetDependencies.Add(path);
                }
            }

            //if we ourselves are a .mat or .prefab generaiton...
            if (schematics[0].type_and_function.primary_function == PrimaryFunction.material)
            { 
                string matPath = srcPath.Replace(".mon", ".mat");
                //file exists but unity hasn't imported it yet, this means we need to update the mat, but we can't b/c it isn't loaded, we'll try again
                if (File.Exists(matPath))
                {
                    UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(matPath);
                    if (obj == null)
                    {
                        unmetDependencies.Add(matPath);
                    }
                }
            }
            if(
                //artitst tools
                (schematics[0].type_and_function.primary_function == PrimaryFunction.item && schematics[0].type_and_function.artisttools_function == ArtistToolsFunction.item)
                || 
                //moonshot
                (schematics[0].type_and_function.primary_function == PrimaryFunction.unknown && schematics[0].type_and_function.artisttools_function == ArtistToolsFunction.geometry)
            )
            {
                string prefabPath = srcPath.Replace(".mon", ".prefab");
                if (File.Exists(prefabPath))
                {
                    UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(prefabPath);
                    if (obj == null)
                    {
                        unmetDependencies.Add(prefabPath);
                    }
                }
            }


            MonDeserializer monDes = new MonDeserializer ();
            foreach(string guid in GUIDs)
            {
                bool found = false;
                bool checkDisk = true;

                //does this GUID exist internally in the schematics?
                foreach (AssetSchematic schematic in schematics)
                {
                    if(schematic == null || schematic.origin_and_description == null || String.IsNullOrEmpty(schematic.origin_and_description.mcs_id) || !guid.Equals(schematic.origin_and_description.mcs_id))
                    {
                        continue;
                    }

                    if (schematic.type_and_function == null || schematic.type_and_function.primary_function != PrimaryFunction.material)
                    {
                        continue;
                    }

                    string matPath = schematic.stream_and_path.generated_path;
                    if (String.IsNullOrEmpty(matPath))
                    {
                        matPath = dirPath + "/Materials/" + schematic.origin_and_description.name + ".mat";
                    }
                    else
                    {
                        matPath = MCS_Utilities.Paths.ConvertRelativeToAbsolute(dirPath, matPath);
                    }

                    //fix for maya
                    matPath = matPath.Replace(":", "_");

                    /*
                    string baseFBXPath = dirPath + "/Materials/material__" + guid + ".mat";
                    string baseFBXMetaPath = dirPath + "/Materials/material__" + guid + ".mat.meta";
                    if (File.Exists(baseFBXPath))
                    {
                        File.Delete(baseFBXPath);
                    }
                    if (File.Exists(baseFBXMetaPath))
                    {
                        File.Delete(baseFBXMetaPath);
                    }
                    */

                    UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(matPath);

                    if (obj != null)
                    {
                        found = true;
                    }
                    else
                    {
                        //we know the path now
                        unmetDependencies.Add(matPath);
                    }

                    checkDisk = false;
                }

                if (checkDisk)
                {
                    string[] paths = Directory.GetFiles(dirPath, "*.mon", SearchOption.AllDirectories);
                    for (int i = 0; i < paths.Length; i++)
                    {
                        string path = paths[i].Replace(@"\", "/");
                        AssetSchematic[] otherSchematics = monDes.DeserializeMonFile(path);
                        if (otherSchematics[0].origin_and_description.mcs_id.Equals(guid))
                        {
                            //if the guid was a mat, make sure the mat exists
                            if (otherSchematics[0].type_and_function.primary_function == PrimaryFunction.material)
                            {
                                string matPath = path.Replace(".mon", ".mat");
                                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(matPath);
                                if (obj != null)
                                {
                                    found = true;
                                }
                                else
                                {
                                    //we know the path now
                                    unmetDependencies.Add(path);
                                }
                            }
                            else
                            {
                                found = true;
                            }
                            break;
                        }

                    }
                }

                if (!found)
                {
                    unmetDependencies.Add("Material: " + guid);
                }
            }

            return unmetDependencies.Count == 0;
        }
    }
}
