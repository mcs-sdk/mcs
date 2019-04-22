using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace MCS.Utility.Schematic.Enumeration
{
	public enum HierarchyRank
	{
		unknown,
		collection,
		item,
		geometry,
		skeleton
	}
	public enum ArtistToolsFunction
	{
		unknown,
		item,
		model,
		geometry,
		skeleton,
		material,
		skin,
		morph,
		animation
	}
	public enum PrimaryFunction
	{
		unknown,
		item,
		material,
		morph,
		animation
	}
	public enum ItemFunction
	{
		unknown,
		figure,
		hair,
		prop,
		soft_wearable,
		rigid_wearable,
		appendage
	}
	public enum MaterialFunction
	{
		unknown,
		basic,
		decal,
		damage,
		morph
	}
	public enum MorphFunction
	{
		unknown,
		blendshape,
		jct
	}
	public enum AnimationFunction
	{
		unknown
	}
	public enum PhysicalSubstance
	{
		unknown,
		cotton,
		marble,
		wood,
		aluminum,
		steal,
		bone
	}
	public enum Gender
	{
		unknown,
		none,
		female,
		male
	}
	public enum Category
	{
		unknown,
		scifi,
		fantasy,
		modern,
		horror
	}

    public static class EnumHelpers
	{

		public static T GetAttributeOfType<T>(this Enum enumVal) where T : System.Attribute
		{
			var type = enumVal.GetType();
			var memInfo = type.GetMember(enumVal.ToString());
			var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
			return (attributes.Length > 0) ? (T)attributes[0] : null;
		}


		public static string GetDesc(this Enum enumValue)
		{
			var attribute = enumValue.GetAttributeOfType<DescriptionAttribute>();

			return attribute == null ? String.Empty : attribute.Description;
		}


		public static T GetEnum<T>(string description)
		{
			var type = typeof(T);
			if (!type.IsEnum) throw new InvalidOperationException();
			foreach (var field in type.GetFields())
			{
				var attribute = Attribute.GetCustomAttribute(field,
															 typeof(DescriptionAttribute)) as DescriptionAttribute;
				if (attribute != null)
				{
					if (attribute.Description == description)
						return (T)field.GetValue(null);
				}
				else
				{
					if (field.Name == description)
						return (T)field.GetValue(null);
				}
			}
			throw new ArgumentException("Not found.", "description");
		// or return default(T);
		}
	}

}


