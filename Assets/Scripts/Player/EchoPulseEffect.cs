using UnityEngine;
using System.Collections;

/// <summary>
/// ECHOES - High-Quality Echo Pulse Effect
/// Creates a visual pulse/wave effect that reveals environment geometry through sound.
/// Uses shader-based rendering for high performance and quality.
/// </summary>
[RequireComponent(typeof(Camera))]
public class EchoPulseEffect : MonoBehaviour
{
    [Header("Pulse Settings")]
    [SerializeField] private Material pulseMaterial;
    [SerializeField] private Color pulseColor = new Color(0.2f, 0.8f, 1f, 0.8f);
    [SerializeField] private float pulseThickness = 0.5f;
    [SerializeField] private AnimationCurve pulseIntensityCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [Header("Environment Reveal")]
    [SerializeField] private float revealDuration = 2f;
    [SerializeField] private LayerMask revealLayers = ~0;
    [SerializeField] private Color wireframeColor = new Color(0.1f, 0.9f, 1f, 0.6f);
    
    private Camera cam;
    private bool isPulseActive = false;
    private float pulseProgress = 0f;
    private float currentRadius = 0f;
    private float currentSpeed = 10f;
    private float currentMaxRadius = 30f;
    private float currentDuration = 3f;
    
    // Edge detection effect
    private Material edgeDetectionMaterial;
    private RenderTexture edgeDetectionRT;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        
        // Create pulse material if not assigned
        if (pulseMaterial == null)
        {
            Shader pulseShader = Shader.Find("Hidden/EchoPulse");
            if (pulseShader != null)
            {
                pulseMaterial = new Material(pulseShader);
            }
            else
            {
                // Fallback to simple unlit shader
                pulseMaterial = new Material(Shader.Find("Unlit/Color"));
                pulseMaterial.color = pulseColor;
            }
        }
        
