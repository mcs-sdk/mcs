using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;

using MCS;
using MCS.CONSTANTS;
using MCS.FOUNDATIONS;
using MCS.UTILITIES;
using MCS.COSTUMING; //TODO: only used for 1.5
using MCS_Utilities.MorphExtraction;
using MCS_Utilities.Morph;
using MCS_Utilities;


using System.Reflection;
using System.Runtime.Serialization;
//using MCS_Utilities.MorphExtraction.Structs;

//Used on Android for reading APK
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

/**
 * Notes:
 * 
 * The morph files will end up residing in the following path
 * Assets/StreamingAssets/{VENDOR_NAME}/{COLLECTION_NAME}/{ITEM_NAME}/{SUBITEM_NAME}/{MORPH_NAME}.morph -- individual morphs not packed into single file
 *  or
 * Assets/StreamingAssets/MCS/{COLLECTION_NAME}/{ITEM_NAME}/{SUBITEM_NAME}.morphs.mr -- all morphs packed per lod into a single file
 * 
 * eg:
 * Assets/StreaminAssets/MCS/CiaoBella/CBPants/LOD0/FBMHeavy.morph
 * Assets/StreaminAssets/MCS/CiaoBella/CBPants/LOD0.morphs.mr
 * 
 */

namespace MCS.SERVICES
{
    /// <summary>
    /// Handles extracting and storing the morphs from the StreamingAssets folder.
    /// Normally a developer would not need to use this class directly. Instead use the
    /// <see cref="MCSCharacterManager"/> and <see cref="CoreMorphs"/> classes to attach morphs. 
    /// </summary>
    public class StreamingMorphs
    {

        /// <summary>
        /// Where do we store our extracted morph data (the blendshapes), the "." is so unity ignores these files
        /// </summary>
        public string streamableMorphEditorPath = "Assets/MCS/MorphData/.Extracted";

		//we'll make use of this in the future as an optimization technique to prevent having to install all the moprhs in the resources folder, this works but for ease of 1.5 we're not going to ship it
		public bool isUsingExtractedPath = false;

        public string extension = "morph"; //foo.morph

        public bool silentFailureWhenMissingMorphFile = true; //should we silently fail when injecting a morph?

        protected static string _streamingAssetsPath = null;
        protected static string _persistentAssetsPath = null;
        protected static string _streamingAssetsOverridePath = null;

        protected static readonly System.Object _lock = new System.Object();

        protected static Dictionary<int,HashSet<string>> failureCache = new Dictionary<int, HashSet<string>>();
        public static void PurgeFailureCache() { failureCache.Clear(); }


        //This needs to be tripped during a main thread execution
        public static void LoadMainThreadAssetPaths()
        {
            if (_streamingAssetsPath == null)
            {
                _streamingAssetsPath = Application.streamingAssetsPath;
            }
            if (_persistentAssetsPath == null)
            {
                _persistentAssetsPath = Application.persistentDataPath;
            }

            //even if we already set _streamingAssetsPath, allow the override to replace the default location
            if (!string.IsNullOrEmpty(_streamingAssetsOverridePath))
            {
                _streamingAssetsPath = _streamingAssetsOverridePath;
            }
        }

        /// <summary>
        /// Used to override the default streaming assets path, it defaults to Application.streamingAssetsPath
        /// </summary>
        public static void OverrideDefaultSourcePath(string path)
        {
            _streamingAssetsOverridePath = path;
            _streamingAssetsPath = (string.IsNullOrEmpty(path) ? Application.streamingAssetsPath : path);
        }

        /// <summary>
        /// Returns the current import folder path for streaming morphs
        /// </summary>
        public static string GetDefaultSourcePath()
        {
            LoadMainThreadAssetPaths();
            return _streamingAssetsPath;
        }



        public static int MaxPoolSize = 32;
        public static Dictionary<string,PoolEntry> Pool = new Dictionary<string,PoolEntry>();
        public struct PoolEntry
        {
            public int slot;
            public MCSResource resource;
        }
        public static int PoolSlot = 0;

        /// <summary>
        /// Where do we store our copied morph data used for runtime (this needs to be in a Resources folder for builds)
        /// </summary>

		//TODO: this is only applicable for 1.5+ releases, once 2.0 ships (not-backwards compatible) we should remove this as we don't need to test it
		protected static Dictionary<string,bool> usesSideCar = new Dictionary<string,bool>();

		//TODO: this is ONLY FOR 1.5 release to support backwards compatible (prevent us from injecting/clearing blendshapes)
		protected void CheckForSideCarSupport(GameObject obj){
			CoreMesh[] cms = obj.GetComponentsInChildren<CoreMesh> ();

			foreach(CoreMesh cm in cms){
				HasSideCarSupport (cm);
			}
		}
		protected bool HasSideCarSupport(CoreMesh cm,string extraKey=null){
			if (cm == null) {
				return false;
			}
			if (cm.runtimeMorphPath.Length > 0) {
				usesSideCar [cm.runtimeMorphPath] = true;
			} else {
				usesSideCar [cm.runtimeMorphPath] = false;
			}

			if (extraKey != null) {
				usesSideCar[extraKey] = usesSideCar [cm.runtimeMorphPath];
			}

			//Debug.Log ("usesSideCar: " + cmmd.geometryId + " => "  + (usesSideCar [cmmd.geometryId] ? "true" : "false"));
			return usesSideCar [cm.runtimeMorphPath];
		}

		protected bool HasSideCarSupport(SkinnedMeshRenderer smr){
			CoreMesh tmp = smr.GetComponent<CoreMesh> ();
			if (tmp == null) {
				return false;
			}

			return HasSideCarSupport (tmp);
		}

        

		public string GetMorphRuntimePathFromGameObject(GameObject obj){
			CoreMesh coreMesh = obj.GetComponent<CoreMesh> ();
			if (coreMesh == null) {
				return null;
			}

			if (coreMesh.runtimeMorphPath.Length <= 0) {
				Debug.LogWarning ("Can't find proper runtimeMorphPath for obj: " + obj.name);
				return null;
			}

			return coreMesh.runtimeMorphPath;
		}

