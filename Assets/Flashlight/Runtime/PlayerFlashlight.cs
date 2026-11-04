using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jamlloween.Flashlight
{
    /// <summary>
    /// Owns the beam: turns the radial fog on and drives the cone (position, angles, fog and brightness inside it).
    /// The fog and brightness outside the beam belong to WorldLighting (Jamlloween.Lighting).
    /// F toggles the light: only surfaces inside the beam see through the fog,
    /// because most of the scene is unlit and ignores the spot light itself.
    /// The player may start without it (<see cref="startOwned"/> off) and pick it up later with <see cref="Acquire"/>.
    /// While <see cref="WorkMode"/> is on (a minigame is being played) the beam is held on, whatever F says.
    /// </summary>
    public class PlayerFlashlight : MonoBehaviour
    {
        static readonly int EnabledId = Shader.PropertyToID("_RadialFogEnabled");
        static readonly int ConeDensityId = Shader.PropertyToID("_RadialFogConeDensity");
        static readonly int ConeBrightnessId = Shader.PropertyToID("_RadialFogConeBrightness");
        static readonly int CosInnerId = Shader.PropertyToID("_FlashlightCosInner");
        static readonly int PositionId = Shader.PropertyToID("_FlashlightPosition");
        static readonly int DirectionId = Shader.PropertyToID("_FlashlightDirection");

        [SerializeField] FlashlightConfig config;
        [SerializeField] Light beam;
        [Tooltip("Off: the player must find the flashlight and call Acquire() before F does anything.")]
        [SerializeField] bool startOwned = true;

        float blend, workBlend, cosOuter, cosInner;
        bool workMode;

        public bool IsOn { get; private set; }
        public bool IsOwned { get; private set; }
        public bool WorkMode => workMode;
        public float ConeAmount => blend;
        public event Action<bool> Toggled;
        public event Action Acquired;

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
            IsOwned = startOwned;
            cosOuter = Mathf.Cos(config.spotAngle * 0.5f * Mathf.Deg2Rad);
            cosInner = Mathf.Cos(config.innerSpotAngle * 0.5f * Mathf.Deg2Rad);
        }

        void OnEnable()
        {
            Shader.SetGlobalFloat(EnabledId, 1f);
            Shader.SetGlobalFloat(ConeDensityId, config.coneFogDensity);
            Shader.SetGlobalFloat(ConeBrightnessId, config.coneBrightness);
        }

        void OnDisable() => Shader.SetGlobalFloat(EnabledId, 0f);

        void Update()
        {
            // Keyboard.current is legitimately null on a machine with no keyboard.
            Keyboard keys = Keyboard.current;
            if (IsOwned && keys != null && keys.fKey.wasPressedThisFrame) SetOn(!IsOn);

            bool lit = IsOwned && (IsOn || workMode);
            blend = Mathf.MoveTowards(blend, lit ? 1f : 0f, Time.deltaTime / config.transitionSeconds);
            beam.enabled = blend > 0f;

            // Work mode opens the beam wide, so a close-up minigame is lit all over and not just in the middle.
            workBlend = workMode ? 1f : 0f;   // no fade: the beam changes at once when a minigame starts or ends
            float outer = Mathf.Lerp(config.spotAngle, config.workSpotAngle, workBlend);
            float inner = Mathf.Lerp(config.innerSpotAngle, config.workInnerSpotAngle, workBlend);
            beam.intensity = Mathf.Lerp(config.intensity, config.workIntensity, workBlend) * blend;
            Shader.SetGlobalFloat(ConeDensityId, Mathf.Lerp(config.coneFogDensity, config.workConeFogDensity, workBlend));
            Shader.SetGlobalFloat(ConeBrightnessId, Mathf.Lerp(config.coneBrightness, config.workConeBrightness, workBlend));
            beam.spotAngle = outer;
            beam.innerSpotAngle = inner;
            cosOuter = Mathf.Cos(outer * 0.5f * Mathf.Deg2Rad);
            cosInner = Mathf.Cos(inner * 0.5f * Mathf.Deg2Rad);
        }

        void LateUpdate()
        {
            Transform t = beam.transform;
            Vector3 position = t.position, forward = t.forward;
            Shader.SetGlobalVector(PositionId, new Vector4(position.x, position.y, position.z, blend));
            Shader.SetGlobalFloat(CosInnerId, cosInner);
            Shader.SetGlobalVector(DirectionId, new Vector4(forward.x, forward.y, forward.z, cosOuter));
        }

        /// <summary>The player now has the flashlight. It is switched on at once, so the pickup is felt.</summary>
        public void Acquire()
        {
            if (IsOwned) return;
            IsOwned = true;
            Acquired?.Invoke();
            SetOn(true);
        }

        /// <summary>Holds the beam on while the player works on a minigame, then gives it back to the F toggle.</summary>
        public void SetWorkMode(bool on)
        {
            if (on == workMode) return;
            workMode = on;
            blend = IsOwned && (IsOn || workMode) ? 1f : 0f;   // snap, so the beam does not fade in or out around a minigame
        }

        public void SetOn(bool on)
        {
            if (on && !IsOwned) return;
            if (on == IsOn) return;
            IsOn = on;
            Toggled?.Invoke(on);
        }
    }
}
