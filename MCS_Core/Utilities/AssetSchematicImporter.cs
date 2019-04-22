using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using UnityEngine;

using M3D_DLL;
using MCS.SERVICES;
using MCS.CORESERVICES;
using MCS.FOUNDATIONS;
using MCS.COSTUMING;

using MCS.Utility.Schematic;
using MCS.Utility.Schematic.Enumeration;
using MCS.Utility.Schematic.Base;
using MCS.Item;

namespace MCS.UTILITIES
{
    public class AssetSchematicImporter
    {
        // various absolute strings
        private const string ROOT_FOLDER = "Assets/MCS";
        private const string INJECTION_FOLDER = "/InjectionMasks";
        private const string RESOURCES_FOLDER = "Assets/MCS/Resources";
        private const string PREFAB_FOLDER = "Assets/MCS/Prefabs";
        private const string RESOURCES_PREFAB_FOLDER = "Assets/MCS/Resources/Prefabs";
        private const string GENERATED_MATERIALS_FOLDER = "Assets/MCS/Generated Materials";
        private const string RESOURCES_MATERIALS_FOLDER = "Assets/MCS/Resources/Materials";

        public delegate AssetSchematic GetSchematicFromGameObjectDelegate(GameObject go, Dictionary<string, AssetSchematic> schematicLookup);
        public delegate string GetGUIDFromGameObjectDelegate(GameObject go);
        public delegate Texture2D GetTextureFromPathDelegate(string path, string rootPath = null);
        public delegate Material GetMaterialFromGUIDDelegate(string path, string rootPath = null);

        protected GetSchematicFromGameObjectDelegate GetSchematicFromGameObject;
        protected GetGUIDFromGameObjectDelegate GetGUIDFromGameObject;
        protected GetTextureFromPathDelegate GetTextureFromPath;
        protected GetMaterialFromGUIDDelegate GetMaterialFromGUID;

        //protected List<AssetSchematicResource> resources = null;
        protected List<AssetSchematic> schematics = new List<AssetSchematic>();
        //protected Dictionary<string, AssetSchematic> schematicLookup = new Dictionary<string, AssetSchematic>();

        public string basePath; //where is the parent directory of the asset you're loading, this is not always applicable and can be null

        //as we dive into each node, which components will we keep, we'll remove all other components that are not in this list
        protected string[] componentWhiteList =
        {
            "UnityEngine.SkinnedMeshRenderer",
            "UnityEngine.MeshFilter",
            "UnityEngine.MeshRenderer",
            "UnityEngine.Transform",
            "MCS.Item.MCSItemModel",
			"Moprh3d.ArtTools.MCSATSceneNode",
        };


        //setup the default callbacks
        public AssetSchematicImporter(
            GetGUIDFromGameObjectDelegate guidCallback=null,
            GetSchematicFromGameObjectDelegate schematicCallback=null,
            GetTextureFromPathDelegate textureCallback=null,
            GetMaterialFromGUIDDelegate materialCallback=null
        )
        {
            GetGUIDFromGameObject = (guidCallback == null ? GetGUIDFromGameObjectDefault : guidCallback);
            GetSchematicFromGameObject = (schematicCallback == null ? GetSchematicFromGameObjectDefault : schematicCallback);
            GetTextureFromPath = (textureCallback == null ? GetTextureFromPathDefault : textureCallback);
            GetMaterialFromGUID = (materialCallback == null ? GetMaterialFromGUIDDefault : materialCallback);
        }

        //get a mcs id (guid string, not a real guid) from the gameobject by looking at components
        public string GetGUIDFromGameObjectDefault(GameObject go)
        {
            string guid = null;

            //Try to find the guid by an attached component
            MCSProperty property = go.GetComponent<MCSProperty>();
            if (property != null)
            {
                guid = property.mcs_id;
            }

            //is it still null?
            if (guid == null)
            {
                MCSItemModel model = go.GetComponent<MCSItemModel>();
                if (model != null)
                {
                    guid = model.schematic.origin_and_description.mcs_id;
                }
            }

            return guid;
        }

        public string GetPathFromGameObject(GameObject go,bool stripRoot = true)
        {

            string path = "";
            GetNodeTraversalPathFromGOToRoot(null, go, ref path, stripRoot);
            if (String.IsNullOrEmpty(path))
            {
                path = "/";
            }
            return path;
        }

