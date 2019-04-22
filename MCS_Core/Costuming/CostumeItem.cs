using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MCS.FOUNDATIONS;
using MCS.CONSTANTS;

namespace MCS.COSTUMING
{
	/// <summary>
	/// The CostumeItem class a MonoBehaviour Component base class attached to all CostumeItems
    /// 
    /// Obtain a reference to a subclass of CostumeItem ( <see cref="CIprop"/>, <see cref="CIclothing"/>, <see cref="CIhair"/>)
    /// with the <see cref="MCSCharacterManager"/> which provides useful methods for getting attached clothing, hair or props.
	/// </summary>
	[ExecuteInEditMode]
	public class CostumeItem : MonoBehaviour
	{
        /// <summary>
        /// Delegate for subscribing to <see cref="OnCostumeItemLODDidChange"/> 
        /// </summary>
        /// <param name="item"></param>
        public delegate void CostumeItemLODDidChange (CostumeItem item);
		/// <summary>
		/// Broadcast Event called when the LOD level changes via a call to setLODLevel().
		/// </summary>
		public event CostumeItemLODDidChange OnCostumeItemLODDidChange;



		/// <summary>
		/// Internal method to raise the CostumeItemLODDidChange event; indicating that the LOD level has changed via a call to SetLODLevel().
		/// </summary>
		private void BroadcastLODChange ()
		{
			if (OnCostumeItemLODDidChange != null)
				OnCostumeItemLODDidChange (this);
		}


        /// <summary>
        /// Delegate for subscribing to <see cref="OnCostumeItemVisibilityDidChange"/> 
        /// </summary>
        /// <param name="item"></param>
		public delegate void CostumeItemVisibilityChange (CostumeItem item);
		/// <summary>
		/// Broadcast Event called when the visibility changes via a call to setVisibility().
		/// </summary>
		/// <remarks>Currently SetVisibility is remarked out, and a constant check for visibility is done in the MonoBehaviour Update event.</remarks>
		public event CostumeItemVisibilityChange OnCostumeItemVisibilityDidChange;


		/// <summary>
		/// Internal method to raise the OnCostumeItemVisibilityDidChange event; indicating that the visibility has changed.
		/// </summary>
		private void BroadcastVisibilityChange ()
		{
			if (OnCostumeItemVisibilityDidChange != null)
				OnCostumeItemVisibilityDidChange (this);
		}



		/// <summary>
		/// Our DAZName eg CiaoBella_Jacket
		/// </summary>
		public string dazName;
        /// <summary>
        /// Uniqique ID for this CostumeItem
        /// </summary>
        public string ID;


		/// <summary>
		/// The List of CoreMeshes for this CostumeItem.
		/// </summary>
		public List<CoreMesh> LODlist;



		/// <summary>
		/// The quality array - unknown and currently unused.
		/// </summary>
		protected float[] qualityArray;



		/// <summary>
		/// The current LOD level.
        /// Should be treated as READONLY as setting this will not
        /// change the current LOD level.
		/// </summary>
		public float currentLODlevel = 1f;



		/// <summary>
		/// The MeshType of this CostumeItem see <see cref="MESH_TYPE"/> 
		/// </summary>
		public MESH_TYPE meshType = MESH_TYPE.BODY;



		/// <summary>
		/// Internal. Our reference to the CoreMesh
		/// </summary>
		private CoreMesh _currentCoreMesh;



		/// <summary>
		/// Provides access to the current <see cref="CoreMesh"/>
        /// of this CostumeItem.
		/// </summary>
		/// <value>The current core mesh.</value>
		public CoreMesh currentCoreMesh
		{
			get {
				if (_currentCoreMesh == null)
					DetectCoreMeshes ();
				return _currentCoreMesh;
			}
		}



		/// <summary>
		/// Flag to allow importer to assign materials.
		/// </summary>
		[HideInInspector]
		public bool lockMaterials = false;



		/// <summary>
		/// Internal Dictionary of blendshapes.
		/// </summary>
		Dictionary<string, float> _blendshape_cache;



		/// <summary>
		/// Provides access to the blendshape cache dictionary.
		/// </summary>
		/// <value>The blendshape cache.</value>
		public Dictionary<string, float> blendshape_cache
		{
			get {
				if (_blendshape_cache == null)
					_blendshape_cache =  new Dictionary<string, float>();
				return _blendshape_cache;
			}
		}



