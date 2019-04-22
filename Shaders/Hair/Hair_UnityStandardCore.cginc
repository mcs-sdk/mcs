// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

#ifndef UNITY_STANDARD_CORE_INCLUDED
#define UNITY_STANDARD_CORE_INCLUDED

//begin-volund-hair: Override builtin standard input
#include "Hair_UnityStandardInput.cginc"
//end-volund-hair:

#include "UnityCG.cginc"
#include "UnityShaderVariables.cginc"
#include "UnityStandardConfig.cginc"
#include "UnityPBSLighting.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityStandardBRDF.cginc"

#include "AutoLight.cginc"

//-------------------------------------------------------------------------------------
// counterpart for NormalizePerPixelNormal
// skips normalization per-vertex and expects normalization to happen per-pixel
half3 NormalizePerVertexNormal (half3 n)
{
	#if (SHADER_TARGET < 30)
		return normalize(n);
	#else
		return n; // will normalize per-pixel instead
	#endif
}

half3 NormalizePerPixelNormal (half3 n)
{
	#if (SHADER_TARGET < 30)
		return n;
	#else
		return normalize(n);
	#endif
}

//-------------------------------------------------------------------------------------
UnityLight MainLight (half3 normalWorld)
{
	UnityLight l;
	#ifdef LIGHTMAP_OFF
		
		l.color = _LightColor0.rgb;
		l.dir = _WorldSpaceLightPos0.xyz;
		l.ndotl = LambertTerm (normalWorld, l.dir);
	#else
		// no light specified by the engine
		// analytical light might be extracted from Lightmap data later on in the shader depending on the Lightmap type
		l.color = half3(0.f, 0.f, 0.f);
		l.ndotl  = 0.f;
		l.dir = half3(0.f, 0.f, 0.f);
	#endif

	return l;
}

UnityLight AdditiveLight (half3 normalWorld, half3 lightDir, half atten)
{
	UnityLight l;

	l.color = _LightColor0.rgb;
	l.dir = lightDir;
	#ifndef USING_DIRECTIONAL_LIGHT
		l.dir = NormalizePerPixelNormal(l.dir);
	#endif
	l.ndotl = LambertTerm (normalWorld, l.dir);

	// shadow the light
	l.color *= atten;
	return l;
}

UnityLight DummyLight (half3 normalWorld)
{
	UnityLight l;
	l.color = 0;
	l.dir = half3 (0,1,0);
	l.ndotl = LambertTerm (normalWorld, l.dir);
	return l;
}

UnityIndirect ZeroIndirect ()
{
	UnityIndirect ind;
	ind.diffuse = 0;
	ind.specular = 0;
	return ind;
}

//-------------------------------------------------------------------------------------
// Common fragment setup
half3 WorldNormal(half4 tan2world[3])
{
	return normalize(tan2world[2].xyz);
}

#ifdef _TANGENT_TO_WORLD
	half3x3 ExtractTangentToWorldPerPixel(half4 tan2world[3])
	{
		half3 t = tan2world[0].xyz;
		half3 b = tan2world[1].xyz;
		half3 n = tan2world[2].xyz;

	#if UNITY_TANGENT_ORTHONORMALIZE
		n = NormalizePerPixelNormal(n);

		// ortho-normalize Tangent
		t = normalize (t - n * dot(t, n));

		// recalculate Binormal
		half3 newB = cross(n, t);
		b = newB * sign (dot (newB, b));
	#endif

		return half3x3(t, b, n);
	}
#else
	half3x3 ExtractTangentToWorldPerPixel(half4 tan2world[3])
	{
		return half3x3(0,0,0,0,0,0,0,0,0);
	}
#endif

#ifdef _PARALLAXMAP
	#define IN_VIEWDIR4PARALLAX(i) NormalizePerPixelNormal(half3(i.tangentToWorldAndParallax[0].w,i.tangentToWorldAndParallax[1].w,i.tangentToWorldAndParallax[2].w))
	#define IN_VIEWDIR4PARALLAX_FWDADD(i) NormalizePerPixelNormal(i.viewDirForParallax.xyz)
