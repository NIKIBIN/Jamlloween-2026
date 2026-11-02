using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Gera o prefab do painel de conserto do Zatan (Assets/Prefabs/Minigames/ZatanRepairPanel.prefab) e seus
/// materiais. Menu: Circus > Build Zatan Repair Panel Prefab. Pode ser rodado de novo: sobrescreve o prefab.
/// Tudo e feito de primitivas, entao o visual e facil de trocar depois sem mexer na logica.
/// O prefab fica olhando para +Z (frente do painel em -Z). Coloque-o nas costas do Zatan.
/// </summary>
public static class ZatanRepairPanelBuilder
{
    const string PrefabPath = "Assets/Prefabs/Minigames/ZatanRepairPanel.prefab";
    const string MatDir = "Assets/Materials/Minigames/ZatanRepair";
    const string FontPath = "Assets/Data/Fonts/OSD_SDF.asset";

    static TMP_FontAsset font;

    [MenuItem("Circus/Build Zatan Repair Panel Prefab")]
    public static void Build()
    {
        Directory.CreateDirectory(MatDir);
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        // ---------- materiais ----------
        var tex = Blob("TarBlob", 3, 0.82f, 0.14f);
        var puff = Puff("SmokePuff");
        var paint = Mat("ZatanPaint", new Color(0.66f, 0.71f, 0.6f), 0.25f, 0.1f);          // fundo: metal pintado gasto, bege esverdeado
        var bezel = Mat("ZatanBezel", new Color(0.3f, 0.36f, 0.3f), 0.3f, 0.3f);
        var cover = Mat("ZatanCover", new Color(0.24f, 0.4f, 0.3f), 0.3f, 0.3f);
        var darkSteel = Mat("ZatanDarkSteel", new Color(0.22f, 0.24f, 0.28f), 0.45f, 0.7f);
        var chrome = Mat("ZatanChrome", new Color(0.85f, 0.87f, 0.9f), 0.8f, 0.9f);
        var orange = Mat("ZatanOrange", new Color(0.92f, 0.5f, 0.1f), 0.5f, 0.3f);
        var navy = Mat("ZatanNavy", new Color(0.1f, 0.18f, 0.38f), 0.4f, 0.3f);
        var blue = Mat("ZatanBlue", new Color(0.18f, 0.45f, 0.8f), 0.5f, 0.4f);
        var red = Mat("ZatanRed", new Color(0.8f, 0.14f, 0.1f), 0.45f, 0.2f);
        var yellow = Mat("ZatanYellow", new Color(0.95f, 0.78f, 0.15f), 0.45f, 0.2f);
        var teal = Mat("ZatanTeal", new Color(0.15f, 0.58f, 0.58f), 0.5f, 0.4f);
        var cream = Mat("ZatanCream", new Color(0.95f, 0.92f, 0.78f), 0.1f);
        var white = Mat("ZatanWhite", new Color(0.97f, 0.97f, 0.95f), 0.2f);
        var ink = Mat("ZatanInk", new Color(0.04f, 0.04f, 0.05f), 0.15f);
        var cloth = Mat("ZatanCloth", new Color(0.93f, 0.9f, 0.82f), 0.05f);
        var clothShade = Mat("ZatanClothShade", new Color(0.72f, 0.68f, 0.6f), 0.05f);
        var plate = Mat("ZatanPlate", new Color(0.16f, 0.17f, 0.19f), 0.3f, 0.5f);
        var lampOff = Mat("ZatanLampOff", new Color(0.12f, 0.1f, 0.09f), 0.8f);
        var lampAmber = Mat("ZatanLampAmber", new Color(0.9f, 0.5f, 0.1f), 0.8f, 0f, new Color(2.5f, 1.2f, 0.2f));
        var lampGreen = Mat("ZatanLampGreen", new Color(0.2f, 0.8f, 0.3f), 0.8f, 0f, new Color(0.3f, 2.2f, 0.5f));
        var lampRed = Mat("ZatanLampRed", new Color(0.9f, 0.15f, 0.1f), 0.8f, 0f, new Color(2.5f, 0.2f, 0.1f));
        var hoseMat = Unlit("ZatanHose", new Color(0.03f, 0.03f, 0.035f));
        var tarDecal = Transparent(Mat("ZatanTarDecal", new Color(0.008f, 0.008f, 0.01f, 1f), 0f, 0f), tex);
        var shineMat = Transparent(Unlit("ZatanShine", Color.white), puff);
        var smokeMat = Transparent(ParticleMat("ZatanSmoke", new Color(0.12f, 0.12f, 0.12f, 0.7f)), puff);
        var steamMat = Transparent(ParticleMat("ZatanSteam", new Color(1f, 1f, 1f, 0.55f)), puff);

        // ---------- raiz ----------
        // Convencao: cada peca e um GameObject com a logica (posicao, rotacao, colisor, componentes) e um filho
        // "Visual" com o desenho. Para trocar por um modelo final, apague o Visual e ponha o modelo no lugar.
        var root = new GameObject("ZatanRepairPanel");
        var game = root.AddComponent<ZatanRepairMinigame>();
        var audio = root.AddComponent<ZatanRepairAudio>();
        Transform R = root.transform;

        var view = Empty("ViewPoint", R, new Vector3(0f, 0.07f, -0.53f));
        view.localRotation = Quaternion.Euler(9f, 0f, 0f);
        var light = new GameObject("WorkLight").AddComponent<Light>();
        light.transform.SetParent(R, false);
        light.transform.localPosition = new Vector3(0f, 0.15f, -0.4f);
        light.type = LightType.Point;
        light.color = new Color(1f, 0.9f, 0.75f);
        light.intensity = 1.1f;
        light.range = 1.6f;

        // ---------- caixa ----------
        var boxVis = Vis(Piece("Box", R, Vector3.zero));
        var back = Prim(PrimitiveType.Cube, "Backplate", boxVis, new Vector3(0f, 0f, 0.01f), new Vector3(0.8f, 0.56f, 0.02f), paint, true);
        Prim(PrimitiveType.Cube, "Bezel", boxVis, new Vector3(0f, 0f, 0.03f), new Vector3(0.9f, 0.66f, 0.02f), bezel);
        // as bordas elevadas dao profundidade ao fundo
        Prim(PrimitiveType.Cube, "RimTop", boxVis, new Vector3(0f, 0.285f, -0.01f), new Vector3(0.84f, 0.03f, 0.04f), bezel);
        Prim(PrimitiveType.Cube, "RimBottom", boxVis, new Vector3(0f, -0.285f, -0.01f), new Vector3(0.84f, 0.03f, 0.04f), bezel);
        Prim(PrimitiveType.Cube, "RimLeft", boxVis, new Vector3(-0.415f, 0f, -0.01f), new Vector3(0.03f, 0.6f, 0.04f), bezel);
        Prim(PrimitiveType.Cube, "RimRight", boxVis, new Vector3(0.415f, 0f, -0.01f), new Vector3(0.03f, 0.6f, 0.04f), bezel);

        var sideL = Vis(Piece("SideLeft", R, new Vector3(-0.47f, 0f, 0.0f)));
        Prim(PrimitiveType.Cube, "Plate", sideL, Vector3.zero, new Vector3(0.09f, 0.42f, 0.014f), plate);
        Prim(PrimitiveType.Cube, "Hook", sideL, new Vector3(0f, 0.08f, -0.014f), new Vector3(0.014f, 0.045f, 0.02f), chrome);
        var sideR = Vis(Piece("SideRight", R, new Vector3(0.47f, 0f, 0.0f)));
        Prim(PrimitiveType.Cube, "Plate", sideR, Vector3.zero, new Vector3(0.09f, 0.42f, 0.014f), plate);
        Prim(PrimitiveType.Cube, "Clip", sideR, new Vector3(0f, 0.09f, -0.014f), new Vector3(0.05f, 0.016f, 0.02f), chrome);

        // ---------- mostrador ----------
        Vector3 dialC = new Vector3(-0.22f, 0.10f, 0f);
        var dialVis = Vis(Piece("Dial", R, dialC));
        Disc("Rim", dialVis, new Vector3(0f, 0f, -0.012f), 0.105f, 0.03f, navy);
        Disc("Face", dialVis, new Vector3(0f, 0f, -0.024f), 0.09f, 0.006f, cream);
        for (int p = 0; p <= 180; p += 10)
        {
            float a = (180f - p) * Mathf.Deg2Rad;
            bool major = p % 30 == 0;
            float len = major ? 0.018f : 0.01f;
            var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            var t = Prim(PrimitiveType.Cube, "Tick" + p, dialVis, dir * (0.082f - len * 0.5f) + new Vector3(0f, 0f, -0.028f),
                new Vector3(major ? 0.004f : 0.003f, len, 0.002f), ink);
            t.transform.localRotation = Quaternion.Euler(0f, 0f, 180f - p - 90f);
        }
        foreach (int p in new[] { 30, 60, 90, 120, 150 })
        {
            float a = (180f - p) * Mathf.Deg2Rad;
            Text("Num" + p, dialVis, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.056f + new Vector3(0f, 0f, -0.03f), p.ToString(), 0.25f, new Color(0.08f, 0.07f, 0.06f), 0f);
        }
        // O "0" impresso (esquerda, 0 graus reais) foi riscado; o "0" refeito a mao fica onde na verdade sao 180 graus.
        Text("PrintedZero", dialVis, new Vector3(-0.056f, 0f, -0.03f), "0", 0.25f, new Color(0.08f, 0.07f, 0.06f), 0f);
        var s1 = Prim(PrimitiveType.Cube, "Strike1", dialVis, new Vector3(-0.056f, 0f, -0.032f), new Vector3(0.028f, 0.004f, 0.002f), red);
        s1.transform.localRotation = Quaternion.Euler(0f, 0f, 38f);
        var s2 = Prim(PrimitiveType.Cube, "Strike2", dialVis, new Vector3(-0.056f, 0f, -0.032f), new Vector3(0.028f, 0.004f, 0.002f), red);
        s2.transform.localRotation = Quaternion.Euler(0f, 0f, -52f);
        Text("HandZero", dialVis, new Vector3(0.058f, 0.002f, -0.031f), "0", 0.5f, new Color(0.75f, 0.08f, 0.06f), -14f, FontStyles.Italic);

        var pointer = Piece("Pointer", R, dialC + new Vector3(0f, 0f, -0.037f));
        pointer.localRotation = Quaternion.Euler(0f, 0f, 180f);
        var pv = Vis(pointer);
        Prim(PrimitiveType.Cube, "Needle", pv, new Vector3(0.036f, 0f, 0f), new Vector3(0.076f, 0.006f, 0.004f), red);
        Disc("Hub", pv, Vector3.zero, 0.011f, 0.008f, chrome);

        // ---------- engrenagem do pescoco ----------
        Vector3 gearC = new Vector3(0.04f, 0.10f, 0f);
        var housingVis = Vis(Piece("GearHousing", R, gearC));
        Disc("Housing", housingVis, new Vector3(0f, 0f, -0.012f), 0.105f, 0.03f, darkSteel);
        var gear = Piece("Gear", R, gearC + new Vector3(0f, 0f, -0.032f));
        var gv = Vis(gear);
        Disc("GearDisc", gv, Vector3.zero, 0.068f, 0.014f, orange);
        for (int i = 0; i < 12; i++)
        {
            var tooth = Prim(PrimitiveType.Cube, "Tooth" + i, gv, Quaternion.Euler(0f, 0f, i * 30f) * new Vector3(0.076f, 0f, 0f), new Vector3(0.024f, 0.02f, 0.014f), orange);
            tooth.transform.localRotation = Quaternion.Euler(0f, 0f, i * 30f);
        }
        Prim(PrimitiveType.Cube, "Axle", gv, new Vector3(0f, 0f, -0.026f), new Vector3(0.03f, 0.03f, 0.04f), chrome);
        var axleSlot = Empty("AxleSlot", R, gearC + new Vector3(0f, 0f, -0.085f));

        // ---------- tubo, valvula e juntas ----------
        var pipeVis = Vis(Piece("Pipe", R, new Vector3(0.07f, -0.06f, -0.02f)));
        Cyl("PipeBody", pipeVis, Vector3.zero, 0.02f, 0.46f, new Vector3(0f, 0f, 90f), blue);
        var valveVis = Vis(Piece("Valve", R, new Vector3(-0.16f, -0.06f, -0.02f)));
        Prim(PrimitiveType.Cube, "ValveBody", valveVis, Vector3.zero, new Vector3(0.07f, 0.07f, 0.04f), red);
        var nozzle = Vis(Piece("ValveNozzle", R, new Vector3(-0.16f, -0.06f, -0.045f)));
        var ring = Disc("NozzleRing", nozzle, Vector3.zero, 0.032f, 0.012f, yellow);
        var hole = Disc("NozzleHole", nozzle, new Vector3(0f, 0f, -0.007f), 0.018f, 0.006f, ink);
        var socket = Empty("ValveSocket", R, new Vector3(-0.16f, -0.06f, -0.065f));
        var flA = Vis(Piece("FlangeA", R, new Vector3(0.0f, -0.06f, -0.02f)));
        Cyl("Flange", flA, Vector3.zero, 0.036f, 0.02f, new Vector3(0f, 0f, 90f), yellow);
        var flB = Vis(Piece("FlangeB", R, new Vector3(0.16f, -0.06f, -0.02f)));
        Cyl("Flange", flB, Vector3.zero, 0.036f, 0.02f, new Vector3(0f, 0f, 90f), yellow);

        // ---------- pistao e manometro (so visual, movidos pela respiracao) ----------
        var pistHousing = Vis(Piece("PistonHousing", R, new Vector3(0.33f, -0.15f, -0.03f)));
        Cyl("Cylinder", pistHousing, Vector3.zero, 0.045f, 0.16f, Vector3.zero, teal);
        Cyl("CapBottom", pistHousing, new Vector3(0f, -0.085f, 0f), 0.052f, 0.015f, Vector3.zero, darkSteel);
        Cyl("CapTop", pistHousing, new Vector3(0f, 0.085f, 0f), 0.052f, 0.015f, Vector3.zero, darkSteel);
        var rod = Piece("PistonRod", R, new Vector3(0.33f, -0.1f, -0.05f));
        var rodVis = Vis(rod);
        Cyl("Rod", rodVis, new Vector3(0f, 0.05f, 0f), 0.011f, 0.1f, Vector3.zero, chrome);
        Disc("Head", rodVis, new Vector3(0f, 0.105f, 0f), 0.038f, 0.02f, orange);

        var gaugeVis = Vis(Piece("Gauge", R, new Vector3(0.04f, -0.2f, 0f)));
        Disc("Bezel", gaugeVis, new Vector3(0f, 0f, -0.012f), 0.056f, 0.03f, ink);
        Disc("Face", gaugeVis, new Vector3(0f, 0f, -0.028f), 0.047f, 0.004f, white);
        for (int i = 0; i <= 8; i++)
        {
            float ang = Mathf.Lerp(125f, -125f, i / 8f) * Mathf.Deg2Rad;
            var gt = Prim(PrimitiveType.Cube, "GTick" + i, gaugeVis, new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * 0.037f + new Vector3(0f, 0f, -0.031f),
                new Vector3(0.003f, 0.012f, 0.002f), i >= 7 ? red : ink);
            gt.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(125f, -125f, i / 8f) - 90f);
        }
        var needleRoot = Piece("GaugeNeedle", R, new Vector3(0.04f, -0.2f, -0.036f));
        needleRoot.localRotation = Quaternion.Euler(0f, 0f, 125f);
        var nv = Vis(needleRoot);
        Prim(PrimitiveType.Cube, "Needle", nv, new Vector3(0.019f, 0f, 0f), new Vector3(0.042f, 0.004f, 0.003f), ink);
        Disc("Hub", nv, Vector3.zero, 0.006f, 0.006f, red);