        public Dictionary<string,float> GetAllAttachedBlendshapesFromMesh(Mesh m)
        {
            Dictionary<string,float> blendshapes = new Dictionary<string, float>();

            int blendshapeCount = m.blendShapeCount;
            for(int i=0;i< blendshapeCount; i++)
            {
                //we only have frameindex of 0 currently...

                //GetBlendShapeFrameWeight only exists for unity 5.3+
                blendshapes.Add(m.GetBlendShapeName(i), m.GetBlendShapeFrameWeight(i, 0));
                //blendshapes.Add(m.GetBlendShapeName(i), 0f); //for backwards compatible, let's start at 0
            }

            return blendshapes;
        }

		/// <summary>
		/// Typically used to convert the original skinned mesh renderer's name and explode it out
		/// It will look like: CBTop.Shape_LOD0, we'll split it in two based on the "." and return "Shape_LOD0" which we consider the "sub name"
		/// </summary>
		/// <returns>The item sub name from full name.</returns>
		/// <param name="name">Name.</param>
		public string GetItemSubNameFromFullName(string name){
			int pos = name.IndexOf (".");
			if (pos > -1) {
				return PurifyKey(name.Substring (pos + 1));
			}
			return null;
		}
		public string GetItemNameFromFullName(string name){
			int pos = name.IndexOf (".");
			if (pos > -1) {
				return PurifyKey(name.Substring (0,pos));
			}
			return null;
		}
		/// <summary>
		/// Clean a key string to only the characters we want to pay attention to
		/// </summary>
		/// <returns>The key.</returns>
		/// <param name="dirty">Dirty.</param>
		public string PurifyKey(string dirty){
			Regex regex = new Regex ("[^a-zA-Z0-9_ ]");
			//strip any "optimized" reference
			dirty = dirty.Replace ("_OPTIMIZED", "");
			return regex.Replace (dirty,"");
		}

        /// <summary>
        /// Parses and loads a manifest file that contains things like the blendshapes available for a mesh
        /// </summary>
        /// <param name="meshName"></param>
        /// <param name="runtime"></param>
        /// <returns></returns>
        public MorphManifest GetManifest(string meshKey, bool runtime=true)
        {
            MorphManifest manifest = new MorphManifest();

			string filePath = _streamingAssetsPath + "/MCS/" + meshKey + "/manifest.json";

			//found it
			if (System.IO.File.Exists (filePath)) {
				string text = System.IO.File.ReadAllText (filePath);
				manifest = JsonUtility.FromJson<MorphManifest> (text);
				return manifest;
			}

			//rebuild it
			Debug.LogWarning ("Unable to find manifest for: " + meshKey + " rebuilding");
			manifest = RebuildManifest (meshKey);

			return manifest;
        }

		protected MorphManifest RebuildManifest(string meshKey){
			string basePath = _streamingAssetsPath;
			basePath += "/" + "MCS/" + meshKey;

			string fileName = basePath + "/manifest.json";


			MorphManifest manifest = new MorphManifest ();

			int tmp = meshKey.LastIndexOf ('/');
			string manifestName = meshKey;
			if (tmp >= 0) {
				manifestName = meshKey.Substring (tmp + 1);
			}

			HashSet<string> morphNames = new HashSet<string> ();

			List<string> names = new List<string> ();

			string[] paths = System.IO.Directory.GetFiles (basePath, "*.morph*",SearchOption.AllDirectories);
		
			foreach (string path in paths) {
				//skip unity meta files
				if (path.EndsWith (".meta")) {
					continue;
				}
				if (path.Contains (".mr")) {
					//resource file
					MCSResource resource = new MCSResource();
					resource.Read (path, false);

					foreach (string key in resource.header.Keys) {
						names.Add (key);
					}
					continue;
				}

				//loose file
				names.Add(path);
			}

			foreach (string name in names) {
				string key = name;
				int pos = 0;

				//convert foo/bar/car.morph to car.morph
				pos = name.LastIndexOf ('/');
				if (pos >= 0) {
					key = key.Substring (pos + 1);
				}

				//convert car.morph to car
				pos = key.IndexOf (".");
				if (pos >= 0) {
					key = key.Substring (0,pos);
				}

				morphNames.Add (key);
			}

			manifest.count = morphNames.Count;
			manifest.name = meshKey;
			manifest.names = new string[manifest.count];

			int i = 0;
			foreach (string name in morphNames) {
				manifest.names [i++] = name;
			}

			manifest.WriteToDisk (fileName);

			return manifest;
		}

        //retrieves the blendshape state from disk and returns a copy in memory
        public MorphData GetMorphDataFromDisk(string name, bool runtime = true, bool compressed = false, bool nameIsPath = false)
        {
            //file in editor land is gzipped, in resources it's not

            //runtime also has to have a ".bytes" extension... HAS TO, it's stupid... (well or a .txt... lol)
            string filePath = (runtime ? "" : streamableMorphEditorPath + "/") + name + ".morph" + (compressed ? ".gz" : "");

            if (nameIsPath)
            {
                filePath = name;
            }

            MorphData morphData;

            Stream streamFile;

            if (!File.Exists(filePath))
            {
                Debug.LogError("GetMorphDataFromDisk failed, src file missing: " + filePath);
                throw new UnityException("Unable to fetch morph file from extracted");
            }
            streamFile = File.OpenRead(filePath);
            

            //uncompress the stream
            if (compressed)
            {
                Stream decompressed = Compression.DecompressStream(streamFile);
                streamFile.Close();
                streamFile = decompressed;
            }

            morphData = MorphData.ConvertStreamToMorphData(streamFile);
            streamFile.Close();

            return morphData;
        }

