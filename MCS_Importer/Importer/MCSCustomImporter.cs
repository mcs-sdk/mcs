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
using MCS.FOUNDATIONS;
using MCS.COSTUMING;
using MCS.UTILITIES;
using MCS.CORESERVICES;
using MCS.SERVICES;
using MCS;
using MCS.Utility.Schematic;
using MCS.Utility.Schematic.Enumeration;

namespace M3DIMPORT
{
	/// <summary>
	/// Asset Importing class ultimately inherits from UnityEditor
	/// </summary>
	public class MCSCustomImporter : AssetPostprocessor
	{
		// various absolute strings
		private const string ROOT_FOLDER = "Assets/MCS";
		private const string INJECTION_FOLDER = "/InjectionMasks";
		private const string RESOURCES_FOLDER = "Assets/MCS/Resources";

		// private List<string> figure_bone_names;


        public override int GetPostprocessOrder()
        {
            return 5;
        }

		/// <summary>
		/// AssetPostprocessor event raised before models are procesed.
		/// </summary>
		void OnPreprocessModel ()
		{
			// ensure that the root folder is existant
			if (AssetDatabase.IsValidFolder(ROOT_FOLDER) == false)
				AssetDatabase.CreateFolder("Assets", "MCS");
			// ensure that the resources folder is existant
			if (AssetDatabase.IsValidFolder(RESOURCES_FOLDER) == false)
				AssetDatabase.CreateFolder(ROOT_FOLDER, "Resources");
		}
		


		/// <summary>
		/// AssetPostprocessor event raised for each GameObject with user properties imported.
		/// </summary>
		/// <param name="go">The GameObject being imported.</param>
		/// <param name="names">The names of the user properties.</param>
		/// <param name="values">The values of the user properties.</param>
		void OnPostprocessGameObjectWithUserProperties (GameObject go, string[] names, object[] values)
		{
            for(int i = 0; i < names.Length; i++)
            {
                //did we find an asset schematic from aritst tools, if so add our model
                if(names[i] == "AssetSchematic")
                {
                    MCS.Item.MCSItemModel model = go.AddComponent<MCS.Item.MCSItemModel>();
                    model.schematic = AssetSchematic.CreateFromJSON(values[i].ToString());
                }
                if(names[i] == "mcs_id")
                {
                    var property = go.AddComponent<MCS.Item.MCSProperty>();
                    property.mcs_id = values[i].ToString();
                }
            }

            //stop processing if we aren't in the Assets/MCS folder
			if (!assetPath.Contains (ROOT_FOLDER)) {
				return;
			}

            //If we find an fbx file, and we ALSO find a mon file with the same name, then skip processing the fbx file
            if (assetPath.EndsWith(".fbx"))
            {
                string alt = assetPath.Replace(".fbx", ".mon");
                if (File.Exists(alt))
                {
                    //Debug.Log("FOUND MON FILE :" + alt);
                    return;
                }
            }

			//We want to know if we are processing old content or new awesome content. Assume the worst.
			IMPORTER_TYPE is_it_old_and_bad = IMPORTER_TYPE.LEGACY;
			if (names.Contains("AssetSchematic"))
			{
				is_it_old_and_bad = IMPORTER_TYPE.ASSETSCHEMATIC;
			}
			//Give it a chance to be new awesome content
			else if (names.Contains("hierarchy_rank") && names.Contains("id"))
			{
				is_it_old_and_bad = IMPORTER_TYPE.KEYVALUE;
			}

			//the most future sighted way to handle this would to not be so "boolean"
			switch (is_it_old_and_bad)
			{
				case IMPORTER_TYPE.LEGACY:
					ProcessLegacyUserPropertyContent(go, names, values);
					break;
				case IMPORTER_TYPE.KEYVALUE:
					//we'll handoff processing based on what it is, for nicer, cleaner processing
					HierarchyRank rank = GetHierarchyRank(go, names, values);
					switch (rank)
					{
						case HierarchyRank.collection:
							break;
						case HierarchyRank.item:
							ConfigureItem(go, names, values);
							break;
						case HierarchyRank.skeleton:
							ConfigureSkeleton(go, names, values);
							break;
						case HierarchyRank.geometry:
							ConfigureGeometry(go, names, values);
							break;
						default:
							Debug.LogWarning("MCS: Unkown Hierarchy Rank");
							break;
					}
					break;
				case IMPORTER_TYPE.ASSETSCHEMATIC:
					ProcessContentWithAssetSchematic(go,  GetAssetSchematic(names,values));
					break;
			}
		}




		/***********************************************************************/
		/*********************** BEGIN DARWIN IMPORTER *************************/
		/***********************************************************************/

		public void ConfigureItem(GameObject go, string[] names, object[] values)
		{
			//all items share this component
			CoreMeshMetaData metadata = go.GetComponent<CoreMeshMetaData>();
			if (metadata == null)
				metadata = go.AddComponent<CoreMeshMetaData>();
			metadata.PopulateValuesFromImportArrays(names, values);
		}

		public void ConfigureSkeleton(GameObject go, string[] names, object[] values)
		{
			CSBoneService bs = go.GetComponent<CSBoneService>();
			if (bs == null)
				bs = go.AddComponent<CSBoneService>();
		}

