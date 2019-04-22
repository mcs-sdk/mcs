using System;
namespace MCS.Utility.Schematic.Base
{
	[Serializable]
	public class StreamAndPath
	{
		#region Stream
		public string image_thumbnail;
		public string interactive_thumbnail;
		public string url;
		#endregion
		#region Path
		public string root_path;
		public string source_path;
		public string generated_path;
		#endregion
	}
}