		/// <summary>
		/// True if this CostumeItem is attached to a figure.
        /// Directly setting this property will not change 
        /// whether this CostumeItem is attached. Instead use
        /// <see cref="MCSCharacterManager"/> detatch methods. 
		/// </summary>
		public bool isAttached = false;


		//for non serialized runtime access of jct maanger and characmanager
		private MCSCharacterManager _charman;
		protected MCSCharacterManager charman{
			get{
				if (_charman == null)
					_charman = gameObject.GetComponentInParent<MCSCharacterManager> ();
				return _charman;
			}
		}
		private JCTTransition _jct;
		protected JCTTransition jct{
			get{
				if (_jct == null && charman != null) {
					_jct = charman.GetJctManager ();
				}

				return _jct;
			}
		}

        internal void SetCharman(MCSCharacterManager charMan)
        {
            _charman = charMan;
        }


        // public bool isLocked = false;
        // private bool currentLock = false;
        /// <summary>
        /// Provides information on the visibility state of this CostumeItem.
        /// Setting this property will not change it's visibility status.
        /// To set the visibliity of this CostumeItem use <see cref="SetVisibility(bool)"/>.  
        /// </summary>
        public bool isVisible = true;



		/// <summary>
		/// boolean flag. Are we visible?
		/// </summary>
		protected bool currentVisibility = true;



		/// <summary>
		/// MonoBehaviour Awake event
		/// </summary>
		public virtual void Awake ()
		{
			//pull our current lod level from the character manager, otherwise this may be set to the default value of 1f
			if (Application.isPlaying && charman != null) {
				currentLODlevel = charman.currentLODLevel;
			}

			//Debug.Log ("LOD Level here:" + currentLODlevel);

			if (this.meshType == MESH_TYPE.UNKNOWN)
				Debug.LogWarning ("WARNING : Costume Item is of MESHTYPE UNKOWN. Please set a functional meshtype on the comstume item component : "+ name);

			if (LODlist == null)
				LODlist = new List<CoreMesh> ();

            currentVisibility = isVisible;
		}




		/// <summary>
		/// MonoBehaviour Start event
		/// </summary>
		public virtual void Start ()
		{
			if (Application.isPlaying && charman != null) {
				currentLODlevel = charman.currentLODLevel;
			}
			// lets attempt to compensate for someone hand deleting lods
			if (_currentCoreMesh == null) {
				//we only need to do this once, there is a GOOD chance the core meshes are already loaded, ie: see initCharacterManager
				DetectCoreMeshes ();
			}
			TrySubscribeToJcts ();
		}

        /// <summary>
        /// MonoBehaviour OnDestroy implementation, can be overridden in sub classes.
        /// </summary>
        public virtual void OnDestroy()
        {
            UnsubscribeToJCTs();
        }



		/// <summary>
		/// Internal method to subscribe us to the bind pose change event.
		/// </summary>
		private void TrySubscribeToJcts ()
		{
			// must be a valid meshtype
			if (this.meshType != MESH_TYPE.UNKNOWN && this.meshType != MESH_TYPE.PROP && Application.isPlaying) {
				//#ROOT HACK

//				if (charman == null)
//					//you should be able to reliably get the character manager with the same call from anywhere in the stack
//					charman = gameObject.GetComponentInParent<CharacterManager> ();
//				if (jct == null)
//					//the character manager is the obvious place to get anything deep in the stack because getinchildren works better than get in parent due to sibling relationships
//					jct = charman.GetJctManager ();

				//this method was stupid...
//				JCTTransition jct = gameObject.transform.root.GetComponentInChildren<JCTTransition> ();
//				if (this.meshType == MESH_TYPE.BODY || this.dazName.Contains("SimplifiedEyes") == true) {
////					Debug.Log (this.dazName);
//					jct = transform.parent.gameObject.GetComponentInChildren<JCTTransition> ();
//				}


				if (jct != null) {
					foreach (CoreMesh cm in LODlist) {
                        // Mesh m = cm.GetRuntimeMesh (); // hack to force unique instance
                        //UnityEngine.Debug.Log("Subscribing mesh: " + cm.skinnedMeshRenderer.name + " | " + cm.skinnedMeshRenderer.rootBone.name + " | " + cm.skinnedMeshRenderer.bones.Length);

                        if(cm.skinnedMeshRenderer == null)
                        {
                            //This might be the case for something like a prop or anything weird that has just a meshfilter but no smr
                            continue;
                        }
						jct.SubscribeToBindPoseChanges (cm.skinnedMeshRenderer);
					}
				}
			}
		}

