using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MCS.CONSTANTS;
using MCS.UTILITIES;
using System;
using MCS.Utility.Schematic;
using MCS.Utility.Schematic.Structure;
using MCS.Utility.Schematic.Enumeration;

/// <summary>
/// MonoBehaviour component class CoreMeshMetaData. Holds all data for a single CoreMesh.
/// As a component class the CoreMeshMetaData is added automatically to the same node
/// as the <see cref="MCS.COSTUMING.CostumeItem"/> component is added.
/// 
/// Public class to provide read access to data. End users should not set this data themselves.
/// Values are populated through the <see cref="AssetSchematic"/> or on import.
/// </summary>
public class CoreMeshMetaData : MonoBehaviour 
{

	//old data
	public string vendorId;
	public string versionId;
	public MESH_TYPE meshType;
	public string geometryId;
	public string ID;
	public string compatabilityBase;
	public string declarativeUse;
	public List<KeyValueFloat> controllerScales;


	public string item_id;
	public string collection_id;
	public HierarchyRank rank;//unneeded if this is always at the item level
	public ItemFunction function;
	public ItemSchematic schematic;
	public ItemCompatibilities compatibilities;
	//version info
	public float collection_version;
	public float item_version;
	public float mcs_version;
	//changeable
	public string item_name;//we could use existing "name" or override, but unity assigns this for its referencing, so lets avoid it
	public string collection_name;

	[ContextMenu ("Repopulate Values")]
	public void  RepopulateValues()
	{
		MCS.FOUNDATIONS.CoreMesh core_mesh = GetComponent<MCS.FOUNDATIONS.CoreMesh>();
		core_mesh.dazName = geometryId;
		core_mesh.ID = ID;
		core_mesh.meshType = meshType;
	}

	public void PopulateValuesFromAssetSchematic(AssetSchematic schematic)
	{
		//Debug.Log("Populating values.");
		//Origin & Description
		this.item_id = schematic.origin_and_description.mcs_id;
		this.item_name = schematic.origin_and_description.name;
		this.collection_id = schematic.origin_and_description.collection_id;
		this.collection_name = schematic.origin_and_description.collection_name;
		this.vendorId = schematic.origin_and_description.vendor_id;


		//Version
		this.collection_version = schematic.version_and_control.collection_version;
		this.item_version = schematic.version_and_control.item_version;
		this.mcs_version = schematic.version_and_control.mcs_version;

		//Type & function
		this.function = schematic.type_and_function.item_function;
		this.rank = schematic.type_and_function.hierarchy_rank;

		// TODO: Should be using AssetSchematic.ItemStructure instead.  
		this.schematic = new ItemSchematic();
		this.schematic.lods = schematic.structure_and_physics.item_structure.lods;
		this.schematic.persistent = schematic.structure_and_physics.item_structure.persistent;

		if (this.function != ItemFunction.figure)
		{
			this.compatibilities = new ItemCompatibilities();
			this.compatibilities.figures = schematic.version_and_control.compatibilities;
		}
		DownConvertDarwinSpecToProtoSpec();
	}
	//during the import process unity gives us these arrays per gameobject - they are our metatadata from fbx
	public void PopulateValuesFromImportArrays(string[] names, object[] values)
	{

		for (int i = 0; i < names.Length; i++)
		{

			//our placeholder values - make sure you dont use a previous parsed bad value!!!
			string name = names[i];
			string string_value;
			float float_value;

			switch (name)
			{

				case "hierarchy_rank":
					string_value = values[i].ToString();
					this.rank = EnumHelper.ParseEnum<HierarchyRank>(string_value);
					break;
//				case "primary_function":
//					string_value = values[i].ToString();
//					this.function = EnumHelper.ParseEnum<PrimaryFunction>(string_value);
//					break;
				case "item_function":
					string_value = values[i].ToString();
					this.function = EnumHelper.ParseEnum<ItemFunction>(string_value);
					break;
				case "collection_version":
					float_value = float.Parse(values[i].ToString());
					this.collection_version = float_value;
					break;
				case "item_version":
					float_value = float.Parse(values[i].ToString());
					this.item_version = float_value;
					break;
				case "mcs_version":
					float_value = float.Parse(values[i].ToString());
					this.mcs_version = float_value;
					break;
				case "item_id":
					string_value = values[i].ToString();
					this.item_id = string_value;
				break;
				case "id":
					string_value = values[i].ToString();
					this.item_id = string_value;
					break;
				case "collection_id":
					string_value = values[i].ToString();
					this.collection_id = string_value;
					break;
				case "item_name":
					string_value = values[i].ToString();
					this.item_name = string_value;
				break;
				case "name":
					string_value = values[i].ToString();
					this.item_name = string_value;
					break;
				case "collection_name":
					string_value = values[i].ToString();
					this.collection_name = string_value;
					break;
				case "geometries":
					string_value = values[i].ToString();//json bundle
					this.schematic = ItemSchematic.CreateFromJSON(string_value);
					break;
				case "compatibilities":
					string_value = values[i].ToString();//json bundle
					if(this.function != ItemFunction.figure){
						this.compatibilities = ItemCompatibilities.CreateFromJSON(string_value);
					}
					break;

			}
		}



		DownConvertDarwinSpecToProtoSpec();
	}

