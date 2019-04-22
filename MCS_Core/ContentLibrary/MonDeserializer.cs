using System;
using MCS.FOUNDATIONS;
using UnityEngine;
using System.IO;
using MCS.Item;

using MCS.Utility.Schematic;

namespace M3D_DLL
{
	public class MonDeserializer
	{

		/// <summary>
		/// Deserializes the mon file. Path needs to be within the assets folder
		/// </summary>
		/// <returns>The mon file.</returns>
		/// <param name="path">Path.</param>
		public AssetSchematic[] DeserializeMonFile(string path){

			string revisedPath = path.Replace ("Assets", "");

			string result = "";

			try
			{
                string streamPath = Application.dataPath + revisedPath;
                using (FileStream fs = new FileStream(streamPath,FileMode.Open))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        string line;

                        while ((line = sr.ReadLine()) != null)
                        {
                            result += line;
                        }
                    }
                }
			}
			catch(Exception e)
			{
                Debug.LogError("Caught exception while processing mon file: " + path);
                Debug.LogException(e);
			}

			AssetSchematic[] schematics = null;
			if (result != null) {

                //try a multi one first
                try
                {
                    schematics = AssetSchematic.CreateArrayFromJSON(result);
                    if (schematics != null && schematics.Length <= 0)
                    {
                        schematics = null;
                    }
                } catch(Exception e)
                {
                }


                if (schematics == null)
                {
                    //try a single one
                    try
                    {
                        AssetSchematic schematic = AssetSchematic.CreateFromJSON(result);
                        schematics = new AssetSchematic[1];
                        schematics[0] = schematic;
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError("Unable to parse mon file: " + path);
                        UnityEngine.Debug.LogException(e);
                    }
                }
			}
			return schematics;
		}
	}
}

