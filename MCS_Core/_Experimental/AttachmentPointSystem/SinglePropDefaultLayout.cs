using UnityEngine;
using System.Collections;
using MCS.COSTUMING;

/// <summary>
/// Default Layout class for a single Attachment Point. Inherits from APLayout.
/// </summary>
public class SinglePropDefaultLayout : APLayout
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SinglePropDefaultLayout"/> class.
	/// </summary>
	public SinglePropDefaultLayout ()
	{
	}



	/// <summary>
	/// Overridden event.
	/// </summary>
	/// <param name="ap">ap.</param>
	/// <remarks>Unusual for an event to be able to return a value.</remarks>
	override public int OnNeedMaximumVisibleItems (CIattachmentPoint ap)
	{
		return 1;
	}



	/// <summary>
	/// Overridden event - absolute local position
	/// </summary>
	/// <param name="ap">Ap.</param>
	/// <remarks>Unusual for an event to be able to return a value.</remarks>
	override public Vector3 OnPositionForAttachmentPoint (CIattachmentPoint ap)
	{
		return ap.transform.localPosition;
	}



	/// <summary>
	/// Overridden event - absolute local rotation
	/// </summary>
	/// <param name="ap">Ap.</param>
	/// <remarks>Unusual for an event to be able to return a value.</remarks>
	override public Vector3 OnRotationForAttachmentPoint (CIattachmentPoint ap)
	{
		return ap.transform.localEulerAngles;
	}



	/// <summary>
	/// Overridden event - absolute positional offset to localPosition
	/// </summary>
	/// <param name="item">The GameObject Item.</param>
	/// <param name="index">The index.</param>
	/// <remarks>Unusual for an event to be able to return a value.</remarks>
	override public Vector3 OnPositionForItemByIndex (GameObject item, int index)
	{
		return item.transform.localPosition;
	}



	/// <summary>
	/// Overridden event - absolute rotational offset to localRotation
	/// </summary>
	/// <param name="item">The GameObject Item.</param>
	/// <param name="index">The index</param>
	/// <remarks>Unusual for an event to be able to return a value.</remarks>
	override public Vector3 OnRotationForItemByIndex (GameObject item, int index)
	{
		return item.transform.localEulerAngles;
	}



}
