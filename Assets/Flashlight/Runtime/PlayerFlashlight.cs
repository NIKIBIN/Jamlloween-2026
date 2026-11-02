using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jamlloween.Flashlight
{
    /// <summary>
    /// Owns the radial fog globals. F toggles the light: only surfaces inside the beam see through the fog,
    /// because most of the scene is unlit and ignores the spot light itself.
    /// </summary>
    public class PlayerFlashlight : MonoBehaviour
    {
        static readonly int EnabledId = Shader.PropertyToID("_RadialFogEnabled");
        static readonly int BaseDensityId = Shader.PropertyToID("_RadialFogBaseDensity");
        static readonly int ConeDensityId = Shader.PropertyToID("_RadialFogConeDensity");
        static readonly int BaseBrightnessId = Shader.PropertyToID("_RadialFogBaseBrightness");
        static readonly int ConeBrightnessId = Shader.PropertyToID("_RadialFogConeBrightness");
        static readonly int CosInnerId = Shader.PropertyToID("_FlashlightCosInner");
        static readonly int PositionId = Shader.PropertyToID("_FlashlightPosition");
        static readonly int DirectionId = Shader.PropertyToID("_FlashlightDirection");

        [SerializeField] FlashlightConfig config;
        [SerializeField] Light beam;

        float blend, cosOuter;

        public bool IsOn { get; private set; }
        public float ConeAmount => blend;
        public event Action<bool> Toggled;

        void Awake()
        {
            if (config == null) throw new MissingReferenceException(nameof(config));
            if (beam == null) throw new MissingReferenceException(nameof(beam));
            if (beam.type != LightType.Spot) throw new InvalidOperationException("The beam must be a spot light.");

            beam.spotAngle = config.spotAngle;
            beam.innerSpotAngle = config.innerSpotAngle;
            beam.range = config.ConeDistance;
            beam.intensity = 0f;
            beam.enabled = false;
            cosOuter = Mathf.Cos(config.spotAngle * 0.5f * Mathf.Deg2Rad);
        }

        void OnEnable()
        {
            Shader.SetGlobalFloat(EnabledId, 1f);
            Shader.SetGlobalFloat(BaseDensityId, config.baseFogDensity);
            Shader.SetGlobalFloat(ConeDensityId, config.coneFogDensity);
            Shader.SetGlobalFloat(BaseBrightnessId, config.baseBrightness);
            Shader.SetGlobalFloat(ConeBrightnessId, config.coneBrightness);
            Shader.SetGlobalFloat(CosInnerId, Mathf.Cos(config.innerSpotAngle * 0.5f * Mathf.Deg2Rad));
        }

        void OnDisable() => Shader.SetGlobalFloat(EnabledId, 0f);

        void Update()
        {
            // Keyboard.current is legitimately null on a machine with no keyboard.
            Keyboard keys = Keyboard.current;
            if (keys != null && keys.fKey.wasPressedThisFrame) SetOn(!IsOn);

            blend = Mathf.MoveTowards(blend, IsOn ? 1f : 0f, Time.deltaTime / config.transitionSeconds);
            beam.enabled = blend > 0f;
            beam.intensity = config.intensity * blend;
        }

        void LateUpdate()
        {
            Transform t = beam.transform;
            Vector3 position = t.position, forward = t.forward;
            Shader.SetGlobalVector(PositionId, new Vector4(position.x, position.y, position.z, blend));
            Shader.SetGlobalVector(DirectionId, new Vector4(forward.x, forward.y, forward.z, cosOuter));
        }

        public void SetOn(bool on)
        {
            if (on == IsOn) return;
            IsOn = on;
            Toggled?.Invoke(on);
        }
    }
}
