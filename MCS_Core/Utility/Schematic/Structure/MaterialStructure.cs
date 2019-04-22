using System;
using UnityEngine;
namespace MCS.Utility.Schematic.Structure
{
	//this is the data for the textures we support
	[Serializable]
	public class MaterialStructure
	{
		//{  
		//   "albedo":"images/albedo.jpg",
		//   "tint":"255,200,120",
		//   "metal":"images/metal.jpg",
		//   "smoothness":"images/smoothness.jpg",
		//   "emission":"images/emission.jpg",
		//   "emission_modifier":1.0,
		//   "normal":"images/normal.jpg",
		//   "normal_modifier":1.0,
		//   "transparency":"images/transparency.jpg",
		//   "transparency_mode":"cutout"
		//}
		public string shader;

		public string albedo;
        public string albedo_tint;
        public string metal;
		public string specular;
		public string smoothness_channel;
        public float metallic_value;
        public float smoothness_value;
		public string normal;
		public float normal_value;
		public string detail_normal;
		public float detail_normal_value;
        public string height;
        public string transparency;
        public string transparency_mode;
		public float transparency_value;
		public string emission; //image map
		public string emission_value; //color r,g,b
		public string emission_color; //Artist Tools uses this as the rgb value for emission
		public string global_illumination;
		public string detail;
        public string occlusion; //ambient occlusion map, Standard Shader: _OcclusionMap
        public float occlusion_value; //Standard shader: _OcclusionStrength
		public float tile_x;
		public float tile_y;
		public float offset_x;
		public float offset_y;
        public string tint; //TODO: this should probably automagically get converted into a color (r,g,b) as a number from 0-255

        public string[] shader_keywords = null; //eg: _ALPHATEST_ON
        public string[] shader_tags = null; //eg: RenderType=TransparentCutout
        public string[] shader_properties = null; //eg: "int:_SrcBlend=1","float:_Range=2.0","color:_Col=127,255,3,0.5"
        public int render_queue = -2; //-1+ are valid ranges

        public string[] textures = null;//list of textures on disk
		public static MaterialStructure CreateFromJSON(string jsonString)
		{
			return JsonUtility.FromJson<MaterialStructure>(jsonString);
		}
	}
}