        // ---------- mangueira ----------
        var portVis = Vis(Piece("HosePort", R, new Vector3(-0.37f, -0.14f, -0.02f)));
        Disc("Port", portVis, Vector3.zero, 0.026f, 0.03f, darkSteel);
        var hoseAnchor = Empty("HoseAnchor", R, new Vector3(-0.37f, -0.14f, -0.045f));
        var hoseRest = Empty("HoseRest", R, new Vector3(-0.27f, -0.2f, -0.06f));
        var hoseGo = new GameObject("HoseLine");
        hoseGo.transform.SetParent(R, false);
        var line = hoseGo.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 16;
        line.startWidth = line.endWidth = 0.03f;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.sharedMaterial = hoseMat;
        line.shadowCastingMode = ShadowCastingMode.Off;
        var hoseGrip = Piece("HoseGrip", R, hoseRest.localPosition);
        var hgv = Vis(hoseGrip);
        Disc("Coupling", hgv, Vector3.zero, 0.024f, 0.06f, chrome);
        Disc("CouplingRing", hgv, new Vector3(0f, 0f, 0.016f), 0.03f, 0.01f, red);
        AddPart(hoseGrip.gameObject, ZatanRepairPart.Kind.Hose, new Vector3(0.07f, 0.07f, 0.07f), Vector3.zero);

