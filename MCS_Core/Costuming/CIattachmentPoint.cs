using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MCS.COSTUMING;
using MCS;

namespace MCS.COSTUMING
{
	/// <summary>
	/// Clothing Item Attachment Point class. Contains all information and methods for controlling an attachment point.
	/// Derives from MonoBehaviour and is a Component class.
	/// </summary>
	[ExecuteInEditMode]
	public class CIattachmentPoint : MonoBehaviour
	{
		/// <summary>
		/// The name of this attachment point.
		/// </summary>
		public string attachmentPointName;



		/// <summary>
		/// The bone that we reference.
		/// </summary>
		public GameObject boneReference;



		/// <summary>
		/// boolean flag denotes if this attachment point is a mirror of another attachment point.
		/// </summary>
		public bool isMirror;



		/// <summary>
		/// The attached list.
		/// </summary>
		public List<CIprop> _attachedList;



		/// <summary>
		/// Internal accessor for the attached list.
		/// </summary>
		/// <value>The attached list.</value>
		private List<CIprop> attachedList
		{
			get {
				if (_attachedList == null)
					_attachedList = new List<CIprop> ();
				return _attachedList;
			}
		}



		// private bool isQuitting = false;



		/// <summary>
		/// boolean flag denoting whether or not to show this in the editor.
		/// </summary>
		[System.NonSerialized]
		public bool showInEditor = false;



		/// <summary>
		/// Internal. Our Attachment Point layout reference.
		/// </summary>
		private APLayout _layoutObject;



		/// <summary>
		/// Internal. boolean flag to denote if we need a layout update.
		/// </summary>
		private bool layoutNeedsUpdate = true;



		/// <summary>
		/// The maximum number of visible items allowed.
		/// </summary>
		public int MaxVisibleItems = 1;



		//layout deletegate methods
		public delegate int NeedMaxVisibleItems(CIattachmentPoint ap);
		public event NeedMaxVisibleItems OnNeedMaxVisibleItems;

		public delegate Vector3 PositionForAttachmentPoint(CIattachmentPoint ap);
		public event PositionForAttachmentPoint OnPositionForAttachmentPoint;

		public delegate Vector3 RotationForAttachmentPoint(CIattachmentPoint ap);
		public event RotationForAttachmentPoint OnRotationForAttachmentPoint;

		
		public delegate Vector3 PositionForItemByIndex(GameObject item, int index);
		public event PositionForItemByIndex OnPositionForItemByIndex;

		public delegate Vector3 RotationForItemByIndex(GameObject item, int index);
		public event RotationForItemByIndex OnRotationForItemByIndex;



		/// <summary>
		/// Sets the layout object.
		/// </summary>
		/// <param name="layout_object">Layout object.</param>
		public void setLayoutObject (APLayout layout_object)
		{
			if (_layoutObject != null) {
				//remove events
				this.OnNeedMaxVisibleItems -= _layoutObject.OnNeedMaximumVisibleItems;
				this.OnPositionForAttachmentPoint -= _layoutObject.OnPositionForAttachmentPoint;
				this.OnRotationForAttachmentPoint -= _layoutObject.OnRotationForAttachmentPoint;
				this.OnPositionForItemByIndex -= _layoutObject.OnPositionForItemByIndex;
				this.OnRotationForItemByIndex -= _layoutObject.OnRotationForItemByIndex;
			}

			_layoutObject = layout_object;
			//init delegate methods on object
			this.OnNeedMaxVisibleItems += _layoutObject.OnNeedMaximumVisibleItems;
			this.OnPositionForAttachmentPoint += _layoutObject.OnPositionForAttachmentPoint;
			this.OnRotationForAttachmentPoint += _layoutObject.OnRotationForAttachmentPoint;
			this.OnPositionForItemByIndex += _layoutObject.OnPositionForItemByIndex;
			this.OnRotationForItemByIndex += _layoutObject.OnRotationForItemByIndex;

			invalidateLayout ();
		}



		/// <summary>
		/// Invalidates the layout, so that it needs updating.
		/// </summary>
		public void invalidateLayout ()
		{
			layoutNeedsUpdate = true;
		}



