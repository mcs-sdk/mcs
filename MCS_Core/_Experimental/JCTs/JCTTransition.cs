using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using MCS.CONSTANTS;
using MCS.FOUNDATIONS;
using MCS;
using MCS.COSTUMING;
using MCS.CORESERVICES;

/// <summary>
/// JCTTransition MonoBehaviour Component class.
/// </summary>
[ExecuteInEditMode]
public class JCTTransition : MonoBehaviour
{
    public delegate void PostJCT();
    public event PostJCT OnPostJCT;
    public delegate void PreJCT();
    public event PostJCT OnPreJCT;
    public delegate void BindPoseJCT();
    public event BindPoseJCT OnBindPoseJCT;

    public bool EnableOnlyDuringPlay = true;

    /// <summary>
    /// Array of type Morph. Set internally during import.
    /// </summary>
    public JCTMorph[] m_morphs;



	/// <summary>
	/// Base BindPose Matrix array. Set internally during import.
	/// </summary>
	public Matrix4x4[] base_bind_poses;



	/// <summary>
	/// Current BindPose Matrix array.
	/// </summary>
	public Matrix4x4[] current_bind_poses;



	/// <summary>
	/// Base Bone Position array. Set internally during import.
	/// </summary>
	public Vector3[]   base_bone_positions;



	/// <summary>
	/// Internal. Current Bone Position array. Temp cache for runtime - bone tranform totals are collected here.
	/// </summary>
	private Vector3[]   current_bone_positions;



	/// <summary>
	/// Internal. The temp runtime cahce - collect difference between
	/// </summary>
	private Vector3[]   temp_bone_position_deltas;



	/// <summary>
	/// Internal. The SkinnedMeshRenderer associated with this GameObject
	/// </summary>
	private SkinnedMeshRenderer m_meshRender;


	/// <summary>
	/// Internal. Temporary cache for bone Transforms.
	/// </summary>
	private Transform[] temp_bones;


	/// <summary>
	/// Internal. Boolean flag do JCTs need updating?
	/// </summary>
	private bool JCTsNeedUpdate = true;

    /// <summary>
    /// Has the sharedMesh been cloned, it's only needed if we actually drive a jct on the figure
    /// </summary>
    protected bool m_cloned = false;
    protected Dictionary<int, bool> m_cloned_lookup = new Dictionary<int, bool>();
    protected Mesh m_originalMesh;

	/// <summary>
	/// Internal. Our reference to the main Character Manager.
	/// </summary>
	private MCSCharacterManager charman;

    /// <summary>
    /// Internal cache lookup
    /// </summary>
    private Dictionary<string, int> _meshRenderBoneDict;
    private Dictionary<string, int> meshRenderBoneDict
    {
        get
        {
            if(_meshRenderBoneDict == null)
            {
                _meshRenderBoneDict = new Dictionary<string, int>();

                Transform[] mrBones = MeshRendererBones;

                for (int j = 0; j < mrBones.Length; j++)
                {
                    _meshRenderBoneDict.Add(mrBones[j].name, j);
                }
            }
            return _meshRenderBoneDict;
        }
    }



    /// <summary>
    /// Internal. Dictionary of morph name to blendshape value. 
    /// </summary>
    private Dictionary<string, float> _morph_to_BS_float = new Dictionary<string, float>();
	private Dictionary<string, float> morph_to_BS_float
	{
		get {
			if (_morph_to_BS_float == null) {
                buildMorphDictionaries();
			}
			return _morph_to_BS_float;
		}
	}

    


	/// <summary>
	/// Internal. Dictionary of morph name to blendshape index. 
	/// </summary>
	private Dictionary<string, int> _morph_to_BS_index = new Dictionary<string, int>();
	private Dictionary<string, int> morph_to_BS_index
	{
		get {
			if (_morph_to_BS_index == null) {
                buildMorphDictionaries();
            }
			return _morph_to_BS_index;
		}
	}

