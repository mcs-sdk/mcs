using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace M3DIMPORT
{
	/// <summary>
	/// MonoBehaviour inheriting class for importing JCTs in the Editor.
	/// </summary>
	/// <remarks>Is only static functions - does it need to be a monobehaviour?</remarks>
	public class JCTImportUtility : MonoBehaviour
	{



		/// <summary>
		/// Creates the JCT transition on import. Passing in the Exact geometry GameObject, one at a time.
		/// </summary>
		/// <param name="destination">Destination GameObject.</param>
		/// <param name="go">GameObject with geometry.</param>
		/// <param name="geometryId">Geometry identifier.</param>
		/// <param name="asset_path">Asset path.</param>
		public static void CreateJCTTransitionOnImport(GameObject destination,  GameObject go, string geometryId, string asset_path)
		{
			// Morpher morpher  = obj.transform.parent.gameObject.GetComponentInChildren (typeof (Morpher)) as Morpher;
			SkinnedMeshRenderer skinned_mesh_renderer = go.GetComponent<SkinnedMeshRenderer> ();

			// we'll skip calling handleskinnedmeshrenderer and just do what it does here but streamline it for our usage
			Transform[] bones = skinned_mesh_renderer.bones;
			// the importer passes in the path
			string assetPath = asset_path;

			string baseName = geometryId;


			//first let's look for the "new" way, which is a JCTs.json file that sits next to the .fbx file, if we can't find anything we'll fall back
			string jsonPath = assetPath.Substring (0, assetPath.LastIndexOf ("/")) + "/JCTs.json";
			if (File.Exists (jsonPath)) {
				//UnityEngine.Debug.Log ("I found a JCTs.json file: " + jsonPath + " , unpacking and using");
				JCTsPacked packed = JsonUtility.FromJson<JCTsPacked> (File.ReadAllText(jsonPath));

				Regex basePattern = new Regex (baseName+@"\.base$");
				Regex baseNamePattern = new Regex (baseName);

				//we need to get the base first...

				string[] baseNames = null;
				Vector3[] baseNodes = null;
				Vector3[] baseOffsets = null;

				bool foundBase = false;

                //convert XYZFemale_LOD0.Aged_Posture to Aged_Posture, this is for dual compatibility
                for(int i = 0; i < packed.names.Length; i++)
                {
                    int pos = packed.names[i].IndexOf('.');
                    if (pos >= 0)
                    {
                        packed.names[i] = packed.names[i].Substring(pos+1);
                    }
                }

                //UnityEngine.Debug.Log("Packed file contains: " + packed.count + " | " + packed.names.Length + " | " + packed.jcts.Length);

				for (int i = 0; i < packed.count; i++) {
                    if (packed.names[i].Equals("base")) { 
						//found the base

						Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(packed.jcts[i]));


						using(StreamReader reader = new StreamReader(stream)){
							getJCTDataFromStream (reader, out baseNames, out baseNodes, out baseOffsets);
						}
						foundBase = true;
						break;
					}
				}

                if (!foundBase)
                {
                    UnityEngine.Debug.LogWarning("Unable to locate base morph in JCT file, this means JCTs WILL NOT WORK");
                }

				if (foundBase) {

					int[] nodeMap = calcNodeMap (bones, baseNames);
					List<JCTMorph> jct_list = new List<JCTMorph> ();

					//loop again and put the regular ones in

					for (int j = 0; j < packed.count; j++) {
						//if (baseNamePattern.Match (packed.names [j]).Success) { //pre 1.5
							Stream stream = new MemoryStream (Encoding.UTF8.GetBytes (packed.jcts [j]));
							string[] morph_names;
							Vector3[] nodes;
							Vector3[] offsets;

							string morph_name = Unescape (packed.names [j]);

							using (StreamReader reader = new StreamReader (stream)) {
								getJCTDataFromStream (reader, out morph_names, out nodes, out offsets);




								JCTMorph morph = new JCTMorph ();
								morph.m_name = morph_name;
								morph.m_value = 0;

								bool hasNodeDelta = false;
								Vector3[] nodeDeltas = new Vector3[bones.Length];

								Vector3[] nodeOffsets = new Vector3[bones.Length];
								for (int i = 0; i < bones.Length; i++) {
									int ii = nodeMap [i];
									nodeDeltas [i] = nodes [ii] - baseNodes [ii];
									nodeOffsets [i] = offsets [ii] - baseOffsets [ii];
									if (nodeDeltas [i].sqrMagnitude > Mathf.Epsilon) {
										hasNodeDelta = true;
									}
								}
								if (hasNodeDelta) {
									morph.m_nodes = nodeDeltas;
									morph.m_offsets = nodeOffsets;

									jct_list.Add (morph);
								}

							}
						//}
					}

                    if(jct_list.Count <= 0)
                    {
                        UnityEngine.Debug.LogWarning("JCT morph list is empty, morphs that affect bones will not work properly");
                    }

					JCTMorph[] morphs = jct_list.ToArray ();

					// add the data to the jct_adapter
					JCTTransition jct_adapter = destination.GetComponent<JCTTransition> ();
					// if (jct_adapter == null) {
					jct_adapter = destination.AddComponent<JCTTransition> ();
					// added by jesse for a compelte reference to all bones - theoretically since it comes from the figure mesh
					// jct_adapter.mesh_skeleton = skinned_mesh_renderer.bones;
					// }

					jct_adapter.m_morphs = morphs;
					jct_adapter.hideFlags = HideFlags.HideInInspector;
					// morpher.m_duplicates = duplicates;
					jct_adapter.CreationSetup (skinned_mesh_renderer);

					return;
				}
			}



			//LEGACY (1.0 way)


			// need root folder

			string morphFolder = assetPath.Substring (0, assetPath.LastIndexOf ("/")) + "/Morphs";


			string baseFile = morphFolder + "/" + baseName + ".base.morph";

			if (File.Exists (baseFile) == true) {
				string[] baseNames;
				Vector3[] baseNodes;
				Vector3[] baseOffsets;

				// root bone position data. all bones are stored in absolute format. we need both to put them in relative positioning
				getJCTDataFromFile (baseFile, out baseNames, out baseNodes, out baseOffsets);

				int[] nodeMap = calcNodeMap (bones, baseNames);
				string[] files = Directory.GetFiles (morphFolder, baseName + ".*.morph");
				List<JCTMorph> jct_list = new List<JCTMorph> ();

				foreach (var file in files) {
					if (File.Exists (file) == true) {
						string[] morph_names;
						Vector3[] nodes;
						Vector3[] offsets;
						getJCTDataFromFile (file, out morph_names, out nodes, out offsets);

						// we parse the file name to get the morph name
						char[] splitter = {'\\', '/'};
						string[] parts = file.Split (splitter);
						string morph_name = parts [parts.Length - 1];
						char[] splitter1 = {'.'};
						parts = morph_name.Split (splitter1);
						morph_name = Unescape (parts [1]);





						JCTMorph morph = new JCTMorph ();
						morph.m_name = morph_name;
						morph.m_value = 0;

						bool hasNodeDelta = false;
						Vector3[] nodeDeltas = new Vector3[bones.Length];

						Vector3[] nodeOffsets = new Vector3[bones.Length];
						for (int i = 0; i < bones.Length; i++) {
							int ii = nodeMap [i];
							nodeDeltas [i] = nodes [ii] - baseNodes [ii];
							nodeOffsets [i] = offsets [ii] - baseOffsets [ii];
							if (nodeDeltas [i].sqrMagnitude > Mathf.Epsilon) {
								hasNodeDelta = true;
							}
						}
						if (hasNodeDelta) {
							morph.m_nodes = nodeDeltas;
							morph.m_offsets = nodeOffsets;

							jct_list.Add (morph);
						}
					} else {
						Debug.LogWarning ("Missing morph file:"+file);
					}
				}

				JCTMorph[] morphs = jct_list.ToArray ();

				// add the data to the jct_adapter
				JCTTransition jct_adapter  = destination.GetComponent<JCTTransition> ();
				// if (jct_adapter == null) {
					jct_adapter = destination.AddComponent<JCTTransition> ();
					// added by jesse for a compelte reference to all bones - theoretically since it comes from the figure mesh
					// jct_adapter.mesh_skeleton = skinned_mesh_renderer.bones;
				// }

				jct_adapter.m_morphs = morphs;
				jct_adapter.hideFlags = HideFlags.HideInInspector;
				// morpher.m_duplicates = duplicates;
				jct_adapter.CreationSetup (skinned_mesh_renderer);
			}
		}

		/// <summary>
		/// Internal method for removing escape characters from a string
		/// </summary>
		/// <param name="s">S.</param>
		private static string Unescape (string s)
		{
			return s.Replace ("%20", " ");
		}
        
		/// <summary>
		/// Retrieve and parse JCT data from a file.
		/// </summary>
		/// <param name="file">File.</param>
		/// <param name="names">Names.</param>
		/// <param name="nodes">Nodes.</param>
		/// <param name="offsets">Offsets.</param>
		private static void getJCTDataFromFile (string file, out string[] names, out Vector3[] nodes, out Vector3[] offsets)
		{
			using (StreamReader reader = File.OpenText( file )) {
				getJCTDataFromStream (reader, out names, out nodes, out offsets);
			}
		}

		public static void getJCTDataFromStream (StreamReader reader, out string[] names, out Vector3[] nodes, out Vector3[] offsets)
		{
			int numNodes = System.Convert.ToInt32 (reader.ReadLine ());
			names = new string[numNodes];
			nodes = new Vector3[numNodes];
			offsets = new Vector3[numNodes];
			for (int i = 0; i < numNodes; i++) {
				string line = reader.ReadLine ();
				char[] splitter = {' '};
				string[] parts = line.Split (splitter);
				int n = parts.Length;
				string name = parts [0];
				for (int j = 1; j < (n - 6); j++)
					name += " " + parts [j];
				names [i] = Unescape (name);
				//the *.01 converts units from daz to unity meters methinks
				nodes [i].x = -(float)(System.Convert.ToDouble (parts [n - 6]) * 0.01);
				nodes [i].y = (float)(System.Convert.ToDouble (parts [n - 5]) * 0.01);
				nodes [i].z = (float)(System.Convert.ToDouble (parts [n - 4]) * 0.01);
				offsets [i].x = -(float)(System.Convert.ToDouble (parts [n - 3]) * 0.01);
				offsets [i].y = (float)(System.Convert.ToDouble (parts [n - 2]) * 0.01);
				offsets [i].z = (float)(System.Convert.ToDouble (parts [n - 1]) * 0.01);
			}
		}

		/// <summary>
		/// Internal method for calculating the node map for a given array of Transform bones.
		/// </summary>
		/// <returns>The node map.</returns>
		/// <param name="bones">Bones.</param>
		/// <param name="names">Names.</param>
		private static int[] calcNodeMap (Transform[] bones, string[] names)
		{
			int[] map = new int[bones.Length];
			for (int i = 0; i < bones.Length; i++) {
				int j = 0;
				for (j = 0; j < names.Length; j++) {
					if (JCTImportUtility.CleanBoneName( bones[i].name) == names[j]) {
						map[i] = j;
						// Debug.Log(i + " >> " + j + ", " + bones[i].name + " -> " + names[j]);
						break;
					}
				}

				if (j == names.Length)
					Debug.Log ("Error: Could not find match for bone " + i + ":" + bones[i].name);
			}
			return map;
		}



		/// <summary>
		/// Internal method for cleaning up a bone name.
		/// </summary>
		/// <returns>The bone name.</returns>
		/// <param name="bone">Bone.</param>
		private static string CleanBoneName (string bone) {
			string name = "";
		
			string pattern = @"(\s(?<=\s)\d+)?";
			Regex rgx = new Regex (pattern);
			name = rgx.Replace (bone, "");
			return name;
		}






		[Serializable]
		public struct JCTsPacked
		{
			public int count;
			public string[] names;
			public string[] jcts;
		}

	}
}
