using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCS.Utility.Schematic.Enumeration;

namespace MCS.Utility.Schematic
{
    public class AssetDependency
    {
        public int attempts = 0;
        public string srcPath = null; //The asset that HAS a dependency
        public HashSet<string> paths = new HashSet<string>(); //The asset(s) that the src element is depending on (other mon files, mats, textures, etc)
        public HashSet<string> GUIDs = new HashSet<string>(); //material guids to find
        public HashSet<string> textures = new HashSet<string>(); //Same as paths, but just for textures
        public HashSet<string> fbxPaths = new HashSet<string>(); //Same as paths, but just for textures
        public AssetSchematic[] schematics = null;
        bool analyzed = false; //true once dependencies crawled

        public void DetermineAllDependencies(bool refresh = false)
        {
            if(analyzed && !refresh)
            {
                //don't rescan
                return;
            }

            if(schematics == null)
            {
                UnityEngine.Debug.LogWarning("No schematics found, aborting");
                return;
            }

            foreach (AssetSchematic schematic in schematics)
            {
                if (schematic == null)
                {
                    UnityEngine.Debug.LogWarning("Schematic is null and it shouldn't be, check your AssetSchematic");
                    continue;
                }

                if (schematic.structure_and_physics != null)
                {
                    if (schematic.structure_and_physics.item_structure != null)
                    {
                        //alpha masks and assigned materials
                        if (schematic.structure_and_physics.item_structure.alpha_masks != null)
                        {
                            foreach (string path in schematic.structure_and_physics.item_structure.alpha_masks)
                            {
                                textures.Add(path);
                            }
                        }

                        if (schematic.structure_and_physics.item_structure.assigned_materials != null)
                        {
                            foreach (string guid in schematic.structure_and_physics.item_structure.assigned_materials)
                            {
                                GUIDs.Add(guid);
                            }
                        }
                    }

                    if (schematic.structure_and_physics.material_structure != null)
                    {
                        //material textures

                        //TODO: we should do something better here
                        var matdef = schematic.structure_and_physics.material_structure;

                        if (!string.IsNullOrEmpty(matdef.albedo))
                        {
                            textures.Add(matdef.albedo);
                        }
                        if (!string.IsNullOrEmpty(matdef.metal))
                        {
                            textures.Add(matdef.metal);
                        }
                        if (!string.IsNullOrEmpty(matdef.normal))
                        {
                            textures.Add(matdef.normal);
                        }
                        if (!string.IsNullOrEmpty(matdef.detail_normal))
                        {
                            textures.Add(matdef.detail_normal);
                        }
                        if (!string.IsNullOrEmpty(matdef.height))
                        {
                            //optional, not required
                            //textures.Add(matdef.height);
                        }
                        if (!string.IsNullOrEmpty(matdef.transparency))
                        {
                            textures.Add(matdef.transparency);
                        }
                        if (!string.IsNullOrEmpty(matdef.emission))
                        {
                            //optional, not required
                            //textures.Add(matdef.emission);
                        }
                    }
                }
            }

            //Add all the textures to the main paths
            foreach (string texturePath in textures)
            {
                string trimmed = texturePath != null ? texturePath.Trim() : null;
                if (!String.IsNullOrEmpty(trimmed))
                {
                    paths.Add(trimmed);
                }
            }

            AddFBXToDependenciesIfNeeded();

            analyzed = true;
        }

        public void AddFBXToDependenciesIfNeeded()
        {
            if(srcPath == null)
            {
                return;
            }
            if(
                //artitst tools
                (schematics[0].type_and_function.primary_function == PrimaryFunction.item && schematics[0].type_and_function.artisttools_function == ArtistToolsFunction.item)
                || 
                //moonshot
                (schematics[0].type_and_function.primary_function == PrimaryFunction.unknown && (schematics[0].type_and_function.artisttools_function == ArtistToolsFunction.geometry || schematics[0].type_and_function.artisttools_function == ArtistToolsFunction.item))
            )
            {
                string fbxPath = srcPath.Replace(".mon", ".fbx");
                fbxPaths.Add(fbxPath);
                paths.Add(fbxPath);
            }
        }


        #region c# helpers
        //use the src path as the "key" for comparison
        public override int GetHashCode()
        {
            return srcPath.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            AssetDependency other = (AssetDependency)obj;
            return this.srcPath.Equals(other.srcPath);
        }
        #endregion
    }
}
