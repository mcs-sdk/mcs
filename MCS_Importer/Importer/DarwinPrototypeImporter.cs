using UnityEngine; 

using UnityEditor; 
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Linq;

using MCS.CONSTANTS;
using MCS.COSTUMING;
using MCS.UTILITIES;
using MCS.CORESERVICES;
using MCS;
using MCS.CONTENTLIBRARY;
using M3D_DLL;
using MCS.Item;
using MCS.Utility.Schematic;
using MCS.Utility.Schematic.Enumeration;

namespace M3DIMPORT
{
	class DarwinPrototypeImporter : AssetPostprocessor {


		// various absolute strings
		private const string ROOT_FOLDER = "Assets/MCS";
		private const string INJECTION_FOLDER = "/InjectionMasks";
		private const string RESOURCES_FOLDER = "Assets/MCS/Resources";
		private const string PREFAB_FOLDER = "Assets/MCS/Prefabs";
		private const string RESOURCES_PREFAB_FOLDER = "Assets/MCS/Resources/Prefabs";
		private const string GENERATED_MATERIALS_FOLDER = "Assets/MCS/Generated Materials";
		private const string RESOURCES_MATERIALS_FOLDER = "Assets/MCS/Resources/Materials";
        protected static Dictionary<string, AssetSchematic> schematicLookup = new Dictionary<string, AssetSchematic>();

        public override int GetPostprocessOrder()
        {
            return 4;
        }
        /// <summary>
        /// AssetPostprocessor event raised before models are procesed.
        /// </summary>
        void OnPreprocessModel ()
		{
			// ensure that the root folder is existant
			if (AssetDatabase.IsValidFolder (ROOT_FOLDER) == false)
				AssetDatabase.CreateFolder ("Assets", "MCS");
			// ensure that the resources folder is existant
			if (AssetDatabase.IsValidFolder (RESOURCES_FOLDER) == false)
				AssetDatabase.CreateFolder (ROOT_FOLDER, "Resources");


            if (assetPath.Contains(ROOT_FOLDER))
            {
                if (assetPath.EndsWith(".fbx") || assetPath.EndsWith(".obj"))
                {
                    ModelImporter mi = (ModelImporter)assetImporter;
                    mi.importMaterials = false;
                }
            }

		}

        public static string ConvertRelativeToAbsolute(string path, string basePath)
        {
            string finalPath = path.Replace("./", basePath + "/");
            return finalPath;
        }

