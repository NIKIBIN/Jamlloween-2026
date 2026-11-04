using UnityEngine;
using UnityEngine.Rendering;

namespace Jamlloween.Lighting
{
    /// <summary>
    /// The one owner of the world's light: fog, brightness of the unlit scenery, ambient light and camera background.
    /// Put one in each scene (prefab <c>Assets/Lighting/WorldLighting.prefab</c>) and pick the starting profile.
    /// Other systems change the look with <see cref="Set"/>; they never write these globals themselves.
    /// The flashlight only owns its beam: position, cone and the fog inside the cone.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-100)]
    public class WorldLighting : MonoBehaviour
    {
        static readonly int BaseDensityId = Shader.PropertyToID("_RadialFogBaseDensity");
        static readonly int BaseBrightnessId = Shader.PropertyToID("_RadialFogBaseBrightness");

        static WorldLighting current;
        static LightingProfile pending;
        static float pendingSeconds;

        [Tooltip("Profile at the start of the scene, and the one shown in the Scene View.")]
        [SerializeField] LightingProfile initialProfile;
        [Tooltip("Cameras that clear to the fog colour. Empty: Camera.main.")]
        [SerializeField] Camera[] cameras;

        struct State
        {
            public Color fogColor, ambientColor;
            public float fogDensity, brightness;

            public static State From(LightingProfile p) => new State
            {
                fogColor = p.fogColor, ambientColor = p.ambientColor, fogDensity = p.fogDensity, brightness = p.brightness,
            };

            public static State Lerp(State a, State b, float t) => new State
            {
                fogColor = Color.Lerp(a.fogColor, b.fogColor, t),
                ambientColor = Color.Lerp(a.ambientColor, b.ambientColor, t),
                fogDensity = Mathf.Lerp(a.fogDensity, b.fogDensity, t),
                brightness = Mathf.Lerp(a.brightness, b.brightness, t),
            };
        }

        LightingProfile target;
        State from, shown;
        float fade = 1f, fadeSeconds;

        /// <summary>The profile the world shows, or fades to.</summary>
        public static LightingProfile Current => current ? current.target : pending;

        /// <summary>Fades the world to <paramref name="profile"/>. Safe to call before the WorldLighting wakes up.</summary>
        public static void Set(LightingProfile profile, float seconds = 0f)
        {
            if (profile == null) return;
            if (current) current.FadeTo(profile, seconds);
            else { pending = profile; pendingSeconds = seconds; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { current = null; pending = null; }

        void OnEnable()
        {
            if (Application.isPlaying)
            {
                if (current && current != this) Debug.LogWarning("[WorldLighting] mais de um na cena; o ultimo ativo manda.", this);
                current = this;
            }
            LightingProfile.Changed += OnProfileChanged;

            LightingProfile start = Application.isPlaying && pending ? pending : initialProfile;
            pending = null;
            if (start == null) { Debug.LogWarning("[WorldLighting] sem profile inicial.", this); return; }
            target = start;
            shown = State.From(start);
            fade = 1f;
            Push(shown);
        }

        void OnDisable()
        {
            LightingProfile.Changed -= OnProfileChanged;
            if (current == this) current = null;
        }

        void OnValidate()
        {
            if (!Application.isPlaying && isActiveAndEnabled && initialProfile) { target = initialProfile; shown = State.From(initialProfile); Push(shown); }
        }

        void OnProfileChanged(LightingProfile p)
        {
            if (p != target) return;
            fade = 1f;
            shown = State.From(p);
            Push(shown);
        }

        void FadeTo(LightingProfile profile, float seconds)
        {
            if (profile == target) return;
            target = profile;
            from = shown;
            fadeSeconds = seconds;
            fade = seconds > 0f ? 0f : 1f;
            if (fade >= 1f) { shown = State.From(profile); Push(shown); }
        }

        void Update()
        {
            if (target == null || fade >= 1f) return;
            fade = Mathf.MoveTowards(fade, 1f, Time.deltaTime / fadeSeconds);
            shown = State.Lerp(from, State.From(target), Mathf.SmoothStep(0f, 1f, fade));
            Push(shown);
        }

        void Push(State s)
        {
            // Radial fog (when a PlayerFlashlight drives it) and the native fog (Scene View, scenes without one) stay equal.
            Shader.SetGlobalFloat(BaseDensityId, s.fogDensity);
            Shader.SetGlobalFloat(BaseBrightnessId, s.brightness);
            // Write only on change: in edit mode every write marks the scene as modified.
            if (!RenderSettings.fog) RenderSettings.fog = true;
            if (RenderSettings.fogMode != FogMode.ExponentialSquared) RenderSettings.fogMode = FogMode.ExponentialSquared;
            if (RenderSettings.fogColor != s.fogColor) RenderSettings.fogColor = s.fogColor;
            if (!Mathf.Approximately(RenderSettings.fogDensity, s.fogDensity)) RenderSettings.fogDensity = s.fogDensity;
            if (RenderSettings.ambientMode != AmbientMode.Flat) RenderSettings.ambientMode = AmbientMode.Flat;
            if (RenderSettings.ambientLight != s.ambientColor) RenderSettings.ambientLight = s.ambientColor;

            if (cameras != null && cameras.Length > 0)
            {
                foreach (var c in cameras) if (c && c.backgroundColor != s.fogColor) c.backgroundColor = s.fogColor;
            }
            else if (Application.isPlaying && Camera.main)
            {
                Camera.main.backgroundColor = s.fogColor;
            }
        }
    }
}
