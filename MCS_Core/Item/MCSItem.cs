using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using MCS;

using MCS.Utility.Event;
using MCS.Utility.Serialize;

namespace MCS.Item
{

	public class MCSItem : MonoBehaviour, IlazyUpdate, IlazyLateUpdate, ISerializable
	{

		public event AfterLazyUpdate OnAfterLazyUpdate;
		public event BeforeLazyUpdate OnBeforeLazyUpdate;
		public event BeforeLazyLateUpdate OnBeforeLazyLateUpdate;
		public event AfterLazyLateUpdate OnAfterLazyLateUpdate;

		bool needs_lazy_update = false;
		bool needs_lazy_late_update = false;


		public void Update()
		{
			if (needs_lazy_update)
			{
				if (OnBeforeLazyUpdate != null)
					OnBeforeLazyUpdate();
				//do some processing
				if (OnAfterLazyUpdate != null)
					OnAfterLazyUpdate();
			}
		}

		public void LateUpdate()
		{
			if (needs_lazy_late_update)
			{
				if (OnBeforeLazyLateUpdate != null)
					OnBeforeLazyLateUpdate();
				//do some processing
				if (OnAfterLazyLateUpdate != null)
					OnAfterLazyLateUpdate();
			}
		}

		public void SerializeStateToJson()
		{
			
		}

		public void DeserializeStateFromJson()
		{
			
		}

	}
}
