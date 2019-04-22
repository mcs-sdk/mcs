using UnityEngine;
using System.Collections;
using System;
using System.ComponentModel;
using System.Reflection;

namespace MCS.CONSTANTS
{	
	public enum POLARIZED_OPTION
	{
		NONE,
		POSITIVE,
		NEGATIVE
	}

    /// <summary>
    /// Determines the type of Importer available for the Import_DLL
    /// </summary>
	public enum IMPORTER_TYPE
	{
		LEGACY,
		ASSETSCHEMATIC,
		KEYVALUE
	}
    /// <summary>
    /// Enums for the different types of Blendshapes or Morphs available
    /// for the MCS Figures. The Enums roughly correspond to the prefix 
    /// of the localName of the Morph.
    /// </summary>
	public enum BLENDSHAPE_TYPE
	{
		[Description("Full Body Morph")]
		FBM, // BODY GROUP
		[Description("Full Head Morph")]
		FHM, // HEAD GROUP
		[Description("Partial Body Morph")]
		PBM, // BODY GROUP
		[Description("Partial Head Morph")]
		PHM, // HEAD GROUP
		[Description("Morph Control Morph")]
		MCM, // HIDE DO NOT DISPLAY
		[Description("Joint Center Morph")]
		JCM, // HIDE DO NOT DISPLAY
		[Description("Vocal Shape Morph")]
		VSM, // PHONEME GROUP
		[Description("Proportion Morph")]
		SCL, // PROPORTION GROUP
		[Description("Morph Group")]
		CTRL, // IGNORE
		[Description("Complete Body Morph")]
		CBM, // custom morphs made by us
		[Description("Miscellaneous Morphs")]
		MSC // custom morphs made by us
	}

    /// <summary>
    /// Enum to distinguish the different types of meshes we deal with
    /// Body, Hair, Cloth or Prop.
    /// </summary>
	public enum MESH_TYPE
	{
		[Description("Body Mesh")]
		BODY,
		[Description("Hair Mesh")]
		HAIR,
		[Description("Clothing Mesh")]
		CLOTH,
		[Description("Prop Mesh")]
		PROP,
		[Description("UNNKOWN")]
		UNKNOWN,
	}

    /// <summary>
    /// Enum to distinguish between visible and hidden meshes.
    /// </summary>
	public enum VISIBILITY { Hidden, Visible }

    /// <summary>
    /// These are the material slots (or uv maps) that we care about on a figure. We use these slot identifiers specifically for alpha injection in 1.5 so that we can produce 2 separate alpha injection maps (one for head and one for body)
    /// </summary>
    public enum MATERIAL_SLOT
    {
        [Description("This is the uv map for the head of a figure")]
        HEAD,
        [Description("This is the uv map for the body of a figure")]
        BODY,
        [Description("This is the uv map for the eyes and lash of a figure")]
        EYEANDLASH,
        [Description("This is a uv map for an unknown region, we can ignore it")]
        UNKNOWN,
    }

    /// <summary>
    /// Enum used by the CharacterManager to determine how frequently
    /// the bounding box of a CostumeItem is recalculated.
    /// </summary>
    public enum COSTUME_BOUNDS_UPDATE_FREQUENCY {
        [Description("Automatically recalculate bounds of a mesh when attached to the figure")]
        ON_ATTACH,
        [Description("Automatically recalcualte bounds of a mesh whenever the figure is morphed")]
        ON_MORPH,
        [Description("Never recalculate mesh boundaries")]
        NEVER
    }

}



public static class EnumHelper
{

    /*
	/// <summary>
	/// Retrieve the description on the enum, e.g.
	/// [Description("Bright Pink")]
	/// BrightPink = 2,
	/// Then when you pass in the enum, it will retrieve the description
	/// </summary>
	/// <param name="en">The Enumeration</param>
	/// <returns>A string representing the friendly name</returns>
	public static string GetDescription (Enum en)
	{
		Type type = en.GetType ();
		MemberInfo[] memInfo = type.GetMember (en.ToString ());
			
		if (memInfo != null && memInfo.Length > 0) {
			object[] attrs = memInfo [0].GetCustomAttributes (typeof(DescriptionAttribute), false);
			if (attrs != null && attrs.Length > 0)
				return ((DescriptionAttribute)attrs [0]).Description;
		}
		return en.ToString ();
	}
    */

	//we cast string to enum of type by the grace of stack overflow : http://stackoverflow.com/questions/13970257/casting-string-to-enum
	public static T ParseEnum<T>(string value)
	{
		//we should always have "unkown" be the first value for an enum so we can add a default here, and use a simplified default chekck
		//rather than a potential red error, weird null, undefined, etc
		return (T)Enum.Parse(typeof(T), value, ignoreCase: true);
	}

}
