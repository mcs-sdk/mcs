using MCS.FOUNDATIONS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCS.FOUNDATIONS
{
    /// <summary>
    /// A group of morphs tracked by their LocalName.
    /// Used to organize Morphs in the <see cref="MCSCharacterManager"/> editor script
    /// into convenient groups.
    /// </summary>
    public class MorphGroup
    {
        /// <summary>
        /// A sub <see cref="MorphGroup"/> that is a child of this MorphGroup.
        /// </summary>
        public Dictionary<string, MorphGroup> SubGroups;
        /// <summary>
        /// List of Morph Names that are direct children of this MorphGroup
        /// </summary>
        public Dictionary<string, string> Morphs;
        /// <summary>
        /// True if this MorphGroup is currently open in the Unity Editor.
        /// </summary>
        public bool IsOpenInEditor;
        /// <summary>
        /// Key to key off of
        /// </summary>
        public string Key;

        public MorphGroup(string key)
        {
            SubGroups = new Dictionary<string, MorphGroup>();
            Morphs = new Dictionary<string, string>();
            Key = key;
            IsOpenInEditor = false;
        }
    }
}
