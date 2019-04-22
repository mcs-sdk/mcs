using System;
using UnityEngine;

namespace M3D_DLL
{
	public class MCSObjectLoader
	{
		public float progress;

		public void Complete(object obj, float timeToLoad){
			MCSGameObjectEventAgs args = new MCSGameObjectEventAgs ();
			args.MCSObject = obj;
			args.TimeToLoadInMilliseconds = timeToLoad;
			DownloadComplete (args);
		}

		protected virtual void DownloadComplete(MCSGameObjectEventAgs e)
		{
			EventHandler<MCSGameObjectEventAgs> handler = OnDownloadComplete;
			if (handler != null) {
				handler (this, e);
			}
		}

		public event EventHandler<MCSGameObjectEventAgs> OnDownloadComplete;
	}

	public class MCSGameObjectEventAgs : EventArgs
	{
		public object MCSObject { get; set; }
		public float TimeToLoadInMilliseconds { get; set; }
	}

}

