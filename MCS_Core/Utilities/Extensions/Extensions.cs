using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MCS.UTILITIES.EXTENSIONS
{
	/// <summary>
	/// Dictionary extension class.
	/// </summary>
	public class DictionaryExtensions
	{
		/// <summary>
		/// Find method extension for Dictionary class. Returns the TValue if found, or the default for type TValue if not found.
		/// </summary>
		/// <returns>A value of type TValue</returns>
		/// <param name="source">The TKey,TValue Dictionary to inspect.</param>
		/// <param name="key">The key to find, of type TKey.</param>
		/// <typeparam name="TKey">The type of TKey.</typeparam>
		/// <typeparam name="TValue">The type f TValue</typeparam>
		public static TValue Find<TKey,TValue>(Dictionary<TKey,TValue> source, TKey key)
		{
			TValue value;
			source.TryGetValue(key, out value);
			return value;
		}
	}



}