        // Setup edge detection for wireframe effect
        SetupEdgeDetection();
    }
    
    void SetupEdgeDetection()
    {
        // Enable depth texture for edge detection
        if (cam != null)
        {
            cam.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.DepthNormals;
        }
        
        // Create edge detection material
        Shader edgeShader = Shader.Find("Hidden/EchoEdgeDetection");
        if (edgeShader == null)
        {
            // Create a simple edge detection shader
            CreateEdgeDetectionShader();
            edgeShader = Shader.Find("Hidden/EchoEdgeDetection");
        }
        
        if (edgeShader != null)
        {
            edgeDetectionMaterial = new Material(edgeShader);
        }
    }
    
    void Update()
    {
        if (isPulseActive)
        {
            pulseProgress += Time.deltaTime;
            float normalizedTime = pulseProgress / currentDuration;
            
            // Expand pulse radius
            currentRadius = Mathf.Lerp(0, currentMaxRadius, normalizedTime);
            
            // End pulse when complete
            if (normalizedTime >= 1f)
            {
                isPulseActive = false;
                pulseProgress = 0f;
                currentRadius = 0f;
            }
        }
    }
    
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!isPulseActive || pulseMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }
        
        // Apply edge detection and pulse effect
        RenderTexture temp = RenderTexture.GetTemporary(source.width, source.height, 0);
        
        // Edge detection pass
        if (edgeDetectionMaterial != null)
        {
            float normalizedProgress = pulseProgress / currentDuration;
            float intensity = pulseIntensityCurve.Evaluate(normalizedProgress);
            
            edgeDetectionMaterial.SetFloat("_EdgeThickness", pulseThickness);
            edgeDetectionMaterial.SetColor("_EdgeColor", wireframeColor * intensity);
            edgeDetectionMaterial.SetFloat("_PulseRadius", currentRadius);
            edgeDetectionMaterial.SetVector("_PulseCenter", new Vector4(0.5f, 0.5f, 0, 0));
            edgeDetectionMaterial.SetFloat("_PulseThickness", 2f);
            
            Graphics.Blit(source, temp, edgeDetectionMaterial);
        }
        else
        {
            Graphics.Blit(source, temp);
        }
        
        // Draw expanding pulse ring
        DrawPulseRing(temp, destination);
        
        RenderTexture.ReleaseTemporary(temp);
    }
    
    void DrawPulseRing(RenderTexture source, RenderTexture destination)
    {
        // This will be handled by the edge detection shader
        // For now, just pass through
        Graphics.Blit(source, destination);
    }
    
    /// <summary>
    /// Trigger a pulse effect
    /// </summary>
    public void TriggerPulse(float radius, float speed, float duration, float frequency)
    {
        isPulseActive = true;
        pulseProgress = 0f;
        currentRadius = 0f;
        currentMaxRadius = radius;
        currentSpeed = speed;
        currentDuration = duration;
        
        // Adjust effect based on frequency
        float normalizedFreq = Mathf.InverseLerp(20f, 20000f, frequency);
        pulseColor = Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(0.2f, 0.2f, 1f), normalizedFreq);
        
        Debug.Log($"[EchoPulseEffect] Pulse triggered! Radius: {radius}, Duration: {duration}, Frequency: {frequency}Hz");
    }
    
    void CreateEdgeDetectionShader()
    {
        // Create a simple edge detection shader at runtime
        string shaderCode = @"
        Shader ""Hidden/EchoEdgeDetection""
        {
            Properties
            {
                _MainTex (""Texture"", 2D) = ""white"" {}
                _EdgeThickness (""Edge Thickness"", Float) = 1.0
                _EdgeColor (""Edge Color"", Color) = (0.1, 0.9, 1, 0.6)
                _PulseRadius (""Pulse Radius"", Float) = 0
                _PulseCenter (""Pulse Center"", Vector) = (0.5, 0.5, 0, 0)
                _PulseThickness (""Pulse Thickness"", Float) = 2.0
            }
            
            SubShader
            {
                Cull Off ZWrite Off ZTest Always
                
                Pass
                {
                    CGPROGRAM
                    #pragma vertex vert
                    #pragma fragment frag
                    #include ""UnityCG.cginc""
                    
                    struct appdata
                    {
                        float4 vertex : POSITION;
                        float2 uv : TEXCOORD0;
                    };
                    
                    struct v2f
                    {
                        float2 uv : TEXCOORD0;
                        float4 vertex : SV_POSITION;
                    };
                    
                    sampler2D _MainTex;
                    sampler2D _CameraDepthNormalsTexture;
                    float4 _MainTex_ST;
                    float4 _MainTex_TexelSize;
                    float _EdgeThickness;
                    float4 _EdgeColor;
                    float _PulseRadius;
                    float4 _PulseCenter;
                    float _PulseThickness;
                    
                    v2f vert (appdata v)
                    {
                        v2f o;
                        o.vertex = UnityObjectToClipPos(v.vertex);
                        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                        return o;
                    }
                    
                    float4 frag (v2f i) : SV_Target
                    {
                        float4 col = tex2D(_MainTex, i.uv);
                        
                        // Sample depth normals
                        float3 normal;
                        float depth;
                        DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.uv), depth, normal);
                        
                        // Sample neighboring pixels for edge detection
                        float offset = _EdgeThickness * _MainTex_TexelSize.x;
                        float3 n1, n2, n3, n4;
                        float d1, d2, d3, d4;
                        
                        DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.uv + float2(offset, 0)), d1, n1);
                        DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.uv + float2(-offset, 0)), d2, n2);
                        DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.uv + float2(0, offset)), d3, n3);
                        DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.uv + float2(0, -offset)), d4, n4);
                        
                        // Detect edges based on normal and depth differences
                        float normalDiff = length(n1 - n2) + length(n3 - n4);
                        float depthDiff = abs(d1 - d2) + abs(d3 - d4);
                        
                        float edge = saturate(normalDiff * 10.0 + depthDiff * 100.0);
                        
                        // Calculate distance from pulse center
                        float dist = length(i.uv - _PulseCenter.xy) * 50.0;
                        float pulseDist = abs(dist - _PulseRadius);
                        float pulseRing = 1.0 - saturate(pulseDist / _PulseThickness);
                        
                        // Combine edge detection with pulse effect
                        float finalEdge = max(edge * pulseRing, pulseRing * 0.3);
                        
                        return lerp(col, _EdgeColor, finalEdge);
                    }
                    ENDCG
                }
            }
        }";
        
        // Note: This shader code will be saved to a file in the next step
        Debug.LogWarning("[EchoPulseEffect] Edge detection shader needs to be created manually. See shader code in script.");
    }
    
    void OnDestroy()
    {
        if (edgeDetectionMaterial != null)
        {
            Destroy(edgeDetectionMaterial);
        }
        
        if (pulseMaterial != null)
        {
            Destroy(pulseMaterial);
        }
        
        if (edgeDetectionRT != null)
        {
            edgeDetectionRT.Release();
            Destroy(edgeDetectionRT);
        }
    }
}
