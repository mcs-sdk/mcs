using System;
using UnityEngine;
using MCS.Utility.Schematic;
using MCS.Utility.Schematic.Enumeration;
namespace MCS.Item
{
	public class MCSItemModel : MonoBehaviour
	{
		//our actual schematic, helpers functions for convenience below
		public AssetSchematic schematic;


		//pull out things for easy access here
		public string item_id
		{
			get
			{
				return this.schematic.origin_and_description.mcs_id;
			}
			set
			{
				this.schematic.origin_and_description.mcs_id = value;
			}
		}
		public string item_name
		{
			get
			{
				return this.schematic.origin_and_description.name;
			}
			set
			{
				this.schematic.origin_and_description.name = value;
			}
		}
		//public float mass
		//{
		//	get
		//	{
		//		return this.schematic.structure_and_physics.mass;
		//	}
		//	set
		//	{
		//		this.schematic.structure_and_physics.mass = value;
		//	}
		//}
		public HierarchyRank hierarchy_rank
		{
			get
			{
				return this.schematic.type_and_function.hierarchy_rank;
			}
			set
			{
				this.schematic.type_and_function.hierarchy_rank = value;
			}
		}
		public PrimaryFunction primary_function
		{
			get
			{
				return this.schematic.type_and_function.primary_function;
			}
			set
			{
				this.schematic.type_and_function.primary_function = value;
			}
		}

		public ItemFunction item_function
		{
			get
			{
				return this.schematic.type_and_function.item_function;
			}
			set
			{
				this.schematic.type_and_function.item_function = value;
			}
		}

		// Used in Artist Tools
		public void InitializeItem()
		{
			schematic = new AssetSchematic();
			schematic.InitializeSchematic();
		}

	}

}

