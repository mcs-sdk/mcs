using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine;

using MCS.CONSTANTS;
using MCS.SERVICES;
using MCS.CORESERVICES;
using MCS_Utilities.Morph;

namespace MCS.FOUNDATIONS
{

    /// <summary>
    /// <para>
    ///     Manages blendshape states for a mesh or figure including Attaching and Detatching morphs on a figure
    ///     as well as CostumeItems attached to that figure.
    ///     Through the CoreMorphs class developers can gain access to
    ///     individual morphs on the figure. For high level maniupulations such as setting the value of a morph it may be easier to use
    ///     functionality exposed by <see cref="MCSCharacterManager"/> 
    /// </para>
    /// <para>
    ///     CoreMorphs is a component class and a reference to it is held in the 
    ///     <see cref="MCSCharacterManager"/>. To get a reference to the CoreMorphs
    ///     object of an MCSFigure use the <see cref="MCSCharacterManager.coreMorphs"/> property.
    /// </para>
    /// </summary>
    [ExecuteInEditMode]
    [System.Serializable]
    public class CoreMorphs : MonoBehaviour, ISerializationCallbackReceiver
    {

        /// <summary>
        /// Delegate for subscribing to OnPreMorph
        /// </summary>
        public delegate void PreMorph();
        /// <summary>
        /// Fired right before a morph
        /// </summary>
        public event PreMorph OnPreMorph;
        
        /// <summary>
        /// Delegate for subscribing to OnPostMorph
        /// </summary>
        public delegate void PostMorph();
        /// <summary>
        /// Fired right after a morph
        /// </summary>
        public event PostMorph OnPostMorph;

        /// <summary>
        /// <para>
        ///     The list of all morphs available/attached/etc to a mesh, here for serialization reasons 
        ///     because Unity does not serialize dictionaries.
        /// </para>
        /// <para>
        ///     For faster access See also: <see cref="morphLookup"/> which is a dictionary and will provide faster lookup.
        /// </para>
        /// </summary>
        public List<Morph> morphs = new List<Morph>();
        /// <summary>
        /// <para>
        ///     Root GameObject for this CoreMorphs object. The Root [should] have SkinnedMeshRenderer components on One or more children.
        ///     When Morphs are Attached or Detatched they are Attached or Detatched based on the SkinnedMeshRenderer components in the
        ///     children of rootObject.
        /// </para>
        /// <para>
        ///     Should be set using <see cref="SetRootObject(GameObject, bool)"/> 
        /// </para>
        /// </summary>
        public GameObject rootObject;

        /// <summary>
        /// <para>
        ///     Maps our list of morphs to their localName which can be found in the <see cref="MCSCharacterManager"/> for quick lookups.
        ///     morphLookup is not related to morphGroups and is provided as a convenient way to simply find a morph by name regardless of it's group.
        /// </para>
        /// <para>
        ///     The references in morphLookup are the same as in <see cref="morphs"/>, 
        ///     so they can be modified with immediate results without having to update both sources. 
        /// </para>
        /// </summary>
        public Dictionary<string, Morph> morphLookup = new Dictionary<string, Morph>();
        private StreamingMorphs _streamingMorphs = null;
        private StreamingMorphs streamingMorphs
        {
            get
            {
                if(_streamingMorphs == null)
                {
                    _streamingMorphs = new StreamingMorphs();
                    StreamingMorphs.LoadMainThreadAssetPaths();
                }
                return _streamingMorphs;
            }
        }

        /// <summary>
        /// <para>
        ///     Tracks which morphs are in which groups, there are 3 prebuilt groups which are: All, Attached, and Available; see <see cref="BuildDefaultStateMorphGroups"/>.
        ///     morphStateGroups is provided to end users for read purpose only. To move morphs between groups see
        ///     <list type=">">
        ///     <item><see cref="AttachMorphs(Morph[], bool, bool, StreamingMorphs.OnPostInjectionMorphs)"/></item>
        ///     <item><see cref="AttachMorphs(string[], bool, bool, StreamingMorphs.OnPostInjectionMorphs)"/></item>
        ///     <item><see cref="DetachAllMorphs"/></item>
        ///     </list> 
        /// </para>
        /// <para>
        ///     Key will either be "All", "Attached" or "Available", Value will be a list of <see cref="Morph"/> objects in the specified group.
        /// </para>
        /// <list type=">">
        ///     <item>"All" contains a list of ALL morphs that can be streamed in from the StreamingAssets folder <see cref="StreamingMorphs"/>.</item>
        ///     <item>"Attached" contains a list of every morph that has been attached to a figure (currently loaded into memory).</item>
        ///     <item>"Available" contains a list of every morph that is available to be streamed in but has not yet been attached.</item>
        /// </list>
        /// </summary>
        public Dictionary<string,List<Morph>> morphStateGroups = new Dictionary<string,List<Morph>>();