		public void ConfigureGeometry(GameObject go, string[] names, object[] values){
			CoreMesh core_mesh = go.GetComponent<CoreMesh> ();
			if (core_mesh == null)
				core_mesh = go.AddComponent<CoreMesh> ();
			core_mesh.ID = GetId (go, names, values);
			core_mesh.meshType = ItemFunctionToMeshType (GetItemFunction (go, names, values));
		}
		private string GetId(GameObject go, string[] names, object[] values){
			for (int i = 0; i < names.Length; i++) {
				if(names[i] == "id"){
					return values[i].ToString();
				}
			}
			return go.name;
		}
		private string GetItemFunction(GameObject go, string[] names, object[] values){
			for (int i = 0; i < names.Length; i++) {
				if(names[i] == "item_function"){
					return values[i].ToString();
				}
			}
			return "unkown";
		}

		private MESH_TYPE ItemFunctionToMeshType(ItemFunction function)
		{
			switch (function)
			{
				case ItemFunction.rigid_wearable:
				case ItemFunction.soft_wearable:
					return MESH_TYPE.CLOTH;
				case ItemFunction.prop:
					return MESH_TYPE.PROP;
				case ItemFunction.hair:
					return MESH_TYPE.HAIR;
				case ItemFunction.figure:
					return MESH_TYPE.BODY;
				default:
					return MESH_TYPE.UNKNOWN;

			}
		}
		private MESH_TYPE ItemFunctionToMeshType(string function)
		{
			switch (function)
			{
			case "soft_wearable":
				return MESH_TYPE.CLOTH;
			case "prop":
				return MESH_TYPE.PROP;
			case "hair":
				return MESH_TYPE.HAIR;
			case "figure":
				return MESH_TYPE.BODY;
			default:
				return MESH_TYPE.UNKNOWN;
				
			}
		}
		public void ConfigureItem(GameObject go, AssetSchematic schematic)
		{
			//all items share this component
			Debug.Log("Configure Item - Schematic : " + schematic.origin_and_description.mcs_id + " "+ schematic.origin_and_description.name); 
			CoreMeshMetaData metadata = go.GetComponent<CoreMeshMetaData>();
			Debug.Log(" Core Mesh Meta data is null = " + (metadata == null).ToString());
			if (metadata == null)
				metadata = go.AddComponent<CoreMeshMetaData>();
			Debug.Log(" Core Mesh Meta data is null = " + (metadata == null).ToString());
			metadata.PopulateValuesFromAssetSchematic(schematic);
			metadata.meshType = ItemFunctionToMeshType(schematic.type_and_function.item_function);
		}

		public void ConfigureSkeleton(GameObject go)
		{
			CSBoneService bs = go.GetComponent<CSBoneService>();
			if (bs == null)
				bs = go.AddComponent<CSBoneService>();
		}

		public void ConfigureGeometry(GameObject go,AssetSchematic schematic)
		{
			CoreMesh core_mesh = go.GetComponent<CoreMesh>();
			if (core_mesh == null)
				core_mesh = go.AddComponent<CoreMesh>();
			core_mesh.ID = (String.IsNullOrEmpty(schematic.origin_and_description.mcs_id))? go.name : schematic.origin_and_description.mcs_id;
			core_mesh.meshType = ItemFunctionToMeshType(schematic.type_and_function.item_function);
		}
		public void ProcessContentWithAssetSchematic(GameObject go, AssetSchematic schematic)
		{
			//we'll handoff processing based on what it is, for nicer, cleaner processing

			switch (schematic.type_and_function.hierarchy_rank)
			{
				case HierarchyRank.collection:
					break;
				case HierarchyRank.item:
					Debug.Log(go.name + " Schematic.name " + schematic.origin_and_description.name);
					ConfigureItem(go,schematic);
					break;
				case HierarchyRank.skeleton:
					ConfigureSkeleton(go);
					break;
				case HierarchyRank.geometry:
					ConfigureGeometry(go, schematic);
					break;
				default:
					Debug.LogWarning("MCS: Unkown Hierarchy Rank");
					break;
			}
		}

		public AssetSchematic GetAssetSchematic(string[] names, object[] values)
		{
			AssetSchematic schematic = null; 
			for (int i = 0; i < names.Length; i++)
			{
				//our placeholder values - make sure you dont use a previous parsed bad value!!!
				string name = names[i];
				string string_value;

				switch (name)
				{
					case "AssetSchematic":
						string_value = values[i].ToString();
						schematic = AssetSchematic.CreateFromJSON(string_value);
						break;
				}
			}
			return schematic;
		}

		public HierarchyRank GetHierarchyRank(GameObject go, string[] names, object[] values)
		{

			HierarchyRank rank = HierarchyRank.unknown;

			for (int i = 0; i < names.Length; i++)
			{
				//our placeholder values - make sure you dont use a previous parsed bad value!!!
				string name = names[i];
				string string_value;

				switch (name)
				{
					case "hierarchy_rank":
						string_value = values[i].ToString();
						rank = EnumHelper.ParseEnum<HierarchyRank>(string_value);
						break;
				}
			}

			return rank;
		}

		/***********************************************************************/
		/************************* END DARWIN IMPORTER *************************/
		/***********************************************************************/






