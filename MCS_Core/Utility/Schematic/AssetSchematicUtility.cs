using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using MCS.Utility.Schematic.Base;
using MCS.Utility.Schematic.Enumeration;
using MCS.Utility.Schematic.Structure;

namespace MCS.Utility.Schematic
{
	public static class AssetSchematicUtility
	{

		public static List<string> GetValidMetadataKeys()
		{
			List<System.Type> classList = new List<System.Type>
			{
				typeof(MCS.Utility.Schematic.AssetSchematic),
				typeof(MCS.Utility.Schematic.Base.OriginAndDescription),
				typeof(MCS.Utility.Schematic.Base.StreamAndPath),
				typeof(MCS.Utility.Schematic.Base.StructureAndPhysics),
				typeof(MCS.Utility.Schematic.Base.TypeAndFunction),
				typeof(MCS.Utility.Schematic.Base.VersionAndControl),
				typeof(MCS.Utility.Schematic.Structure.ItemStructure),
				typeof(MCS.Utility.Schematic.Structure.MaterialStructure),
				typeof(MCS.Utility.Schematic.Structure.MorphStructure)
			};
			List<string> MetaDataList = new List<string>();
			BindingFlags bindingFlags = BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance |
				BindingFlags.Static;

			foreach (System.Type a in classList)
				foreach (FieldInfo field in a.GetFields(bindingFlags))
					MetaDataList.Add(field.Name);

			return MetaDataList;
		}

        public static Color ConvertColorStringToColor(string raw)
        {
            if (String.IsNullOrEmpty(raw))
            {
                return Color.clear;
            }

            string[] tints = raw.Split (',');
            float inR = float.Parse(tints[0]);
            float inG = float.Parse(tints[1]);
            float inB = float.Parse(tints[2]);
            float inA = (tints.Length > 3 ? float.Parse(tints[3]) : 255f); //default to fully visible

            float r, g, b, a;

            if(inR <= 1f && inG <= 1f && inB <= 1f)
            {
                //1,0.8,0.5,0.1
                r = inR;
                g = inG;
                b = inB;
                a = (inA >= 255f ? 1f : inA); //clamp it if it's set to 255
            }
            else
            {
                //255,200,120,10
                r = (inR / 255);
                g = (inG / 255);
                b = (inB / 255);
                a = (inA / 255);
            }


            Color c = new Color (r, g, b, a);
            //UnityEngine.Debug.Log("Color: " + c);
            return c;
        }
	}
}

