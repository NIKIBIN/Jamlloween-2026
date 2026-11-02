using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Circus.EditorTools
{
    /// <summary>Gameplay components attached by the builder (kept apart from raw geometry).</summary>
    public static partial class CircusMapBuilder
    {
        static readonly Dictionary<string, string> ZoneLabels = new Dictionary<string, string>
        {
            { "trailer", "trailer" }, { "zatan_booth", "cabine" }, { "generator_shed", "galpão norte" },
            { "control_room", "sala de controle" }, { "carousel", "carrossel" },
        };

        static RepairConfig RepairConfigFor(Dictionary<string, object> m)
        {
            string id = (string)m["id"];
            string path = $"{RepairDir}/{id}.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<RepairConfig>(path);
            if (cfg == null) { cfg = ScriptableObject.CreateInstance<RepairConfig>(); AssetDatabase.CreateAsset(cfg, path); }
            cfg.variant = (string)m["repair"] == "sequence" ? RepairVariant.Sequence : RepairVariant.Hold;
            cfg.durationSeconds = F(m, "duration_s", cfg.variant == RepairVariant.Hold ? 4f : F(m, "steps", 5) * 1.5f);
            cfg.steps = Mathf.RoundToInt(F(m, "steps", Mathf.Max(1, Mathf.RoundToInt(cfg.durationSeconds * 0.5f))));
            cfg.noiseRadius = F(m, "noise_radius_m", 4f);
            cfg.locksCamera = m.TryGetValue("locks_camera", out var lc) && (bool)lc;
            cfg.onRepairedNote = m.TryGetValue("on_repaired", out var n) ? (string)n : "";
            EditorUtility.SetDirty(cfg);
            return cfg;
        }

        static RepairableMachine AddRepairable(GameObject go, Dictionary<string, object> m, Renderer led)
        {
            var rm = go.AddComponent<RepairableMachine>();
            rm.id = (string)m["id"];
            rm.label = (string)m["label"];
            string zone = (string)m["zone"];
            rm.zone = ZoneLabels.TryGetValue(zone, out var zl) ? zl : zone;
            rm.order = Mathf.RoundToInt(F(m, "order"));
            rm.config = RepairConfigFor(m);
            if (led)
            {
                Set(rm, "statusLed", led);
                Set(rm, "brokenMat", EmissiveMat("led_red"));
                Set(rm, "repairedMat", EmissiveMat("led_green"));
            }
            return rm;
        }

        static partial void AddMachineComponents(GameObject go, Dictionary<string, object> m, Renderer led)
        {
            AddRepairable(go, m, led);
            AddMachineExtras(go, (string)m["id"]);
        }

        static partial void AddMachineExtras(GameObject go, string id);

        static partial void AddHidingComponents(GameObject go, Dictionary<string, object> h)
        {
            go.AddComponent<HidingSpot>().id = (string)h["id"];
        }

        static partial void AddCircuitComponent(GameObject go, string id, List<Light> lights, List<Renderer> bulbs, bool batteryBacked)
        {
            var c = go.AddComponent<LightCircuit>();
            c.id = id;
            c.batteryBacked = batteryBacked;
            Set(c, "lights", lights);
            Set(c, "bulbs", bulbs);
            Set(c, "bulbOn", EmissiveMat("bulb_on"));
            Set(c, "bulbOff", EmissiveMat("bulb_off"));
        }

        static partial void AddSwitchComponent(GameObject root, GameObject box, string id, string[] circuits, string label)
        {
            var s = root.AddComponent<LightSwitch>();
            s.id = id; s.label = label; s.circuitIds = circuits;
        }

        static partial void AddZatanComponents(GameObject zatan, Dictionary<string, object> z)
        {
            var agent = zatan.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f; agent.height = 2.3f; agent.speed = 2.2f; agent.angularSpeed = 300f;
            agent.acceleration = 14f; agent.stoppingDistance = 0.2f; agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

            var eye = new GameObject("EyePoint").transform;
            eye.SetParent(zatan.transform, false);
            eye.localPosition = new Vector3(0, 2.1f, 0.3f);

            var perc = zatan.AddComponent<ZatanPerception>();
            Set(perc, "eye", eye);
            var brain = zatan.AddComponent<ZatanBrain>();
            Set(brain, "patrolRoute", Waypoints.Cast<Object>().ToList());
            var eyes = zatan.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Eye").Select(t => (Object)t.gameObject).ToList();
            Set(brain, "eyes", eyes);
            zatan.AddComponent<ZatanCatch>();

            // The animatronic itself is machine M2.
            var m2 = L(J["machines"]).Cast<Dictionary<string, object>>().First(m => (string)m["id"] == "M2_zatan");
            AddRepairable(zatan, m2, null);
            AddZatanExtras(zatan);
        }

        static partial void AddZatanExtras(GameObject zatan);

        static partial void AddSystemComponents(GameObject sys)
        {
            sys.AddComponent<PowerGrid>();
            sys.AddComponent<TaskListUI>();
            sys.AddComponent<RepairMinigameUI>();
            sys.AddComponent<MessageUI>();
            sys.AddComponent<GameOverScreen>();
            var dd = sys.AddComponent<DebugDraw>();
            Set(dd, "lineMaterial", LineMaterial());
            AddSystemExtras(sys);
        }

        static partial void AddSystemExtras(GameObject sys);

        static Material LineMaterial()
        {
            const string path = MatDir + "/debug_lines.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Hidden/Internal-Colored"));
                m.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                m.SetInt("_Cull", 0);
                AssetDatabase.CreateAsset(m, path);
            }
            return m;
        }
    }
}
