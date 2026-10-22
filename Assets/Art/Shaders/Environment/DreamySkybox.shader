Shader "MyGame/Environment/DreamySkybox"
{
    Properties
    {
        [Header(Gradient)]
        _ZenithColor ("Zenith", Color) = (0.13, 0.10, 0.36, 1)
        _UpperColor ("Upper Sky", Color) = (0.30, 0.22, 0.62, 1)
        _HorizonColor ("Horizon", Color) = (0.62, 0.52, 0.86, 1)
        _GroundColor ("Below Horizon", Color) = (0.40, 0.50, 0.78, 1)
        _CyanTint ("Cyan Tint (opposite sun)", Color) = (0.36, 0.70, 0.92, 1)
        _CyanTintStrength ("Cyan Tint Strength", Range(0, 1)) = 0.35

        [Header(Sun And Glow)]
        _SunDirection ("Sun Direction", Vector) = (-0.82, 0.30, -0.18, 0)
        _SunColor ("Sun Color", Color) = (1.0, 0.80, 0.66, 1)
        _SunSize ("Sun Size", Range(0.001, 0.1)) = 0.025
        _SunIntensity ("Sun Intensity", Range(0, 8)) = 2.5
        _GlowColor ("Horizon Glow", Color) = (1.0, 0.62, 0.66, 1)
        _GlowStrength ("Horizon Glow Strength", Range(0, 2)) = 0.75

        [Header(Clouds)]
        _CloudColor ("Cloud Lit Color", Color) = (1.0, 0.86, 0.92, 1)
        _CloudShadeColor ("Cloud Shade Color", Color) = (0.48, 0.42, 0.74, 1)
        _CloudCoverage ("Coverage", Range(0, 1)) = 0.48
        _CloudSoftness ("Softness", Range(0.01, 0.5)) = 0.22
        _CloudScale ("Scale", Range(0.1, 5)) = 1.1
        _CloudSpeed ("Drift Speed", Range(0, 0.1)) = 0.008
        _CloudOpacity ("Opacity", Range(0, 1)) = 0.8

        [Header(Stars)]
        _StarDensity ("Star Density", Range(0, 1)) = 0.03
        _StarIntensity ("Star Intensity", Range(0, 4)) = 0.8
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _UpperColor;
                half4 _HorizonColor;
                half4 _GroundColor;
                half4 _CyanTint;
                half _CyanTintStrength;
                float4 _SunDirection;
                half4 _SunColor;
                half _SunSize;
                half _SunIntensity;
                half4 _GlowColor;
                half _GlowStrength;
                half4 _CloudColor;
                half4 _CloudShadeColor;
                half _CloudCoverage;
                half _CloudSoftness;
                half _CloudScale;
                half _CloudSpeed;
                half _CloudOpacity;
                half _StarDensity;
                half _StarIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float sum = 0.0;
                float amplitude = 0.5;
                for (int octave = 0; octave < 4; octave++)
                {
                    sum += ValueNoise(p) * amplitude;
                    p = p * 2.03 + float2(17.1, 9.4);
                    amplitude *= 0.5;
                }
                return sum;
            }

            half3 SkyGradient(float3 dir, float3 sunDir)
            {
                float h = dir.y;
                half3 color = lerp(_HorizonColor.rgb, _UpperColor.rgb, smoothstep(0.0, 0.35, h));
                color = lerp(color, _ZenithColor.rgb, smoothstep(0.3, 1.0, h));

                float2 dirXZ = normalize(dir.xz + 1e-5);
                float2 sunXZ = normalize(sunDir.xz + 1e-5);
                float towardSun = dot(dirXZ, sunXZ) * 0.5 + 0.5;

                float cyanBand = (1.0 - towardSun) * smoothstep(0.45, 0.05, abs(h - 0.12));
                color = lerp(color, _CyanTint.rgb, cyanBand * _CyanTintStrength);

                float glow = exp(-abs(h) * 6.0) * (0.25 + 0.75 * pow(towardSun, 3.0));
                color += _GlowColor.rgb * glow * _GlowStrength;

                half3 below = lerp(_HorizonColor.rgb, _GroundColor.rgb, smoothstep(0.0, -0.25, h));
                return h >= 0.0 ? color : below + _GlowColor.rgb * glow * _GlowStrength * 0.5;
            }

            half4 ApplyClouds(half3 sky, float3 dir, float3 sunDir)
            {
                float h = dir.y;
                float2 uv = dir.xz / (h + 0.18) * _CloudScale;
                float2 drift = float2(_Time.y * _CloudSpeed, _Time.y * _CloudSpeed * 0.35);

                float density = Fbm(uv + drift);
                float detail = Fbm(uv * 2.7 - drift * 1.6);
                density = density * 0.78 + detail * 0.22;

                float mask = smoothstep(_CloudCoverage, _CloudCoverage + _CloudSoftness, density);
                mask *= smoothstep(0.02, 0.18, h) * (1.0 - smoothstep(0.65, 0.95, h));

                float lit = saturate(dot(normalize(dir + float3(0, 0.25, 0)), sunDir) * 0.5 + 0.5);
                float thickness = smoothstep(_CloudCoverage, _CloudCoverage + 0.35, density);
                half3 cloud = lerp(_CloudColor.rgb, _CloudShadeColor.rgb, thickness * 0.7);
                cloud = lerp(_CloudShadeColor.rgb, cloud, 0.45 + 0.55 * lit);
                cloud += _GlowColor.rgb * pow(lit, 6.0) * 0.25;

                return half4(lerp(sky, cloud, mask * _CloudOpacity), mask);
            }

            half Stars(float3 dir, float cloudMask)
            {
                float3 cell = floor(dir * 160.0);
                float rnd = Hash31(cell);
                float visible = step(1.0 - _StarDensity * 0.1, rnd);
                float twinkle = 0.6 + 0.4 * sin(_Time.y * (1.0 + rnd * 3.0) + rnd * 40.0);
                float heightFade = smoothstep(0.35, 0.8, dir.y);
                return visible * twinkle * heightFade * (1.0 - cloudMask) * _StarIntensity;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.direction);
                float3 sunDir = normalize(_SunDirection.xyz);

                half3 color = SkyGradient(dir, sunDir);
                half4 clouded = ApplyClouds(color, dir, sunDir);
                color = clouded.rgb;

                float sunDot = dot(dir, sunDir);
                float disc = smoothstep(1.0 - _SunSize, 1.0 - _SunSize * 0.4, sunDot);
                float halo = pow(saturate(sunDot), 48.0) * 0.35;
                color += _SunColor.rgb * (disc * _SunIntensity + halo) * (1.0 - clouded.a * 0.7);

                color += Stars(dir, clouded.a);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