        /// <summary>
        /// Unsubscribes this CostumeITem from the <see cref="JCTTransition"/> service.
        /// </summary>
        public void UnsubscribeToJCTs()
        {
            if(jct != null)
            {
                foreach (CoreMesh cm in LODlist) {
                    if(cm == null)
                    {
                        UnityEngine.Debug.LogWarning("Attempting to unsubscribe from jct but cm is null for: " + gameObject.name);
                        continue;
                    }
                    jct.UnsubscribeToBindPoseChanges(cm.skinnedMeshRenderer);
                }
            }
        }

        /// <summary>
        /// Toggles the visibility of this CostumeItem in the scene.
        /// Use this method to set visibliity instead of directly setting <see cref="isVisible"/> 
        /// </summary>
        /// <param name="visible"></param>
        public virtual void SetVisibility(bool visible)
        {
            isVisible = visible;
            if (currentVisibility != isVisible)
            {

                if (currentCoreMesh == null)
                {
                    UnityEngine.Debug.LogWarning("No core mesh found for item: " + gameObject.name + " skipping visibility toggle");
                    return;
                }

                currentCoreMesh.setVisibility(isVisible);
                currentVisibility = isVisible;
                BroadcastVisibilityChange();
            }
        }


		/// <summary>
		/// MonoBehaviour Update event.
		/// </summary>
		public virtual void Update ()
		{

            if (isBoundsDirty)
            {
                RecalculateBounds();
            }

            //support the old method which is to set isVisible = ? in the item directly, this is not suggested as you will have race conditions on Update calls
            if (currentVisibility != isVisible)
            {
                UnityEngine.Debug.LogWarning("You should switch your call from OBJ.isVisible = ? to OBJ.SetVisibility(?) to improve performance and prevent race conditions");
                SetVisibility(isVisible);
            }
		}



		/// <summary>
		/// Adds a given CoreMesh reference to the interal LOD list.
		/// </summary>
		/// <param name="cm">Cm.</param>
		virtual public void AddCoreMeshToLODlist (CoreMesh cm)
		{
			Cleanup ();

			if (LODlist == null)
				LODlist = new List<CoreMesh> ();

            //make sure we can find a mesh, otherwise skip it
            Mesh m = null;
            SkinnedMeshRenderer smr = cm.GetComponent<SkinnedMeshRenderer>();
            MeshFilter mf = cm.GetComponent<MeshFilter>();
            if(smr != null)
            {
                m = smr.sharedMesh;
            } else if(mf != null)
            {
                m = mf.sharedMesh;
            }

            if (m == null)
            {
                if (Application.isEditor)
                {
                    UnityEngine.Debug.LogError("Could not locate a mesh for CoreMesh: " + cm.name + ", skipping. Try removing the content pack from your figure, reimport, then re-add.");
                }
                return;
            }


            LODlist.Add(cm);
			List<CoreMesh> new_list = LODlist.OrderByDescending (o=>o.vertexCount).ToList ();
			LODlist = new_list;

			// float base_verts = (float) new_list.First ().vertexCount;
			// sets meshquality based on vertcount from of highest
			// foreach (CoreMesh c in new_list) {
			// 		float compare_verts = (float) c.vertexCount;
			// 		c.meshQuality = compare_verts/base_verts;
			// }

			// set meshquality by index of total list size
			int i = 0;
			int count = LODlist.Count;

			for (;i < count; i++) {
				float value =  1f/(float)Math.Pow (2, i);
				new_list[i].meshQuality = value;
			}

			setLODLevel (currentLODlevel);

			// make new current mesh have updated blendshapes
			if (blendshape_cache.Keys != null) {
				foreach (string bs in blendshape_cache.Keys) {
					foreach (CoreMesh mesh in LODlist) {
						mesh.SetUnityBlendshapeWeight (bs, blendshape_cache[bs]);
					}
				}
			}
		}



