using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Circus
{
    /// <summary>Full-screen VHS pass (Render Graph blit) on the main game camera, after post-processing.</summary>
    public class VHSRenderFeature : ScriptableRendererFeature
    {
        public static bool Enabled = true;

        [SerializeField] Shader shader;
        [SerializeField] RenderPassEvent passEvent = RenderPassEvent.AfterRenderingPostProcessing;

        Material material;
        VHSPass pass;

        public override void Create()
        {
            if (shader == null) shader = Shader.Find("Hidden/Circus/VHS");
            if (shader != null) material = CoreUtils.CreateEngineMaterial(shader);
            pass = new VHSPass { renderPassEvent = passEvent };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!Enabled || material == null) return;
            var camData = renderingData.cameraData;
            if (camData.cameraType != CameraType.Game || !camData.camera.CompareTag("MainCamera")) return;
            pass.material = material;
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing) => CoreUtils.Destroy(material);

        class VHSPass : ScriptableRenderPass
        {
            public Material material;
            static readonly int TargetSize = Shader.PropertyToID("_VhsTargetSize");

            public VHSPass() { requiresIntermediateTexture = true; }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var res = frameData.Get<UniversalResourceData>();
                if (res.isActiveTargetBackBuffer) return;
                var src = res.activeColorTexture;
                var desc = renderGraph.GetTextureDesc(src);
                desc.name = "_VHSColor";
                desc.clearBuffer = false;
                var dst = renderGraph.CreateTexture(desc);
                material.SetVector(TargetSize, new Vector4(desc.width, desc.height, 0, 0));
                renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(src, dst, material, 0), "VHS");
                res.cameraColor = dst;
            }
        }
    }
}
