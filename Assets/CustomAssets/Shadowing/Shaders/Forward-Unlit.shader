Shader "Gamu2059/Shadowing/Forward-Unlit"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
        }

        // シャドウキャスターパスを使う
        UsePass "Hidden/Gamu2059/Shadowing/ShadowCaster/SHADOW_CASTER"

        Pass
        {
            Tags
            {
                "LightMode" = "Forward"
            }

            HLSLPROGRAM
            #include "UnityCG.cginc"

            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _Color;

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = UnityObjectToClipPos(i.positionOS);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
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