    protected Dictionary<string, int> jct_morph_dictionary;
    private void buildJCTMorphDictionary()
    {
        jct_morph_dictionary = new Dictionary<string, int>(m_morphs.Length);
        for (int i = 0; i < m_morphs.Length; i++)
        {
            jct_morph_dictionary[m_morphs[i].m_name] = i;
        }
    }

    private void buildMorphDictionaries()
    {
        _morph_to_BS_index.Clear();// = new Dictionary<string, int>();
        _morph_to_BS_float.Clear();// = new Dictionary<string, float>();

        if(MeshRender == null || MeshRender.sharedMesh == null)
        {
            return;
        }

        for (int index = MeshRender.sharedMesh.blendShapeCount - 1; index >= 0; index--)
        {
            string name = MeshRender.sharedMesh.GetBlendShapeName(index);
            name = name.Substring(name.IndexOf("__") + 2);
            _morph_to_BS_index[name] = index;
            _morph_to_BS_float[name] = 0f;
        }
    }

    public bool SetMorphValue(string key, float val){
		int index = 0;

		if (jct_morph_dictionary == null) {

			buildJCTMorphDictionary ();

			//UnityEngine.Debug.LogWarning ("You're calling JCT too early, jct_morph_dictionary is null");
			//return false;
		}

		if ( !jct_morph_dictionary.TryGetValue (key, out index)) {
			return false;
		}

		m_morphs [index].m_value = val;
		return true;
	}



    /// <summary>
    /// Internal. Returns the SkinnedMeshRenderer component for this GameObject.
    /// </summary>
    /// <value>The mesh render.</value>
    private SkinnedMeshRenderer MeshRender
	{
		get {
			if (m_meshRender == null) {
                //#ROOT HACK
                //				CIbody figure = transform.root.gameObject.GetComponentInChildren<CIbody> ();


                if (charman == null)
                {
                    charman = gameObject.GetComponentInParent<MCSCharacterManager>();
                    if(charman == null)
                    {
                        //if it's still null, walk up the chain, perhaps we're in play mode and the figure is disabled
                        GameObject topObj = gameObject.transform.parent.parent.gameObject;
                        if(topObj != null)
                        {
                            charman = topObj.GetComponent<MCSCharacterManager>();
                            if(charman == null)
                            {

                                throw new UnityException("Can't find character manager from hip joint");
                            }
                        }
                    }
                }
				
				CIbody figure = charman.figureMesh;

				m_meshRender = figure.currentCoreMesh.gameObject.GetComponentInChildren (typeof(SkinnedMeshRenderer)) as SkinnedMeshRenderer;

			}
			return m_meshRender;
		}
	}

    private Transform[] _MeshRendererBones;
    private Transform[] MeshRendererBones
    {
        get
        {
            if(_MeshRendererBones == null)
            {
                _MeshRendererBones = MeshRender.bones;
            }
            return _MeshRendererBones;
        }
    }


    private int[] indexes = new int[20];



	/********************************************/
	/*************** SUBSCRIBERS ****************/
	/********************************************/
	private Dictionary<SkinnedMeshRenderer,Matrix4x4[]> SubscriberBoneMaps;
	private Dictionary<SkinnedMeshRenderer,Matrix4x4[]> SubscriberBindposeBackups;
	private Dictionary<SkinnedMeshRenderer,int[]> SubscriberBoneMapsproxy;
	private List<SkinnedMeshRenderer> subscribers;

    //tracks if we need to recheck and make clones of meshes, SUPER IMPORTANT!
    private bool subscribersDirty = false;

    private bool hasStarted = false;