		/// <summary>
		/// Internal method to update the layout.
		/// </summary>
		void ReDrawLayout ()
		{
			if (OnNeedMaxVisibleItems != null) {
				MaxVisibleItems = OnNeedMaxVisibleItems (this);
			}
			
			if (OnPositionForAttachmentPoint != null) {
				Vector3 ap_postion = OnPositionForAttachmentPoint (this);
				transform.localPosition = ap_postion;
			}
			
			if (OnRotationForAttachmentPoint != null) {
				Vector3 ap_rotation = OnRotationForAttachmentPoint (this);
				transform.localEulerAngles = ap_rotation;
			}
			
			int i = 0;
			foreach (CIprop prop in attachedList) {
				if (OnPositionForItemByIndex != null) {
					Vector3 item_position = OnPositionForItemByIndex (prop.gameObject, i);
					prop.transform.localPosition = item_position;
				}
				
				if (OnRotationForItemByIndex != null) {
					Vector3 item_rotation = OnRotationForItemByIndex (prop.gameObject, i);
					prop.transform.localEulerAngles  = item_rotation;
				}
				
				i++;
			}
		}



		/// <summary>
		/// MonoBehaviour Start event.
		/// </summary>
		void Start ()
		{ 
			// Debug.Log("START");
			PrepEvents ();

			if (boneReference == null)
				boneReference = transform.parent.gameObject;
			attachmentPointName = transform.name;
			// Ask the layouts for a layout (redraw layout)
		}



		/// <summary>
		/// Internal method to prepare all events.
		/// </summary>
		void PrepEvents ()
		{
			// if we don't have a alayout, then this fails. the importer should do this
			APLayout layout = gameObject.GetComponent<APLayout>();
			if (layout != null)
				setLayoutObject (gameObject.GetComponent<APLayout>());
		}



		/// <summary>
		/// MonoBehaviour Awake event.
		/// </summary>
		void Awake()
		{
		}
		


		/// <summary>
		/// UnityEditor DrawGizmos event.
		/// </summary>
		void OnDrawGizmos()
		{
			if (showInEditor == true) {
				Gizmos.color = Color.blue;
				Gizmos.DrawWireCube (transform.position, new Vector3(.04f,.04f,.04f));
			}
		}



		/// <summary>
		/// MonoBehaviour Update event.
		/// </summary>
		void Update ()
		{
			if (layoutNeedsUpdate == true) {
				if (_layoutObject == null) {
					PrepEvents ();
				} else {
					if (Application.isEditor == true && Application.isPlaying == false) {
						setLayoutObject (_layoutObject);
					}
				}

				// call delegate methods
				ReDrawLayout ();
				layoutNeedsUpdate = false;
			}
		}



		/// <summary>
		/// Returns a boolean denoting whether or not the attachment list is at full capacity.
		/// </summary>
		/// <returns><c>true</c>, if capacity reached, <c>false</c> otherwise.</returns>
		public bool isFull ()
		{
			if (attachedList.Count < MaxVisibleItems)
				return false;
			return true;
		}



		/// <summary>
		/// Returns the current attachment list as an array of type CIprop.
		/// </summary>
		/// <returns>The attachment array.</returns>
		public CIprop[] getAttachmentArray ()
		{
			return attachedList.ToArray ();
		}



		/// <summary>
		/// Adds the given CIprop to the attachment array, with optional cloning of the prop.
		/// </summary>
		/// <param name="_prop">The CIprop to add.</param>
		/// <param name="_cloneGameobject">boolean flag denoting whether or not to clone the prop.</param>
		public void AddProp(CIprop _prop, bool _cloneGameobject)
		{
			CIprop cloneProp = _prop;

			if (_cloneGameobject) {
				GameObject clone = Instantiate (_prop.gameObject);
                clone.name = clone.name.Replace("(Clone)", "(Attached)");
				cloneProp = clone.GetComponentInChildren<CIprop> ();
				attachedList.Add (clone.GetComponentInChildren<CIprop>());
				//printAPList ();
			} else {
				attachedList.Add (_prop);
			}

			cloneProp.transform.SetParent (transform);
			cloneProp.transform.localPosition = cloneProp.basePosition; // Vector3.zero;
			cloneProp.transform.localEulerAngles = cloneProp.baseRotation; // Quaternion.identity;

			if (attachedList.Count > MaxVisibleItems)
				RemoveProp ();

			invalidateLayout ();
		}



		/// <summary>
		/// Adds the given CIprop to the attachment array, with explicit cloning of the prop.
		/// </summary>
		/// <param name="_prop">Property.</param>
		public void AddProp (CIprop _prop)
		{
			AddProp (_prop, true);
		}



		/// <summary>
		/// Removes a CIprop from a given position in the current attachment list.
		/// </summary>
		/// <param name="_position">The index position in the attachment list.</param>
		/// <param name="_destroy">Boolean flag denoting whether or not to destroy the removed prop.</param>
		public void RemoveProp (int _position, bool _destroy)
		{
			if (attachedList.Count > 0 && (_position + 1) <= attachedList.Count) {
				if (_destroy) {
					GameObject _temp = attachedList[_position].gameObject;
					attachedList.RemoveAt (_position);
					if (Application.isEditor)
						DestroyImmediate (_temp);
					else
						Destroy (_temp);
				} else {
					attachedList.RemoveAt (_position);
				}
			}
		}



