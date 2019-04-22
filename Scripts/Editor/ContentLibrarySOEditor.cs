using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;
using MCS.CONTENTLIBRARY;

[CustomEditor (typeof(ContentLibrarySO))]
public class ContentLibrarySOEditor : Editor
{

	protected bool[] showItemData = null;
	protected bool[] compatibilities = null;

	//changed the name of this so it doesn't get used at the moment. Will fix this later - Ben
//	public void OnInspectorGUI ()
//	{
//		EditorGUILayout.Space ();
//
//		ContentLibrarySO data;
//		data = (ContentLibrarySO)target;
//		if (data.AssetSchematicList != null) {
//
//			if (showItemData == null || showItemData.Length != data.AssetSchematicList.Count) {
//				showItemData = new bool[data.AssetSchematicList.Count];
//			}
//
//			if (compatibilities == null || compatibilities.Length != data.AssetSchematicList.Count) {
//				compatibilities = new bool[data.AssetSchematicList.Count];
//			}
//
//			EditorGUILayout.BeginVertical();
//
//			for (int i = 0; i < showItemData.Length; i++) {
//				showItemData [i] = EditorGUILayout.Foldout (showItemData [i], data.AssetSchematicList [i].item_name);
//				if (showItemData [i]) {
//					EditorGUI.indentLevel++;
//
//					EditorGUILayout.LabelField("item id: " + data.AssetSchematicList [i].item_id);
//					EditorGUILayout.LabelField("item version: " + data.AssetSchematicList [i].item_version);
//					EditorGUILayout.LabelField("collection id: " + data.AssetSchematicList [i].collection_id);
//					EditorGUILayout.LabelField("collection name: " + data.AssetSchematicList [i].collection_name);
//					EditorGUILayout.LabelField("collection version: " + data.AssetSchematicList [i].collection_version);
//					EditorGUILayout.LabelField("rank: " + data.AssetSchematicList [i].hierarchy_rank.ToString());
//					EditorGUILayout.LabelField("function: " + data.AssetSchematicList [i].function.ToString());
//					EditorGUILayout.LabelField("mcs version: " + data.AssetSchematicList [i].mcs_version);
//
//					if (data.AssetSchematicList [i].mon_file_path != "") {
//						EditorGUILayout.LabelField("mon path: " + data.AssetSchematicList [i].mon_file_path);
//					}
//					if(data.AssetSchematicList [i].donor_file_path != "" && data.AssetSchematicList [i].donor_file_path != null){
//						EditorGUILayout.LabelField("donor path: " + data.AssetSchematicList [i].donor_file_path);
//					}
//					if (data.AssetSchematicList [i].prefab_path != "") {
//						EditorGUILayout.LabelField("prefab path: " + data.AssetSchematicList [i].prefab_path);
//					}
//
//					if (data.AssetSchematicList [i].compatibilities != null && data.AssetSchematicList [i].compatibilities.Length > 0) {
//
//						compatibilities[i] = EditorGUILayout.Foldout (compatibilities[i], "compatibilities:");
//						if (compatibilities [i]) {
//							EditorGUI.indentLevel++;
//							for (int j = 0; j < data.AssetSchematicList [i].compatibilities.Length; j++) {
//								EditorGUILayout.LabelField(data.AssetSchematicList [i].compatibilities[j]);
//							}
//							EditorGUI.indentLevel--;
//						}
//
//					}
//
//
//					EditorGUILayout.Space ();
//					EditorGUI.indentLevel--;
//				}
//			}
//
//			EditorGUILayout.EndVertical();
//		}
//
//
//
//
//
//	}
}

