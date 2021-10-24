using UnityEngine;

namespace Gamu2059.render_pipeline.Shadowing {
    /// <summary>
    /// レンダーパイプラインアセット
    /// </summary>
    [ExecuteInEditMode]
    [CreateAssetMenu(menuName = "Gamu2059/Shadowing/RenderPipelineAsset", fileName = "render_pipeline_asset.asset")]
    public class RenderPipelineAsset : UnityEngine.Rendering.RenderPipelineAsset {
        /// <summary>
        /// シャドウマップの解像度
        /// </summary>
        [SerializeField]
        private int shadowResolution;

        public int ShadowResolution => shadowResolution;

        /// <summary>
        /// シャドウを投影する最大距離
        /// </summary>
        [SerializeField]
        private float shadowDistance;

        public float ShadowDistance => shadowDistance;

        /// <summary>
        /// レンダーパイプラインを作る
        /// </summary>
        protected override UnityEngine.Rendering.RenderPipeline CreatePipeline() {
            return new RenderPipeline(this);
        }
    }
}