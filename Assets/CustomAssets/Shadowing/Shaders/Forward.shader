Shader "Gamu2059/Shadowing/Forward-Unlit"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Pass
        {
            Tags
            {
                "LightMode" = "Forward"
                "RenderType" = "Opaque"
                "Queue" = "Geometry"
            }
            
            Blend One Zero

            HLSLPROGRAM
            #include "UnityCG.cginc"
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _Color;

            Varyings vert(Attributes attributes)
            {
                Varyings o;
                o.positionCS = UnityObjectToClipPos(attributes.positionOS);
                o.uv = TRANSFORM_TEX(attributes.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 color = tex2D(_MainTex, i.uv);
                color.xyz *= _Color.xyz;
                return half4(color.xyz, 1);
            }
            ENDHLSL
        }
    }
}