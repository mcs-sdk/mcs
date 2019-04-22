using UnityEngine;
using System.Collections.Generic;

using MCS;
using MCS.COSTUMING;

[System.Serializable]
/// <summary>
/// Content Pack definition class. Every ContentPack
/// 
/// A ContentPack represents an attachable unit in the MCS Ecosystem.
/// ContentPacks consist of 0 or more GameObjects. These GameObjects will
/// be attached to an MCSFigure when the ContentPack is added to that figure's
/// CharacterManager. In order for the GameObjects to attach correctly they should
/// have a CostumeItem component (CIHair, CIClothing, CIProp) attached.
/// 
/// A ContentPack can be added to the CharacterManager of a figure 
/// by calling the manager's AddContentPack method.
/// A ContentPack can be removed from the CharacterManager of an MCS 
/// Figure by calling the manager's RemoveContentPack method.
/// </summary>
public class ContentPack
{
	/// <summary>
	/// The name of the content pack.
    /// If this ContentPack is constructed using the 2 argument
    /// constructor the name will be set to the name of the 
    /// root GameObject passed to the constructor.
	/// </summary>
	public string name;



	/// <summary>
	/// The root GameObject for the ContentPack.
    /// If this ContentPack is constructed using the 2 argument constructor
    /// this will be the root of the GameObject passed to the constructor.
	/// </summary>
	public GameObject RootGameObject;



	/// <summary>
	/// CIclothing array of any clothing items in this content pack.
	/// </summary>
	public CIclothing[] availableClothing;



	/// <summary>
	/// CIhair array of any hair items in this content pack.
	/// </summary>
	public CIhair[] availableHair;



	/// <summary>
	/// CIprop array of any prop items in this content pack.
	/// </summary>
	public CIprop[] availableProps;

    /// <summary>
    /// All items exist in here
    /// </summary>
    public CostumeItem[] availableItems;



	/// <summary>
	/// Initializes a new, empty instance of the <see cref="ContentPack"/> class.
    /// 
    /// This constructor does not initialize public class members and they 
    /// are left to their default values.
    /// These should be initialized prior to access. This can be done with 
    /// setupWithGameObject
	/// </summary>
	public ContentPack ()
	{
	}



	/// <summary>
	/// Initializes a new instance of the <see cref="ContentPack"/> class.
    /// 
    /// The ContentPack is initialized with the provided game object.
    /// See <see cref="setupWithGameObject(GameObject, bool)"/> 
	/// </summary>
	/// <param name="obj">Object.</param>
	public ContentPack (GameObject obj, bool clone = false)
	{
		setupWithGameObject (obj,clone);
	}



	/// <summary>
	/// Attempts to initializes this ContentPack with data gleamed from the given root GameObject.
    /// 
    /// The root GameObject must have at least one CostumeItem component in it's children otherwise 
    /// initializing the ContentPack will fail with an Exception.
    /// 
    /// If successful references to the CostumeItems in the GameObject will be stored in the appropriate
    /// public Arrays of this ContentPack.
	/// </summary>
	/// <param name="obj">
    /// Root GameObject to be used for initializing this ContentPack. Should have 
    /// at least one child with a CostumeItem component.
    /// </param>
    /// <param name="clone">
    /// Optional bool. If true the root obj will be cloned and the clone will be used to initialize this ContentPack.
    /// </param>
	public void setupWithGameObject (GameObject obj,bool clone=false)
	{
        if (clone)
        {
            GameObject cloneObj = GameObject.Instantiate(obj);
            cloneObj.name = obj.name;
            obj = cloneObj;
        }
        
		name = obj.name;
		RootGameObject = obj;
		availableClothing = obj.GetComponentsInChildren<CIclothing> (true);
		availableHair = obj.GetComponentsInChildren<CIhair> (true);
		availableProps = obj.GetComponentsInChildren<CIprop> (true);
        availableItems = obj.GetComponentsInChildren<CostumeItem>(true);

        if(
                (availableClothing == null || availableClothing.Length <=0)
                && (availableHair == null || availableHair.Length <= 0)
                && (availableProps == null || availableProps.Length <= 0)
        )
        {
            UnityEngine.Debug.LogError("Could not locate any clothing, hair, or props in content pack, aborting object: " + obj.name);
            throw new System.Exception("Invalid content pack");
        }
	}



}