		private void ProcessLegacyUserPropertyContent(GameObject go, string[] names, object[] values)
		{
            //UnityEngine.Debug.Log("Legacy item: " + go.name + " " + names.Length + " values: " + values.Length);
			//hack to ignore bones
			bool not_bone = (names.Length > 2) ? true : false;
			Regex lodRegex = new Regex (@"\.Shape|\.LOD|LOD_?[0-9]+$");

			if (containsBoneinfluenceWeights(names) == true)
			{

				not_bone = false;

				// this is not the best place to put adding the bone service because it needs to crawl children and children dont exist
				// until theOnPostProcessModel

				//add bone serivce if hip bone and on figure skeleton

				if (go.name == "hip" && go.transform.parent.name.Contains("Genesis") || go.transform.parent.name.Contains("M3D") || go.transform.parent.name.Contains("MCS"))
				{
					if (go.transform.parent.name.Contains("Male") || go.transform.parent.name.Contains("Female"))
					{
						CSBoneService bone_service = go.GetComponent<CSBoneService>();
						if (bone_service == null)
							bone_service = go.AddComponent<CSBoneService>();
						bone_service.showBonePositions = false;
					}
					else {
						//						Debug.Log ("HIP NOT MALE OR FEMALE");
					}
				}
				else {
					//					Debug.Log ("NOT HIP");
				}
			}
			else {
				//				Debug.Log ("NO BONE INFLUENCE WEIGHTS");
			}

			// if it contains a LOD model, it is a bone
			Match m = lodRegex.Match(go.name);
			if (m.Success) {
				not_bone = false;
			}

			// if we are possibly NOT a bone, we'll figure out what to do with this node
			if (not_bone == true)
			{
				// get the coremeshmetadata component (add if not existant)		
				CoreMeshMetaData metadata = go.GetComponent<CoreMeshMetaData>();
				if (metadata == null)
					metadata = go.AddComponent<CoreMeshMetaData>();

				// this will be dynamic in the future
				if(String.IsNullOrEmpty(metadata.versionId))
					metadata.versionId = "0.01";
				if(String.IsNullOrEmpty(metadata.vendorId))
					metadata.vendorId = "DAZ3D";


				//lets default to geometry id being the incoming geometry name
				if(String.IsNullOrEmpty(metadata.geometryId))
					metadata.geometryId = go.name;

				// iterate through the names array
				for (int i = 0; i < names.Length; i++)
				{

					// what is this name?
					switch (names[i])
					{

						case "StudioNodeName":
							metadata.geometryId = values[i].ToString();
							break;

						case "StudioNodeLabel":
							bool is_fig = false;
							if (go.name.Contains("Genesis") || go.transform.parent.name.Contains("M3D") || go.transform.parent.name.Contains("MCS"))
							{
								if (go.name.Contains("Male") || go.name.Contains("Female"))
								{
									//is_fig = true;
								}
							}
							if (is_fig == false)
							{
								go.name = values[i].ToString();
								metadata.ID = values[i].ToString();

							}
							break;

						case "StudioPresentationType":
							parsePresentationType(metadata, values[i].ToString());
							break;

						case "StudioPresentationAutoFitBase":
							metadata.declarativeUse = values[i].ToString();
							break;

						case "StudioPresentationPreferredBase":
							metadata.compatabilityBase = values[i].ToString();
							break;

						case "ControllerScales":
							metadata.controllerScales = convertControllerScaleStringToDictionary(values[i].ToString());
							break;

					}
				}

			}
			else if (not_bone == false)
			{

				// it is a bone - potentially

			}
		}




		//we have this ugly string of key value stuff we need to store in a list of specical object so it's serializable
		//at runtime, we should convert this to a dictionary so it's fast for lookup.
		/// <summary>
		/// Internal method
		/// </summary>
		/// <returns>The controller scale string to dictionary.</returns>
		/// <param name="constrollers_string">Constrollers string.</param>
		private List<KeyValueFloat> convertControllerScaleStringToDictionary(string constrollers_string){
			
			List<KeyValueFloat> lookup_table = new List<KeyValueFloat>();
			string[] root_separators = {";"};
			string[] value_separators = {"="};
			string[] controllers = constrollers_string.Split(root_separators, StringSplitOptions.RemoveEmptyEntries);
			foreach(string controller in controllers){
				string[] key_value = controller.Split(value_separators, StringSplitOptions.RemoveEmptyEntries);
				KeyValueFloat lookup = new KeyValueFloat();
				lookup.key = key_value[0];
				lookup.value = float.Parse(key_value[1]);
				lookup_table.Add(lookup);
			}
			
			return lookup_table;
		}
		
		bool containsBoneinfluenceWeights(string[] names){
			
			bool contains_influence_weights = false;
			foreach(string name in names){
				if(name == "lockInfluenceWeights"){
					contains_influence_weights = true;
				}
			}
			
			return contains_influence_weights;
		}
		
		
		void parsePresentationType(CoreMeshMetaData metadata, string presentation_type){

			metadata.meshType = MESH_TYPE.UNKNOWN;//default should be unkown until it's known

			if(presentation_type.Contains("Character")){
				metadata.meshType = MESH_TYPE.BODY;
			}

			if(presentation_type.Contains("/Eyes")){
				metadata.meshType = MESH_TYPE.CLOTH;
			}
			
			if(presentation_type.Contains("Wardrobe")){
				metadata.meshType = MESH_TYPE.CLOTH;
			}
			
			if(presentation_type.Contains("Accessory")){
				metadata.meshType = MESH_TYPE.CLOTH;
			}
			
			if(presentation_type.Contains("Prop")){
				metadata.meshType = MESH_TYPE.PROP;
			}
			
			if(presentation_type.Contains("Hair")){
				metadata.meshType = MESH_TYPE.HAIR;
			}
			
		}


