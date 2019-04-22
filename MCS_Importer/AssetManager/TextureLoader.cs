using UnityEngine;
using System.IO;
using MCS.Utility.Schematic;
using System.Collections.Generic;
using UnityEditor;
using MCS_Utilities;

public static class TextureLoader
{

    public static bool GetTextures(AssetSchematic schematic, string basePath, out Dictionary<string,Texture2D> textures )
    {
        bool success = true; 
        textures = new Dictionary<string, Texture2D>();

        // ALBEDO
        if(!string.IsNullOrEmpty(schematic.structure_and_physics.material_structure.albedo))
        {
            string assetPath = string.Empty;
            if (schematic.structure_and_physics.material_structure.albedo.Contains(@"./"))
                assetPath = Paths.ConvertRelativeToAbsolute(basePath, schematic.structure_and_physics.material_structure.albedo);
            else
                assetPath = Path.Combine(basePath, schematic.structure_and_physics.material_structure.albedo);

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if(albedo == null)
            {
                success = false;
                return success;
            }
            else
            {
                textures.Add("albedo", albedo);
            }

        }

        // metal
        if (!string.IsNullOrEmpty(schematic.structure_and_physics.material_structure.metal))
        {
            string assetPath = string.Empty;
            if (schematic.structure_and_physics.material_structure.metal.Contains(@"./"))
                assetPath = Paths.ConvertRelativeToAbsolute(basePath, schematic.structure_and_physics.material_structure.metal);
            else
                assetPath = Path.Combine(basePath, schematic.structure_and_physics.material_structure.metal);

            Texture2D metal = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (metal == null)
            {
                success = false;
                return success;
            }
            else
            {
                textures.Add("metal", metal);
            }

        }

        // height
        if (!string.IsNullOrEmpty(schematic.structure_and_physics.material_structure.height))
        {
            string assetPath = string.Empty;
            if (schematic.structure_and_physics.material_structure.height.Contains(@"./"))
                assetPath = Paths.ConvertRelativeToAbsolute(basePath, schematic.structure_and_physics.material_structure.height);
            else
                assetPath = Path.Combine(basePath, schematic.structure_and_physics.material_structure.height);

            Texture2D height = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (height == null)
            {
                //optional, do not error here
                //success = false;
                //return success;
            }
            else
            {
                textures.Add("height", height);
            }

        }

        // emission
        if (!string.IsNullOrEmpty(schematic.structure_and_physics.material_structure.emission))
        {
            string assetPath = string.Empty;
            if (schematic.structure_and_physics.material_structure.emission.Contains(@"./"))
                assetPath = Paths.ConvertRelativeToAbsolute(basePath, schematic.structure_and_physics.material_structure.emission);
            else
                assetPath = Path.Combine(basePath, schematic.structure_and_physics.material_structure.emission);

            Texture2D emission = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (emission == null)
            {
                //optional, do not error here
                //success = false;
                //return success;
            }
            else
            {
                textures.Add("emission", emission);
            }

        }

        // specular
        if (!string.IsNullOrEmpty(schematic.structure_and_physics.material_structure.specular))
        {
            string assetPath = string.Empty;
            if (schematic.structure_and_physics.material_structure.specular.Contains(@"./"))
                assetPath = Paths.ConvertRelativeToAbsolute(basePath, schematic.structure_and_physics.material_structure.specular);
            else
                assetPath = Path.Combine(basePath, schematic.structure_and_physics.material_structure.specular);

            Texture2D specular = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (specular == null)
            {
                success = false;
                return success;
            }
            else
            {
                textures.Add("specular", specular);
            }

        }

        // normal
        if (!string.IsNullOrEmpty(schematic.structure_and_physics.material_structure.normal))
        {
            string assetPath = string.Empty;
            if (schematic.structure_and_physics.material_structure.normal.Contains(@"./"))
                assetPath = Paths.ConvertRelativeToAbsolute(basePath, schematic.structure_and_physics.material_structure.normal);
            else
                assetPath = Path.Combine(basePath, schematic.structure_and_physics.material_structure.normal);

            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (normal == null)
            {
                success = false;
                return success;
            }
            else
            {
                TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(assetPath);
                if (importer.textureType != TextureImporterType.Bump)
                {
                    importer.textureType = TextureImporterType.Bump;
                    AssetDatabase.ImportAsset(assetPath);
                    AssetDatabase.Refresh();
                }
                textures.Add("normal", normal);
            }

        }

        // detail_normal
        if (!string.IsNullOrEmpty(schematic.structure_and_physics.material_structure.detail_normal))
        {
            string assetPath = string.Empty;
            if (schematic.structure_and_physics.material_structure.detail_normal.Contains(@"./"))
                assetPath = Paths.ConvertRelativeToAbsolute(basePath, schematic.structure_and_physics.material_structure.detail_normal);
            else
                assetPath = Path.Combine(basePath, schematic.structure_and_physics.material_structure.detail_normal);

            Texture2D detail_normal = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (detail_normal == null)
            {
                success = false;
                return success;
            }
            else
            {
                TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(assetPath);
                if (importer.textureType != TextureImporterType.Bump)
                {
                    importer.textureType = TextureImporterType.Bump;
                    AssetDatabase.ImportAsset(schematic.structure_and_physics.material_structure.detail_normal);
                    AssetDatabase.Refresh();
                }
                textures.Add("detail_normal", detail_normal);
            }

        }

        // transparency
        if (!string.IsNullOrEmpty(schematic.structure_and_physics.material_structure.transparency))
        {
            string assetPath = string.Empty;
            if (schematic.structure_and_physics.material_structure.transparency.Contains(@"./"))
                assetPath = Paths.ConvertRelativeToAbsolute(basePath, schematic.structure_and_physics.material_structure.transparency);
            else
                assetPath = Path.Combine(basePath, schematic.structure_and_physics.material_structure.transparency);

            Texture2D transparency = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (transparency == null)
            {
                success = false;
                return success;
            }
            else
            {
                textures.Add("transparency", transparency);
            }

        }
        return success;

    }


