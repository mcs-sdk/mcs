using System;
using MCS.Utility.Schematic.Structure;
using MCS.Utility.Schematic.Enumeration;
using UnityEngine;


namespace MCS.Utility.Schematic.Base
{
	[Serializable]
	public class StructureAndPhysics : ISerializationCallbackReceiver
	{

		public StructureAndPhysics(){
			item_structure = new ItemStructure ();
			material_structure = new MaterialStructure ();
			morph_structure = new MorphStructure ();
		}

		#region Structure
		public ItemStructure item_structure;
		public MaterialStructure material_structure;
		public MorphStructure morph_structure;
		#endregion

		#region Physics
		//public float mass;
		//public float volume;
		//[ATEditable]
		//[ATInputSlider]
		//public float density;

		//[SerializeField]
		//[HideInInspector]
		//private string substance_enum;
		//[ATListView(typeof(PhysicalSubstance))]
		//[ATEditable]
		//public PhysicalSubstance substance;
		#endregion

		#region ISerializationCallbackReceiver implementation

		public void OnBeforeSerialize ()
		{
			//substance_enum = substance.ToString ();
		}

		public void OnAfterDeserialize ()
		{
			//if (substance_enum != null) {
			//	substance = (PhysicalSubstance)Enum.Parse (typeof(PhysicalSubstance), substance_enum);
			//}
		}

		#endregion
	}
}