        // ---------- chave geral ----------
        Vector3 swC = new Vector3(0.30f, 0.15f, 0f);
        var swPlateRoot = Vis(Piece("SwitchPlate", R, swC + new Vector3(0f, 0f, -0.004f)));
        var swPlate = Prim(PrimitiveType.Cube, "Plate", swPlateRoot, Vector3.zero, new Vector3(0.085f, 0.15f, 0.02f), plate);
        Text("On", swPlateRoot, new Vector3(0f, 0.055f, -0.012f), "ON", 0.22f, Color.white, 0f);
        Text("Off", swPlateRoot, new Vector3(0f, -0.055f, -0.012f), "OFF", 0.22f, Color.white, 0f);
        var swPivot = Piece("SwitchLever", R, swC + new Vector3(0f, 0f, -0.026f));
        swPivot.localRotation = Quaternion.Euler(0f, 0f, 180f);
        var slv = Vis(swPivot);
        var leverBar = Prim(PrimitiveType.Cylinder, "LeverBar", slv, new Vector3(0f, 0.03f, 0f), new Vector3(0.014f, 0.03f, 0.014f), chrome);
        var leverTip = Prim(PrimitiveType.Sphere, "LeverTip", slv, new Vector3(0f, 0.062f, 0f), Vector3.one * 0.028f, yellow);
        var swHit = Empty("SwitchHit", R, swC + new Vector3(0f, 0f, -0.03f));
        var swPart = AddPart(swHit.gameObject, ZatanRepairPart.Kind.Switch, new Vector3(0.1f, 0.17f, 0.06f), Vector3.zero);

