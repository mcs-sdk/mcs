using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MCS.CONSTANTS;
using MCS.FOUNDATIONS;

namespace MCS.COSTUMING
{
    /// <summary>
    /// <para>
    /// The CIprop class contains information on an idividual prop.
    /// Inheriting from CostumeItem, which in turn is a MonoBehaviour; making this class a Component class.
    /// </para>
    /// <para>
    /// Use CharacterManager GetLoadedPropByName to get a reference to a specific CIProp attached to a figure.
    /// Use CharacterManager GetAllLoadedProps or GetAllAttachedProps to get a List<CIProp> that contains all loaded or all attached props on the figure.
    /// </para>
    /// <para>
    /// The DLL takes care of attaching this MonoBehaviour as a component to hairs that are attached to 
    /// an MCS Figure either in Editor or through the API via the CharacterManager.
    /// </para>
    /// <para>
    /// GameObjects with a CIProp component that are added to an MCS Figure through the CharacterManager will not be visible until they are added to
    /// a CIAttachementPoint on the figure.
    /// </para>
    /// For attaching hair at runtime see <see cref="MCSCharacterManager.AddContentPack(ContentPack)"/>  <see cref="ContentPack"/> 
    /// </summary>
    [ExecuteInEditMode]
	public class CIprop : CostumeItem
	{
		/// <summary>
		/// the initial position for this prop.
		/// </summary>
		public Vector3 basePosition;



		/// <summary>
		/// The initial rotation for this prop.
		/// </summary>
		public Vector3 baseRotation;



		/// <summary>
		/// Overridden MonoBehaviour Awake event
		/// </summary>
		public override void Awake () 
		{
			initProp ();
		}



		/// <summary>
		/// Overridden MonoBehaviour Start event
		/// </summary>
		public override void Start ()
		{
			initProp ();
		}



		/// <summary>
		/// Internal method, called on instance creation and start.
		/// </summary>
		void initProp ()
		{
			// Debug.Log ("INIT PROP");
			if (LODlist == null)
				LODlist = new List<CoreMesh> ();

			if (LODlist.Count <= 0) {
				CoreMesh[] meshes = gameObject.GetComponentsInChildren<CoreMesh>();
				foreach (CoreMesh mesh in meshes) {
					AddCoreMeshToLODlist (mesh);
				}
			}
		}



	}
}
