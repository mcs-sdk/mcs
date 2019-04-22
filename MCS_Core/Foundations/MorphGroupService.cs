using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

namespace MCS.FOUNDATIONS
{
    /// <summary>
    /// In charge of handling <see cref="MorphGroup"/>s  for the <see cref="MCSCharacterManager"/> editor script.
    /// </summary>
	public class MorphGroupService
	{
		private static Dictionary<string, MorphGroup> morphGroups = new Dictionary<string, MorphGroup>();

		public static void GenerateMorphGroupsFromFile(string name)
		{
			var assetpath = getAssetPath(name);
			var filePath = GetFilePath(assetpath);
			if (MorphGroupFileExists(assetpath))
			{
				morphGroups[name] = new MorphGroup(name);
				var fileData = File.ReadAllText(filePath);
				var lines = fileData.Split("\n"[0]);
				GenerateSubgroupsFromLines(lines, name);
			}
		}

		public static MorphGroup GetMorphGroups(string name)
		{
			MorphGroup group = null;
			if (!morphGroups.ContainsKey(name))
			{
				var assetpath = getAssetPath(name);
				GenerateMorphGroupsFromFile(name);
			}
			group = morphGroups[name];
			return group;
		}

		public static bool MorphGroupFileExists(string assetpath)
		{
			var filePath = GetFilePath(assetpath);
			return File.Exists(filePath);
		}

		private static string GetFilePath(string assetpath)
		{
			return assetpath.Substring(0, assetpath.LastIndexOf("/")) + "/MorphGroups.csv";
		}

		private static string getAssetPath(string name)
		{
			return Application.dataPath + "/MCS/Content/" + name + "/Figure/" + name + "/";
		}

		private static void GenerateSubgroupsFromLines(string[] lines, string gameObjectName)
		{
			foreach (var line in lines)
			{
				var lineHasMultipleGroups = line.Contains('"');
				if (lineHasMultipleGroups)
				{
					GenerateMultipleSubgroupsFromLine(line, gameObjectName);
				}
				else
				{
					GenerateSubgroupFromLine(line, gameObjectName);
				}
			}
		}

		private static void GenerateSubgroupFromLine(string line, string gameObjectName)
		{
			var lineData = line.Trim().Split(',');
			var key = lineData[0];
			var display = lineData[1];
			var groupHierarchy = lineData[2].Trim().Split('/');
			var group = GetGroup(groupHierarchy, gameObjectName);
			group.Morphs[key] = display;
		}

		private static void GenerateMultipleSubgroupsFromLine(string line, string gameObjectName)
		{
			var lineData = line.Trim().Split('"');
			var keyAndDisplay = lineData[0].Split(',');
			var key = keyAndDisplay[0];
			var display = keyAndDisplay[1];
			var groups = lineData[1].Split(',');
			foreach (var groupName in groups)
			{
				var groupHierarchy = groupName.Trim().Split('/');
				var group = GetGroup(groupHierarchy, gameObjectName);
				group.Morphs[key] = display;
			}
		}

		private static MorphGroup GetGroup(string[] groupHierarchy, string gameObjectName)
		{
			var currentGroup = morphGroups[gameObjectName];
			while (groupHierarchy.Length > 0)
			{
				var groupName = groupHierarchy[0];
				if (!currentGroup.SubGroups.ContainsKey(groupName))
				{
					currentGroup.SubGroups[groupName] = new MorphGroup(groupName);
				}
				currentGroup = currentGroup.SubGroups[groupName];
				groupHierarchy = groupHierarchy.Skip(1).ToArray();
			}
			return currentGroup;
		}
	}
}