	/// <summary>
	/// Subscribe given SkinnedMeshRenderer to included in bindpose changes. 
	/// </summary>
	/// <param name="sub">Sub.</param>
	public void SubscribeToBindPoseChanges (SkinnedMeshRenderer sub)
	{

        if(sub == null)
        {
            throw new System.Exception("Subscriber in SubscribeToPindPoseChanges can not be null");
        }

        //UnityEngine.Debug.LogWarning("SubscribeToBindPoseChanges: " + sub.name);
		if (SubscriberBindposeBackups == null)
			SubscriberBindposeBackups = new Dictionary<SkinnedMeshRenderer,Matrix4x4[]> ();

		if (SubscriberBoneMaps == null)
			SubscriberBoneMaps = new Dictionary<SkinnedMeshRenderer,Matrix4x4[]> ();

		if (SubscriberBoneMapsproxy == null)
			SubscriberBoneMapsproxy = new Dictionary<SkinnedMeshRenderer,int[]> ();

		if (subscribers == null)
			subscribers = new List<SkinnedMeshRenderer> ();
		
		if (SubscriberBoneMaps.ContainsKey (sub) == false && sub != MeshRender) {
			SubscriberBindposeBackups [sub] = sub.sharedMesh.bindposes;

            Transform[] smrBones = sub.bones;
            Transform[] meshBones = MeshRendererBones;
			int[] bone_refs = new int[smrBones.Length];

			// build map
			int i = 0;



            //Debug.Log("SubscriptToBindPoseCHanges Lookup: " + name + " | " + sub.bones.Length);
			foreach (Transform bone in smrBones) {
                if (bone == null)
                {
                    //TODO: this is really an error, only in here for testing, this is likely the case when the bones don't match between the figure and the clothing
                    UnityEngine.Debug.LogWarning("Subscriber bone is null, will ignore bone slot: " + i.ToString() + " for mesh: " + sub.name);
                    bone_refs[i] = -1;
                }
                else
                {
                    int target_bone = FindBone(meshBones, bone.name);
                    //Debug.Log("Bone: " + bone.name + ":F:" + target_bone + " | " + MeshRender.bones[target_bone].name + " | " + MeshRender.bones.Length);
                    bone_refs[i] = target_bone;
                }
				i++;
			}

			Matrix4x4[] targets = new Matrix4x4[smrBones.Length];
			i = 0;
			foreach (int index in bone_refs) {
                if (index == -1)
                {
                    targets[i] = Matrix4x4.identity;
                }
                else
                {
                    targets[i] = current_bind_poses[index];
                }
				i++;
			}

			// Debug.Log (bone_refs.Length + ":" + targets.Length);
			// possibly need to force the item to use our bones if geometry cloner is all wonky....

			// add key value with map as value
			SubscriberBoneMaps [sub] = targets;//bone_refs;
			SubscriberBoneMapsproxy [sub] = bone_refs;
			subscribers.Add (sub);
			sub.sharedMesh.MarkDynamic();
		}

        subscribersDirty = true;
	}

    public void UnsubscribeToBindPoseChanges(SkinnedMeshRenderer smr)
    {
        if(subscribers == null || smr == null)
        {
            return;
        }
        for(int i= subscribers.Count-1; i>=0; i--)
        {
            if (subscribers[i].GetInstanceID().Equals(smr.GetInstanceID()))
            {
                subscribers.RemoveAt(i);
            }
        }
    }



	/// <summary>
	/// Internal method to find the index of the bone refered to by the given string
	/// </summary>
	/// <returns>Integer bone index</returns>
	/// <param name="destination_bones">An array of type Transform.</param>
	/// <param name="bone_to_find">A string name of the bone to find.</param>
	/// <remarks>Using a regular expression inside a loop has to be terribly inefficient. Doubly inefficient as this method is called within a loop.</remarks>
	private int FindBone (Transform[] destination_bones, string bone_to_find) {
        int i = 0;

        if(meshRenderBoneDict == null)
        {
            Debug.Log("MeshRenderBoneDict is null");
            return 0;
        }

        //does this bone exist in our cache?
        if (meshRenderBoneDict.TryGetValue(bone_to_find,out i))
        {
            return i;
        }

		string pattern = @"(\s(?<=\s)\d+)?";
		Regex rgx = new Regex(pattern);
		
		foreach (Transform bone in destination_bones) {
			if (rgx.Replace (bone.name, "") == rgx.Replace (bone_to_find, "")) {
				return i;
			}
			i++;
		}
		return 0;
	}

