Shader "Custom/GhostShader"
{
    Properties
    {
        _MainTex ("Ghost Texture", 2D) = "white" {}
        _Color ("Ghost Color", Color) = (0.8, 0.9, 1.0, 0.5)
        _GlowColor ("Glow Color", Color) = (0.5, 0.7, 1.0, 1.0)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 2.0
        _Transparency ("Transparency", Range(0, 1)) = 0.5
        _FlickerSpeed ("Flicker Speed", Range(0, 10)) = 3.0
        _FlickerAmount ("Flicker Amount", Range(0, 1)) = 0.3
        _DistortionMap ("Distortion Map", 2D) = "bump" {}
        _DistortionStrength ("Distortion Strength", Range(0, 0.5)) = 0.1
        _DistortionSpeed ("Distortion Speed", Range(0, 2)) = 0.5
        _FresnelPower ("Fresnel Power", Range(0, 10)) = 3.0
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 200
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_DistortionMap);
            SAMPLER(sampler_DistortionMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _GlowColor;
                float _GlowIntensity;
                float _Transparency;
                float _FlickerSpeed;
                float _FlickerAmount;
                float _DistortionStrength;
                float _DistortionSpeed;
                float _FresnelPower;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Time-based animation
                float time = _Time.y;
                
                // Flickering effect
                float flicker = sin(time * _FlickerSpeed) * 0.5 + 0.5;
                flicker = lerp(1.0 - _FlickerAmount, 1.0, flicker);
                
                // Distortion animation
                float2 distortUV = input.uv + float2(time * _DistortionSpeed * 0.1, time * _DistortionSpeed * 0.15);
                half2 distortion = SAMPLE_TEXTURE2D(_DistortionMap, sampler_DistortionMap, distortUV).rg;
                distortion = (distortion - 0.5) * _DistortionStrength;
                
                // Apply distortion to main texture UV
                float2 finalUV = input.uv + distortion;
                
                // Sample main texture
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, finalUV);
                
                // Fresnel effect (edges glow more)
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                fresnel = pow(fresnel, _FresnelPower);
                
                // Combine colors
                half3 baseColor = texColor.rgb * _Color.rgb;
                half3 glowColor = _GlowColor.rgb * _GlowIntensity;
                
                // Apply fresnel glow
                half3 finalColor = baseColor + glowColor * fresnel;
                
                // Apply flicker
                finalColor *= flicker;
                
                // Pulsing transparency
                float pulse = sin(time * 2.0) * 0.1 + 0.9;
                float alpha = _Transparency * _Color.a * texColor.a * pulse * flicker;
                
                // Add extra glow at edges
                alpha = saturate(alpha + fresnel * 0.3);
                
                half4 color = half4(finalColor, alpha);
                
                // Apply fog (with special transparent fog)
                color.rgb = MixFog(color.rgb, input.fogFactor);
                
                return color;
            }
            ENDHLSL
        }
    }
    
    FallBack Off
}
