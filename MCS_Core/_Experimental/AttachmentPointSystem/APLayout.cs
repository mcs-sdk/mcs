using UnityEngine;
using System.Collections;
using MCS.COSTUMING;

[System.Serializable]
/// <summary>
/// Attachment Point layout base class inherits from MonoBehaviour
/// </summary>
public class APLayout : MonoBehaviour
{
	/// <summary>
	/// Initializes a new instance of the <see cref="APLayout"/> class.
	/// </summary>
	public APLayout ()
	{
	}



	/// <summary>
	/// Overrideable event - Unknown use.
	/// </summary>
	/// <param name="ap">ap.</param>
	/// <remarks>Unusual for an event to be able to return a value.</remarks>
	virtual public int OnNeedMaximumVisibleItems (CIattachmentPoint ap)
	{
		return 0;
	}



	/// <summary>
	/// Overrideable event - absolute local position
	/// </summary>
	/// <param name="ap">Ap.</param>
	/// <remarks>Unusual for an event to be able to return a value.</remarks>
	virtual public Vector3 OnPositionForAttachmentPoint (CIattachmentPoint ap)
	{
		return new Vector3();
	}



	/// <summary>
	/// Overrideable event - absolute local rotation
	/// </summary>
	/// <param name="ap">Ap.</param>
	/// <remarks>Unusual for an event to be able to return a value.</remarks>
	virtual public Vector3 OnRotationForAttachmentPoint (CIattachmentPoint ap)
	{
		return new Vector3();
	}



	/// <summary>
	/// Overrideable event - absolute positional offset to localPosition
	/// </summary>
	/// <param name="item">The GameObject Item.</param>
	/// <param name="index">The index.</param>
	/// <remarks>Unusual for an event to be able to return a value.</remarks>
	virtual public Vector3 OnPositionForItemByIndex (GameObject item, int index)
	{
		return new Vector3();
	}



	/// <summary>
	/// Overrideable event - absolute rotational offset to localRotation
	/// </summary>
	/// <param name="item">The GameObject Item.</param>
	/// <param name="index">The index</param>
	/// <remarks>Unusual for an event to be able to return a value.</remarks>
	virtual public Vector3 OnRotationForItemByIndex (GameObject item, int index)
	{
		return new Vector3();
	}



}
