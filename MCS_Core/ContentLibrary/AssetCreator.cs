using System;
using UnityEngine;
using MCS.FOUNDATIONS;
using MCS.Item;

using MCS.Utility.Schematic;
using System.Collections.Generic;
using MCS.COSTUMING;

namespace M3D_DLL
{
	public class AssetCreator
	{
		//todo: expand this...
		/**
		schematic.origin_and_description.gender = MCS.Utility.Schematic.Enumeration.Gender.male;
		schematic.origin_and_description.vendor_name = "DAZ3D";
		schematic.type_and_function.item_function = MCS.Utility.Schematic.Enumeration.ItemFunction.soft_wearable;
		schematic.origin_and_description.collection_name = "UrbanMetro";
		schematic.origin_and_description.id = "UMPants";
		schematic.origin_and_description.name = "UMPants";
		schematic.version_and_control.item_version = 0.01;
		schematic.origin_and_description.description = "/Urban Metro Outfit for Genesis 2 Male(s)/Pants";
		schematic.version_and_control.compatibilities = new string[1]{ "/Genesis 2/Male" };
		schematic.version_and_control.mcs_version = 1.5;
		*/
		public GameObject CreateMorphGameObjectFromFbx(GameObject go, AssetSchematic schematic, Dictionary<string,Texture2D> textures,bool InstantiateNewObjectUponCreation = true)
		{

            UnityEngine.Debug.Log("CreateMorphGameObjectFromFbx: " + go.name + " | " + schematic.stream_and_path.source_path);

			GameObject gameObject = null;
			if (InstantiateNewObjectUponCreation) {
				gameObject = GameObject.Instantiate (go) as GameObject;
			} else {
				gameObject = go;
			}
			GameObject child = gameObject.transform.GetChild (0).gameObject;
			child.AddComponent <MCSItemModel> ();
			child.GetComponent <MCSItemModel> ().schematic = schematic;
			GameObject child2 = child.transform.GetChild (0).gameObject;

			//Adding the coremesh component to the root...ish gameobject of the item. Then we set the various values from the asset schematic (.mon file)
			child2.AddComponent <CoreMeshMetaData>();
			CoreMeshMetaData cmmd = child2.GetComponent <CoreMeshMetaData>();
			cmmd.vendorId = schematic.origin_and_description.vendor_name;
			cmmd.versionId = schematic.version_and_control.item_version.ToString ();

			switch(schematic.type_and_function.item_function){
				case MCS.Utility.Schematic.Enumeration.ItemFunction.soft_wearable:
					cmmd.meshType = MCS.CONSTANTS.MESH_TYPE.CLOTH;
					break;
				case MCS.Utility.Schematic.Enumeration.ItemFunction.rigid_wearable:
					cmmd.meshType = MCS.CONSTANTS.MESH_TYPE.CLOTH;
					break;
				case MCS.Utility.Schematic.Enumeration.ItemFunction.figure:
					cmmd.meshType = MCS.CONSTANTS.MESH_TYPE.BODY;
					break;
				case MCS.Utility.Schematic.Enumeration.ItemFunction.hair:
					cmmd.meshType = MCS.CONSTANTS.MESH_TYPE.HAIR;
					break;
				case MCS.Utility.Schematic.Enumeration.ItemFunction.prop:
					cmmd.meshType = MCS.CONSTANTS.MESH_TYPE.PROP;
					break;
				case MCS.Utility.Schematic.Enumeration.ItemFunction.unknown:
					cmmd.meshType = MCS.CONSTANTS.MESH_TYPE.UNKNOWN;
					break;
				default:
					cmmd.meshType = MCS.CONSTANTS.MESH_TYPE.UNKNOWN;
					break;
			}
			cmmd.geometryId = schematic.origin_and_description.mcs_id;
			cmmd.ID = schematic.origin_and_description.mcs_id;
			cmmd.declarativeUse = schematic.origin_and_description.description;
			cmmd.mcs_version = schematic.version_and_control.mcs_version;
			cmmd.collection_name = schematic.origin_and_description.collection_name;

			//Adding all the coremesh components to anything that has a skinned mesh renderer. We also add the paths to the runtime morphs here
			foreach (SkinnedMeshRenderer rend in gameObject.GetComponentsInChildren <SkinnedMeshRenderer>()) {
				rend.gameObject.AddComponent <CoreMesh>();

				CoreMesh cm = rend.gameObject.GetComponent <CoreMesh> ();
				cm.dazName = schematic.origin_and_description.name;
				cm.ID = schematic.origin_and_description.mcs_id;
				cm.meshType = cmmd.meshType;
				int index = -1;
				for (int i = 0; i < schematic.structure_and_physics.morph_structure.lodMorphObjectNames.Length; i++) {
					if (schematic.structure_and_physics.morph_structure.lodMorphObjectNames [i] == cm.gameObject.name) {
						index = i;
					}
				}
				if (index > -1) {
					cm.runtimeMorphPath = schematic.structure_and_physics.morph_structure.lodMorphLocations [index];
				}

			}

			//Adding the different CICostumeItem classes, ie. CIClothing, CIBody, CIHair, CIProp.
			switch (cmmd.meshType) {
			case MCS.CONSTANTS.MESH_TYPE.CLOTH:
				child2.AddComponent <CIclothing> ();
				CIclothing cicl = child2.GetComponent <CIclothing> ();
				cicl.dazName = schematic.origin_and_description.name;
				cicl.ID = schematic.origin_and_description.mcs_id;
				cicl.meshType = cmmd.meshType;
				cicl.DetectCoreMeshes ();
                //TODO: textures is a list of textures, not a dictionary, right?
                /*
				if (textures.ContainsKey ("alphaMask")) {
					cicl.alphaMask = textures ["alphaMask"];
				}
                */
				cicl.isAttached = false;
				break;
			case MCS.CONSTANTS.MESH_TYPE.BODY:
				//todo: all the figure stuff needs to be added here, ie. CharacterManager, Core Morphs, JCT stuff, etc

				child2.AddComponent <CIbody>();
				CIbody body = child2.GetComponent <CIbody> ();

				break;
			case MCS.CONSTANTS.MESH_TYPE.HAIR:
				child2.AddComponent <CIhair> ();
				CIhair hair = child2.GetComponent <CIhair> ();
				hair.dazName = schematic.origin_and_description.name;
				hair.ID = schematic.origin_and_description.mcs_id;
				hair.meshType = cmmd.meshType;
				hair.DetectCoreMeshes ();
				break;
			case MCS.CONSTANTS.MESH_TYPE.PROP:
				child2.AddComponent <CIprop> ();
				CIprop prop = child2.GetComponent <CIprop> ();
				prop.dazName = schematic.origin_and_description.name;
				prop.ID = schematic.origin_and_description.mcs_id;
				prop.meshType = cmmd.meshType;
				prop.DetectCoreMeshes ();
				prop.basePosition = prop.transform.localPosition;
				prop.baseRotation = prop.transform.localEulerAngles;

				//todo: add bone and attachment point stuff to the prop object
				break;
			case MCS.CONSTANTS.MESH_TYPE.UNKNOWN:
				//Unknown
				break;
			}

			return gameObject;
		}

