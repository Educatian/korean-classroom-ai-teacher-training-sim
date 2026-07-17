Shader "AdieLab/StudentClothingTint"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _FabricTex ("Imagegen Fabric Albedo", 2D) = "gray" {}
        _GraphicAtlas ("Imagegen Chest Graphic Atlas", 2D) = "black" {}
        _ClothingColor ("Clothing Color", Color) = (0.1,0.2,0.35,1)
        _AccentColor ("Clothing Accent", Color) = (0.85,0.82,0.70,1)
        _GraphicColor ("Chest Graphic Color", Color) = (0.92,0.88,0.72,1)
        _TintStrength ("Tint Strength", Range(0,1)) = 0.78
        _PatternType ("Pattern Type", Range(0,6)) = 0
        _PatternScale ("Pattern Scale", Range(1,24)) = 10
        _PatternStrength ("Pattern Strength", Range(0,1)) = 0
        _FabricScale ("Fabric Texture Scale", Range(0.5,8)) = 2
        _FabricStrength ("Fabric Texture Strength", Range(0,1)) = 0.32
        _GraphicIndex ("Chest Graphic Index", Range(0,14)) = 0
        _GraphicStrength ("Chest Graphic Strength", Range(0,1)) = 0.92
        _GraphicScale ("Chest Graphic Scale", Range(0.6,1.4)) = 1
        _GraphicRotation ("Chest Graphic Rotation", Range(-20,20)) = 0
        _GraphicOffset ("Chest Graphic Offset", Vector) = (0,0,0,0)
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0,2)) = 1
        _Metallic ("Metallic", Range(0,1)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.28
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _FabricTex;
        sampler2D _GraphicAtlas;
        sampler2D _BumpMap;
        fixed4 _Color;
        fixed4 _ClothingColor;
        fixed4 _AccentColor;
        fixed4 _GraphicColor;
        half _TintStrength;
        half _PatternType;
        half _PatternScale;
        half _PatternStrength;
        half _FabricScale;
        half _FabricStrength;
        half _GraphicIndex;
        half _GraphicStrength;
        half _GraphicScale;
        half _GraphicRotation;
        half4 _GraphicOffset;
        half _BumpScale;
        half _Metallic;
        half _Glossiness;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
        };

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            fixed4 source = tex2D(_MainTex, input.uv_MainTex) * _Color;
            half warmRed = saturate((source.r - source.b) * 5.5);
            half warmGreen = saturate((source.g - source.b) * 7.0);
            half skinBrightness = smoothstep(0.20, 0.48, source.r);
            half skinMask = saturate(warmRed * warmGreen * skinBrightness);
            half luminance = dot(source.rgb, half3(0.299, 0.587, 0.114));
            half maxChannel = max(source.r, max(source.g, source.b));
            half minChannel = min(source.r, min(source.g, source.b));
            half chroma = maxChannel - minChannel;
            half torsoX = smoothstep(0.325, 0.355, input.uv_MainTex.x) *
                (1.0 - smoothstep(0.665, 0.695, input.uv_MainTex.x));
            half torsoY = smoothstep(0.12, 0.16, input.uv_MainTex.y) *
                (1.0 - smoothstep(0.91, 0.95, input.uv_MainTex.y));
            half torsoMask = torsoX * torsoY;
            half chromaGarmentMask = smoothstep(0.18, 0.38, chroma) * (1.0 - skinMask);
            half garmentMask = max(chromaGarmentMask, torsoMask);

            half2 patternUv = input.uv_MainTex * max(_PatternScale, 1.0);
            half horizontal = smoothstep(0.72, 0.96, abs(sin(patternUv.y * 3.14159265)));
            half vertical = smoothstep(0.72, 0.96, abs(sin(patternUv.x * 3.14159265)));
            half checker = horizontal * vertical;
            half2 dotCell = frac(patternUv) - 0.5;
            half dots = 1.0 - smoothstep(0.11, 0.23, length(dotCell));
            half diagonal = smoothstep(0.72, 0.96, abs(sin((patternUv.x + patternUv.y) * 2.22144147)));
            half chestBand = smoothstep(0.38, 0.42, input.uv_MainTex.y) *
                (1.0 - smoothstep(0.53, 0.57, input.uv_MainTex.y));
            half pattern = 0.0;
            if (_PatternType < 0.5)
            {
                pattern = 0.0;
            }
            else if (_PatternType < 1.5)
            {
                pattern = horizontal;
            }
            else if (_PatternType < 2.5)
            {
                pattern = vertical;
            }
            else if (_PatternType < 3.5)
            {
                pattern = checker;
            }
            else if (_PatternType < 4.5)
            {
                pattern = dots;
            }
            else if (_PatternType < 5.5)
            {
                pattern = diagonal;
            }
            else
            {
                pattern = chestBand;
            }

            half sourceFabricShading = 0.68 + luminance * 0.46;
            half fabricShading = lerp(sourceFabricShading, 0.96, torsoMask * 0.86);
            half3 generatedFabric = tex2D(_FabricTex, input.uv_MainTex * _FabricScale).rgb;
            half generatedFabricLuminance = dot(generatedFabric, half3(0.299, 0.587, 0.114));
            half generatedFabricDetail = lerp(1.0, 0.68 + generatedFabricLuminance * 0.64, _FabricStrength);
            half3 baseFabric = _ClothingColor.rgb * fabricShading * generatedFabricDetail;
            half3 accentFabric = _AccentColor.rgb * fabricShading * generatedFabricDetail;
            half3 fabric = lerp(baseFabric, accentFabric, pattern * _PatternStrength);

            half2 graphicLocal = input.uv_MainTex - (half2(0.5, 0.43) + _GraphicOffset.xy);
            graphicLocal /= half2(0.155, 0.135) * max(_GraphicScale, 0.01);
            half angle = radians(_GraphicRotation);
            half sine = sin(angle);
            half cosine = cos(angle);
            graphicLocal = half2(
                graphicLocal.x * cosine - graphicLocal.y * sine,
                graphicLocal.x * sine + graphicLocal.y * cosine) + 0.5;
            half cellInterior = smoothstep(0.035, 0.075, graphicLocal.x) *
                (1.0 - smoothstep(0.925, 0.965, graphicLocal.x)) *
                smoothstep(0.035, 0.075, graphicLocal.y) *
                (1.0 - smoothstep(0.925, 0.965, graphicLocal.y));
            half graphicIndex = clamp(floor(_GraphicIndex + 0.5), 0.0, 14.0);
            half graphicColumn = fmod(graphicIndex, 4.0);
            half graphicRow = floor(graphicIndex / 4.0);
            half2 atlasUv = half2(
                (graphicColumn + saturate(graphicLocal.x)) * 0.25,
                1.0 - (graphicRow + 1.0 - saturate(graphicLocal.y)) * 0.25);
            half3 graphicSample = tex2D(_GraphicAtlas, atlasUv).rgb;
            half graphicLuminance = dot(graphicSample, half3(0.299, 0.587, 0.114));
            half graphicMask = smoothstep(0.12, 0.72, graphicLuminance) *
                cellInterior * torsoMask * garmentMask * _GraphicStrength;
            fabric = lerp(fabric, _GraphicColor.rgb * fabricShading, graphicMask);

            half clothingBlend = saturate(_TintStrength * garmentMask + torsoMask * 0.06);

            output.Albedo = lerp(source.rgb, fabric, clothingBlend);
            output.Normal = UnpackScaleNormal(tex2D(_BumpMap, input.uv_BumpMap), _BumpScale);
            output.Metallic = _Metallic;
            output.Smoothness = _Glossiness;
            output.Alpha = source.a;
        }
        ENDCG
    }

    FallBack "Standard"
}
