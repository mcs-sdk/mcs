using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// AttachmentPointConfiguration class. Inherits from ScriptableObject
/// </summary>
public class MCSAttachmentPointConfiguration : ScriptableObject
{
	/// <summary>
	/// List of AttachmentPoint presets.
	/// </summary>
	public List<AttachmentPointPreset> attachmentPointPresets;



	/// <summary>
	/// Initializes a new instance of the <see cref="MCSAttachmentPointConfiguration"/> class.
	/// </summary>
	public MCSAttachmentPointConfiguration ()
	{
		if (attachmentPointPresets == null)
			attachmentPointPresets = new List<AttachmentPointPreset> ();
	}



	/// <summary>
	/// AttachmentPoint preset class, contains all the data for a single Attachment Point
	/// </summary>
	[System.Serializable]
	public class AttachmentPointPreset
	{
		[SerializeField]public string targetBone;
		public Vector3 positionOffset;
		public Vector3 rotationOffset;
		public bool autoMirror = true;
		public string layoutClassName;
	}



}