#else
	#define IN_VIEWDIR4PARALLAX(i) half3(0,0,0)
	#define IN_VIEWDIR4PARALLAX_FWDADD(i) half3(0,0,0)
#endif

#if UNITY_SPECCUBE_BOX_PROJECTION
	#define IN_WORLDPOS(i) i.posWorld
#else
	#define IN_WORLDPOS(i) half3(0,0,0)
#endif

#define IN_LIGHTDIR_FWDADD(i) half3(i.tangentToWorldAndLightDir[0].w, i.tangentToWorldAndLightDir[1].w, i.tangentToWorldAndLightDir[2].w)

#define FRAGMENT_SETUP(x) FragmentCommonData x = \
	FragmentSetup(i.tex, i.eyeVec, WorldNormal(i.tangentToWorldAndParallax), IN_VIEWDIR4PARALLAX(i), ExtractTangentToWorldPerPixel(i.tangentToWorldAndParallax), IN_WORLDPOS(i));

#define FRAGMENT_SETUP_FWDADD(x) FragmentCommonData x = \
	FragmentSetup(i.tex, i.eyeVec, WorldNormal(i.tangentToWorldAndLightDir), IN_VIEWDIR4PARALLAX_FWDADD(i), ExtractTangentToWorldPerPixel(i.tangentToWorldAndLightDir), half3(0,0,0));

struct FragmentCommonData
{
	half3 diffColor, specColor;
	// Note: oneMinusRoughness & oneMinusReflectivity for optimization purposes, mostly for DX9 SM2.0 level.
	// Most of the math is being done on these (1-x) values, and that saves a few precious ALU slots.
	half oneMinusReflectivity, oneMinusRoughness;
	half3 normalWorld, eyeVec, posWorld;
	half alpha;
};

#ifndef UNITY_SETUP_BRDF_INPUT
	#define UNITY_SETUP_BRDF_INPUT SpecularSetup
#endif

inline FragmentCommonData SpecularSetup (float4 i_tex)
{
	half4 specGloss = SpecularGloss(i_tex.xy);
	half3 specColor = specGloss.rgb;
	half oneMinusRoughness = specGloss.a;

	half oneMinusReflectivity;
	half3 diffColor = EnergyConservationBetweenDiffuseAndSpecular (Albedo(i_tex), specColor, /*out*/ oneMinusReflectivity);
	
	FragmentCommonData o = (FragmentCommonData)0;
	o.diffColor = diffColor;
	o.specColor = specColor;
	o.oneMinusReflectivity = oneMinusReflectivity;
	o.oneMinusRoughness = oneMinusRoughness;
	return o;
}

