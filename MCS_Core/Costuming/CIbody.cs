using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MCS.CONSTANTS;
using MCS.FOUNDATIONS;

namespace MCS.COSTUMING
{
	/// <summary>
	/// The CIbody class contains information on an idividual body item.
	/// Inheriting from CostumeItem, which in turn is a MonoBehaviour; making this class a Component class.
	/// </summary>
	public class CIbody : CostumeItem
	{
		/// <summary>
		/// The backup texture.
		/// </summary>
		public Texture2D backupTexture;



		/// <summary>
		/// Adds a given CoreMesh reference to the interal LOD list.
		/// </summary>
		/// <param name="cm">The CoreMesh reference to add.</param>
		override public void AddCoreMeshToLODlist (CoreMesh cm)
		{
			if (backupTexture == null)
				backupTexture = cm.skinnedMeshRenderer.sharedMaterial.mainTexture as Texture2D;
			base.AddCoreMeshToLODlist (cm);
		}

        /// <summary>
        /// Returns the Material in the specified MATERIAL_SLOT
        /// </summary>
        public Material GetActiveMaterialInSlot(MATERIAL_SLOT querySlot)
        {
            SkinnedMeshRenderer smr = GetSkinnedMeshRenderer();
            /*
            //this causes a problem at runtime where all the mats are wrong
            Material[] materials;
            if (Application.isPlaying)
            {
                materials = smr.materials;
            } else
            {
                materials = smr.sharedMaterials;
            }
            */
            Material[] materials = smr.sharedMaterials;

            for(int i = 0; i < materials.Length; i++)
            {
                if(materials[i] == null)
                {
                    continue;
                }
                string name = materials[i].name.ToLower();
                MATERIAL_SLOT slot = MATERIAL_SLOT.UNKNOWN;

                if (name.Contains("head"))
                {
                    slot = MATERIAL_SLOT.HEAD;
                } else if (name.Contains("body"))
                {
                    slot = MATERIAL_SLOT.BODY;
                } else if (name.Contains("genesis2"))
                {
                    slot = MATERIAL_SLOT.BODY;
                } else if (name.Contains("eyeandlash")){
                    slot = MATERIAL_SLOT.EYEANDLASH;
                }

                if (querySlot.Equals(slot))
                {
                    return materials[i];
                }
            }

            return null;
        }



	}
}