		/// <summary>
		/// Overrideable method for setting the LOD level.
		/// </summary>
		/// <param name="lodlevel">Lodlevel.</param>
		/// <param name="broadcast_change">If set to <c>true</c> broadcast change.</param>
		virtual public bool setLODLevel(float lodlevel, bool broadcast_change = false)
		{
			Cleanup();

			_currentCoreMesh =  (_currentCoreMesh == null) ? LODlist.FirstOrDefault() : _currentCoreMesh; // there must always be one!!!!
            CoreMesh bestCoreMesh = null;


            float bestQuality = Mathf.Infinity;

            //find the closest matching mesh
            foreach(CoreMesh cm in LODlist)
            {
                if (cm.meshQuality >= lodlevel && cm.meshQuality < bestQuality)
                {
                    bestCoreMesh = cm;
                    bestQuality = cm.meshQuality;
                }
            }

            
            if(bestCoreMesh == null)
            {
                UnityEngine.Debug.LogWarning("Could not detect best core mesh to show for: " + gameObject.name + " LOD: " + lodlevel + " forcing default: " + _currentCoreMesh.name);
                bestCoreMesh = _currentCoreMesh;
            }


            bool hasChanged = bestCoreMesh != _currentCoreMesh;

            foreach(CoreMesh cm in LODlist)
            {
                if(cm != bestCoreMesh)
                {
                    cm.setVisibility(false);
                } else
                {
                    cm.setVisibility(isVisible);
                }
            }

            if (hasChanged)
            {
                _currentCoreMesh = bestCoreMesh;

                if (blendshape_cache.Keys != null) {
					foreach (string bs in blendshape_cache.Keys)
						currentCoreMesh.SetUnityBlendshapeWeight(bs, blendshape_cache[bs]);
				}

                //if we modified the body, force a recalculation of alpha injection
                CIbody cibody = gameObject.GetComponent<CIbody>();
                if(cibody != null)
                {
                    charman.SyncAlphaInjection();
                }

                if (broadcast_change)
                {
                    BroadcastLODChange();
                }

                currentLODlevel = bestQuality;
            }

            return hasChanged;
		}



		/// <summary>
		/// Overrideable method to set a blendshape weight given a DisplayName
		/// </summary>
		/// <returns><c>true</c>, if unity blendshape weight was set, <c>false</c> otherwise.</returns>
		/// <param name="displayName">Display name.</param>
		/// <param name="newValue">New value.</param>
		/// <remarks>We need to track this internally so when a lod level changes we can update the latest blendshape.</remarks>
		virtual public bool SetUnityBlendshapeWeight (string id, float newValue)
		{
			blendshape_cache[id] = newValue;
			return currentCoreMesh.SetUnityBlendshapeWeight (id, newValue);
		}

        //walk the up the hierachy and verify there is no other CostumeItem between the passed go and the current game object
        protected bool EnsureNoOtherCostumeItemBetweenCoreMesh(GameObject go)
        {
            GameObject parent = go.transform.parent.gameObject;

            if (parent == null || parent.GetInstanceID().Equals(gameObject.GetInstanceID()))
            {
                //we're done, we found a good match or no match
                return true;
            }

            CostumeItem tmpCI = parent.GetComponent<CostumeItem>();
            if (tmpCI != null)
            {
                return false;
            }

            return EnsureNoOtherCostumeItemBetweenCoreMesh(parent);
        }