inline FragmentCommonData MetallicSetup (float4 i_tex)
{
	half2 metallicGloss = MetallicGloss(i_tex.xy);
	half metallic = metallicGloss.x;
	half oneMinusRoughness = metallicGloss.y;

	half oneMinusReflectivity;
	half3 specColor;
	half3 diffColor = DiffuseAndSpecularFromMetallic (Albedo(i_tex), metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

	FragmentCommonData o = (FragmentCommonData)0;
	o.diffColor = diffColor;
	o.specColor = specColor;
	o.oneMinusReflectivity = oneMinusReflectivity;
	o.oneMinusRoughness = oneMinusRoughness;
	return o;
} 

inline FragmentCommonData FragmentSetup (float4 i_tex, half3 i_eyeVec, half3 i_normalWorld, half3 i_viewDirForParallax, half3x3 i_tanToWorld, half3 i_posWorld)
{
	i_tex = Parallax(i_tex, i_viewDirForParallax);

	half alpha = Alpha(i_tex.xy);
	#if defined(_ALPHATEST_ON)
		clip (alpha - _Cutoff);
	#endif

	#ifdef _NORMALMAP
		half3 normalWorld = NormalizePerPixelNormal(mul(NormalInTangentSpace(i_tex), i_tanToWorld)); // @TODO: see if we can squeeze this normalize on SM2.0 as well
	#else
		// Should get compiled out, isn't being used in the end.
	 	half3 normalWorld = i_normalWorld;
	#endif
	
	half3 eyeVec = i_eyeVec;
	eyeVec = NormalizePerPixelNormal(eyeVec);


	FragmentCommonData o = UNITY_SETUP_BRDF_INPUT (i_tex);
	o.normalWorld = normalWorld;
	o.eyeVec = eyeVec;
	o.posWorld = i_posWorld;

	// NOTE: shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
	o.diffColor = PreMultiplyAlpha (o.diffColor, alpha, o.oneMinusReflectivity, /*out*/ o.alpha);
	return o;
}

inline UnityGI FragmentGI (
	float3 posWorld, 
	half occlusion, half4 i_ambientOrLightmapUV, half atten, half oneMinusRoughness, half3 normalWorld, half3 eyeVec,
	UnityLight light)
{
	UnityGIInput d;
	d.light = light;
	d.worldPos = posWorld;
	d.worldViewDir = -eyeVec;
	d.atten = atten;
	#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
		d.ambient = 0;
		d.lightmapUV = i_ambientOrLightmapUV;
	#else
		d.ambient = i_ambientOrLightmapUV.rgb;
		d.lightmapUV = 0;
	#endif
	d.boxMax[0] = unity_SpecCube0_BoxMax;
	d.boxMin[0] = unity_SpecCube0_BoxMin;
	d.probePosition[0] = unity_SpecCube0_ProbePosition;
	d.probeHDR[0] = unity_SpecCube0_HDR;

	d.boxMax[1] = unity_SpecCube1_BoxMax;
	d.boxMin[1] = unity_SpecCube1_BoxMin;
	d.probePosition[1] = unity_SpecCube1_ProbePosition;
	d.probeHDR[1] = unity_SpecCube1_HDR;

	return UnityGlobalIllumination (
		d, occlusion, oneMinusRoughness, normalWorld);
}

//-------------------------------------------------------------------------------------
half4 OutputForward (half4 output, half alphaFromSurface)
{
	#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
		output.a = alphaFromSurface;
	#else
		UNITY_OPAQUE_ALPHA(output.a);
	#endif
	return output;
}

// ------------------------------------------------------------------
//  Base forward pass (directional light, emission, lightmaps, ...)

struct VertexOutputForwardBase
{
	float4 pos							: SV_POSITION;
	float4 tex							: TEXCOORD0;
	half3 eyeVec 						: TEXCOORD1;
	half4 tangentToWorldAndParallax[3]	: TEXCOORD2;	// [3x3:tangentToWorld | 1x3:viewDirForParallax]
	half4 ambientOrLightmapUV			: TEXCOORD5;	// SH or Lightmap UV
	SHADOW_COORDS(6)
	UNITY_FOG_COORDS(7)

	// next ones would not fit into SM2.0 limits, but they are always for SM3.0+
	#if UNITY_SPECCUBE_BOX_PROJECTION
		float3 posWorld					: TEXCOORD8;
	#endif
	
//begin-volund-hair: Vertex color interp
	float4 vcolor						: TEXCOORD10;
//end-volund-hair:
};

VertexOutputForwardBase vertForwardBase (VertexInput v)
{
	VertexOutputForwardBase o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputForwardBase, o);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
	#if UNITY_SPECCUBE_BOX_PROJECTION
		o.posWorld = posWorld.xyz;
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
		o.tangentToWorldAndParallax[0].xyz = 0;
		o.tangentToWorldAndParallax[1].xyz = 0;
		o.tangentToWorldAndParallax[2].xyz = normalWorld;
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
	
//begin-volund-hair: Pass vertex color
	o.vcolor = v.color;
//end-volund-hair:

	UNITY_TRANSFER_FOG(o,o.pos);
	return o;
}

