/***************************************************************
* For reference only
* Copyright MCS 2017 All Rights Reserved
***************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MCS.FOUNDATIONS;
using MCS;

/// <summary>
/// Component class to be attached to an MCS Figure. Attempts to drive the given
/// Blendshape linearly from 0 to the given value over the period of 5 seconds.
/// </summary>
public class DriveMorph : MonoBehaviour
{

    /// <summary>
    /// The localName of the morph we want to drive e.g. "FBMHeavy"
    /// </summary>
    public string MorphName;
    /// <summary>
    /// Maximum value of the morph we want to drive.
    /// Between 0 and 100
    /// </summary>
    public float MaxMorphValue;

    /// <summary>
    /// Read Only reference to the Morph that we will be driving
    /// </summary>
    private Morph morph;

    /// <summary>
    /// Did the user supplied MorphName map to a valid Morph?
    /// </summary>
    private bool exists;

    /// <summary>
    /// Reference to the CharacterManager
    /// </summary>
    private MCSCharacterManager m_CharacterManager;


    // Use this for initialization
    void Start()
    {
        //Get a reference to the CharacterManager
        m_CharacterManager = GetComponent<MCSCharacterManager>();

        /* 
         * Attempt to get the Morph from the "User" supplied MorphName
        /* We access the CoreMorphs member of the CharacterManager.
         * From there we can get the morphLookup Dictionary that maps all possible MorphNames to their Morphs.
         */
        exists = m_CharacterManager.coreMorphs.morphLookup.TryGetValue(MorphName, out morph);

        // Set the Morph (or blenddshape) value to 0 so we can drive starting from 0.
        m_CharacterManager.SetBlendshapeValue(morph.name, 0);


    }

    // Update is called once per frame
    void Update()
    {
        //If the blendshape name supplied by the user doesn't exist, we're done.
        if (!exists)
        {
            return;
        }

        //Calculate the delta for the morph value for this call to update
        float delta = (MaxMorphValue / 5) * Time.deltaTime;

        /*
         * Set the blendshape value to the current value + delta, or MaxMorphValue if the value is already there.
         * This could be optimized without the ternary expression so that unecessary code is not run.
         */
        m_CharacterManager.SetBlendshapeValue(morph.name,
                    morph.value >= MaxMorphValue ? MaxMorphValue : morph.value + delta);
    }
}
