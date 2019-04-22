using UnityEngine;
using System.Collections;
using MCS.COSTUMING;

	
public class SpiralPropLayout : APLayout {

	int starting_angle = -45;
	int angle_spacing = 15;

	override public int OnNeedMaximumVisibleItems(CIattachmentPoint ap){
		return 6;
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
		return new Vector3(0f,0f,starting_angle + (index*angle_spacing));
	}
	
	 public Vector3 OnPositionOffsetForItemByIndex (GameObject item, int index){
		return new Vector3();
	}
	 public Vector3 OnRotationOffsetForItemByIndex (GameObject item, int index){
		return new Vector3(0f,0f,starting_angle + (index*angle_spacing));
	}


	
}
