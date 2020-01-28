using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System;

using MCS.UTILITIES;
using MCS.FOUNDATIONS;
using MCS.COSTUMING;
using MCS.SERVICES;
using MCS.CONSTANTS;

namespace MCS.CORESERVICES
{
    /// <summary>
    /// <para>
    /// The AlphaInjectionManager gets instantiated and bound to the figure events and the costumteModel by the character manager
    /// but in every other way it completely bypasses anything else that is going on with the character manager.
    /// </para>
    /// <para>
    /// The AlphaInjectionmanager is responsible for combining the <see cref="CIclothing.alphaMasks"/> for all <see cref="CIclothing"/>
    /// attatched to the figure into an Alpha Injection Mask that is used to add transparency to faces on the figure that are occluded by clothing.
    /// </para>
    /// <para>
    /// Normally a developer won't need to use the AlphInjectionManager as everything that needs to be done is already handled. Nevertheless,
    /// a reference to teh AlphaInjectionManager is available through the <see cref="MCSCharacterManager.alphaInjection"/> property.
    /// </para>
    /// </summary>
    [ExecuteInEditMode]
    public class AlphaInjectionManager : MonoBehaviour //, ISerializationCallbackReceiver
    //public class AlphaInjectionManager
    {


        //tracks our temporary render textures for easy cleanup
        [NonSerialized]
        private Dictionary<int,RenderTexture> temporaryRenderTextures = new Dictionary<int,RenderTexture>();
        [NonSerialized]
        private List<Texture2D> temporaryTexture2D = new List<Texture2D>();

        //It doesn't look like we use this right now?
        [SerializeField]
        public List<Material[]> _originalMaterials = new List<Material[]>();

        /// <summary>
        /// Contains a reference to the original shared materials loaded on the SkinnedMeshRenderer components
        /// that are children of the figure. This is exposed to provide state information to developers, but should
        /// not be modified.
        /// </summary>
        [SerializeField]
        public Material[] originalMaterialReferences;

        [NonSerialized]
        private Dictionary<int,Material[]> _duplicatedMaterials = new Dictionary<int, Material[]>();

        private bool restoreOnNextFrame = false; //used for serialization


        //private List<int> __clonedMaterialsKeys = new List<int>();


		/// <summary>
		/// The currently processing. To stop the draw function from being called until we have drawn our stuff
		/// </summary>
		private bool currentlyProcessing = false;


        private bool dirty = false;

        /// <summary>
        /// <para>
        /// FAST = Do most processing in gpu, copy to cpu, then back to gpu, very reliable and costs ~8ms
        /// </para>
        /// <para>
        /// FASTEST = Do all processing gpu, never copy to general memory, this is VERY fast at sub 1ms.
        /// </para>
        /// </summary>
        public enum TEXTURE_MODE {
            FAST, //Do most processing in gpu, copy to cpu, then back to gpu, this is very reliable and costs about 8ms
            FASTEST //Do all processing in gpu, never copy to general memory, this is VERY fast at sub 1ms
        };
        /// <summary>
        /// Current <see cref="TEXTURE_MODE"/> for this <see cref="AlphaInjectionManager"/>  
        /// </summary>
        public TEXTURE_MODE textureMode = TEXTURE_MODE.FASTEST;



		// Timer timer;//our delay call



		/// <summary>
		/// Our reference to the the main figure
		/// </summary>
        //[SerializeField]
		CostumeItem figure;



		/// <summary>
		/// Our reference tot he main clothing model.
		/// </summary>
        //[SerializeField]
		CostumeModel clothingModel;



        /// <summary>
        /// The List of unique items to draw. We'll add unique items to the list and draw what's needed then empty it.
        /// </summary>
        /*
		HashSet<Texture2D> _imagesToDraw;
		public HashSet<Texture2D> imagesToDraw
		{
			get {
				if (_imagesToDraw == null)
					_imagesToDraw = new HashSet<Texture2D> ();
				return _imagesToDraw;
			}
		}
		*/

		//1.5 multi material alpha injection texture list (this will be populated with each clothing's injection mask slot)
		public Dictionary<MATERIAL_SLOT,Dictionary<Texture2D,Texture2D>> subAlphaTextures = new Dictionary<MATERIAL_SLOT,Dictionary<Texture2D,Texture2D>>();


        /*
        public void OnBeforeSerialize()
        {
            //UnityEngine.Debug.Log("Before Serialize: " + gameObject.name + " id: " + gameObject.GetInstanceID());
        }
        public void OnAfterDeserialize()
        {
            //UnityEngine.Debug.Log("After Serialize: " + gameObject.name + " id: " + gameObject.GetInstanceID());
        }
        */


        [NonSerialized]
        private bool isAwake = false;
        /// <summary>
        /// Monobehaviour Awake method.
        /// </summary>
        public void Awake()
        {
            //UnityEngine.Debug.Log("Awake: " + gameObject.name);
            _duplicated = false;
            //don't show this component in the inspector pane
            this.hideFlags = HideFlags.HideInInspector;
            isAwake = true;
        }

        /// <summary>
        /// Monobehaviour Start method.
        /// </summary>
        public void Start()
        {
            if (_duplicated)
            {
              //  return;
            }
            //UnityEngine.Debug.Log("Start: " + gameObject.name + " awake: " + isAwake);

            if (originalMaterialReferences == null)
            {
                ForceCurrentMaterialsIntoOriginals();
            }
        }

        /// <summary>
        /// Places the current Materials on each SkinnedMeshRender that is a component of any child of the GameObject into <see cref="originalMaterialReferences"/>.
        /// You can use this method if you replace the original mat with a custom mat and need to work with a prefab or duplicate or similar
        /// </summary>
        public void ForceCurrentMaterialsIntoOriginals()
        {
            CoreMesh[] coreMeshes = gameObject.GetComponentsInChildren<CoreMesh>(true);

            List<Material> allMats = new List<Material>();

            for (int i = 0; i < coreMeshes.Length; i++)
            {
                if (coreMeshes[i].meshType != MESH_TYPE.BODY)
                {
                    continue;
                }

                SkinnedMeshRenderer smr = coreMeshes[i].gameObject.GetComponent<SkinnedMeshRenderer>();
                if (smr == null)
                {
                    continue;
                }

                Material[] materials = smr.sharedMaterials;

                for (int j = 0; j < materials.Length; j++)
                {
                    allMats.Add(materials[j]);
                }
            }

            originalMaterialReferences = allMats.ToArray();
        }

        /*
        /// <summary>
        /// Initializes a new instance of the <see cref="MCS.CORESERVICES.AlphaInjectionManager"/> class.
        /// </summary>
        //public AlphaInjectionManager ()
		//{
		//}

        //NOTE, remove this if we convert to a monobehaviour
        /*
        ~AlphaInjectionManager()
        {
            OnDestroy();
        }
        */

        /// <summary>
        /// Monobehaviour OnDestroy implementation.
        /// </summary>
        public void OnDestroy()
        {
            cleanup();
            FreeEditorDuplicateMaterials();
        }

        /// <summary>
        /// Frees resources associated with RenderTextures and Texture2Ds that are being 
        /// used by this AlphaInjectionManager.
        /// </summary>
        public void cleanup()
        {
            var tempEnum = temporaryRenderTextures.GetEnumerator();
            //TODO: MoveNext() on a dictionary<T> causes a 32byte allocation on the first call, maybe update this in the future to not need it, and work off lod numbers instead of smr ids
            while(tempEnum.MoveNext())
            {
                RenderTexture rt = temporaryRenderTextures[tempEnum.Current.Key];
                if (rt.IsCreated())
                {
                    rt.Release();
                }
                if (Application.isPlaying)
                {
                    RenderTexture.Destroy(rt);
                }
                else
                {
                    RenderTexture.DestroyImmediate(rt);
                }
            }
            temporaryRenderTextures.Clear();
            
            for(int i=0;i<temporaryTexture2D.Count;i++)
            {
                if (Application.isPlaying)
                {
                    Texture2D.Destroy(temporaryTexture2D[i]);
                }
                else
                {
                    Texture2D.DestroyImmediate(temporaryTexture2D[i]);
                }
            }
            temporaryTexture2D.Clear();
        }

        protected int GetKeyFromSMR(SkinnedMeshRenderer smr)
        {
            return smr.GetInstanceID();
        }

        [NonSerialized]
        private bool _duplicated = false;

        /// <summary>
        /// Frees resources associated with duplicated Materials that this AlphaInjectionManager is using.
        /// </summary>
        public void FreeEditorDuplicateMaterials()
        {
            //only do this in editor and if we duped
            if (Application.isPlaying || _duplicatedMaterials.Count<=0)
            {
                return;
            }
            foreach (int key in _duplicatedMaterials.Keys)
            {
                for (int i = _duplicatedMaterials[key].Length - 1; i >= 0; i--)
                {
                    DestroyImmediate(_duplicatedMaterials[key][i]);
                }
            }
        }

        //Currently does nothing. I think because this is done in FreeEditorDuplicateMaterials
        public void ClearMaterialBuffers(bool fetch = true)
        {
            //_duplicatedMaterials.Clear();
        }

        /// <summary>
        /// Gets a clone of the shared materials on the current SkinnedMeshRender of the figure.
        /// </summary>
        public Material[] GetMaterialsClone()
        {
            if(figure == null)
            {
                return null;
            }

            SkinnedMeshRenderer renderer = figure.GetSkinnedMeshRenderer();
            Material[] materials = figure.GetSkinnedMeshRenderer().sharedMaterials;

            int id = GetKeyFromSMR(renderer);

            bool hadErrors = false;

            if (!_duplicatedMaterials.ContainsKey(id))
            {
                for(int i = 0; i < materials.Length; i++)
                {

                    if(materials[i] == null)
                    {

                        //can we recover this material from our backups?

                        int offset = -1;

                        float lod = figure.currentLODlevel;


                        if (lod > 0.5f)
                        {
                            offset = 0;
                        } else if(lod > 0.25f)
                        {
                            offset = 1;
                        } else if(lod > 0.12f)
                        {
                            offset = 2;
                        } else
                        {
                            offset = 3;
                        }

                        //UnityEngine.Debug.Log("LOD: " + lod + " figure: " + figure.currentLODlevel + " mesh: " + figure.currentCoreMesh.name + " offset: " + offset);

                        if (offset < 0)
                        {
                            continue;
                        }

                        int slot = (offset * (materials.Length)) + i;

                        //UnityEngine.Debug.Log("Slot: " + slot + " offset: " + offset + " materials: " + materials.Length + " i: " + i);

                        if (originalMaterialReferences.Length - 1 >= slot)
                        {
                            if (originalMaterialReferences[slot] != null) { 
                                UnityEngine.Debug.Log("Unable to recover instanced material, using original mat instead: " + originalMaterialReferences[slot].name + " slot: " + slot + " i: " + i);
                                materials[i] = new Material(originalMaterialReferences[slot]);
                                hadErrors = true;
                                continue;
                            } else
                            {
                                UnityEngine.Debug.LogWarning("Unable to locate suitable replacement material, consider replacing all figure skinned mesh renderer materials with non instanced versions then calling ForceCurrentMaterialsIntoOriginals");
                            }
                        }
                    }

                    materials[i] = (materials[i] != null ? new Material(materials[i]) : null);
                }
                _duplicatedMaterials[id] = materials;
            }

            if (hadErrors)
            {
                MCSCharacterManager charMan = GetComponentInParent<MCSCharacterManager>();
                if(charMan != null)
                {
                    //update our overlays b/c they might be dirty now, but do it on the next frame
                    charMan.MarkHairAsDirty();
                }
            }

            return materials;
        }

        /// <summary>
        /// Internal method. Updates the figure texture.
        /// </summary>
        private void updateFigureTexture ()
		{
			// if (baseTextures == null)
			// 		baseTextures = new Dictionary<CostumeItem, Texture2D>();
			// if (baseTextures.ContainsKey(figure) == null) {
			// 		Debug.Log ("ADDING FIGURE TEX:"+figure.name+":"+figure.GetSkinnedMeshRenderer().sharedMaterial.mainTexture.name);
			// 		baseTextures[figure] = figure.GetSkinnedMeshRenderer().sharedMaterial.mainTexture as Texture2D;
			// }
			// Debug.Log("UPDATE FIGURE TEXTURE:"+ figure.name);
			// if (Application.isPlaying)
			// 		baseTexture = figure.GetSkinnedMeshRenderer().material.mainTexture as Texture2D;
		}



		/// <summary>
		/// Internal method. Updates the clothing textures.
		/// </summary>
		private void updateClothingTextures ()
		{
			List<CostumeItem> visible_clothing = clothingModel.GetVisibleItems ();
            //Debug.Log("AlphaInjectionManager.updateClothingTextures: " + visible_clothing.Count);
            subAlphaTextures.Clear();

            foreach (CostumeItem clothing_item in visible_clothing) {
				// Debug.Log ("VISBLECLOTH:"+clothing_item.name+":"+clothing_item.isVisible);
				CIclothing ci = (CIclothing)clothing_item;

                //LEGACY 1.0 code
                //if (ci.alphaMask != null)
                //	imagesToDraw.Add (ci.alphaMask);

                var matEnum = ci.alphaMasks.GetEnumerator();
                while (matEnum.MoveNext()) {
                    MATERIAL_SLOT slot = matEnum.Current.Key;
					if (!subAlphaTextures.ContainsKey (slot)) {
						subAlphaTextures [slot] = new Dictionary<Texture2D,Texture2D> ();
					}

                    Texture2D tex = ci.alphaMasks[slot];
                    if(tex == null)
                    {
                        continue;
                    }

                    //subAlphaTextures [slot].Add (ci.alphaMasks [slot],ci.alphaMasks [slot]);

                    subAlphaTextures[slot][tex] = tex;
				}
			}

		}



		/// <summary>
		/// The figure visibility change handler. Called when the figure changes visibility
		/// </summary>
		/// <param name="figure">The CostumeItem in which a change has happened.</param>
		public void OnFigureVisibilityChange (CostumeItem figure)
		{
			// Debug.Log("ALPHA INJECTION RECIEVED FIGURE VIS CHANGE");
			//updateFigureTexture ();
			//updateClothingTextures ();
			invalidate ();
		}
		


		/// <summary>
		/// The clothing visibility change handler. Called when any costume item changes visibility.
		/// </summary>
		/// <param name="clothing_model">The CostumeModel in which a change has happened.</param>
		public void OnClothingVisibilityChange (CostumeModel clothing_model)
		{
			// Debug.Log("ALPHA INJECTION RECIEVED CLOTHING VIS CHANGE");
			//updateFigureTexture ();
			//updateClothingTextures ();
			invalidate ();
		}



		/// <summary>
		/// Invalidate the AlphaInjectionManager, and start processing all textures.
		/// This should set a timeout to give a few other calls a chance to adds their changes to the list so we redraw minimally.
		/// </summary>
		public void invalidate ()
		{
            dirty = true;
		}

        /// <summary>
        /// Force our figure to update the texture now if it needs to, this is normally called by CharacterManager.LateUpdate
        /// </summary>
        public void Process()
        {
            //if we need to re-render the alpha injection mask do so now
            if (dirty)
            {
				//if (Application.isPlaying && figure != null && clothingModel != null) {
				if (figure != null && clothingModel != null) {
                    updateClothingTextures ();
                    cleanup();
					drawTexture (); 
				}
            }
        }


		/// <summary> 
		/// Draws the texture after a delay triggered by the invalidate function
		/// </summary>
		private void drawTexture ()
		{
            dirty = false;
            //Back in 1.0 we only had one material we cared about which was the entire body which was ALWAYS at slot0, now we need to care about HEAD and BODY and order is not guaranteed so we now use a lookup

            //Debug.Log ("REDRAWING ALPHA INJECTION: " + Time.frameCount + " | " + figure.gameObject.transform.parent.name );

            //Material[] materials = figure.GetSkinnedMeshRenderer().sharedMaterials;

            Material[] materials = GetMaterialsClone();
            SkinnedMeshRenderer smr = figure.GetSkinnedMeshRenderer();

            for (int i = 0; i < materials.Length; i++)
            {

                if (materials[i] == null || !materials[i].HasProperty(MaterialConstants.AlphaTexPropID))
                {
                    continue;
                }

                MATERIAL_SLOT slot = MATERIAL_SLOT.UNKNOWN;

                Texture2D[] masks = null;
                //TODO this is a TERRIBLE way of checking, we need to build a map
                if (materials[i].name.ToLower().Contains("head"))
                {
                    slot = MATERIAL_SLOT.HEAD;
                } else if (materials[i].name.ToLower().Contains("body"))
                {
                    slot = MATERIAL_SLOT.BODY;

                } else if (materials[i].name.ToLower().Contains("genesis2"))
                {
                    slot = MATERIAL_SLOT.BODY;
                }

                if(slot == MATERIAL_SLOT.UNKNOWN)
                {
                    //do nothing, we're not sure what slot to process
                    continue;
                }

                if (subAlphaTextures.ContainsKey(slot))
                {
                    masks = new Texture2D[subAlphaTextures[slot].Count];
                    subAlphaTextures[slot].Values.CopyTo(masks, 0);
                }

                RenderTexture rt = null;
                Texture2D tex = null;

                if (masks != null && masks.Length>0)
                {
                    switch (textureMode)
                    {
                        case TEXTURE_MODE.FAST:
                            tex = new Texture2D(1024, 1024, TextureFormat.ARGB32, true);
                            TextureUtilities.OverlayArrayOfTexturesGPU(ref tex, masks);

                            //uncomment if you want to debug the masks
                            //TextureUtilities.OverlayArrayOfTexturesGPU(ref tex, masks,"Unlit/AlphaCombiner",true);
                            temporaryTexture2D.Add(tex);
                            materials[i].SetTexture(MaterialConstants.AlphaTexPropID, tex);

                            break;
                        case TEXTURE_MODE.FASTEST:
                            rt = TextureUtilities.OverlayArrayOfTexturesGPU(masks);
                            //rt = TextureUtilities.OverlayArrayOfTexturesGPU(masks, "Unlit/AlphaCombiner", true);
                            temporaryRenderTextures[i] = rt;
                            //UnityEngine.Debug.Log("Assigning rt: " + smr.name + " | " + slot);
                            materials[i].SetTexture(MaterialConstants.AlphaTexPropID, rt);
                            break;
                    }
                } else
                {
                    //no textures, clear it
                    materials[i].SetTexture(MaterialConstants.AlphaTexPropID, null);
                }

                /*
                RenderTexture rtOld = materials[i].GetTexture(MaterialIDs.AlphaTexMatID) as RenderTexture;
                if(rtOld != null)
                {
                    RenderTexture.Destroy(rtOld);
                }
                */

                //UnityEngine.Debug.Log("Drawing Alpha: " + materials[i].name + " | " + materials.Length + " | " + (masks != null ? masks.Length.ToString() : "null") + " | " + slot + " | " + (tex != null ? "Tex is not null" : "tex is null"));
                //install the texture
                //materials[i].SetTexture(MaterialIDs.AlphaTexMatID, tex);
            }

            if (Application.isPlaying)
            {
                smr.materials = materials;
            }
            else
            {
                smr.sharedMaterials = materials;
            }
        }



		/// <summary>
		/// Sets the figure and costume model.
		/// </summary>
		/// <param name="body">Body.</param>
		/// <param name="costume_model">Costume model.</param>
		public void SetFigureAndCostumeModel (CostumeItem body, CostumeModel costume_model)
		{
			figure = body;
			clothingModel = costume_model;
			figure.OnCostumeItemLODDidChange += this.OnFigureVisibilityChange;
			clothingModel.OnItemVisibilityDidChange += this.OnClothingVisibilityChange;

			updateClothingTextures();
			invalidate();
		}
		


//		public void AddAndActivateMask (Texture2D new_mask)
//		{
//			if (injection_list == null)
//				injection_list = new List<Texture2D> ();
//			if (new_mask != null && injection_list.Contains (new_mask) == false) {
//				injection_list.Add (new_mask);
//				invalidate ();
//			}
//		}



//		public void RemoveAndDeactivateMask (Texture2D new_mask)
//		{
//			if (injection_list.Contains (new_mask) == true) {
//				injection_list.Remove (new_mask);
//				invalidate ();
//			}
//		}

				

	}
}