		void ParseItemType(CoreMeshMetaData metadata, string mcsType){

			metadata.meshType = MESH_TYPE.UNKNOWN;//default should be unkown until it's known

			if(mcsType.Contains("figure")){
				metadata.meshType = MESH_TYPE.BODY;
			}

			if(mcsType.Contains("cloth")){
				metadata.meshType = MESH_TYPE.CLOTH;
			}

			if(mcsType.Contains("prop")){
				metadata.meshType = MESH_TYPE.PROP;
			}

			if(mcsType.Contains("hair")){
				metadata.meshType = MESH_TYPE.HAIR;
			}

		}

		void OnPreprocessTexture () {

			if (!assetPath.Contains (ROOT_FOLDER)) {
				return;
			}

			if (assetPath.Contains(INJECTION_FOLDER)) {
				TextureImporter textureImporter  = (TextureImporter) assetImporter;
				textureImporter.textureType = TextureImporterType.Advanced;
				textureImporter.isReadable = true;
				textureImporter.maxTextureSize = 1024;
				textureImporter.wrapMode = TextureWrapMode.Clamp;
				textureImporter.textureFormat = TextureImporterFormat.DXT1;
			}
		}

		/// <summary>
		/// AssetPostprocessor event raised for each entire FBX, after it is imported and finished processing.
		/// </summary>
		/// <param name="go">The GameObject that has just been imported.</param>
		void OnPostprocessModel (GameObject go)
		{
            //UnityEngine.Debug.Log("go: " + go.name);
			if (!assetPath.Contains (ROOT_FOLDER)) {
				return;
			}

			/******************************************************/
			/***************** COSTUME PROCESSING *****************/
			/******************************************************/

			// we may need to treat asset packs differently at the end - like setting costumItem attached = false
			bool asset_pack_only = true;
			bool is_morph_geometry = false;

			// we need to move teh coremetadata over to the actual geometry
			CoreMeshMetaData[] meta_data_list = go.GetComponentsInChildren<CoreMeshMetaData> ();
			if (meta_data_list != null && meta_data_list.Length > 0)
				is_morph_geometry = true;

			if(is_morph_geometry == true){
				foreach(CoreMeshMetaData metadata in meta_data_list){
					//assetpacks can come in with coremetadata declaring a body exists, but there migt not be a body

					bool valid_body = false;
					SkinnedMeshRenderer[] potential_meshes = null;

					switch (metadata.meshType) {

					case MESH_TYPE.UNKNOWN:
						Debug.LogWarning ("WARNING : Costume Item is of MESHTYPE UNKOWN. There is something wrong with the incoming FBX metdata for : "+ go.name);
						break;

					case MESH_TYPE.BODY:
							// lets start by removing any false CIBody
							SkinnedMeshRenderer[] all_possible_body_geometry = metadata.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();

							foreach (SkinnedMeshRenderer sknmshr in all_possible_body_geometry)
							{
								if (string.IsNullOrEmpty(sknmshr.name) == false && string.IsNullOrEmpty(metadata.geometryId) == false)
								{
									if (sknmshr.name.Contains(metadata.geometryId) && sknmshr.name.Contains("_CAP") == false && sknmshr.name.ToLower().Contains("_feathered") == false && sknmshr.name.ToLower().Contains("_opaque") == false)
									{
										valid_body = true;
									}
									if (metadata.function == ItemFunction.figure && metadata.rank == HierarchyRank.item)
										valid_body = true;
								}

							}
							if (valid_body)
							{
								asset_pack_only = false;

								CIbody ci_body = metadata.gameObject.GetComponent<CIbody>();
								if (ci_body == null)
									ci_body = metadata.gameObject.AddComponent<CIbody>();
								ci_body.isAttached = true;

								potential_meshes = metadata.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
								foreach (SkinnedMeshRenderer pomesh in potential_meshes)
								{
									if (pomesh.name.Contains("Eyes") == false)
									{
										if (pomesh.name.Contains(metadata.geometryId) == true ||(metadata.function == ItemFunction.figure && metadata.rank == HierarchyRank.item))
										{
											
											CoreMesh core_mesh = pomesh.gameObject.GetComponent<CoreMesh>();
											if (core_mesh == null)
												core_mesh = pomesh.gameObject.AddComponent<CoreMesh>();
											core_mesh.meshType = metadata.meshType;

											ci_body.dazName = metadata.geometryId;
											ci_body.ID = metadata.ID;
											ci_body.meshType = metadata.meshType;

											if (String.IsNullOrEmpty(core_mesh.dazName))
												core_mesh.dazName = metadata.geometryId;
											if (String.IsNullOrEmpty(core_mesh.ID))
												core_mesh.ID = metadata.ID;

											if (core_mesh.meshType == MESH_TYPE.UNKNOWN)
												core_mesh.meshType = metadata.meshType;

											ci_body.meshType = metadata.meshType;

											// set backup texture
											try
											{
												ci_body.backupTexture = ci_body.GetSkinnedMeshRenderer().sharedMaterial.mainTexture as Texture2D;
											}
											catch(Exception e)
											{
												// WHy??
												Debug.Log(e.Message);
											}
										}
									}
								}
								ci_body.DetectCoreMeshes();
							}
							else
							{
								UnityEngine.Object.DestroyImmediate(metadata);
							}
							break;

						case MESH_TYPE.CLOTH:
						CIclothing ci_cloth = metadata.gameObject.GetComponent<CIclothing>();
						if(ci_cloth == null)
							ci_cloth = metadata.gameObject.AddComponent<CIclothing>();
						potential_meshes = metadata.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();

						ci_cloth.isAttached = false;
						ci_cloth.meshType = metadata.meshType;

						try {
							// find injection masks if available
							string current_directory = StripFileName (assetPath);
							string injection_mask_folder = current_directory + INJECTION_FOLDER;
							if (AssetDatabase.IsValidFolder (injection_mask_folder) == true) {
								string file_name = string.Format ("{0}/{1}_INJECTION_MASK.png", injection_mask_folder, metadata.geometryId);
								Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D> (file_name) as Texture2D;
								if (texture != null) {
									ci_cloth.alphaMask = texture;
								}
							}
						} catch {
						}

						//add coremesh to child geometry
						foreach(SkinnedMeshRenderer pomesh in potential_meshes){
							CoreMesh core_mesh = pomesh.gameObject.GetComponent<CoreMesh>();
							if(core_mesh == null)
								core_mesh = pomesh.gameObject.AddComponent<CoreMesh>();

							core_mesh.meshType = metadata.meshType;
							ci_cloth.dazName = core_mesh.dazName = metadata.geometryId;
							ci_cloth.ID = core_mesh.ID = metadata.ID;
						}
						ci_cloth.DetectCoreMeshes ();
						break;
						
					case MESH_TYPE.PROP:
						CIprop ci_prop = metadata.gameObject.GetComponent<CIprop> ();
						if (ci_prop == null)
							ci_prop = metadata.gameObject.AddComponent<CIprop> ();
						ci_prop.isAttached = false;

						MeshRenderer[] potential_meshs = metadata.gameObject.GetComponentsInChildren<MeshRenderer>();
						foreach(MeshRenderer pomesh in potential_meshs){
							CoreMesh core_mesh = pomesh.gameObject.GetComponent<CoreMesh>();
							if (core_mesh == null)
								core_mesh = pomesh.gameObject.AddComponent<CoreMesh>();
							core_mesh.meshType = metadata.meshType;

							ci_prop.dazName = core_mesh.dazName = metadata.geometryId;
							ci_prop.ID = core_mesh.ID = metadata.ID;


							ci_prop.basePosition = ci_prop.transform.localPosition;
							ci_prop.baseRotation = ci_prop.transform.localEulerAngles;
							ci_prop.meshType = metadata.meshType;
						}

						ci_prop.DetectCoreMeshes ();
						break;
						
					case MESH_TYPE.HAIR:

						Shader errorShader = Shader.Find ("Hidden/InternalErrorShader");
						Shader volundShader = Shader.Find ("MCS/Volund Variants/Standard Hair");

						CIhair ci_hair = metadata.gameObject.GetComponent<CIhair> ();
						if (ci_hair == null)
							ci_hair = metadata.gameObject.AddComponent<CIhair> ();
						ci_hair.isAttached = false;
						potential_meshes = metadata.gameObject.GetComponentsInChildren<SkinnedMeshRenderer> ();

						foreach (SkinnedMeshRenderer pomesh in potential_meshes) {
							CoreMesh core_mesh = pomesh.gameObject.GetComponent<CoreMesh> ();
							if (core_mesh == null)
								core_mesh = pomesh.gameObject.AddComponent<CoreMesh> ();
							core_mesh.meshType = metadata.meshType;
							ci_hair.dazName = core_mesh.dazName = metadata.geometryId;
							ci_hair.ID = core_mesh.ID = metadata.ID;
							ci_hair.meshType = metadata.meshType;
						}

						//Are our hair material shaders broken and can we fix it?
						if (errorShader != null && volundShader != null) {
							string basePath = assetPath.Substring (0, assetPath.LastIndexOf ("/")) + "/MCSMaterials";

							string[] lookIn = { basePath };

							string[] guids = AssetDatabase.FindAssets ("t:material", lookIn);
							foreach (string guid in guids) {
								string matPath = AssetDatabase.GUIDToAssetPath (guid);
								Material m = AssetDatabase.LoadAssetAtPath<Material> (matPath) as Material;

								if (m.shader.GetHashCode () == errorShader.GetHashCode ()) {
									//Debug.LogWarning ("The hair shader for: " + metadata.name + " / " + matPath + " was assigned to a shader reference you don't have, reassigning to Volund shader. Consider updating your base mcs figure asset.");
									m.shader = volundShader;
								}
							}
						}

						ci_hair.DetectCoreMeshes();
						break;
					}

					//We only need this for our packing, so temporarily disable it...
                    if(metadata.meshType == MESH_TYPE.BODY || metadata.meshType == MESH_TYPE.CLOTH || metadata.meshType == MESH_TYPE.HAIR)
                    {
                        #region MorphInferredMeta
                        SkinnedMeshRenderer[] smrArray = go.GetComponentsInChildren<SkinnedMeshRenderer>();
                        foreach (SkinnedMeshRenderer smr in smrArray)
                        {
                            InferredMeta meta = new InferredMeta(assetPath, go, smr);
                            CoreMesh coreMesh = smr.GetComponent<CoreMesh>();
                            if (coreMesh == null)
                            {
                                Debug.LogWarning("Skipping: " + smr.name + " at: " + assetPath + ", it does not contain a CoreMesh Component");
                                continue;
                            }

                            coreMesh.runtimeMorphPath = meta.morphPathKey;
                        }
                        #endregion

                    }

                } //end foreach(CoreMeshMetaData metadata in meta_data_list)

				/******************************************************/
				/********************* ASSET PACK *********************/
				/******************************************************/
				if (asset_pack_only == true) {

					CIclothing[] clothing = go.GetComponentsInChildren<CIclothing> (true);
					foreach (CIclothing cloth in clothing) {
						cloth.isAttached = false;
					}

					CIhair[] hairs = go.GetComponentsInChildren<CIhair> (true);
					foreach (CIhair hair in hairs) {
						hair.isAttached = false;
					}

					CIprop[] props = go.GetComponentsInChildren<CIprop> (true);
					foreach (CIprop prop in props) {
						prop.isAttached = false;
					}
				}

				/******************************************************/
				/****************** BONE PROCESSING *******************/
				/******************************************************/
				
				// this bone service should get attached at user data level, but there arent child object at the time, so we setup here
				CSBoneService bone_service = go.GetComponentInChildren<CSBoneService> ();
				if (bone_service != null) {
					addDefaultAttachmentPointsToBones (bone_service);

					// we have bones, so lets look to add jcts to it
					JCTTransition trans = go.GetComponentInChildren<JCTTransition> ();
					if (trans == null) {
						CIbody temp_body = go.GetComponentInChildren<CIbody>();
						if(temp_body != null){
							CoreMeshMetaData meta = temp_body.gameObject.GetComponent<CoreMeshMetaData>();

							//attach the JCT component to the hip of the figure's bone
							JCTImportUtility.CreateJCTTransitionOnImport(bone_service.gameObject, temp_body.LODlist[0].gameObject , meta.geometryId, assetPath);

						}
					}
				}
				
				/******************************************************/
				/****************** CHARACTER MANAGER *****************/
				/******************************************************/
				// we ned to check if there is a CIBody object, because asset packs shouldnt have them, and therefore do not need a chracater manager,
				// but may have a false one due to import/export errors
				CIbody body = go.GetComponentInChildren<CIbody> ();
				if (body != null) {
					//we add a character manager instance last
					MCSCharacterManager char_man = go.GetComponent<MCSCharacterManager> ();
					if (char_man == null) {
						char_man = go.AddComponent<MCSCharacterManager> ();
					}
				}

				// lets remove teh lodgroup for now
				LODGroup lod_group = go.GetComponent<LODGroup> ();
				if (lod_group != null) {
					lod_group.enabled = false;
					UnityEngine.Object.DestroyImmediate (lod_group);
				}








				//eyeball hack - added in 1.0r2
				List<GameObject> eyeballs = new List<GameObject>();
				FindGTwoEyeballs (go.transform, eyeballs);

				if (eyeballs.Count > 0) {
					if (eyeballs [0].transform.parent.name != "G2FSimplifiedEyes") {
						GameObject eye_group = new GameObject ("G2FSimplifiedEyes");

						eye_group.transform.parent = eyeballs[0].transform.parent;
						foreach (GameObject eye in eyeballs) {
							eye.transform.parent = eye_group.transform;
							CoreMesh core_eye = eye.AddComponent<CoreMesh> ();
							core_eye.dazName = eye.name;
							core_eye.meshType = MESH_TYPE.CLOTH;
							core_eye.ID = core_eye.dazName;

							SkinnedMeshRenderer smr = eye.GetComponent<SkinnedMeshRenderer> ();

							InferredMeta meta = new InferredMeta(assetPath,eye,smr);
							CoreMesh coreMesh = smr.GetComponent<CoreMesh>();
							if(coreMesh == null){
								continue;
							}
							Debug.Log("Updating eye: " + meta.collectionName + " | " + meta.morphPathKey);
							Debug.Log("Extraction path: " + meta.morphExtractionPath);

							coreMesh.runtimeMorphPath = meta.morphPathKey;
						}

						CIclothing cloth = eye_group.AddComponent<CIclothing> ();
						cloth.ID = eye_group.name;
						cloth.dazName = eye_group.name;
						cloth.meshType = MESH_TYPE.CLOTH;
						cloth.isAttached = false;
						cloth.DetectCoreMeshes();

					}
				}

                // Load Morph Group Info
                MorphGroupService.GenerateMorphGroupsFromFile(body.dazName);

            }//if morph geometry end


        }