        //How do you retrieve a schmeatic from a gameobject, the default is specific to MCS
        public AssetSchematic GetSchematicFromGameObjectDefault(GameObject go, Dictionary<string, AssetSchematic> schematicLookup)
        {
            MCSItemModel itemModel = go.GetComponent<MCSItemModel>();
            if(itemModel != null)
            {
                return itemModel.schematic;
            }

            //we couldn't find it on the gameobject, check in our dictionary
            AssetSchematic schematic;
            string guid = GetGUIDFromGameObject(go);
            if (guid != null)
            {
                if (schematicLookup.TryGetValue(guid, out schematic))
                {
                    //we found it in our main dictinoary
                    //UnityEngine.Debug.Log("Found Schematic via guid: " + guid + " | " + go.name);
                    return schematic;
                }
            }

            //couldn't find it via a guid, try the path stripping the root node's name out first
            string path = GetPathFromGameObject(go,true);
            if(!String.IsNullOrEmpty(path))
            {
                if (schematicLookup.TryGetValue(path, out schematic))
                {
                    //we found it in our main dictionary
                    //UnityEngine.Debug.LogWarning("Found Schematic via Path: " + guid + " | " + path + " | " + go.name);
                    return schematic;
                }
            }
            //same thing but do not strip root (GL Stockings Left/glstockings_15340_left_LOD_0/glstockings_15340_left_LOD0 vs glstockings_15340_left_LOD_0/glstockings_15340_left_LOD0)
            path = GetPathFromGameObject(go,false);
            if(!String.IsNullOrEmpty(path))
            {
                if (schematicLookup.TryGetValue(path, out schematic))
                {
                    //we found it in our main dictionary
                    //UnityEngine.Debug.LogWarning("Found Schematic via Path: " + guid + " | " + path + " | " + go.name);
                    return schematic;
                }
            }

            //UnityEngine.Debug.Log("GetSchmeatic: " + guid + " | " + path + " | " + go.name);

            //couldn't find it
            return null;
        }

        //How do we retrieve a texture2d from a path, this is system specific
        //NOTE: you should avoid using this because the texture loaded this way (and not assetdatabase.load...) will not serialize!
        public Texture2D GetTextureFromPathDefault(string path, string rootPath=null)
        {
            UnityEngine.Debug.LogWarning("You should avoid using GetTextureFromPathDefault as your texture will not serialize, implement your own.");

            string finalPath = path;

            if (rootPath != null)
            {
                finalPath = path.Replace("./", rootPath + "/");
            }

            Texture2D tex = new Texture2D(1, 1);
            byte[] bytes = System.IO.File.ReadAllBytes(finalPath);
            tex.LoadImage(bytes);

            return tex;
        }

        public Material GetMaterialFromGUIDDefault(string path, string rootPath = null)
        {
            UnityEngine.Debug.LogWarning("Not implemented yet");
            return null;
        }

        //stores the schematics by guid for ease of lookup
        protected void IndexSchematics(Dictionary<string, AssetSchematic> schematicLookup)
        {
            schematicLookup.Clear();

            foreach(AssetSchematic schematic in schematics)
			{
                if(schematic == null)
                {
                    continue;
                }

                if(schematic.origin_and_description != null && !String.IsNullOrEmpty(schematic.origin_and_description.mcs_id))
                {
                    //key off the real guid
    				schematicLookup[schematic.origin_and_description.mcs_id] = schematic;
				}
                if(schematic.stream_and_path != null && !String.IsNullOrEmpty(schematic.stream_and_path.source_path))
                {
                    //key off the fbx node path
                    schematicLookup[schematic.stream_and_path.source_path] = schematic;
                }
            }
        }