        public void Awake()
        {
            this.hideFlags = HideFlags.HideInInspector;
        }

        /// <summary>
        /// Sets the root GameObject for this CoreMorphs object. See <see cref="rootObject"/> 
        /// </summary>
        /// <param name="root">The game object to be the new rootObject</param>
        /// <param name="refresh">True if the list of morphs should be refreshed after setting.</param>
        public void SetRootObject(GameObject root, bool refresh=false)
        {
            //store a copy of our root object for later use (such as when we're injecting)
            rootObject = root;
            string key = root.name;

            //hack
            if (key.IndexOf(".") == -1)
            {
                //swap the key for the first lod0 mesh name
                key = GetMeshKeyFromFigure(root);
            }

            //TODO: should we be injected alongside CIBody?

            //lookup available morphs if the current list is empty
            if (refresh || morphs.Count == 0 || morphLookup.Count == 0 || morphStateGroups.Count == 0)
            {
                RefreshMorphsFromMeshKey(key);
            }
        }

        /// <summary>
        /// Refreshes the list of morphs availabe, installed, etc, this does NOT inject missing morphs into submeshes
        /// </summary>
        public void Refresh()
        {
			string key = GetMeshKeyFromFigure(rootObject);
            
            RefreshMorphsFromMeshKey(key);
        }

        /// <summary>
        /// Syncs all blendshapes with children, should be called after clothing/props/hair/etc changes
        /// </summary>
        /// <param name="syncRoot">Optional Root GameObjects with which to sync. If null uses <see cref="rootObject"/></param>
        public void Resync(List<GameObject> syncRoot = null)
        {
            //TODO: implement
            //Debug.LogWarning("CoreMorphs.Resync is not implemented yet");

            //reattaches and sets values for items it needs to
            Morph[] attached = morphStateGroups["Attached"].ToArray();
            AttachMorphs(attached);
            SyncMorphValues(attached,syncRoot);
            //UnityEngine.Debug.Log("CoreMorphs->Resync");
        }

        /// <summary>
        /// Syncs all blendshapes with children with a single GameObject as its syncRoot.
        /// See <see cref="Resync(List{GameObject})"/> 
        /// </summary>
        /// <param name="syncRoot">Optional Root GameObject with which to sync. If null uses <see cref="rootObject"/></param>
        public void Resync(GameObject syncRoot)
        {
            List<GameObject> objs = new List<GameObject>() { syncRoot };
            Resync(objs);
        }

        private void BuildDefaultStateMorphGroups()
        {
            morphStateGroups["All"] = new List<Morph>(); //every morph that could be used whether or not it's injected
            morphStateGroups["Attached"] = new List<Morph>(); //only morphs injected into the mesh
            morphStateGroups["Available"] = new List<Morph>(); //morphs that could be injected but are not
        }

