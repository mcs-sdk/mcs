using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using MCS.FOUNDATIONS;
using MCS.CONSTANTS;
using MCS.UTILITIES;

namespace MCS.FOUNDATIONS
{
    /// <summary>
    /// Component class that is attached to each LOD of a
    /// figure or costume item.
    /// 
    /// The CoreMesh component is used internally to simplify the difference between
    /// a SkinnedMeshRenderer and a MeshRenderer+MeshFilter.
    /// 
    /// End users of the plugin will generally not need to use this class as the
    /// MCS system handles most use cases for this class automatically.
    /// </summary>
    [ExecuteInEditMode]
	public class CoreMesh : MonoBehaviour
	{
		/// <summary>
		/// Delegate for subscribing to OnBlendshapeValueChange
		/// </summary>
		public delegate void BlendshapeValueChange();
        /// <summary>
        /// Event raised when the value of a Blendshape on the SkinnedMeshRenderer wrapped
        /// in this CoreMesh changes.
        /// </summary>
		public event BlendshapeValueChange OnBlendshapeValueChange;



		/// <summary>
		/// Reference to mesh in the FBX
		/// </summary>
		public string dazName;



		/// <summary>
		/// Name and reference display to the API and consumer.
		/// </summary>
		public string ID;
			

		/// <summary>
		/// Flag that when true prevents modifications to the mesh.
		/// </summary>
		//public bool isLocked;

		//public bool isMeshEnabled { get { return (reference != null && reference.activeInHierarchy); } }
			
		//public GameObject reference;

		/// <summary>
		/// Costuming Item sets this value and uses it to sort by lod level.
		/// </summary>
		public float meshQuality;
			


		/// <summary>
		/// Internal method to raise and broadcast the OnBlendshapeValueChangge event.
		/// </summary>
		private void BroadcastBlendshapeValueChange ()
		{
			if (OnBlendshapeValueChange != null)
				OnBlendshapeValueChange ();
		}



		/// <summary>
		/// The type of the mesh. See <see cref="MCS.CONSTANTS.MESH_TYPE"/> 
		/// </summary>
		public MESH_TYPE meshType;

					
		// when we are told to set a morph to a particular value, we also scale if needed
		internal List<KeyValueFloat> blendshapeScalingList;
		internal SkinnedMeshRenderer _skinnedMeshRenderer ;



		/// <summary>
		/// The original mesh.
		/// </summary>
		[HideInInspector]
		[SerializeField]
		private Mesh originalMesh;



		/// <summary>
		/// The original bindposes.
		/// </summary>
		[HideInInspector]
		[SerializeField]
		private Matrix4x4[] originalBindposes;


        /// <summary>
        /// Path to the runtime location of the Morphs for the underlying Mesh.
        /// </summary>
		public string runtimeMorphPath;



        /// <summary>
        /// Gets a value indicating whether this <see cref="MCS.FOUNDATIONS.CoreMesh"/> is backed up.
        /// </summary>
        /// <value><c>true</c> if is backed up; otherwise, <c>false</c>.</value>
        public bool isBackedUp
		{
			get {
				return (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != originalMesh);
			}
		}



		/// <summary>
		/// MonoBehavour Awake Event
		/// </summary>
		void Awake ()
		{	
//			if (meshType != MESH_TYPE.PROP) {
//					buildBlendshapeDictionary ();
//			}

		}



		/// <summary>
		/// MonoBehavour Start Event
		/// </summary>
		void Start ()
		{
			if (meshType != MESH_TYPE.PROP) {
				if (Application.isPlaying) {
					SetupBackups ();
					//MakeRuntimeMeshInstance ();
				} else if (originalMesh == null || originalBindposes == null)
					SetupBackups ();
			}
		}



		/// <summary>
		/// Returns the skinned mesh renderer.
        /// 
        /// Will only have a SkinnedMeshRenderer if <see cref="meshType"/> is not <see cref="MESH_TYPE.PROP"/>  
		/// </summary>
		/// <value>The skinned mesh renderer.</value>
		public SkinnedMeshRenderer skinnedMeshRenderer
		{
			get {
				if (meshType != MESH_TYPE.PROP) {
					if (_skinnedMeshRenderer == null)
						_skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer> ();
					return _skinnedMeshRenderer;
				} else {
					return null;
				}
			}
		}



		/// <summary>
		/// Resets all bindposes for the mesh.
		/// </summary>
		[ContextMenu ("Reset Bindposes")]
		public void resetBindposes ()
		{
            Debug.Log("resetBindposes");
			if (originalBindposes != null)
				skinnedMeshRenderer.sharedMesh.bindposes = originalBindposes;
		}

        /// <summary>
        /// Tracks whether this CoreMesh contains duplicate meshes which may be required
        /// when <see cref="MakeRuntimeMeshInstance(bool)"/> or <see cref="MakeMeshBackupIfNeeded"/> are called.
        /// </summary>
        protected bool hasDuplicatedMesh = false;

		/// <summary>
		/// Creates a new mesh from the original.
		/// </summary>
		[ContextMenu ("Reset Mesh")]
		public void MakeRuntimeMeshInstance (bool keepName = false)
		{
			if (originalMesh == null || hasDuplicatedMesh)
				return;
			Mesh newmesh = (Mesh)Instantiate (originalMesh);
			if (keepName)
				newmesh.name = originalMesh.name;
			newmesh.MarkDynamic(); // http://docs.unity3d.com/ScriptReference/Mesh.MarkDynamic.html
			skinnedMeshRenderer.sharedMesh = newmesh;
            hasDuplicatedMesh = true;
        }