        /// <summary>
        /// Create a NEW game object by cloning an existing game object and passing in schematics or reading them directly off the nodes
        /// NOTE: this works for BOTH runtime and editor and from loose schematics or embedded schematics
        /// NOTE: you will likely want to specificy several callbacks based on the delegates at the top of this file (textures/mats/guid/scehmatic/etc)
        /// </summary>
        /// <param name="gameObjectIn"></param>
        /// <param name="schematicsIn"></param>
        /// <returns></returns>
        public GameObject CreateGameObjectFromExistinGameObject(GameObject gameObjectIn, Dictionary<string, AssetSchematic> schematicLookup, List<AssetSchematic> schematicsIn=null)
        {
            string targetName = gameObjectIn.name;
            GameObject targetGO = GameObject.Instantiate(gameObjectIn);
            targetGO.name = gameObjectIn.name; //ditch things like "(Clone)"




			if (schematicsIn == null || schematicsIn.Count <= 0) {
				GetAllSchematicsFromNodes (targetGO, schematicLookup);
			}
			else 
			{
				schematics = schematicsIn;
			}
            IndexSchematics(schematicLookup);

			//Debug.Log ("Number of schematics: " + schematics.Count + " | vs: " + (schematicsIn != null ?  schematicsIn.Count.ToString() : "null"));

            GameObject rootNode = targetGO;
            AssetSchematic rootSchematic = GetSchematicFromGameObject(rootNode, schematicLookup);


            if (rootSchematic == null)
            {
                //this is the wrong root node, let's check for a child that has the same name and or has a item_function defined
                for(int i = 0; i < targetGO.transform.childCount; i++)
                {
                    GameObject node = targetGO.transform.GetChild(i).gameObject;

                    if(node.name == targetName || MCS_Utilities.Paths.ScrubKey(node.name) == targetName)
                    {
                        rootNode = node;
                        rootSchematic = GetSchematicFromGameObject(rootNode, schematicLookup);
                    }

                }
            }

            if(rootSchematic == null)
            {
                throw new Exception("Root schematic must not be null");
            }

			//Debug.Log ("Target: " + targetGO + " RootSchematic: " + rootSchematic.type_and_function.item_function);

            ProcessNode(rootNode, rootSchematic, targetGO, schematicLookup);

            if(rootSchematic.type_and_function.primary_function == PrimaryFunction.material)
            {
                Debug.Log("Found Material Asset Schematic.");
            }

            //do some final handling now that our stuff is assembled
            switch(rootSchematic.type_and_function.item_function)
            {
                case ItemFunction.soft_wearable:
                case ItemFunction.hair:
                case ItemFunction.prop:
                    //CoreMeshMetaData coreMeshMetaData = targetGO.GetComponent<CoreMeshMetaData>();
                    //if(rootSchematic.type_and_function.artisttools_function == ArtistToolsFunction.item)
                    //{
                        
                    //}
                    CoreMeshMetaData coreMeshMetaData = targetGO.GetComponentInChildren<CoreMeshMetaData>(true);
                    /*
                    if (coreMeshMetaData == null)
                    {
                        coreMeshMetaData = targetGO.GetComponentInChildren<CoreMeshMetaData>(true);
                    }
                    */

                    CIclothing ciclothing = targetGO.GetComponentInChildren<CIclothing>(true);
                    CIhair cihair = targetGO.GetComponentInChildren<CIhair>(true);
                    CIprop ciprop = targetGO.GetComponentInChildren<CIprop>(true);

                    CoreMesh[] coreMeshes = targetGO.GetComponentsInChildren<CoreMesh>(true);
                    List<CoreMesh> listCoreMeshes = new List<CoreMesh>();

                    foreach (CoreMesh coreMesh in coreMeshes)
                    {
                        listCoreMeshes.Add(coreMesh);

                        //is the geometry id empty?
                        if (coreMeshMetaData.geometryId == null || coreMeshMetaData.geometryId.Length <= 0)
                        {
                            coreMeshMetaData.geometryId = coreMesh.gameObject.name;
                            //take the left half of the name if we find a "." , eg: "NMBoots_1462.Shape" -> "NMBoots_1462"
                            int pos = coreMeshMetaData.geometryId.LastIndexOf(".");
                            if (pos >= 0)
                            {
                                coreMeshMetaData.geometryId = coreMeshMetaData.geometryId.Substring(0, pos);
                            }
                        }
                    }

                    if (ciclothing != null)
                    {
                        ciclothing.LODlist = listCoreMeshes;
                    }
                    if (cihair != null)
                    {
                        cihair.LODlist = listCoreMeshes;
                    }
                    if (ciprop != null)
                    {
                        ciprop.LODlist = listCoreMeshes;
                    }
                    break;
            }

            return targetGO;
        }