        public delegate void OnPostGetMorphData(MorphData md);
        public delegate void OnPostInjectionMorphs(Dictionary<int, InjectMorphNamesIntoFigureAsyncResult> result);
        public void GetMorphDataFromResourcesAsync(string basePath, string morphName, OnPostGetMorphData callback=null)
        {
            GetMorphDataFromResourcesThread t = new GetMorphDataFromResourcesThread(basePath, morphName, callback);
            System.Threading.Thread child = new System.Threading.Thread(new System.Threading.ThreadStart(t.ThreadRun));
            try
            {
                child.Start();
            } catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                callback(null);
            }
        }

        public class GetMorphDataFromResourcesThread
        {
            public string basePath;
            public string morphName;
            public OnPostGetMorphData callback;
            //global lock between parsing
            public static readonly System.Object _lock = new System.Object();

            public GetMorphDataFromResourcesThread(string basePath, string morphName, OnPostGetMorphData callback = null)
            {
                this.basePath = basePath;
                this.morphName = morphName;
                this.callback = callback;
            }

            public void ThreadRun()
            {
                StreamingMorphs sm = new StreamingMorphs();
                //TODO: kind of a bad place for a lock, but it doesn't require too much refactoring
                // we need to be careful of messing with file streams 
                //lock (_lock)
                //{
                    MorphData md = sm.GetMorphDataFromResources(basePath, morphName);
                    if (callback != null)
                    {
                        callback(md);
                    }
                //}
            }
        }

