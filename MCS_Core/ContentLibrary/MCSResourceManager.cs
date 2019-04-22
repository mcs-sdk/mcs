using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MCS;
using MCS.FOUNDATIONS;
using MCS.Item;
using M3D_DLL;

using MCS.Utility.Schematic;

namespace MCS.CONTENTLIBRARY
{		
	public class MCSResourceManager : MonoBehaviour
	{

		#region Object Pool
		private Dictionary<string, object> objectCache;

		/// <summary>
		/// Adds to object pool. Only usable at runtime
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="gameObject">Game object.</param>
		public void AddToObjectCache(string id, object objectToAdd)
		{
			if (!GetObjectCache().ContainsKey (id)) {
				GetObjectCache().Add (id, objectToAdd);
			} 
		}

		public Dictionary<string, object> GetObjectCache(){
			if (objectCache == null) {
				objectCache = new Dictionary<string, object> ();
			}
			return objectCache;
		}

		#endregion

		#region Streaming Assets

		//Async call to load gameobjects right now
		//todo: change this to load all types of objects
		public MCSObjectLoader LoadAnyType(AssetSchematic data)
		{
			MCSObjectLoader loader = new MCSObjectLoader ();
			StartCoroutine (DownloadAny (data,loader));
			return loader;
		}

		IEnumerator DownloadAny(AssetSchematic schematic,MCSObjectLoader loader){
			var datetime = DateTime.Now;

			if (GetObjectCache().ContainsKey (schematic.origin_and_description.mcs_id)) {
				//load it from the cache list
				yield return new WaitForSeconds (0);
				loader.progress = 100;
				float difference = (float) System.DateTime.Now.Subtract (datetime).TotalMilliseconds;
				loader.Complete (GetObjectCache()[schematic.origin_and_description.mcs_id],difference);

			} else if (schematic.stream_and_path.generated_path != null && schematic.stream_and_path.generated_path != "") {
				//load it from disk
				yield return new WaitForSeconds (0); 
				loader.progress = 100;
				float difference = (float) System.DateTime.Now.Subtract (datetime).TotalMilliseconds;

				switch(schematic.type_and_function.primary_function){
					case MCS.Utility.Schematic.Enumeration.PrimaryFunction.item:
						loader.Complete (LoadPrefabFromResources(schematic),difference);
						break;
					case MCS.Utility.Schematic.Enumeration.PrimaryFunction.material:
						loader.Complete (LoadMaterialFromResources(schematic),difference);
						break;
					default:
						loader.Complete (LoadPrefabFromResources(schematic),difference);
						break;
				}

			} else if (schematic.type_and_function.primary_function == MCS.Utility.Schematic.Enumeration.PrimaryFunction.material) {
				StartCoroutine (TextureStreamer (schematic,loader));
			} else if (schematic.stream_and_path.url != null && schematic.stream_and_path.url != "") {
				StartCoroutine (AssetBundleStreamer (schematic,loader));
			} else { 
				Debug.LogWarning ("Object: "+ schematic.origin_and_description.name + " with id: "+ schematic.origin_and_description.mcs_id + " was not found");
			}
		}

		IEnumerator TextureStreamer(AssetSchematic schematic, MCSObjectLoader loader){
			Dictionary<string,Texture2D> textures = new Dictionary<string, Texture2D> ();
			Dictionary<string,string> texturePaths = new Dictionary<string,string> ();

			if (schematic.structure_and_physics.material_structure.albedo != null && schematic.structure_and_physics.material_structure.albedo != "") {
				texturePaths.Add ("albedo",schematic.structure_and_physics.material_structure.albedo);
			}
			if (schematic.structure_and_physics.material_structure.metal != null && schematic.structure_and_physics.material_structure.metal != "") {
				texturePaths.Add ("metal",schematic.structure_and_physics.material_structure.metal);
			}
			if (schematic.structure_and_physics.material_structure.normal != null && schematic.structure_and_physics.material_structure.normal != "") {
				texturePaths.Add ("normal",schematic.structure_and_physics.material_structure.normal);
			}

			var datetime = DateTime.Now;

            //TODO: this is commented out to prevent errors with Wii U compiles
            /*
			while (!Caching.ready)
				yield return null;
            */

			foreach (KeyValuePair<string, string> entry in texturePaths) {
				using (WWW www = new WWW(entry.Value)) {

					while (!www.isDone) {
						loader.progress += www.progress * 100 / texturePaths.Count;
						yield return 0;
					}

					if (www.error != null)
						throw new Exception ("Download of " + entry.Key + "from: " + entry.Value + "  has failed. Error: " + www.error);
					Texture2D streamedTexture = www.texture;
					if (streamedTexture != null) {
						textures.Add (entry.Key, streamedTexture);
					}
				}
			}
			AssetCreator ac = new AssetCreator ();
			float difference = (float) System.DateTime.Now.Subtract (datetime).TotalMilliseconds;
			loader.Complete (ac.CreateMorphMaterial (schematic,textures),difference);
		}

		IEnumerator AssetBundleStreamer(AssetSchematic schematic, MCSObjectLoader loader){
			var datetime = DateTime.Now;
            
            //TODO: this is commented out to prevent errors with Wii U compiles
            /*
			while (!Caching.ready)
				yield return null;
            */

			using (WWW www = WWW.LoadFromCacheOrDownload (schematic.stream_and_path.url, Convert.ToInt32 (schematic.version_and_control.item_version))) {

				while (!www.isDone) {
					loader.progress = www.progress * 100;
					yield return 0;
				}

				if (www.error != null)
					throw new Exception ("Download of " + schematic.origin_and_description.mcs_id + "  has failed. Error: " + www.error);
				AssetBundle bundle = www.assetBundle;
				float difference = (float) System.DateTime.Now.Subtract (datetime).TotalMilliseconds;
				loader.Complete (bundle.LoadAsset (schematic.origin_and_description.name),difference);

				bundle.Unload (false);
			}
		}

		public MCSObjectLoader LoadFromUrl(AssetSchematic data){
			MCSObjectLoader loader = new MCSObjectLoader ();
			if (data.type_and_function.primary_function == MCS.Utility.Schematic.Enumeration.PrimaryFunction.material) {
				StartCoroutine (TextureStreamer (data, loader));
			} else {
				StartCoroutine (AssetBundleStreamer (data, loader));
			}

			return loader;
		}

		#endregion

		#region Resource Folder Loading

		//These are direct calls to load things from the resources folder;

		public GameObject LoadPrefabFromResources(AssetSchematic data)
		{
			GameObject gameO = null;
			string localpath = data.stream_and_path.generated_path.Replace (".prefab", "");
			localpath = localpath.Replace ("Assets/MCS/Resources/", "");
			gameO = Resources.Load (localpath, typeof(GameObject)) as GameObject;
			return gameO;
		}

		public Material LoadMaterialFromResources(AssetSchematic data)
		{
			Material gameO = null;
			string localpath = data.stream_and_path.generated_path.Replace (".mat", "");
			localpath = localpath.Replace ("Assets/MCS/Resources/", "");
			gameO = Resources.Load (localpath, typeof(Material)) as Material;
			return gameO;
		}

		#endregion

	}

}