        /// <summary>
        /// <para>
        /// Loads <see cref="Morph"/>s  from a given key. This is done when the rootObject is initially set or when
        /// this CoreMorphs object is reset.
        /// </para>
        /// <para>
        /// Loading Morphs for CoreMorphs is handled by the <see cref="MCSCharacterManager"/> when it sets the
        /// <see cref="rootObject"/> for it's CoreMorphs object. Under normal circumstances a 3rd party developer 
        /// would not need to use this method.
        /// </para>
        /// </summary>
        /// <param name="key">Should be the mesh.name, eg: Genesis2Female.Shape_LOD0</param>
        public void RefreshMorphsFromMeshKey(string key)
        {
            MorphManifest manifest = streamingMorphs.GetManifest(key, Application.isPlaying);

            //purge the old ones
            morphs.Clear();
            morphLookup.Clear();

            //dump the old groups
            morphStateGroups.Clear();
            BuildDefaultStateMorphGroups();

            foreach (string name in manifest.names)
            {
                Morph m = new Morph(name, 0f, false, false);

#if !NIKEI_ENABLED
                if (m.name.ToLower().Contains("nikei"))
                {
                    //Skip nikei
                    continue;
                }
#endif

                morphs.Add(m);
                morphLookup.Add(m.localName, m);

                //add this morph to the All group
                morphStateGroups["All"].Add(m);
            }

            //by default always sort the root morphs group alphabetically
            //SortMorphs(morphs);
        }

        /// <summary>
        /// True if <see cref="morphs"/> is sorted.
        /// </summary>
        public bool isSorted 
        {
            get;
            protected set;
        }

        /// <summary>
        /// Sorts any list of moprhs in sort order (determined by environment culture) by their display name.
        /// </summary>
        /// <param name="morphs">The List of morphs to be sorted.</param>
        /// <remarks>
        /// Side-effect that isSorted gets set to true regardless of the list passed in..
        /// Is this intentional?
        /// </remarks>
        public void SortMorphs(List<Morph> morphs)
        {
            morphs.Sort(delegate (Morph a, Morph b) {
                return string.Compare(a.displayName,b.displayName, true);
            });

            isSorted = true;
        }

        /// <summary>
        /// Sorts <see cref="morphs"/> in sort order based on the environments culture if it is not already sorted.
        /// </summary>
        public void SortIfNeeded()
        {
            if (isSorted)
            {
                return;
            }
            SortMorphs(morphs);
        }


        /// <summary>
        /// <para>
        /// Attach multipe <see cref="Morph"/>s from a list of morph names into the current <see cref="rootObject"/> and all it's children. This means
        /// if Hair, Clothing, Etc. are attached they will have the morphs attached as well.
        /// </para>
        /// <para>
        /// Method can be done Async wiht optional CallBack.
        /// </para>
        /// </summary>
        /// <param name="morphNames">localNames of the morphs to attach </param>
        public void AttachMorphs(string[] morphNames,bool refresh=false, bool async=false, StreamingMorphs.OnPostInjectionMorphs callback =null)
        {
            StreamingMorphs.LoadMainThreadAssetPaths();
            //do nothing if we're empty
            if (morphNames.Length <= 0)
            {
                return;
            }

            List<string> keepNames = new List<string>();

            //we need to update the morph groups now...
            foreach (string morphName in morphNames)
            {
                Morph m;
                if (morphLookup.TryGetValue(morphName,out m))
                {
                    //add this morph to the Attached list if not already in there
                    if (!morphStateGroups["Attached"].Contains(m))
                    {
                        morphStateGroups["Attached"].Add(m);
                        keepNames.Add(morphName);
                    }

                    //remove this one from available since we're installing it
                    morphStateGroups["Available"].Remove(m); //will return false, not throw, if the kye isn't found

                    m.attached = true;
                }
            }

            if (!async)
            {
                //tell our streamingblendshapes object to add the blendshape into our mesh(es), this is recursive (so things like hair/clothes/etc will get the morph as well as the base figure)
                streamingMorphs.InjectMorphNamesIntoFigure(rootObject, (refresh ? morphNames : keepNames.ToArray()), true);
            } else
            {

                streamingMorphs.InjectMorphNamesIntoFigureAsync(rootObject, (refresh ? morphNames : keepNames.ToArray()), true,callback);
            }
        }

        /// <summary>
        /// Dettaches all morphs from the current <see cref="rootObject"/> and sets all values to zero.
        /// </summary>
        public void DetachAllMorphs()
        {
            streamingMorphs.RemoveAllBlendshapesFromFigure(rootObject);
            Morph[] morphsAttached = new Morph[morphStateGroups["Attached"].Count];
            Array.Copy(morphStateGroups["Attached"].ToArray(), morphsAttached, morphStateGroups["Attached"].Count);

            morphStateGroups["Attached"].Clear();
            for (int i = 0; i < morphsAttached.Length; i++)
            {
                morphsAttached[i].attached = false;
                morphsAttached[i].value = 0;
                //Debug.Log("Detaching: " + morphsAttached[i].localName);
                morphStateGroups["Available"].Add(morphsAttached[i]);
                morphStateGroups["Attached"].Remove(morphsAttached[i]);
            }
            
        }