		public Material CreateMorphMaterial(AssetSchematic schematic, Dictionary<string,Texture2D> textures)
		{
			string shaderName = "Standard";
			if (schematic.structure_and_physics.material_structure.shader != null && schematic.structure_and_physics.material_structure.shader != "") {
				shaderName = schematic.structure_and_physics.material_structure.shader;
			}
            Shader shader = Shader.Find(shaderName);
            if(shader == null)
            {
                UnityEngine.Debug.LogError("Unable to find shader: " + shaderName);
                throw new UnityException("Unable to locate shader");
            }

			var material = new Material (shader);

            /**
			List of textures that a material can have:
				albedo;
				metal;
				smoothness;
				emission;
				normal;
				detail_normal;
				transparency;
			*/

            //we also need to enable keywords if we use certain layers
            // For more info see: https://docs.unity3d.com/Manual/MaterialsAccessingViaScript.html


            if (textures.ContainsKey ("albedo")) {
				material.SetTexture (MaterialConstants.MainTexPropID, textures["albedo"]);
			}
			if (textures.ContainsKey ("metal")) {
				material.SetTexture (MaterialConstants.MetallicGlossMapPropID, textures["metal"]);
                material.EnableKeyword(MaterialConstants.METALLICGLOSSMAP_KEYWORD);
			}
			if (textures.ContainsKey ("height")) {
				material.SetTexture (MaterialConstants.ParallaxMapPropID, textures["height"]);
                material.EnableKeyword(MaterialConstants.PARALLAXMAP_KEYWORD);
			}
			if (textures.ContainsKey ("normal")) {
				material.SetTexture (MaterialConstants.BumpMapPropID, textures["normal"]);
                material.EnableKeyword(MaterialConstants.NORMALMAP_KEYWORD);
			}
			if (textures.ContainsKey ("detail_normal")) {
				material.SetTexture (MaterialConstants.DetailNormalMapPropID, textures["detail_normal"]);
                material.EnableKeyword(MaterialConstants.DETAIL_MULX2_KEYWORD);
			}
			if (textures.ContainsKey ("specular")) {
				material.SetTexture (MaterialConstants.SpecGlossMapPropID, textures["specular"]);
                material.EnableKeyword(MaterialConstants.SPECGLOSSMAP_KEYWORD);
			}
			if (textures.ContainsKey ("emission")) {
				material.SetTexture (MaterialConstants.EmissionMapPropID, textures["emission"]);
                material.EnableKeyword(MaterialConstants.EMISSION_KEYWORD);
			}

            /*
            //uncomment if you want to know which keywords were enabled
            string[] shaderKeywords = material.shaderKeywords;
            for(int i = 0;  i < shaderKeywords.Length; i++)
            {
                UnityEngine.Debug.Log("Keyword: " + shaderKeywords[i]);
            }
            */

			if (!String.IsNullOrEmpty(schematic.structure_and_physics.material_structure.albedo_tint)) {
                Color c = AssetSchematicUtility.ConvertColorStringToColor(schematic.structure_and_physics.material_structure.albedo_tint);
                material.SetColor (MaterialConstants.ColorPropID, c);
			}
            if (!String.IsNullOrEmpty(schematic.structure_and_physics.material_structure.emission_value))
            {
                Color c = AssetSchematicUtility.ConvertColorStringToColor(schematic.structure_and_physics.material_structure.emission_value);
                material.SetColor(MaterialConstants.EmissionColorPropID, c);
            }
            if (material.HasProperty(MaterialConstants.DetailNormalMapScalePropID) && schematic.structure_and_physics.material_structure.detail_normal_value>0f)
            {
                material.SetFloat(MaterialConstants.DetailNormalMapScalePropID, schematic.structure_and_physics.material_structure.detail_normal_value);
            }

            if (schematic.structure_and_physics.material_structure.shader_keywords != null)
            {
                for(int i=0;i< schematic.structure_and_physics.material_structure.shader_keywords.Length; i++)
                {
                    material.EnableKeyword(schematic.structure_and_physics.material_structure.shader_keywords[i]);
                }
            }		
	        if(schematic.structure_and_physics.material_structure.shader_tags != null)
            {
                for(int i=0;i< schematic.structure_and_physics.material_structure.shader_tags.Length; i++)
                {
                    string key = schematic.structure_and_physics.material_structure.shader_tags[i];
                    int pos = key.IndexOf('=');
                    if(pos < 0)
                    {
                        UnityEngine.Debug.Log("Invalid material shader_tag: " + schematic.origin_and_description.name + " => " + schematic.origin_and_description.mcs_id + " tag: " + key);
                        throw new Exception("Invalid shader tag");
                    }
                    material.SetOverrideTag(key.Substring(0, pos), key.Substring(pos + 1));
                }
            }		
	        if(schematic.structure_and_physics.material_structure.shader_properties != null)
            {
                for (int i=0;i< schematic.structure_and_physics.material_structure.shader_properties.Length; i++)
                {
                    string key = schematic.structure_and_physics.material_structure.shader_properties[i];
                    int typePos = key.IndexOf(':');
                    int equalPos = key.IndexOf('=');
                    string type = key.Substring(0, typePos);
                    string propertyKey = key.Substring(typePos + 1, equalPos - (typePos + 1));
                    string propertyValue = key.Substring(equalPos + 1);

                    switch (type)
                    {
                        case "int":
                            material.SetInt(propertyKey, Int32.Parse(propertyValue));
                            break;
                        case "float":
                            material.SetFloat(propertyKey, float.Parse(propertyValue));
                            break;
                        case "color":
                            Color c = AssetSchematicUtility.ConvertColorStringToColor(propertyValue);
                            material.SetColor(propertyKey, c);
                            break;
                        default:
                            UnityEngine.Debug.Log("Invalid material property: " + schematic.origin_and_description.name + " => " + schematic.origin_and_description.mcs_id + " key: " + key);
                            throw new Exception("Invalid shader property");
                    }
                }
            }
            if(schematic.structure_and_physics.material_structure.render_queue != -2)
            {
                material.renderQueue = schematic.structure_and_physics.material_structure.render_queue;
            }


			return material;
		}
	}
}
