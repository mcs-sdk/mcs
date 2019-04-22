#ifndef FILE_HAIR_SETUP_CGINC
#define FILE_HAIR_SETUP_CGINC

#include "Hair_UnityStandardInput.cginc"

#include "UnityCG.cginc"
#include "Lighting.cginc"
 
sampler2D _KKFlowMap;
half _KKReflectionSmoothness;
half _KKReflectionGrayScale;
half4 _KKPrimarySpecularColor;
half _KKPrimarySpecularExponent;
half _KKPrimaryRootShift;
half4 _KKSecondarySpecularColor;
half _KKSecondarySpecularExponent;
half _KKSecondaryRootShift;
half3 _KKSpecularMixDirectFactors;
half3 _KKSpecularMixIndirectFactors;

half KKDiffuseApprox(half3 normal, half3 lightDir) {
	return max(0.f, dot(normal, lightDir) * 0.75f + 0.25f);
}

half3 BRDF_Unity_KK_ish(half3 baseColor, half3 specColor, half reflectivity, half roughness, half3 normal, half3 normalVertex, half3 viewDir, UnityLight light, UnityIndirect indirect, half3 specGI, half3 tanDir1, half3 tanDir2, half occlusion, half atten) {
	half3 halfDir = normalize (light.dir + viewDir);
	half nl = light.ndotl;
	half nh = BlinnTerm (normal, halfDir);
	half sp = RoughnessToSpecPower (roughness);
		
	half diffuseTerm = nl;
	half specularTerm = pow(nh, sp);
	
	// Poor man's KK. Not physically correct.
	half th1 = dot(tanDir1, halfDir);
	half th2 = dot(tanDir2, halfDir);
	
	half3 kkSpecTermPrimary = pow(sqrt(1.f - th1 * th1), _KKPrimarySpecularExponent) * _KKPrimarySpecularColor.rgb;
	half3 kkSpecTermSecondary = pow(sqrt(1.f - th2 * th2), _KKSecondarySpecularExponent) * _KKSecondarySpecularColor.rgb;
	half3 kkSpecTermBlinn = specularTerm * specColor;

	half kkDirectFactor = min(1.f, Luminance(indirect.diffuse) + nl * atten);
	_KKSpecularMixDirectFactors *= kkDirectFactor;
	
	half3 kkSpecTermDirect = kkSpecTermPrimary * _KKSpecularMixDirectFactors.x
		+ kkSpecTermSecondary * _KKSpecularMixDirectFactors.y
		+ kkSpecTermBlinn * _KKSpecularMixDirectFactors.z;
	kkSpecTermDirect *= light.color;
	
	half3 kkSpecTermIndirect = kkSpecTermPrimary * _KKSpecularMixIndirectFactors.x
		+ kkSpecTermSecondary * _KKSpecularMixIndirectFactors.y
		+ kkSpecTermBlinn * _KKSpecularMixIndirectFactors.z;			
	kkSpecTermIndirect *= specGI;

#ifdef DBG_LIGHTING
	baseColor = 0.5f;
#endif
#ifdef DBG_SPECULAR
	baseColor = 0;
#endif

	half3 diffuseColor = baseColor;
	half3 color = half3(0.f, 0.f, 0.f);	
	color += baseColor * (indirect.diffuse + light.color * diffuseTerm);
	color += (kkSpecTermIndirect + kkSpecTermDirect) * occlusion;
					
	return color;
}

// We'll use this for now, but we're really just wasting performance 
// including a full PBS setup and then throwing most of it away.
//
// For the time being, this is a convenient way of keeping up with enlighten and material
// setup changes, though. (We still need quite a few modifications to inject ourselves into it)
#include "Hair_UnityStandardCore.cginc"

#endif // FILE_HAIR_SETUP_CGINC