        protected void GetAllSchematicsFromNodes(GameObject currentNode, Dictionary<string, AssetSchematic> schematicLookup)
        {
            AssetSchematic schematic = GetSchematicFromGameObject(currentNode, schematicLookup);
            if(schematic != null)
            {
                schematics.Add(schematic);
            }

            for(int i = 0; i < currentNode.transform.childCount; i++)
            {
                GetAllSchematicsFromNodes(currentNode.transform.GetChild(i).gameObject, schematicLookup);
            }
        }


        protected void ProcessNode(GameObject rootNode, AssetSchematic rootSchematic, GameObject currentNode, Dictionary<string, AssetSchematic> schematicLookup)
        {
            //what am i?
            AssetSchematic schematic = GetSchematicFromGameObject(currentNode, schematicLookup);

            //strip any components we don't understand
            Component[] components = currentNode.GetComponents<Component>();
            for(int i = 0; i < components.Length; i++)
            {
                Type type = components[i].GetType();
                string typeName = type.ToString();

                if (componentWhiteList.Contains(typeName))
                {
                    continue;
                }

                //only strip these if we're in play mode, it's possible we could actually mess stuff up permanently if not
                if (Application.isPlaying)
                {
                    GameObject.Destroy(components[i]);
                }

            }

            //Debug.Log ("Current Node: " + currentNode + " Schematic: " + (schematic==null?"null":"not null"));

            bool skip = false;

            if (schematic != null)
            {

                //UnityEngine.Debug.Log("Found a node with a schematic: " + currentNode.name);

                string guid = schematic.origin_and_description.mcs_id;
                //UnityEngine.Debug.Log("node a: " + currentNode.name + " | " + guid + " | " + schematic.type_and_function.artisttools_function + " | " + schematic.type_and_function.item_function + " | " + rootSchematic.type_and_function.item_function);
                //found the schematic, do something with it

                /*
				if (currentNode.transform.parent == null && currentNode.GetInstanceID()==rootNode.GetInstanceID())
				{
				
					GameObject tgo = new GameObject ();
					tgo.name = currentNode.name + " Root";
					currentNode.transform.SetParent (tgo.transform);
				}
                */

                GameObject parentNode;

                if(currentNode.transform.parent != null)
                {
                   parentNode = currentNode.transform.parent.gameObject;
                } else { 
                    parentNode = currentNode;
                }

                //UnityEngine.Debug.Log("Processing node: " + currentNode.name + " type: " + schematic.type_and_function.hierarchy_rank);

                switch (schematic.type_and_function.artisttools_function)
                {
                    case ArtistToolsFunction.material:
                        break; 
                    case ArtistToolsFunction.item:

                        //does my parent have a CostumeItem and CoreMeshMetaData, if yes bail on processing
                        // You can see an example item in the Lawless Survivor outfit "LSPants", the nodes will
                        // look like LSPants -> LSPants -> LSPants_LOD0, the double name on the top is causing problems
                        // which this check fixes
                        if(currentNode.transform.parent != null)
                        {
                            CoreMeshMetaData cmmdParent = parentNode.GetComponent<CoreMeshMetaData>();
                            CostumeItem ciParent = parentNode.GetComponent<CostumeItem>();

                            if(cmmdParent != null && ciParent != null)
                            {
                                //UnityEngine.Debug.LogWarning("Parent node has CoreMeshMetaData and CostumeItem, skipping node: " + currentNode.name);
                                skip = true;
                                break;
                            }
                        }

                        CoreMeshMetaData coreMeshMetaData = null;
                        if (schematic.type_and_function.hierarchy_rank == HierarchyRank.item)
                        {
                            //AssetPrepper -> PrepForMCS does it this way
                            coreMeshMetaData = currentNode.AddComponent<CoreMeshMetaData>();
                            AttachingCIItemToNode(currentNode, schematic);
                        } else
                        {
                            //Artist tools does it this way
                            coreMeshMetaData = parentNode.AddComponent<CoreMeshMetaData>();
                        }
                        coreMeshMetaData.vendorId = schematic.origin_and_description.vendor_id;
                        coreMeshMetaData.versionId = schematic.version_and_control.item_version.ToString();
                        coreMeshMetaData.geometryId = schematic.origin_and_description.name;
                        coreMeshMetaData.ID = schematic.origin_and_description.mcs_id;

                        //commented out fields can be skipped safely
                        //coreMeshMetaData.compatabilityBase = schematic.compatibilities[0];
                        //coreMeshMetaData.compatibilities = schematic.compatibilities;
                        //coreMeshMetaData.declarativeUse = "";
                        //coreMeshMetaData.controllerScales = ...';
                        coreMeshMetaData.item_id = schematic.origin_and_description.mcs_id;
                        coreMeshMetaData.collection_id = schematic.origin_and_description.collection_id;
                        coreMeshMetaData.rank = schematic.type_and_function.hierarchy_rank;
                        coreMeshMetaData.function = schematic.type_and_function.item_function;
                        //coreMeshMetaData.schematic = ...;
                        coreMeshMetaData.collection_version = schematic.version_and_control.collection_version;
                        coreMeshMetaData.item_version = schematic.version_and_control.item_version;
                        coreMeshMetaData.mcs_version = schematic.version_and_control.mcs_version;
                        coreMeshMetaData.item_name = schematic.origin_and_description.name;
                        coreMeshMetaData.collection_name = schematic.origin_and_description.collection_name;

                        switch (rootSchematic.type_and_function.item_function) {
                            case ItemFunction.soft_wearable:
                               coreMeshMetaData.meshType = CONSTANTS.MESH_TYPE.CLOTH;
                                break;
                            case ItemFunction.hair:
                                coreMeshMetaData.meshType = CONSTANTS.MESH_TYPE.HAIR;
                                break;
                            case ItemFunction.prop:
                                coreMeshMetaData.meshType = CONSTANTS.MESH_TYPE.PROP;
                                break;
                        }
                        break;
                    case ArtistToolsFunction.model:
                        //handles path for artist tools
                        //AttachingCIItemToNode(parentNode, schematic);

                        break;
                    case ArtistToolsFunction.geometry:
                        //CoreMesh, but we'll handle it via hierarchy rank instead
                        AttachCoreMeshToNode(currentNode, schematic, rootSchematic);

                        break;
                }

                if (!skip)
                {

                    switch (schematic.type_and_function.hierarchy_rank)
                    {
                        case HierarchyRank.geometry:
                            AttachCoreMeshToNode(currentNode, schematic, rootSchematic);
                            break;
                    }
                }
            }

            //dive into each child
            for(int i = 0; i < currentNode.transform.childCount; i++)
            {
                GameObject child = currentNode.transform.GetChild(i).gameObject;
                ProcessNode(rootNode, rootSchematic, child, schematicLookup);
            }


            //return nothing, as we've modified the current game object
        }

