Shader "Mirza Beig/Distortion Shockwave Particle"
{
    Properties
    {
        _Colour("Colour", Color) = (1,1,1,1)
        _Distortion("Distortion", Range( 0 , 1)) = 0.5
        _WaveSmoothness("Wave Smoothness", Range( 0.01 , 2)) = 1
        _InnerRadialDistortionMaskRadius("Inner Radial Distortion Mask Radius", Range( 0 , 1)) = 0.5
        _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        
        GrabPass { "_GrabTexture" }
        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };
            
            sampler2D _GrabTexture;
            float4 _Colour;
            float _Distortion;
            float _WaveSmoothness;
            float _InnerRadialDistortionMaskRadius;
            sampler2D _MainTex;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                o.screenPos = ComputeGrabScreenPos(o.vertex);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // Distance from center of particle quad
                float2 center = i.uv - 0.5;
                float dist = length(center);
                
                // Create a smooth ring/wave based on distance
                float innerRadius = _InnerRadialDistortionMaskRadius * 0.35;
                float outerRadius = 0.5;
                
                // Ring mask using smoothstep
                float ring = smoothstep(outerRadius, outerRadius - 0.05 * _WaveSmoothness, dist) 
                           * smoothstep(innerRadius - 0.05 * _WaveSmoothness, innerRadius, dist);
                
                // Vector pointing outward from center
                float2 dir = dist > 0.0001 ? normalize(center) : float2(0, 0);
                
                // Distort Grab UVs along the outward vector
                float2 offset = dir * ring * _Distortion * 0.5 * i.color.a;
                float4 uvGrab = i.screenPos;
                uvGrab.xy += offset * uvGrab.w;
                
                // Sample the grabbed screen texture
                fixed4 grabColor = tex2Dproj(_GrabTexture, uvGrab);
                
                // Add a subtle color tint representing the shockwave color
                fixed4 col = grabColor + ring * _Colour * i.color * 0.5;
                col.a = i.color.a;
                return col;
            }
            ENDCG
        }
    }
}