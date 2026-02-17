Shader "Hidden/EchoEdgeDetection"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _EdgeThickness ("Edge Thickness", Float) = 1.0
        _EdgeColor ("Edge Color", Color) = (0.1, 0.9, 1, 0.6)
        _PulseRadius ("Pulse Radius", Float) = 0
        _PulseCenter ("Pulse Center", Vector) = (0.5, 0.5, 0, 0)
        _PulseThickness ("Pulse Thickness", Float) = 2.0
        _PulseFalloff ("Pulse Falloff", Float) = 1.0
        _TrailMultiplier ("Trail Length Multiplier", Float) = 10.0
    }
    
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 viewRay : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            sampler2D _CameraDepthNormalsTexture;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float _EdgeThickness;
            float4 _EdgeColor;
            float _PulseRadius;
            float4 _PulseCenter;
            float _PulseThickness;
            float _PulseFalloff;
            float _TrailMultiplier;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                // Create view ray for world position reconstruction
                float4 clipPos = float4(v.uv * 2.0 - 1.0, 1.0, 1.0);
                o.viewRay = mul(unity_CameraInvProjection, clipPos).xyz;
                
                return o;
            }
            
            float4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                
                // Sample depth
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float linearDepth = LinearEyeDepth(depth);
                
                // Sample depth normals for better edge detection
                float3 normal;
                float depthValue;
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.uv), depthValue, normal);
                
                // Sample neighboring pixels for edge detection (Sobel-like)
                float offset = _EdgeThickness * _MainTex_TexelSize.x;
                
                // Horizontal and vertical samples
                float3 n1, n2, n3, n4, n5, n6, n7, n8;
                float d1, d2, d3, d4, d5, d6, d7, d8;
                
                // 3x3 kernel
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.uv + float2(-offset, offset)), d1, n1);
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.uv + float2(0, offset)), d2, n2);
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.uv + float2(offset, offset)), d3, n3);
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.uv + float2(-offset, 0)), d4, n4);
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.uv + float2(offset, 0)), d5, n5);
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.uv + float2(-offset, -offset)), d6, n6);
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.uv + float2(0, -offset)), d7, n7);
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.uv + float2(offset, -offset)), d8, n8);
                
                // Sobel edge detection (normals)
                float3 sobelNormalX = -n1 - 2.0 * n4 - n6 + n3 + 2.0 * n5 + n8;
                float3 sobelNormalY = -n1 - 2.0 * n2 - n3 + n6 + 2.0 * n7 + n8;
                float normalEdge = length(sobelNormalX) + length(sobelNormalY);
                
                // Sobel edge detection (depth)
                float sobelDepthX = -d1 - 2.0 * d4 - d6 + d3 + 2.0 * d5 + d8;
                float sobelDepthY = -d1 - 2.0 * d2 - d3 + d6 + 2.0 * d7 + d8;
                float depthEdge = abs(sobelDepthX) + abs(sobelDepthY);
                
                // Combine edge detections
                float edge = saturate(normalEdge * 8.0 + depthEdge * 150.0);
                
                // Calculate world position for pulse distance
                float3 viewPos = i.viewRay * linearDepth;
                float worldDist = length(viewPos);
                
                // Calculate pulse ring effect with TRAIL
                // _PulseRadius is the current outer edge of the wave
                float distFromFront = _PulseRadius - worldDist;
                float pulseRing = 0;
                
                if (distFromFront >= 0)
                {
                    // Wave has passed this point - Fade out slowly (Trail)
                    float trailLen = _PulseThickness * 20.0; // Much longer trail (was 2.0)
                    pulseRing = 1.0 - saturate(distFromFront / trailLen);
                    
                    // Apply falloff curve
                    pulseRing = pow(pulseRing, _PulseFalloff);
                }
                else
                {
                    // Wave hasn't reached yet (Front edge softness)
                    // Make it fairly sharp or small soft edge
                    pulseRing = 1.0 - saturate(-distFromFront / 0.5);
                }
                
                // Combine edge with pulse
                float finalEdge = edge * pulseRing;
                
                // Add subtle full-screen pulse glow
                float pulseGlow = pulseRing * 0.15 * (1.0 - edge); // Don't glow on edges twice
                
                // Apply edge color with additive blending for glow effect
                // Use _EdgeColor alpha to control overall intensity
                float4 edgeEffect = _EdgeColor * finalEdge * _EdgeColor.a;
                float4 glowEffect = _EdgeColor * pulseGlow * _EdgeColor.a * 0.5; // Softer glow
                
                return col + edgeEffect + glowEffect;
            }
            ENDCG
        }
    }
    
    Fallback Off
}