		/// <summary>
		/// Saves the current Mesh and bindposes as a backup.
		/// </summary>
		public void SetupBackups ()
		{
			if (_skinnedMeshRenderer == null)
				return;
			originalMesh = skinnedMeshRenderer.sharedMesh;
			originalBindposes = originalMesh.bindposes;
		}



		/// <summary>
		/// Checks if mesh is backed up, creates a backup if needed.
		/// </summary>
		public void MakeMeshBackupIfNeeded ()
		{
			if (meshType != MESH_TYPE.PROP && !isBackedUp) {
				if (originalMesh == null || originalBindposes == null)
					SetupBackups ();
				MakeRuntimeMeshInstance ();
			}
		}



		/// <summary>
		/// Returns a Mesh reference, even in the Editor.
		/// </summary>
		/// <returns>The runtime mesh.</returns>
		public Mesh GetRuntimeMesh ()
		{
			if (Application.isPlaying) {
				SetupBackups ();
				//MakeRuntimeMeshInstance ();
			} else if (originalMesh == null || originalBindposes == null) {
				SetupBackups ();
			}

			return skinnedMeshRenderer.sharedMesh;
		}



		/// <summary>
		/// Internal method to return the vertex count for this mesh.
		/// </summary>
		/// <value>The integer vertex count.</value>
		internal int vertexCount
		{
			get {
				if (meshType == MESH_TYPE.PROP) {
					return gameObject.GetComponent<MeshFilter>().sharedMesh.vertexCount;
				} else {
					return gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh.vertexCount;
				}
			}
		}
	

		/// <summary>
		/// Internal function to return the first section of a string up to but not including the first underscore character. 
		/// </summary>
		/// <returns>The resultant string.</returns>
		/// <param name="dirtyName">The string to slice.</param>
		internal static string ParseName (string dirtyName)
		{
			return dirtyName.Split (new char[]{'_'}) [0];
		}



		// Gets the mesh type from incoming dazName
//		protected string getMeshType ()
//		{
//			string[] templist = System.Enum.GetNames (typeof(MESH_TYPE));
//			foreach (string rmt in templist) {
//				if (dazName.Contains (rmt.ToString ())) {
//					meshType = (MESH_TYPE)System.Enum.Parse (typeof(MESH_TYPE), rmt);
//					return rmt;
//				}
//			}
//			return "";
//		}


				
			
	    /// <summary>
	    /// Given a Daz Blendshape Name, set the value of attached blendshape if it exist returns a boolean for success.
        /// On success fires the BlendshapeValueChange event.
        /// This method should be called with <see cref="CoreMorphs.SyncMorphValues(Morph[], List{GameObject})"/>.
	    /// </summary>
	    /// <returns><c>true</c>, if unity blendshape weight was set, <c>false</c> otherwise.</returns>
	    /// <param name="displayName">The string displayName.</param>
	    /// <param name="newValue">The float value</param>
		public bool SetUnityBlendshapeWeight (string ID, float newValue)
		{
            Debug.LogWarning("CoreMesh.SetUnityBlendshapeWeight being called directly, this should be done from coreMorphs instead");
			if(this.meshType != MESH_TYPE.PROP){
                Debug.Log("Setting: " + ID + " | " + ID + " | " + newValue + " | " + this.skinnedMeshRenderer.GetInstanceID());
                int blendshapeIndex = this.skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ID);
                if (blendshapeIndex >= 0)
                {
                    this.skinnedMeshRenderer.SetBlendShapeWeight(blendshapeIndex, newValue);
                    BroadcastBlendshapeValueChange();
                    return true;
                }

			}
			return false;
		}



		/// <summary>
		/// Sets the visibility of this GameObject by enabling/disabling the gameObject's Renderer component.
		/// </summary>
		/// <param name="new_vis">If set to <c>true</c> new vis.</param>
		public void setVisibility(bool new_vis)
		{
			//gameObject.SetActive(new_visibility); 
//			if (new_visibility == VISIBILITY.Visible) {
//				gameObject.GetComponent<Renderer> ().enabled = true;
//			} else {
//				gameObject.GetComponent<Renderer> ().enabled = false;
//			}
			gameObject.GetComponent<Renderer> ().enabled = new_vis;
		}



		/// <summary>
		/// Zeros all blendshapes.
		/// </summary>
        /// <remarks>
        /// Not currently supported.
        /// </remarks>
		public void zeroAllBlendshapes()
		{
            Debug.LogWarning("zeroALlBlendshapes is not supported right now");

			BroadcastBlendshapeValueChange ();
		}



		/// <summary>
		/// Zeros all blendshapes, and resets all blendshape weights
		/// </summary>
		[ContextMenu ("Reset Blendshapes")]
		public void ClearAllUnityBlendshapeWeights ()
		{
			zeroAllBlendshapes ();
		}



//		public Dictionary<string, int> blendshapeDictionary{
//			get{
//				if(_blendshapeDictionary == null)
//					buildBlendshapeDictionary();
//				return _blendshapeDictionary;
//			}
//		}
			


	}
}