	void Start(){
        hasStarted = false;

        if(!Application.isPlaying && EnableOnlyDuringPlay)
        {
            //short circuit out now
            return;
        }

		// possibly don't need these initers because we do them at creation setup, which is at import, and they serialize
		SkinnedMeshRenderer meshRender = MeshRender;
		Mesh mesh = meshRender.sharedMesh;

		// Vector3[] vertices = mesh.vertices;
		// m_vertices  = new Vector3[vertices.Length];

		Matrix4x4[] bindPoses = mesh.bindposes;
		current_bone_positions = new Vector3[bindPoses.Length];
		temp_bone_position_deltas = new Vector3[current_bone_positions.Length];
        
        buildJCTMorphDictionary();

        // this is for JCTs snd we only care about JCTs at runtime
        if (Application.isPlaying || !EnableOnlyDuringPlay) {
            //#ROOT HACK

            if (charman == null)
				//you should be able to reliably get the character manager with the same call from anywhere in the stack
				charman = gameObject.GetComponentInParent<MCSCharacterManager> ();


			//			charman = transform.root.gameObject.GetComponentInChildren<CharacterManager> ();
			if (charman != null) {
				charman.OnCMBlendshapeValueChange += OnCMBlendshapeValueChange;
			}
		}

        hasStarted = true;
    }



	/// <summary>
	/// MonoBehaviour Awake event called on instance creation.
	/// </summary>
	void Awake()
	{
		


		// this is for JCTs snd we only care about JCTs at runtime

	}

    /// <summary>
    /// If the JCT needs to be woken up, or things change, call this which will rebuild lists and check to see if things should be cloned
    /// </summary>
    public void WakeUp()
    {
        //UnityEngine.Debug.Log("JCT Wakeup");
        //TODO: this should be put elsewhere, but jct is going to be obsolete for 2.0 so I don't see too much harm in leaving this here for now
        buildMorphDictionaries();

        //we no longer trigger hotfixupdate immediately, we only run in LateUpdate
        JCTsNeedUpdate = true;

        //TODO: move this to be more efficient, only run if we actually need it (as in, use DoesBlendshapeUseJCT)
        CheckAndCloneSelfAndSubscribers();
    }



	/// <summary>
	/// Called when the CharaceterManager raises the OnCMBlendshapeValueChange event.
	/// </summary>
	void OnCMBlendshapeValueChange ()
	{
        WakeUp();
    }




	/// <summary>
	/// Called during jctimport utility setup and never again
	/// </summary>
	/// <param name="mesh_renderer">Mesh renderer.</param>
	public void CreationSetup (SkinnedMeshRenderer mesh_renderer)
	{
		m_meshRender = null;
		// m_children = new List<JCTAdapter>();

		SkinnedMeshRenderer meshRender = mesh_renderer;
		Mesh mesh = meshRender.sharedMesh;

		// 	Vector3[] vertices = mesh.vertices;
		// 	m_originals = new Vector3[vertices.Length];
		// 	m_vertices  = new Vector3[vertices.Length];
		// 	for (int i = 0; i < vertices.Length; i++)
		// 		m_originals[i] = vertices[i];

		Matrix4x4[] bindPoses = mesh.bindposes;
		Transform[] bones = meshRender.bones;

		base_bind_poses = new Matrix4x4[bindPoses.Length];
		// Debug.Log ("BIND ORIGIN LENGTH: " + base_bind_poses.Length);
		current_bind_poses = new Matrix4x4[bindPoses.Length];
		current_bone_positions = new Vector3[bindPoses.Length];
		temp_bone_position_deltas = new Vector3[bindPoses.Length];
		base_bone_positions = new Vector3[bindPoses.Length];

		for (int i = 0; i < bindPoses.Length; i++) {
			base_bind_poses[i]  = bindPoses[i];
			current_bind_poses[i] = bindPoses[i];
			base_bone_positions[i]  = bones[i].localPosition; 
		}
	}

