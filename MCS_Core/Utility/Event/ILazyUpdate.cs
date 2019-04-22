using System;
namespace MCS.Utility.Event
{
	public delegate void BeforeLazyUpdate();
	public delegate void AfterLazyUpdate();

	interface IlazyUpdate
	{
		event BeforeLazyUpdate OnBeforeLazyUpdate;
		event AfterLazyUpdate OnAfterLazyUpdate;
	}
}