        // ---------- luzes do painel ----------
        var lamps = new Renderer[3];
        for (int i = 0; i < 3; i++)
        {
            var lv = Vis(Piece("Lamp" + i, R, new Vector3(-0.05f + 0.05f * i, 0.25f, -0.012f)));
            lamps[i] = Disc("Bulb", lv, Vector3.zero, 0.014f, 0.02f, lampOff).GetComponent<Renderer>();
        }

        // ---------- camadas de piche (esfregar) ----------
        var dialTar = ScrubArea("DialTar", R, dialC + new Vector3(0f, 0f, -0.06f), 0.27f, 0.105f, tarDecal, shineMat, true);
        var gearTar = ScrubArea("GearTar", R, gearC + new Vector3(0f, 0f, -0.1f), 0.27f, 0.105f, tarDecal, shineMat, true);
        var jointA = ScrubArea("JointTarA", R, new Vector3(0.0f, -0.06f, -0.07f), 0.13f, 0.055f, tarDecal, shineMat, false);
        var jointB = ScrubArea("JointTarB", R, new Vector3(0.16f, -0.06f, -0.07f), 0.22f, 0.085f, tarDecal, shineMat, false);
        var smoke = Smoke(jointB.transform, smokeMat);

        // ---------- vapor nas juntas ----------
        var steam = new[]
        {
            Steam(R, new Vector3(0.0f, -0.025f, -0.05f), steamMat),
            Steam(R, new Vector3(0.16f, -0.025f, -0.05f), steamMat),
            Steam(R, new Vector3(-0.16f, -0.025f, -0.06f), steamMat),
            Steam(R, new Vector3(0.33f, -0.07f, -0.06f), steamMat),
        };

