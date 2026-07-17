Shader "AdieLab/StudentHeadHairTint"
{
    Properties
    {
        _MainTex ("Generated Head Albedo", 2D) = "white" {}
        _ReferenceTex ("Rocketbox Eye Socket Reference", 2D) = "white" {}
        _EyeSocketCorrection ("Eye Socket Correction", Range(0,1)) = 0.90
        _EyeSocketBrightness ("Eye Socket Skin Brightness", Range(0.75,1.05)) = 0.88
        _PaintedEyeCenters ("Painted Eye Centers (Lx Ly Rx Ry)", Vector) = (0.433,0.705,0.567,0.705)
        _PaintedEyeRadius ("Painted Eye Mask Radius", Vector) = (0.060,0.030,0,0)
        _SocketEyeCenters ("Rocketbox Socket Centers (Lx Ly Rx Ry)", Vector) = (0.431,0.718,0.570,0.718)
        _SocketEyeRadius ("Rocketbox Socket Radius", Vector) = (0.062,0.035,0,0)
        _SocketBlendStrength ("Socket Restoration Strength", Range(0,1)) = 0.72
        _PaintedMouthCenterRadius ("Painted Mouth Center and Radius", Vector) = (0.500,0.558,0.105,0.055)
        _MouthRestoreStrength ("Rigged Mouth Restoration Strength", Range(0,1)) = 0.0
        _PaintedNoseCenterRadius ("Painted Central Feature Center and Radius", Vector) = (0.500,0.610,0.130,0.125)
        _NoseNeutralizeStrength ("Rigged Nose Neutralization Strength", Range(0,1)) = 0.0
        _FacialNormalSuppression ("Facial Normal Suppression", Range(0,1)) = 1.0
        _IrisAtlasCenter ("Rocketbox Iris Atlas Center", Vector) = (0.2636,0.0688,0,0)
        _IrisColor ("Reddish Brown Iris", Color) = (0.30,0.11,0.04,1)
        _IrisTintStrength ("Iris Tint Strength", Range(0,1)) = 1.0
        _IrisRadius ("Iris Radius", Range(0.018,0.034)) = 0.0235
        _PupilRadius ("Pupil Radius", Range(0.004,0.012)) = 0.0075
        _HairColor ("Natural Hair Color", Color) = (0.018,0.014,0.012,1)
        _HairTintStrength ("Hair Tint Strength", Range(0,1)) = 0.92
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0,2)) = 1
        _Glossiness ("Smoothness", Range(0,1)) = 0.22
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _ReferenceTex;
        sampler2D _BumpMap;
        half _EyeSocketCorrection;
        half _EyeSocketBrightness;
        float4 _PaintedEyeCenters;
        float4 _PaintedEyeRadius;
        float4 _SocketEyeCenters;
        float4 _SocketEyeRadius;
        half _SocketBlendStrength;
        float4 _PaintedMouthCenterRadius;
        half _MouthRestoreStrength;
        float4 _PaintedNoseCenterRadius;
        half _NoseNeutralizeStrength;
        half _FacialNormalSuppression;
        float4 _IrisAtlasCenter;
        fixed4 _IrisColor;
        half _IrisTintStrength;
        half _IrisRadius;
        half _PupilRadius;
        fixed4 _HairColor;
        half _HairTintStrength;
        half _BumpScale;
        half _Glossiness;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
        };

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            fixed4 source = tex2D(_MainTex, input.uv_MainTex);
            fixed3 reference = tex2D(_ReferenceTex, input.uv_MainTex).rgb;
            half2 leftPaintedEye = (input.uv_MainTex - _PaintedEyeCenters.xy) / _PaintedEyeRadius.xy;
            half2 rightPaintedEye = (input.uv_MainTex - _PaintedEyeCenters.zw) / _PaintedEyeRadius.xy;
            half leftEyeRegion = 1.0 - smoothstep(0.70, 1.04, dot(leftPaintedEye, leftPaintedEye));
            half rightEyeRegion = 1.0 - smoothstep(0.70, 1.04, dot(rightPaintedEye, rightPaintedEye));
            half paintedEyeRegion = max(leftEyeRegion, rightEyeRegion);
            fixed3 generatedLowerSkin = 0.4 * (
                tex2D(_MainTex, input.uv_MainTex + float2(0, -0.046)).rgb +
                tex2D(_MainTex, input.uv_MainTex + float2(0, -0.060)).rgb) +
                0.1 * (
                tex2D(_MainTex, input.uv_MainTex + float2(-0.075, -0.010)).rgb +
                tex2D(_MainTex, input.uv_MainTex + float2(0.075, -0.010)).rgb);
            half3 neutralized = lerp(source.rgb, generatedLowerSkin, paintedEyeRegion * _EyeSocketCorrection);
            half2 leftSocket = (input.uv_MainTex - _SocketEyeCenters.xy) / _SocketEyeRadius.xy;
            half2 rightSocket = (input.uv_MainTex - _SocketEyeCenters.zw) / _SocketEyeRadius.xy;
            half leftSocketRegion = 1.0 - smoothstep(0.72, 1.04, dot(leftSocket, leftSocket));
            half rightSocketRegion = 1.0 - smoothstep(0.72, 1.04, dot(rightSocket, rightSocket));
            half socketRegion = max(leftSocketRegion, rightSocketRegion);
            half2 leftAperture = (input.uv_MainTex - _SocketEyeCenters.xy) / (_SocketEyeRadius.xy * float2(0.82, 0.56));
            half2 rightAperture = (input.uv_MainTex - _SocketEyeCenters.zw) / (_SocketEyeRadius.xy * float2(0.82, 0.56));
            half leftApertureRegion = 1.0 - smoothstep(0.70, 1.04, dot(leftAperture, leftAperture));
            half rightApertureRegion = 1.0 - smoothstep(0.70, 1.04, dot(rightAperture, rightAperture));
            half socketRestoreMask = max(max(leftApertureRegion, rightApertureRegion), socketRegion * 0.08);
            fixed3 referenceLowerSkin = 0.5 * (
                tex2D(_ReferenceTex, input.uv_MainTex + float2(0, -0.052)).rgb +
                tex2D(_ReferenceTex, input.uv_MainTex + float2(0, -0.066)).rgb);
            half3 socketToneRatio = clamp(generatedLowerSkin / max(referenceLowerSkin, half3(0.05, 0.05, 0.05)), 0.62, 1.58);
            half3 restoredSocket = reference * socketToneRatio;
            half3 socketCorrected = lerp(neutralized, restoredSocket, socketRestoreMask * _SocketBlendStrength);
            half2 paintedMouth = (input.uv_MainTex - _PaintedMouthCenterRadius.xy) / _PaintedMouthCenterRadius.zw;
            half paintedMouthRegion = 1.0 - smoothstep(0.68, 1.06, dot(paintedMouth, paintedMouth));
            half3 generatedFeatureSkin = 0.25 * (
                tex2D(_MainTex, float2(0.350, 0.610)).rgb +
                tex2D(_MainTex, float2(0.650, 0.610)).rgb +
                tex2D(_MainTex, float2(0.370, 0.570)).rgb +
                tex2D(_MainTex, float2(0.630, 0.570)).rgb);
            half3 referenceFeatureSkin = 0.25 * (
                tex2D(_ReferenceTex, float2(0.350, 0.610)).rgb +
                tex2D(_ReferenceTex, float2(0.650, 0.610)).rgb +
                tex2D(_ReferenceTex, float2(0.370, 0.570)).rgb +
                tex2D(_ReferenceTex, float2(0.630, 0.570)).rgb);
            half3 featureToneRatio = clamp(
                generatedFeatureSkin / max(referenceFeatureSkin, half3(0.05, 0.05, 0.05)),
                0.62,
                1.58);
            half3 restoredFeature = reference * featureToneRatio;
            half3 restoredMouth = restoredFeature;
            half3 mouthCorrected = lerp(socketCorrected, restoredMouth, paintedMouthRegion * _MouthRestoreStrength);
            half2 paintedNose = (input.uv_MainTex - _PaintedNoseCenterRadius.xy) / _PaintedNoseCenterRadius.zw;
            half paintedNoseRegion = 1.0 - smoothstep(0.64, 1.06, dot(paintedNose, paintedNose));
            half3 noseCorrected = lerp(mouthCorrected, restoredFeature, paintedNoseRegion * _NoseNeutralizeStrength);
            half3 corrected = noseCorrected * _EyeSocketBrightness;

            half luminance = dot(corrected, half3(0.299, 0.587, 0.114));
            half redness = saturate((corrected.r - corrected.b) * 4.0);
            half skinBrightness = smoothstep(0.43, 0.62, luminance);
            half skinMask = saturate(redness * skinBrightness);
            half darkDetail = 1.0 - smoothstep(0.05, 0.20, luminance);
            half hairCandidate = 1.0 - smoothstep(0.48, 0.68, luminance);
            half hairMask = saturate(hairCandidate * (1.0 - skinMask) - darkDetail);
            half preservedDetail = lerp(0.58, 1.18, luminance);
            half3 tintedHair = _HairColor.rgb * preservedDetail;
            half faceHorizontal = smoothstep(0.20, 0.30, input.uv_MainTex.x) *
                (1.0 - smoothstep(0.70, 0.80, input.uv_MainTex.x));
            half faceVertical = smoothstep(0.43, 0.50, input.uv_MainTex.y) *
                (1.0 - smoothstep(0.79, 0.86, input.uv_MainTex.y));
            half facialRegion = faceHorizontal * faceVertical;
            half3 mappedNormal = UnpackScaleNormal(tex2D(_BumpMap, input.uv_BumpMap), _BumpScale);
            half3 surfaceAlbedo = lerp(corrected, tintedHair, hairMask * _HairTintStrength * (1.0 - paintedEyeRegion));

            half2 eyeAtlasPosition = (input.uv_MainTex - _IrisAtlasCenter.xy) / float2(0.078, 0.074);
            half eyeAtlasMask = 1.0 - smoothstep(0.84, 1.05, dot(eyeAtlasPosition, eyeAtlasPosition));
            half2 irisPosition = (input.uv_MainTex - _IrisAtlasCenter.xy) / float2(_IrisRadius, _IrisRadius * 0.96);
            half irisMask = 1.0 - smoothstep(0.70, 1.04, dot(irisPosition, irisPosition));
            half2 pupilPosition = (input.uv_MainTex - _IrisAtlasCenter.xy) / float2(_PupilRadius, _PupilRadius);
            half pupilMask = 1.0 - smoothstep(0.68, 1.04, dot(pupilPosition, pupilPosition));
            half referenceLuminance = dot(reference, half3(0.299, 0.587, 0.114));
            half highlightMask = smoothstep(0.90, 0.98, referenceLuminance);
            half3 reddishBrownIris = _IrisColor.rgb * lerp(0.85, 1.18, saturate(referenceLuminance * 2.2));
            half irisTintMask = irisMask * (1.0 - pupilMask) * (1.0 - highlightMask) * _IrisTintStrength;
            half3 eyeAlbedo = lerp(reference, reddishBrownIris, irisTintMask);

            output.Albedo = lerp(surfaceAlbedo, eyeAlbedo, eyeAtlasMask);
            output.Normal = normalize(lerp(mappedNormal, half3(0, 0, 1), facialRegion * _FacialNormalSuppression));
            output.Metallic = 0;
            output.Smoothness = _Glossiness;
            output.Alpha = source.a;
        }
        ENDCG
    }

    FallBack "Standard"
}