        /// <summary>
        /// <para>
        /// Attach multipe <see cref="Morph"/>s into the current <see cref="rootObject"/> and all it's children. This means
        /// if Hair, Clothing, Etc. are attached they will have the morphs attached as well.
        /// </para>
        /// <para>
        /// Method can be done Async wiht optional CallBack.
        /// </para>
        /// </summary>
        /// <param name="morphs"><see cref="Morph"/>s to be attached. </param>
        public void AttachMorphs(Morph[] morphs,bool refresh=false, bool async=false, StreamingMorphs.OnPostInjectionMorphs callback =null)
        {
            if (morphs.Length <= 0)
            {
                return;
            }
            string[] morphNames = new string[morphs.Length];
            for(int i = 0; i < morphs.Length; i++)
            {
                Morph m = morphs[i];
                morphNames[i] = m.localName;
            }
            AttachMorphs(morphNames,refresh, async,callback);
        }

        /// <summary>
        /// <para>
        /// Detatches the given <see cref="Morph"/>s from the current <see cref="rootObject"/>
        /// and all it's children and sets their values to zero.
        /// </para>
        /// <para>
        /// Note this has to re-create the list on the mesh in unity, this is expensive and should not be done frequently.
        /// </para>
        /// </summary>
        public void DettachMorphs(Morph[] morphs)
        {
            if (morphs.Length <= 0)
            {
                return;
            }
            string[] morphNames = new string[morphs.Length];
            for (int i = 0; i < morphs.Length; i++)
            {
                Morph m = morphs[i];
                morphNames[i] = m.localName;
            }
            DettachMorphs(morphNames);
        }

        /// <summary>
        /// Sets the value of the model of every morph in the array to zero
        /// </summary>
        /// <param name="morphNames"></param>
        private void SetDettachedMorphsToZero(string[] morphNames)
        {
            for (int j = 0; j < morphNames.Length; j++)
            {
                Morph m;
                if (morphLookup.TryGetValue(morphNames[j], out m))
                {
                    m.value = 0;
                }
            }
        }

        /// <summary>
        /// <para>
        /// Uses the localNames given to detatch the corresponding <see cref="Morph"/>s from the current <see cref="rootObject"/>
        /// and all it's children and sets their values to zero.
        /// </para>
        /// <para>
        /// Note this has to re-create the list on the mesh in unity, this is expensive and should not be done frequently.
        /// </para>
        /// </summary>
        public void DettachMorphs(string[] morphNames)
        {
            //do nothing if we're empty
            if(morphNames.Length <= 0)
            {
                return;
            }

            SetDettachedMorphsToZero(morphNames);

            //we need to update the morph groups now...
            foreach (string morphName in morphNames)
            {
                Morph m;
                if (morphLookup.TryGetValue(morphName, out m))
                {
                    //add this morph to the Attached list if not already in there
                    if (!morphStateGroups["Available"].Contains(m))
                    {
                        morphStateGroups["Available"].Add(m);
                    }
                    morphStateGroups["Attached"].Remove(m); //will return false, not throw, if the kye isn't found

                    m.attached = false;
                }
            }

            //tell our streamingblendshapes object to add the blendshape into our mesh(es), this is recursive (so things like hair/clothes/etc will get the morph as well as the base figure)
            streamingMorphs.RemoveAllBlendshapesFromFigure(rootObject);

            return;

            //add back in the ones we want installed
            if (morphStateGroups["Attached"].Count() > 0) {
                string[] installedNames = new string[morphStateGroups["Attached"].Count()];
                int i = 0;
                foreach (Morph m in morphStateGroups["Attached"])
                {
                    installedNames[i++] = m.localName;
                }
                streamingMorphs.InjectMorphNamesIntoFigure(rootObject, installedNames, true);

                SyncMorphValues(morphStateGroups["Attached"].ToArray());
            }
        }