	//this function is to only be used AFTER the new importer added NEW metadata and needs to populate old values
	//if you use the old importer and had old data, this will ruin the world. we will all cry, and everything will be aweful. I think I spelled awful incorrectly.
	private void DownConvertDarwinSpecToProtoSpec()
	{		
		this.vendorId = "DAZ3D";
		this.versionId = this.mcs_version.ToString();
		this.meshType = ItemFunctionToMeshType(this.function);
		this.geometryId = this.item_name;
		this.ID = this.item_id;
		if (this.function != ItemFunction.figure) {
			if (this.compatibilities.figures.Length != 0)
			{
				Debug.Log("this.compatibilities.figures is not null");
				this.compatabilityBase = this.compatibilities.figures[0];
			}
		} else {
			this.compatabilityBase = this.item_name;
		}
		//declarativeUse;
		//controllerScales;

		MCS.FOUNDATIONS.CoreMesh core_mesh = GetComponent<MCS.FOUNDATIONS.CoreMesh>();
		if (core_mesh != null) {
			//Debug.Log("Core Mesh != null ");
			core_mesh.dazName = geometryId;
			core_mesh.ID = ID;
			core_mesh.meshType = meshType;
		}
	}

	private MESH_TYPE ItemFunctionToMeshType(ItemFunction function)
	{
		switch (function)
		{
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

	//items have optional parts, they can use or not use whatever parts as they wish... does this hold up to the test of time?
	[Serializable]
	public class ItemSchematic
	{
		public string[] lods = null;//geometries that we toggle on and off
		public string[] persistent = null;//always active

		public static ItemSchematic CreateFromJSON(string jsonString)
		{
			return JsonUtility.FromJson<ItemSchematic>(jsonString);
		}
	}
	[Serializable]
	public class ItemCompatibilities
	{
		public string[] figures = null;//geometries that we toggle on and off
		public static ItemCompatibilities CreateFromJSON(string jsonString)
		{
			return JsonUtility.FromJson<ItemCompatibilities>(jsonString);
		}
	}

	//public enum HierarchyRank
	//{
	//	unkown,
	//	collection,
	//	item,
	//	geometry,
	//	skeleton
	//}
	//public enum PrimaryFunction
	//{
	//	unkown,
	//	item,
	//	material,
	//	morph,
	//	animation
	//}
	//public enum ItemFunction
	//{
	//	unkown,
	//	figure,
	//	hair,
	//	prop,
	//	soft_wearable,
	//	rigid_wearable,
	//	appendage
	//}
	//public enum MaterialFunction
	//{
	//	unkown,
	//	basic,
	//	decal,
	//	damage,
	//	morph
	//}
}
