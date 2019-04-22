using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace MCS.Item
{

    //This gets attached to all interesting fbx nodes if a key "MCS_ID" is found, which will be saved to GUID inside the component
    // we don't show it in the inspector by default as this is specific to how artist tools exports fbx nodes
    [HideInInspector]
    public class MCSProperty : MonoBehaviour
    {
        public string mcs_id;
    }
}
