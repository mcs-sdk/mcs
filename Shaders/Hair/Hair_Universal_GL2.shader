Shader "Morph3D/Volund Variants/Standard Hair Legacy"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}
		
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
		_SpecColor("Specular", Color) = (0.2,0.2,0.2)
		_SpecGlossMap("Specular", 2D) = "white" {}

		_BumpScale("Scale", Float) = 1.0
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

		_KKFlowMap("KK FlowMap", 2D) = "white" {}
		_KKReflectionSmoothness("KK Reflective Smoothness", Range(0.0, 1.0)) = 0.5
		_KKReflectionGrayScale("KK Reflective Gray Scale", Range(0.0, 48.0)) = 5.0
		_KKPrimarySpecularColor("KK Primary Specular Color", Color) = (1.0, 1.0, 1.0)	
		_KKPrimarySpecularExponent("KK Primary Exponent", Range(1.0, 192.0)) = 64.0
		_KKPrimaryRootShift("KK Primary Root Shift", Range(-1.0, 1.0)) = 0.275
		_KKSecondarySpecularColor("KK Secondary Specular Color", Color) = (1.0, 1.0, 1.0)	
		_KKSecondarySpecularExponent("KK Secondary Exponent", Range(1.0, 192.0)) = 48.0
		_KKSecondaryRootShift("KK Secondary Root Shift", Range(-1.0, 1.0)) = -0.040
		_KKSpecularMixDirectFactors("KK Spec Mix Direct Factors", Vector) = (0.15, 0.10, 0.05, 0)
		_KKSpecularMixIndirectFactors("KK Spec Mix Indirect Factors", Vector) = (0.75, 0.60, 0.15, 0)
		
		[HideInInspector] _SrcBlend ("__src", Float) = 5.0
		[HideInInspector] _DstBlend ("__dst", Float) = 10.0
		[HideInInspector] _ZWrite ("__zw", Float) = 1.0
		[HideInInspector] _ZTest ("__zt", Float) = 4.0
		[HideInInspector] _Cull ("__cull", Float) = 0.0
	}

	CGINCLUDE
		#define UNITY_SETUP_BRDF_INPUT SpecularSetup
	ENDCG

	SubShader
	{
		Tags { "Special"="Hair" "RenderType"="Transparent" "PerformanceChecks"="False" }
		
		// Low LOD level since there's no meaningful fallback.
		LOD 150

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			ZTest [_ZTest]
			Cull [_Cull]
			
			CGPROGRAM
			#pragma target 2.5
			#pragma only_renderers d3d11 d3d9 opengl glcore gles gles3 metal
			
			#pragma shader_feature _NORMALMAP
			#pragma multi_compile _ _ALPHATEST_ON
			#pragma multi_compile _ _ALPHABLEND_ON
			#pragma multi_compile _ _UNITY_5_4
			#pragma shader_feature _EMISSIONMAP
			#pragma shader_feature _SPECGLOSSMAP 
			#pragma shader_feature ___ _DETAIL_MULX2
			//NEVER ON shader_feature _PARALLAXMAP
			
			#pragma multi_compile_fwdbase 
			#pragma multi_compile_fog

			// Volund variants
			// (these debug variants should only be active in Editor, but there's currently no automatic
			//  way of defining variants for editor-only)
			#pragma multi_compile DBG_NONE DBG_OCCLUSION DBG_GRAYMASK DBG_MASKEDALBEDO DBG_SPECULAR DBG_LIGHTING DBG_FLOW

			#pragma vertex vertForwardBase
			#pragma fragment fragForwardBase
			
			#include "Hair_setup.cginc"
			
			ENDCG
		}
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			ZWrite Off
			ZTest [_ZTest]
			Cull [_Cull]

			CGPROGRAM
			#pragma target 2.5
			
			#pragma only_renderers d3d11 d3d9 opengl glcore gles gles3 metal

			#pragma shader_feature _NORMALMAP
			#pragma multi_compile _ _ALPHATEST_ON _ALPHABLEND_ON
			#pragma multi_compile _ _UNITY_5_4
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			//NEVER ON shader_feature _PARALLAXMAP
			
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog

			// Volund variants
			#define DBG_NONE
			#define ADDITIVE_PASS
			
			#pragma vertex vertForwardAdd
			#pragma fragment fragForwardAdd

			#include "Hair_setup.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 2.5

			#pragma only_renderers d3d11 d3d9 opengl glcore gles gles3 metal
			
			#define UNITY_BRDF_PBS BRDF1_Unity_PBS_KK

			#pragma multi_compile _ _ALPHATEST_ON _ALPHABLEND_ON
			#pragma multi_compile_shadowcaster
			#pragma multi_compile _ _UNITY_5_4

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"

			ENDCG
		}	
	}

	FallBack Off
}