        /// <summary>
        /// <para>
        /// Recursively sets all morph values in the mesh to match the values in subMorphs.
        /// </para>
        /// <para>
        /// Syncs morphs to the SkinnedMeshRenderers <see cref="rootObject"/> if optional
        /// syncRoot parameter is not included.
        /// </para>
        /// </summary>
        /// <param name="subMorphs"></param>
        public void SyncMorphValues(Morph[] subMorphs, List<GameObject> syncRoot = null)
        {
            //GameObject tmpRoot = syncRoot == null ? rootObject : syncRoot;
            //UnityEngine.Debug.Log("SyncMorphValues: " + subMorphs.Length);
            //do nothing if we're empty
            if (subMorphs.Length <= 0)
            {
                return;
            }

            if(OnPreMorph != null)
            {
                OnPreMorph();
            }

            SkinnedMeshRenderer[] smrs;

            if(syncRoot == null)
            {
                smrs = rootObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            } else
            {
                List<SkinnedMeshRenderer> tmpSMRS = new List<SkinnedMeshRenderer>();
                foreach(GameObject tmpRoot in syncRoot)
                {
                    //UnityEngine.Debug.LogWarning("tmpRoot: " + tmpRoot.name);
                    SkinnedMeshRenderer[] tmp = tmpRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
                    for(int i = 0; i < tmp.Length; i++)
                    {
                        //UnityEngine.Debug.LogWarning("add: " + tmp[i].name);
                        tmpSMRS.Add(tmp[i]);
                    }
                }

                smrs = tmpSMRS.ToArray();
            }


            foreach (SkinnedMeshRenderer smr in smrs)
            {
                if (!smr.enabled)
                {
                    continue;
                }

                if (smr.sharedMesh == null)
                {
                    Debug.LogWarning("SMR: " + smr.name + " does not have a sharedMesh, skipping.");
                    continue;
                }

                //UnityEngine.Debug.Log("SMR: " + smr.name + " morph count: " + subMorphs.Length);

                foreach (Morph m in subMorphs)
                {
                    Morph morphBase;
                    if (morphLookup.TryGetValue(m.localName, out morphBase))
                    {
                        //update the value in our main list
                        morphBase.value = m.value;
                    }
                    else
                    {
                        Debug.LogWarning("Unknown morph: " + m.localName);
                        continue;
                    }

                    //TODO: we should cache this
                    int index = smr.sharedMesh.GetBlendShapeIndex(m.localName);

                    if (index < 0)
                    {
                        //can we inject it now, because technically we expected it to be here.
                        string[] tmpList = { m.localName };
                        streamingMorphs.InjectMorphNamesIntoFigure(rootObject, tmpList, true);

                        index = smr.sharedMesh.GetBlendShapeIndex(m.localName);
                        if (index < 0)
                        {
                            //well we still couldn't install it, abort
                            if (!streamingMorphs.silentFailureWhenMissingMorphFile)
                            {
                                Debug.LogWarning("Unable to locate/inject blendshape: " + m.localName + " on " + smr.name);
                            }
                            continue;
                        }
                    }
                    smr.SetBlendShapeWeight(index, m.value);
                }
            }


            if(OnPostMorph != null)
            {
                OnPostMorph();
            }
        }

