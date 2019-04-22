using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MCS.CONSTANTS;
using MCS.FOUNDATIONS;

namespace MCS.COSTUMING
{
    /// <summary>
    /// <para>
    /// The CIhair class contains information on an idividual hair item.
    /// Inheriting from CostumeItem, which in turn is a MonoBehaviour; making this class a Component class.
    /// </para>
    /// <para>
    /// Use CharacterManager GetHairByID to get a reference to a specific CIHair attached to a figure.
    /// Use CharacterManager GetVisibleHair to get a List<CIHair> that contains all visible hair on the figure.
    /// </para>
    /// <para>
    /// The DLL takes care of attaching this MonoBehaviour as a component to hairs that are attached to 
    /// an MCS Figure either in Editor or through the API via the CharacterManager.
    /// </para>
    /// For attaching hair at runtime see <see cref="MCSCharacterManager.AddContentPack(ContentPack)"/> and <see cref="ContentPack"/> 
    /// </summary>
    public class CIhair : CostumeItem
	{

		private CoreMesh cap;
		//specific to support r2 hair
		private CoreMesh feathered;
		private CoreMesh opaque;

        //if set will install into the head material's shader if applicable to paint a texture
        /// <summary>
        /// If set - Overlay Texture2D that will be placed in the head material shader of the MCSFigure to which this CIhair is attached.
        /// </summary>
        public Texture2D overlay;
        /// <summary>
        /// If set - Overlay Color that will be applied to the head materail shader of hte MCSFigure to which this CIhair is attached.
        /// </summary>
        public Color overlayColor;

        /// <summary>
        /// Marks this CIHair to have it's overlay and overlayColor synced in the next frame update.
        /// </summary>
        public bool dirty = false;

//		CharacterManager charman;
//		JCTTransition jct;
		
        /// <summary>
        /// Implementation of MonoBehaviour Update method. Called once per frame.
        /// </summary>
		public override void Update(){
			if (currentVisibility != isVisible) {

				if(feathered == null && opaque == null)
					base.Update();
//				currentVisibility = isVisible;
				if(cap != null)
					cap.setVisibility (isVisible);
				if(feathered != null)
					feathered.setVisibility (isVisible);
				if(opaque != null)
					opaque.setVisibility (isVisible);

				currentVisibility = isVisible;
			}

            if (dirty)
            {
                SyncOverlay();
                dirty = false;
            }

            base.Update();
		}


		/// <summary>
		/// Overridden method for changing the LOD level.
		/// </summary>
		/// <param name="lodlevel">Lodlevel.</param>
		/// <param name="broadcast_change">If set to <c>true</c> then the change is broadcast .</param>
		override public bool setLODLevel (float lodlevel, bool broadcast_change = false)
		{
            bool changed = false;
            if (feathered == null && opaque == null)
            {
                changed = base.setLODLevel(lodlevel, broadcast_change);
            }
            return changed;
		}

		/// <summary>
		/// Overridden method to set a blendshape weight given a DisplayName
		/// </summary>
		/// <returns>true</returns>
		/// <c>false</c>
		/// <param name="displayName">Display name.</param>
		/// <param name="newValue">New value.</param>
		override public bool SetUnityBlendshapeWeight (string id, float newValue)
		{	
//			Debug.Log("GBLENDNDNDF");
//			DetectCoreMeshes();
			blendshape_cache[id] = newValue;
			foreach(CoreMesh mesh in LODlist){
//				Debug.Log("$($");
				mesh.SetUnityBlendshapeWeight(id, newValue);

			}
			if(cap != null)
				cap.SetUnityBlendshapeWeight (id, newValue);
			if(feathered != null)
				feathered.SetUnityBlendshapeWeight (id, newValue);
			if(opaque != null)
				opaque.SetUnityBlendshapeWeight (id, newValue);
			return true;
		}

		/// <summary>
		/// Overrideable method to get all CoreMesh Componenets.
		/// </summary>
		override public void DetectCoreMeshes(){
			base.DetectCoreMeshes();// this will not detect caps, feathered, and opaque pieces
			FindAndSyncCap ();
			FindAndSyncMeshesForR2 ();
            SyncOverlay();
		}

        /// <summary>
        /// Syncs the current overlay and overlayColor with the Head MATERIAL_SLOT of the MCS Figure to which this CIHair is atatched.
        /// 
        /// To schedule this task for the next call to Update set dirty to true.
        /// </summary>
        public void SyncOverlay()
        {
            if(charman == null || charman.figureMesh == null)
            {
                return;
            }
            Material headMat = charman.figureMesh.GetActiveMaterialInSlot(MATERIAL_SLOT.HEAD);
            if(headMat == null)
            {
                //UnityEngine.Debug.LogWarning("Could not locate head material");
                return;
            }

            Texture tmpOverlay = overlay;
            Color tmpOverlayColor = overlayColor;

            if (!isVisible)
            {
                tmpOverlay = null;
                tmpOverlayColor = Color.clear;
                headMat.DisableKeyword("_OVERLAY");
            } else
            {
                if (tmpOverlay != null || tmpOverlayColor.a > 0)
                {
                    headMat.EnableKeyword("_OVERLAY");
                } else
                {
                    headMat.DisableKeyword("_OVERLAY");
                }
            }

            if (headMat.HasProperty("_Overlay"))
            {
                headMat.SetTexture("_Overlay", tmpOverlay);
            }
            if(headMat.HasProperty("_OverlayColor"))
            {
                headMat.SetColor("_OverlayColor", tmpOverlayColor);
            }
        }