        //retrieves the blendshape state from disk and returns a copy in memory
        public MorphData GetMorphDataFromResources(string basePath, string morphName)
        {
            MorphData morphData = null;
            byte[] bytes = null;

            //if the didn't specify the extension, add it now
            if (morphName.LastIndexOf(".morph") == -1)
            {
                morphName += ".morph";
            }

            //ARTIST TOOLS specific override
            string filePath = (basePath[0] == '!') ? Path.Combine(basePath.Substring(1), morphName) : Path.Combine(basePath, morphName);
            string streamingPath = Path.Combine(_streamingAssetsPath, filePath);
            
            /**
             * Operation order:
             *  1) Check if it's in streaming assets as a standalone .morph file
             *  2) Check if it's in streaming assets as a mr file
             */

            MCSResource mr = null;

			string directFilePath = null;
			if (File.Exists(filePath))
			{
				//Debug.Log("FILE EXISTS");
				directFilePath = streamingPath;
			}
			// IS THIS A TYPO? BOTH CONDITIONS ARE SAME.
			else if (File.Exists(streamingPath))
			{
				directFilePath = streamingPath;
			}

			if (directFilePath != null)
			{
				bytes = File.ReadAllBytes(directFilePath);
			}





            if (bytes == null) {
                //let's see if we have it in a mr anywhere starting from the top folder

                streamingPath = streamingPath.Replace(@"\", "/");
                int pos;
                pos = streamingPath.LastIndexOf(@"/");


                string directoryPath = streamingPath.Substring(0, pos);
                pos = directoryPath.LastIndexOf(@"/");
                string itemName = directoryPath.Substring(pos + 1);
                string dir = directoryPath.Substring(0, pos);
                string path = dir + "/" + itemName + ".morphs.mr"; //Application.streamingAssetsPath + "/" + dir + "/" + itemName + ".morphs.mr";

                string morphRelativePath = morphName;

                //Convert "StarterPacks/Genesis3Male/Shape/Aged_Posture.morph.gz.bytes to Shape/Aged_Posture.morph.gz if the morphs.mr.bytes file exists in StarterPacks/Genesis3Male

                //if we can find the file on disk it's a lot faster as we don't have to load everything into memory
                //UnityEngine.Debug.Log("Resource file exists at: " + path);
                try
                {
                    PoolEntry entry;
                    if (Pool.TryGetValue(path, out entry))
                    {
                        mr = entry.resource;
                        entry.slot = PoolSlot++;
                    }
                    else
                    {
                        //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                        //watch.Start();
                        if (System.IO.File.Exists(path))
                        {
                            mr = new MCSResource();
                            mr.Read(path, false);
                            //watch.Stop();

                            entry = new PoolEntry();
                            entry.slot = PoolSlot++;
                            entry.resource = mr;

                            Pool.Add(path, entry);

                            if (Pool.Count > MaxPoolSize)
                            {
                                string removeKey = null;
                                int minSlot = int.MaxValue;

                                foreach (string key in Pool.Keys)
                                {

                                    if (Pool[key].slot < minSlot)
                                    {
                                        minSlot = Pool[key].slot;
                                        removeKey = key;
                                    }
                                }

                                if (removeKey != null)
                                {
                                    Pool.Remove(removeKey);
                                }


                            }
                        }
                    }

                    if (mr != null)
                    {

                        //UnityEngine.Debug.Log("Watch A: " + watch.ElapsedMilliseconds);
                        //watch.Reset();
                        //watch.Start();
                        bytes = mr.GetResource(morphRelativePath, true);
                        if (bytes == null)
                        {
                            return null;
                        }
                        //watch.Stop();
                        //UnityEngine.Debug.Log("Watch B: " + watch.ElapsedMilliseconds);
                        //watch.Reset();
                        //watch.Start();
                        morphData = MorphData.ConvertBytesToMorphData(bytes);
                        //watch.Stop();
                        //UnityEngine.Debug.Log("Watch C: " + watch.ElapsedMilliseconds);
                        return morphData;
                    }
                }
                catch (Exception e)
                {
                    if (!silentFailureWhenMissingMorphFile)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                    return null;
                }
            }


			if(bytes == null && PlatformRequiresPersistentMorphs())
			{
				string customPath = basePath + ".morphs.mr";

				try
				{
					LoadStreamFromMR(customPath);
					if (_openZipStream != null)
					{
						//we found the file inside the apk
						mr = new MCSResource();
						mr.Read(_openZipStream);
						bytes = mr.GetResource(morphName);
						morphData = MorphData.ConvertBytesToMorphData(bytes);
						return morphData;
					}
				} catch (Exception e)
				{
					UnityEngine.Debug.LogException(e);
				}

				return null;
			}

            if (bytes == null || bytes.Length<=0)
            {
                if (!silentFailureWhenMissingMorphFile)
                {
                    Debug.LogError("GetBlendshapeStateFromResources, can't locate: " + filePath);
                }
                return null;

                //throw new UnityException("Unable to fetch morph file from streaming assets");
            }

            try
            {
                morphData = MorphData.ConvertBytesToMorphData(bytes);
            }
            catch (Exception e)
            {
                Debug.Log("IN THE SILENT EXCEPTION: \n" + e.ToString());
                if (!silentFailureWhenMissingMorphFile)
                {
                    Debug.LogError("GetBlendshapeStateFromResources, corrupted: " + filePath);
                }
                throw;
            }

            //Debug.Log("GOT TO END WITH THIS DATA: " + morphData.ToString());

            return morphData;
        }
        
        //retrieves the blendshape state from disk and returns a copy in memory
        [Obsolete("Please switch to MorphData")]
        public MCS_Utilities.MorphExtraction.Structs.BlendshapeState GetBlendshapeStateFromResources(string basePath, string morphName)
        {
            MCS_Utilities.MorphExtraction.Structs.BlendshapeState bsIn;
            byte[] bytes;

			//if the didn't specify the extension, add it now
			if (morphName.LastIndexOf (".moprh.gz") == -1) {
				morphName += ".morph.gz";
			}

			string filePath = basePath + "/" + morphName;
            //UnityEngine.Debug.Log("filePath: " + filePath);


            /**
             * Operation order:
             *  1) Check if it's in Unity.Resources
             *  2) Check if it's in a mr system file
             *  3) Check if it's in a mr embedded in a Unity.Resource
             */

            TextAsset asset = Resources.Load(filePath) as TextAsset;

            if(asset == null)
            {
                //let's see if we have it in a mr
                //List<string> paths = new List<string>();
                int pos = basePath.LastIndexOf('/');
                while (pos > -1)
                {
                    string dir = basePath.Substring(0, pos);
                    string path = dir + "/morphs.mr";
                    //Convert "StarterPacks/Genesis3Male/Shape/Aged_Posture.morph.gz.bytes to Shape/Aged_Posture.morph.gz if the morphs.mr.bytes file exists in StarterPacks/Genesis3Male
                    string morphRelativePath = filePath.Replace(dir, "").TrimStart('/');

                    //UnityEngine.Debug.Log("Morph Relative Path: " + morphRelativePath);

                    //UnityEngine.Debug.Log("Checking path: " + path);

                    if (System.IO.File.Exists(path+".bytes"))
                    {
                        //UnityEngine.Debug.Log("Resource file exists at: " + path);

                        try
                        {
                            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                            watch.Start();
                            MCSResource mr = new MCSResource();
                            mr.Read(path + ".bytes", false);
                            UnityEngine.Debug.Log("Watch A: " + watch.ElapsedMilliseconds);
                            //UnityEngine.Debug.Log("Read complete");
                            bytes = mr.GetResource(morphRelativePath+".bytes");
                            UnityEngine.Debug.Log("Watch B: " + watch.ElapsedMilliseconds);
                            //UnityEngine.Debug.Log("Byte count: " + bytes.Length);
                            bsIn = MCS.Utility.MCSResourceConverter.CompressedToBlendshapeState(ref bytes);
                            UnityEngine.Debug.Log("Watch C: " + watch.ElapsedMilliseconds);
                            UnityEngine.Debug.Log("bsIn: " + bsIn.name);
                            return bsIn;
                        }catch(Exception e)
                        {
                            UnityEngine.Debug.LogException(e);
                            throw;
                        }
                        break;
                    }

                    pos = basePath.LastIndexOf('/', (pos-1));

                    asset = Resources.Load(path) as TextAsset;
                }
            }

			if (asset == null || asset.bytes.Length <= 0) {
                if (!silentFailureWhenMissingMorphFile)
                {
                    Debug.LogError("GetBlendshapeStateFromResources, can't locate: " + filePath);
                }

                throw new UnityException("Unable to fetch morph file from resources");
			}

            bytes = asset.bytes;


            try
            {
                bsIn = DecompressAndConvertBytesToBlendshapeState(ref bytes);
            }catch(Exception e)
            {
                if (!silentFailureWhenMissingMorphFile)
                {
                    Debug.LogError("GetBlendshapeStateFromResources, corrupted: " + filePath);
                }
                throw;
            }


            return bsIn;
        }

        [Obsolete("Please switch to MorphData")]
        public static MCS_Utilities.MorphExtraction.Structs.BlendshapeState DecompressAndConvertBytesToBlendshapeState(ref byte[] bytes)
        {
            Stream streamFile = new MemoryStream(bytes);
            BinaryFormatter serializer = new BinaryFormatter();

            //decompress the stream

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            Stream decompressed = Compression.DecompressStream(streamFile);
            streamFile.Close();
            UnityEngine.Debug.Log("Decompression: " + watch.ElapsedMilliseconds);

            streamFile = decompressed;
            serializer.Binder = new MCS_Utilities.MorphExtraction.Structs.CurrentAssemblyDeserializationBinder();
            //serializer.Binder = new MCS.FOUNDATIONS.CurrentAssemblyDeserializationBinderLegacy();
            MCS_Utilities.MorphExtraction.Structs.BlendshapeStateCereal bscIn = (MCS_Utilities.MorphExtraction.Structs.BlendshapeStateCereal)serializer.Deserialize(streamFile);
            streamFile.Close();
            UnityEngine.Debug.Log("Deserialize: " + watch.ElapsedMilliseconds);

			if (bscIn.name == null || bscIn.name == "") {
				throw new UnityException("Morph file is corrupted");
			}

            MCS_Utilities.MorphExtraction.Structs.BlendshapeState bsIn = bscIn.Extract();
            UnityEngine.Debug.Log("Cereal to state: " + watch.ElapsedMilliseconds);
            watch.Stop();
            return bsIn;
        }

        [Obsolete("Please switch to MorphData")]
        public static MCS_Utilities.MorphExtraction.Structs.BlendshapeState ConvertBytesToBlendshapeState(ref byte[] bytes)
        {
            Stream streamFile = new MemoryStream(bytes);
            BinaryFormatter serializer = new BinaryFormatter();
            serializer.Binder = new MCS_Utilities.MorphExtraction.Structs.CurrentAssemblyDeserializationBinder();
            MCS_Utilities.MorphExtraction.Structs.BlendshapeStateCereal bscIn = (MCS_Utilities.MorphExtraction.Structs.BlendshapeStateCereal)serializer.Deserialize(streamFile);
            streamFile.Close();

			if (bscIn.name == null || bscIn.name == "") {
				throw new UnityException("Morph file is corrupted");
			}

            MCS_Utilities.MorphExtraction.Structs.BlendshapeState bsIn = bscIn.Extract();
            return bsIn;
        }

        public string GetCompressedPathNameFromMesh(string meshName)
        {
            string baseName = GetBlendshapeNamePrefixFromMeshName(meshName);
            string pathName = meshName + "/" + baseName;
            return pathName;
        }

        /// <summary>
        /// Converts a name like G2FSimplifiedEyes_394.Shape_LOD0 to G2FSimplifiedEyes_394
        /// </summary>
        /// <param name="meshName"></param>
        /// <returns></returns>
        public string GetBlendshapeNamePrefixFromMeshName(string meshName) { 
            int pos = meshName.IndexOf(".");

			//HACK: one off for incorrect micahhairf blendshape names
			int symbolPos = meshName.IndexOf("MicahF_CAP");
			if (symbolPos > -1) {
				return "MicahFCAP";
			}

            string baseName = meshName.Substring(0, pos);
            return baseName;
        }

        public bool InjectMorphDatasIntoFigure(GameObject root, MorphData[] morphDatas)
        {
            SkinnedMeshRenderer[] smrs = root.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (SkinnedMeshRenderer smr in smrs)
            {
                foreach (MorphData md in morphDatas)
                {
                    InjectMorphDataIntoSMR(smr, md);
                }

                //UNITYBUG: unity has a stale state of the sharedMesh blendshape array, this is a workaround that fixes that issue, note this does not cause any memory issue, I checked in the profiler
                Mesh m = smr.sharedMesh;
                smr.sharedMesh = m;
            }

            return true;
        }

        /// <summary>
        /// It's not recommended to use this directly as there is a bug with the "stale" state of a smr.sharedMesh, which you need to reassign with the new mesh from it.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="bs"></param>
        /// <returns></returns>
        protected bool InjectMorphDataIntoMesh(Mesh m, MorphData morphData)
        {
            try
            {
                BlendshapeData bd = morphData.blendshapeData;
				Debug.LogWarning("InjectBlendshapeIntoMesh should not be used directly...");
                m.AddBlendShapeFrame(bd.name, 100f, bd.deltaVertices, bd.deltaNormals, bd.deltaTangents);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Couldn't inject morph: " + morphData.name);
                throw (e);
            }

            return true;
        }

        public bool InjectMorphDataIntoSMR(SkinnedMeshRenderer smr, MorphData morphData)
        {
            try
            {
				//TODO: remove for 2.0
				if(!HasSideCarSupport(smr)){
					return false;
				}

                BlendshapeData bd = morphData.blendshapeData;

                //inject the blendshape into the smr
                if(bd.deltaVertices.Length != smr.sharedMesh.vertexCount)
                {
                    Debug.LogError("Vertex counts do not match between blendshape: " + bd.name + " (" + bd.deltaVertices.Length + ") are receiver: " + smr.name + " (" + smr.sharedMesh.vertexCount + ")");
                    return false;
                }

                smr.sharedMesh.AddBlendShapeFrame(bd.name, 100f, bd.deltaVertices, bd.deltaNormals, bd.deltaTangents);
            }
            catch (Exception e)
            {
                //this could be the case where we already have this blendshape loaded... TODO: we should do a second check to see if this is a true error
                Debug.LogWarning("Couldn't inject blendshape: " + morphData.name);
                Debug.LogException(e);
                //throw (e);
                return false;
            }

            return true;
        }

        public struct InjectMorphNamesIntoFigureAsyncResult
        {
            public int id;
            public SkinnedMeshRenderer smr;
            public List<MorphData> morphDatas;
            public List<string> morphs;
        }

        public bool InjectMorphNamesIntoFigureAsync(GameObject root, string[] morphs, bool compressed = false, OnPostInjectionMorphs callback=null)
        {
            //UnityEngine.Debug.Log("About to inject: " + root.name);

            SkinnedMeshRenderer[] smrs = root.GetComponentsInChildren<SkinnedMeshRenderer>();
            Dictionary<int, InjectMorphNamesIntoFigureAsyncResult> result = new Dictionary<int, InjectMorphNamesIntoFigureAsyncResult>();

            int count = 0;
            foreach (SkinnedMeshRenderer smr in smrs)
            {
                if (!smr.enabled)
                {
                    continue;
                }
                int id = smr.GetInstanceID();
                InjectMorphNamesIntoFigureAsyncResult res = new InjectMorphNamesIntoFigureAsyncResult();
                res.id = id;
                res.smr = smr;
                res.morphDatas = new List<MorphData>();
                res.morphs = new List<string>();
                foreach (string morph in morphs)
                {
                    count++;
                    res.morphs.Add(morph);
                }

                result[id] = res;
            }

            //UnityEngine.Debug.Log("Need to inject: " + count);

            if (count <= 0 && callback != null)
            {
                callback(result);
                return true;
            }

            foreach (SkinnedMeshRenderer smr in smrs)
            {
                if (!smr.enabled)
                {
                    continue;
                }
                InjectMorphNamesIntoFigureAsyncResult res = result[smr.GetInstanceID()];
				string basePath = GetMorphRuntimePathFromGameObject (smr.gameObject);

                foreach (string morph in morphs)
                {
                    //does this blendshape already exist, if so just continue and ignore it, and don't lookup from disk
					int blendshapeIndex = smr.sharedMesh.GetBlendShapeIndex(morph);
                    if (blendshapeIndex >= 0)
                    {
                        count--; //already installed
                        //UnityEngine.Debug.Log("Count C: " + count);
                        if (count <= 0 && callback != null)
                        {
                            callback(result);
                        }
                        continue;
                    }

                    try
                    {

                        //UnityEngine.Debug.Log("Fetching: " + morph + " For: " + smr.name);
                        GetMorphDataFromResourcesAsync(basePath, morph,(MorphData morphData)=> {
                            count--;

                            lock (_lock)
                            {
                                res.morphDatas.Add(morphData);
                            }

                            //UnityEngine.Debug.Log("Count A: " + count);
                            if (count <= 0 && callback != null)
                            {
                                callback(result);
                            }

                        });

                    }
                    catch (Exception e)
                    {
                        count--;
                        //UnityEngine.Debug.Log("Count B: " + count);
                        if (count <= 0 && callback != null)
                        {
                            callback(result);
                        }
                        if (!silentFailureWhenMissingMorphFile)
                        { 
                            Debug.LogWarning("Couldn't locate morph file in resources for: " + smr.name + " morph: " + morph);
                            Debug.LogException(e);
                            throw;
                        }

                        continue;
                    }

                }

            }


            return true;
        }

        public bool SkipCheck(int instanceId, string morphName)
        {
            return failureCache.ContainsKey(instanceId) && failureCache[instanceId].Contains(morphName);
        }

        /// <summary>
        /// Loads up a blendshape into a gameobject
        /// </summary>
        /// <param name="smr"></param>
        /// <param name="bs"></param>
        /// <returns>True on success, false otherwise</returns>
        public bool InjectMorphNamesIntoFigure(GameObject root, string[] morphs, bool compressed = false)
        {
            SkinnedMeshRenderer[] smrs = root.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (SkinnedMeshRenderer smr in smrs)
            {
                if (!smr.enabled)
                {
                    continue;
                }
				string basePath = GetMorphRuntimePathFromGameObject (smr.gameObject);

                foreach (string morph in morphs)
                {
                    //does this blendshape already exist, if so just continue and ignore it, and don't lookup from disk
					int blendshapeIndex = smr.sharedMesh.GetBlendShapeIndex(morph);
                    if (blendshapeIndex >= 0)
                    {
                        continue;
                    }

                    MorphData morphData = null;

                    int smrId = smr.GetInstanceID();

                    //we already know we can't find this
                    if (SkipCheck(smrId, morph))
                    {
                        continue;
                    }

                    try
                    {
                        morphData = GetMorphDataFromResources(basePath, morph);
                    }
                    catch (Exception e)
                    {
                        if (!silentFailureWhenMissingMorphFile)
                        { 
                            Debug.LogException(e);
                            //throw;
                        }

                        continue;
                    }

                    if (morphData != null)
                    {


                        if ( morphData.name.CompareTo(morph) != 0)
                        {
                            //TODO: remove this hack
                            if(morphData.name.EndsWith("NEGATIVE") && !morphData.name.EndsWith("_NEGATIVE_"))
                            {
                                morphData.name = morphData.name.Replace("NEGATIVE", "_NEGATIVE_");
                                morphData.blendshapeData.name = morphData.name;
                            } else
                            {
                                Debug.LogWarning("Morph name mismatch, requesting name is: " + morph + ", found name is: " + morphData.blendshapeData.name + " in: " + basePath + "/" + morph + ", please report to MCS");
                            }

                            //does the name we're about to use exist?
                            blendshapeIndex = smr.sharedMesh.GetBlendShapeIndex(morphData.blendshapeData.name);
                            if (blendshapeIndex >= 0)
                            {
                                continue;
                            }
                        }
                        InjectMorphDataIntoSMR(smr, morphData);
                    } else
                    {
                        if (!failureCache.ContainsKey(smrId))
                        {
                            failureCache.Add(smrId, new HashSet<string>());
                        }
                        failureCache[smrId].Add(morph);

                        if (!silentFailureWhenMissingMorphFile)
                        {
                            Debug.LogWarning("Failed to locate a morph for: " + smr.name + " morph: " + morph);
                        }
                    }
                }

                //UNITYBUG: unity has a stale state of the sharedMesh blendshape array, this is a workaround that fixes that issue, note this does not cause any memory issue, I checked in the profiler
                Mesh m = smr.sharedMesh;
                smr.sharedMesh = m;
            }

            return true;
        }

        public void RemoveAllBlendshapesFromFigure(GameObject root)
        {
            SkinnedMeshRenderer[] smrs = root.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (SkinnedMeshRenderer smr in smrs)
            {
                //quick sanity check
                if(smr == null || smr.sharedMesh == null)
                {
                    continue;
                }

                if (smr.sharedMesh.blendShapeCount > 0)
                {
                    //This looks stupid, and it is stupid. This is a fix for a bug in unity where the values need to be reset to 0 before removing the blendshape or you'll crash
                    for(int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
                    {
                        smr.SetBlendShapeWeight(i, 0f);
                    }
                }

                //only do this check if we're in editor non play land
                if (!Application.isPlaying)
                {
                    if (HasSingleSMRReferencesInScene(smr))
                    {

                        //UnityEngine.Debug.Log("Can clear smr: " + smr.name);
                        smr.sharedMesh.ClearBlendShapes();
                    }
                    else
                    {
                        //UnityEngine.Debug.LogError("Can't clear smr: " + smr.name);
                        //In 5.3 doing a ClearBlendshapes when not all 0 will cause a crash, in 5.4 this generates an error message
                    }
                } else
                {
                    smr.sharedMesh.ClearBlendShapes();
                }
            }

        }

        public bool HasSingleSMRReferencesInScene(SkinnedMeshRenderer smr)
        {
            CoreMesh[] coreMeshes = GameObject.FindObjectsOfType<CoreMesh>();

            int id = smr.sharedMesh.GetInstanceID();

            int count = 0;

            foreach(CoreMesh cm in coreMeshes)
            {
                SkinnedMeshRenderer smrCM = cm.GetComponent<SkinnedMeshRenderer>();
                if(smrCM == null)
                {
                    continue;
                }

                if(smrCM.sharedMesh.GetInstanceID() == id)
                {
                    count++;
                    //UnityEngine.Debug.Log("Found: " + smrCM.name + " count: " + count);
                    if (count > 1)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        #region platform specific handling

        //global
        protected static FileStream _openAPKStream = null;
        protected static ZipFile _openZipFile = null;

        //local
        protected ZipEntry _openZipEntry = null;
        protected Stream _openZipStream = null;

        protected void LoadStreamFromMR(string relativePath)
        {
            string apkPath = Application.dataPath;

            bool dirty = false;

            if (_openAPKStream == null)
            {
                _openAPKStream = File.OpenRead(apkPath);
                dirty = true;
            }
            if (dirty || _openZipFile == null)
            {
                _openZipFile = new ZipFile(_openAPKStream);
            }

            string key = "assets/" + relativePath;

            UnityEngine.Debug.Log("About to get entry: " + key);
            ZipEntry _openZipEntry = _openZipFile.GetEntry(key);
            UnityEngine.Debug.Log("Was null: " + (_openZipEntry == null ? "null" : "not null"));

            if (_openZipEntry != null)
            {
                _openZipStream = _openZipFile.GetInputStream(_openZipEntry);
            }
        }

        /// <summary>
        /// Find mr files in streaming assets and copy them to persitent storage (eg: sdcard)
        ///  This is used on platforms like Android so we can use normal disk operations on it
        /// </summary>
        public static string InstallMorphsToPersistentAsync(string relativePath, bool block = true, bool refresh = false)
        {
            string srcPath = _streamingAssetsPath + "/" + relativePath;
            string dstPath = _persistentAssetsPath + "/" + relativePath;

            //already installed
            if(!refresh && File.Exists(dstPath))
            {
                return dstPath;
            }

            GameObject helper = GameObject.Find("StreamingMorphsInstallHelper");
            StreamingMorphsInstallHelper smih = null;
            if (!helper)
            {
                helper = new GameObject();
                smih = helper.AddComponent<StreamingMorphsInstallHelper>();
            } else
            {
                smih = helper.GetComponent<StreamingMorphsInstallHelper>();
            }
            smih.CopyFromURLToPersistent(srcPath, dstPath,block);
            return dstPath;
        }

        public static bool PlatformRequiresPersistentMorphs()
        {
            //only one platform requires this now
            return Application.platform == RuntimePlatform.Android;
        }

        public static string InstallMorpshForPlatformIfNeeded(string path)
        {
            //no need we're not using a platform that we care about
            if (!PlatformRequiresPersistentMorphs())
            {
                return null;
            }

            return InstallMorphsToPersistentAsync(path);
        }

        #endregion



    }

    //need this for www
    public class StreamingMorphsInstallHelper : MonoBehaviour
    {
        public delegate void OnPostFetch(string key);
        public Dictionary<string, byte[]> results = new Dictionary<string, byte[]>();

        public void CopyFromURLToPersistent(string srcFilePath, string dstFilePath, bool block = true)
        {
            string key = System.Guid.NewGuid().ToString();
            if (block) {
                bool waiting = true;
                StartCoroutine(Fetch(key, srcFilePath));

                int tries = 0;
                int maxtries = 1000;

                //here's the bad part, basically a spinlock TODO: we need to update our morph handling to be event driven and not blocking
                while (waiting)
                {

                    if (results.ContainsKey(key))
                    {
                        byte[] bytes = results[key];
                        if (bytes != null && bytes.Length > 0)
                        {
                            int pos = dstFilePath.LastIndexOf("/");
                            string dir = dstFilePath.Substring(0, pos);
                            if (!Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }
                            File.WriteAllBytes(dstFilePath, bytes);
                        }
                        results.Remove(key);
                        waiting = false;
                    }

                    if (waiting)
                    {
                        UnityEngine.Debug.Log("Waiting");
                        //wait 1ms
                        System.Threading.Thread.Sleep(10);
                        tries++;
                    }

                    if (tries > maxtries)
                    {
                        waiting = false;
                    }
                }
            }
        }
        protected IEnumerator Fetch(string key, string srcURL)//, OnPostFetch callback)
        {
            UnityEngine.Debug.Log("srcURL: " + srcURL);
            WWW www = new WWW(srcURL);
            yield return www;

            UnityEngine.Debug.Log("www: " + www.responseHeaders);

            results[key] = www.bytes;
            //callback(key);
        }

    }


    [Serializable]
    public class ProjectionMeshMap
    {
        public sealed class CurrentAssemblyDeserializationBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                return Type.GetType(String.Format("{0}, {1}", typeName, Assembly.GetExecutingAssembly().FullName));
            }
        }

        //for now all we need is a list of vector3s, it's quite possible we'll need more data in the future though
        [SerializeField]
        protected Vector3Spatial[] _vertices;

        [NonSerialized]
        public Vector3[] vertices;

        public void Fill(Vector3[] verts = null)
        {
            if (verts != null)
            {
                vertices = verts;
            }
            _vertices = new Vector3Spatial[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                _vertices[i].Fill(vertices[i], i);
            }
        }

        public Vector3[] Extract()
        {
            vertices = new Vector3[_vertices.Length];
            for (int i = 0; i < _vertices.Length; i++)
            {
                vertices[i] = _vertices[i].Get(ref i);
            }

            return vertices;
        }

        public bool Save(string filePath)
        {
            Stream fs = File.Create(filePath);
            MemoryStream ms = new MemoryStream();

            Fill();
            BinaryFormatter serializer = new BinaryFormatter();
            serializer.Serialize(ms, this);
            ms.Flush();
            ms.Position = 0;

            byte[] bytes = ms.ToArray();

            fs.Write(bytes, 0, bytes.Length);
            fs.Close();
            ms.Close();

            return true;
        }

        public bool Read(string filePath)
        {
            BinaryFormatter serializer = new BinaryFormatter();
            Stream streamFile = File.OpenRead(filePath);

            serializer.Binder = new CurrentAssemblyDeserializationBinder();
            ProjectionMeshMap map = (ProjectionMeshMap)serializer.Deserialize(streamFile);
            streamFile.Close();

            this._vertices = map._vertices;
            this.Extract();

            return true;
        }
        
        public bool IsReMappingNeeded(SkinnedMeshRenderer smr, MorphData md, bool checkMorphVerts = false)
        {
            Mesh mesh = smr.sharedMesh;
            Vector3[] targetVertices = mesh.vertices;
            Vector3[] sourceVertices = Extract();
            if(checkMorphVerts)
            {
                if (targetVertices.Length != md.blendshapeData.deltaVertices.Length)
                    return true;
                else
                    return false; 
            }

            float epsilon = 0.000000001f;
            if (targetVertices.Length != sourceVertices.Length)
                return true; 
            else
            {
                for(int i = 0; i< targetVertices.Length; i++)
                {
                    if ((targetVertices[i] - sourceVertices[i]).sqrMagnitude <= epsilon)
                        continue;
                    else
                        return true; 
                }
                return false; 
            }
        }

        public Dictionary<int, int> GenerateTargetToSourceMap(SkinnedMeshRenderer smr)
        {
            Mesh mesh = smr.sharedMesh;
            Vector3[] targetVertices = mesh.vertices;
            Vector3[] verts = Extract();

            return GenerateTargetToSourceMap(verts, targetVertices);
        }

        public Dictionary<int, int> GenerateTargetToSourceMap(Vector3[] sourceVertices, Vector3[] targetVertices)
        {
            Dictionary<int, int> targetToSourceMap = new Dictionary<int, int>();
            //TODO: O(n^2) performance, we can do better...
            for (int i = 0; i < targetVertices.Length; i++)
            {
                float minDistance = Mathf.Infinity;
                float maxDistance = 0f;
                int index = -1;
                int j = 0;
                for (j = 0; j < sourceVertices.Length; j++)
                {
                    //float delta = (targetVertices[i] - sourceVertices[j]).sqrMagnitude;
                    float delta = Mathf.Abs((targetVertices[i] - sourceVertices[j]).magnitude);
                    if (delta < minDistance)
                    { 
                        index = j;
                        minDistance = delta;
                    }

                    if (delta > maxDistance)
                    {
                        maxDistance = delta;
                    }
                }

                if (index < 0)
                {
                    UnityEngine.Debug.LogError("I: " + i + " j: " + j + " min: " + minDistance + " max: " + maxDistance );
                    throw new Exception("Unable to find a compatible vertex, can't create map");
                }                
                targetToSourceMap[i] = index;
            }
            return targetToSourceMap;            
        }

        //Converts a blendshape from a vert-incompatible/out-of-index blendshape to a compatible one based on closest point
        public MorphData ConvertMorphDataFromMap(SkinnedMeshRenderer smr, MorphData morphData, Dictionary<int, int> targetToSourceMap = null)
        {

            Mesh mesh = smr.sharedMesh;
            Vector3[] targetVertices = mesh.vertices;
            Vector3[] verts = Extract();

            MorphData morphDataNew = new MorphData();
            morphDataNew.name = morphData.name;
            morphDataNew.jctData = morphData.jctData;
            morphDataNew.blendshapeData = new BlendshapeData();
            morphDataNew.blendshapeData.frameIndex = morphData.blendshapeData.frameIndex;
            morphDataNew.blendshapeData.shapeIndex = morphData.blendshapeData.shapeIndex;

            morphDataNew.blendshapeData.deltaVertices = new Vector3[targetVertices.Length];
            morphDataNew.blendshapeData.deltaNormals = new Vector3[targetVertices.Length];
            morphDataNew.blendshapeData.deltaTangents = new Vector3[targetVertices.Length];

            Dictionary<int, int> tsMap = (targetToSourceMap == null) ? GenerateTargetToSourceMap(verts, targetVertices) : targetToSourceMap;             

            foreach(var ts in tsMap)
            {

                if (morphData.blendshapeData.deltaNormals != null)
                {
                    morphDataNew.blendshapeData.deltaNormals[ts.Key] = morphData.blendshapeData.deltaNormals[ts.Value];
                }
                if (morphData.blendshapeData.deltaVertices != null)
                {
                    if(ts.Key >= morphDataNew.blendshapeData.deltaVertices.Length)
                    {
                        throw new Exception("ts.key in: " + smr.name +" is too large for deltas: " + ts.Key + " => " + ts.Value + " | " + morphDataNew.blendshapeData.deltaVertices.Length);
                    }
                    if(ts.Value >= morphData.blendshapeData.deltaVertices.Length)
                    {
                        throw new Exception("ts.value in: " + smr.name + " is too large for deltas: " + ts.Key + " => " + ts.Value +" | " + morphData.blendshapeData.deltaVertices.Length);
                    }
                    morphDataNew.blendshapeData.deltaVertices[ts.Key] = morphData.blendshapeData.deltaVertices[ts.Value];
                }
                if (morphData.blendshapeData.deltaTangents != null)
                {
                    morphDataNew.blendshapeData.deltaTangents[ts.Key] = morphData.blendshapeData.deltaTangents[ts.Value];
                }
            }

            return morphDataNew;
        }
        
    }
}
