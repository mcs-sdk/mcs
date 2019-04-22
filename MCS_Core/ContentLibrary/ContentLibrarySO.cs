using UnityEngine;
using System.Collections.Generic;
using MCS.FOUNDATIONS;
using System.Linq;
using MCS.Item;

using MCS.Utility.Schematic;
using MCS.Utility.Schematic.Enumeration;

namespace MCS.CONTENTLIBRARY
{
    public class ContentLibrarySO : ScriptableObject
	{
		public List<AssetSchematic> AssetSchematicList;

        public Dictionary<string,AssetDependency> importerDependencies = new Dictionary<string,AssetDependency>();
        public HashSet<string> importerSeenPaths = new HashSet<string>();

        public bool refreshOnComplete = false;

        public void ClearDependencies()
        {
            importerDependencies.Clear();
        }

		public void UpsertItem(AssetSchematic data)
		{
			if (AssetSchematicList == null) {
				AssetSchematicList = new List<AssetSchematic> ();
			}

			if (AssetSchematicList.Select (x => x.origin_and_description.mcs_id == data.origin_and_description.mcs_id) == null) {
				//does not exist
				AssetSchematicList.Add (data);

			} else {
				//exists already
				AssetSchematicList.RemoveAll(x => x.origin_and_description.mcs_id == data.origin_and_description.mcs_id);
				AssetSchematicList.Add (data);
			}

		}

		public void DeleteItem(AssetSchematic data)
		{
			AssetSchematicList.RemoveAll(x => x.origin_and_description.mcs_id == data.origin_and_description.mcs_id);
		}

		public void DeleteAll()
		{
			if (AssetSchematicList != null) {
				AssetSchematicList.Clear ();
			}

		}
	}
}
