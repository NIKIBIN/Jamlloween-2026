using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Circus
{
    /// <summary>
    /// Low internal resolution (Tunables.internalHeight lines) with point upscaling, through URP render scale.
    /// Also pushes the VHS shader globals and the fog density.
    /// </summary>
    public class LowResRenderer : MonoBehaviour
    {
        static readonly int IntensityId = Shader.PropertyToID("_VhsIntensity");
        static readonly int InterferenceId = Shader.PropertyToID("_VhsInterference");
        static readonly int TimeId = Shader.PropertyToID("_VhsTime");

        UniversalRenderPipelineAsset asset;
        float originalScale = 1f;
        UpscalingFilterSelection originalFilter;

        void OnEnable()
        {
            asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (asset) { originalScale = asset.renderScale; originalFilter = asset.upscalingFilter; }
        }

        void OnDisable()
        {
            if (asset) { asset.renderScale = originalScale; asset.upscalingFilter = originalFilter; }
            VHSRenderFeature.Enabled = true;
        }

        void Update()
        {
            var t = Tunables.I;
            if (asset)
            {
                asset.upscalingFilter = UpscalingFilterSelection.Point;
                asset.renderScale = Mathf.Clamp(t.internalHeight / (float)Mathf.Max(1, Screen.height), 0.1f, 1f);
            }
            VHSRenderFeature.Enabled = t.vhsFilter;
            Shader.SetGlobalFloat(IntensityId, t.vhsBaseIntensity);
            Shader.SetGlobalFloat(InterferenceId, VHSGlobals.Total);
            Shader.SetGlobalFloat(TimeId, Time.unscaledTime);
            RenderSettings.fogDensity = t.fogDensity;
        }
    }
}