        public void AttachingCIItemToNode(GameObject currentNode, AssetSchematic schematic)
        {
            switch (schematic.type_and_function.item_function)
            {
                case ItemFunction.soft_wearable:
                    CIclothing ciclothing = currentNode.AddComponent<CIclothing>();
                    ciclothing.meshType = CONSTANTS.MESH_TYPE.CLOTH;
                    ciclothing.dazName = schematic.origin_and_description.name;
                    ciclothing.ID = schematic.origin_and_description.mcs_id;


                    if (schematic.structure_and_physics.item_structure.alpha_masks_key != null)
                    {
                        for(int i=0;i< schematic.structure_and_physics.item_structure.alpha_masks_key.Count(); i++)
                        {
                            string slot = schematic.structure_and_physics.item_structure.alpha_masks_key[i];
                            string path = schematic.structure_and_physics.item_structure.alpha_masks[i];


                            switch (slot)
                            {
                                case "BODY":
                                    ciclothing.alphaMasks[CONSTANTS.MATERIAL_SLOT.BODY] = GetTextureFromPath(path, basePath);
                                    break;
                                case "HEAD":
                                    ciclothing.alphaMasks[CONSTANTS.MATERIAL_SLOT.HEAD] = GetTextureFromPath(path, basePath);
                                    break;
                                case "EYE":
                                    //do nothing, we don't support this slot yet
                                    break;
                            }

                        }
                    }


                    break;
                case ItemFunction.hair:
                    CIhair cihair = currentNode.AddComponent<CIhair>();
                    if(schematic.structure_and_physics.item_structure.overlay != null)
                    {
                        cihair.overlay = GetTextureFromPath(schematic.structure_and_physics.item_structure.overlay, basePath);
                        cihair.overlayColor = AssetSchematicUtility.ConvertColorStringToColor(schematic.structure_and_physics.item_structure.overlay_color);
                    }
                    cihair.meshType = CONSTANTS.MESH_TYPE.HAIR;
                    cihair.dazName = schematic.origin_and_description.name;
                    cihair.ID = schematic.origin_and_description.mcs_id;
                    break;
                case ItemFunction.prop:
                    CIprop ciprop = currentNode.AddComponent<CIprop>();
                    ciprop.meshType = CONSTANTS.MESH_TYPE.PROP;
                    ciprop.dazName = schematic.origin_and_description.name;
                    ciprop.ID = schematic.origin_and_description.mcs_id;
                    break;
            }
        }