        /// <summary>
        /// Removes the overlay and overlayColor from the Head MATERIAL_SLOT
        /// of the MCS Figure to which this CIHair is attached.
        /// </summary>
        public void RemoveOverlay(bool onlyIfWasVisible=true)
        {

            if (!isVisible)
            {
                return;
            }

            if(charman == null || charman.figureMesh == null)
            {
                return;
            }
            Material headMat = charman.figureMesh.GetActiveMaterialInSlot(MATERIAL_SLOT.HEAD);
            if(headMat == null)
            {
                return;
            }

            Texture tmpOverlay = null;
            Color tmpOverlayColor = Color.clear;

            if (headMat.HasProperty("_Overlay"))
            {
                headMat.SetTexture("_Overlay", tmpOverlay);
            }
            if(headMat.HasProperty("_OverlayColor"))
            {
                headMat.SetColor("_OverlayColor", tmpOverlayColor);
            }

            headMat.DisableKeyword("_OVERLAY");
        }

		private void FindAndSyncCap(){
			CoreMesh[] meshes = gameObject.GetComponentsInChildren<CoreMesh>(true);
			foreach (CoreMesh mesh in meshes) {
				if (mesh.name.Contains ("_LOD") == false && mesh.name.Contains ("_CAP") == true) {
					cap = mesh;
					//resync blendshapes as needed
					if(blendshape_cache.Keys != null){
						foreach(string bs in blendshape_cache.Keys)
							cap.SetUnityBlendshapeWeight(bs, blendshape_cache[bs]);
					}
				}
			}
		}

		private void FindAndSyncMeshesForR2(){
			CoreMesh[] meshes = gameObject.GetComponentsInChildren<CoreMesh>(true);
			foreach (CoreMesh mesh in meshes) {
				if (mesh.name.Contains ("_LOD") == false && mesh.name.Contains ("_CAP") == false) {

					//assign feathered and opque for future use
					if (mesh.name.ToLower().Contains ("_feathered") == true)
						feathered = mesh;
					if (mesh.name.ToLower().Contains ("_opaque") == true)
						opaque = mesh;

					//resync blendshapes as needed
					if(blendshape_cache.Keys != null){
						foreach(string bs in blendshape_cache.Keys)
							mesh.SetUnityBlendshapeWeight(bs, blendshape_cache[bs]);
					}
				}
			}
		}

        /// <summary>
        /// Called when this MonoBehaviour is destoryed. Unecessary if CIHair
        /// is destoryed through CharacterManager Content Pack Removal.
        /// </summary>
        public override void OnDestroy()
        {
            //this isn't really necessary if you rely on content pack removal
            RemoveOverlay();
            base.OnDestroy();
        }

        /// <summary>
        /// Makes this CIHair visible in the scene.
        /// </summary>
        /// <param name="visible"></param>
        public override void SetVisibility(bool visible)
        {
            base.SetVisibility(visible);
            SyncOverlay();
        }

        /// <summary>
        /// MonoBehaviour implementation of Start method.
        /// </summary>
        public override void Start() {
			//lets attempt to compensate for someone hand deleting lods
			DetectCoreMeshes();
			TrySubscribeToJcts ();
        }

        private  void TrySubscribeToJcts(){
			if (this.meshType != MESH_TYPE.UNKNOWN && this.meshType != MESH_TYPE.PROP && Application.isPlaying) {
				//#ROOT HACK


//				if (charman == null)
//					//you should be able to reliably get the character manager with the same call from anywhere in the stack
//					charman = gameObject.GetComponentInParent<CharacterManager> ();
//				if (jct == null)
//					//the character manager is the obvious place to get anything deep in the stack because getinchildren works better than get in parent due to sibling relationships
//					jct = charman.GetJctManager ();


//				JCTTransition jct = gameObject.transform.root.GetComponentInChildren<JCTTransition> ();
				if (jct != null) {
					foreach (CoreMesh cm in LODlist) {
						jct.SubscribeToBindPoseChanges (cm.skinnedMeshRenderer);
					}
					if(cap != null)
						jct.SubscribeToBindPoseChanges (cap.skinnedMeshRenderer);
					if(feathered != null)
						jct.SubscribeToBindPoseChanges (feathered.skinnedMeshRenderer);
					if(opaque != null)
						jct.SubscribeToBindPoseChanges (opaque.skinnedMeshRenderer);
				}

			}
		}



	}
}