		/// <summary>
		/// Removes the first CIprop from the current attachment list.
		/// </summary>
		public void RemoveProp()
		{
			RemoveProp(0, true);
		}



		/// <summary>
		/// Removes a CIprop from the current attachment list.
		/// </summary>
		/// <param name="_prop">The CIprop.</param>
		/// <param name="_destroy">Boolean flag denoting whether or not to destroy the removed prop.</param>
		public void RemoveProp(CIprop _prop, bool _destroy)
		{
			if (attachedList.Contains (_prop))
				RemoveProp (attachedList.IndexOf (_prop),_destroy);
		}


		
		/// <summary>
		/// Removes a CIprop from the current attachment list.
		/// </summary>
		/// <param name="_prop">The string prop name.</param>
		/// <param name="_destroy">Boolean flag denoting whether or not to destroy the removed prop.</param>
		public void RemoveProp(string _prop, bool _destroy)
		{
			CIprop prop = GetPropByName (_prop);
			if (prop != null)
				RemoveProp (attachedList.IndexOf (prop),_destroy);
		}



		/// <summary>
		/// Moves a CIprop to another attachment point.
		/// </summary>
		/// <param name="_prop">The CIprop to move.</param>
		/// <param name="_ap">The target CIattachmentPoint.</param>
		public void MoveProp(CIprop _prop, CIattachmentPoint _ap)
		{
			printAPList ();
			if (attachedList.Contains (_prop)) {
				_ap.AddProp (_prop,false);
				RemoveProp (_prop,false);
			}
		}

	

		/// <summary>
		/// Attaches the given CIprop to the attachment point. Returns true on success.
		/// </summary>
		/// <returns><c>true</c>, if property was attached, <c>false</c> otherwise.</returns>
		/// <param name="prop">The CIprop to attach.</param>
		public bool AttachProp (CIprop prop)
		{
			if (!attachedList.Contains (prop)) {
				prop.transform.SetParent (transform);
				prop.transform.localPosition = Vector3.zero;
				prop.transform.localRotation = Quaternion.identity;
				attachedList.Add (prop);
				return true;
			}
			return false;
		}



		/// <summary>
		/// Returns a boolean result denoting whether or not a prop by the given prop name is currently attached.
		/// </summary>
		/// <returns><c>true</c> if the prop is attached; otherwise, <c>false</c>.</returns>
		/// <param name="propName">The prop name.</param>
		public bool IsPropAttached (string propName)
		{
			foreach (CIprop prop in attachedList) {
				if (prop == null)
					continue;
				if (prop.ID == propName)
					return true;
			}
			return false;
		}



		/// <summary>
		/// Returns a List of type string of all prop names that are currently attached.
		/// </summary>
		/// <returns>The all attached property names.</returns>
		public List<string> GetAllAttachedPropNames ()
		{
			List<string> retList = new List<string> ();
			foreach (CIprop prop in attachedList)
				retList.Add (prop.ID);
			return retList;
		}



		/// <summary>
		/// Returns a CIprop from the attached list matching with a given propName.
		/// </summary>
		/// <returns>The CIprop reference or null.</returns>
		/// <param name="propName">The prop name.</param>
		public CIprop GetPropByName (string propName)
		{
			foreach (CIprop prop in attachedList) {
				if (prop.ID == propName)
					return prop;
			}
			return null;
		}
		

		
		/// <summary>
		/// Purely debug method for printing out the attached props to the console.
		/// </summary>
		internal void printAPList ()
		{
			string blah = name + " list: ";
			foreach (CIprop p in attachedList) {
				blah += "[" + attachedList.IndexOf(p).ToString() + "]" + p.dazName + ", ";
			}
			Debug.Log(blah);
		}



		/// <summary>
		/// Cleans the list of attached props.
		/// </summary>
		public void cleanAttachedList ()
		{
			attachedList.Remove (null);
		}



		/// <summary>
		/// Detects the attached props, includes cleanup detection of lost items.
		/// </summary>
		public void DetectAttachedProps ()
		{
			// Debug.Log ("autodetecting props on attachment points");
			CIprop[] attached_props = gameObject.GetComponentsInChildren<CIprop> (true);
			bool needs_new_layout = false;

			foreach (CIprop prop in attached_props) {
				if (attachedList.Contains(prop) == false) {
					prop.isAttached = true;
					attachedList.Add (prop);
					needs_new_layout = true;
				}
			}

			cleanAttachedList ();

			if (needs_new_layout)
				invalidateLayout ();
		}



	}
}
