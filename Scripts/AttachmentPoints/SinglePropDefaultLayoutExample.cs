using UnityEngine;
using System.Collections;
using MCS.COSTUMING;


public class SinglePropDefaultLayoutExample : APLayout {

	public SinglePropDefaultLayoutExample(){

	}

	override public int OnNeedMaximumVisibleItems(CIattachmentPoint ap)
	{
		return 1;
	}
	override public Vector3 OnPositionForAttachmentPoint (CIattachmentPoint ap){
		return new Vector3();
	}
	override public Vector3 OnRotationForAttachmentPoint (CIattachmentPoint ap){
		return new Vector3();
	}
	
	override public Vector3 OnPositionForItemByIndex (GameObject item, int index){
		return new Vector3();
	}
	override public Vector3 OnRotationForItemByIndex (GameObject item, int index){
		return new Vector3();
	}

}
