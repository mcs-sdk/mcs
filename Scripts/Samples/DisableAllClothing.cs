/***************************************************************
* For reference only
* Copyright MCS 2017 All Rights Reserved
***************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MCS;
using MCS.COSTUMING;

/// <summary>
/// Component class that can be attached to an MCS figure. The Monobehavior uses the CharacterManager
/// to disable all clothing attached to the CharacterManager on start.
/// </summary>
class DisableAllClothing : MonoBehaviour
{

    //Use this for initializations
    public void Start()
    {
        //Get a reference to the CharacterManager component
        MCSCharacterManager m_CharacterManager = GetComponent<MCSCharacterManager>();

        //This call will update 
        m_CharacterManager.DetectAttachedClothing();
        //Iterate through each attached clothing item
        foreach (CIclothing item in m_CharacterManager.GetAllClothing())
        {
            //Set the item's visibility to false.
            item.SetVisibility(false);
        }
    }

    //Update is called once per frame
    public void Update()
    {

    }
}

