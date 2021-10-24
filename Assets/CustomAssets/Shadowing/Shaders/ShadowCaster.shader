// シェーダー名
Shader "Hidden/Gamu2059/Shadowing/ShadowCaster"
{
    SubShader
    {
        // シャドウキャスター
        Pass
        {
            // パス名
            Name "SHADOW_CASTER"
            Tags
            {
                // 【重要】レンダーパイプライン側でDrawShadowsを使ってシャドウ描画する時は、必ずShadowCasterにする
                "LightMode" = "ShadowCaster"
            }

            HLSLPROGRAM
            #include "Light.cginc"
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionLVP : SV_POSITION;
            };

            // シャドウキャスターの頂点シェーダー
            Varyings vert(Attributes i)
            {
                Varyings o;
                // オブジェクト空間からLVP空間へと変換する
                o.positionLVP = TransformObjectToLightViewProjection(i.positionOS);
                return o;
            }

            // シャドウキャスターのピクセルシェーダー
            float frag(Varyings i) : SV_Target
            {
                // ピクセルがある位置のLVP空間上の深度をシャドウマップに書き込む
                return i.positionLVP.z / i.positionLVP.w;
            }
            ENDHLSL
        }
    }
}