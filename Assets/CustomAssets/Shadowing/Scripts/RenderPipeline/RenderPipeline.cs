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
        /// コンストラクタ
        /// </summary>
        public RenderPipeline() {
            RenderTarget = Shader.PropertyToID("_RenderTarget");
            RenderTargetId = new RenderTargetIdentifier(RenderTarget);
            CameraTargetId = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
            RenderTagId = new ShaderTagId("Forward");
        }

        /// <summary>
        /// このレンダーパイプラインを使って描画する
        /// </summary>
        protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
            foreach (var camera in cameras) {
                // コマンドバッファの取得
                var cmd = CommandBufferPool.Get(PipelineName);

                // カメラプロパティの設定(View行列、Projection行列の設定など)
                context.SetupCameraProperties(camera);

                // カリングパラメータの取得
                if (!camera.TryGetCullingParameters(false, out var cullingParameters)) {
                    continue;
                }

                // カメラのカリング
                var cullingResults = context.Cull(ref cullingParameters);

                // レンダーテクスチャの取得リクエスト
                cmd.GetTemporaryRT(RenderTarget, Screen.width, Screen.height, 32);
                // 描画ライブラリで操作するレンダーテクスチャの切り替えリクエスト
                cmd.SetRenderTarget(RenderTarget);
                // レンダーテクスチャの色と深度のクリアリクエスト
                cmd.ClearRenderTarget(true, true, camera.backgroundColor, 1);
                // レンダーテクスチャの取得とクリアの実行
                context.ExecuteCommandBuffer(cmd);

                // 不透明描画のソートの仕方の指定
                var opaqueSortingSettings = new SortingSettings(camera) {criteria = SortingCriteria.CommonOpaque};
                // 不透明描画の描画対象のパスとソートの仕方の指定
                var opaqueDrawSettings = new DrawingSettings(RenderTagId, opaqueSortingSettings);
                // 不透明描画の描画するRenderQueueの範囲の指定
                var opaqueRenderQueueRange = new RenderQueueRange(0, (int) RenderQueue.GeometryLast);
                // 不透明描画の描画するRenderQueueの範囲と描画対象のレイヤーの指定
                var opaqueFilterSettings = new FilteringSettings(opaqueRenderQueueRange, camera.cullingMask);
                // 不透明描画の実行
                context.DrawRenderers(cullingResults, ref opaqueDrawSettings, ref opaqueFilterSettings);

                // Skyboxの描画
                context.DrawSkybox(camera);

                // 以前のリクエストのクリア
                cmd.Clear();
                // レンダーテクスチャからカメラのフレームバッファへのコピーリクエスト
                cmd.Blit(RenderTargetId, CameraTargetId);
                // レンダーテクスチャの解放リクエスト
                cmd.ReleaseTemporaryRT(RenderTarget);
                // レンダーテクスチャのコピーと解放の実行
                context.ExecuteCommandBuffer(cmd);

                // コマンドバッファの解放
                CommandBufferPool.Release(cmd);
            }

            // 今までの全ての処理のリクエストを実行
            context.Submit();
        }
    }
}