		//TODO: used just for 1.5, for 2.0 we'll use a .mon file that has meta data
		//NOTE: this requires Editor api calls, so it can't be done at runitme, it should only be done during import of the asset
		public struct InferredMeta {
			public string morphPathKey; //we store this in the coremesh so that we know where to fetch the .morph.gz.bytes resources
			public string morphExtractionPath;
			public string collectionName; //JerseyGirl
			public string itemName; //Pants
			public string itemSubName; //LOD0
			public bool success;

			public InferredMeta (string objAssetPath, GameObject obj, SkinnedMeshRenderer smr){
				string path = objAssetPath;
				StreamingMorphs sm = new StreamingMorphs();

				morphPathKey = "";
				morphExtractionPath = "";
				collectionName = "";
				itemName = "";
				itemSubName = "";
				success = false;


				// [...ContentPacks] / [Collection Name] / [sub dir(s)] / [Item Name] .fbx
				Regex regex = new Regex(@"(.+)/(Content)/([^\/]+)/(.+)/([^\/]+)\.fbx$");
				Regex regexLOD = new Regex(@"(LOD[0-9]+|Shape)");

				Match match = regex.Match(path);

				//Debug.Log("path: " + path);
				//Debug.Log("Match: " + (match.Success ? "true" : "false"));

				for(int i=0; i<match.Groups.Count; i++){
					//Debug.Log("Group: " + i + " | " + match.Groups[i].Value);
				}

				if(match.Success){
					collectionName = match.Groups[3].Value;//sm.PurifyKey(match.Groups[3].Value);
					itemName = match.Groups[5].Value;//sm.PurifyKey(match.Groups[5].Value); //we'll use the smr name portion, not the file path

					//TODO: this is just for 1.5 cleanup/legacy/etc issues, eventually we can remove this
					itemName = itemName.Replace("_LEGACY","");
					itemName = itemName.Replace("_CLEAN","");
					itemName = itemName.Replace("_OPTIMIZED","");
				}

				string itemNameB = sm.GetItemNameFromFullName(smr.name); //replace the existing item name with the submesh name
				if(itemNameB != null && itemNameB.Length>0){
					//this will be the case for something like "UMPants.Shape_LOD0"
					itemName = itemNameB;
					itemSubName = sm.GetItemSubNameFromFullName(smr.name);
				} else {
					//this will be the case for something like "MCSMale_LOD0"
					itemSubName = smr.name;
				}

				Match matchLOD = regexLOD.Match(itemSubName);
				if(!matchLOD.Success){
					Debug.LogWarning("Could not determine LOD level for object: " + itemSubName);
				}

				morphExtractionPath = "Assets/StreamingAssets/MCS/" + collectionName + "/" + itemName + "/" + itemSubName;


				morphPathKey = collectionName + "/" + itemName + "/" + itemSubName;
                if (objAssetPath.Contains("/MCS/"))
                {
                    morphPathKey = "MCS/" + morphPathKey;

                }
				success = true;
			}
		}

