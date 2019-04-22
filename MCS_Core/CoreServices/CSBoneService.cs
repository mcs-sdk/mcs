using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using MCS.UTILITIES.EXTENSIONS;
using MCS.COSTUMING;
using MCS.FOUNDATIONS;
using MCS.CONSTANTS;

namespace MCS.CORESERVICES
{
	/// <summary>
	/// CoreServices BoneService MonoBehaviour Component class - Component utilities for bones
	/// </summary>
	[ExecuteInEditMode]
	public class CSBoneService : MonoBehaviour
	{
		/// <summary>
		/// Local reference to the CharacterManager MonoBehaviour script
		/// </summary>
		private MCSCharacterManager charman;



		/// <summary>
		/// Public flag defining whether or not to show bone position gizmos
		/// </summary>
		public bool showBonePositions = false;



		/// <summary>
		/// Public flag defining whether or not to show attachment point gizmos
		/// </summary>
		public bool showAllAttachmentPoints = false;



		/// <summary>
		/// Internal flag of previous state of show attachments to ensure slow code is not run every frame in gizmos
		/// </summary>
		private bool lastShowAllAttachmentPoints = false;



		/// <summary>
		/// Internal array of all child transforms in this transform. Used exclusively for gizmos
		/// </summary>
		private Transform[] all_children;



		/// <summary>
		/// Internal boneMap dictionary of the bones in this Transform
		/// </summary>
		private BoneUtility.BoneMap _boneMap;



		/// <summary>
		///  Internal accessor for the boneMap dictionary. Creates the boneMap when called the first time or if null.
		/// </summary>
		/// <value>The bone map.</value>
		private BoneUtility.BoneMap BoneMap
		{
			get {
				if (_boneMap == null)
					_boneMap = new BoneUtility.BoneMap (transform);
				return _boneMap;
			}
		}



		/// <summary>
		/// Internal reference to the CIbody component on this GameObject
		/// </summary>
		private CIbody _figure;



		/// <summary>
		/// Internal accessor to return a reference to the CIbody componentn on this GameObject. Retrieves the reference the first time called or if null.
		/// </summary>
		/// <value>The CIbody reference.</value>
		private CIbody figure
		{
			get {
				if (_figure == null)
					_figure = gameObject.transform.parent.gameObject.GetComponentInChildren<CIbody> ();
				return _figure;
			}
		}


		/// <summary>
		/// MonoBehaviour Awake event called at instance instantiation
		/// </summary>
		/// <remarks>Currently unused</remarks>
		void Awake ()
		{
			// BoneMap = new BoneUtility.BoneMap (transform);
		}



		/// <summary>
		/// MonoBehaviour Start event called when the instance is woken up at runtime
		/// </summary>
		void Start ()
		{
			// this is for JCTs nd we only care about JCTs at runtime
			if (Application.isPlaying) {
				//#ROOT HACK
//				charman = transform.root.gameObject.GetComponentInChildren<CharacterManager> ();

				if (charman == null)
					//you should be able to reliably get the character manager with the same call from anywhere in the stack
					charman = gameObject.GetComponentInParent<MCSCharacterManager> ();
//				if (jct == null)
//					//the character manager is the obvious place to get anything deep in the stack because getinchildren works better than get in parent due to sibling relationships
//					jct = charman.GetJctManager ();


				if (charman != null) {
					// add us to the blendshape change event
					charman.OnCMBlendshapeValueChange += OnCMBlendshapeValueChange;
				}
			}
		}



		/// <summary>
		/// Called when the MCSCharaceterManager raises the OnCMBlendshapeValueChange event.
		/// </summary>
		void OnCMBlendshapeValueChange ()
		{
			// Debug.Log ("CAUGHT THE CAHNGE");
		}



		/// <summary>
		/// Public boolean method to compare a given GameObject with this GameObject.
		/// </summary>
		/// <returns><c>true</c> if the given GameObject is this GameObject; otherwise, <c>false</c>.</returns>
		/// <param name="other">Other.</param>
		public bool IsBodyObject (GameObject other)
		{
			return (other == figure.gameObject);
		}



		/// <summary>
		/// Clones a costume item and attaches it to the figure
		/// </summary>
		/// <returns>A GameObject of the cloned costume item.</returns>
		/// <param name="costume_item">The GameObject of the costume item</param>
		public GameObject CloneAndAttachCostumeItemToFigure (GameObject costume_item)
		{
			return GeometryTransferUtility.CloneAndAttachCostumeItemToFigure (costume_item, this.BoneMap,figure,transform);
		}



