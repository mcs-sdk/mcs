/***************************************************************
* For reference only
* Copyright MCS 2017 All Rights Reserved
***************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MCS;
using MCS.COSTUMING;

/// <summary>
/// Component class that randomly selects and attaches a hair from a list of hair to an MCS figure.
/// This component should be attached to an MCSFigure such as MCSMale or MCSFemale.
/// The hair is chosen at start
/// </summary>
public class HairRandomizer : MonoBehaviour
{
    /// <summary>
    /// Set in the Editor - an array of Hairs from which to choose.
    /// </summary>
    public CIhair[] Hairs;

    // Use this for initialization
    void Start()
    {
        //Check if Hairs is non empty
        if (Hairs.Length <= 0)
        {
            //If it's empty, we're done
            return;
        }

        //All of our logic is in the start method so we the reference to the CharacterManager can be local to the function.
        MCSCharacterManager m_CharacterManager = GetComponent<MCSCharacterManager>();

        //Detect any attached hair so we get the most recent list of hairs
        m_CharacterManager.DetectAttachedHair();
        //Get all of the attached hair on the figure
        List<CIhair> attachedHairs = m_CharacterManager.GetAllHair();
        //Iterate through each hair and disable it.
        foreach (CIhair hair in attachedHairs)
        {
            //Disable the hair
            hair.SetVisibility(false);
        }

        //Get a random valid index
        int rndIndex = Random.Range(0, Hairs.Length);
        //Instantiate as a GameObject (default at the origin)
        GameObject obj = Instantiate(Hairs[rndIndex].gameObject) as GameObject;
        //Create a ContentPack (without cloning) from obj. This is safe as obj is guaranteed to have a CostumeItem component class
        ContentPack cp = new ContentPack(obj, false);
        //Add the ContentPack to the figure
        m_CharacterManager.AddContentPack(cp);
        //Disable obj -- Could also destroy obj if space is needed
        obj.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
