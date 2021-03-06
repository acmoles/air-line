Shader "Custom/VertexColored2"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Diffuse ("Diffuse", Range(0,1)) = 0.5
        _Glossiness ("Glossiness", Range(0,1)) = 0.5
        [HDR]
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularAmount ("SpecularAmount", Range(0,1000)) = 100
        [HDR]
        _RimColor ("Rim Color", Color) = (0.26,0.19,0.16,0.0)
        _RimPower ("Rim Power", Range(0.5,8.0)) = 3.0
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.1
        _NoiseAmount ("NoiseAmount", Range(0,1)) = 0.0
        _NoiseScale ("NoiseScale", float) = 1000.0
        _DarknessAmount ("DarknessAmount", Range(0,1)) = 0.5
        _SpecularOverdrive ("SpecularOverdrive", Range(0,5)) = 1.5
        _RimOverdrive ("RimOverdrive", Range(0,5)) = 2.0
        _RimOverlayOverdrive ("RimOverlayOverdrive", Range(0,5)) = 1.0

        _FadeBeginDistance("Fade Begin Distance", Range(0.0, 10.0)) = 0.85
        _FadeCompleteDistance("Fade Complete Distance", Range(0.0, 10.0)) = 0.5
        _FadeMinValue("Fade Min Value", Range(0.0, 1.0)) = 0.0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "LightMode" = "ForwardBase"
            "PassFlags" = "OnlyDirectional"
        }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            #include "SimplexNoise2D.hlsl"
            #include "BlendModes.hlsl"

            float noise(float2 co)
            {
                float a = 12.9898;
                float b = 78.233;
                float c = 43758.5453;
                float dt= dot(co.xy ,float2(a,b));
                float sn= fmod(dt,3.14);
                return frac(sin(sn) * c);
            }

            float2 intToSeedVec(uint seed) {
                uint a = 1;
                uint b = 256;
                uint c = 255;

                float2 iSeedVec = float2(  ((seed / a) & c), ((seed / b) & c) );
                return trunc(iSeedVec * (1.0 / 9.0)) * 10.0 + frac(iSeedVec * (1.0 / 9.0)) * 9.0 + 1.0; // Skip multiples of 9
            }

            struct appdata
            {
                float4 vertexColor: COLOR; // Vertex color
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertexColor: COLOR; // Vertex color
                float4 vertex : SV_POSITION;
                float3 worldNormal : NORMAL;
                float4 screenPosition : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                SHADOW_COORDS(2)
            };

            uniform fixed4 _Color;
            uniform float _Diffuse;
            uniform float _Glossiness;
            uniform fixed4 _SpecularColor;
            uniform float _SpecularAmount;
            uniform float _RimPower;
            uniform float _RimThreshold;
            uniform float _NoiseAmount;
            uniform float _NoiseScale;
            uniform float _DarknessAmount;
            uniform float _SpecularOverdrive;
            uniform float _RimOverdrive;
            uniform float _RimOverlayOverdrive;

            uniform float _FadeBeginDistance;
            uniform float _FadeCompleteDistance;
            uniform fixed _FadeMinValue;


            // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
            // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
            #pragma instancing_options assumeuniformscaling
            UNITY_INSTANCING_BUFFER_START(Props)
                // put more per-instance properties here
                UNITY_DEFINE_INSTANCED_PROP(float4, _RimColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPosition = mul(unity_ObjectToWorld, v.vertex);
                o.screenPosition = ComputeScreenPos(o.vertex);
                o.vertexColor = v.vertexColor;
                TRANSFER_SHADOW(o)

                float rangeInverse = 1.0 / (_FadeBeginDistance - _FadeCompleteDistance);
                float fadeDistance = -UnityObjectToViewPos(v.vertex).z;
                o.worldPosition.w = max(saturate(mad(fadeDistance, rangeInverse, -_FadeCompleteDistance * rangeInverse)), _FadeMinValue);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Albedo comes from a vertex color
                fixed4 col = i.vertexColor;

                // Lighting
                float3 normal = normalize(i.worldNormal);
                float3 viewDirection = normalize(_WorldSpaceCameraPos - i.worldPosition);

                float NdotL = saturate(dot(_WorldSpaceLightPos0, normal));
                float shadow = SHADOW_ATTENUATION(i);
                float lightIntensity = NdotL * shadow;
                float4 light = lightIntensity * _LightColor0;

                // Specular
                float3 halfVector = normalize(_WorldSpaceLightPos0.xyz + viewDirection);
                float NdotH = saturate(dot(halfVector, normal));
                float specularIntensity = pow(NdotH * lightIntensity, _Glossiness * _SpecularAmount);
                float4 specular = specularIntensity * _SpecularColor;

                // Rim lighting light
                float rimDot = 1.0 - saturate(dot (viewDirection, normal));
                float rimIntensity = rimDot * pow(NdotL, _RimThreshold);
                float3 rim = UNITY_ACCESS_INSTANCED_PROP(Props, _RimColor).rgb * pow(rimIntensity, _RimPower);

                float rimInvert = rimDot * pow(1 - NdotL, 1 - _RimThreshold);
                float3 rimOverlay = UNITY_ACCESS_INSTANCED_PROP(Props, _RimColor).rgb * pow(rimInvert, _RimPower * _RimOverlayOverdrive);

                // Noise
                float2 screenUV = i.screenPosition.xy / i.screenPosition.w;
                screenUV *= float2(_NoiseScale, _NoiseScale);
                screenUV *= 0.75 + (frac(_Time.y)/2.0);
                float n = _NoiseAmount * SimplexNoise( screenUV );
                float3 noise = float3(n, n, n);

                // Blends
                col.rgb = col.rgb - _DarknessAmount*(1 - min(col.rgb, light));
                //col.rgb = blendOverlay(col.rgb, light);
                col.rgb = col.rgb + (noise - .5 * noise);
                col.rgb = blendColorDodge(col.rgb, rimOverlay);
                col.rgb = blendScreen(col.rgb, (_SpecularOverdrive*specular + _RimOverdrive*rim + light*_DarknessAmount*.5).rgb);
                
                col.a = i.worldPosition.w;

                return col;
                //return float4(rimOverlay, 1);
            }
            ENDCG
        }
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
    FallBack "Diffuse"
}
