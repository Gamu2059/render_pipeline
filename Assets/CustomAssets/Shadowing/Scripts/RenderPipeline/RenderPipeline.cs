using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gamu2059.render_pipeline.Shadowing {
    /// <summary>
    /// レンダーパイプライン
    /// </summary>
    public class RenderPipeline : UnityEngine.Rendering.RenderPipeline {
        /// <summary>
        /// パイプライン名
        /// </summary>
        private const string PipelineName = "RenderPipeline";

        /// <summary>
        /// レンダーパイプラインアセット
        /// </summary>
        private readonly RenderPipelineAsset Asset;

        /// <summary>
        /// 描画用レンダーテクスチャのハッシュ値
        /// </summary>
        private readonly int RenderTarget;

        /// <summary>
        /// 描画用レンダーテクスチャのID
        /// </summary>
        private readonly RenderTargetIdentifier RenderTargetId;

        /// <summary>
        /// カメラのレンダーターゲットのID
        /// </summary>
        private readonly RenderTargetIdentifier CameraTargetId;

        /// <summary>
        /// 描画に使うパスのID
        /// </summary>
        private readonly ShaderTagId RenderTagId;

        /// <summary>
        /// シャドウマップのハッシュ値
        /// </summary>
        private readonly int LightShadow;

        /// <summary>
        /// シャドウマップのID
        /// </summary>
        private readonly RenderTargetIdentifier LightShadowId;

        /// <summary>
        /// LVP行列のハッシュ値
        /// </summary>
        private readonly int LightVP;

        /// <summary>
        /// ライトの向きのハッシュ値
        /// </summary>
        private readonly int LightDir;

        /// <summary>
        /// ライトの色のハッシュ値
        /// </summary>
        private readonly int LightColor;

        /// <summary>
        /// シャドウバイアスのハッシュ値
        /// </summary>
        private readonly int ShadowBias;

        /// <summary>
        /// シャドウの法線バイアスのハッシュ値
        /// </summary>
        private readonly int ShadowNormalBias;

        /// <summary>
        /// シャドウの最大距離の2乗のハッシュ値
        /// </summary>
        private readonly int ShadowDistanceSqrt;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public RenderPipeline(RenderPipelineAsset asset) {
            Asset = asset;

            RenderTarget = Shader.PropertyToID("_RenderTarget");
            RenderTargetId = new RenderTargetIdentifier(RenderTarget);
            CameraTargetId = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
            RenderTagId = new ShaderTagId("Forward");

            LightShadow = Shader.PropertyToID("_LightShadow");
            LightShadowId = new RenderTargetIdentifier(LightShadow);

            LightVP = Shader.PropertyToID("_LightVP");
            LightDir = Shader.PropertyToID("_LightDir");
            LightColor = Shader.PropertyToID("_LightColor");
            ShadowBias = Shader.PropertyToID("_ShadowBias");
            ShadowNormalBias = Shader.PropertyToID("_ShadowNormalBias");
            ShadowDistanceSqrt = Shader.PropertyToID("_ShadowDistanceSqrt");
        }

        /// <summary>
        /// このレンダーパイプラインを使って描画する
        /// </summary>
        protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
            var shadowResolution = Asset.ShadowResolution;
            var shadowDistance = Asset.ShadowDistance;

            foreach (var camera in cameras) {
                // コマンドバッファの取得
                var cmd = CommandBufferPool.Get(PipelineName);

                // カメラプロパティの設定(View行列、Projection行列の設定など)
                context.SetupCameraProperties(camera);

                // カリング
                if (!TryCulling(context, camera, shadowDistance, out var cullingResults)) {
                    continue;
                }

                // 視界内で有効なディレクショナルライトのインデックスを取得
                var lightIndexes = SearchLightIndexes(cullingResults, LightType.Directional);

                // 有効なライトが存在するかどうか
                var existValidLight = lightIndexes != null && lightIndexes.Count > 0;

                // 有効なライトが存在する時
                if (existValidLight) {
                    // 1つ目のライトを取得
                    var lightIndex = lightIndexes[0];
                    var light = cullingResults.visibleLights[lightIndex].light;

                    // ライトビュープロジェクション行列の計算
                    var lightVP = CalcLightViewProjection(cullingResults, lightIndex);
                    
                    // ライトプロパティの設定
                    SetupLightProperties(context, cmd, light, lightVP, shadowDistance);

                    // シャドウマップ用レンダーテクスチャのセットアップ
                    SetupLightRT(context, cmd, shadowResolution);

                    // シャドウの描画
                    DrawShadow(context, cmd, cullingResults, lightIndex);
                }

                // 描画用レンダーテクスチャのセットアップ
                SetupMainRT(context, cmd, camera);

                // 不透明物体の描画
                DrawOpaque(context, cmd, camera, cullingResults);

                // Skyboxの描画
                DrawSkybox(context, camera);

                // レンダーテクスチャからカメラのフレームバッファへのコピー
                Restore(context, cmd);

                // 描画用レンダーテクスチャのクリーンアップ
                CleanupMainRT(context, cmd);

                // 有効なライトが存在する時
                if (existValidLight) {
                    // シャドウマップ用レンダーテクスチャのクリーンアップ
                    CleanupLightRT(context, cmd);
                }

                // コマンドバッファの解放
                CommandBufferPool.Release(cmd);
            }

            // 今までの全ての処理のリクエストを実行
            context.Submit();
        }

        /// <summary>
        /// 描画用レンダーテクスチャのセットアップ
        /// </summary>
        private void SetupMainRT(ScriptableRenderContext context, CommandBuffer cmd, Camera camera) {
            cmd.Clear();
            cmd.GetTemporaryRT(RenderTarget, Screen.width, Screen.height, 32);
            cmd.SetRenderTarget(RenderTarget);
            cmd.ClearRenderTarget(true, false, camera.backgroundColor, 1);
            context.ExecuteCommandBuffer(cmd);
        }

        /// <summary>
        /// 描画用レンダーテクスチャのクリーンアップ
        /// </summary>
        private void CleanupMainRT(ScriptableRenderContext context, CommandBuffer cmd) {
            cmd.Clear();
            cmd.ReleaseTemporaryRT(RenderTarget);
            context.ExecuteCommandBuffer(cmd);
        }

        /// <summary>
        /// カメラのカリング
        /// </summary>
        /// <param name="shadowDistance">シャドウを投影する最大距離</param>
        /// <param name="cullingResults">CullingResults(取得用)</param>
        private bool TryCulling(ScriptableRenderContext context, Camera camera, float shadowDistance, out CullingResults cullingResults) {
            cullingResults = default;
            if (!camera.TryGetCullingParameters(false, out var cullingParameters)) {
                return false;
            }

            cullingParameters.shadowDistance = Mathf.Clamp(shadowDistance, camera.nearClipPlane, camera.farClipPlane);
            cullingResults = context.Cull(ref cullingParameters);
            return true;
        }

        /// <summary>
        /// レンダーテクスチャからカメラのフレームバッファへのコピー
        /// </summary>
        private void Restore(ScriptableRenderContext context, CommandBuffer cmd) {
            cmd.Clear();
            cmd.Blit(RenderTargetId, CameraTargetId);
            context.ExecuteCommandBuffer(cmd);
        }

        /// <summary>
        /// 不透明物体の描画
        /// </summary>
        private void DrawOpaque(ScriptableRenderContext context, CommandBuffer cmd, Camera camera, CullingResults cullingResults) {
            // 描画用レンダーテクスチャにレンダーターゲットを切り替える
            cmd.Clear();
            cmd.SetRenderTarget(RenderTargetId);
            context.ExecuteCommandBuffer(cmd);

            // 描画順序とフィルタのデータの設定
            var opaqueSortingSettings = new SortingSettings(camera) {criteria = SortingCriteria.CommonOpaque};
            var opaqueDrawSettings = new DrawingSettings(RenderTagId, opaqueSortingSettings);
            var opaqueRenderQueueRange = new RenderQueueRange(0, (int) RenderQueue.GeometryLast);
            var opaqueFilterSettings = new FilteringSettings(opaqueRenderQueueRange, camera.cullingMask);

            // 不透明物体の描画
            context.DrawRenderers(cullingResults, ref opaqueDrawSettings, ref opaqueFilterSettings);
        }

        /// <summary>
        /// Skyboxの描画
        /// </summary>
        private void DrawSkybox(ScriptableRenderContext context, Camera camera) {
            context.DrawSkybox(camera);
        }

        /// <summary>
        /// 指定したタイプのライトのインデックスリストを取得する
        /// </summary>
        /// <param name="lightType">取得したいライトの種類</param>
        private List<int> SearchLightIndexes(CullingResults cullingResults, LightType lightType) {
            var lights = new List<int>();

            // カメラから見える範囲にあるライトの中から指定したタイプのライトを探す
            for (var i = 0; i < cullingResults.visibleLights.Length; i++) {
                var visibleLight = cullingResults.visibleLights[i];

                // 指定したタイプと異なればスキップ
                if (visibleLight.lightType != lightType) {
                    continue;
                }

                var light = visibleLight.light;

                // シャドウが無効ならばスキップ
                if (light == null || light.shadows == LightShadows.None || light.shadowStrength <= 0) {
                    continue;
                }

                // ライトに照らされる範囲にシャドウキャスターが存在しないならばスキップ
                if (!cullingResults.GetShadowCasterBounds(i, out var bounds)) {
                    continue;
                }

                lights.Add(i);
            }

            return lights;
        }

        /// <summary>
        /// ライトビュープロジェクション行列の計算
        /// </summary>
        /// <param name="lightIndex">ライトのインデックス</param>
        private Matrix4x4 CalcLightViewProjection(CullingResults cullingResults, int lightIndex) {
            var light = cullingResults.visibleLights[lightIndex].light;

            // ライトのビュー行列とプロジェクション行列を取得する
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                lightIndex,
                0,
                1,
                Vector3.zero,
                0,
                light.shadowNearPlane,
                out var viewMatrix,
                out var projMatrix,
                out var shadowSplitData);

            // プロジェクション行列を描画ライブラリに適合した状態にする
            projMatrix = GL.GetGPUProjectionMatrix(projMatrix, true);

            // ビュー行列とプロジェクション行列を乗算して返す
            return projMatrix * viewMatrix;
        }

        /// <summary>
        /// ライトプロパティの設定(ViewProjection行列、ライトパラメータの設定など)
        /// </summary>
        /// <param name="light">プロパティを設定するライト</param>
        /// <param name="lightVP">ライトビュープロジェクション行列</param>
        /// <param name="shadowDistance">シャドウを投影する最大距離</param>
        private void SetupLightProperties(ScriptableRenderContext context, CommandBuffer cmd, Light light, Matrix4x4 lightVP, float shadowDistance) {
            cmd.Clear();
            // LVP行列をシェーダーに送信
            cmd.SetGlobalMatrix(LightVP, lightVP);
            // ライトの向きをシェーダーに送信
            cmd.SetGlobalVector(LightDir, -light.transform.forward);
            // ライトの色をシェーダーに送信
            cmd.SetGlobalColor(LightColor, light.color * light.intensity);
            // シャドウバイアスをシェーダーに送信
            cmd.SetGlobalFloat(ShadowBias, light.shadowBias);
            // シャドウ法線バイアスをシェーダーに送信
            cmd.SetGlobalFloat(ShadowNormalBias, light.shadowNormalBias);
            // シャドウを投影する最大距離をシェーダーに送信
            cmd.SetGlobalFloat(ShadowDistanceSqrt, shadowDistance * shadowDistance);
            context.ExecuteCommandBuffer(cmd);
        }

        /// <summary>
        /// シャドウマップ用レンダーテクスチャのセットアップ
        /// </summary>
        /// <param name="shadowResolution">シャドウマップの解像度</param>
        private void SetupLightRT(ScriptableRenderContext context, CommandBuffer cmd, int shadowResolution) {
            cmd.Clear();
            // 色を1チャネルの32bit、深度を32bitでシャドウマップを取得
            cmd.GetTemporaryRT(LightShadow, shadowResolution, shadowResolution, 32, FilterMode.Bilinear,
                RenderTextureFormat.RFloat);
            cmd.SetRenderTarget(LightShadowId);
            cmd.ClearRenderTarget(true, true, Color.white, 1);
            context.ExecuteCommandBuffer(cmd);
        }

        /// <summary>
        /// シャドウマップ用レンダーテクスチャのクリーンアップ
        /// </summary>
        private void CleanupLightRT(ScriptableRenderContext context, CommandBuffer cmd) {
            cmd.Clear();
            cmd.ReleaseTemporaryRT(RenderTarget);
            context.ExecuteCommandBuffer(cmd);
        }

        /// <summary>
        /// シャドウの描画
        /// </summary>
        /// <param name="lightIndex">ライトインデックス</param>
        private void DrawShadow(ScriptableRenderContext context, CommandBuffer cmd, CullingResults cullingResults, int lightIndex) {
            // シャドウマップにレンダーターゲットを切り替える
            cmd.Clear();
            cmd.SetRenderTarget(LightShadowId);
            context.ExecuteCommandBuffer(cmd);

            // シャドウ描画データの設定
            var shadowDrawingSettings = new ShadowDrawingSettings(cullingResults, lightIndex);

            // シャドウの描画
            context.DrawShadows(ref shadowDrawingSettings);

            // シャドウマップをシェーダーに送信
            cmd.Clear();
            cmd.SetGlobalTexture(LightShadow, LightShadowId);
            context.ExecuteCommandBuffer(cmd);
        }
    }
}