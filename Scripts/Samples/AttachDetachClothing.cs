/***************************************************************
* For reference only
* Copyright MCS 2017 All Rights Reserved
***************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MCS.COSTUMING;
using MCS;

/// <summary>
/// Component class that can be attached to the root of an MCS figure. 
/// The component listens for input from the mouse
/// and attaches or detaches clothing on click.
/// </summary>
public class AttachDetachClothing : MonoBehaviour
{

    /// <summary>
    /// Array of CIclothing components or their GameObjects that we want to attach at runtime.
    /// This array must be populated in the Editor.
    /// </summary>
    public CIclothing[] clothing;

    //We will construct a ContentPack from the array of clothing
    private ContentPack cp;

    //Tracks whether the ContentPack cp is attached to our figure.
    private bool isAttached;

    //Reference to the CharacterManager component on our MCS Figure for convenenience.
    private MCSCharacterManager m_CharacterManager;

    // Use this for initialization
    void Start()
    {
        //Cnstruct an empty gameobject that we'll use to hold each of our CIclothing items clothing.
        GameObject go = new GameObject();
        //Give the GameObject a name
        go.name = "Root";
        //Disable it so we don't see it
        go.SetActive(false);

        //Loop through the array and set each GameObject in the array
        //As a child of our empty GameObject go
        for (int i = 0; i < clothing.Length; i++)
        {
            //Make sure element is no null
            if (clothing[i] == null)
            {
                continue;
            }
            //Instantiate the prefab references as GameObjects at the origin
            GameObject cloth = Instantiate(clothing[i].gameObject, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;

            cloth.transform.SetParent(go.transform);
        }

        //We can now construct a single ContentPack that contains all of the clothing items in clothing.
        //The optional boolean parameter in the constructor allows us to clone the GameObject. It is
        //false by default.
        cp = new ContentPack(go);

        //Obtain a reference to the MCS figure's CharacterManager Component
        m_CharacterManager = GetComponent<MCSCharacterManager>();

        //Start out unattached
        isAttached = false;
    }

    // Update is called once per frame
    void Update()
    {

        //If the left mouse button was released
        if (Input.GetMouseButtonUp(0))
        {
            //If the ContentPack is attached
            if (isAttached)
            {
                //Remove it and update our isAttached
                m_CharacterManager.RemoveContentPack(cp);
                isAttached = false;
                /*
                 * Note the call to RemoveContentPack can be at times expensive if the GameObject is also destroyed.
                 * It may be faster to toggle the visibility of the individual CIclothing items in the empty GameObject
                 */

            }
            else
            {
                //Otherwise add the ContetnPack to the CharacterManager
                m_CharacterManager.AddContentPack(cp);
                isAttached = true;

            }
        }
    }


}
