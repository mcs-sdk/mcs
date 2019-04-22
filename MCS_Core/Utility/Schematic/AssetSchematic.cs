using System;
using UnityEngine;
using MCS.Utility.Schematic.Base;
using MCS.Utility.Schematic.Enumeration;
using MCS.Utility.Schematic.Structure;
using MCS.Utility.Serialize;
using System.Collections.Specialized;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace MCS.Utility.Schematic
{
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class ATEditable : System.Attribute
    {
        public ATEditable()
        {
        }
    }
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class ATInputSlider : System.Attribute
    {
        public ATInputSlider()
        {
        }
    }
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class ATListView : System.Attribute
    {
        public Type EnumType;
        public List<string> ListContent;
        public ATListView(Type content)
        {
            EnumType = content;
            ListContent = new List<string>(Enum.GetNames(content));
        }
    }
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class ATInputText : System.Attribute
    {
        public ATInputText()
        {
        }
    }

    [Serializable]
    public class AssetSchematics
    {
        public string[] schematics;
    }

    [Serializable]
    public class AssetSchematicsReal
    {
        public AssetSchematic[] schematics;
    }

    [Serializable]
	public class AssetSchematic
	{
		public OriginAndDescription origin_and_description;
		public StreamAndPath stream_and_path;
		public VersionAndControl version_and_control;
		public StructureAndPhysics structure_and_physics;
		public TypeAndFunction type_and_function;

		public AssetSchematic()
		{

		}



		// Used in Artist Tools
		public void InitializeSchematic()
		{
			origin_and_description = new OriginAndDescription();
			stream_and_path = new StreamAndPath();
			version_and_control = new VersionAndControl();
			structure_and_physics = new StructureAndPhysics();
			type_and_function = new TypeAndFunction();
		}


		/**Example json(mon file)
		{  
		   "origin_and_description":{  
		      "id":"",
		      "name":"",
		      "vendor_id":"",
		      "vendor_name":"",
		      "collection_id":"",
		      "collection_name":"",
		      "gender_enum":"unknown",
		      "gender":0,
		      "category_enum":"unknown",
		      "category":0,
		      "tags":[  

		      ],
		      "description":""
		   },
		   "stream_and_path":{  
		      "image_thumbnail":"",
		      "interactive_thumbnail":"",
		      "url":"",
		      "root_path":"",
		      "source_path":"",
		      "generated_path":""
		   },
		   "version_and_control":{  
		      "mcs_version":0.0,
		      "mcs_revision":0.0,
		      "collection_version":0.0,
		      "item_version":0.0,
		      "compatibilities":[  

		      ]
		   },
		   "structure_and_physics":{  
		      "item_structure":{  
		         "lods":[  

		         ],
		         "persistent":[  

		         ],
		         "cap":[  

		         ]
		      },
		      "material_structure":{  
				   "albedo":"images/albedo.jpg",
				   "metal":"images/metal.jpg",
				   "tint":"255,200,120",
				   "smoothness":"images/smoothness.jpg",
				   "emission":"images/emission.jpg",
				   "emission_modifier":1.0,
				   "normal":"images/normal.jpg",
				   "normal_modifier":1.0,
				   "transparency":"images/transparency.jpg",
				   "transparency_mode":"cutout"
		      },
		      "mass":0.0,
		      "volume":0.0,
		      "density":0.0,
		      "substance_enum":"unknown",
		      "substance":0
		   },
		   "type_and_function":{  
		      "primary_function_enum":"unknown",
		      "primary_function":0,
		      "hierarchy_rank_enum":"unknown",
		      "hierarchy_rank":0,
		      "item_function_enum":"unknown",
		      "item_function":0,
		      "material_function_enum":"unknown",
		      "material_function":0,
		      "morph_function_enum":"unknown",
		      "morph_function":0,
		      "animation_function_enum":"unknown",
		      "animation_function":0
		   }
		}
		*/
		//public ItemFunction animation_function
		//{
		//	get;
		//	set;
		//}

		//public Category category
		//{
		//	get;
		//	set;
		//}

		//public float collection_version
		//{
		//	get;
		//	set;
		//}

		//[SerializeField]
		//private string[] compatibilities;

		//public string[] Compatibilities
		//{
		//	get{ return this.compatibilities; }
		//	set{ this.compatibilities = value; }
		//}

		//public float density
		//{
		//	get;
		//	set;
		//}

		//public string description
		//{
		//	get;
		//	set;
		//}

		//public Gender gender
		//{
		//	get;
		//	set;
		//}

		//public HierarchyRank hierarchy_rank
		//{
		//	get;
		//	set;
		//}

		//[SerializeField]
		//private string root_path;

		//public string Root_path 
		//{
		//	get{ return this.root_path; }
		//	set{ this.root_path = value; }
		//}

		//[SerializeField]
		//private string source_path;

		//public string Source_path 
		//{
		//	get{ return this.source_path; }
		//	set{ this.source_path = value; }
		//}

		//[SerializeField]
		//private string generated_path;

		//public string Generated_path 
		//{
		//	get{ return this.generated_path; }
		//	set{ this.generated_path = value; }
		//}

		//[SerializeField]
		//private string id;

		//public string Id
		//{
		//	get{ return this.id; }
		//	set{ this.id = value; }
		//}

		//public string image_thumbnail
		//{
		//	get;
		//	set;
		//}

		//public string interactive_thumbnail
		//{
		//	get;
		//	set;
		//}

		//public ItemFunction item_function
		//{
		//	get;
		//	set;
		//}

		//public ItemStructure item_structure
		//{
		//	get;
		//	set;
		//}

		//[SerializeField]
		//private float item_version;

		//public float Item_version
		//{
		//	get{ return this.item_version; }
		//	set{ this.item_version = value; }
		//}

		//public float mass
		//{
		//	get;
		//	set;
		//}

		//public ItemFunction material_function
		//{
		//	get;
		//	set;
		//}

		//public MaterialStructure material_structure
		//{
		//	get;
		//	set;
		//}

		//public float mcs_revision
		//{
		//	get;
		//	set;
		//}

		//public float mcs_version
		//{
		//	get;
		//	set;
		//}

		//public ItemFunction morph_function
		//{
		//	get;
		//	set;
		//}

		//[SerializeField]
		//private string name;

		//public string Name
		//{
		//	get{ return this.name; }
		//	set{ this.name = value; }
		//}

		//[SerializeField]
		//private string primary_func;

		//public PrimaryFunction primary_function
		//{
		//	get;
		//	set;
		//}

		//public PhysicalSubstance substance
		//{
		//	get;
		//	set;
		//}

		//public string[] tags
		//{
		//	get;
		//	set;
		//}

		//[SerializeField]
		//private string url;

		//public string Url
		//{
		//	get{ return this.url; }
		//	set{ this.url = value; }
		//}

		//public string vendor_id
		//{
		//	get;
		//	set;
		//}

		//public string vendor_name
		//{
		//	get;
		//	set;
		//}

		//public float volume
		//{
		//	get;
		//	set;
		//}

        //Mon files can contain one or more schematics

		public static AssetSchematic CreateFromJSON(string jsonString)
		{
			return JsonUtility.FromJson<AssetSchematic>(jsonString);
		}

        public static AssetSchematic[] CreateArrayFromJSON(string jsonString)
		{

            try
            {
                AssetSchematicsReal schematics = JsonUtility.FromJson<AssetSchematicsReal>(jsonString);
                return schematics.schematics;
            } catch (Exception e)
            {

            }

            try
            {
                AssetSchematics schematics = JsonUtility.FromJson<AssetSchematics>(jsonString);
                List<AssetSchematic> retSchematic = new List<AssetSchematic>();
                foreach (string scheme in schematics.schematics)
                    retSchematic.Add(CreateFromJSON(scheme));
                return retSchematic.ToArray();
            } catch (Exception e)
            {

            }

            throw new Exception("Unable to parse json for schematics");
		}

		public static string SendToJSON(AssetSchematic asset)
		{
			return JsonUtility.ToJson (asset,true);
		}
	}
}