        // ---------- estopa e manivela ----------
        var ragHome = Empty("RagHome", R, new Vector3(-0.47f, 0.0f, -0.03f));
        var ragGo = Piece("Rag", R, ragHome.localPosition);
        var rv = Vis(ragGo);
        var r1 = Prim(PrimitiveType.Cube, "Cloth1", rv, Vector3.zero, new Vector3(0.09f, 0.075f, 0.016f), cloth);
        r1.transform.localRotation = Quaternion.Euler(0f, 0f, 6f);
        var r2 = Prim(PrimitiveType.Cube, "Cloth2", rv, new Vector3(0.008f, -0.006f, -0.01f), new Vector3(0.075f, 0.06f, 0.014f), clothShade);
        r2.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
        var r3 = Prim(PrimitiveType.Cube, "Cloth3", rv, new Vector3(-0.006f, 0.006f, -0.019f), new Vector3(0.06f, 0.045f, 0.012f), cloth);
        r3.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
        Prim(PrimitiveType.Cube, "Stain", rv, new Vector3(0.012f, -0.008f, -0.026f), new Vector3(0.022f, 0.016f, 0.002f), ink).transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
        AddPart(ragGo.gameObject, ZatanRepairPart.Kind.Rag, new Vector3(0.1f, 0.09f, 0.06f), Vector3.zero);

