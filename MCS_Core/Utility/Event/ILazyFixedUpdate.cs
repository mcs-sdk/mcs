using System;
namespace MCS.Utility.Event
{
	public delegate void BeforeLazyFixedUpdate();
	public delegate void AfterLazyFixedUpdate();

	interface IlazyFixedUpdate
	{
		event BeforeLazyFixedUpdate OnBeforeLazyFixedUpdate;
		event AfterLazyFixedUpdate OnAfterLazyFixedUpdate;
	}
}
