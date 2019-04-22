using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MCS.UTILITIES
{
	/// <summary>
	/// Utility class for manipulation of Textures.
	/// </summary>
	public class TextureUtilities
	{
		/// <summary>
		/// Produces a Texture2D image composite of two images, where the base texture's R channel is only overwritten when the copy texture's R channel is less
		/// than the threshold of 0.48f for a given pixel.
		/// </summary>
		/// <returns>The textures.</returns>
		/// <param name="aBaseTexture">A base texture.</param>
		/// <param name="aToCopyTexture">A to copy texture.</param>
		public static Texture2D OverlayTextures (Texture2D aBaseTexture, Texture2D aToCopyTexture)
		{
			int aWidth = aBaseTexture.width;
			int aHeight = aBaseTexture.height;
				
			Texture2D aReturnTexture = new Texture2D (aWidth, aHeight, TextureFormat.RGBA32, false);
				
			Color[] aBaseTexturePixels = aBaseTexture.GetPixels();
			Color[] aCopyTexturePixels = aToCopyTexture.GetPixels();
			Color[] aColorList = new Color[aBaseTexturePixels.Length];
				
			int aPixelLength = aBaseTexturePixels.Length;
				
			for (int p = 0; p < aPixelLength; p++) {
				aColorList[p].r = aBaseTexturePixels[p].r;
				aColorList[p].g = aBaseTexturePixels[p].g;
				aColorList[p].b = aBaseTexturePixels[p].b;

				if (aCopyTexturePixels[p].r < 0.48f)
					aColorList[p].r = aCopyTexturePixels[p].r;
				else
					aColorList[p].r = aBaseTexturePixels[p].r;
			}
				
			aReturnTexture.SetPixels (aColorList);
			aReturnTexture.Apply (false);
				
			return aReturnTexture;
		}



		/// <summary>
		/// Produces a Texture2D image which is the result of overlaying a List of textures on top of a base texture. Only the R channel is altered from the base image,
		/// and then only when the R channel of any image in the List is less than 0.3f for a given pixel.
		/// </summary>
		/// <returns>A Texture2D composite result.</returns>
		/// <param name="base_texture">A Texture2D base image.</param>
		/// <param name="texture_list">A List of Texture2D images.</param>
		public static Texture2D OverlayListOfTextures (Texture2D base_texture, List<Texture2D>texture_list)
		{
			return OverlayArrayOfTextures (base_texture, texture_list.ToArray());
		}



		/// <summary>
		/// Produces a Texture2D image which is the result of overlaying an array of textures on top of a base texture. Only the R channel is altered from the base image,
		/// and then only when the R channel of any image in the array is less than 0.3f for a given pixel.
		/// </summary>
		/// <returns>A Texture2D composite result.</returns>
		/// <param name="base_texture">A Texture2D base image.</param>
		/// <param name="texture_list">An array of Texture2D images.</param>
		public static Texture2D OverlayArrayOfTextures (Texture2D base_texture, Texture2D[] texture_list)
		{
			int image_width = base_texture.width;
			int image_height = base_texture.height;
			Texture2D composite_texture = new Texture2D (image_width, image_height, TextureFormat.RGBA32, false);
			Color32[] base_pixels = base_texture.GetPixels32(); 
			Color32[] destination_pixels = base_texture.GetPixels32();

			foreach (Texture2D layer in texture_list) {
				int i = 0;
				int pixel_length = base_pixels.Length;
				Color32[] layer_pixels = layer.GetPixels32();
				for (;i<pixel_length;i++) {
					if (layer_pixels[i].r < 0.3f)
						destination_pixels[i].r = 0;
				}
			}
			composite_texture.SetPixels32 (destination_pixels);
			composite_texture.Apply (false);
			return composite_texture;
		}

        /// <summary>
        /// Merge multiple textures into one, this is done on the GPU and is currently specific to the alpha injection system
        /// This is MUCH faster then the original cpu only method (150ms to about 8.5ms)
        /// </summary>
        public static void OverlayArrayOfTexturesGPU(ref Texture2D outputTexture, Texture2D[] textures, string shaderName = "Unlit/AlphaCombiner", bool debug = false)
        {
            int slot = 1;

            //our current alpha texture works off black = transparent, white = keep
            Color background = Color.white;

            //RenderTexture rt = new RenderTexture(1024, 1024, 16, RenderTextureFormat.ARGB32);
            RenderTexture rt = RenderTexture.GetTemporary(1024, 1024, 16, RenderTextureFormat.ARGB32);

            GameObject holder = new GameObject();
            GameObject cameraObject = new GameObject();
            if (debug)
            {
                //we only really want these names if we're debugging
                holder.name = "TextureUtilities.Combiner";
                cameraObject.name = "TextureUtilities.Camera";
            }

            cameraObject.transform.parent = holder.transform;
            Camera cam = cameraObject.AddComponent<Camera>();

            string layerName = "MorphOffscreenCamera";

            int layer = LayerMask.NameToLayer(layerName);
            int layerMask = LayerMask.GetMask(layerName);

            //the layer doesn't exist, so... let's find an empty one?
            if (layer == -1)
            {
                for (int tmpLayer = 8; tmpLayer<32; tmpLayer++)
                {
                    string name = LayerMask.LayerToName(tmpLayer);
                    if (name.Length == 0)
                    {
                        layer = tmpLayer;
                        layerMask = 1 << tmpLayer;
                        break;
                    }
                }

                if(layer == -1)
                {
                    throw new UnityException("Unable to find a free layer for texture combination");
                }
            }

            //TODO: this doesn't work there is no UNITY_5_4_OR_NEWER, we need to do something else here
            //#if UNITY_5_4_OR_NEWER
            //            cam.stereoTargetEye = false; //this is only available in 5.4
            //#endif

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.cullingMask = layerMask;
            cam.targetTexture = rt;
            cam.targetDisplay = 0;
            cam.orthographic = true;
            cam.orthographicSize = 0.5f;
            cam.farClipPlane = 5;
            cam.nearClipPlane = 0;
            cam.backgroundColor = background;
            cam.useOcclusionCulling = false;

            //loop through each texture and pack them next to each other
            foreach (Texture tex in textures)
            {
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Vector3 position = cam.transform.position;

                //this is our special shader that turns white into transparent and leaves the rest as is
                // we use this for backwards compatibility of the injection masks
                Material m = new Material(Shader.Find(shaderName));
                m.mainTexture = tex;

                MeshRenderer mr = quad.GetComponent<MeshRenderer>();
                mr.sharedMaterial = m;

                //this is only used so we can set the proper layering for when textures need to have a priority over others
                position.z += slot * 0.01f;

                quad.transform.position = position;
                quad.transform.parent = holder.transform;
                quad.layer = layer;

                slot += 1;
            }

            //force it to render right now, not on the next frame
            cam.Render();
            
            RenderTexture oldRT = RenderTexture.active;
            RenderTexture.active = rt;

            //copy the render texture into the texture, we do this so we can destroy the camera and render texture
            outputTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            outputTexture.Apply();

            cam.targetTexture = null;

            RenderTexture.active = oldRT;

            //disable immediately so if we just do a destroy it won't overlap
            holder.SetActive(false);
            if (!debug)
            {
                //only destroy the object if not debugging

                //A quick note, we need to use Immediate otherwise we can end up merging multiple quads meant for other merges into each other
                if (!Application.isPlaying)
                {
                    GameObject.DestroyImmediate(holder);
                } else
                {
                    GameObject.Destroy(holder);
                }
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        //This method is much faster then the readpixels/apply method
        // it's due to the fact that the texture does not need to be copied between 
        // gpu/cpu/gpu and instead can stay all in gpu
        // It's about 20 times faster then the other method (8.5ms to 0.5ms)
        public static RenderTexture OverlayArrayOfTexturesGPU(Texture2D[] textures, string shaderName = "Unlit/AlphaCombiner", bool debug=false)
        {
            int slot = 1;

            //our current alpha texture works off black = transparent, white = keep
            Color background = Color.white;

            //TODO: should we try to reuse a render texture, and/or free the old one?, might be a good idea...
            //RenderTexture rt = new RenderTexture(1024, 1024, 16, RenderTextureFormat.ARGB32);
            //RenderTexture rt = RenderTexture.GetTemporary(1024, 1024, 16,RenderTextureFormat.ARGB32);
            //RenderTexture rt2 = new RenderTexture(1024, 1024, 16, RenderTextureFormat.ARGB32);
            //RenderTexture rt2 = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32);
            //RenderTexture rt2 = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32);
            RenderTexture rt2 = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32);
            //rt2.anisoLevel = 1;
            rt2.anisoLevel = 0;
            rt2.useMipMap = false;
            rt2.wrapMode = TextureWrapMode.Repeat;
            rt2.filterMode = FilterMode.Bilinear;

            //rt.depth = 0;
            //rt.antiAliasing = 1;
            //rt.anisoLevel = 0;
            //rt.isPowerOfTwo = true;
            //rt.useMipMap = false;
            //rt.Create();
            //do not use temporary textures, that's not what they're for, they're designed for a single quick use then discard (they'd be better for the slower call above)
            //RenderTexture rt = RenderTexture.GetTemporary(1024, 1024, 16,RenderTextureFormat.ARGB32);

            GameObject holder = new GameObject();
            GameObject cameraObject = new GameObject();
            if (debug)
            {
                //we only really want these names if we're debugging
                holder.name = "TextureUtilities.Combiner";
                cameraObject.name = "TextureUtilities.Camera";
            }

            cameraObject.transform.parent = holder.transform;
            Camera cam = cameraObject.AddComponent<Camera>();

            string layerName = "MorphOffscreenCamera";

            int layer = LayerMask.NameToLayer(layerName);
            int layerMask = LayerMask.GetMask(layerName);

            //the layer doesn't exist, so... let's find an empty one?
            if (layer == -1)
            {
                for (int tmpLayer = 8; tmpLayer < 32; tmpLayer++)
                {
                    string name = LayerMask.LayerToName(tmpLayer);
                    if (name.Length == 0)
                    {
                        layer = tmpLayer;
                        layerMask = 1 << tmpLayer;
                        break;
                    }
                }

                if (layer == -1)
                {
                    throw new UnityException("Unable to find a free layer for texture combination");
                }
            }

            //cam.stereoTargetEye = false; //this is only available in 5.4

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.cullingMask = layerMask;
            //cam.targetTexture = rt;
            cam.targetTexture = rt2;
            //cam.targetDisplay = 0;
            cam.orthographic = true;
            cam.orthographicSize = 0.5f;
            cam.farClipPlane = 5;
            cam.nearClipPlane = 0;
            cam.backgroundColor = background;
            cam.useOcclusionCulling = false;

            //loop through each texture and pack them next to each other
            if (textures != null)
            {
                foreach (Texture tex in textures)
                {
                    GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    Vector3 position = cam.transform.position;

                    //this is our special shader that turns white into transparent and leaves the rest as is
                    // we use this for backwards compatibility of the injection masks
                    Material m = new Material(Shader.Find(shaderName));
                    m.mainTexture = tex;

                    MeshRenderer mr = quad.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = m;

                    //this is only used so we can set the proper layering for when textures need to have a priority over others
                    position.z += slot * 0.01f;

                    quad.transform.position = position;
                    quad.transform.parent = holder.transform;
                    quad.layer = layer;

                    slot += 1;
                }
            }


            //force it to render right now, not on the next frame
            cam.Render();

            //RenderTexture oldRT = RenderTexture.active;
            //RenderTexture.active = rt;
            RenderTexture.active = rt2;
            //RenderTexture.active = oldRT;

            //Graphics.Blit(rt, rt2);

            //unbind our render texture from this camera so we can drop it
            //FYI, this is VERY important, or the texture becomes corrupted
            cam.targetTexture = null;

            //disable immediately so if we just do a destroy it won't overlap
            holder.SetActive(false);

            if (!debug)
            {
                if (!Application.isPlaying)
                {
                    GameObject.DestroyImmediate(holder);
                } else
                {
                    GameObject.Destroy(holder);
                }
            }
            //RenderTexture.ReleaseTemporary(rt);
            RenderTexture.active = null;

            return rt2;
        }
    }
}