        //we replace the default handler so the asset is properly serialized, otherwise it will not be b/c the Texture2D is not serialized if loaded dynamically (byte[] vs using a unity asset)
        public static Texture2D GetTextureFromPathEditor(string path, string basePath)
        {
            string finalPath = MCS_Utilities.Paths.ConvertRelativeToAbsolute(basePath,path);

            Texture2D tmp = AssetDatabase.LoadAssetAtPath<Texture2D>(finalPath);
            if(tmp == null)
            {
                UnityEngine.Debug.LogWarning("Failed to load texture at: " + finalPath);
            }

            return tmp;
        }       
        public static Material GetMaterialFromGUIDEditor(string guid, string basePath)
        {
            AssetSchematic schematic = new AssetSchematic();
            if (schematicLookup.TryGetValue(guid, out schematic))
            {
                try
                {
                    if (schematic != null
                                && schematic.origin_and_description != null
                                && schematic.origin_and_description.mcs_id != null
                                && schematic.origin_and_description.mcs_id.Equals(guid))
                    {                        
                        string matPath = MCS_Utilities.Paths.ConvertRelativeToAbsolute(basePath, schematic.stream_and_path.generated_path);
                        //fix for maya
                        matPath = matPath.Replace(":", "_");
                        Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                        return m;
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
            else
            {
                string[] paths = Directory.GetFiles(basePath, "*.mon", SearchOption.AllDirectories);
                foreach (string path in paths)
                {
                    try
                    {
                        schematic = AssetSchematic.CreateFromJSON(File.ReadAllText(path));
                        if (schematic != null
                            && schematic.origin_and_description != null
                            && schematic.origin_and_description.mcs_id != null
                            && schematic.origin_and_description.mcs_id.Equals(guid))
                        {

                            string monDir = MCS_Utilities.Paths.ConvertFileToDir(path);

                            string matPath = MCS_Utilities.Paths.ConvertRelativeToAbsolute(monDir, schematic.stream_and_path.generated_path);
                            Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                            return m;
                        }
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                }
            }

            UnityEngine.Debug.LogWarning("Failed to locate material for GUID: " + guid);
            return null;
        }

        static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) 
		{
            ContentLibrarySO content_so = (ContentLibrarySO)Resources.Load ("ContentLibrarySO");
			bool created = false;
        
            //set to true if we find a new asset we weren't tracking before, used to clear out our depenedency psuedo graph and prevent loops
            bool newAssetImported = false;
            HashSet<string> pathsToReimport = new HashSet<string>();

			if (content_so == null) {
				created = true;
				content_so = ScriptableObject.CreateInstance<ContentLibrarySO>();
			}

			foreach (var str in importedAssets)
			{
                //UnityEngine.Debug.Log("importing: " + str);
                if (!content_so.importerSeenPaths.Contains(str))
                {
                    content_so.importerSeenPaths.Add(str);
                    newAssetImported = true;
                }

                if (str.Contains(".mr"))
                {
                    // Only process install files here. morph files will be handled separately. 
                    if (!(str.Contains(".morph.mr") || str.Contains(".morphs.mr")))
                    {
                        Debug.Log("Found MR File. ");
                        MCS_Utilities.MCSResource resource = new MCS_Utilities.MCSResource();
                        resource.Read(str, false);

                        foreach(string key in resource.header.Keys)
                        {
                            string outputDir = String.Empty;
                            if (key.Contains(".morph") || key.Contains(".bin"))
                            outputDir = System.IO.Path.Combine(Application.streamingAssetsPath, key);
                            else
                            outputDir = Path.Combine(Path.Combine(Path.GetDirectoryName(str), Path.GetFileNameWithoutExtension(str)), key);
                            //Debug.Log("Output File :" + outputDir);
                            resource.UnpackResource(key, outputDir);
                        }
                        AssetDatabase.Refresh();
                    }
                }
				if (str.Contains (".mon")) {
                    schematicLookup.Clear();
                    int tmpPos = str.LastIndexOf('/');
                    string dirMon = Path.GetDirectoryName(str);// str.Substring(0, tmpPos);

                    AssetSchematic[] schematics;

                    AssetDependency ad = null;
                    AssetDependencyImporter adi = null;

                    bool skipAsset = false;

                    if (!content_so.importerDependencies.TryGetValue(str,out ad))
                    {
                        MonDeserializer monDes = new MonDeserializer ();
                        schematics = monDes.DeserializeMonFile (str);

                        adi = new AssetDependencyImporter();
                        adi.srcPath = str;
                        adi.schematics = schematics;
                        adi.DetermineAllDependencies();
                        content_so.importerDependencies.Add(str,adi);
                    } else
                    {
                        adi = (AssetDependencyImporter)ad;
                        schematics = adi.schematics;
                    }

                    if (!adi.HasAllDependencies())
                    {
                        adi.attempts++;
                        pathsToReimport.Add(str);
                        skipAsset = true;
                    }


                    AssetCreator ac = new AssetCreator();
                    if (!TryToImportMaterialsFromSchematics(str, dirMon, schematics))
                    {
                        //we have missing materials
                        continue;
                    }

                    if (skipAsset)
                    {
                        continue;
                    }

                    //clean up the variables before re-using them. 
                    //look at the first schematic to determine the "MAIN" function, this is legacy, eventually assetschematicimporter will handle this instead
                    AssetSchematic mon = schematics[0];

                    ac = new AssetCreator();

                    mon.stream_and_path.root_path = str;

                    switch (mon.type_and_function.primary_function)
                    {
                        case PrimaryFunction.material:
                            break;
                        default:
                            //handle an FBX (clothing, hair, figure, etc)
                            string fbxFilePath = str.Replace(".mon", ".fbx");
                            if (File.Exists(fbxFilePath))
                            {
                                //use a custom texture and material locator
                                AssetSchematicImporter assetSchematicImporter = new AssetSchematicImporter(null, null, GetTextureFromPathEditor, GetMaterialFromGUIDEditor);
                                assetSchematicImporter.basePath = dirMon;
                                GameObject fbxGO = AssetDatabase.LoadAssetAtPath<GameObject>(fbxFilePath);
                                if (fbxGO == null)
                                {
                                    UnityEngine.Debug.LogError("Missing required FBX: " + fbxFilePath);
                                    //monDes.ResaveMonFile(str);
                                    break;
                                }
                                if (fbxGO != null)
                                {
                                    List<AssetSchematic> schematicsList = new List<AssetSchematic>();
                                    foreach (AssetSchematic schematic in schematics)
                                    {
                                        schematicsList.Add(schematic);
                                    }
                                    GameObject outputGO = assetSchematicImporter.CreateGameObjectFromExistinGameObject(fbxGO, schematicLookup, schematicsList);
                                    //one note, components like Animator and LODGroup will be STRIPPED as they are not included in the component whitelist from CreateGameObjectFromExistinGameObject
                                    if (ImportUtilities.RemapMorphsIfRequired(outputGO))
                                    {
                                        content_so.refreshOnComplete = true;
                                    }
                                    //AssetDatabase.Refresh();

                                    string prefabFilePath = str.Replace(".mon", ".prefab");

                                    GameObject prefabObj;
                                    if (!File.Exists(prefabFilePath))
                                    {
                                        //UnityEngine.Debug.Log("File does not exist: " + prefabFilePath);
                                        prefabObj = PrefabUtility.CreatePrefab(prefabFilePath, outputGO, ReplacePrefabOptions.Default); //force a completely new and clean prefab, do not allow old values to transfer over
                                    }
                                    else
                                    {
                                        prefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFilePath);
                                        //UnityEngine.Debug.Log("Result of load: " + (prefabObj == null ? "null" : "not null"));
                                        if (prefabObj != null)
                                        {
                                            prefabObj = PrefabUtility.ReplacePrefab(outputGO, prefabObj, ReplacePrefabOptions.ConnectToPrefab);
                                            //UnityEngine.Debug.Log("Update of load: " + (prefabObj == null ? "null" : "not null"));
                                        } else
                                        {
                                            //replace it, there is something wrong with it's state
                                            UnityEngine.Debug.LogWarning("Replacing prefab that is in unworkable state: " + prefabFilePath);
                                            prefabObj = PrefabUtility.CreatePrefab(prefabFilePath, outputGO, ReplacePrefabOptions.Default); //force a completely new and clean prefab, do not allow old values to transfer over
                                        }
                                    }

                                    //These components automatically come back, we'll FORCE them to be destroyed b/c we don't need them
                                    try
                                    {
                                        Animator animator = prefabObj.GetComponent<Animator>();
                                        LODGroup lodGroup = prefabObj.GetComponent<LODGroup>();

                                        if (animator != null)
                                        {
                                            GameObject.DestroyImmediate(animator, true);
                                        }
                                        if (lodGroup != null)
                                        {
                                            GameObject.DestroyImmediate(lodGroup, true);
                                        }
                                        PrefabUtility.RecordPrefabInstancePropertyModifications(prefabObj);

                                        if (Application.isPlaying)
                                        {
                                            GameObject.Destroy(outputGO);
                                        }
                                        else
                                        {
                                            GameObject.DestroyImmediate(outputGO, false);
                                        }
                                    } catch (Exception e)
                                    {
                                        UnityEngine.Debug.Log("Caught an exception during import component update");
                                        UnityEngine.Debug.LogException(e);
                                    }
                                }
                            }
                            break;
                    }
				} 
			}

			foreach (var str in deletedAssets) 
			{
				if(str.Contains ("MCS") && str.Contains (".fbx"))
				{
					AssetSchematic mon = content_so.AssetSchematicList.Where(x => x.stream_and_path.source_path == str).SingleOrDefault ();
                    if (mon != null)
                    {
                        AssetDatabase.DeleteAsset(mon.stream_and_path.generated_path);
                        mon.stream_and_path.generated_path = "";
                        mon.stream_and_path.source_path = "";
                        if (mon.stream_and_path.root_path == "")
                        {
                            content_so.DeleteItem(mon);
                        }
                    }
				}
				else if (str.Contains ("MCS") && str.Contains (".prefab")) {
					AssetSchematic mon = content_so.AssetSchematicList.Where(x => x.stream_and_path.generated_path == str).SingleOrDefault ();
					if (mon != null) {
						if (AssetDatabase.LoadAssetAtPath (mon.stream_and_path.generated_path,typeof(GameObject)) == null) {
							mon.stream_and_path.generated_path = "";
						}

					}

				}
				else if (str.Contains ("MCS") && str.Contains (".mon")) {
					AssetSchematic mon = content_so.AssetSchematicList.Where(x => x.stream_and_path.root_path == str).SingleOrDefault ();
                    if (mon != null && mon.stream_and_path != null)
                    {
                        mon.stream_and_path.root_path = "";
                        if (mon.stream_and_path.source_path == "")
                        {
                            content_so.DeleteItem(mon);
                        }
                    }
				}
				else if (str.Contains ("MCS") && str.Contains (".mat")) {
					AssetSchematic mon = content_so.AssetSchematicList.Where(x => x.stream_and_path.generated_path == str).SingleOrDefault ();
					if (mon != null && mon.stream_and_path.generated_path != null && AssetDatabase.LoadAssetAtPath (mon.stream_and_path.generated_path,typeof(Material)) == null) {
						mon.stream_and_path.generated_path = "";
					}


				}


			}

			for (var i=0; i<movedAssets.Length; i++)
			{
				//Debug.Log("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);
			}

			if (created) {
				if (AssetDatabase.IsValidFolder (ROOT_FOLDER) == false) {
					AssetDatabase.CreateFolder ("Assets", "MCS"); 
				} 
				if (AssetDatabase.IsValidFolder (RESOURCES_FOLDER) == false) {
					AssetDatabase.CreateFolder (ROOT_FOLDER, "Resources"); 
				}

				if (AssetDatabase.IsValidFolder (RESOURCES_PREFAB_FOLDER) == false) {
					AssetDatabase.CreateFolder (RESOURCES_FOLDER, "Prefabs"); 
				}

				if (AssetDatabase.IsValidFolder (RESOURCES_MATERIALS_FOLDER) == false) {
					AssetDatabase.CreateFolder (RESOURCES_FOLDER, "Materials"); 
				}

				AssetDatabase.CreateAsset(content_so,RESOURCES_FOLDER + "/ContentLibrarySO.asset");

			}


            bool recursed = false;

            if (newAssetImported)
            {
                foreach(string path in pathsToReimport)
                {
                    //UnityEngine.Debug.Log("Reimporting unmet dependency asset: " + path);
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);

                    AssetDependencyImporter adi = (AssetDependencyImporter)content_so.importerDependencies[path];
                    if (adi != null)
                    {
                        foreach(string dependencyPath in adi.unmetDependencies)
                        {
                            if(dependencyPath.StartsWith("Material: "))
                            {
                                continue;
                            }

                            if (content_so.importerSeenPaths.Contains(dependencyPath))
                            {
                                continue;
                            }

                            AssetDatabase.ImportAsset(dependencyPath, ImportAssetOptions.ForceSynchronousImport);
                            recursed = true;
                        }
                    }
                }
            } else
            {
                //nothing has changed, so stop
                //UnityEngine.Debug.Log("Flushing dependencies graph");
                foreach(string key in content_so.importerDependencies.Keys)
                {
                    AssetDependencyImporter adi = (AssetDependencyImporter)content_so.importerDependencies[key];
                    if (adi.unmetDependencies.Count > 0)
                    {
                        UnityEngine.Debug.LogError("Missing dependencies for: " + key);
                        foreach(string dp in adi.unmetDependencies)
                        {
                            UnityEngine.Debug.Log("Unmet: " + dp);
                        }
                    }
                }
                content_so.ClearDependencies();
            }

            if(!recursed && content_so.refreshOnComplete)
            {
                content_so.refreshOnComplete = false;
                //This causes an error with the prefab, but does work
                //AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                //This does not work
                //AssetDatabase.ImportAsset("Assets/StreamingAssets");
            }

			EditorUtility.SetDirty (content_so);
		}

        public static bool TryToImportMaterialsFromSchematics(string str, string dirMon, AssetSchematic[] schematics)
        {

            bool materialCreationStatus = true;
            AssetCreator ac = new AssetCreator();

            //let's look for any materials in the mon file, if we find any create and import them now before we move on
            for (int i = 0; i < schematics.Length && materialCreationStatus; i++)
            {
                AssetSchematic mon = schematics[i];
                if (mon.origin_and_description != null && !String.IsNullOrEmpty(mon.origin_and_description.mcs_id))
                {
                    schematicLookup[mon.origin_and_description.mcs_id] = mon;
                }
                if (mon.type_and_function.artisttools_function == ArtistToolsFunction.material || mon.type_and_function.primary_function == PrimaryFunction.material)
                {
                    try
                    {
                        Dictionary<string, Texture2D> textureDict = new Dictionary<string, Texture2D>();
                        bool textureLoadStatus = TextureLoader.GetTextures(mon, dirMon, out textureDict);
                        if (textureLoadStatus == false)
                        {
                            //UnityEngine.Debug.LogError("Failed to find textures for material: " + str);
                            continue;
                        }

                        Material mat = ac.CreateMorphMaterial(mon, textureDict);

                        if (mon.stream_and_path.generated_path == "" || mon.stream_and_path.generated_path == null)
                        {

                            //mon.stream_and_path.generated_path = GENERATED_MATERIALS_FOLDER + "/" + mon.origin_and_description.name + ".mat";
                            string newDir = dirMon + "/Materials";
                            if (!Directory.Exists(newDir))
                            {
                                Directory.CreateDirectory(newDir);
                            }
                            mon.stream_and_path.generated_path =  "Materials/" + mon.origin_and_description.name + ".mat";

                        }

                        int pos = mon.stream_and_path.generated_path.LastIndexOf('/');
                        string directoryPath = mon.stream_and_path.generated_path.Substring(0, pos);

                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }

                        //does a material already exist, if so, replace over it
                        string dstPath = MCS_Utilities.Paths.ConvertRelativeToAbsolute(dirMon, mon.stream_and_path.generated_path);

                        //Convert incompatible maya ":" to "_"
                        dstPath = dstPath.Replace(":", "_");

                        //UnityEngine.Debug.Log("dstPath: " + dstPath);

                        if (!File.Exists(dstPath))
                        {
                            AssetDatabase.CreateAsset(mat, dstPath);
                        }
                        else
                        {
                            Material oldMat = AssetDatabase.LoadAssetAtPath<Material>(dstPath);
                            if (oldMat == null)
                            {
                                //UnityEngine.Debug.LogError("Unable to update material because we can't load it: " + dstPath);
                            }
                            else
                            {
                                oldMat.CopyPropertiesFromMaterial(mat);
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        Debug.LogWarning("Material creation failed. " + e.Message);
                        materialCreationStatus = false;
                    }
                }
            }

            return materialCreationStatus;
        }

        
		public HierarchyRank GetHierarchyRank(GameObject go, string[] names, object[] values){

			HierarchyRank rank = HierarchyRank.unknown;

			for (int i = 0; i < names.Length; i++) {
				//our placeholder values - make sure you dont use a previous parsed bad value!!!
				string name = names [i];
				string string_value;

				switch(name){
				case "hierarchy_rank":
					Debug.Log ("RANK");
					string_value = values [i].ToString ();
					rank = MCS.Utilities.EnumHelper.ParseEnum<HierarchyRank> (string_value);
					break;
				}
			}

			return rank;
		}

		public void ConfigureSkeleton(GameObject go, string[] names, object[] values){
			CSBoneService bs = go.GetComponent<CSBoneService> ();
			if (bs == null)
				bs = go.AddComponent<CSBoneService> ();
		}
	}
}
