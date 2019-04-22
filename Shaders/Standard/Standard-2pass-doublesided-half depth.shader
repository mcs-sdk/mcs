// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "MCS/Standard-2pass-double sided half depth"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}
		
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.35
		_GlossMapScale("Smoothness Scale",Range(0.0,1.0)) = 1.0
		[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
		_MetallicGlossMap("Metallic", 2D) = "white" {}

		_BumpScale("Scale", Float) = 0.12
		_BumpMap("Normal Map", 2D) = "bump" {}

		_Parallax ("Height Scale", Range (0.005, 0.08)) = 0.02
		_ParallaxMap ("Height Map", 2D) = "black" {}

		_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
		_OcclusionMap("Occlusion", 2D) = "white" {}

		_EmissionColor("Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}
		
		_DetailMask("Detail Mask", 2D) = "white" {}

		_DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
		_DetailNormalMapScale("Scale", Float) = 1.0
		_DetailNormalMap("Normal Map", 2D) = "bump" {}

		[Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0

		// UI-only data
		[HideInInspector] _EmissionScaleUI("Scale", Float) = 0.0
		[HideInInspector] _EmissionColorUI("Color", Color) = (1,1,1)

		// Blending state
		[HideInInspector] _Mode ("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend ("__src", Float) = 1.0
		[HideInInspector] _DstBlend ("__dst", Float) = 0.0
		_ZWrite ("__zw", Float) = 1.0
		
		_ColorTint ("Tint", Color) = (1.0, 0.6, 0.6, 1.0)
	}

	CGINCLUDE
		#define UNITY_SETUP_BRDF_INPUT MetallicSetup
	ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 300
		

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }
			
			
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			Cull Front
			

			CGPROGRAM
			#pragma target 3.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles
			
			// -------------------------------------
			
			
					
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP 
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
				
			#pragma vertex myVert
			#pragma fragment myFrag

			#include "UnityStandardCore.cginc"
			
			VertexOutputForwardBase myVert (VertexInput v)
			{
				VertexOutputForwardBase o;
				UNITY_INITIALIZE_OUTPUT(VertexOutputForwardBase, o);

				float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
				#if UNITY_SPECCUBE_BOX_PROJECTION
				#if UNITY_REQUIRE_FRAQ_WORLDPOS
					o.posWorld = posWorld.xyz;
				#endif
				#endif
				o.pos = UnityObjectToClipPos(v.vertex);
				o.tex = TexCoords(v);
				o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
				float3 normalWorld = UnityObjectToWorldNormal(-v.normal);
				#ifdef _TANGENT_TO_WORLD
					float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

					float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
					o.tangentToWorldAndPackedData[0].xyz = tangentToWorld[0];
					o.tangentToWorldAndPackedData[1].xyz = tangentToWorld[1];
					o.tangentToWorldAndPackedData[2].xyz = tangentToWorld[2];
				#else
					o.tangentToWorldAndPackedData[0].xyz = 0;
					o.tangentToWorldAndPackedData[1].xyz = 0;
					o.tangentToWorldAndPackedData[2].xyz = normalWorld;
				#endif
				//We need this for shadow receving
				TRANSFER_SHADOW(o);

				// Static lightmaps
				#ifndef LIGHTMAP_OFF
					o.ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
					o.ambientOrLightmapUV.zw = 0;
				// Sample light probe for Dynamic objects only (no static or dynamic lightmaps)
				#elif UNITY_SHOULD_SAMPLE_SH
					#if UNITY_SAMPLE_FULL_SH_PER_PIXEL
						o.ambientOrLightmapUV.rgb = 0;
					#elif (SHADER_TARGET < 30)
						o.ambientOrLightmapUV.rgb = ShadeSH9(half4(normalWorld, 1.0));
					#else
						// Optimization: L2 per-vertex, L0..L1 per-pixel
						o.ambientOrLightmapUV.rgb = ShadeSH3Order(half4(normalWorld, 1.0));
					#endif
					// Add approximated illumination from non-important point lights
					#ifdef VERTEXLIGHT_ON
						o.ambientOrLightmapUV.rgb += Shade4PointLights (
							unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
							unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
							unity_4LightAtten0, posWorld, normalWorld);
					#endif
				#endif

				#ifdef DYNAMICLIGHTMAP_ON
					o.ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
				#endif
				
				#ifdef _PARALLAXMAP
					TANGENT_SPACE_ROTATION;
					half3 viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
					o.tangentToWorldAndParallax[0].w = viewDirForParallax.x;
					o.tangentToWorldAndParallax[1].w = viewDirForParallax.y;
					o.tangentToWorldAndParallax[2].w = viewDirForParallax.z;
				#endif
				
				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}
			
			
			half4 myFrag (VertexOutputForwardBase i) : SV_Target
			{
				FRAGMENT_SETUP(s)
				UnityLight mainLight = MainLight ();
				half atten = SHADOW_ATTENUATION(i);
				
				half occlusion = Occlusion(i.tex.xy);
				UnityGI gi = FragmentGI (
					s.posWorld, occlusion, i.ambientOrLightmapUV, atten, s.smoothness, s.normalWorld, s.eyeVec, mainLight);

				half4 c = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
				c.rgb += UNITY_BRDF_GI (s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, occlusion, gi);
				c.rgb += Emission(i.tex.xy);
				//c.rgb *= (.7,.7,.7)

				UNITY_APPLY_FOG(i.fogCoord, c.rgb);
				return OutputForward (c, s.alpha);
			}


			

			

			ENDCG
		}
		
		
		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			Cull Back
			
			CGPROGRAM
			#pragma target 3.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles
			
			// -------------------------------------
					
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP 
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
				
			
			#pragma vertex myVert
			#pragma fragment myFrag

			#include "UnityStandardCore.cginc"
			
			VertexOutputForwardBase myVert (VertexInput v)
			{
				VertexOutputForwardBase o;
				UNITY_INITIALIZE_OUTPUT(VertexOutputForwardBase, o);

				float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
				#if UNITY_SPECCUBE_BOX_PROJECTION
				#if UNITY_REQUIRE_FRAQ_WORLDPOS
					o.posWorld = posWorld.xyz;
				#endif
				#endif
				o.pos = UnityObjectToClipPos(v.vertex);
				o.tex = TexCoords(v);
				o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
				float3 normalWorld = UnityObjectToWorldNormal(v.normal);
				#ifdef _TANGENT_TO_WORLD
					float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

					float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
					o.tangentToWorldAndParallax[0].xyz = tangentToWorld[0];
					o.tangentToWorldAndParallax[1].xyz = tangentToWorld[1];
					o.tangentToWorldAndParallax[2].xyz = tangentToWorld[2];
				#else
					o.tangentToWorldAndPackedData[0].xyz = 0;
					o.tangentToWorldAndPackedData[1].xyz = 0;
					o.tangentToWorldAndPackedData[2].xyz = normalWorld;
				#endif
				//We need this for shadow receving
				TRANSFER_SHADOW(o);

				// Static lightmaps
				#ifndef LIGHTMAP_OFF
					o.ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
					o.ambientOrLightmapUV.zw = 0;
				// Sample light probe for Dynamic objects only (no static or dynamic lightmaps)
				#elif UNITY_SHOULD_SAMPLE_SH
					#if UNITY_SAMPLE_FULL_SH_PER_PIXEL
						o.ambientOrLightmapUV.rgb = 0;
					#elif (SHADER_TARGET < 30)
						o.ambientOrLightmapUV.rgb = ShadeSH9(half4(normalWorld, 1.0));
					#else
						// Optimization: L2 per-vertex, L0..L1 per-pixel
						o.ambientOrLightmapUV.rgb = ShadeSH3Order(half4(normalWorld, 1.0));
					#endif
					// Add approximated illumination from non-important point lights
					#ifdef VERTEXLIGHT_ON
						o.ambientOrLightmapUV.rgb += Shade4PointLights (
							unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
							unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
							unity_4LightAtten0, posWorld, normalWorld);
					#endif
				#endif

				#ifdef DYNAMICLIGHTMAP_ON
					o.ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
				#endif
				
				#ifdef _PARALLAXMAP
					TANGENT_SPACE_ROTATION;
					half3 viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
					o.tangentToWorldAndParallax[0].w = viewDirForParallax.x;
					o.tangentToWorldAndParallax[1].w = viewDirForParallax.y;
					o.tangentToWorldAndParallax[2].w = viewDirForParallax.z;
				#endif
				
				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}
			
			
			half4 myFrag (VertexOutputForwardBase i) : SV_Target
			{
				FRAGMENT_SETUP(s)
				UnityLight mainLight = MainLight ();
				half atten = SHADOW_ATTENUATION(i);
				
				half occlusion = Occlusion(i.tex.xy);
				UnityGI gi = FragmentGI (
					s.posWorld, occlusion, i.ambientOrLightmapUV, atten, s.smoothness, s.normalWorld, s.eyeVec, mainLight);

				half4 c = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
				c.rgb += UNITY_BRDF_GI (s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, occlusion, gi);
				c.rgb += Emission(i.tex.xy);

				UNITY_APPLY_FOG(i.fogCoord, c.rgb);
				return OutputForward (c, s.alpha);
			}

			ENDCG
		}
		
		
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual

			CGPROGRAM
			#pragma target 3.0
			// GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles

			// -------------------------------------

			
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP
			
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			
			#pragma vertex vertForwardAdd
			#pragma fragment fragForwardAdd

			#include "UnityStandardCore.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 3.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles
			
			// -------------------------------------


			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Deferred pass
		Pass
		{
			Name "DEFERRED"
			Tags { "LightMode" = "Deferred" }

			CGPROGRAM
			#pragma target 3.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers nomrt gles
			

			// -------------------------------------

			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP

			#pragma multi_compile ___ UNITY_HDR_ON
			#pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
			#pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
			#pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON
			
			#pragma vertex vertDeferred
			#pragma fragment fragDeferred

			#include "UnityStandardCore.cginc"

			ENDCG
		}

		// ------------------------------------------------------------------
		// Extracts information for lightmapping, GI (emission, albedo, ...)
		// This pass it not used during regular rendering.
		Pass
		{
			Name "META" 
			Tags { "LightMode"="Meta" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2

			#include "UnityStandardMeta.cginc"
			ENDCG
		}
		
		
		
		
		
		
		
		
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 150

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			CGPROGRAM
			#pragma target 2.0
			
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION 
			#pragma shader_feature _METALLICGLOSSMAP 
			#pragma shader_feature ___ _DETAIL_MULX2
			// SM2.0: NOT SUPPORTED shader_feature _PARALLAXMAP

			#pragma skip_variants SHADOWS_SOFT DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
	
			#pragma vertex vertForwardBase
			#pragma fragment fragForwardBase

			#include "UnityStandardCore.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual
			
			CGPROGRAM
			#pragma target 2.0

			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			// SM2.0: NOT SUPPORTED shader_feature _PARALLAXMAP
			#pragma skip_variants SHADOWS_SOFT
			
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			
			#pragma vertex vertForwardAdd
			#pragma fragment fragForwardAdd

			#include "UnityStandardCore.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 2.0

			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma skip_variants SHADOWS_SOFT
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"

			ENDCG
		}

		// ------------------------------------------------------------------
		// Extracts information for lightmapping, GI (emission, albedo, ...)
		// This pass it not used during regular rendering.
		Pass
		{
			Name "META" 
			Tags { "LightMode"="Meta" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2

			#include "UnityStandardMeta.cginc"
			ENDCG
		}
	}

	FallBack "VertexLit"
	CustomEditor "StandardShaderGUI"
}
