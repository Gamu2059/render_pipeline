#ifndef LIGHT
#define LIGHT
#include "UnityCG.cginc"
#endif

/// <summary>
/// ディレクショナルライト
/// </summary>
struct DirectionalLight
{
    // ライトの向き
    float3 lightDir;
    // ライトの色
    half3 lightColor;
};

// LVP行列
float4x4 _LightVP;
// ライトの向き
float3 _LightDir;
// ライトの色
half3 _LightColor;
// シャドウバイアス
float _ShadowBias;
// シャドウ法線バイアス
float _ShadowNormalBias;
// シャドウを投影する最大距離の2乗
float _ShadowDistanceSqrt;
// シャドウマップ
sampler2D _LightShadow;

/// <summary>
/// ワールド空間からLVP空間へと変換する
/// </summary>
float4 TransformWorldToLightViewProjection(float3 positionWS)
{
    return mul(_LightVP, float4(positionWS, 1));
}

/// <summary>
/// オブジェクト空間からLVP空間へと変換する
/// </summary>
float4 TransformObjectToLightViewProjection(float3 positionOS)
{
    return mul(_LightVP, mul(unity_ObjectToWorld, float4(positionOS, 1)));
}

float CalcLightViewProjectionDepth(float4 positionLVP)
{
    float depth = positionLVP.z / positionLVP.w;
    #if UNITY_REVERSED_Z
    return 1 - depth;
    #else
    return depth;
    #endif
}

/// <summary>
/// シャドウによるライトの減衰度を取得する
/// </summary>
float GetShadowAttenuation(float4 positionWS, float dotNL)
{
    // ワールド空間からLVP空間へと変換する
    float4 positionLVP = TransformWorldToLightViewProjection(positionWS);
    // LVP空間からシャドウマップのUV座標へと変換する
    float2 uv = positionLVP.xy / positionLVP.w * float2(0.5f, -0.5f) + 0.5f;

    // ワールド空間からカメラのビュー空間へと変換する
    float3 positionVS = mul(unity_MatrixV, positionWS);
    // カメラのビュー空間の座標を使って、カメラからの距離の2乗を求める
    float distanceSqrt = dot(positionVS, positionVS);

    // ピクセルがある位置のLVP空間上の深度を取得する
    float zInLVP = CalcLightViewProjectionDepth(positionLVP);
    // シャドウマップに書き込まれている位置のLVP空間上の深度を取得する
    float zInShadow = tex2D(_LightShadow, uv).x;
    // シャドウバイアスを求める
    float bias = _ShadowNormalBias * tan(acos(dotNL)) + _ShadowBias;
    // ピクセルがある位置がシャドウマップに書き込まれている位置よりも奥なら影になる
    float attenuation = zInLVP - bias > zInShadow ? 0 : 1;

    // UV座標がシャドウマップ範囲外、またはシャドウ投影範囲外なら、強制的に影にさせない
    return uv.x > 0 && uv.x < 1 && uv.y > 0 && uv.y < 1 && distanceSqrt <= _ShadowDistanceSqrt ? attenuation : 1;
}

/// <summary>
/// ディレクショナルライトを取得する
/// </summary>
DirectionalLight GetDirectionalLight()
{
    DirectionalLight light;
    light.lightDir = _LightDir;
    light.lightColor = _LightColor;
    return light;
}
