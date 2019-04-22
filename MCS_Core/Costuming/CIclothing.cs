using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using MCS.CONSTANTS;
using MCS.FOUNDATIONS;

namespace MCS.COSTUMING
{
    /// <summary>
    /// <para>
    /// The CIclothing class contains information on an idividual clothing item.
    /// Inheriting from CostumeItem, which in turn is a MonoBehaviour; making this class a Component class.
    /// </para>
    /// <para>
    /// Use CharacterManager GetClothingByID to get a reference to a specific CIClothing attached to a figure.
    /// Use CharacterManager GetVisibleClothing to get a List that contains all visible hair on the figure.
    /// </para>
    /// <para>
    /// The DLL takes care of attaching this MonoBehaviour as a component to clothing items that are attached to 
    /// an MCS Figure either in Editor or through the API via the CharacterManager.
    /// </para>
    /// For attaching clothing at runtime see <see cref="MCSCharacterManager.AddContentPack(ContentPack)"/> and <see cref="ContentPack"/> 
    /// </summary>
    [Serializable]
	public class CIclothing : CostumeItem, ISerializationCallbackReceiver
    {


		/// <summary>
		/// The alpha mask texture for this clothing item.
		/// </summary>
		[Obsolete("This property is deprecated and you should now use alphaMasks, this is only here for pre 1.5 assets")]
		public Texture2D alphaMask;

		/// <summary>
		/// The alpha mask texture (injection mask) for the head, this will frequently be null as many clothing items do not need to affect any part of the head
		/// </summary>
		public Dictionary<MATERIAL_SLOT,Texture2D> alphaMasks = new Dictionary<MATERIAL_SLOT,Texture2D>();
        
        [SerializeField]
        private List<MATERIAL_SLOT> _alphaMasksKeys = new List<MATERIAL_SLOT>();
        [SerializeField]
        private List<Texture2D> _alphaMasksVals = new List<Texture2D>();



        #region serialization helpers
        //pack anything up for serialization if we need
        public void OnBeforeSerialize()
        {
            _alphaMasksKeys.Clear();
            _alphaMasksVals.Clear();
            foreach (MATERIAL_SLOT slot in alphaMasks.Keys)
            {
                Texture2D value = alphaMasks[slot];
                _alphaMasksKeys.Add(slot);
                _alphaMasksVals.Add(value);
            }
        }

        //regenerate all the fields we couldn't deserialize automatically
        public void OnAfterDeserialize()
        {
            alphaMasks.Clear();
            for (int i = 0; i < _alphaMasksKeys.Count;i++)
            {
                MATERIAL_SLOT slot = _alphaMasksKeys[i];
                Texture2D tex = _alphaMasksVals[i];
                alphaMasks[slot] = tex;
            }
        }
        #endregion
    }
}