        //TODO: this is a temporary hack until we have proper meta
		//This is just to get the path to the manifest file for LOD0 so we can figure out which morphs are available to the figure, nothing more
		protected string GetMeshKeyFromFigure(GameObject obj)
        {

			CoreMeshMetaData cmmd = obj.GetComponentInChildren<CoreMeshMetaData> ();
			if (cmmd != null) {
				string key = cmmd.geometryId;
				return key;
			}

			/*
			//we need to find a gameobject that includes the figure and use that runtime path
			CoreMesh[] cms = obj.GetComponentsInChildren<CoreMesh>();
			foreach (CoreMesh cm in cms) {
				if (cm.meshType == MESH_TYPE.BODY && cm.runtimeMorphPath.Length > 0) {
					return cm.runtimeMorphPath;
				}
			}

			//if that fails, try to get it from the core mesh meta data, note the names may not always match....

			CoreMeshMetaData cmmd = obj.GetComponentInChildren<CoreMeshMetaData> ();
			if (cmmd != null) {
				string key = cmmd.geometryId + "/" + cmmd.geometryId + "/" + cmmd.geometryId + "_LOD0";
				return key;
			}
			*/

			/*

			//coremesh isn't attached to the root figure, it is to children though...
			CSBoneService boneService = obj.GetComponent<CSBoneService>();
			if (boneService != null) {
				CoreMesh[] coreMeshes =boneService.gameObject.GetComponentsInChildren<CoreMesh> ();
				if(coreMeshes.Count > 0){


			}

			CoreMesh cm = obj.GetComponent<CoreMesh> ();

			if(cm == null || cm.runtimeMorphPath.Length <= 0){
				//can we attempt to infer it?
				throw new Exception("Unknown figure data");
			}

			return cm.runtimeMorphPath;

			*/

			throw new Exception ("Unknown Figure");
        }

#region serialization helpers
        
        //pack anything up for serialization if we need
        public void OnBeforeSerialize()
        {

        }
        

        //regenerate all the fields we couldn't deserialize automatically
        public void OnAfterDeserialize()
        {
            BuildDefaultStateMorphGroups();

            //unity doesn't serialize dicts, so this rebuilds our groups
            for (int i=0;i<morphs.Count;i++)
            {
                Morph m = morphs[i];
                morphLookup[m.localName] = m;

                morphStateGroups["All"].Add(m);

                if (m.attached)
                {
                    morphStateGroups["Attached"].Add(m);
                }
                else
                {
                    morphStateGroups["Available"].Add(m);
                }

            }
        }
#endregion
    }

#region structure definitions

    /// <summary>
    /// <para>
    /// In the MCS ecosystem the abstract concept of a morph is anything that can change the appearance of the figure.
    /// A morph could, for example, change the mesh of the figure to make it heavy, tall, short etc.
    /// Another morph could change the texture or material on an MCS Figure. An MCS figure has a collection of morphs that
    /// a developer can take advantage of to customize the appearance of the figure.
    /// </para>
    /// <para>
    /// The <see cref="Morph"/> class contains state information about a morph including it's value, status and name[s].
    /// The usefulness of a <see cref="Morph"/> object to a developer is in the information it contains. 
    /// </para>
    /// <para>
    /// A developer can gain access to the <see cref="Morph"/>s on a figure through the <see cref="MCSCharacterManager"/> and the
    /// <see cref="CoreMorphs"/> class (see <see cref="MCSCharacterManager.coreMorphs"/>, <see cref="CoreMorphs.morphLookup"/> and
    /// <see cref="CoreMorphs.morphs"/>. With a reference to a <see cref="Morph"/> a developer can examine the state of the <see cref="Morph"/>
    /// object. However, modifying the properties in the <see cref="Morph"/> object will not have an immediate effect.
    /// To set the value of a particular <see cref="Morph"/> use <see cref="MCSCharacterManager.SetBlendshapeValue(string, float)"/> 
    /// </para>
    /// <para>
    /// This is the morph variable you use as a developer, this is not the value stored on disk, see <see cref="CONTENTLIBRARY.MCSResourceManager"/> for the file format specification.
    /// </para>
    /// </summary>
    [System.Serializable]
    public class Morph
    {
        /// <summary>
        /// The original, unmodified morph name (eg: Genesis2Female_FBMHeavy).
        /// Generally this is not used.
        /// </summary>
        public string name = ""; //The is the original unmodified morph name (eg: Genesis2Female__FBMHeavy), generally don't use this
        /// <summary>
        /// This is the modified morph name specific to the mesh (eg: FBMHeavy), generally we use this to key off of.
        /// See <see cref="CoreMorphs.morphLookup"/>  
        /// </summary>
        public string localName = ""; //This is the modified moprh name specific to the mesh (eg: FBMHeavy), genearlly use this (we key off this one)
        /// <summary>
        /// This is the modified human readable name appropriate for use in a GUI (eg: Heavy).
        /// </summary>
        public string displayName = ""; //This is the modified human readable name (eg: Heavy), this should be used for human readable UIs
        /// <summary>
        /// The current value of the morph.
        /// </summary>
        public float value = 0f; //the current value of the morph
        /// <summary>
        /// True of the morph has been injected into the mesh.
        /// Should be considered READONLY by devlopers.
        /// To obtain a list of all attached Morphs on a figure see <see cref="CoreMorphs.morphStateGroups"/>["Attatched"] 
        /// </summary>
        public bool attached = false; //has the morph been injected into the mesh, this should be considered READONLY by users, the coreMorphs.morphGroups["Attached"] is the "real list" TODO: this should be a setterable variable that trips a resync with coreMorphs, but should we have a reference to coreMorph?
        /// <summary>
        /// Does this morph require the <see cref="JCTTransition"/> (Joint Center Translation) system to handle it.
        /// Generally morphs that significantly alter the shape of the figure require JCT.
        /// </summary>
        public bool hasJCT = false; //does this morph require the JCT system to handle it (as in, it modifies bones)
        /// <summary>
        /// True if this <see cref="Morph"/> supports negative <see cref="value"/>s. 
        /// </summary>
        public bool hasNegative = false;
        public string[] morphTypeGroups;

