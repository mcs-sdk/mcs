using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using MCS;
using MCS.COSTUMING;

using System.Linq;

[System.Serializable]
internal class ContentPackModel
{
	/// <summary>
	/// List of type ContentPack of all packs associated with this model.
	/// </summary>
	public List<ContentPack> _availableContentPacks;
	public List<ContentPack> availableContentPacks
	{
		get {
			if (_availableContentPacks == null)
				_availableContentPacks = new List<ContentPack> ();
			return _availableContentPacks;
		}
	}



	/// <summary>
	/// Initializes a new instance of the <see cref="ContentPackModel"/> class.
	/// </summary>
	public ContentPackModel ()
	{

    }



	/// <summary>
	/// Adds a ContentPack given a root GameObject.
	/// </summary>
	/// <returns><c>true</c>, if game object as content pack was added, <c>false</c> otherwise.</returns>
	/// <param name="obj">The root GameObject to add.</param>
	public bool AddGameObjectAsContentPack (GameObject obj)
	{
		ContentPack pack = GetContentPackByName (obj.name);
		if (pack == null)
		{
			availableContentPacks.Add(new ContentPack(obj));
			return true;
		}
		return false;
		
	}



	/// <summary>
	/// Adds a given ContentPack to the model.
	/// </summary>
	/// <returns><c>true</c>, if content pack was added, <c>false</c> otherwise.</returns>
	/// <param name="content_pack">Content pack.</param>
	public bool AddContentPack (ContentPack content_pack)
	{
		ContentPack pack = GetContentPackByName (content_pack.name);

        if (pack == null)
		{
			availableContentPacks.Add(content_pack);
			return true;
		} else
        {
            UnityEngine.Debug.LogWarning("Content pack was already in list: " + pack.name +", skipping. Please remove the content pack before adding if you want to replace it.");
        }
		return false;
	}



	/// <summary>
	/// Removes a content pack given a root GameObject for the pack to be removed.
	/// </summary>
	/// <returns><c>true</c>, if content pack was removed, <c>false</c> otherwise.</returns>
	/// <param name="obj">The root GameObject of the ContentPack</param>
	public bool RemoveContentPack (GameObject obj)
	{
        
		if (obj == null) {
			Debug.LogWarning ("RemoveContentPack obj is null, clearing out bad data");
			RemoveNullPacks ();
			return false;
		}

		ContentPack pack = GetContentPackByName(obj.name);

        if (pack == null) {
			return false;
		} else {
			availableContentPacks.Remove(pack);
			return true;
		}
	}

    /// <summary>
    /// Attempts to find items that are on the figure that should not be and will remove them
    /// </summary>
    public void RemoveRogueContent(GameObject RootObject)
    {
        CoreMeshMetaData[] cmmds = RootObject.GetComponentsInChildren<CoreMeshMetaData>();

        HashSet<string> availableIds = new HashSet<string>();

        List<ContentPack> badCPs = new List<ContentPack>();

        //build a map
        foreach(ContentPack cp in availableContentPacks)
        {
            CoreMeshMetaData[] cpCMMDs = null;
            try
            {
                cpCMMDs = cp.RootGameObject.GetComponentsInChildren<CoreMeshMetaData>();
            }
            catch
            {
                badCPs.Add(cp);
                continue;
            }

            if (cpCMMDs != null)
            {
                foreach (CoreMeshMetaData cmmd in cpCMMDs)
                {
                    availableIds.Add(cmmd.ID);
                }
            }
        }

        foreach(ContentPack cp in badCPs)
        {
            availableContentPacks.Remove(cp);
        }

        foreach(CoreMeshMetaData cmmd in cmmds)
        {
            if (availableIds.Contains(cmmd.ID))
            {
                continue;
            }

            CIbody selfBody = cmmd.gameObject.GetComponent<CIbody>();
            if(selfBody != null)
            {
                //skip the body
                continue;
            }

            //we want to remove this node
            GameObject nodeParent = cmmd.transform.parent.gameObject;
            CIbody cibody = nodeParent.GetComponent<CIbody>();
            MCSCharacterManager nCharMan = nodeParent.GetComponent<MCSCharacterManager>();

            GameObject deleteObj = cmmd.gameObject;

            //make sure we're not removing the main figure or anything like that
            if(cibody == null && nCharMan == null)
            {
                deleteObj = nodeParent;
            }

            UnityEngine.Debug.Log("Found rogue item: " + deleteObj + " removing from " + RootObject.name);

            if (Application.isPlaying)
            {
                GameObject.Destroy(deleteObj);
            } else
            {
                GameObject.DestroyImmediate(deleteObj);
            }
            
        }

    }

	/// <summary>
	/// purges empty content packs
	/// </summary>
	public void RemoveNullPacks(){
		//clear out any nulled/empty content packs
		List<ContentPack> removeList = new List<ContentPack>();

		for (int i = 0; i < availableContentPacks.Count; i++) {

			if (availableContentPacks [i] == null) {
				removeList.Add (availableContentPacks [i]);
				continue;
			}
			if (availableContentPacks [i].name.Length <= 0) {
				removeList.Add (availableContentPacks [i]);
				continue;
			}
			if (availableContentPacks [i].RootGameObject == null) {
				removeList.Add (availableContentPacks [i]);
				continue;
			}
		}

		availableContentPacks.RemoveAll (i => removeList.Contains (i));

	}


	/// <summary>
	/// Internal method to get a ContentPack by name
	/// </summary>
	/// <returns>The found ContentPack or null.</returns>
	/// <param name="name">The string name to search for</param>
	private ContentPack GetContentPackByName (string name)
	{
		return availableContentPacks.Find (pack => pack.name == name);
	}



	/// <summary>
	/// Return a List of type ContentPack of all content packs on this model.
	/// </summary>
	/// <returns>The all packs.</returns>
	public List<ContentPack> GetAllPacks ()
	{
		return availableContentPacks;
	}

    public void ClearPacks()
    {
        availableContentPacks.Clear();
    }



//	public void OnBeforeSerialize ()
//	{
//	}



//	public void OnAfterDeserialize ()
//	{
//		if (availableContentPacks == null)
//			availableContentPacks = new List<ContentPack> ();
//	}



}
