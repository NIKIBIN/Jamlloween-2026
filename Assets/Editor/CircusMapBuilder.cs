using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Circus.EditorTools
{
    /// <summary>
    /// Tools > Build Circus Map. Reads Assets/circus_map.json and (re)generates Assets/Scenes/Spike_Circus.unity.
    /// Idempotent: deletes the generated roots before recreating them.
    /// </summary>
    public static partial class CircusMapBuilder
    {
        public const string ScenePath = "Assets/Scenes/Spike_Circus.unity";
        const string JsonPath = "Assets/circus_map.json";
        const string DataDir = "Assets/Data";
        const string MatDir = "Assets/Data/Materials";
        const string RepairDir = "Assets/Data/RepairConfigs";
        static readonly string[] GeneratedRoots = { "Map", "Systems", "Player", "Zatan", "Lighting" };

        static Dictionary<string, object> J;
        static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();

        // ---------- palette (desaturated, one color per block type) ----------
        static readonly Dictionary<string, Color> Palette = new Dictionary<string, Color>
        {
            { "ground", new Color(0.13f, 0.125f, 0.12f) },
            { "fence", new Color(0.33f, 0.33f, 0.34f) },
            { "wall", new Color(0.52f, 0.50f, 0.45f) },
            { "stall", new Color(0.42f, 0.31f, 0.23f) },
            { "crate", new Color(0.50f, 0.40f, 0.26f) },
            { "container", new Color(0.28f, 0.36f, 0.40f) },
            { "prop_solid", new Color(0.38f, 0.36f, 0.33f) },
            { "tent", new Color(0.46f, 0.14f, 0.12f) },
            { "tent_stripe", new Color(0.62f, 0.58f, 0.50f) },
            { "carousel", new Color(0.55f, 0.46f, 0.24f) },
            { "carousel_column", new Color(0.62f, 0.56f, 0.44f) },
            { "horse", new Color(0.70f, 0.66f, 0.60f) },
            { "door", new Color(0.34f, 0.24f, 0.17f) },
            { "machine", new Color(0.22f, 0.42f, 0.50f) },
            { "screen", new Color(0.10f, 0.12f, 0.12f) },
            { "hiding", new Color(0.20f, 0.30f, 0.22f) },
            { "switch", new Color(0.72f, 0.64f, 0.22f) },
            { "camera", new Color(0.18f, 0.18f, 0.20f) },
            { "post", new Color(0.16f, 0.16f, 0.16f) },
            { "zatan_body", new Color(0.32f, 0.14f, 0.38f) },
            { "zatan_head", new Color(0.82f, 0.78f, 0.68f) },
            { "tarot", new Color(0.85f, 0.80f, 0.62f) },
        };

        static readonly Dictionary<string, Color> Emissive = new Dictionary<string, Color>
        {
            { "bulb_on", new Color(1.0f, 0.72f, 0.40f) * 2.5f },
            { "bulb_off", new Color(0.05f, 0.04f, 0.03f) },
            { "led_red", new Color(1.0f, 0.08f, 0.05f) * 2f },
            { "led_green", new Color(0.15f, 1.0f, 0.25f) * 2f },
            { "led_off", new Color(0.02f, 0.02f, 0.02f) },
            { "zatan_eyes", new Color(1.0f, 0.15f, 0.1f) * 3f },
            { "tarot_glow", new Color(0.9f, 0.8f, 0.5f) * 0.8f },
        };

        [MenuItem("Tools/Build Circus Map")]
        public static void Build()
        {
            J = (Dictionary<string, object>)MiniJson.Parse(File.ReadAllText(JsonPath));
            EnsureFolder(DataDir); EnsureFolder(MatDir); EnsureFolder(RepairDir); EnsureFolder("Assets/Data/Resources");
            Mats.Clear();
            EnsureTunables();

            OpenOrCreateScene();
            var scene = SceneManager.GetActiveScene();
            foreach (var go in scene.GetRootGameObjects())
                if (GeneratedRoots.Contains(go.name)) Object.DestroyImmediate(go);

            SetupLighting();
            var map = new GameObject("Map");
            BuildGround(map.transform);
            BuildBlocks(map.transform);
            BuildTent(map.transform);
            BuildCarousel(map.transform);
            BuildDoors(map.transform);
            BuildMachines(map.transform);
            BuildHiding(map.transform);
            BuildCameras(map.transform);
            BuildSwitchesAndLights(map.transform);
            BuildTarot(map.transform);
            BuildTriggers(map.transform);
            BuildWaypoints(map.transform);
            BuildSpawnAndZones(map.transform);
            BuildZoneIdentity(map.transform);

            BakeNavMesh(map);

            BuildZatan();
            BuildPlayer();
            BuildSystems();
            PostBuild();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[CircusMapBuilder] Built {ScenePath}: {CountAll(scene)} objects.");
        }

        static partial void PostBuild();

        public static int CountAll(Scene s) => s.GetRootGameObjects().Sum(g => g.GetComponentsInChildren<Transform>(true).Length);

        // ============================== scene / lighting ==============================

        static void OpenOrCreateScene()
        {
            if (SceneManager.GetActiveScene().path == ScenePath) return;
            if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            else
            {
                var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(s, ScenePath);
            }
        }

        static void SetupLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.065f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.015f, 0.015f, 0.02f);
            RenderSettings.fogDensity = 0.055f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.reflectionIntensity = 0f;

            const string lsPath = "Assets/Data/SpikeLighting.lighting";
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(lsPath);
            if (ls == null)
            {
                ls = new LightingSettings { bakedGI = false, realtimeGI = false };
                AssetDatabase.CreateAsset(ls, lsPath);
            }
            Lightmapping.lightingSettings = ls;

            var lighting = new GameObject("Lighting");
            var moon = new GameObject("Moon").AddComponent<Light>();
            moon.transform.SetParent(lighting.transform);
            moon.type = LightType.Directional;
            moon.color = new Color(0.45f, 0.55f, 0.8f);
            moon.intensity = 0.06f;
            moon.shadows = LightShadows.None;
            moon.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
        }

        // ============================== geometry ==============================

        static void BuildGround(Transform map)
        {
            var g = (Dictionary<string, object>)((Dictionary<string, object>)J["meta"])["ground"];
            var c = V2(g["center"]); var s = V2(g["size"]);
            var go = Box(map, "Ground", c, s, 0.2f, "ground", -0.2f);
            go.isStatic = true;
        }

        static void BuildBlocks(Transform map)
        {
            var blocksRoot = Child(map, "Blocks");
            foreach (Dictionary<string, object> b in L(J["blocks"]))
            {
                string type = (string)b["type"];
                var parent = Child(blocksRoot, type);
                var go = Box(parent, (string)b["id"], V2(b["center"]), V2(b["size"]), F(b, "height"), type);
                go.isStatic = true;
                if (((string)b["id"]).EndsWith("_movable")) MakeMovable(go);
            }
        }

        static void MakeMovable(GameObject go)
        {
            go.isStatic = false;
            go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            var ob = go.AddComponent<NavMeshObstacle>();
            ob.carving = true; ob.shape = NavMeshObstacleShape.Box; ob.size = Vector3.one;
        }

        static void BuildTent(Transform map)
        {
            var t = (Dictionary<string, object>)J["tent"];
            var c = V2(t["center"]); float r = F(t, "radius"), h = F(t, "height");
            var tent = Cyl(map, "BigTop", c, r, h, "tent");
            tent.isStatic = true;
            // stripes ring + top cone-ish cap for silhouette
            var cap = Cyl(tent.transform.parent, "BigTop_Cap", c, r * 0.55f, 2.5f, "tent_stripe", h);
            cap.isStatic = true;
            var pole = Cyl(tent.transform.parent, "BigTop_Pole", c, 0.3f, 6.5f, "post", h + 2.5f);
            pole.isStatic = true;
        }

        static void BuildCarousel(Transform map)
        {
            var c = (Dictionary<string, object>)J["carousel"];
            var center = V2(c["center"]);
            var root = Child(map, "Carousel");
            root.position = new Vector3(center.x, 0, center.y);

            var platformRoot = new GameObject("Rotor").transform;
            platformRoot.SetParent(root, false);
            Cyl(platformRoot, "Platform", Vector2.zero, F(c, "platform_radius"), F(c, "platform_height"), "carousel", 0, true);
            Cyl(root, "Column", Vector2.zero, F(c, "column_radius"), F(c, "column_height"), "carousel_column", 0, true);
            var roof = Cyl(platformRoot, "Roof", Vector2.zero, F(c, "platform_radius") + 0.4f, 0.25f, "tent", F(c, "column_height"), true);
            roof.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f;
                var p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (F(c, "platform_radius") - 1.1f);
                var horse = Box(platformRoot, "Horse" + i, p, new Vector2(0.35f, 1.1f), 0.9f, "horse", F(c, "platform_height") + 0.4f, true);
                horse.transform.localRotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
                horse.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
                var pole = Cyl(platformRoot, "HorsePole" + i, p, 0.05f, F(c, "column_height") - F(c, "platform_height"), "post", F(c, "platform_height"), true);
                pole.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            }

            // low fence ring with openings
            float fr = F(c, "fence_radius"), fh = F(c, "fence_height"), ow = F(c, "opening_width");
            var openings = L(c["fence_openings_deg"]).Select(o => (float)(double)o).ToList();
            var fence = Child(root, "Fence");
            int segs = 44;
            float segLen = 2f * Mathf.PI * fr / segs;
            float halfOpenDeg = Mathf.Rad2Deg * (ow * 0.5f + segLen * 0.5f) / fr;
            for (int i = 0; i < segs; i++)
            {
                float deg = (i + 0.5f) * 360f / segs;
                if (openings.Any(o => Mathf.Abs(Mathf.DeltaAngle(deg, o)) < halfOpenDeg)) continue;
                float a = deg * Mathf.Deg2Rad;
                var p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * fr;
                var seg = Box(fence, "F" + i, p, new Vector2(0.15f, segLen * 1.05f), fh, "fence", 0, true);
                seg.transform.localRotation = Quaternion.Euler(0, -deg, 0);
                seg.isStatic = true;
            }
            AddCarouselComponents(root.gameObject, platformRoot);
        }

        static partial void AddCarouselComponents(GameObject root, Transform rotor);

        static Dictionary<string, Dictionary<string, object>> RoomsById()
            => L(J["rooms"]).Cast<Dictionary<string, object>>().ToDictionary(r => (string)r["id"]);

        static void BuildDoors(Transform map)
        {
            var root = Child(map, "Doors");
            root.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            var rooms = RoomsById();
            const float doorH = 2.2f, thick = 0.12f;
            foreach (Dictionary<string, object> d in L(J["doors"]))
            {
                var c = V2(d["center"]); float w = F(d, "width");
                bool alongX = (string)d["wall_axis"] == "x";
                float wallH = F(rooms[(string)d["room"]], "wall_height");

                // lintel above the opening
                var lintelSize = alongX ? new Vector2(w, 0.3f) : new Vector2(0.3f, w);
                Box(root, (string)d["id"] + "_lintel", c, lintelSize, wallH - doorH, "wall", doorH);

                var hinge = new GameObject((string)d["id"]).transform;
                hinge.SetParent(root, false);
                hinge.position = alongX ? new Vector3(c.x - w / 2, 0, c.y) : new Vector3(c.x, 0, c.y - w / 2);
                var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                panel.name = "Panel";
                panel.transform.SetParent(hinge, false);
                panel.transform.localPosition = alongX ? new Vector3(w / 2, doorH / 2, 0) : new Vector3(0, doorH / 2, w / 2);
                panel.transform.localScale = alongX ? new Vector3(w - 0.04f, doorH, thick) : new Vector3(thick, doorH, w - 0.04f);
                panel.GetComponent<Renderer>().sharedMaterial = Mat("door");
                var ob = panel.AddComponent<NavMeshObstacle>();
                ob.shape = NavMeshObstacleShape.Box; ob.size = Vector3.one; ob.carving = true; ob.enabled = false;

                bool lockable = d.TryGetValue("lockable", out var lk) && (bool)lk;
                Renderer indicator = null;
                if (lockable)
                {
                    var ind = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    ind.name = "LockLed";
                    Object.DestroyImmediate(ind.GetComponent<Collider>());
                    ind.transform.SetParent(hinge, false);
                    ind.transform.localPosition = alongX ? new Vector3(w - 0.15f, 1.1f, 0) : new Vector3(0, 1.1f, w - 0.15f);
                    ind.transform.localScale = alongX ? new Vector3(0.08f, 0.08f, thick + 0.06f) : new Vector3(thick + 0.06f, 0.08f, 0.08f);
                    indicator = ind.GetComponent<Renderer>();
                    indicator.sharedMaterial = EmissiveMat("led_green");
                }

                var door = hinge.gameObject.AddComponent<Door>();
                door.id = (string)d["id"];
                door.lockable = lockable;
                Set(door, "panel", panel.transform);
                Set(door, "obstacle", ob);
                if (indicator)
                {
                    Set(door, "lockIndicator", indicator);
                    Set(door, "lockedMat", EmissiveMat("led_red"));
                    Set(door, "unlockedMat", EmissiveMat("led_green"));
                }
            }
        }

        // ============================== interactive placeholders ==============================

        static readonly Dictionary<string, Vector3> MachineSize = new Dictionary<string, Vector3>
        {
            { "M1_radio", new Vector3(0.8f, 1.0f, 0.5f) },
            { "M3_generator", new Vector3(1.8f, 1.3f, 1.1f) },
            { "M4_monitors", new Vector3(0.8f, 0.9f, 2.4f) },
            { "M5_carousel_panel", new Vector3(0.6f, 1.4f, 0.5f) },
        };

        static void BuildMachines(Transform map)
        {
            var root = Child(map, "Machines");
            foreach (Dictionary<string, object> m in L(J["machines"]))
            {
                string id = (string)m["id"];
                if (id == "M2_zatan") continue; // the Zatan animatronic itself is the machine (see BuildZatan)
                var size = MachineSize.TryGetValue(id, out var s) ? s : new Vector3(1, 1.2f, 1);
                var go = Box(root, id, V2(m["pos"]), new Vector2(size.x, size.z), size.y, "machine");
                if (id == "M4_monitors")
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        var scr = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        scr.name = "Screen" + i;
                        Object.DestroyImmediate(scr.GetComponent<Collider>());
                        scr.transform.SetParent(go.transform.parent, false);
                        scr.transform.position = go.transform.position + new Vector3(-0.15f, size.y / 2 + 0.3f, i * 0.75f);
                        scr.transform.localScale = new Vector3(0.1f, 0.55f, 0.7f);
                        scr.GetComponent<Renderer>().sharedMaterial = Mat("screen");
                        scr.transform.SetParent(go.transform, true);
                    }
                }
                var led = StatusLed(go.transform, size.y);
                AddMachineComponents(go, m, led);
            }
        }

        static Renderer StatusLed(Transform machine, float height)
        {
            var led = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            led.name = "StatusLed";
            Object.DestroyImmediate(led.GetComponent<Collider>());
            led.transform.position = machine.position + Vector3.up * (height / 2 + 0.12f);
            led.transform.localScale = Vector3.one * 0.22f;
            led.transform.SetParent(machine, true);
            var r = led.GetComponent<Renderer>();
            r.sharedMaterial = EmissiveMat("led_red");
            return r;
        }

        static partial void AddMachineComponents(GameObject go, Dictionary<string, object> m, Renderer led);

        static void BuildHiding(Transform map)
        {
            var root = Child(map, "Hiding");
            foreach (Dictionary<string, object> h in L(J["hiding_spots"]))
            {
                string type = (string)h["type"];
                bool tent = type.Contains("barraca");
                var size = tent ? new Vector2(1.3f, 1.3f) : new Vector2(0.9f, 0.9f);
                var go = Box(root, (string)h["id"], V2(h["pos"]), size, tent ? 2.2f : 2.0f, "hiding");
                AddHidingComponents(go, h);
            }
        }

        static partial void AddHidingComponents(GameObject go, Dictionary<string, object> h);

        static void BuildCameras(Transform map)
        {
            var root = Child(map, "Cameras");
            root.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            foreach (Dictionary<string, object> c in L(J["cameras"]))
            {
                var p = V2(c["pos"]); float hgt = F(c, "height");
                var look = V2(c["default_look"]);
                var pivot = new GameObject((string)c["id"]).transform;
                pivot.SetParent(root, false);
                pivot.position = new Vector3(p.x, hgt, p.y);
                pivot.rotation = Quaternion.LookRotation(new Vector3(look.x, 1f, look.y) - pivot.position);

                var mount = Cyl(root, (string)c["id"] + "_mount", p, 0.06f, hgt, "post", 0, false);
                Object.DestroyImmediate(mount.GetComponent<Collider>());

                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.name = "Body";
                Object.DestroyImmediate(body.GetComponent<Collider>());
                body.transform.SetParent(pivot, false);
                body.transform.localScale = new Vector3(0.25f, 0.22f, 0.45f);
                body.GetComponent<Renderer>().sharedMaterial = Mat("camera");
                var led = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                led.name = "Led";
                Object.DestroyImmediate(led.GetComponent<Collider>());
                led.transform.SetParent(pivot, false);
                led.transform.localPosition = new Vector3(0.07f, 0.08f, 0.24f);
                led.transform.localScale = Vector3.one * 0.06f;
                led.GetComponent<Renderer>().sharedMaterial = EmissiveMat("led_off");
                AddCameraComponents(pivot.gameObject, c, led.GetComponent<Renderer>());
            }
        }

        static partial void AddCameraComponents(GameObject pivot, Dictionary<string, object> c, Renderer led);

        static readonly Dictionary<string, string[]> SwitchCircuits = new Dictionary<string, string[]>
        {
            { "S1", new[] { "trailer" } },
            { "S2", new[] { "control_room" } },
            { "S3", new[] { "plaza", "ring" } },
            { "S4", new[] { "carousel" } },
            { "S5", new[] { "generator_shed" } },
        };

        static void BuildSwitchesAndLights(Transform map)
        {
            var lightsRoot = Child(map, "Lights");
            lightsRoot.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            var lp = (Dictionary<string, object>)J["lamp_posts"];
            foreach (var kv in lp)
            {
                var circuit = Child(lightsRoot, kv.Key);
                int i = 0;
                var lights = new List<Light>(); var bulbs = new List<Renderer>();
                foreach (var posObj in L(kv.Value))
                {
                    var p = V2(posObj);
                    var post = Cyl(circuit, $"Lamp{i}", p, 0.08f, 4f, "post");
                    var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    bulb.name = "Bulb";
                    Object.DestroyImmediate(bulb.GetComponent<Collider>());
                    bulb.transform.SetParent(post.transform.parent, false);
                    bulb.transform.position = new Vector3(p.x, 4.1f, p.y);
                    bulb.transform.localScale = Vector3.one * 0.35f;
                    bulb.GetComponent<Renderer>().sharedMaterial = EmissiveMat("bulb_off");
                    bulbs.Add(bulb.GetComponent<Renderer>());
                    var l = new GameObject("Light").AddComponent<Light>();
                    l.transform.SetParent(post.transform.parent, false);
                    l.transform.position = new Vector3(p.x, 3.9f, p.y);
                    l.type = LightType.Point; l.range = 14f; l.intensity = 22f; l.color = LampColor(kv.Key);
                    l.shadows = LightShadows.None; l.enabled = false;
                    lights.Add(l);
                    i++;
                }
                AddCircuitComponent(circuit.gameObject, kv.Key, lights, bulbs, false);
            }

            // room lights
            foreach (var r in RoomsById().Values)
            {
                string id = (string)r["id"];
                if (id == "zatan_booth") continue;
                var circuit = Child(lightsRoot, id);
                var c = V2(r["center"]); float h = F(r, "wall_height");
                var lamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lamp.name = "CeilingLamp";
                Object.DestroyImmediate(lamp.GetComponent<Collider>());
                lamp.transform.SetParent(circuit, false);
                lamp.transform.position = new Vector3(c.x, h - 0.1f, c.y);
                lamp.transform.localScale = new Vector3(0.6f, 0.08f, 0.6f);
                lamp.GetComponent<Renderer>().sharedMaterial = EmissiveMat("bulb_off");
                var l = new GameObject("Light").AddComponent<Light>();
                l.transform.SetParent(circuit, false);
                l.transform.position = new Vector3(c.x, h - 0.3f, c.y);
                var inner = V2(r["inner_size"]);
                l.type = LightType.Point; l.range = Mathf.Max(inner.x, inner.y) + 1f; l.intensity = 7f;
                l.color = RoomColor(id); l.shadows = LightShadows.None; l.enabled = false;
                AddCircuitComponent(circuit.gameObject, id, new List<Light> { l }, new List<Renderer> { lamp.GetComponent<Renderer>() }, id == "trailer");
            }

            var swRoot = Child(map, "Switches");
            swRoot.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            foreach (Dictionary<string, object> s in L(J["switches"]))
            {
                var p = V2(s["pos"]);
                string id = (string)s["id"];
                var root = new GameObject(id).transform;
                root.SetParent(swRoot, false);
                root.position = new Vector3(p.x, 0, p.y);
                var pole = Box(root, "Pole", Vector2.zero, new Vector2(0.08f, 0.08f), 1.15f, "post", 0, true);
                Object.DestroyImmediate(pole.GetComponent<Collider>());
                var box = Box(root, "Switch", Vector2.zero, new Vector2(0.25f, 0.25f), 0.3f, "switch", 1.1f, true);
                AddSwitchComponent(root.gameObject, box, id, SwitchCircuits.TryGetValue(id, out var cs) ? cs : new string[0], (string)s["controls"]);
            }
        }

        static partial void AddCircuitComponent(GameObject go, string id, List<Light> lights, List<Renderer> bulbs, bool batteryBacked);
        static partial void AddSwitchComponent(GameObject root, GameObject box, string id, string[] circuits, string label);

        static void BuildTarot(Transform map)
        {
            var root = Child(map, "Tarot");
            root.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            foreach (Dictionary<string, object> t in L(J["tarot_cards"]))
            {
                var p = V2(t["pos"]);
                var card = GameObject.CreatePrimitive(PrimitiveType.Cube);
                card.name = (string)t["id"];
                card.transform.SetParent(root, false);
                card.transform.position = new Vector3(p.x, 1.2f, p.y);
                card.transform.localScale = new Vector3(0.3f, 0.5f, 0.03f);
                card.GetComponent<Renderer>().sharedMaterial = EmissiveMat("tarot_glow");
                AddTarotComponent(card, t);
            }
        }

        static partial void AddTarotComponent(GameObject card, Dictionary<string, object> t);

        static void BuildTriggers(Transform map)
        {
            var root = Child(map, "Triggers");
            foreach (Dictionary<string, object> t in L(J["triggers"]))
            {
                var c = V2(t["center"]); var s = V2(t["size"]);
                var go = new GameObject((string)t["id"]);
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(c.x, 1.5f, c.y);
                var bc = go.AddComponent<BoxCollider>();
                bc.isTrigger = true; bc.size = new Vector3(s.x, 3f, s.y);
                AddTriggerComponent(go, t);
            }
        }

        static partial void AddTriggerComponent(GameObject go, Dictionary<string, object> t);

        static readonly List<Transform> Waypoints = new List<Transform>();

        static void BuildWaypoints(Transform map)
        {
            var z = (Dictionary<string, object>)J["zatan"];
            var root = Child(map, "ZatanWaypoints");
            var byId = new Dictionary<string, Transform>();
            foreach (Dictionary<string, object> w in L(z["waypoints"]))
            {
                var p = V2(w["pos"]);
                var t = new GameObject((string)w["id"]).transform;
                t.SetParent(root, false);
                t.position = new Vector3(p.x, 0, p.y);
                byId[t.name] = t;
            }
            Waypoints.Clear();
            foreach (var id in L(z["patrol_default"])) Waypoints.Add(byId[(string)id]);
        }

        static Vector3 spawnPos; static float spawnYaw;

        static void BuildSpawnAndZones(Transform map)
        {
            var s = (Dictionary<string, object>)J["spawn"];
            var p = V2(s["pos"]);
            spawnPos = new Vector3(p.x, 0.05f, p.y); spawnYaw = F(s, "yaw");
            var spawn = new GameObject("Spawn").transform;
            spawn.SetParent(map, false);
            spawn.position = spawnPos; spawn.rotation = Quaternion.Euler(0, spawnYaw, 0);

            var zones = Child(map, "Zones");
            foreach (Dictionary<string, object> z in L(J["debug_teleports"]))
            {
                var zp = V2(z["pos"]);
                var go = new GameObject((string)z["label"]);
                go.transform.SetParent(zones, false);
                go.transform.position = new Vector3(zp.x, 0.1f, zp.y);
                go.transform.rotation = Quaternion.Euler(0, F(z, "yaw"), 0);
                go.AddComponent<ZoneMarker>().label = (string)z["label"];
            }
        }

        // ============================== navmesh ==============================

        static void BakeNavMesh(GameObject map)
        {
            SetAgentRadius(0.4f);
            // Everything is an obstacle (no walkable islands on rooftops) except the ground and carousel platform.
            var notWalk = map.AddComponent<NavMeshModifier>();
            notWalk.overrideArea = true; notWalk.area = 1;
            foreach (var name in new[] { "Ground", "Carousel/Rotor/Platform" })
            {
                var t = map.transform.Find(name);
                var m = t.gameObject.GetComponent<NavMeshModifier>() ?? t.gameObject.AddComponent<NavMeshModifier>();
                m.overrideArea = true; m.area = 0;
            }
            var surface = map.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.agentTypeID = 0;
            surface.BuildNavMesh();
            const string path = "Assets/Data/NavMesh_Spike_Circus.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surface.navMeshData, path);
            EditorUtility.SetDirty(surface);
        }

        static void SetAgentRadius(float r)
        {
            var o = Unsupported.GetSerializedAssetInterfaceSingleton("NavMeshProjectSettings");
            var so = new SerializedObject(o);
            var settings = so.FindProperty("m_Settings");
            for (int i = 0; i < settings.arraySize; i++)
            {
                var el = settings.GetArrayElementAtIndex(i);
                if (el.FindPropertyRelative("agentTypeID").intValue == 0)
                    el.FindPropertyRelative("agentRadius").floatValue = r;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ============================== actors ==============================

        static GameObject zatanRoot;

        static void BuildZatan()
        {
            var z = (Dictionary<string, object>)J["zatan"];
            var p = V2(z["start"]);
            zatanRoot = new GameObject("Zatan");
            zatanRoot.transform.position = new Vector3(p.x, 0, p.y);
            zatanRoot.transform.rotation = Quaternion.Euler(0, 270, 0); // faces the booth opening (west)

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(zatanRoot.transform, false);
            body.transform.localPosition = new Vector3(0, 0.95f, 0);
            body.transform.localScale = new Vector3(0.8f, 0.95f, 0.8f);
            body.GetComponent<Renderer>().sharedMaterial = Mat("zatan_body");
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            Object.DestroyImmediate(head.GetComponent<Collider>());
            head.transform.SetParent(zatanRoot.transform, false);
            head.transform.localPosition = new Vector3(0, 2.05f, 0);
            head.transform.localScale = Vector3.one * 0.55f;
            head.GetComponent<Renderer>().sharedMaterial = Mat("zatan_head");
            var hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hat.name = "Turban";
            Object.DestroyImmediate(hat.GetComponent<Collider>());
            hat.transform.SetParent(zatanRoot.transform, false);
            hat.transform.localPosition = new Vector3(0, 2.38f, 0);
            hat.transform.localScale = new Vector3(0.6f, 0.12f, 0.6f);
            hat.GetComponent<Renderer>().sharedMaterial = Mat("zatan_body");
            for (int i = -1; i <= 1; i += 2)
            {
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = "Eye";
                Object.DestroyImmediate(eye.GetComponent<Collider>());
                eye.transform.SetParent(zatanRoot.transform, false);
                eye.transform.localPosition = new Vector3(0.1f * i, 2.1f, 0.24f);
                eye.transform.localScale = Vector3.one * 0.08f;
                eye.GetComponent<Renderer>().sharedMaterial = EmissiveMat("zatan_eyes");
                eye.SetActive(false);
            }
            AddZatanComponents(zatanRoot, z);
        }

        static partial void AddZatanComponents(GameObject zatan, Dictionary<string, object> z);

        static void BuildPlayer()
        {
            var player = new GameObject("Player");
            player.transform.position = spawnPos;
            player.transform.rotation = Quaternion.Euler(0, spawnYaw, 0);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0); cc.stepOffset = 0.45f; cc.slopeLimit = 50f;

            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(player.transform, false);
            pivot.localPosition = new Vector3(0, 1.65f, 0);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(pivot, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 150f; cam.fieldOfView = 70f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            var urp = camGo.AddComponent<UniversalAdditionalCameraData>();
            urp.renderPostProcessing = true;
            urp.antialiasing = AntialiasingMode.None;
            camGo.AddComponent<AudioListener>();

            var fl = new GameObject("Flashlight").AddComponent<Light>();
            fl.transform.SetParent(pivot, false);
            fl.transform.localPosition = new Vector3(0.2f, -0.15f, 0.1f);
            fl.type = LightType.Spot; fl.color = new Color(1f, 0.95f, 0.85f); fl.shadows = LightShadows.Soft;

            var fpc = player.AddComponent<FirstPersonController>();
            Set(fpc, "cameraPivot", pivot);
            var flash = player.AddComponent<Flashlight>();
            Set(flash, "spot", fl);
            player.AddComponent<PlayerNoise>();
            player.AddComponent<Interactor>();
            AddPlayerComponents(player, cam);
        }

        static partial void AddPlayerComponents(GameObject player, Camera cam);

        static void BuildSystems()
        {
            var sys = new GameObject("Systems");
            sys.AddComponent<GameState>();
            sys.AddComponent<DebugPanel>();
            AddSystemComponents(sys);
        }

        static partial void AddSystemComponents(GameObject sys);

        // ============================== helpers ==============================

        static void EnsureTunables()
        {
            const string p = "Assets/Data/Resources/Tunables.asset";
            if (AssetDatabase.LoadAssetAtPath<Tunables>(p) == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<Tunables>(), p);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static Transform Child(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t) return t;
            t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        static GameObject Box(Transform parent, string name, Vector2 centerXZ, Vector2 sizeXZ, float height, string mat, float y0 = 0f, bool local = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            var pos = new Vector3(centerXZ.x, y0 + height / 2f, centerXZ.y);
            if (local) go.transform.localPosition = pos; else go.transform.position = pos;
            go.transform.localScale = new Vector3(sizeXZ.x, height, sizeXZ.y);
            go.GetComponent<Renderer>().sharedMaterial = Mat(mat);
            return go;
        }

        static GameObject Cyl(Transform parent, string name, Vector2 centerXZ, float radius, float height, string mat, float y0 = 0f, bool local = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            var pos = new Vector3(centerXZ.x, y0 + height / 2f, centerXZ.y);
            if (local) go.transform.localPosition = pos; else go.transform.position = pos;
            go.transform.localScale = new Vector3(radius * 2f, height / 2f, radius * 2f);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var mc = go.AddComponent<MeshCollider>();
            mc.convex = true;
            go.GetComponent<Renderer>().sharedMaterial = Mat(mat);
            return go;
        }

        public static Material Mat(string key)
        {
            if (Mats.TryGetValue(key, out var m) && m) return m;
            string path = $"{MatDir}/{key}.mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", Palette.TryGetValue(key, out var c) ? c : Color.magenta);
            m.SetFloat("_Smoothness", 0.05f);
            EditorUtility.SetDirty(m);
            Mats[key] = m;
            return m;
        }

        public static Material EmissiveMat(string key)
        {
            if (Mats.TryGetValue(key, out var m) && m) return m;
            string path = $"{MatDir}/{key}.mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            var e = Emissive[key];
            m.SetColor("_BaseColor", new Color(Mathf.Min(1, e.r), Mathf.Min(1, e.g), Mathf.Min(1, e.b)) * 0.5f);
            m.SetColor("_EmissionColor", e);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetKeyword(new UnityEngine.Rendering.LocalKeyword(m.shader, "_EMISSION"), true);
            m.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(m);
            Mats[key] = m;
            return m;
        }

        static void Set(Object target, string prop, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogError($"[CircusMapBuilder] {target.GetType().Name}.{prop} not found"); return; }
            switch (value)
            {
                case Object o: p.objectReferenceValue = o; break;
                case float f: p.floatValue = f; break;
                case int i: p.intValue = i; break;
                case bool b: p.boolValue = b; break;
                case string s: p.stringValue = s; break;
                case Vector3 v: p.vector3Value = v; break;
                case IList list:
                    p.arraySize = list.Count;
                    for (int k = 0; k < list.Count; k++)
                    {
                        var el = p.GetArrayElementAtIndex(k);
                        switch (list[k])
                        {
                            case Object lo: el.objectReferenceValue = lo; break;
                            case string ls: el.stringValue = ls; break;
                            case float lf: el.floatValue = lf; break;
                            case Vector3 lv: el.vector3Value = lv; break;
                        }
                    }
                    break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static List<object> L(object o) => (List<object>)o;
        static float F(Dictionary<string, object> d, string k, float def = 0f) => d.TryGetValue(k, out var v) && v is double x ? (float)x : def;
        static Vector2 V2(object o) { var l = (List<object>)o; return new Vector2((float)(double)l[0], (float)(double)l[1]); }
    }
}
