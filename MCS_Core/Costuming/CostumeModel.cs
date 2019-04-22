using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MCS.CONSTANTS;
using MCS.FOUNDATIONS;
using MCS.COSTUMING;
using MCS.CORESERVICES;

namespace MCS.SERVICES
{
	/// <summary>
    /// <para>
	/// The Costume model class contains all control functionality for all <see cref="CostumeItem"/>s on a figure.
    /// </para>
    /// <para>
    /// The <see cref="MCSCharacterManager"/> uses an internal instance of CostumeModel class to manage all CostumeItems attatched to it.
    /// Because of this most developers will not need to conern themselves with the CostumeModel class.
    /// </para>
	/// </summary>
	[System.Serializable]
	public class CostumeModel
	{
        /// <summary>
        /// Backing reference for the <see cref="availableItems"/> property. 
        /// </summary>
        public List<CostumeItem> _availableItems;
		/// <summary>
		/// The CostumeItems available on this model.
		/// </summary>
		public List<CostumeItem> availableItems
		{
			get {
				if (_availableItems == null)
					_availableItems = new List<CostumeItem> ();
				return _availableItems;
			}
		}



		/// <summary>
		/// Internal. The force single visible boolean flag.
		/// </summary>
		private bool force_single_visible = false;



		/// <summary>
		/// Delegate for subscribing to the <see cref="OnItemVisibilityDidChange"/> event. 
		/// </summary>
		/// <remarks>Some piece of geometry must change visibility. Currently used by alpha injection on character manager level.</remarks>
		public delegate void ItemVisibilityDidChange(CostumeModel model);
        /// <summary>
        /// Event fired when the visibility of a <see cref="CostumeItem"/> within this <see cref="CostumeModel"/> changes.  
        /// </summary>
        public event ItemVisibilityDidChange OnItemVisibilityDidChange;



		/// <summary>
		/// Broadcasts the visibility change event to subscribers.
		/// </summary>
		void broadcastVisibilityChange ()
		{
			if (OnItemVisibilityDidChange != null)
				OnItemVisibilityDidChange (this);
		}



		/// <summary>
		/// Initializes a new instance of the <see cref="MCS.SERVICES.CostumeModel"/> class.
		/// </summary>
		/// <param name="_force_single_visible">If set to <c>true</c> force single visible.</param>
		public CostumeModel (bool _force_single_visible = false)
		{
			// availableItems = new List<CostumeItem> ();
			force_single_visible = _force_single_visible;
		}



		/// <summary>
		/// Clears all costume items from the costume model.
		/// </summary>
		public void  ClearItems ()
		{
			// Debug.Log ("CKEAIN ITEMS");
			_availableItems = new List<CostumeItem> ();
			availableItems.Clear ();
		}



		/// <summary>
		/// Returns a List of type CostumeItem of all CostumeItems associated with this CostumeModel.
		/// </summary>
		/// <returns>The all items.</returns>
		public List<CostumeItem> GetAllItems ()
		{
			Cleanup ();
			return availableItems;
		}

		/// <summary>
		/// Returns a CostumeItem associated with a given display name.
		/// </summary>
		/// <returns>The CostumeItem or null</returns>
		/// <param name="displayName">The display name.</param>
		public CostumeItem GetItemByName (string id)

		{
			Cleanup ();
			foreach (CostumeItem item in availableItems) {
				if (item.ID == id)
					return item;
			}
			return null;
		}



		/// <summary>
		/// Return a List of type CostumeItem of all currently visible CostumeItems.
		/// </summary>
		/// <returns>The visible items List.</returns>
		public List<CostumeItem> GetVisibleItems ()
		{
			return availableItems.FindAll (item => item != null && item.isVisible);
		}



		/// <summary>
		/// Adds a <see cref="CostumeItem"/>  to the CostumeModel.
		/// </summary>
		/// <param name="item">The CostumeItem.</param>
		public void AddItem (CostumeItem item)
		{
			Cleanup ();
			if (availableItems.Contains (item) == false) {
				availableItems.Add (item);
				// we need to listen to the events of those we watch, in case someone manipulates them
				item.OnCostumeItemVisibilityDidChange += OnCostumeItemVisibilityDidChange;
				item.OnCostumeItemLODDidChange += OnCostumeItemLODDidChange;
				// item.OnCostumeItemLockChange += OnCostumeItemLockChange;
			}
		}



