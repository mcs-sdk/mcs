using System;
using MCS.Utility.Schematic.Enumeration;
using System.Diagnostics;
using UnityEngine;


namespace MCS.Utility.Schematic.Base
{
	[Serializable]
	public class TypeAndFunction : ISerializationCallbackReceiver
	{
		#region Type

		[SerializeField]
		[HideInInspector]
		private string primary_function_enum;
		[ATEditable]
		[ATListView(typeof(PrimaryFunction))]
		public PrimaryFunction primary_function;

		[SerializeField]
		[HideInInspector]
		private string hierarchy_rank_enum;
		[ATEditable]
		[ATListView(typeof(HierarchyRank))]
		public HierarchyRank hierarchy_rank;

		[SerializeField]
		[HideInInspector]
		private string artisttools_function_enum;
		public ArtistToolsFunction artisttools_function;

		#endregion
		#region Function
		[SerializeField]
		[HideInInspector]
		private string item_function_enum;
		[ATEditable]
		[ATListView(typeof(ItemFunction))]
		public ItemFunction item_function;

		[SerializeField]
		[HideInInspector]
		private string material_function_enum;
		[ATEditable]
		[ATListView(typeof(MaterialFunction))]
		public MaterialFunction material_function; 

		[SerializeField]
		[HideInInspector]
		private string morph_function_enum;
		[ATEditable]
		[ATListView(typeof(MorphFunction))]
		public MorphFunction morph_function;

		[SerializeField]
		[HideInInspector]
		private string animation_function_enum;
		[ATEditable]
		[ATListView(typeof(AnimationFunction))]
		public AnimationFunction animation_function;
		#endregion

		#region ISerializationCallbackReceiver implementation

		public void OnBeforeSerialize ()
		{
			primary_function_enum = primary_function.ToString ();
			hierarchy_rank_enum = hierarchy_rank.ToString ();
			item_function_enum = item_function.ToString ();
			material_function_enum = material_function.ToString ();
			morph_function_enum = morph_function.ToString ();
			animation_function_enum = animation_function.ToString ();
			artisttools_function_enum = artisttools_function.ToString();
		}

		public void OnAfterDeserialize ()
		{
			if (primary_function_enum != null) {
				primary_function = (PrimaryFunction)Enum.Parse (typeof(PrimaryFunction), primary_function_enum);
			}
			if (hierarchy_rank_enum != null) {
				hierarchy_rank = (HierarchyRank)Enum.Parse (typeof(HierarchyRank), hierarchy_rank_enum);
			}
			if (item_function_enum != null) {
				item_function = (ItemFunction)Enum.Parse (typeof(ItemFunction), item_function_enum);
			}
			if (material_function_enum != null) {
				material_function = (MaterialFunction)Enum.Parse (typeof(MaterialFunction), material_function_enum);
			}
			if (morph_function_enum != null) {
				morph_function = (MorphFunction)Enum.Parse (typeof(MorphFunction), morph_function_enum);
			}
			if (animation_function_enum != null) {
				animation_function = (AnimationFunction)Enum.Parse (typeof(AnimationFunction), animation_function_enum);
			}
			if (artisttools_function_enum != null)
			{
				artisttools_function = (ArtistToolsFunction)Enum.Parse(typeof(ArtistToolsFunction), artisttools_function_enum);
			}
		}

		#endregion


	}
}

