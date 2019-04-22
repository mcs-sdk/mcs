/***************************************************************
* For reference only
* Copyright MCS 2017 All Rights Reserved
***************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component class that sets the Materials defined by the user as the Materials for
/// each available LOD on an MCS figure. Should be attached to the root MCS Figure.
/// 
/// Materials need to be set in the editor. Script does no checking for null references.
/// Materials should use a MCS shader either StandardCharacterShader, or SkinDefferred
/// Currently no checking to enforce this.
/// </summary>
public class SetMaterials : MonoBehaviour
{
    /// <summary>
    /// A Material for the Head material slot.
    /// Should use either the MCS/VolundVariants/StandardCharacterShader or the
    /// MCS/SkinDeffered shader.
    /// </summary>
    public Material Mat_Head;
    /// <summary>
    /// A Material for the Body material slot.
    /// Should use either the MCS/VolundVariants/StandardCharacterShader or the
    /// MCS/SkinDeffered shader.
    /// </summary>
    public Material Mat_Body;
    /// <summary>
    /// A Material for the Eye and Lash material slot.
    /// Should use either the MCS/VolundVariants/StandardCharacterShader or the
    /// MCS/SkinDeffered shader.
    /// </summary>
    public Material Mat_EyeLash;

    public void Start()
    {
        //Loop through the Lods and set the materials
        //See FindLODS() below for how we find the LODs on the figure
        foreach (SkinnedMeshRenderer smr in FindLODS())
        {
            //Create a new material array for the current LOD
            Material[] newMats = new Material[smr.materials.Length];

            //LOD0 has a differnt material ordering so assign materials accordingly
            if (smr.name.EndsWith("ale_LOD0"))
            {
                //Assign Eye and Lash material
                newMats[0] = Mat_EyeLash;
                //Assign Body Material
                newMats[1] = Mat_Body;
            }
            else
            {
                //Assign body material
                newMats[0] = Mat_Body;
                //Assign eye and lash material
                newMats[1] = Mat_EyeLash;
            }
            //Assign head material
            newMats[2] = Mat_Head;

            //Assign the newMaterials array as the materials for the SkinnedMeshRenderer
            //We assign the entire array rather than individual materials because that is what Unity requires.
            smr.materials = newMats;
        }

    }

    /// <summary>
    /// Performs a smart breadth first search for the LODS of the figure
    /// and yields each one as it finds it.
    /// </summary>
    /// <returns></returns>
    private IEnumerable<SkinnedMeshRenderer> FindLODS()
    {
        //Declare a transform curr that we'll use later
        Transform curr = null;
        //Loop through the chilren of the gameobject's transform until we find the transform we're looking for
        for (int i = 0; i < transform.childCount; i++)
        {
            //Get the current child
            curr = transform.GetChild(i);
            //if it's M3DMale or M3DFemale we're done
            if (curr.name.Equals("M3DMale") || curr.name.Equals("M3DFemale")
             || curr.name.Equals("MCSMale") || curr.name.Equals("MCSFemale"))
            {
                break;
            }
        }

        //If we've found the transform we're looking for curr will not be null
        //As long as this component is attached to the Root M3D figure curr will not be null
        if (!(curr == null))
        {
            //Loop through curr's children. This is where we [should] find the LODs for hte figure.
            for (int i = 0; i < curr.childCount; i++)
            {
                //Get the current child
                Transform child = curr.GetChild(i);
                //If it is a LOD yield the SkinnedMeshRenderer component to the caller.
                if ( child.name.StartsWith("M3DMale_LOD") || child.name.StartsWith("M3DFemale_LOD")
                 || child.name.StartsWith("MCSMale_LOD") || child.name.StartsWith("MCSFemale_LOD"))
                {
                    yield return child.gameObject.GetComponent<SkinnedMeshRenderer>();
                }
            }
            
        }


    }
}

