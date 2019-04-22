using System;
namespace MCS.Utility.Serialize
{
	public delegate void SerializeToJson();
	public delegate void DeserializeFromJson();

	interface ISerializable
	{
		void SerializeStateToJson();
		void DeserializeStateFromJson();
	}

}
