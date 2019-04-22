using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MCS.FOUNDATIONS;
using System.Linq;
using MCS;
using MCS.Item;
using M3D_DLL;

using MCS.Utility.Schematic;
using System.IO;

namespace MCS.CONTENTLIBRARY
{
	public class ContentLibrary : MonoBehaviour
	{
		private ContentLibrarySO so;
		
		private MCSResourceManager ResourceManager(){
			return GetComponent <MCSResourceManager>();
		}

		public ContentLibrarySO GetContentLibrary(){
			if (so == null) {
				so = (ContentLibrarySO)Resources.Load ("ContentLibrarySO");
			}
			return so;
		}

		//pass in an id and get a coreMeshData in return
		public AssetSchematic GetItemData(string id){
			AssetSchematic result = GetContentLibrary ().AssetSchematicList.Where(x => x.origin_and_description.mcs_id == id).SingleOrDefault ();
			return result;
		}

		/// <summary>
		/// Loads the game object. It looks first in the object cache, then in the resources folder on disk, 
		/// then in the unity disk cache, and finally it will stream it if the url is present. 
		/// </summary>
		/// <returns>The GameObject.</returns>
		/// <param name="id">Identifier.</param>
		public MCSObjectLoader LoadMCSGameObject(string id){
			return ResourceManager ().LoadAnyType (GetItemData (id));
		}

		public MCSObjectLoader LoadGameObjectFromUrl(string id){
			return ResourceManager ().LoadFromUrl (GetItemData (id));
		}

		public GameObject LoadGameObjectFromResources(string id){
			return ResourceManager ().LoadPrefabFromResources (GetItemData (id));
		}

		public GameObject LoadGameObjectFromObjectCache(string id){
			return (GameObject )ResourceManager ().GetObjectCache () [id];
		}

		public AssetSchematic[] GetMyCompatibilities(string Id){
			List<AssetSchematic> schematics = new List<AssetSchematic> ();
			foreach (string id in GetItemData(Id).version_and_control.compatibilities) {
				schematics.Add (GetItemData (id));
			}
			return schematics.ToArray ();
		}

		//Not really sure what to name this... :( -Ben
		public AssetSchematic[] GetCompatibilitiesWithMeInIt(string Id){
			return GetContentLibrary ().AssetSchematicList.Where(x => x.version_and_control.compatibilities.Contains (Id)).ToArray();
		}

	}
}
