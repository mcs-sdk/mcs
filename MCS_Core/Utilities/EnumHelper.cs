using System;

namespace MCS.Utilities
{
	public static class EnumHelper
	{
		//we cast string to enum of type by the grace of stack overflow : http://stackoverflow.com/questions/13970257/casting-string-to-enum
		public static T ParseEnum<T>(string value)
		{
			//we should always have "unkown" be the first value for an enum so we can add a default here, and sue a simplified default chekck
			//rather than a potential red error, weird null, undefined, etc
			return (T)Enum.Parse(typeof(T), value, ignoreCase: true);
		}

	}
}

