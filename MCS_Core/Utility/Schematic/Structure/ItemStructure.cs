using System;
using UnityEngine;
namespace MCS.Utility.Schematic.Structure
{
	//items have optional parts, they can use or not use whatever parts as they wish... does this hold up to the test of time?
	[Serializable]
	public class ItemStructure
	{
		public string[] lods = null;//geometries that we toggle on and off
		public string[] persistent = null;//always active
		public string[] cap = null;//cap for hair - we could just use always active
								   //if we go down this route, we'll eventually have a ton of things, like particles, bullets, 
								   //but... things will be explicit. sorda.
		public string[] assigned_materials = null; // list of materials associated with the item.
		public string[] alpha_masks = null;  // List of Alpha Masks required by each item. They should be in the order defined by the array below. 
		public string[] alpha_masks_key = null; // List of Alpha Mask keys - HEAD, BODY, EYES. 

        public string overlay = null; //a texture to apply to the figure's skin
        public string overlay_color = null; //a tint color for the overlay

        public string[] morph_resouces = null; 
		public static ItemStructure CreateFromJSON(string jsonString)
		{
			return JsonUtility.FromJson<ItemStructure>(jsonString);
		}
	}
}