//begin-volund-hair: Shared helper
half4 ShadeHair(const half3 tanFlow, const half grayMask, const half vtxOcclusion, const half atten, const half3 normalWorldVertex, const half3x3 tangentToWorldMatrix, UnityLight light, UnityIndirect indirect, inout FragmentCommonData s) {
	half3 worldTangent = mul(tangentToWorldMatrix, tanFlow);
	half3 tanDir = normalize(worldTangent + normalWorldVertex.xyz * _KKPrimaryRootShift);
	half3 tanDir2 = normalize(worldTangent + normalWorldVertex.xyz * _KKSecondaryRootShift);
	
#ifndef ADDITIVE_PASS
	half3 reflectVector = reflect(s.eyeVec, normalWorldVertex);
	
	indirect.specular = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, reflectVector, _KKReflectionSmoothness);

	//for 5.4, you need to use: 
#ifdef UNITY_5_4
	UNITY_BRANCH
		if (unity_SpecCube0_BoxMin.w < 0.99999f) {
			indirect.specular = lerp(
				Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1, unity_SpecCube0), unity_SpecCube1_HDR, reflectVector, _KKReflectionSmoothness),
				indirect.specular,
				unity_SpecCube0_BoxMin.w
			);
		}
#else
	
	UNITY_BRANCH
	if (unity_SpecCube0_BoxMin.w < 0.99999f) {
		indirect.specular = lerp(
			Unity_GlossyEnvironment (UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube1_HDR, reflectVector, _KKReflectionSmoothness),
			indirect.specular,
			unity_SpecCube0_BoxMin.w
		);
	}
#endif
#endif
	
	s.diffColor *= grayMask;
	half specOcclusion = (1.f - min(1.f, _KKReflectionGrayScale * Luminance(s.diffColor))) * min(1.f, vtxOcclusion * 2.f);
	s.diffColor *= vtxOcclusion;

	half3 c = BRDF_Unity_KK_ish(s.diffColor, s.specColor, 1.f - s.oneMinusReflectivity, 1.f - s.oneMinusRoughness, s.normalWorld, normalWorldVertex, -s.eyeVec, light, indirect, indirect.specular, tanDir, tanDir2, specOcclusion, atten);
	return float4(c, 1.f);
}
//end-volund-hair:

half4 fragForwardBase (VertexOutputForwardBase i) : SV_Target
{
	FRAGMENT_SETUP(s)
	UnityLight mainLight = MainLight (s.normalWorld);
	half atten = SHADOW_ATTENUATION(i);

	half occlusion = Occlusion(i.tex.xy);
	UnityGI gi = FragmentGI (
		s.posWorld, occlusion, i.ambientOrLightmapUV, atten, s.oneMinusRoughness, s.normalWorld, s.eyeVec, mainLight);

//begin-volund-hair: Hair shading
	half vtxOcclusion = i.vcolor.b;
	half grayMask = i.vcolor.a;
	half3 tanFlow = tex2D(_KKFlowMap, i.tex.xy).xyz * 2.f - 1.f;
	
	half4 c = ShadeHair(tanFlow, grayMask, vtxOcclusion, atten, WorldNormal(i.tangentToWorldAndParallax), half3x3(i.tangentToWorldAndParallax[0].xyz, i.tangentToWorldAndParallax[1].xyz, i.tangentToWorldAndParallax[2].xyz), gi.light, gi.indirect, s);

#ifdef DBG_OCCLUSION
	return half4(vtxOcclusion, vtxOcclusion, vtxOcclusion, s.alpha);
#endif
#ifdef DBG_GRAYMASK
	return half4(grayMask, grayMask, grayMask, s.alpha);
#endif
#ifdef DBG_FLOW
	return half4(half3(tanFlow.x, tanFlow.y, -1.f) * 0.5 + 0.5, s.alpha);
#endif
#ifdef DBG_MASKEDALBEDO
	return half4(s.diffColor.rgb, s.alpha);
#endif
#ifdef DBG_SPECULAR 
	return half4(c.rgb, s.alpha);
#endif
//end-volund-hair:

	c.rgb += Emission(i.tex.xy);

	UNITY_APPLY_FOG(i.fogCoord, c.rgb);
	return OutputForward (c, s.alpha);
}

