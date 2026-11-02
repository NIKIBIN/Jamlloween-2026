using UnityEngine; using UnityEditor; using UnityEngine.Rendering; using TMPro;
public static class TmpRebuildRadio {
  const string MatDir = "Assets/Materials/Minigames/Radio";
  const string PrefabPath = "Assets/Prefabs/Minigames/RadioMinigame.prefab";
  static Material M(string n) => AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{n}.mat");
  static Material NewMat(string name, Color c, float metallic, float smooth) {
    var m = M(name); if (m != null) { m.SetColor("_BaseColor", c); EditorUtility.SetDirty(m); return m; }
    m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", c); m.SetFloat("_Metallic", metallic); m.SetFloat("_Smoothness", smooth);
    AssetDatabase.CreateAsset(m, $"{MatDir}/{name}.mat"); return m;
  }
  static GameObject Prim(string name, PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Material mat, Vector3? euler = null, bool collider = false) {
    var g = GameObject.CreatePrimitive(t); g.name = name; g.transform.SetParent(parent, false);
    g.transform.localPosition = pos; g.transform.localScale = scale; if (euler.HasValue) g.transform.localEulerAngles = euler.Value;
    g.GetComponent<Renderer>().sharedMaterial = mat; if (!collider) Object.DestroyImmediate(g.GetComponent<Collider>()); return g;
  }
  static GameObject Pivot(string name, Transform parent, Vector3 pos) { var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = pos; return g; }
  static RadioPart Part(GameObject g, RadioPart.Kind k, bool isNew = false) { var p = g.AddComponent<RadioPart>(); p.kind = k; p.isNew = isNew; return p; }
  static BoxCollider Box(GameObject g, Vector3 size, Vector3? center = null) { var b = g.AddComponent<BoxCollider>(); b.size = size; b.center = center ?? Vector3.zero; return b; }
  static void Label(Transform parent, string text, Vector3 pos, float size) {
    var g = new GameObject("Label_" + text); g.transform.SetParent(parent, false);
    var t = g.AddComponent<TextMeshPro>(); t.text = text; t.fontSize = size; t.alignment = TextAlignmentOptions.Center; t.color = new Color(0.05f,0.05f,0.05f);
    t.textWrappingMode = TextWrappingModes.NoWrap; t.rectTransform.sizeDelta = new Vector2(0.03f, 0.012f);
    g.transform.localPosition = pos; g.transform.localRotation = Quaternion.Euler(90, 0, 0);
  }
  static void SetArr(SerializedObject so, string prop, Object[] objs) {
    var p = so.FindProperty(prop); p.arraySize = objs.Length;
    for (int i = 0; i < objs.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = objs[i];
  }
  static RadioPart Battery(string name, Transform parent, bool isNew, Vector3 pos, float yaw, Material body, Material band, Material metal) {
    var b = Pivot(name, parent, pos); b.transform.localRotation = Quaternion.Euler(0, yaw, 0);
    Prim("Body", PrimitiveType.Cylinder, b.transform, Vector3.zero, new Vector3(0.022f, 0.035f, 0.022f), body, new Vector3(90, 0, 0));
    Prim("PlusBand", PrimitiveType.Cylinder, b.transform, new Vector3(0, 0, 0.0285f), new Vector3(0.0226f, 0.0035f, 0.0226f), band, new Vector3(90, 0, 0));
    Prim("PlusNub", PrimitiveType.Cylinder, b.transform, new Vector3(0, 0, 0.0372f), new Vector3(0.008f, 0.0022f, 0.008f), metal, new Vector3(90, 0, 0));
    Prim("MinusCap", PrimitiveType.Cylinder, b.transform, new Vector3(0, 0, -0.0352f), new Vector3(0.019f, 0.0006f, 0.019f), metal, new Vector3(90, 0, 0));
    Box(b, new Vector3(0.024f, 0.024f, 0.076f));
    return Part(b, RadioPart.Kind.Battery, isNew);
  }
  // bandeja/prato: base + 4 bordas
  static void Dish(string name, Transform parent, Vector3 center, Material m) {
    var d = Pivot(name, parent, center).transform;
    Prim("Base", PrimitiveType.Cube, d, new Vector3(0, 0.002f, 0), new Vector3(0.11f, 0.004f, 0.12f), m);
    foreach (var sx in new[]{-0.0535f, 0.0535f}) Prim("RimSide", PrimitiveType.Cube, d, new Vector3(sx, 0.006f, 0), new Vector3(0.003f, 0.008f, 0.12f), m);
    foreach (var sz in new[]{-0.0585f, 0.0585f}) Prim("RimEnd", PrimitiveType.Cube, d, new Vector3(0, 0.006f, sz), new Vector3(0.11f, 0.008f, 0.003f), m);
  }

  public static string Run() {
    var bodyM = M("RadioBody"); var darkM = M("RadioDark"); var panelM = M("RadioPanel"); var knobM = M("RadioKnob"); var pointerM = M("RadioPointer");
    var needleM = M("RadioNeedle"); var pttM = M("RadioPtt"); var metalM = M("RadioMetal"); var coverM = M("RadioCover"); var markM = M("RadioMark");
    var ledOff = M("RadioLedOff"); var ledOn = M("RadioLedOn"); var newBody = M("BatteryNewBody"); var newBand = M("BatteryNewBand");
    var oldBody = M("BatteryOldBody"); var corrode = M("BatteryCorrosion"); var felt = M("WorkMat");
    var trayM = NewMat("RadioTray", new Color(0.42f,0.45f,0.5f), 0.4f, 0.4f);
    var slotOutlineM = NewMat("RadioTraySlot", new Color(0.07f,0.08f,0.09f), 0f, 0.2f);

    var root = PrefabUtility.LoadPrefabContents(PrefabPath);
    var kill = new System.Collections.Generic.List<GameObject>(); foreach (Transform c in root.transform) kill.Add(c.gameObject);
    foreach (var g in kill) Object.DestroyImmediate(g);
    var rootT = root.transform;
    foreach (var c in root.GetComponents<BoxCollider>()) Object.DestroyImmediate(c);
    Box(root, new Vector3(0.7f, 0.1f, 0.5f), new Vector3(0, 0.05f, 0.02f));

    // bancada
    Prim("WorkMat", PrimitiveType.Cube, rootT, new Vector3(0, -0.003f, 0.0f), new Vector3(0.84f, 0.006f, 0.6f), felt);
    var loose = Pivot("Loose", rootT, Vector3.zero);
    Dish("Tray", rootT, new Vector3(-0.2f, 0, -0.03f), trayM);
    foreach (var sx in new[]{-0.025f, 0.025f}) Prim("TraySlot", PrimitiveType.Cube, rootT, new Vector3(-0.2f + sx, 0.0043f, -0.03f), new Vector3(0.026f, 0.0006f, 0.08f), slotOutlineM);
    Dish("Dish", rootT, new Vector3(0.2f, 0, -0.03f), trayM);
    Prim("DishStain", PrimitiveType.Cube, rootT, new Vector3(0.2f, 0.0043f, -0.03f), new Vector3(0.07f, 0.0006f, 0.06f), corrode);

    var radio = Pivot("Radio", rootT, new Vector3(0, 0.0345f, 0)); radio.transform.localRotation = Quaternion.Euler(0, 0, 180);
    var rt = radio.transform;

    var bodyPivot = Pivot("Body", rt, Vector3.zero);
    Prim("Mesh", PrimitiveType.Cube, bodyPivot.transform, Vector3.zero, new Vector3(0.09f,0.045f,0.26f), bodyM);
    Box(bodyPivot, new Vector3(0.09f,0.045f,0.26f));
    var body = Part(bodyPivot, RadioPart.Kind.Body);

    for (int i = 0; i < 5; i++) Prim("Grille_" + i, PrimitiveType.Cube, rt, new Vector3(0,0.0227f,-0.07f - 0.011f*i), new Vector3(0.05f,0.0008f,0.004f), darkM);
    Prim("ScaleStrip", PrimitiveType.Cube, rt, new Vector3(0,0.0226f,0.07f), new Vector3(0.074f,0.0006f,0.024f), panelM);
    for (int i = 0; i <= 10; i++) Prim("Tick_" + i, PrimitiveType.Cube, rt, new Vector3(-0.034f + 0.068f*i/10f,0.0230f,0.0635f), new Vector3(0.0005f,0.0004f,i % 2 == 0 ? 0.008f : 0.005f), darkM);
    foreach (int f in new[]{90, 95, 100, 105}) Label(rt, f.ToString(), new Vector3(-0.034f + 0.068f*(f-88)/20f, 0.0232f, 0.0775f), 0.075f);
    var needle = Prim("Needle", PrimitiveType.Cube, rt, new Vector3(-0.034f,0.0238f,0.07f), new Vector3(0.0007f,0.0006f,0.026f), needleM);

    var dial = Pivot("DialKnob", rt, new Vector3(0,0.0225f,0.005f));
    Prim("Cap", PrimitiveType.Cylinder, dial.transform, new Vector3(0,0.005f,0), new Vector3(0.04f,0.005f,0.04f), knobM);
    Prim("Pointer", PrimitiveType.Cube, dial.transform, new Vector3(0,0.0108f,0.01f), new Vector3(0.004f,0.0012f,0.016f), pointerM);
    Box(dial, new Vector3(0.042f,0.014f,0.042f), new Vector3(0,0.006f,0));
    var dialPart = Part(dial, RadioPart.Kind.Dial);

    var vol = Pivot("VolumeKnob", rt, new Vector3(-0.025f,0.0225f,0.103f));
    Prim("Cap", PrimitiveType.Cylinder, vol.transform, new Vector3(0,0.004f,0), new Vector3(0.024f,0.004f,0.024f), knobM);
    Prim("Pointer", PrimitiveType.Cube, vol.transform, new Vector3(0,0.0088f,0.006f), new Vector3(0.003f,0.001f,0.009f), pointerM);
    Box(vol, new Vector3(0.026f,0.012f,0.026f), new Vector3(0,0.005f,0));
    var volPart = Part(vol, RadioPart.Kind.Volume);

    var led = Prim("Led", PrimitiveType.Sphere, rt, new Vector3(0.022f,0.0228f,0.105f), Vector3.one * 0.009f, ledOff);

    var ptt = Pivot("PushToTalk", rt, new Vector3(-0.052f,0,0));
    Prim("Cap", PrimitiveType.Cube, ptt.transform, Vector3.zero, new Vector3(0.014f,0.024f,0.032f), pttM);
    Box(ptt, new Vector3(0.014f,0.024f,0.032f));
    var pttPart = Part(ptt, RadioPart.Kind.Talk);

    var aBase = Pivot("AntennaBase", rt, new Vector3(0.03f,0,0.13f));
    var rod = Prim("Rod", PrimitiveType.Cylinder, aBase.transform, new Vector3(0,0,0.015f), new Vector3(0.006f,0.015f,0.006f), metalM, new Vector3(90,0,0));
    var tip = Prim("AntennaTip", PrimitiveType.Sphere, aBase.transform, new Vector3(0,0,0.03f), Vector3.one * 0.014f, metalM, null, true);
    var tipPart = Part(tip, RadioPart.Kind.Antenna);

    // costas (-Y): compartimento. Pilhas (raio 0.011) ficam a 1 mm da superficie do corpo e da tampa.
    float cz = -0.045f; const float slotY = -0.0345f;
    Prim("CompartmentFloor", PrimitiveType.Cube, rt, new Vector3(0,-0.0229f,cz), new Vector3(0.058f,0.0008f,0.114f), darkM);
    foreach (var sx in new[]{-0.0293f, 0.0293f}) Prim("WallSide", PrimitiveType.Cube, rt, new Vector3(sx,-0.0335f,cz), new Vector3(0.0015f,0.02f,0.114f), darkM);
    foreach (var sz in new[]{-0.0575f, 0.0575f}) Prim("WallEnd", PrimitiveType.Cube, rt, new Vector3(0,-0.0335f,cz+sz), new Vector3(0.058f,0.02f,0.0015f), darkM);
    float[] slotX = { -0.0125f, 0.0125f }; bool[] plusUp = { true, false };
    var slots = new Transform[2];
    for (int s = 0; s < 2; s++) {
      slots[s] = Pivot("Slot_" + s, rt, new Vector3(slotX[s], slotY, cz)).transform;
      float plusZ = cz + (plusUp[s] ? 0.0465f : -0.0465f), minusZ = cz + (plusUp[s] ? -0.0465f : 0.0465f);
      Prim("Plus_h", PrimitiveType.Cube, rt, new Vector3(slotX[s],-0.0236f,plusZ), new Vector3(0.008f,0.0006f,0.0018f), markM);
      Prim("Plus_v", PrimitiveType.Cube, rt, new Vector3(slotX[s],-0.0236f,plusZ), new Vector3(0.0018f,0.0006f,0.008f), markM);
      Prim("Minus", PrimitiveType.Cube, rt, new Vector3(slotX[s],-0.0236f,minusZ), new Vector3(0.008f,0.0006f,0.0018f), markM);
    }
    Prim("Leak_0", PrimitiveType.Sphere, rt, new Vector3(-0.012f,-0.0236f,cz+0.012f), new Vector3(0.018f,0.0008f,0.013f), corrode);
    Prim("Leak_1", PrimitiveType.Sphere, rt, new Vector3(0.014f,-0.0236f,cz-0.02f), new Vector3(0.014f,0.0008f,0.016f), corrode);
    var old0 = Battery("OldBattery_0", slots[0], false, Vector3.zero, 0f, oldBody, corrode, metalM);
    var old1 = Battery("OldBattery_1", slots[1], false, Vector3.zero, 180f, oldBody, corrode, metalM);

    var anchor = Pivot("CoverClosed", rt, new Vector3(0,-0.0495f,cz));
    var coverPivot = Pivot("Cover", rt, new Vector3(0,-0.0495f,cz));
    Prim("Mesh", PrimitiveType.Cube, coverPivot.transform, Vector3.zero, new Vector3(0.062f,0.004f,0.12f), coverM);
    for (int i = 0; i < 3; i++) Prim("Grip_" + i, PrimitiveType.Cube, coverPivot.transform, new Vector3(0,-0.0023f,-0.04f + 0.008f*i), new Vector3(0.04f,0.0006f,0.002f), darkM);
    Box(coverPivot, new Vector3(0.062f,0.006f,0.12f));
    var coverPart = Part(coverPivot, RadioPart.Kind.Cover);

    // lugares fixos da bancada
    var homes = new Transform[2]; var newB = new RadioPart[2]; var oldRests = new Transform[2];
    for (int i = 0; i < 2; i++) {
      homes[i] = Pivot("NewHome_" + i, rootT, new Vector3(-0.225f + 0.05f*i, 0.015f, -0.03f)).transform;
      newB[i] = Battery("NewBattery_" + i, rootT, true, homes[i].localPosition, 0f, newBody, newBand, metalM);
      oldRests[i] = Pivot("OldRest_" + i, rootT, new Vector3(0.175f + 0.05f*i, 0.015f, -0.03f)).transform;
      oldRests[i].localRotation = Quaternion.Euler(0, i == 0 ? 0f : 180f, 0);
    }
    var coverRest = Pivot("CoverRest", rootT, new Vector3(0.2f, 0.002f, 0.13f)).transform;

    var viewBack = Pivot("ViewBack", rootT, new Vector3(0, 0.40f, -0.038f)); viewBack.transform.localRotation = Quaternion.Euler(86, 0, 0);
    var viewFront = Pivot("ViewFront", rootT, new Vector3(0, 0.40f, 0.033f)); viewFront.transform.localRotation = Quaternion.Euler(84, 0, 0);
    var lightGo = new GameObject("WorkLight", typeof(Light)); lightGo.transform.SetParent(rootT, false); lightGo.transform.localPosition = new Vector3(0,0.3f,0f);
    var li = lightGo.GetComponent<Light>(); li.type = LightType.Point; li.range = 2f; li.intensity = 3.5f; li.color = new Color(1f,0.94f,0.82f);

    radio.AddComponent<AudioSource>(); var audio = radio.AddComponent<RadioAudio>();

    // sem sombras: evita os degraus de sombra do kit nas bordas e o "clipping" visual
    foreach (var r in root.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; }

    var mg = root.GetComponent<RadioMinigame>();
    var so = new SerializedObject(mg);
    so.FindProperty("viewPoint").objectReferenceValue = viewBack.transform;
    SetArr(so, "activeWhilePlaying", new Object[]{ lightGo });
    so.FindProperty("radio").objectReferenceValue = rt; so.FindProperty("loose").objectReferenceValue = loose.transform;
    so.FindProperty("body").objectReferenceValue = body; so.FindProperty("cover").objectReferenceValue = coverPart;
    so.FindProperty("volumeKnob").objectReferenceValue = volPart; so.FindProperty("dialKnob").objectReferenceValue = dialPart;
    so.FindProperty("antennaTip").objectReferenceValue = tipPart; so.FindProperty("pushToTalk").objectReferenceValue = pttPart;
    so.FindProperty("coverClosedAnchor").objectReferenceValue = anchor.transform;
    so.FindProperty("antennaBase").objectReferenceValue = aBase.transform; so.FindProperty("antennaRod").objectReferenceValue = rod.transform;
    so.FindProperty("needle").objectReferenceValue = needle.transform;
    SetArr(so, "slots", slots); SetArr(so, "oldBatteries", new Object[]{ old0, old1 }); SetArr(so, "newBatteries", newB); SetArr(so, "newBatteryHomes", homes);
    SetArr(so, "oldBatteryRests", oldRests); so.FindProperty("coverRest").objectReferenceValue = coverRest;
    so.FindProperty("viewBack").objectReferenceValue = viewBack.transform; so.FindProperty("viewFront").objectReferenceValue = viewFront.transform;
    so.FindProperty("led").objectReferenceValue = led.GetComponent<Renderer>();
    so.FindProperty("ledOnMaterial").objectReferenceValue = ledOn; so.FindProperty("ledOffMaterial").objectReferenceValue = ledOff;
    so.FindProperty("radioAudio").objectReferenceValue = audio;
    so.ApplyModifiedPropertiesWithoutUndo();

    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); PrefabUtility.UnloadPrefabContents(root);
    AssetDatabase.SaveAssets();
    return "ok";
  }
}