		Transform EyeRoot(Transform node){
			if (node.name == "EyeGroup") {
				return node;
			} else {
				foreach (Transform child_node in node) {
					Transform target_node = EyeRoot (node);
					if (target_node != null) {
						return target_node;
					}
				}
			}
			return null;
		}

		void FindGTwoEyeballs(Transform obj, List<GameObject> eyeball_list){
			if(obj.name.StartsWith("G2FSimplifiedEyes_")){
				eyeball_list.Add(obj.gameObject);
			}
			foreach (Transform child in obj) {
				FindGTwoEyeballs (child, eyeball_list);
			}
		}



		/// <summary>
		/// Removes the filename portion of a given filepath
		/// </summary>
		/// <returns>The file name.</returns>
		/// <param name="path">Path.</param>
		private string StripFileName (string path)
		{
			return path.Substring (0, path.LastIndexOf ("/"));
		}



		/// <summary>
		/// Returns the filename portion of a given filepath.
		/// </summary>
		/// <returns>The filename.</returns>
		/// <param name="path">Path.</param>
		private string GetFilename (string path)
		{
			string file_name = path.Substring (path.LastIndexOf ("/")+1);
			file_name = file_name.Substring (0, file_name.LastIndexOf ("."));
			return file_name;
		}



		/// <summary>
		/// Adds the default attachment points to bones.
		/// </summary>
		/// <param name="bone_service">CSBoneService bone_service</param>
		private void addDefaultAttachmentPointsToBones (CSBoneService bone_service)
		{
			// Debug.Log("ADDING DEFAULT ATTACHMENT POINTS");
			// make sure we have a config and get it
			string attachment_point_config_location = RESOURCES_FOLDER+"/MCS_AttachmentPointConfiguration.asset";
			MCSAttachmentPointConfiguration config = AssetDatabase.LoadAssetAtPath (attachment_point_config_location, typeof (MCSAttachmentPointConfiguration)) as MCSAttachmentPointConfiguration;

			if (config == null) {
				config = ScriptableObject.CreateInstance ("MCSAttachmentPointConfiguration") as MCSAttachmentPointConfiguration;
				config.attachmentPointPresets = new List<MCSAttachmentPointConfiguration.AttachmentPointPreset> ();
				MCSAttachmentPointConfiguration.AttachmentPointPreset mypreset = new MCSAttachmentPointConfiguration.AttachmentPointPreset ();
				mypreset.targetBone = "rHand";
				mypreset.autoMirror = true;
				mypreset.layoutClassName = "SinglePropDefaultLayout";
				config.attachmentPointPresets.Add (mypreset);
				AssetDatabase.CreateAsset (config,attachment_point_config_location);

				// EditorUtility.SetDirty(mypreset);
				// EditorUtility.SetDirty(config);
			}

			if (config.attachmentPointPresets.Count < 1) {
			}

			// for each preset check if bones exists, or attachmentopint already exists, then configure
			foreach (MCSAttachmentPointConfiguration.AttachmentPointPreset preset in config.attachmentPointPresets) {
				// Debug.Log("AP");
				Transform target_bone = bone_service.GetBoneByName(preset.targetBone); 
				if (target_bone != null) {
					// Debug.Log("FOUND BONE");
					string suffix = "";
					if (preset.autoMirror == true)
						suffix = "R";

					// set intended defaults for this bone
					Transform ap = target_bone.Find (target_bone.name + "AttachmentPoint" + suffix);
					if (ap == null) {
						// Debug.Log("NO EXITING");
						GameObject go = new GameObject ();
						go.name = target_bone.name + "AttachmentPoint" + suffix;
						go.AddComponent<CIattachmentPoint> ();
						ap = go.transform;
						ap.SetParent(target_bone);
					}

					ap.transform.localPosition = preset.positionOffset;
					ap.transform.localEulerAngles = preset.rotationOffset;

					// set default layoutobject
					addLayoutToAttachementPoint (ap, preset);

					// set defaults for mirrored bone if needed
					if (preset.autoMirror == true) {
						Transform mirrored_target = bone_service.getMirroredBoneOrNull (target_bone);
						if (mirrored_target == null) {
							//we'll use the current bone to mirror on
							mirrored_target = target_bone;
						}

						Transform m_ap = mirrored_target.Find (mirrored_target.name + "AttachmentPointL");
						if (m_ap == null) {
							GameObject go = new GameObject ();
							go.name = mirrored_target.name + "AttachmentPointL";
							CIattachmentPoint code= go.AddComponent<CIattachmentPoint> ();
							code.isMirror = true;
							m_ap = go.transform;
							m_ap.SetParent (mirrored_target);
						}
						
						m_ap.transform.localPosition = new Vector3 (preset.positionOffset.x * -1, preset.positionOffset.y, preset.positionOffset.z);
						m_ap.transform.localEulerAngles =  new Vector3 (preset.rotationOffset.x, preset.rotationOffset.y, preset.rotationOffset.z * -1);

						// set default layoutobject
						addLayoutToAttachementPoint (m_ap, preset);
					}
				} else {
				}
			}
		}



