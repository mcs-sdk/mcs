using System;
namespace MCS.Utility.Event
{
	public delegate void BeforeLazyLateUpdate();
	public delegate void AfterLazyLateUpdate();

	interface IlazyLateUpdate
	{
		event BeforeLazyLateUpdate OnBeforeLazyLateUpdate;
		event AfterLazyLateUpdate OnAfterLazyLateUpdate;
	}
}