		/// <summary>
		/// Remove a <see cref="CostumeItem"/>  from the CostumeModel.
		/// </summary>
		/// <param name="item">The CostumeItem.</param>
		public void RemoveItem (CostumeItem item)
		{
            if (item != null)
            {
                var key = item.ID;
                for (int i = availableItems.Count - 1; i >= 0; i--)
                {
                    if (availableItems[i].ID.Equals(key))
                    {
                        availableItems.RemoveAt(i);
                    }
                    item.OnCostumeItemVisibilityDidChange -= OnCostumeItemVisibilityDidChange;
                    item.OnCostumeItemLODDidChange -= OnCostumeItemLODDidChange;
                }
            }
            else
            {
                availableItems.Remove(null);
            }
			// item.OnCostumeItemLockChange -= OnCostumeItemLockChange;
			Cleanup ();
		}



        //		public void SetItemLock (string name, bool lock_setting)
        //		{
        //			CostumeItem item = GetItemByName (name);
        //			if (item != null) {
        //				if (item.isLocked != lock_setting) {
        //					item.isLocked = lock_setting;
        //				}
        //			}
        //		}



        /// <summary>
        /// Set the visibility for a <see cref="CostumeItem"/> by name.
        /// </summary>
        /// <param name="name">The string name of the costume item. See <see cref="CostumeItem.ID"/>.</param>
        /// <param name="new_visibility">boolean flag visibility.</param>
        public CostumeItem SetItemVisibility (string name, bool new_visibility)
		{
			CostumeItem item = GetItemByName (name);
			if (item != null) {
				// if(item.isLocked == false && item.isVisible != new_visibility) {
				if (item.isVisible != new_visibility) {
					if (new_visibility == true && force_single_visible == true) {
						foreach (CostumeItem t_item in GetVisibleItems()) {
                            t_item.SetVisibility(false);
						}
					}
                    item.SetVisibility(new_visibility);
				}
			}
            return item;
		}



		/// <summary>
		/// Sets the clothing LOD levels for clothing in the rootClothingSet.
		/// </summary>
		/// <param name="level">Level.</param>
		public void SetItemLODLevel(float level)
		{
			Cleanup ();
			foreach(CostumeItem cicl in availableItems) {
				cicl.setLODLevel(level);
			}
		}



		/// <summary>
		/// Cleanup this CostumeModel instance by removing null
        /// <see cref="CostumeItem"/> and GameObject references from 
        /// <see cref="availableItems"/> 
		/// </summary>
		public void Cleanup ()
		{
            for(int i = availableItems.Count - 1; i >= 0; i--)
            {
                CostumeItem ci = availableItems[i];
                if(ci == null)
                {
                    availableItems.RemoveAt(i);
                    continue;
                }

                if(ci.gameObject == null)
                {
                    availableItems.RemoveAt(i);
                }
            }
		}



		/// <summary>
		/// Prepare this CostumeModel instance for runtime.
		/// </summary>
		public void PrepareForRuntime ()
		{
			// we lose our delegates in serialization, so clear it all
			// Debug.Log ("PREPARING");
			OnItemVisibilityDidChange = null;
			// Debug.Log ("COUNT:" + availableItems.Count);

			foreach (CostumeItem item in availableItems) {
				// Debug.Log("ITEM LSIENGIN");
				item.OnCostumeItemVisibilityDidChange += OnCostumeItemVisibilityDidChange;
				item.OnCostumeItemLODDidChange += OnCostumeItemLODDidChange;
				// item.OnCostumeItemLockChange += OnCostumeItemLockChange;
			}
		}



		/// <summary>
		/// Raise and broadcast the LOD change event for the costume item.
		/// </summary>
		/// <param name="item">The changed CostumeItem.</param>
		void OnCostumeItemLODDidChange (CostumeItem item)
		{
		}



//		void OnCostumeItemLockChange(CostumeItem item)
//		{
//		}



		/// <summary>
		/// Raise and broadcast the visibility change event for the costume item.
		/// </summary>
		/// <param name="item">The changed CostumeItem.</param>
		void OnCostumeItemVisibilityDidChange (CostumeItem item)
		{
			// alpha injection needs to listen to the result of this event
			// Debug.Log ("MODEL CAUGHT ITEM CHANGE");
			broadcastVisibilityChange ();
		}



	}
}
