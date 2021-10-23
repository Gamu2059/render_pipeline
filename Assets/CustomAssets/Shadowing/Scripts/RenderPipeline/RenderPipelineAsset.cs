using UnityEngine;

namespace Gamu2059.render_pipeline.Shadowing {
    /// <summary>
    /// レンダーパイプラインアセット
    /// </summary>
    [ExecuteInEditMode]
    [CreateAssetMenu(menuName = "Gamu2059/Shadowing/RenderPipelineAsset", fileName = "render_pipeline_asset.asset")]
    public class RenderPipelineAsset : UnityEngine.Rendering.RenderPipelineAsset {
        /// <summary>
        /// レンダーパイプラインを作る
        /// </summary>
        protected override UnityEngine.Rendering.RenderPipeline CreatePipeline() {
            return new RenderPipeline();
        }
    }
}