        var crankHome = Empty("CrankHome", R, new Vector3(0.47f, 0.05f, -0.03f));
        crankHome.localRotation = Quaternion.Euler(0f, 0f, -90f);
        var crankGo = Piece("Crank", R, crankHome.localPosition);
        crankGo.localRotation = crankHome.localRotation;
        var cv = Vis(crankGo);
        Disc("Hub", cv, Vector3.zero, 0.02f, 0.03f, chrome);
        Prim(PrimitiveType.Cube, "Arm", cv, new Vector3(0.06f, 0f, -0.004f), new Vector3(0.12f, 0.018f, 0.014f), darkSteel);
        Cyl("Handle", cv, new Vector3(0.12f, 0f, -0.03f), 0.014f, 0.05f, new Vector3(90f, 0f, 0f), cream);   // cabo claro
        AddPart(crankGo.gameObject, ZatanRepairPart.Kind.Crank, new Vector3(0.16f, 0.05f, 0.07f), new Vector3(0.06f, 0f, -0.015f));

        // ---------- tampa ----------
        var coverAnchor = Empty("CoverAnchor", R, new Vector3(0f, 0f, -0.12f));
        var coverRest = Empty("CoverRest", R, new Vector3(0f, -0.55f, -0.16f));
        coverRest.localRotation = Quaternion.Euler(0f, 0f, 6f);
        var coverGo = Piece("Cover", R, coverAnchor.localPosition);
        var cov = Vis(coverGo);
        Prim(PrimitiveType.Cube, "Plate", cov, Vector3.zero, new Vector3(0.84f, 0.6f, 0.014f), cover);
        Prim(PrimitiveType.Cube, "Handle", cov, new Vector3(0f, 0f, -0.03f), new Vector3(0.26f, 0.04f, 0.024f), yellow);
        Prim(PrimitiveType.Cube, "PostL", cov, new Vector3(-0.11f, 0f, -0.015f), new Vector3(0.022f, 0.034f, 0.018f), yellow);
        Prim(PrimitiveType.Cube, "PostR", cov, new Vector3(0.11f, 0f, -0.015f), new Vector3(0.022f, 0.034f, 0.018f), yellow);
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
                Disc("Latch", cov, new Vector3(0.37f * sx, 0.25f * sy, -0.014f), 0.018f, 0.014f, chrome);
        AddPart(coverGo.gameObject, ZatanRepairPart.Kind.Cover, new Vector3(0.84f, 0.6f, 0.07f), new Vector3(0f, 0f, -0.01f));

        // ---------- ligacoes ----------
        var so = new SerializedObject(game);
        Ref(so, "prompt", null, "Consertar Zatan");
        Ref(so, "viewPoint", view);
        Refs(so, "activeWhilePlaying", light.gameObject);
        Ref(so, "cover", coverGo.GetComponent<ZatanRepairPart>());
        Ref(so, "coverAnchor", coverAnchor);
        Ref(so, "coverRest", coverRest);
        Ref(so, "rag", ragGo.GetComponent<ZatanRepairPart>());
        Ref(so, "ragHome", ragHome);
        Ref(so, "dialTar", dialTar);
        Ref(so, "gearTar", gearTar);
        Ref(so, "hose", hoseGrip.GetComponent<ZatanRepairPart>());
        Ref(so, "hoseLine", line);
        Ref(so, "hoseAnchor", hoseAnchor);
        Ref(so, "hoseRest", hoseRest);
        Ref(so, "valveSocket", socket);
        Refs(so, "socketMarker", ring.GetComponent<Renderer>(), hole.GetComponent<Renderer>());
        Ref(so, "mainSwitch", swPart);
        Ref(so, "switchLever", swPivot);
        Refs(so, "lamps", lamps);
        Refs(so, "lampOnMaterials", lampAmber, lampGreen, lampRed);
        Ref(so, "lampOffMaterial", lampOff);
        Ref(so, "pointer", pointer);
        Ref(so, "gearVisual", gear);
        Ref(so, "pistonRod", rod);
        Ref(so, "gaugeNeedle", needleRoot);
        Refs(so, "steamJets", steam);
        Ref(so, "crank", crankGo.GetComponent<ZatanRepairPart>());
        Ref(so, "crankHome", crankHome);
        Ref(so, "axleSlot", axleSlot);
        Ref(so, "repairAudio", audio);
        so.FindProperty("hoverColor").colorValue = new Color(1f, 0.85f, 0.2f);
        var locks = so.FindProperty("locks");
        locks.arraySize = 2;
        SetLock(locks.GetArrayElementAtIndex(0), 60f, 0.25f, jointA, null);
        SetLock(locks.GetArrayElementAtIndex(1), 120f, 0.9f, jointB, smoke);
        so.ApplyModifiedPropertiesWithoutUndo();