    public void Reset()
    {
        //reset the all sub items attached to the figure
        if (subscribers != null)
        {
            foreach (SkinnedMeshRenderer rend in subscribers)
            {
                if (rend != null && rend.sharedMesh != null && SubscriberBindposeBackups != null)
                {
                    rend.sharedMesh.bindposes = SubscriberBindposeBackups[rend];
                }
            }
        }

        //reset the figure
        if (m_meshRender != null && m_meshRender.sharedMesh != null)
        {
            m_meshRender.sharedMesh.bindposes = base_bind_poses;
        }

        //we don't serialize ANYTHING in jct land, this does not work unless we serialize these variables
        /*
        if(m_meshRender != null)
        {
            UnityEngine.Debug.LogWarning("Restoring to original");
            m_meshRender.sharedMesh = m_originalMesh;
        }
        */
    }

    public void CheckAndCloneSelfAndSubscribers()
    {
        //are we in editor land, if so abandon ship, we do this so we don't get clones of clones b/c we don't serialize anything in jct land
        if (!Application.isPlaying && EnableOnlyDuringPlay)
        {
            return;
        }

		//do we need a clone?
		if (!UsesJCT()) {
			return;
		}

        if (subscribers != null)
        {
            foreach (SkinnedMeshRenderer rend in subscribers)
            {
                CloneMeshProperly(rend);
            }
        }

        CloneMeshProperly(m_meshRender);

        //we've made clones as needed, so turn off our check
        subscribersDirty = false;
    }

    public void CloneMeshProperly(SkinnedMeshRenderer smr)
    {
        if(smr == null || smr.sharedMesh == null)
        {
            return;
        }
        int id = smr.GetInstanceID();
        if (m_cloned_lookup.ContainsKey(id))
        {
            return;
        }
        Mesh mesh = smr.sharedMesh;

        mesh = (Mesh)Instantiate(mesh);
        mesh.name = mesh.name.Replace("(Clone)", "-Copy");
        smr.sharedMesh = mesh;
        m_cloned_lookup[id] = true;
    }



	/// <summary>
	/// MonoBehaviour event broadcast when application closes.
	/// </summary>
	void OnApplicationQuit ()
	{
        Reset();
	}

	public bool UsesJCT()
	{
		foreach(JCTMorph m in m_morphs){
			if (m.m_value > 0f) {
				return true;
			}	
		}

		return false;
	}


	/// <summary>
	/// Static Clamp function similar to Unity's Mathf.Clamp 
	/// </summary>
	/// <param name="value">Value.</param>
	/// <param name="min">Minimum.</param>
	/// <param name="max">Max.</param>
	public static float Clamp(float value, float min, float max)  
	{  
		return (value < min) ? min : (value > max) ? max : value;  
	}

    public bool DoesBlendshapeUseJCT(string blendshapeName)
    {
        return jct_morph_dictionary.ContainsKey(blendshapeName);
    }

    protected bool _enabled = true;
    public void ToggleJCTs(bool enabled)
    {
        _enabled = enabled;
    }


