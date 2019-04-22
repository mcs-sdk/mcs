using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using MCS.CONSTANTS;
using MCS.UTILITIES;
using MCS.Utility.Schematic;
using MCS.Utility.Schematic.Enumeration;

namespace MCS.FOUNDATIONS
{
	//should only go at the ITEM level?
	public class CoreMetaData : MonoBehaviour
	{
		//essential functional data
		public string item_id;//gonna change the damn reference again. shame. shame.
		public string collection_id;
		public HierarchyRank rank;//unneeded if this is always at the item level
		public PrimaryFunction function;
		public ItemSchematic schematic;
		public ItemCompatibilities compatibilities;
		//version info
		public float collection_version;
		public float item_version;
		public float mcs_version;
		//changeable
		public string item_name;//we could use existing "name" or override, but unity assigns this for its referencing, so lets avoid it
		public string collection_name;


		//during the import process unity gives us these arrays per gameobject - they are our metatadata from fbx
		public void PopulateValuesFromImportArrays(string[] names, object[] values){
			//sweet. got the good stuff
			for (int i = 0; i < names.Length; i++) {

				//our placeholder values - make sure you dont use a previous parsed bad value!!!
				string name = names [i];
				string string_value;
				float float_value;

				switch(name){

				case "hierarchy_rank":
					string_value = values [i].ToString ();
					this.rank = MCS.Utilities.EnumHelper.ParseEnum<HierarchyRank> (string_value);
					break;
				case "primary_function":
					string_value = values [i].ToString ();
					this.function = MCS.Utilities.EnumHelper.ParseEnum<PrimaryFunction>(string_value);
					break;
				case "collection_version":
					float_value = float.Parse (values [i].ToString ());
					this.collection_version = float_value;
					break;
				case "item_version":
					float_value = float.Parse (values [i].ToString ());
					this.item_version = float_value;
					break;
				case "mcs_version":
					float_value = float.Parse (values [i].ToString ());
					this.mcs_version = float_value;
					break;
				case "item_id":
					string_value = values [i].ToString ();
					this.item_id = string_value;
					break;
				case "collection_id":
					string_value = values [i].ToString ();
					this.collection_id = string_value;
					break;
				case "item_name":
					string_value = values [i].ToString ();
					this.item_name = string_value;
					break;
				case "collection_name":
					string_value = values [i].ToString ();
					this.collection_name = string_value;
					break;
				case "geometries":
					string_value = values [i].ToString ();//json bundle
					this.schematic = ItemSchematic.CreateFromJSON (string_value);
					break;
				case "compatibilities":
					string_value = values [i].ToString ();//json bundle
					Debug.Log ("FOUND IT: " + string_value);
					this.compatibilities = ItemCompatibilities.CreateFromJSON (string_value);
					break;

				}
			}

		}
	}

	//items have optional parts, they can use or not use whatever parts as they wish... does this hold up to the test of time?
	[Serializable]
	public class ItemSchematic{
		public string[] lods =null;//geometries that we toggle on and off
		public string[] active = null;//always active
		public string[] cap = null;//cap for hair - we could just use always active
		//if we go down this route, we'll eventually have a ton of things, like particles, bullets, 
		//but... things will be explicit. sorda.
		public static ItemSchematic CreateFromJSON(string jsonString)
		{
			return JsonUtility.FromJson<ItemSchematic>(jsonString);
		}
	}
	[Serializable]
	public class ItemCompatibilities{
		public string[] figures = null;//geometries that we toggle on and off
		public static ItemCompatibilities CreateFromJSON(string jsonString)
		{
			return JsonUtility.FromJson<ItemCompatibilities>(jsonString);
		}
	}
	//public enum HierarchyRank{
	//	unkown,
	//	collection,
	//	item,
	//	geometry,
	//	skeleton
	//}
	//public enum PrimaryFunction{
	//	unkown,
	//	figure,
	//	hair,
	//	prop,
	//	soft_wearable,
	//	rigid_wearable,
	//	appendage
	//}


}