		/// <summary>
		/// Adds an Attachment Point APLayout component to the given transform.
		/// </summary>
		/// <param name="ap">The Transform recipient for the new component.</param>
		/// <param name="preset">Preset.</param>
		private void addLayoutToAttachementPoint (Transform ap, MCSAttachmentPointConfiguration.AttachmentPointPreset preset)
		{
			// set default layoutobject
			string layout_class = preset.layoutClassName;
			if (string.IsNullOrEmpty (layout_class)) {
				layout_class = "SinglePropDefaultLayout";
			}

			APLayout layout_object = ap.gameObject.GetComponent (FindTypeInLoadedAssemblies (layout_class)) as APLayout;
			if (layout_object == null)
				layout_object = ap.gameObject.AddComponent (FindTypeInLoadedAssemblies(layout_class)) as APLayout;

			//CIattachmentPoint ap_code = ap.gameObject.GetComponent<CIattachmentPoint>();
			//ap_code.setLayoutObject(layout_object as APLayout);
		}



		/// <summary>
		/// Returns a Type from a string name of that type - This is due to System.Type.GetType not working with Unity types.
		/// http://answers.unity3d.com/questions/497774/cant-get-type-by-type-temptype-typegettypeunityeng.html
		/// </summary>
		/// <returns>The found Type or null.</returns>
		/// <param name="typeName">String equivalent of the Type</param>
		private Type FindTypeInLoadedAssemblies (string typeName)
		{
			Type _type = null;
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies ()) {
				_type = assembly.GetType (typeName);
				if (_type != null)
					break;
			}
			