        public void AttachCoreMeshToNode(GameObject currentNode, AssetSchematic schematic, AssetSchematic rootSchematic)
        {
            CoreMesh coreMesh = currentNode.GetComponent<CoreMesh>();
            if (coreMesh == null)
            {
                coreMesh = currentNode.AddComponent<CoreMesh>();
            }

            //where are the runtime morph files (they are in a resources folder that artist tools exports from projection)
            coreMesh.runtimeMorphPath = rootSchematic.origin_and_description.vendor_name + "/" + rootSchematic.origin_and_description.collection_name + "/" + rootSchematic.origin_and_description.name + "/" + currentNode.name;
            //clean the path up
            coreMesh.runtimeMorphPath = coreMesh.runtimeMorphPath.Replace(" ", "_");
            Regex regex = new Regex(@"[^a-zA-Z0-9-_/]+"); //TODO: this regex or formatting should come from streaming morphs or morph extraction, not here
            coreMesh.runtimeMorphPath = regex.Replace(coreMesh.runtimeMorphPath, "");
            coreMesh.dazName = ""; //this can't be null, but it can be blank

            switch (schematic.type_and_function.item_function)
            {
                case ItemFunction.soft_wearable:
                    coreMesh.meshType = CONSTANTS.MESH_TYPE.CLOTH;
                    break;
                case ItemFunction.hair:
                    coreMesh.meshType = CONSTANTS.MESH_TYPE.HAIR;
                    break;
                case ItemFunction.figure:
                    coreMesh.meshType = CONSTANTS.MESH_TYPE.BODY;
                    break;
                case ItemFunction.prop:
                    coreMesh.meshType = CONSTANTS.MESH_TYPE.PROP;
                    break;
            }

            if (schematic.structure_and_physics.item_structure.assigned_materials != null)
            {
                Renderer renderer = currentNode.GetComponent<Renderer>();
                if(renderer != null)
                {
                    Material[] mats = new Material[schematic.structure_and_physics.item_structure.assigned_materials.Length];
                    for(int i=0;i< schematic.structure_and_physics.item_structure.assigned_materials.Length;i++)
                    {
                        string matGUID = schematic.structure_and_physics.item_structure.assigned_materials[i];
                        mats[i] = GetMaterialFromGUID(matGUID, basePath);
                    }

                    renderer.sharedMaterials = mats;
                }

            }
        }

        public static void GetNodeTraversalPathFromGOToRoot(GameObject rootGO, GameObject currentGO, ref string path, bool stripRoot = false)
        {

            if (path.Length > 0)
            {
                path = "/" + path;
            }

            path = currentGO.name + path;

            if (currentGO.transform.parent == null || (rootGO != null && rootGO.GetInstanceID().Equals(currentGO.GetInstanceID())))
            {
                //we're done, there is nothing left to traverse

                if (stripRoot)
                {
                    int pos = path.IndexOf('/');
                    if (pos >= 0)
                    {
                        path = path.Substring(pos+1);
                    }
                }

                return;
            }

            GetNodeTraversalPathFromGOToRoot(rootGO, currentGO.transform.parent.gameObject, ref path, stripRoot);
        }


    }
}