// ------------------------------------------------------------------
//  Additive forward pass (one light per pass)
struct VertexOutputForwardAdd
{
	float4 pos							: SV_POSITION;
	float4 tex							: TEXCOORD0;
	half3 eyeVec 						: TEXCOORD1;
	half4 tangentToWorldAndLightDir[3]	: TEXCOORD2;	// [3x3:tangentToWorld | 1x3:lightDir]
	LIGHTING_COORDS(5,6)
	UNITY_FOG_COORDS(7)

	// next ones would not fit into SM2.0 limits, but they are always for SM3.0+
#if defined(_PARALLAXMAP)
	half3 viewDirForParallax			: TEXCOORD8;
#endif

//begin-volund-hair: Vertex color interp
	float4 vcolor						: TEXCOORD10;
//end-volund-hair:
};

VertexOutputForwardAdd vertForwardAdd (VertexInput v)
{
	VertexOutputForwardAdd o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputForwardAdd, o);
	
	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
	o.pos = UnityObjectToClipPos(v.vertex);
	o.tex = TexCoords(v);
	o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
	float3 normalWorld = UnityObjectToWorldNormal(v.normal);
	#ifdef _TANGENT_TO_WORLD
		float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

		float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
		o.tangentToWorldAndLightDir[0].xyz = tangentToWorld[0];
		o.tangentToWorldAndLightDir[1].xyz = tangentToWorld[1];
		o.tangentToWorldAndLightDir[2].xyz = tangentToWorld[2];
	#else
		o.tangentToWorldAndLightDir[0].xyz = 0;
		o.tangentToWorldAndLightDir[1].xyz = 0;
		o.tangentToWorldAndLightDir[2].xyz = normalWorld;
	#endif
	//We need this for shadow receving
	TRANSFER_VERTEX_TO_FRAGMENT(o);

	float3 lightDir = _WorldSpaceLightPos0.xyz - posWorld.xyz * _WorldSpaceLightPos0.w;
	#ifndef USING_DIRECTIONAL_LIGHT
		lightDir = NormalizePerVertexNormal(lightDir);
	#endif
	o.tangentToWorldAndLightDir[0].w = lightDir.x;
	o.tangentToWorldAndLightDir[1].w = lightDir.y;
	o.tangentToWorldAndLightDir[2].w = lightDir.z;

	#ifdef _PARALLAXMAP
		TANGENT_SPACE_ROTATION;
		o.viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
	#endif
	
//begin-volund-hair: Pass vertex color
	o.vcolor = v.color;
//end-volund-hair:
	
	UNITY_TRANSFER_FOG(o,o.pos);
	return o;
}

half4 fragForwardAdd (VertexOutputForwardAdd i) : SV_Target
{
	FRAGMENT_SETUP_FWDADD(s)

//begin-volund-hair: Hair shading
#ifndef DBG_NONE
	return float4(0,0,0,0);
#endif

	half atten = LIGHT_ATTENUATION(i);
	UnityLight light = AdditiveLight(s.normalWorld, IN_LIGHTDIR_FWDADD(i), atten);
	light.ndotl = KKDiffuseApprox(s.normalWorld, light.dir);
	UnityIndirect indirect = ZeroIndirect();

	half vtxOcclusion = i.vcolor.b;
	half grayMask = i.vcolor.a;
	half3 tanFlow = tex2D(_KKFlowMap, i.tex.xy).xyz * 2.f - 1.f;
	
	half4 c = ShadeHair(tanFlow, grayMask, vtxOcclusion, atten, WorldNormal(i.tangentToWorldAndLightDir), half3x3(i.tangentToWorldAndLightDir[0].xyz, i.tangentToWorldAndLightDir[1].xyz, i.tangentToWorldAndLightDir[2].xyz), light, indirect, s);
//end-volund-hair:

	UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, half4(0,0,0,0)); // fog towards black in additive pass
	return OutputForward (c, s.alpha);
}	
			
#endif // UNITY_STANDARD_CORE_INCLUDED