        // O interruptor e uma peca sem desenho proprio: realca a placa e a alavanca.
        var swSo = new SerializedObject(swPart);
        var targets = swSo.FindProperty("highlightTargets");
        targets.arraySize = 3;
        targets.GetArrayElementAtIndex(0).objectReferenceValue = swPlate.GetComponent<Renderer>();
        targets.GetArrayElementAtIndex(1).objectReferenceValue = leverBar.GetComponent<Renderer>();
        targets.GetArrayElementAtIndex(2).objectReferenceValue = leverTip.GetComponent<Renderer>();
        swSo.ApplyModifiedPropertiesWithoutUndo();

        // Detalhes pequenos nao projetam sombra (dava listras no mostrador).
        foreach (var n in new[] { "Dial", "Pointer", "Gauge", "GaugeNeedle", "Lamp0", "Lamp1", "Lamp2", "SwitchPlate", "SwitchLever", "ValveNozzle" })
            foreach (var r in R.Find(n).GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ZatanRepairPanelBuilder] prefab gerado em " + PrefabPath);
    }

    // =================================================================
    // Construcao
    // =================================================================

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider = false)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    /// <summary>Disco voltado para a camera (eixo ao longo de Z).</summary>
    static GameObject Disc(string name, Transform parent, Vector3 pos, float radius, float thickness, Material mat)
    {
        var go = Prim(PrimitiveType.Cylinder, name, parent, pos, new Vector3(radius * 2f, thickness * 0.5f, radius * 2f), mat);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        return go;
    }

    static GameObject Cyl(string name, Transform parent, Vector3 pos, float radius, float length, Vector3 euler, Material mat)
    {
        var go = Prim(PrimitiveType.Cylinder, name, parent, pos, new Vector3(radius * 2f, length * 0.5f, radius * 2f), mat);
        go.transform.localRotation = Quaternion.Euler(euler);
        return go;
    }

    static Transform Empty(string name, Transform parent, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        return go.transform;
    }

    /// <summary>Raiz de uma peca: logica aqui, desenho no filho Visual.</summary>
    static Transform Piece(string name, Transform parent, Vector3 pos) => Empty(name, parent, pos);

    static Transform Vis(Transform piece)
    {
        var v = new GameObject("Visual");
        v.transform.SetParent(piece, false);
        return v.transform;
    }

    static ZatanRepairPart AddPart(GameObject go, ZatanRepairPart.Kind kind, Vector3 colliderSize, Vector3 colliderCenter)
    {
        var box = go.AddComponent<BoxCollider>();
        box.size = colliderSize;
        box.center = colliderCenter;
        var part = go.AddComponent<ZatanRepairPart>();
        part.kind = kind;
        return part;
    }

    static ZatanScrubArea ScrubArea(string name, Transform parent, Vector3 pos, float decalSize, float radius, Material decalMat, Material shineMat, bool startActive)
    {
        var area = Empty(name, parent, pos);
        var quad = Prim(PrimitiveType.Quad, "TarDecal", area, Vector3.zero, new Vector3(decalSize, decalSize, 1f), decalMat);
        quad.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        var shine = Prim(PrimitiveType.Quad, "Shine", area, new Vector3(-decalSize * 0.18f, decalSize * 0.18f, -0.002f), new Vector3(decalSize * 0.3f, decalSize * 0.18f, 1f), shineMat);
        shine.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
        shine.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        var comp = area.gameObject.AddComponent<ZatanScrubArea>();
        var so = new SerializedObject(comp);
        so.FindProperty("decal").objectReferenceValue = quad.GetComponent<Renderer>();
        so.FindProperty("shine").objectReferenceValue = shine.GetComponent<Renderer>();
        so.FindProperty("radius").floatValue = radius;
        so.FindProperty("startActive").boolValue = startActive;
        so.ApplyModifiedPropertiesWithoutUndo();
        return comp;
    }

    static ParticleSystem Smoke(Transform parent, Material mat)
    {
        var go = new GameObject("Smoke");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // emite para a frente do painel (-Z)
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 2f;
        main.startLifetime = 1.8f;
        main.startSpeed = 0.05f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
        main.startColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);
        main.gravityModifier = -0.012f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission;
        em.rateOverTime = 14f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.012f;
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.2f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 2.2f));
        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = mat;
        return ps;
    }

    /// <summary>Jato de vapor branco. A vazao e controlada pelo minigame (segue a respiracao).</summary>
    static ParticleSystem Steam(Transform parent, Vector3 pos, Material mat)
    {
        var go = new GameObject("Steam");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(-30f, 180f, 0f);   // para a frente do painel (-Z) e um pouco para cima
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = 0.8f;
        main.startSpeed = 0.14f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.05f);
        main.startColor = new Color(1f, 1f, 1f, 0.6f);
        main.gravityModifier = -0.03f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission;
        em.rateOverTime = 0f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 14f;
        shape.radius = 0.006f;
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.4f));
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        return ps;
    }

    static void Text(string name, Transform parent, Vector3 pos, string text, float size, Color color, float rotZ, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        var t = go.AddComponent<TextMeshPro>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.rectTransform.sizeDelta = new Vector2(0.12f, 0.04f);
    }

    // =================================================================
    // Serializacao
    // =================================================================

    static void Ref(SerializedObject so, string field, Object value, string text = null)
    {
        var p = so.FindProperty(field);
        if (p == null) throw new System.Exception("Campo nao encontrado: " + field);
        if (text != null) p.stringValue = text;
        else p.objectReferenceValue = value;
    }

    static void Ref(SerializedObject so, string field, Component value) => Ref(so, field, (Object)value);

    static void Refs(SerializedObject so, string field, params Object[] values)
    {
        var p = so.FindProperty(field);
        if (p == null) throw new System.Exception("Campo nao encontrado: " + field);
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    static void SetLock(SerializedProperty l, float angle, float scrub, ZatanScrubArea joint, ParticleSystem smoke)
    {
        l.FindPropertyRelative("angle").floatValue = angle;
        l.FindPropertyRelative("scrubDistance").floatValue = scrub;
        l.FindPropertyRelative("joint").objectReferenceValue = joint;
        l.FindPropertyRelative("smoke").objectReferenceValue = smoke;
    }

    // =================================================================
    // Materiais e texturas
    // =================================================================

    static Material Asset(string name, string shader)
    {
        string path = $"{MatDir}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(m, path);
        }
        else m.shader = Shader.Find(shader);
        return m;
    }

    static Material Mat(string name, Color color, float smoothness = 0.3f, float metallic = 0f, Color? emission = null)
    {
        var m = Asset(name, "Universal Render Pipeline/Lit");
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", smoothness);
        m.SetFloat("_Metallic", metallic);
        if (emission.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material Unlit(string name, Color color)
    {
        var m = Asset(name, "Universal Render Pipeline/Unlit");
        m.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material ParticleMat(string name, Color color)
    {
        var m = Asset(name, "Universal Render Pipeline/Particles/Unlit");
        m.SetColor("_BaseColor", color);
        return m;
    }

    /// <summary>Deixa o material URP transparente (mistura por alfa, sem escrever profundidade).</summary>
    static Material Transparent(Material m, Texture2D map)
    {
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        // Em material transparente o brilho nao some com o alfa: sem brilho, a mancha nao deixa retangulo.
        m.SetFloat("_Smoothness", 0f);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_SpecularHighlights", 0f);
        m.SetFloat("_EnvironmentReflections", 0f);
        m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetShaderPassEnabled("ShadowCaster", false);
        if (map != null) m.SetTexture("_BaseMap", map);
        EditorUtility.SetDirty(m);
        return m;
    }

    /// <summary>Mancha de piche: disco com a borda irregular.</summary>
    static Texture2D Blob(string name, float wobble, float radius, float wobbleSize)
    {
        const int n = 128;
        var pixels = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(u * u + v * v);
                float ang = Mathf.Atan2(v, u);
                float edge = radius + (Mathf.PerlinNoise(Mathf.Cos(ang) * wobble + 5f, Mathf.Sin(ang) * wobble + 5f) - 0.5f) * 2f * wobbleSize;
                float a = Mathf.Clamp01((edge - d) / 0.07f);
                pixels[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        return SaveTexture(name, n, pixels);
    }

    /// <summary>Bolha macia para a fumaca.</summary>
    static Texture2D Puff(string name)
    {
        const int n = 64;
        var pixels = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                float a = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v)), 2f);
                pixels[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        return SaveTexture(name, n, pixels);
    }

    static Texture2D SaveTexture(string name, int n, Color32[] pixels)
    {
        string path = $"{MatDir}/{name}.png";
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        t.SetPixels32(pixels);
        File.WriteAllBytes(path, t.EncodeToPNG());
        Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