	/// <summary>
	/// Internal method to update the mesh on any morph changes.
	/// </summary>
	/// <remarks>this is not fully optimized even with reduced jct calculations because it gets called on ANY morph change, not specific morph changes.</remarks>
	void HotfixUpdate()
	{
		int i = 0;

		// resets everything to originals

        base_bind_poses.CopyTo(current_bind_poses,0);
        base_bone_positions.CopyTo(current_bone_positions, 0);

		// CHANGE : lets zero all the destinations and get the current active from blendshapemodel - aka charman
		// 10% of 12 total
        var enumerator = morph_to_BS_index.GetEnumerator();
        while (enumerator.MoveNext()) {
            string key = enumerator.Current.Key;
			morph_to_BS_float[key] = Clamp (MeshRender.GetBlendShapeWeight (morph_to_BS_index[key]) / 100.0f, 0.0f, 1.0f);
		}

        // twice as efficient, same-ish memory even without optimzied get active call, but failed likely because the names are different
        // List<CoreBlendshape> active = charman.GetActiveBlendShapes ();
        // foreach (CoreBlendshape bs in active) {
        // 		morph_to_BS_float [bs.dazName] = bs.currentValue;
        // }

        // this goes through each and every local jct affected morph (112) and tries to get value out of current active blendshapes
        // this could be innefficient in ways.
        // 1) if we are local ct morh centric, we always check for 112 morphs
        // 2) if we are active morph centric, we start with a lower number but may eventually ahve more than 112 morphs
        // 3) linq functions are slow for union and itnersect, but we could loop through the active morphs and check if they are a jct morph.
        // this make every frmae slightly slower, but never completely wasteful
        // it is likely splitting hairs...
        // roughly 4% of 12 processing
        i = 0;
		for (; i < m_morphs.Length; i++) {
			float val = m_morphs[i].m_value;
			if (morph_to_BS_float.TryGetValue (m_morphs[i].m_name, out val)) {
				m_morphs[i].m_value = val;
				// Debug.Log (m_morphs [i].m_name + ":" + m_morphs[i].m_value);
			}

			// we wont apply changes, we will save them and broadcast the change to listeners
			if (m_morphs[i].m_value != 0)
				ApplyBindPoseChanges (m_morphs[i]);
		}



        //again no application - just calculation
        MeshRender.sharedMesh.bindposes = current_bind_poses;


        bool needs_swizzle = false;
        if (subscribers != null)
        {
            foreach (SkinnedMeshRenderer ren in subscribers)
            {

                if (ren.enabled == false)
                {
                    needs_swizzle = true;
                }

                // Debug.Log (ren.name);
                indexes = SubscriberBoneMapsproxy[ren];
                // Matrix4x4[] bind_p = ren.sharedMesh.bindposes;
                i = 0;
                foreach (int index in indexes)
                {
                    if (index < 0)
                    {

                    }
                    else
                    {
                        // Debug.Log (ren.sharedMesh.bindposes [i].ToString ("F6"));
                        // Debug.Log ("--------------");
                        // Debug.Log (current_bind_poses [index].ToString ("F6"));
                        // bind_p[i] = current_bind_poses[index];
                        SubscriberBoneMaps[ren][i] = current_bind_poses[index];
                        // Debug.Log (current_bind_poses[index][13].ToString ("F6") + ":" + SubscriberBoneMaps[ren][i][13].ToString ("F6"));
                    }
                    i++;
                }

                // ren.sharedMesh.bindposes = bind_p;
                if (needs_swizzle)
                {
                    ren.enabled = true;
                }

                ren.sharedMesh.bindposes = SubscriberBoneMaps[ren];

                if (needs_swizzle)
                {
                    ren.enabled = false;
                    needs_swizzle = false;
                }
            }
        }

		if (temp_bones == null) {
			temp_bones = MeshRendererBones;
		}

		i = 0;
		for (; i < base_bind_poses.Length; i++) {
			// mecanim will change our bones, so we need the delta between the old and new bind pose to reapply on late update
			temp_bone_position_deltas[i] = current_bone_positions[i] - base_bone_positions[i];
			// no application - just calculation
			// MeshRender.bones[i].localPosition = current_bone_positions[i]; //when applying changes to item in arrays in a unity control item, its generally better to clone the whole array, apply the changes locally, then assign the whole array
			temp_bones [i].localPosition = current_bone_positions [i];
		}

		//there's a chance there is no animator component, so do a second check
		Animator anim = charman.GetAnimatorManager ();
		if (anim != null) {
			//if we do, tell mecanim to rerun it's update, this will trigger a re rooting of the mesh at the root pivot point (typically the feet), see animator.pivotWeight
			anim.Update (0f);
		}
	}