        /// <summary>
        /// If this <see cref="Morph"/> requires <see cref="JCTTransition"/> service (see <see cref="hasJCT"/>) this
        /// list will be populated with the specfic bones it affects.
        /// </summary>
        public List<JCT> jcts = new List<JCT>(); //if hasJCT is true, this list will be populated with the specific bones it affects

        /// <summary>
        /// Constructs an empty Morph where all properties
        /// are set to their default values.
        /// </summary>
        public Morph()
        {
        }

        /// <summary>
        /// Constructs a new <see cref="Morph"/> object
        /// and sets the Properties to the given values.
        /// </summary>
        public Morph(string newName,float newValue,bool newAttached,bool newHasJCT,string newDisplayName="",List<JCT> newJCTs = null)
        {
            name = newName;
            displayName = newDisplayName;
            localName = newName;
            value = newValue;
            attached = newAttached;
            hasJCT = newHasJCT;
            jcts = (newJCTs != null ? newJCTs : null);

            localName = ConvertNameToLocalName(newName);
            displayName = ConvertKeyToDisplayName(newName);
        }

        private string ConvertNameToLocalName(string name)
        {
            string tmp = name;
            if (name.Length == 0)
            {
                return name;
            }

            int underScoreIndex = name.IndexOf("__");
            if (underScoreIndex >= 0)
            {
                tmp = name.Substring(underScoreIndex + 2);
            }
            return tmp;
        }

        private string ConvertKeyToDisplayName(string key)
        {

            if (key.Length == 0)
            {
                return key;
            }

            string displayName = key;
            if (displayName.Length > 3)
            {
                string tripple = displayName.Substring(0, 3);
                if (tripple.Equals(tripple.ToUpper()) && !tripple.Equals("NEG"))
                {
                    displayName = displayName.Substring(3).TrimStart('_');
                }
            }

            //does this name have a 3 letter prefix, eg: FBM or VSM, if so remove it, TODO: this is prob overkill for regex, maybe there is a c# string method for handling this...
            //var reg = new Regex(@"[A-Z]{3}", RegexOptions.IgnorePatternWhitespace);

            //displayName = reg.Replace(displayName, "");

            return displayName;
        }
    }

    /// <summary>
    /// Holds data for a single bone requiring <see cref="JCTTransition"/> service. 
    /// </summary>
    [System.Serializable]
    public struct JCT
    {
        /// <summary>
        /// The name of the bone, eg: hip, rThing, etc.
        /// </summary>
        public string boneName; //The name of a bone, eg: hip, rThigh, etc
        /// <summary>
        /// World position of the bone.
        /// </summary>
        Vector3 worldPosiiton;
        /// <summary>
        /// The position of the bone relative to it's parent.
        /// </summary>
        Vector3 localPosition;

        /*
        public float worldX;    //The position of the bone in world coordinates (where bottom center is 0,0,0)
        public float worldY;
        public float worldZ;
        public float localX;    //The position of the bone relative to it's parent
        public float localY;
        public float localZ;
        */
    }

#endregion
}