		/// <summary>
		/// Overrideable method to add all CoreMesh Componenets attached as to components
        /// to any child GameObject of the GameObject to which this CostumeItem is attached
        /// to <see cref="LODlist"/>. 
		/// </summary>
		virtual public void DetectCoreMeshes()
		{
            CoreMesh[] meshes = gameObject.GetComponentsInChildren<CoreMesh>(true);
            if(meshes.Length <= 0)
            {
                UnityEngine.Debug.LogWarning("DetectCoreMeshes found 0 coreMesh from: " + gameObject.name);
            }
			LODlist = new List<CoreMesh>();

            foreach (CoreMesh mesh in meshes)
            {

                if (mesh.meshType != meshType)
                {
                    //the CoreMesh does not match the CostumeItem mesh type, skip it
                    continue;
                }

                //make sure this costume item "owns" this coremesh, only do this if we're not a prop though
                if (mesh.meshType != MESH_TYPE.PROP && !EnsureNoOtherCostumeItemBetweenCoreMesh(mesh.gameObject))
                {
                    if(mesh.meshType == MESH_TYPE.PROP)
                    {
                        //this is a common case for props, just move along, this is b/c CIBody will trigger props since props are under the skeleton of CIBody
                        continue;
                    }
                    UnityEngine.Debug.LogWarning("I do not own this mesh, skipping: " + mesh.gameObject.name + " from: " + gameObject.name);
                    UnityEngine.Debug.LogWarning("Please try removing the content pack, reimporting the asset's .mon file, then reattaching");
                }

                bool addMesh = false;
                switch (meshType)
                {
                    case MESH_TYPE.HAIR:
                        //we need to check for skull caps

                        string tmpName = mesh.name.ToLower();
                        if(tmpName.Contains("_cap") || tmpName.Contains("_feathered") || tmpName.Contains("_opaque"))
                        {
                            //these are all "skull caps" and we should not include them in the lod list
                        } else
                        {
                            //these are normal hair items, include them
                            addMesh = true;
                        }
                        break;
                    case MESH_TYPE.BODY:
                        //TODO: do we need to do anything specifically for the eyes, gen2 has LOD for eyes...
                        //genesis 3 has embedded eyes (submesh), without lods
                    default:
                        addMesh = true;
                        break;
                }

                if (addMesh)
                {
                    AddCoreMeshToLODlist(mesh);
                }
			}

            //if current core mesh is null, assign the first lod we found to the "active" mesh
			if (_currentCoreMesh == null)
			{
				_currentCoreMesh = LODlist.FirstOrDefault();
			}

            //is it still null, if so this is bad
            if (_currentCoreMesh == null)
            {
                UnityEngine.Debug.LogWarning("Unable to find first CoreMesh/LOD in costume item: " + gameObject.name + ", your asset will likely break and not work");
            }
		}



		/// <summary>
		/// Returns the Runtime Mesh of the <see cref="currentCoreMesh"/> 
		/// </summary>
		/// <returns>The mesh.</returns>
		public Mesh GetMesh ()
		{
			return currentCoreMesh.GetRuntimeMesh ();
		}



		/// <summary>
		/// Return the SkinnedMeshRenderer belonging to the <see cref="currentCoreMesh"/> 
        /// for this CostumeItem, or null if non existant.
        /// Note: <see cref="CIProp"/>s will NOT have a SkinnedMeshRenderer. 
		/// </summary>
		/// <returns>The skinned mesh renderer.</returns>
		public SkinnedMeshRenderer GetSkinnedMeshRenderer ()
		{
			if (currentCoreMesh.meshType != MESH_TYPE.PROP)
			{
				return currentCoreMesh.skinnedMeshRenderer;
			} else {
				return null;
			}
		}

        /// <summary>
        /// Returns all SkinnedMeshRenderer objects belonging to all <see cref="CoreMesh"/>es
        /// in the <see cref="LODlist"/>.
        /// </summary>
        /// <returns></returns>
        public List<SkinnedMeshRenderer> GetSkinnedMeshRenderers()
        {
            List<SkinnedMeshRenderer> smrs = new List<SkinnedMeshRenderer>();
            foreach(CoreMesh cm in LODlist)
            {
                SkinnedMeshRenderer smr = cm.GetComponent<SkinnedMeshRenderer>();
                if(smr != null)
                {
                    smrs.Add(smr);
                }
            }

            return smrs;
        }



		/// <summary>
		/// Return the <see cref="MESH_TYPE"/> for this CostumeItem.
		/// </summary>
		/// <returns>The mesh type.</returns>
		public MESH_TYPE GetMeshType ()
		{
			return currentCoreMesh.meshType;
		}



		/// <summary>
		/// Internal method. Cleanup this CostumeItem, removing all CoreMesh items from the LODlist.
		/// </summary>
		private void Cleanup ()
		{
			bool keepGoing;
			do {
				keepGoing = LODlist.Remove (null);
			} while(keepGoing);

		}

        protected bool isBoundsDirty = false;
        /// <summary>
        /// Tells the system on next frame to update the bounds of the item
        /// </summary>
        public void MarkBoundsDirty()
        {
            isBoundsDirty = true;
        }

#if UNITY_TARGET_GTE_5_5
        private Dictionary<int, List<Vector3>> _smrVertices = new Dictionary<int, List<Vector3>>();
#endif

