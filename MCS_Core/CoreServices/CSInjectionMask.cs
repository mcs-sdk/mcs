//using UnityEngine;
//using System.Collections;
//
//namespace MCS.CORESERVICES
//{
//	[HideInInspector]
//	internal class CSInjectionMask : MonoBehaviour
//	{
//		public Texture2D injection_map;
//		public CSAlphaInjection injection_manager;
//		public bool has_registered = false;
//		private bool isQuitting = false;
//
//		IEnumerator fetchAlphaInjector ()
//		{
//			yield return new WaitForSeconds (0.01f);
//			injection_manager = transform.root.gameObject.GetComponentInChildren<CSAlphaInjection> ();
//			if (injection_manager == null)
//				BindToFigure ();
//			else
//				registerWithAlphaInjector ();
//		}
//
//		void registerWithAlphaInjector ()
//		{
//			injection_manager.AddAndActivateMask (injection_map);
//		}
//
//		//if the clothing, or you just added it to a figure, you can call this to get it to inject alphas n what not.
//		public void BindToFigure ()
//		{
//			StartCoroutine (fetchAlphaInjector ());
//		}
//			
//		void OnEnable ()
//		{
//			if (injection_manager == null)
//				BindToFigure ();
//			else if (has_registered == false)
//				registerWithAlphaInjector ();
//		}
//			
//		void OnApplicationQuit ()
//		{
//			isQuitting = true;
//		}
//			
//		void OnDisable ()
//		{
//			if (injection_manager != null && !isQuitting)
//				injection_manager.RemoveAndDeactivateMask (injection_map);
//		}
//	}
//}
//