	public static Dictionary<string,Texture2D> GetTextures(AssetSchematic schematic, string basePath){

		Dictionary<string,Texture2D> textures = new Dictionary<string,Texture2D> ();

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


		Texture2D albedo = (Texture2D)AssetDatabase.LoadAssetAtPath (Paths.ConvertRelativeToAbsolute(basePath,schematic.structure_and_physics.material_structure.albedo), typeof(Texture2D));
		if (albedo != null) {
			textures.Add ("albedo",albedo);
		}
		Texture2D metal = (Texture2D)AssetDatabase.LoadAssetAtPath (Paths.ConvertRelativeToAbsolute(basePath, schematic.structure_and_physics.material_structure.metal), typeof(Texture2D));
		if (metal != null) {
			textures.Add ("metal",metal);
		}
		Texture2D height = (Texture2D)AssetDatabase.LoadAssetAtPath (Paths.ConvertRelativeToAbsolute(basePath, schematic.structure_and_physics.material_structure.height), typeof(Texture2D));
		if (height != null) {
			textures.Add ("height",height);
		}
//		Texture2D smoothness = (Texture2D)AssetDatabase.LoadAssetAtPath (schematic.structure_and_physics.material_structure.sm, typeof(Texture2D));
//		if (smoothness != null) {
//			textures.Add ("smoothness",smoothness);
//		}
		Texture2D emission = (Texture2D)AssetDatabase.LoadAssetAtPath (Paths.ConvertRelativeToAbsolute(basePath, schematic.structure_and_physics.material_structure.emission), typeof(Texture2D));
		if (emission != null) {
			textures.Add ("emission",emission);
		}
		Texture2D specular = (Texture2D)AssetDatabase.LoadAssetAtPath (Paths.ConvertRelativeToAbsolute(basePath, schematic.structure_and_physics.material_structure.specular), typeof(Texture2D));
		if (specular != null) {
			textures.Add ("specular",specular);
		}
		Texture2D normal = (Texture2D)AssetDatabase.LoadAssetAtPath (Paths.ConvertRelativeToAbsolute(basePath, schematic.structure_and_physics.material_structure.normal), typeof(Texture2D));
		if (normal != null) {
			TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath (Paths.ConvertRelativeToAbsolute(basePath, schematic.structure_and_physics.material_structure.normal));
			if (importer.textureType != TextureImporterType.Bump) {
				importer.textureType = TextureImporterType.Bump;
				AssetDatabase.ImportAsset (schematic.structure_and_physics.material_structure.normal);
				AssetDatabase.Refresh ();
			}
			textures.Add ("normal",normal);

		}
		Texture2D detail_normal = (Texture2D)AssetDatabase.LoadAssetAtPath (Paths.ConvertRelativeToAbsolute(basePath, schematic.structure_and_physics.material_structure.detail_normal), typeof(Texture2D));
		if (detail_normal != null) {
			TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath (Paths.ConvertRelativeToAbsolute(basePath, schematic.structure_and_physics.material_structure.detail_normal));
			if (importer.textureType != TextureImporterType.Bump) {
				importer.textureType = TextureImporterType.Bump;
				AssetDatabase.ImportAsset (schematic.structure_and_physics.material_structure.detail_normal);
				AssetDatabase.Refresh ();
			}
			textures.Add ("detail_normal",detail_normal);

		}
		Texture2D transparency = (Texture2D)AssetDatabase.LoadAssetAtPath (Paths.ConvertRelativeToAbsolute(basePath, schematic.structure_and_physics.material_structure.transparency), typeof(Texture2D));
		if (transparency != null) {
			textures.Add ("transparency",transparency);
		}

		return textures;
	}
}