	/// <summary>
	/// MonoBehaviour LateUpdate event called at the end of every frame
	/// </summary>
	void LateUpdate ()
	{
        if (!Application.isPlaying && EnableOnlyDuringPlay)
        {
            //short circuit out now
            return;
        }

        //if we haven't started, force a start now
        if (!hasStarted)
        {
            Start();
        }

        if (OnPreJCT != null)
        {
            OnPreJCT();
        }


        //don't run unless we need to
        if (!_enabled)
        {
            //if we're short circuiting fire the post jct event even if we don't need to
            if (OnPostJCT != null)
            {
                OnPostJCT();
            }
            return;
        }

        if (subscribersDirty)
        {
            CheckAndCloneSelfAndSubscribers();
        }

		// run the hotfix update if needed
		if (JCTsNeedUpdate) {
			HotfixUpdate ();
			JCTsNeedUpdate = false;
		}

		// by pulling the meshrender.bones access out to a saved variable, we consume 50k less garbage, 
		// and kill processing on non changed frames because meshrender.bones[i] is comparatively expensive
		if(temp_bones == null)
			temp_bones = MeshRendererBones; //these are the runtime bones, like on the bone service. they should be index aligned with bind_poses

		// check to see if the animation system messed with our current bone transforms
		int i = 0;
		// bool needs_update = false;
		for (; i < base_bind_poses.Length; i++) {
			if (temp_bones [i].localPosition != current_bone_positions [i]) {
		// 		needs_update = true;
				temp_bones [i].localPosition += temp_bone_position_deltas [i]; // likely applied to add the needed jct offset without affecting whatever meccanim did
		// 		temp_bones [i].localPosition = current_bone_positions [i];
				current_bone_positions [i] = temp_bones [i].localPosition; // likely set here so that we have a post meccanim sync bone position
			}
		}

        //we might need this - keep it around for a while.
        // if(needs_update == true)
        // MeshRender.bones = temp_bones;

        if (OnPostJCT != null)
        {
            OnPostJCT();
        }

    }

    public void ResetPose()
    {
        if (subscribers != null)
        {
            foreach (SkinnedMeshRenderer smr in subscribers)
            {
                smr.sharedMesh.bindposes = SubscriberBoneMaps[smr];
            }
        }
    }



    /// <summary>
    /// Internal method, applies delta offset translation for each morph.
    /// </summary>
    /// <remarks>
    /// we may want to tally the totals once and apply it once.
    /// </remarks>>
    private void ApplyBindPoseChanges (JCTMorph morph)
	{
		float v = morph.m_value; //range is 0->1 (used as a percent for the deltas below)
		if (morph.m_nodes != null && current_bind_poses.Length == morph.m_nodes.Length)
		{
			for (int i = 0; i < current_bind_poses.Length; i++) {
				current_bone_positions[i] += morph.m_offsets[i] * v;
				current_bind_poses[i][12] -= morph.m_nodes[i].x * v; //updating the x translation value of the matrix
				current_bind_poses[i][13] -= morph.m_nodes[i].y * v; //updating the y translation value of the matrix
				current_bind_poses[i][14] -= morph.m_nodes[i].z * v; //updating the z translation value of the matrix
			}
		}
	}



}



/// <summary>
/// Morph class containing data for an individual morph
/// </summary>
[System.Serializable]
public class JCTMorph 
{
	public string        m_name;
	public float         m_value;
	public float         m_target;
	public Vector3[]     m_nodes;
	public Vector3[]     m_offsets;
	public Texture2D     m_image;
}



/// <summary>
/// VertexDelta class ties an integer index to a Vector3 vertex delta
/// </summary>
public struct VertexDelta
{
	public int      index;
	public Vector3  vector; 
}


