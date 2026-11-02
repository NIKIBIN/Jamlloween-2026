using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Spike2.Core
{
    /// <summary>
    /// The only systems object in Spike2_Circus. On Awake:
    ///  1. turns the F1 panel into spike 2 mode and loads every Tunables copy;
    ///  2. adds the spike 1 scene-level components that stay (GameState, DebugPanel, OSD, VHS low-res, interference, messages);
    ///  3. instantiates every prefab under any Resources/Systems/ folder (each agent ships its own, sorted by name).
    /// Systems adopt their map objects in Start() through MapRegistry (filled on Awake by MapRoot/MapId).
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public class SystemsBootstrap : MonoBehaviour
    {
        public static SystemsBootstrap I { get; private set; }

        [Tooltip("debug_lines.mat (spike 1). Used for the runtime gizmos.")]
        public Material debugLineMaterial;
        [Tooltip("Add the spike 1 scene-level components (GameState, DebugPanel, OSD...).")]
        public bool addSpike1Systems = true;
        [Tooltip("Prefabs to skip (by name), e.g. to test a scene without one system.")]
        public List<string> skipPrefabs = new List<string>();

        readonly List<GameObject> spawned = new List<GameObject>();
        public IReadOnlyList<GameObject> Spawned => spawned;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            GameContext.RunStartTime = Time.time;
            DebugRegistry.Spike2Mode = true;
            TunablesHub.LoadAll();

            if (addSpike1Systems) AddSpike1Systems();
            Circus.GameState.CaughtOverride = cause => Services.Get<ICaptureDirector>().Capture(cause);

            var prefabs = Resources.LoadAll<GameObject>("Systems").OrderBy(p => p.name, System.StringComparer.Ordinal);
            foreach (var p in prefabs)
            {
                if (skipPrefabs.Contains(p.name)) continue;
                var go = Instantiate(p);
                go.name = p.name;
                spawned.Add(go);
            }

            RegisterDebug();
        }

        void OnDestroy()
        {
            if (I == this) I = null;
            DebugRegistry.RemoveOwner(this);
        }

        void AddSpike1Systems()
        {
            T Ensure<T>() where T : Component => GetComponent<T>() ?? gameObject.AddComponent<T>();
            Ensure<Circus.GameState>();
            Ensure<Circus.DebugPanel>();
            Ensure<Circus.OSDOverlay>();
            Ensure<Circus.LowResRenderer>();
            Ensure<Circus.InterferenceDriver>();
            Ensure<Circus.MessageUI>();
            Ensure<DebugOverlayHud>();
            Ensure<DebugGizmoRenderer>().lineMaterial = debugLineMaterial;
        }

        // ------------------------------------------------------------------ core debug tabs

        void RegisterDebug()
        {
            const string tab = "Jogo";
            DebugRegistry.AddText(tab, () => $"Ato: {GameContext.Act}   beat: {GameContext.Beat}   t={GameContext.RunTime:0}s   capturas: {GameContext.Captures}", this);
            DebugRegistry.AddToggle(tab, "God mode", () => Circus.Tunables.I.godMode, v => Circus.Tunables.I.godMode = v, this);
            DebugRegistry.AddToggle(tab, "Gizmos do spike 1 (cone, barulho)", () => Circus.Tunables.I.showGizmos, v => Circus.Tunables.I.showGizmos = v, this);
            DebugRegistry.AddText(tab, ServicesSummary, this);
            DebugRegistry.AddText(tab, () => $"MapRegistry: {MapRegistry.Count} ids, {MapRegistry.Duplicates.Count} duplicados   sistemas: {string.Join(", ", spawned.Where(s => s).Select(s => s.name))}", this);
            DebugRegistry.AddOverlay(tab, "Ato / beat / tempo", () => $"{GameContext.Act} · {GameContext.Beat} · {GameContext.RunTime:0}s", false, this);

            DebugRegistry.AddCustom("Teleporte", DrawTeleport, this);
        }

        static string ServicesSummary()
        {
            string S<T>(string n) where T : class => Services.Has<T>() ? n : $"<color=#888>{n}(stub)</color>";
            return "Serviços: " + string.Join(" ", new[]
            {
                S<IPlayer>("player"), S<IPocket>("bolso"), S<IThrowables>("garrafas"), S<IClipboard>("prancheta"),
                S<IPanelSystem>("painel"), S<IZatan>("zatan"), S<IZatanDirector>("diretor"), S<ICameraNetwork>("câmeras"),
                S<IAnnouncer>("anúncios"), S<IAmbushWarning>("emboscada"), S<ICaptureDirector>("captura"),
            });
        }

        void DrawTeleport()
        {
            if (!MapData.Loaded) { GUILayout.Label("(sem mapa)"); return; }
            var spawn = MapRegistry.Get("spawn");
            if (spawn && GUILayout.Button("Spawn")) TeleportTo(spawn.transform.position);
            foreach (var z in MapData.Zones)
                if (GUILayout.Button($"{z.label} ({z.id})")) TeleportTo(ZoneTeleportPoint(z));
        }

        /// <summary>Walkable point in a zone: the zone's waypoint nearest to its center, else the NavMesh near the center.</summary>
        public static Vector3 ZoneTeleportPoint(MapData.Zone z)
        {
            var c = z.Center3;
            MapId best = null; float bestD = float.MaxValue;
            foreach (var w in MapRegistry.All("waypoint"))
            {
                if (!z.Contains(w.transform.position)) continue;
                float d = (w.transform.position - c).sqrMagnitude;
                if (d < bestD) { bestD = d; best = w; }
            }
            if (best) return best.transform.position;
            return NavMesh.SamplePosition(c, out var hit, 10f, NavMesh.AllAreas) ? hit.position : c;
        }

        static void TeleportTo(Vector3 p)
        {
            var player = Services.Get<IPlayer>();
            player.Teleport(p + Vector3.up * 0.05f, player.Transform ? player.Transform.eulerAngles.y : 0f);
        }
    }
}