			return _type;
		}



		/// <summary>
		/// Internal method to clean the names of all child transforms from a given transform using a RegularExpression.
		/// </summary>
		/// <param name="bone">The initial transform.</param>
		/// <param name="rgx">The regular expression.</param>
		private void cleanAllBoneNames (Transform bone, Regex rgx)
		{
			bone.name = rgx.Replace (bone.name, "");
			foreach (Transform child in bone) {
				cleanAllBoneNames (child, rgx);
			}
		}
		


		/// <summary>
		/// Internal recursive method to build a list of all transforms from parent that start with a given string - seems unused at present.
		/// </summary>
		/// <param name="str">The search string.</param>
		/// <param name="current_object">The object to check.</param>
		/// <param name="stored_transforms">The List<Transform> of found transforms with "hip" being the first.</param>
		private void findAllTransformsThatStartWithThisString (string str, Transform current_object, List<Transform> stored_transforms)
		{
			if (current_object.name.StartsWith (str)) {
				// put the first hip instance first always - just as backup
				if (current_object.name == "hip") {
					stored_transforms.Insert (0, current_object);
				} else {
					stored_transforms.Add (current_object);
				}
			}
			
			foreach (Transform child_object in current_object) {
				findAllTransformsThatStartWithThisString (str, child_object, stored_transforms);
			}
		}

		static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) 
		{
			foreach (string str in importedAssets)
			{
				if (str.Contains (".fbx") && str.Contains (ROOT_FOLDER)) {

					string[] folders = str.Split ('/');

					string line = "";

					int folderLength = folders.Length - 1;

					for (int i = 2; i < folders.Length - 1; i++) {
						if (line == "") {
							string path = "";

							for (int j = 1; j < folderLength - 1; j++) {
								path += folders [j] + "/";
							}
							path += folders [folderLength - 1];

							string finalPath = Application.dataPath + "/" + path + "/Version.info";

							if (File.Exists(finalPath)) 
							{
								using (StreamReader reader = new StreamReader(finalPath))
								{
									line = reader.ReadToEnd ();
								}
							}
							folderLength--;
						}
					}

					if (line != "") {

						line = line.Replace ("\t", "");
						line = line.Replace ("\r", "");

						string[] lines = line.Split ('\n');
						string compatibility = "";

						for (int i = 0; i < lines.Length; i++) {

							if (lines [i] == "Compatibility") {
								compatibility = lines [i + 1];
							}
						}

						if (compatibility != "") {
							compatibility = compatibility.Replace ('r','0');

							float version;

							if (Single.TryParse (compatibility, out version)) {
								GameObject item = (GameObject)AssetDatabase.LoadAssetAtPath (str,typeof(GameObject));

								CoreMeshMetaData metadata = null;
								if (item != null) {
									metadata = item.GetComponentInChildren<CoreMeshMetaData>();
								}
								if (metadata != null) {
									if (metadata.mcs_version < version) {
										metadata.mcs_version = version;
									}

								}
							}

						}
						
					}



				}

			}
		}
	}
}
