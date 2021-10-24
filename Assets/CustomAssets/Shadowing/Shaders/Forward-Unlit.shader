Shader "Gamu2059/Shadowing/Forward-Unlit"
{
    // Unityのマテリアルのインスペクターで指定するパラメータの定義場所
    Properties
    {
        // テクスチャを指定できるようにする
        _MainTex("Diffuse", 2D) = "white" {}
        // 色を指定できるようにする
        _Color("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            // デフォルトのレンダーキューはGeometry(2000)
            "Queue" = "Geometry"
            // レンダータイプはOpaque(不透明)
            "RenderType" = "Opaque"
        }

        Pass
        {
            Tags
            {
                // 【重要】シェーダーパスのフィルタ名はForward
                "LightMode" = "Forward"
            }

            // ここからHLSL
            HLSLPROGRAM

            // Unityのシェーダーライブラリを使う(C#のusingみたいなもの)
            #include "UnityCG.cginc"

            // 頂点シェーダーにvertという関数を指定
            #pragma vertex vert

            // ピクセルシェーダーfragという関数を指定
            #pragma fragment frag

            // Unityから頂点シェーダーに渡すデータ
            struct Attributes
            {
                // 頂点の座標(オブジェクト空間)
                float3 positionOS : POSITION;
                // 頂点のUV座標
                float2 uv : TEXCOORD0;
            };

            // 頂点シェーダーからピクセルシェーダーに渡すデータ
            struct Varyings
            {
                // クリップ空間の座標
                float4 positionCS : SV_POSITION;
                // UV座標
                float2 uv : TEXCOORD0;
            };

            // プロパティに定義してあるテクスチャを使う
            sampler2D _MainTex;
            // プロパティに定義してあるテクスチャのスケールとオフセットを定義したデータ
            float4 _MainTex_ST;
            // プロパティに定義してある色を使う
            half4 _Color;

            // 頂点シェーダー
            Varyings vert(Attributes i)
            {
                Varyings o;
                // 頂点座標をオブジェクト空間からクリップ空間に変換
                o.positionCS = UnityObjectToClipPos(i.positionOS);
                // スケールとオフセットを考慮してUV座標を再計算
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                return o;
            }

            // ピクセルシェーダー
            half4 frag(Varyings i) : SV_Target
            {
                // UV座標を使ってテクスチャの色を取得
                half4 color = tex2D(_MainTex, i.uv);
                // テクスチャの色に指定した色を乗算
                color.xyz *= _Color.xyz;
                return half4(color.xyz, 1);
            }
            
            // ここまでHLSL
            ENDHLSL
        }
    }
}