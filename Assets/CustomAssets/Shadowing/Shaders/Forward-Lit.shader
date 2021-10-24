Shader "Gamu2059/Shadowing/Forward-Lit"
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
            #include "Light.cginc"

            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                // 頂点の座標(オブジェクト空間)
                float4 positionOS : POSITION;
                // 頂点のUV座標
                float2 uv : TEXCOORD0;
                // 頂点の法線(オブジェクト空間)
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                // クリップ空間の座標
                float4 positionCS : SV_POSITION;
                // ワールド空間の座標
                float4 positionWS : TEXCOORD0;
                // UV座標
                float2 uv : TEXCOORD1;
                // ワールド空間の法線
                float3 normalWS : NORMAL;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _Color;

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = UnityObjectToClipPos(i.positionOS);
                // 頂点座標をオブジェクト空間からワールド空間に変換
                o.positionWS = mul(unity_ObjectToWorld, i.positionOS);
                // 法線をオブジェクト空間からワールド空間に変換
                o.normalWS = UnityObjectToWorldNormal(i.normalOS);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // ディレクショナルライトの取得
                DirectionalLight light = GetDirectionalLight();
                // 法線の向きとライトの向きの内積を求める
                float dotNL = saturate(dot(normalize(i.normalWS), light.lightDir));

                half3 color = tex2D(_MainTex, i.uv).xyz;
                color *= _Color.xyz;

                // Lambert拡散反射光を求める
                half3 diffuse = light.lightColor * dotNL;
                // ライトの減衰度を求める
                float shadowAttenuation = GetShadowAttenuation(i.positionWS, dotNL);

                // 影による減衰を考慮した拡散反射を色に反映させる
                color *= diffuse * shadowAttenuation;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}