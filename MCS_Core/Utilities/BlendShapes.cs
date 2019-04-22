using UnityEngine;
using System.Collections;
using System;
using System.Text.RegularExpressions;

namespace MCS.UTILITIES{
	/// <summary>
	/// Utility class for formatting BlendShape display names.
	/// </summary>
	public static class BlendShapes {

		/// <summary>
		/// Converts a DisplayName string to a correctly formatted label.
		/// </summary>
		/// <returns>The formatted label string.</returns>
		/// <param name="displayName">The DisplayName string to format.</param>
		public static string convertBlendshapeIDToLabel (string ID)
		{
			
			string prefix = ID.Substring (0, 3);

			string new_suffix = "";
			switch (prefix) {
				
			case "CTR":
				new_suffix = "(Complete)";
				break;
			case "FHM":
				new_suffix = "(Head)";
				break;
			case "FBM":
				new_suffix = "(Body)";
				break;
			case "PHM":
				new_suffix = "(Head)";
				break;
			case "PBM":
				new_suffix = "(Body)";
				break;
			case "CBM":
				new_suffix = "(Complete)";
				break;
			case "SCL":
				new_suffix = "(Proportion)";
				break;
			case "VSM":
				new_suffix = "(Phoneme)";
				break;
				
			}
			string label = ID;
			if (string.IsNullOrEmpty (new_suffix) == false)
				label = ID.Substring (3);
			if (ID.StartsWith ("CTRL"))
				label = ID.Substring (4);
			label = properSpacing (label);
			if (ID.StartsWith ("VSM"))
				label = VSMformat (label);
			//return string.Format("{0} {1}", label, new_suffix);
			return string.Format ("{0}", label);
		}



		/// <summary>
		/// Internal method to ensure correct spacing of a given string.
		/// </summary>
		/// <returns>A correctly spaced string.</returns>
		/// <param name="instr">The string to work with</param>
		private static string properSpacing (string instr)
		{
			var r = new Regex (@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
			
			return r.Replace (instr, " ");
		}



		/// <summary>
		/// Internal method to correctly format a VSM label.
		/// </summary>
		/// <returns>The formatted string.</returns>
		/// <param name="unformattedVSM">The string to format.</param>
		private static string VSMformat (string unformattedVSM)
		{
			string retstr = "";
			char tempchr = ' ';
			if (unformattedVSM.Length == 1) {
				tempchr = unformattedVSM [0];
				retstr = "\"" + tempchr + Char.ToLower (tempchr) + Char.ToLower (tempchr) + "...\"";
			}
			
			if (unformattedVSM.Length > 1) {
				retstr += unformattedVSM [0];
				for (int i = 1; i < unformattedVSM.Length; i++) {
					tempchr = Char.ToLower (unformattedVSM [i]);
					retstr += tempchr;
				}
				retstr = "\"" + retstr + tempchr + "...\"";
			}
			return retstr;
		}



	}
}