		/// <summary>
		/// Attaches a costume item to the figure
		/// </summary>
		/// <returns>A GameObject of the costume item.</returns>
		/// <param name="costume_item">The GameObject of the costume item</param>
		public GameObject AttachCostumeItemToFigure (GameObject costume_item)
		{
			return GeometryTransferUtility.AttachCostumeItemToFigure (costume_item, this.BoneMap, figure, transform);
		}



		/// <summary>
		/// Binds a given GameObject geometry to a Transform, matching the former's position and rotation.
		/// </summary>
		/// <returns>The geometry to transform.</returns>
		/// <param name="referenceGeometry">GameObject containing the geometry to bind.</param>
		/// <param name="destination_transform">The destination Tansform.</param>
		private GameObject BindGeometryToTransform (GameObject referenceGeometry, Transform destination_transform)
		{
			return GeometryTransferUtility.BindGeometryToTransform(referenceGeometry, destination_transform);
		}



		/// <summary>
		/// Returns a bone Transform from a given bone name.
		/// </summary>
		/// <returns>The bone's Transform or null if not found.</returns>
		/// <param name="name">The string name of the bone.</param>
		public Transform GetBoneByName (string name)
		{  
			Transform target_bone = null;
			this.BoneMap.TryGetValue (name, out target_bone);

			// this is a hack to fix the new issue where on imprt the bone service bonemap is bad <----------- this hack is because of adding csboneserivce on the wrong import method callback - should be OnPostProcessModel
			// this could mess things up at runtime
			if (target_bone == null) {
				_boneMap = new BoneUtility.BoneMap (transform);
				this.BoneMap.TryGetValue (name, out target_bone);
			}

			return target_bone;
		}

        public Transform GetJawBone()
        {
            Transform t = GetBoneByName("lowerJaw");
            return t;
        }

		/// <summary>
		/// Gets the mirrored bone or null.
		/// </summary>
		/// <returns>The mirrored bone or null.</returns>
		/// <param name="bone">Bone.</param>
		public Transform getMirroredBoneOrNull (Transform bone)
		{
			Transform mirrored_bone = null;
			if (bone.name.StartsWith ("l")) {
				// candidate for a mirror
				string target_bone = "r" + bone.name.Substring (1);
				mirrored_bone = GetBoneByName(target_bone);

			} else if (bone.name.StartsWith ("r")) {
				// candidate for mirror
				string target_bone = "l" + bone.name.Substring (1);
				mirrored_bone = GetBoneByName(target_bone);
			}
			return mirrored_bone;
		}



		/// <summary>
		/// Return a string array of all bone names for this model
		/// </summary>
		/// <returns>The string array</returns>
		public string[] getAllBonesNames ()
		{
			return BoneUtility.getAllBonesNames (this.BoneMap);
		}



		/// <summary>
		/// Unity DrawGizmos event - only fired in the Editor
		/// </summary>
		void OnDrawGizmos ()
		{
			// do we show attachment points (we only run this if different to the last time - as it is quite slow)
			if (lastShowAllAttachmentPoints != showAllAttachmentPoints) {

				// set the previous state of show attachments to the current state of show attachments
				lastShowAllAttachmentPoints = showAllAttachmentPoints;

				// gather a list of all attatchment points (even if disabled) - this is VERY slow and should not be done every frame
				CIattachmentPoint[] points = gameObject.GetComponentsInChildren<CIattachmentPoint> (true);

				// Debug.Log("ATTACH POINTS SHOW:"+points.Length);

				// set each attachment point's flag to the current state of show attachments
				foreach (CIattachmentPoint point in points)
					point.showInEditor = lastShowAllAttachmentPoints;
			}

			// do we show bone positions?
			if (showBonePositions == true) {

				// do we need to create a list of all child transforms? this will happen if any child becomes null (either at the start or due to the test below)
				if (all_children == null) {

					// create the list of all child transforms that does not contain an AttachmentPoint or CoreMesh component
					List<Transform> temp = new List<Transform> ();
					all_children = gameObject.GetComponentsInChildren<Transform> ();
					
					foreach (Transform tf in all_children) {
						if (tf.GetComponent<MCS.COSTUMING.CIattachmentPoint> () == null && tf.GetComponent<MCS.FOUNDATIONS.CoreMesh> () == null)
							temp.Add (tf);
					}

					all_children = temp.ToArray ();
				}

				// set the gizmo color
				Gizmos.color = Color.yellow;

				// draw a gizmo for each bone child, whilst checking to see if any bone has become null
				foreach (Transform tf in all_children) {
					if (tf == null) {
						all_children = null;
						return;
					}
					Gizmos.DrawWireSphere (tf.position, .01f);
				}
			}
		}



	}
}
