Shader "Custom/HorrorFloorShader"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (0.4, 0.4, 0.4, 1)
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _HeightMap ("Height Map", 2D) = "gray" {}
        _Parallax ("Parallax Scale", Range(0.005, 0.08)) = 0.02
        _GrimeTex ("Grime Overlay", 2D) = "white" {}
        _GrimeStrength ("Grime Strength", Range(0, 1)) = 0.5
        _PuddleMask ("Puddle Mask", 2D) = "black" {}
        _PuddleReflection ("Puddle Reflection", Range(0, 1)) = 0.8
        _Darkness ("Darkness Multiplier", Range(0, 1)) = 0.25
        _HeightDarkness ("Height-Based Darkness", Range(0, 1)) = 0.3
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 200
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float3 viewDirTS : TEXCOORD5;
                float fogFactor : TEXCOORD6;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_HeightMap);
            SAMPLER(sampler_HeightMap);
            TEXTURE2D(_GrimeTex);
            SAMPLER(sampler_GrimeTex);
            TEXTURE2D(_PuddleMask);
            SAMPLER(sampler_PuddleMask);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Parallax;
                float _GrimeStrength;
                float _PuddleReflection;
                float _Darkness;
                float _HeightDarkness;
            CBUFFER_END
            
            // Parallax Occlusion Mapping function
            float2 ParallaxMapping(float2 uv, float3 viewDirTS)
            {
                const float minLayers = 8;
                const float maxLayers = 32;
                float numLayers = lerp(maxLayers, minLayers, abs(dot(float3(0, 0, 1), viewDirTS)));
                
                float layerDepth = 1.0 / numLayers;
                float currentLayerDepth = 0.0;
                float2 deltaUV = viewDirTS.xy * _Parallax / numLayers;
                
                float2 currentUV = uv;
                float currentDepthMapValue = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, currentUV).r;
                
                for(int i = 0; i < 32; i++)
                {
                    if(currentLayerDepth >= currentDepthMapValue)
                        break;
                    currentUV -= deltaUV;
                    currentDepthMapValue = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, currentUV).r;
                    currentLayerDepth += layerDepth;
                }
                
                // Parallax occlusion mapping interpolation
                float2 prevUV = currentUV + deltaUV;
                float afterDepth = currentDepthMapValue - currentLayerDepth;
                float beforeDepth = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, prevUV).r - currentLayerDepth + layerDepth;
                float weight = afterDepth / (afterDepth - beforeDepth);
                return lerp(currentUV, prevUV, weight);
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = normalInput.normalWS;
                output.tangentWS = normalInput.tangentWS;
                output.bitangentWS = normalInput.bitangentWS;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                // Calculate view direction in tangent space
                float3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                float3x3 TBN = float3x3(normalInput.tangentWS, normalInput.bitangentWS, normalInput.normalWS);
                output.viewDirTS = mul(TBN, viewDirWS);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Parallax mapping
                float2 parallaxUV = ParallaxMapping(input.uv, normalize(input.viewDirTS));
                
                // Sample textures with parallax UVs
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, parallaxUV) * _Color;
                half4 grimeColor = SAMPLE_TEXTURE2D(_GrimeTex, sampler_GrimeTex, parallaxUV * 2);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, parallaxUV));
                half puddleMask = SAMPLE_TEXTURE2D(_PuddleMask, sampler_PuddleMask, input.uv).r;
                half heightValue = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, parallaxUV).r;
                
                // Apply grime overlay
                baseColor.rgb = lerp(baseColor.rgb, baseColor.rgb * grimeColor.rgb, _GrimeStrength);
                
                // Height-based darkness (lower areas are darker)
                half heightDarkness = lerp(1.0 - _HeightDarkness, 1.0, heightValue);
                baseColor.rgb *= heightDarkness;
                
                // Apply overall darkness
                baseColor.rgb *= _Darkness;
                
                // Create TBN matrix
                float3x3 TBN = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                float3 normalWS = normalize(mul(normalTS, TBN));
                
                // Lighting
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.normalWS = normalWS;
                lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                
                // Puddle increases smoothness
                half smoothness = lerp(0.1, _PuddleReflection, puddleMask);
                
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = baseColor.rgb;
                surfaceData.alpha = 1.0;
                surfaceData.metallic = 0.0;
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = normalTS;
                
                half4 color = UniversalFragmentPBR(lightingInput, surfaceData);
                
                // Apply fog
                color.rgb = MixFog(color.rgb, input.fogFactor);
                
                return color;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _MainLightPosition.xyz));
                return output;
            }
            
            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
