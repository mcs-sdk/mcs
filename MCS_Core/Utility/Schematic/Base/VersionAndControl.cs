using System;
namespace MCS.Utility.Schematic.Base
{
	[Serializable]
	public class VersionAndControl
	{
		#region Version
		public float mcs_version;
		public float mcs_revision;
		public float collection_version;
		public float item_version;
		#endregion
		#region Control
		[ATEditable]
		[ATInputText]
		public string[] compatibilities;
		#endregion
	}
}

