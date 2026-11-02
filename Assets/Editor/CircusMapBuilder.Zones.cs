using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Circus.EditorTools
{
    /// <summary>Navigation and zone legibility: paper map (baked from the JSON), wall board, zone lights/sounds, ring signs.</summary>
    public static partial class CircusMapBuilder
    {
        const string MapTexPath = "Assets/Data/Map/CircusMap.png";
        const string SignFontPath = "Assets/Data/Fonts/OSD_SDF.asset";
        const int MapPpm = 12;

        // zone identity colors (also used to tint the paper map)
        static readonly Color TrailerWhite = new Color(0.80f, 0.92f, 1.00f);
        static readonly Color BoothMagenta = new Color(1.00f, 0.15f, 0.85f);
        static readonly Color ControlCyan = new Color(0.25f, 1.00f, 0.78f);
        static readonly Color CarouselYellow = new Color(1.00f, 0.80f, 0.22f);
        static readonly Color GeneratorRed = new Color(1.00f, 0.08f, 0.04f);
        static readonly Color TentWarm = new Color(1.00f, 0.88f, 0.65f);

        public static Color LampColor(string circuit) => circuit == "carousel" ? CarouselYellow : new Color(1f, 0.72f, 0.42f);

        public static Color RoomColor(string room) => room switch
        {
            "trailer" => TrailerWhite,
            "control_room" => ControlCyan,
            "generator_shed" => new Color(1f, 0.45f, 0.30f),
            _ => new Color(1f, 0.8f, 0.55f),
        };

        static Texture2D bakedMap;

        static void BuildZoneIdentity(Transform map)
        {
            EnsureFolder("Assets/Data/Map"); EnsureFolder("Assets/Data/Fonts");
            Emissive["tube_white"] = TrailerWhite * 3f;
            Emissive["neon_magenta"] = BoothMagenta * 4f;
            Emissive["screen_cyan"] = ControlCyan * 2f;
            Emissive["bulb_yellow"] = CarouselYellow * 3f;
            Emissive["bulb_yellow_off"] = CarouselYellow * 0.15f;
            Emissive["beacon_red"] = GeneratorRed * 4f;
            Emissive["marquee_on"] = TentWarm * 3.5f;
            Emissive["marquee_off"] = TentWarm * 0.1f;
            Palette["zebra_yellow"] = new Color(0.85f, 0.70f, 0.10f);
            Palette["zebra_black"] = new Color(0.04f, 0.04f, 0.04f);
            Palette["sign_board"] = new Color(0.07f, 0.06f, 0.05f);
            Palette["map_frame"] = new Color(0.20f, 0.13f, 0.08f);

            var root = Child(map, "ZoneIdentity");
            root.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

            BuildTrailerIdentity(root);
            BuildBoothIdentity(root);
            BuildControlRoomIdentity(root);
            BuildCarouselIdentity(root);
            BuildGeneratorIdentity(root);
            BuildTentIdentity(root);
            BuildSigns(root);

            bakedMap = BakeMapTexture();
            BuildMapBoard(root, bakedMap);
        }

        // ---------------------------------------------------------------- zones

        static void BuildTrailerIdentity(Transform root)
        {
            var z = Child(root, "Trailer");
            var tube = Box(z, "FluorescentTube", new Vector2(0, -22.1f), new Vector2(1.3f, 0.12f), 0.08f, "x", 2.45f);
            tube.GetComponent<Renderer>().sharedMaterial = EmissiveMat("tube_white");
            Object.DestroyImmediate(tube.GetComponent<Collider>());
            var l = PointLight(z, new Vector3(0, 2.3f, -21.5f), TrailerWhite, 8f, 7f);
            AddFx(l.gameObject, ZoneFx.Kind.Flicker, l, 0.3f, 1f);
            Halo(z, new Vector3(0, 2.6f, -21.4f), 0.7f, TrailerWhite, 1.4f);
            var roomLight = GameObject.Find("Map/Lights/trailer/Light");
            if (roomLight) AddFx(roomLight, ZoneFx.Kind.Flicker, roomLight.GetComponent<Light>(), 0.2f, 0.7f);
            Ambience(z, new Vector3(0, 2f, -24.5f), ZoneAmbience.Kind.FluorescentBuzz, 0.25f, 14f);
        }

        static void BuildBoothIdentity(Transform root)
        {
            var z = Child(root, "ZatanBooth");
            // neon frame along the top of the booth walls
            var c = new Vector2(9f, -19f);
            foreach (var (pos, size) in new[]
            {
                (new Vector2(9f, -17.35f), new Vector2(3.6f, 0.1f)), (new Vector2(9f, -20.65f), new Vector2(3.6f, 0.1f)),
                (new Vector2(10.65f, -19f), new Vector2(0.1f, 3.3f)), (new Vector2(7.35f, -19f), new Vector2(0.1f, 3.3f)),
            })
            {
                var n = Box(z, "Neon", pos, size, 0.08f, "x", 2.85f);
                n.GetComponent<Renderer>().sharedMaterial = EmissiveMat("neon_magenta");
                Object.DestroyImmediate(n.GetComponent<Collider>());
            }
            var sign = Box(z, "NeonSignBoard", new Vector2(7.2f, -19f), new Vector2(0.08f, 2.6f), 0.7f, "sign_board", 3.0f);
            Object.DestroyImmediate(sign.GetComponent<Collider>());
            var txt = WorldText(z, "ZATAN", new Vector3(7.12f, 3.35f, -19f), Quaternion.Euler(0, 90, 0), new Vector2(2.4f, 0.6f), BoothMagenta * 1.4f);
            txt.name = "NeonText";
            PointLight(z, new Vector3(6.6f, 2.8f, -19f), BoothMagenta, 10f, 10f);
            PointLight(z, new Vector3(9f, 2.3f, -19f), BoothMagenta, 4f, 3f);
            Halo(z, new Vector3(7.1f, 3.35f, -19f), 1.8f, BoothMagenta, 1.2f);
            Ambience(z, new Vector3(9f, 1.5f, -19f), ZoneAmbience.Kind.FortuneChimes, 0.45f, 22f);
        }

        static void BuildControlRoomIdentity(Transform root)
        {
            var z = Child(root, "ControlRoom");
            var inside = PointLight(z, new Vector3(-35.4f, 1.6f, 6f), ControlCyan, 9f, 6f);
            AddFx(inside.gameObject, ZoneFx.Kind.Pulse, inside, 0.3f, 1.7f);
            var spill = new GameObject("DoorSpill").AddComponent<Light>();
            spill.transform.SetParent(z, false);
            spill.transform.position = new Vector3(-27.4f, 2.0f, 6f);
            spill.transform.rotation = Quaternion.Euler(20f, 90f, 0f);
            spill.type = LightType.Spot; spill.spotAngle = 75f; spill.range = 13f; spill.intensity = 14f; spill.color = ControlCyan; spill.shadows = LightShadows.None;
            Halo(z, new Vector3(-27.1f, 1.4f, 6f), 1.1f, ControlCyan, 0.8f);
            // antenna outside the SW corner
            var mastPos = new Vector2(-37.9f, 1.9f);
            var mast = Cyl(z, "AntennaMast", mastPos, 0.07f, 9f, "post");
            Object.DestroyImmediate(mast.GetComponent<Collider>());
            for (int i = 0; i < 3; i++)
            {
                var bar = Box(z, "AntennaBar" + i, mastPos, new Vector2(1.6f - i * 0.4f, 0.05f), 0.05f, "post", 6.2f + i * 0.9f);
                Object.DestroyImmediate(bar.GetComponent<Collider>());
            }
            var tip = Halo(z, new Vector3(mastPos.x, 9.2f, mastPos.y), 1.2f, ControlCyan, 1.6f);
            var tipLight = PointLight(z, new Vector3(mastPos.x, 9.1f, mastPos.y), ControlCyan, 7f, 4f);
            AddFx(tipLight.gameObject, ZoneFx.Kind.Pulse, tipLight, 0.8f, 0.5f);
            Ambience(z, new Vector3(-35f, 1.5f, 6f), ZoneAmbience.Kind.MonitorWhine, 0.3f, 16f);
            _ = tip;
        }

        static void BuildCarouselIdentity(Transform root)
        {
            var z = Child(root, "Carousel");
            var c = V2(((Dictionary<string, object>)J["carousel"])["center"]);
            var rotorRoof = GameObject.Find("Map/Carousel/Rotor/Roof");
            float r = F((Dictionary<string, object>)J["carousel"], "platform_radius") + 0.45f;
            float y = F((Dictionary<string, object>)J["carousel"], "column_height") + 0.15f;
            var bulbs = new List<Renderer>();
            for (int i = 0; i < 20; i++)
            {
                float a = i * Mathf.PI * 2f / 20f;
                var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                b.name = "Bulb" + i;
                Object.DestroyImmediate(b.GetComponent<Collider>());
                b.transform.position = new Vector3(c.x + Mathf.Cos(a) * r, y, c.y + Mathf.Sin(a) * r);
                b.transform.localScale = Vector3.one * 0.22f;
                b.transform.SetParent(rotorRoof ? rotorRoof.transform.parent : z, true);
                b.GetComponent<Renderer>().sharedMaterial = EmissiveMat("bulb_yellow");
                bulbs.Add(b.GetComponent<Renderer>());
            }
            var fx = z.gameObject.AddComponent<ZoneFx>();
            fx.kind = ZoneFx.Kind.Chase; fx.speed = 1f; fx.bulbs = bulbs.ToArray();
            fx.bulbOn = EmissiveMat("bulb_yellow"); fx.bulbOff = EmissiveMat("bulb_yellow_off");
            PointLight(z, new Vector3(c.x, y + 0.6f, c.y), CarouselYellow, 12f, 7f);
            Halo(z, new Vector3(c.x, y + 0.8f, c.y), 2.6f, CarouselYellow, 1.1f);
        }

        static void BuildGeneratorIdentity(Transform root)
        {
            var z = Child(root, "Generator");
            // rotating red alarm light on a short mast above the shed door, high enough to clear the stalls
            var bpos = new Vector2(2.4f, 34.85f);
            const float by = 5.2f;
            var mast = Cyl(z, "BeaconMast", bpos, 0.06f, by - 3.0f, "post", 3.0f);
            Object.DestroyImmediate(mast.GetComponent<Collider>());
            var housing = Cyl(z, "BeaconHousing", bpos, 0.25f, 0.45f, "x", by - 0.2f);
            housing.GetComponent<Renderer>().sharedMaterial = EmissiveMat("beacon_red");
            Object.DestroyImmediate(housing.GetComponent<Collider>());
            var pivot = new GameObject("BeaconPivot").transform;
            pivot.SetParent(z, false);
            pivot.position = new Vector3(bpos.x, by, bpos.y);
            for (int i = 0; i < 2; i++)
            {
                var sl = new GameObject("Beam" + i).AddComponent<Light>();
                sl.transform.SetParent(pivot, false);
                sl.transform.localRotation = Quaternion.Euler(12f, i * 180f, 0f);
                sl.type = LightType.Spot; sl.spotAngle = 35f; sl.range = 30f; sl.intensity = 45f; sl.color = GeneratorRed; sl.shadows = LightShadows.None;
            }
            var spin = pivot.gameObject.AddComponent<ZoneFx>();
            spin.kind = ZoneFx.Kind.Spin; spin.spinSpeed = 220f;
            var gHalo = Halo(z, new Vector3(bpos.x, by + 0.05f, bpos.y), 1.6f, GeneratorRed, 1.8f);
            gHalo.name = "GeneratorBeacon";
            PointLight(z, new Vector3(0, 3.0f, 34.2f), GeneratorRed, 6f, 4f);

            // zebra stripes framing the door (D4: centre (0, 34.85), width 1.2)
            for (int side = -1; side <= 1; side += 2)
                for (int k = 0; k < 6; k++)
                {
                    var s = Box(z, $"Zebra{side}_{k}", new Vector2(side * 0.68f, 34.62f), new Vector2(0.14f, 0.16f), 0.37f, k % 2 == 0 ? "zebra_yellow" : "zebra_black", k * 0.37f);
                    Object.DestroyImmediate(s.GetComponent<Collider>());
                }
            for (int k = 0; k < 4; k++)
            {
                var s = Box(z, $"ZebraTop{k}", new Vector2(-0.6f + k * 0.4f + 0.2f, 34.62f), new Vector2(0.4f, 0.16f), 0.25f, k % 2 == 0 ? "zebra_black" : "zebra_yellow", 2.22f);
                Object.DestroyImmediate(s.GetComponent<Collider>());
            }
            var hum = Ambience(z, new Vector3(0, 1f, 39f), ZoneAmbience.Kind.GeneratorHum, 0.6f, 34f);
            hum.louderAfterMachine = "M3_generator";
        }

        static void BuildTentIdentity(Transform root)
        {
            var z = Child(root, "BigTop");
            var t = (Dictionary<string, object>)J["tent"];
            var c = V2(t["center"]); float r = F(t, "radius"), h = F(t, "height");
            var bulbs = new List<Renderer>();
            for (int i = 0; i < 36; i++)
            {
                float a = i * Mathf.PI * 2f / 36f;
                var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                b.name = "Marquee" + i;
                Object.DestroyImmediate(b.GetComponent<Collider>());
                b.transform.SetParent(z, false);
                b.transform.position = new Vector3(c.x + Mathf.Cos(a) * (r + 0.1f), h + 0.1f, c.y + Mathf.Sin(a) * (r + 0.1f));
                b.transform.localScale = Vector3.one * 0.4f;
                b.GetComponent<Renderer>().sharedMaterial = EmissiveMat("marquee_on");
                bulbs.Add(b.GetComponent<Renderer>());
            }
            var fx = z.gameObject.AddComponent<ZoneFx>();
            fx.kind = ZoneFx.Kind.Chase; fx.speed = 0.8f; fx.bulbs = bulbs.ToArray();
            fx.bulbOn = EmissiveMat("marquee_on"); fx.bulbOff = EmissiveMat("marquee_off");
            // the cap is lit from above, the pole top glows: visible over the stalls from anywhere
            PointLight(z, new Vector3(c.x, h + 4.5f, c.y), TentWarm, 14f, 30f);
            Halo(z, new Vector3(c.x, h + 9.5f, c.y), 2.6f, TentWarm, 1.6f);
            Ambience(z, new Vector3(c.x, 3f, c.y), ZoneAmbience.Kind.CanvasWind, 0.35f, 26f);
        }

        // ---------------------------------------------------------------- signs

        static void BuildSigns(Transform root)
        {
            var z = Child(root, "Signs");
            Sign(z, "Sign_W", new Vector2(-19.5f, 6f), 270f, "← SALA DE CONTROLE", 4.8f);
            Sign(z, "Sign_E", new Vector2(19.5f, 6f), 90f, "CARROSSEL →", 4.8f);
            Sign(z, "Sign_N", new Vector2(0f, 25.5f), 0f, "GERADOR ↑", 4.2f);
            Sign(z, "Sign_S", new Vector2(0f, -12.5f), 180f, "TRAILER ↓", 4.2f);
        }

        static void Sign(Transform parent, string name, Vector2 pos, float yaw, string text, float width)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.position = new Vector3(pos.x, 0, pos.y);
            root.rotation = Quaternion.Euler(0, yaw, 0);
            var board = Box(root, "Board", Vector2.zero, new Vector2(width, 0.08f), 0.85f, "sign_board", 2.95f, true);
            Object.DestroyImmediate(board.GetComponent<Collider>());
            for (int s = -1; s <= 1; s += 2)
            {
                var post = Box(root, "Post", new Vector2(s * (width / 2f + 0.05f), 0), new Vector2(0.1f, 0.1f), 3.9f, "post", 0, true);
                Object.DestroyImmediate(post.GetComponent<Collider>());
            }
            var t = WorldText(root, text, Vector3.zero, Quaternion.identity, new Vector2(width - 0.3f, 0.7f), new Color(1f, 0.93f, 0.75f));
            t.transform.localPosition = new Vector3(0, 3.37f, -0.06f);
            t.transform.localRotation = Quaternion.identity;
            var l = new GameObject("SignLight").AddComponent<Light>();
            l.transform.SetParent(root, false);
            l.transform.localPosition = new Vector3(0, 4.1f, -0.7f);
            l.type = LightType.Point; l.range = 4f; l.intensity = 4f; l.color = new Color(1f, 0.85f, 0.6f); l.shadows = LightShadows.None;
        }

        static TMP_FontAsset SignFont()
        {
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SignFontPath);
            if (fa) return fa;
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Data/Resources/Fonts/OSD.ttf");
            fa = TMP_FontAsset.CreateFontAsset(font, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
            fa.name = "OSD_SDF";
            AssetDatabase.CreateAsset(fa, SignFontPath);
            fa.atlasTextures[0].name = "OSD_SDF Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = "OSD_SDF Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            fa.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZÇÃÉÊÁÍÓÚÔ 0123456789←→↑↓-.:");
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        static TextMeshPro WorldText(Transform parent, string text, Vector3 pos, Quaternion rot, Vector2 size, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = rot;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = SignFont();
            tmp.text = text;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true; tmp.fontSizeMin = 0.5f; tmp.fontSizeMax = 12f;
            tmp.rectTransform.sizeDelta = size;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        // ---------------------------------------------------------------- map board

        static void BuildMapBoard(Transform root, Texture2D tex)
        {
            var z = Child(root, "TrailerMapBoard");
            float aspect = tex.width / (float)tex.height;
            float h = 0.95f, w = h * aspect;
            var center = new Vector3(-1.75f, 1.55f, -22.53f);   // inner face of the trailer north wall, left of the door
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(z, false);
            frame.transform.position = center + new Vector3(0, 0, 0.01f);
            frame.transform.localScale = new Vector3(w + 0.1f, h + 0.1f, 0.03f);
            frame.GetComponent<Renderer>().sharedMaterial = Mat("map_frame");
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Map";
            quad.transform.SetParent(z, false);
            quad.transform.position = center + new Vector3(0, 0, -0.012f);
            quad.transform.localScale = new Vector3(w, h, 1f);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.GetComponent<Renderer>().sharedMaterial = MapMaterial(tex);
            frame.AddComponent<MapBoard>();
            var l = PointLight(z, center + new Vector3(0, 0.9f, -0.5f), new Color(1f, 0.95f, 0.85f), 2.2f, 1.5f);
            l.name = "MapLamp";
        }

        static Material MapMaterial(Texture2D tex)
        {
            const string path = MatDir + "/map_board.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            m.SetTexture("_EmissionMap", tex);
            m.SetColor("_EmissionColor", new Color(0.35f, 0.33f, 0.3f));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetKeyword(new UnityEngine.Rendering.LocalKeyword(m.shader, "_EMISSION"), true);
            m.SetFloat("_Smoothness", 0.05f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void AddMapSystems(GameObject sys)
        {
            var ui = sys.AddComponent<CircusMapUI>();
            var g = (Dictionary<string, object>)((Dictionary<string, object>)J["meta"])["ground"];
            var gc = V2(g["center"]); var gs = V2(g["size"]);
            Set(ui, "mapTexture", bakedMap);
            var so = new SerializedObject(ui);
            so.FindProperty("worldMin").vector2Value = gc - gs / 2f;
            so.FindProperty("worldSize").vector2Value = gs;
            var labels = so.FindProperty("labels");
            var list = MapLabels();
            labels.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                labels.GetArrayElementAtIndex(i).FindPropertyRelative("text").stringValue = list[i].Item1;
                labels.GetArrayElementAtIndex(i).FindPropertyRelative("worldXZ").vector2Value = list[i].Item2;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static List<(string, Vector2)> MapLabels()
        {
            var rooms = RoomsById();
            Vector2 RC(string id) => V2(rooms[id]["center"]);
            var tent = V2(((Dictionary<string, object>)J["tent"])["center"]);
            var car = V2(((Dictionary<string, object>)J["carousel"])["center"]);
            return new List<(string, Vector2)>
            {
                ("TRAILER", RC("trailer") + new Vector2(0, -4.3f)),
                ("CABINE DO ZATAN", RC("zatan_booth") + new Vector2(4.5f, 3.4f)),
                ("PRAÇA", new Vector2(-4f, -17.5f)),
                ("ANEL", new Vector2(-9f, -8.3f)),
                ("LONA", tent),
                ("SALA DE CONTROLE", RC("control_room") + new Vector2(0, 6.3f)),
                ("CARROSSEL", car + new Vector2(0, -8.8f)),
                ("GERADOR", RC("generator_shed") + new Vector2(9f, 0)),
                ("FUNDOS NW", new Vector2(-31f, 33f)),
            };
        }

        // ---------------------------------------------------------------- paper map texture

        static Texture2D BakeMapTexture()
        {
            var g = (Dictionary<string, object>)((Dictionary<string, object>)J["meta"])["ground"];
            var gc = V2(g["center"]); var gs = V2(g["size"]);
            Vector2 min = gc - gs / 2f;
            int W = Mathf.RoundToInt(gs.x * MapPpm), H = Mathf.RoundToInt(gs.y * MapPpm);
            var px = new Color[W * H];
            var paper = new Color(0.86f, 0.81f, 0.68f);
            var ink = new Color(0.16f, 0.12f, 0.09f);
            var rng = new System.Random(42);

            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float stain = Mathf.PerlinNoise(x * 0.008f, y * 0.008f) - 0.5f;
                    float fiber = Mathf.PerlinNoise(x * 0.09f + 100, y * 0.02f) - 0.5f;
                    float grain = (float)rng.NextDouble() * 0.05f - 0.025f;
                    float ex = Mathf.Min(x, W - 1 - x) / (float)W, ey = Mathf.Min(y, H - 1 - y) / (float)H;
                    float edge = Mathf.Clamp01(Mathf.Min(ex, ey) * 12f);
                    float v = 1f + stain * 0.16f + fiber * 0.05f + grain;
                    var c = paper * v; c = Color.Lerp(c * 0.72f, c, edge); c.a = 1f;
                    px[y * W + x] = c;
                }

            int PX(float wx) => Mathf.RoundToInt((wx - min.x) * MapPpm);
            int PY(float wz) => Mathf.RoundToInt((wz - min.y) * MapPpm);
            void Blend(int x, int y, Color c, float a)
            {
                if (x < 0 || y < 0 || x >= W || y >= H) return;
                int i = y * W + x; var o = px[i]; px[i] = Color.Lerp(o, c, a); px[i].a = 1f;
            }
            void FillRect(Vector2 c0, Vector2 c1, Color c, float a)
            {
                for (int y = PY(c0.y); y < PY(c1.y); y++) for (int x = PX(c0.x); x < PX(c1.x); x++) Blend(x, y, c, a);
            }
            void Outline(Vector2 c0, Vector2 c1, Color c, int th, float a)
            {
                int x0 = PX(c0.x), x1 = PX(c1.x), y0 = PY(c0.y), y1 = PY(c1.y);
                for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
                    if (x - x0 < th || x1 - 1 - x < th || y - y0 < th || y1 - 1 - y < th) Blend(x, y, c, a);
            }
            void Disc(Vector2 cxz, float r, System.Func<float, float, (Color, float)> shade)
            {
                int cx = PX(cxz.x), cy = PY(cxz.y), rp = Mathf.RoundToInt(r * MapPpm);
                for (int y = cy - rp; y <= cy + rp; y++) for (int x = cx - rp; x <= cx + rp; x++)
                {
                    float dx = x - cx, dy = y - cy, d = Mathf.Sqrt(dx * dx + dy * dy) / rp;
                    if (d > 1f) continue;
                    var (c, a) = shade(d, Mathf.Atan2(dy, dx));
                    Blend(x, y, c, a);
                }
            }

            // faint survey grid every 10 m
            for (float gx = Mathf.Ceil(min.x / 10f) * 10f; gx < min.x + gs.x; gx += 10f)
                for (int y = 0; y < H; y++) Blend(PX(gx), y, ink, 0.07f);
            for (float gz = Mathf.Ceil(min.y / 10f) * 10f; gz < min.y + gs.y; gz += 10f)
                for (int x = 0; x < W; x++) Blend(x, PY(gz), ink, 0.07f);

            // zone tints (same hue as each zone's light)
            var rooms = RoomsById();
            foreach (var r in rooms.Values)
            {
                var c = V2(r["center"]); var s = V2(r["inner_size"]);
                string id = (string)r["id"];
                var tint = id == "zatan_booth" ? BoothMagenta : id == "generator_shed" ? GeneratorRed : id == "control_room" ? ControlCyan : TrailerWhite * 0.7f;
                FillRect(c - s / 2f, c + s / 2f, tint, 0.28f);
            }
            var car = (Dictionary<string, object>)J["carousel"];
            var cc = V2(car["center"]);
            Disc(cc, F(car, "fence_radius"), (d, a) => d > 0.96f && Mathf.Repeat(a * 10f, 1f) < 0.6f ? (ink, 0.9f) : (CarouselYellow, 0.10f));
            Disc(cc, F(car, "platform_radius"), (d, a) => d > 0.93f ? (ink, 0.9f) : (CarouselYellow, 0.35f));
            Disc(cc, F(car, "column_radius"), (d, a) => (ink, 0.85f));

            // blocks
            foreach (Dictionary<string, object> b in L(J["blocks"]))
            {
                var c = V2(b["center"]); var s = V2(b["size"]);
                var c0 = c - s / 2f; var c1 = c + s / 2f;
                switch ((string)b["type"])
                {
                    case "wall": FillRect(c0, c1, ink, 0.9f); break;
                    case "fence": FillRect(c0, c1, ink, 0.75f); break;
                    case "stall": FillRect(c0, c1, new Color(0.55f, 0.40f, 0.28f), 0.45f); Outline(c0, c1, ink, 2, 0.85f); break;
                    case "container": FillRect(c0, c1, new Color(0.30f, 0.38f, 0.42f), 0.35f); Outline(c0, c1, ink, 2, 0.8f); break;
                    case "crate": FillRect(c0, c1, ink, 0.25f); Outline(c0, c1, ink, 1, 0.8f); break;
                    default: Outline(c0, c1, ink, 1, 0.7f); break;
                }
            }
            foreach (Dictionary<string, object> d in L(J["doors"]))
            {
                var c = V2(d["center"]); float w = F(d, "width");
                bool alongX = (string)d["wall_axis"] == "x";
                var half = alongX ? new Vector2(w / 2f, 0.12f) : new Vector2(0.12f, w / 2f);
                FillRect(c - half, c + half, new Color(0.55f, 0.30f, 0.15f), 0.8f);
            }

            // big top with stripes
            var tent = (Dictionary<string, object>)J["tent"];
            Disc(V2(tent["center"]), F(tent, "radius"), (d, a) =>
                d > 0.97f ? (ink, 0.9f) : Mathf.Repeat(a / (Mathf.PI * 2f) * 16f, 1f) < 0.5f ? (new Color(0.62f, 0.25f, 0.20f), 0.55f) : (paper, 0.0f));

            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(MapTexPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(MapTexPath, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(MapTexPath);
            imp.mipmapEnabled = false; imp.wrapMode = TextureWrapMode.Clamp; imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed; imp.maxTextureSize = 2048; imp.npotScale = TextureImporterNPOTScale.None;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexPath);
        }

        // ---------------------------------------------------------------- helpers

        static Light PointLight(Transform parent, Vector3 pos, Color color, float range, float intensity)
        {
            var l = new GameObject("ZoneLight").AddComponent<Light>();
            l.transform.SetParent(parent, false);
            l.transform.position = pos;
            l.type = LightType.Point; l.range = range; l.intensity = intensity; l.color = color; l.shadows = LightShadows.None;
            return l;
        }

        static GameObject Halo(Transform parent, Vector3 pos, float radius, Color color, float intensity)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Halo";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * radius * 2f;
            var r = go.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            string key = $"halo_{ColorUtility.ToHtmlStringRGB(color)}_{intensity:0.0}".Replace('.', '_');
            string path = $"{MatDir}/{key}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/Halo.shader")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_Color", color); m.SetFloat("_Intensity", intensity); m.SetFloat("_Falloff", 3.5f); m.SetFloat("_FogScale", 0.35f);
            EditorUtility.SetDirty(m);
            r.sharedMaterial = m;
            return go;
        }

        static ZoneAmbience Ambience(Transform parent, Vector3 pos, ZoneAmbience.Kind kind, float volume, float maxDistance)
        {
            var go = new GameObject("Ambience_" + kind);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            var a = go.AddComponent<ZoneAmbience>();
            a.kind = kind; a.volume = volume; a.maxDistance = maxDistance;
            return a;
        }

        static void AddFx(GameObject go, ZoneFx.Kind kind, Light l, float amount, float speed)
        {
            var fx = go.AddComponent<ZoneFx>();
            fx.kind = kind; fx.targetLight = l; fx.amount = amount; fx.speed = speed;
        }
    }
}