        /// <summary>
        /// Recalculates the boundary of the mesh based on current blends/jct/animation
        /// </summary>
        public void RecalculateBounds()
        {
            //UnityEngine.Debug.Log("Recalculating: " + gameObject.name);
            List<SkinnedMeshRenderer> smrs = GetSkinnedMeshRenderers();

            //nothing to recalc
            if (smrs.Count <= 0)
            {
                return;
            }

            //if we have an animation with unbaked root position we need to offset by this amount or our bounds will be wrong
            Vector3 offset = Vector3.zero;
            if (charman != null)
            {
                Animator anim = charman.gameObject.GetComponent<Animator>();
                if (anim != null && (Vector3.zero - anim.rootPosition).magnitude > 0.001f)
                {
                    offset = anim.rootPosition;
                    //offset -= charman.transform.localPosition;
                }
            } else
            {
                offset = transform.localPosition;
            }

            foreach (SkinnedMeshRenderer smr in smrs)
            {
                if (!smr.enabled)
                {
                    continue;
                }

                /*
                smr.transform.localScale = new Vector3(1f, 1f, 1f);
                smr.transform.localRotation = Quaternion.identity;
                smr.transform.localPosition = Vector3.zero;
                */


                Mesh mesh = new Mesh();
                smr.BakeMesh(mesh);

                //If you want to debug it...
                /*
                GameObject tmpGO = new GameObject();
                MeshFilter mf = tmpGO.AddComponent<MeshFilter>();
                MeshRenderer mr = tmpGO.AddComponent<MeshRenderer>();
                mf.sharedMesh = mesh;
                */

                float minX = Mathf.Infinity;
                float minY = Mathf.Infinity;
                float minZ = Mathf.Infinity;
                float maxX = -1f * Mathf.Infinity;
                float maxY = -1f * Mathf.Infinity;
                float maxZ = -1f * Mathf.Infinity;


                //TODO: if we are using unity 5.5 we can use mesh.GetVertices(List<Vector3> verts) instead which won't allocate
                //NOTE: this directive does NOT exist yet, this will never compile as we haven't coded it in yet
#if UNITY_TARGET_GTE_5_5
                List<Vector3> verts = null;
                int id = smr.GetInstanceID();
                if(!_smrVertices.TryGetValue(id, out verts)){
                    verts = new List<Vector3>();
                    _smrVertices[id] = verts;
                }
                mesh.GetVertices(verts);
                int length = verts.Count;
#else
                //For legacy unity (below 5.5) we'll get a copy of verts (a new allocation) this causes noticable GC
                Vector3[] verts = mesh.vertices;
                int length = verts.Length;
#endif
                for (int i = 0; i < length; i++)
                {
                    Vector3 v = verts[i] + offset;
                    minX = Mathf.Min(minX, v.x);
                    minY = Mathf.Min(minY, v.y);
                    minZ = Mathf.Min(minZ, v.z);
                    maxX = Mathf.Max(maxX, v.x);
                    maxY = Mathf.Max(maxY, v.y);
                    maxZ = Mathf.Max(maxZ, v.z);
                }

                Vector3 c = new Vector3((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);
                Vector3 s = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);

                //if we have a root bone, use it to get the local position instead of world
                if (smr.rootBone != null) {
                    c = smr.rootBone.InverseTransformPoint(c);
                }

                Bounds bounds = new Bounds(c, s);
                //UnityEngine.Debug.Log("Bounds: " + bounds.ToString("F6"));
                smr.localBounds = bounds;
            }

            isBoundsDirty = false;
        }



//		//we'll broadcast this in the update loop
//		public delegate void CostumeItemLockChange(CostumeItem item);



//		public event CostumeItemLockChange OnCostumeItemLockChange;
//		private void BroadcastLockChange(){
//			if(OnCostumeItemLockChange != null)
//				OnCostumeItemLockChange(this);
//		}



//		public void SetVisibility(VISIBILITY new_visibility){
//			currentCoreMesh.setVisibility (new_visibility);
//		}



//		//mostly for importer
//		public void AssignPrimaryMaterial(Material mat, bool lock_material = false)
//		{
//			foreach (CoreMesh mesh in LODlist) {
//				mesh.AssignPrimaryMaterial (mat);
//			}
//
//			if (lock_material) {
//				lockMaterials = true;
//			}
//		}



//		void OnEnable(){
////			Debug.Log(name + ":ENABLED");
//			BroadcastVisibilityChange();
//			if(this.isLocked == false)
//				this.isVisible = true;
//		}
//



//		void OnDisable(){
////			Debug.Log(name+":DISABLED");
//			BroadcastVisibilityChange();
//			if(this.isLocked == false)
//				this.isVisible = false;
//		}



	}
}
