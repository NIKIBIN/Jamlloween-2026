using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Gera as animações do Zatan da perseguição e o Animator Controller delas.
/// O movimento vem do esqueleto Mixamo (Assets/Zatan/Run.fbx): Run é a corrida do FBX (sem o avanço do quadril, para o
/// NavMeshAgent mover); Walk, Idle, Stare, Stalk, Bang e Catch são montados a partir da pose média da corrida, mirando
/// os ossos em direções no espaço do modelo. Cada pose é então retargetada para o modelo que o jogo usa
/// (<see cref="TargetPath"/>, esqueleto HumanIK QuickRig): rotação no espaço do modelo relativa à pose de pé, mais um
/// ajuste para cada osso apontar para o filho na mesma direção do Mixamo. Os clipes animam o esqueleto do alvo.
/// Menu: Jamlloween/Zatan/Gerar animações do rig. Pode rodar de novo: recria os clipes e o controller (mesmos GUIDs).
/// </summary>
public sealed class ZatanRigAnimations
{
    public const string ModelPath = "Assets/Zatan/Run.fbx";
    public const string TargetPath = "Assets/Zatan/Temp/Zatan_Animado_Rig.fbx";
    public const string Folder = "Assets/Zatan/Animation/Rig";
    public const string ControllerPath = Folder + "/Zatan_Rig.controller";

    const string P = "mixamorig:";
    const string TP = "QuickRigCharacter_";

    /// <summary>Ossos retargetados, pais antes dos filhos, e o filho que dá a direção de cada um (vazio: só a rotação).</summary>
    static readonly (string bone, string child)[] Map =
    {
        ("Hips", ""), ("Spine", "Spine1"), ("Spine1", "Spine2"), ("Spine2", "Neck"), ("Neck", "Head"), ("Head", ""),
        ("LeftShoulder", "LeftArm"), ("LeftArm", "LeftForeArm"), ("LeftForeArm", "LeftHand"), ("LeftHand", "-"),
        ("RightShoulder", "RightArm"), ("RightArm", "RightForeArm"), ("RightForeArm", "RightHand"), ("RightHand", "-"),
        ("LeftUpLeg", "LeftLeg"), ("LeftLeg", "LeftFoot"), ("LeftFoot", "LeftToeBase"), ("LeftToeBase", "-"),
        ("RightUpLeg", "RightLeg"), ("RightLeg", "RightFoot"), ("RightFoot", "RightToeBase"), ("RightToeBase", "-")
    };
    // "-": o osso segue o pai como no repouso do alvo (mãos e dedos dos pés sem animação própria).

    GameObject tInst;
    Transform tRoot, tHips;
    Transform[] tBones;
    string[] tPaths;
    readonly Dictionary<string, Transform> tByName = new Dictionary<string, Transform>();
    Quaternion[] tRestLocal;
    Vector3 tRestHipsLocal, tRestHipsModel;
    readonly Dictionary<string, Quaternion> tRestModel = new Dictionary<string, Quaternion>();
    readonly Dictionary<string, Quaternion> mStandModel = new Dictionary<string, Quaternion>();
    Vector3 mStandHips;
    float ratio = 1f;
    bool retargetReady;

    struct TPose
    {
        public Quaternion[] rot;
        public Vector3 hips;
    }

    /// <summary>Velocidades (m/s) em que cada clipe anda sem os pés escorregarem. Medidas pelo próprio gerador.</summary>
    public static float WalkSpeed { get; private set; }
    public static float RunSpeed { get; private set; }

    GameObject inst;
    Transform root;
    Transform[] bones;
    string[] paths;
    Transform hips;
    readonly Dictionary<string, Transform> byName = new Dictionary<string, Transform>();
    float side; // +1 se o lado "Left" do rig fica em +X do modelo

    struct Pose
    {
        public Quaternion[] rot;
        public Vector3 hips;
    }

    [MenuItem("Jamlloween/Zatan/Gerar animações do rig")]
    public static void Generate() => new ZatanRigAnimations().Build();

    void Build()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null) throw new InvalidOperationException($"Modelo não encontrado: {ModelPath}");
        var source = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview"));
        Directory.CreateDirectory(Folder);

        var targetModel = AssetDatabase.LoadAssetAtPath<GameObject>(TargetPath);
        if (targetModel == null) throw new InvalidOperationException($"Modelo alvo não encontrado: {TargetPath}");

        inst = (GameObject)UnityEngine.Object.Instantiate(model);
        inst.hideFlags = HideFlags.HideAndDontSave;
        tInst = (GameObject)UnityEngine.Object.Instantiate(targetModel);
        tInst.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            root = inst.transform;
            root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            hips = root.Find(P + "Hips");
            if (hips == null) throw new InvalidOperationException("Rig sem mixamorig:Hips.");
            bones = hips.GetComponentsInChildren<Transform>(true);
            paths = bones.Select(t => AnimationUtility.CalculateTransformPath(t, root)).ToArray();
            foreach (var t in bones) byName[t.name.Replace(P, "")] = t;
            side = Mathf.Sign(B("LeftUpLeg").position.x - hips.position.x);

            // --- corrida do FBX e pose média
            int n = 24;
            var runPoses = new Pose[n + 1];
            for (int i = 0; i <= n; i++) runPoses[i] = Sample(source, source.length * i / n);
            float hipsZ = runPoses[0].hips.z;
            float realRun = (runPoses[n].hips.z - runPoses[0].hips.z) / source.length;
            // O Mixamo corre com o tronco e a cabeça virados ~40–60° para o lado: endireita o tronco e a cabeça
            // para ele olhar para onde anda (as pernas continuam como no FBX).
            var straight = runPoses.Select(p => { var q = FaceForward(p); q.hips.z = hipsZ; return q; }).ToArray();
            var mean = Mean(straight.Take(n).ToArray());
            mean.hips.z = hipsZ;

            // pose de pé (base de idle, stare, bang, catch) e referência do retarget
            var stand = StandPose(mean);
            InitRetarget(stand);

            float runLen = source.length;
            var runKeys = straight.Select((p, i) => (runLen * i / n, p)).ToList();
            var run = Save(MakeClip(runKeys, true), "Zatan_Rig_Run");
            runPoses = straight;

            // --- caminhada: metade da amplitude da corrida, ciclo mais lento, pés no chão
            const float walkLen = 1.3f;
            var walkKeys = new List<(float, Pose)>();
            for (int i = 0; i <= 26; i++)
            {
                float p = i / 26f;
                var r = runPoses[Mathf.RoundToInt(p * n) % n];
                var w = Blend(mean, r, 0.55f);
                w.hips.y = mean.hips.y + (r.hips.y - mean.hips.y) * 0.35f;
                w.hips.z = hipsZ;
                walkKeys.Add((p * walkLen, Ground(w)));
            }
            var walk = Save(MakeClip(walkKeys, true), "Zatan_Rig_Walk");

            // Idle: respiração e um tique mecânico na cabeça
            const float idleLen = 3.2f;
            var idleKeys = new List<(float, Pose)>
            {
                (0f, stand),
                (idleLen * 0.5f, Ground(Edit(stand, () => RotateModel(B("Spine2"), Vector3.right, -2.5f)))),
                (2.15f, Ground(Edit(stand, () => RotateModel(B("Spine2"), Vector3.right, -1.2f)))),
                (2.25f, Ground(Edit(stand, () => { RotateModel(B("Spine2"), Vector3.right, -1f); RotateModel(B("Head"), Vector3.forward, 9f); }))),
                (2.45f, stand),
                (idleLen, stand)
            };
            var idle = Save(MakeClip(idleKeys, true), "Zatan_Rig_Idle");

            // Stare: rígido, cabeça inclinada, garras um pouco levantadas
            var stare = Ground(Edit(stand, () =>
            {
                RotateModel(B("Head"), Vector3.forward, -16f * side);
                RotateModel(B("Head"), Vector3.right, 8f);
                Aim("LeftForeArm", "LeftHand", new Vector3(side * 0.1f, -0.6f, 0.8f));
                Aim("RightForeArm", "RightHand", new Vector3(-side * 0.1f, -0.6f, 0.8f));
            }));
            var stareTremble = Edit(stare, () => RotateModel(B("Head"), Vector3.forward, 0.8f));
            var stareClip = Save(MakeClip(new List<(float, Pose)> { (0f, stare), (0.5f, stareTremble), (1f, stare) }, true), "Zatan_Rig_Stare");

            // Bang: o braço direito já sobe, bate (no instante do som) e volta
            float r1 = -side; // lado direito
            var raised = Edit(stand, () =>
            {
                Aim("RightArm", "RightForeArm", new Vector3(r1 * 0.15f, 0.85f, 0.5f));
                Aim("RightForeArm", "RightHand", new Vector3(0f, 0.5f, 0.85f));
            });
            var strike = Edit(stand, () =>
            {
                RotateModel(B("Spine1"), Vector3.right, 10f);
                Aim("RightArm", "RightForeArm", new Vector3(r1 * 0.1f, 0.05f, 1f));
                Aim("RightForeArm", "RightHand", new Vector3(0f, -0.25f, 1f));
            });
            var recoil = Edit(stand, () =>
            {
                RotateModel(B("Spine1"), Vector3.right, 6f);
                Aim("RightArm", "RightForeArm", new Vector3(r1 * 0.15f, 0.3f, 0.95f));
                Aim("RightForeArm", "RightHand", new Vector3(0f, 0.1f, 1f));
            });
            var bang = Save(MakeClip(new List<(float, Pose)> { (0f, Ground(raised)), (0.1f, Ground(strike)), (0.3f, Ground(recoil)), (0.65f, stand) }, false), "Zatan_Rig_Bang");

            // Catch: braços abertos para a frente, inclinado sobre o jogador
            var grab = Ground(Edit(stand, () =>
            {
                RotateModel(B("Spine1"), Vector3.right, 15f);
                Aim("LeftArm", "LeftForeArm", new Vector3(side * 0.55f, 0.15f, 1f));
                Aim("LeftForeArm", "LeftHand", new Vector3(-side * 0.2f, 0.05f, 1f));
                Aim("RightArm", "RightForeArm", new Vector3(-side * 0.55f, 0.15f, 1f));
                Aim("RightForeArm", "RightHand", new Vector3(side * 0.2f, 0.05f, 1f));
            }));
            var catchClip = Save(MakeClip(new List<(float, Pose)> { (0f, grab), (1f, grab) }, true), "Zatan_Rig_Catch");

            // Stalk: a caminhada, mais curvado, garras à frente balançando
            var stalkKeys = walkKeys.Select((k, i) =>
            {
                float sway = Mathf.Sin(k.Item1 / walkLen * Mathf.PI * 2f) * 4f;
                return (k.Item1, Ground(Edit(k.Item2, () =>
                {
                    RotateModel(B("Spine1"), Vector3.right, 12f);
                    RotateModel(B("Head"), Vector3.right, -10f);
                    Aim("LeftArm", "LeftForeArm", new Vector3(side * 0.45f, -0.35f, 1f));
                    Aim("LeftForeArm", "LeftHand", new Vector3(0f, -0.2f, 1f));
                    Aim("RightArm", "RightForeArm", new Vector3(-side * 0.45f, -0.35f, 1f));
                    Aim("RightForeArm", "RightHand", new Vector3(0f, -0.2f, 1f));
                    RotateModel(B("LeftArm"), Vector3.right, sway);
                    RotateModel(B("RightArm"), Vector3.right, -sway);
                })));
            }).ToList();
            var stalk = Save(MakeClip(stalkKeys, true), "Zatan_Rig_Stalk");

            // --- velocidades sem escorregar: a corrida real do FBX calibra a medida dos clipes no lugar
            // (a medida no lugar subestima; a razão entre a corrida real e a medida da corrida no Mixamo corrige as duas)
            float k = realRun / MixamoStride(runPoses.Take(n).ToArray(), runLen);
            RunSpeed = k * StrideSpeed(run);
            WalkSpeed = k * StrideSpeed(walk);

            BuildController(idle, walk, run, stareClip, stalk, bang, catchClip);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ZatanRig] clipes em {Folder} para {TargetPath}. Run={RunSpeed:0.00} m/s, Walk={WalkSpeed:0.00} m/s (k={k:0.00}, escala {ratio:0.00}).");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(inst);
            UnityEngine.Object.DestroyImmediate(tInst);
        }
    }

    // ------------------------------------------------------------------ retarget Mixamo -> QuickRig

    Transform T(string name) => tByName.TryGetValue(name, out var t) ? t : throw new InvalidOperationException($"Osso do alvo não encontrado: {TP}{name}");

    void InitRetarget(Pose stand)
    {
        tRoot = tInst.transform;
        tRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        tRoot.localScale = Vector3.one;
        var skin = tInst.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (skin.Length == 0) throw new InvalidOperationException($"{TargetPath} sem SkinnedMeshRenderer.");
        var skinBones = new HashSet<Transform>(skin.SelectMany(s => s.bones).Where(b => b != null));
        tBones = tRoot.GetComponentsInChildren<Transform>(true).Where(skinBones.Contains).ToArray();
        foreach (var t in tBones) tByName[t.name.Replace(TP, "")] = t;
        tHips = T("Hips");
        tPaths = tBones.Select(t => AnimationUtility.CalculateTransformPath(t, tRoot)).ToArray();
        tRestLocal = tBones.Select(t => t.localRotation).ToArray();
        tRestHipsLocal = tHips.localPosition;
        tRestHipsModel = tHips.position;
        foreach (var (bone, _) in Map) tRestModel[bone] = T(bone).rotation;

        Apply(stand);
        foreach (var (bone, _) in Map) mStandModel[bone] = B(bone).rotation;
        mStandHips = hips.position;
        float LegM(string s) => (B(s + "UpLeg").position - B(s + "Leg").position).magnitude + (B(s + "Leg").position - B(s + "Foot").position).magnitude;
        float LegT(string s) => (T(s + "UpLeg").position - T(s + "Leg").position).magnitude + (T(s + "Leg").position - T(s + "Foot").position).magnitude;
        ratio = (LegT("Left") + LegT("Right")) / (LegM("Left") + LegM("Right"));
        retargetReady = true;
    }

    void ApplyTargetRest()
    {
        for (int i = 0; i < tBones.Length; i++) tBones[i].localRotation = tRestLocal[i];
        tHips.localPosition = tRestHipsLocal;
    }

    /// <summary>A pose do Mixamo (já aplicada) passada para o esqueleto do alvo. Os dois ficam na origem, sem rotação.</summary>
    TPose Retarget(Pose p)
    {
        if (!retargetReady) throw new InvalidOperationException("Retarget sem InitRetarget.");
        Apply(p);
        ApplyTargetRest();
        tHips.position = tRestHipsModel + (hips.position - mStandHips) * ratio;
        foreach (var (bone, child) in Map)
        {
            if (child == "-") continue;   // segue o pai como no repouso
            var t = T(bone);
            Quaternion delta = B(bone).rotation * Quaternion.Inverse(mStandModel[bone]);
            t.rotation = delta * tRestModel[bone];
            if (child.Length > 0)
            {
                Vector3 want = B(child).position - B(bone).position;
                Vector3 have = T(child).position - t.position;
                t.rotation = Quaternion.FromToRotation(have, want) * t.rotation;
            }
        }
        // Os pés tocam o chão como no Mixamo (alturas na escala do alvo).
        float lowM = new[] { "LeftFoot", "RightFoot", "LeftToeBase", "RightToeBase" }.Min(n => B(n).position.y);
        float lowT = new[] { "LeftFoot", "RightFoot", "LeftToeBase", "RightToeBase" }.Min(n => T(n).position.y);
        tHips.position += Vector3.up * (lowM * ratio - lowT);
        return new TPose { rot = tBones.Select(b => b.localRotation).ToArray(), hips = tHips.localPosition };
    }

    /// <summary>Velocidade do pé de apoio da corrida no Mixamo, medida como em <see cref="StrideSpeed"/>.</summary>
    float MixamoStride(Pose[] cycle, float length)
    {
        float minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (var p in cycle)
        {
            Apply(p);
            float z = root.InverseTransformPoint(B("LeftToeBase").position).z;
            minZ = Mathf.Min(minZ, z); maxZ = Mathf.Max(maxZ, z);
        }
        return (maxZ - minZ) / (length * 0.5f);
    }

    Transform B(string name) => byName.TryGetValue(name, out var t) ? t : throw new InvalidOperationException($"Osso não encontrado: {name}");

    Pose Sample(AnimationClip clip, float t)
    {
        clip.SampleAnimation(inst, t);
        return Capture();
    }

    Pose Capture() => new Pose { rot = bones.Select(b => b.localRotation).ToArray(), hips = hips.localPosition };

    void Apply(Pose p)
    {
        for (int i = 0; i < bones.Length; i++) bones[i].localRotation = p.rot[i];
        hips.localPosition = p.hips;
    }

    Pose Edit(Pose p, Action edit)
    {
        Apply(p);
        edit();
        return Capture();
    }

    static Pose Mean(Pose[] poses)
    {
        int count = poses[0].rot.Length;
        var rot = new Quaternion[count];
        for (int b = 0; b < count; b++)
        {
            var refQ = poses[0].rot[b];
            Vector4 sum = Vector4.zero;
            foreach (var p in poses)
            {
                var q = p.rot[b];
                if (Quaternion.Dot(q, refQ) < 0f) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                sum += new Vector4(q.x, q.y, q.z, q.w);
            }
            sum.Normalize();
            rot[b] = new Quaternion(sum.x, sum.y, sum.z, sum.w);
        }
        var h = poses.Aggregate(Vector3.zero, (a, p) => a + p.hips) / poses.Length;
        return new Pose { rot = rot, hips = h };
    }

    static Pose Blend(Pose a, Pose b, float t) => new Pose
    {
        rot = a.rot.Select((q, i) => Quaternion.Slerp(q, b.rot[i], t)).ToArray(),
        hips = Vector3.Lerp(a.hips, b.hips, t)
    };

    /// <summary>Gira o osso no espaço do modelo (eixos do Zatan: X lado, Y cima, Z frente).</summary>
    void RotateModel(Transform bone, Vector3 axis, float degrees) =>
        bone.rotation = Quaternion.AngleAxis(degrees, root.TransformDirection(axis)) * bone.rotation;

    /// <summary>Gira o osso para que o filho fique na direção pedida (espaço do modelo).</summary>
    void Aim(string bone, string child, Vector3 dir)
    {
        var b = B(bone); var c = B(child);
        var from = c.position - b.position;
        b.rotation = Quaternion.FromToRotation(from, root.TransformDirection(dir.normalized)) * b.rotation;
    }

    /// <summary>Gira o osso em torno do eixo vertical do modelo até a frente de outro osso (o Z local) apontar para a frente do Zatan.</summary>
    void YawForward(Transform rotate, Transform measure)
    {
        Vector3 f = root.InverseTransformDirection(measure.forward);
        float yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
        rotate.rotation = Quaternion.AngleAxis(-yaw, root.up) * rotate.rotation;
    }

    /// <summary>Tronco e cabeça de frente: o peito (Spine2) e o rosto (Head, Z local) olham para +Z do modelo.</summary>
    Pose FaceForward(Pose p) => Edit(p, () =>
    {
        YawForward(B("Spine"), B("Spine2"));
        YawForward(B("Head"), B("Head"));
    });

    Pose StandPose(Pose mean)
    {
        var p = Edit(mean, () =>
        {
            Aim("Spine", "Neck", new Vector3(0f, 1f, 0.18f));
            Aim("LeftUpLeg", "LeftLeg", new Vector3(side * 0.08f, -1f, 0.03f));
            Aim("LeftLeg", "LeftFoot", new Vector3(side * 0.02f, -1f, -0.04f));
            Aim("LeftFoot", "LeftToeBase", new Vector3(side * 0.1f, -0.35f, 1f));
            Aim("RightUpLeg", "RightLeg", new Vector3(-side * 0.08f, -1f, 0.03f));
            Aim("RightLeg", "RightFoot", new Vector3(-side * 0.02f, -1f, -0.04f));
            Aim("RightFoot", "RightToeBase", new Vector3(-side * 0.1f, -0.35f, 1f));
            Aim("LeftArm", "LeftForeArm", new Vector3(side * 0.35f, -1f, 0.1f));
            Aim("LeftForeArm", "LeftHand", new Vector3(side * 0.15f, -1f, 0.25f));
            Aim("RightArm", "RightForeArm", new Vector3(-side * 0.35f, -1f, 0.1f));
            Aim("RightForeArm", "RightHand", new Vector3(-side * 0.15f, -1f, 0.25f));
            YawForward(B("Spine"), B("Spine2"));
            YawForward(B("Head"), B("Head"));
        });
        return Ground(p);
    }

    /// <summary>Sobe ou desce o quadril para o pé mais baixo encostar no chão.</summary>
    Pose Ground(Pose p)
    {
        Apply(p);
        float low = new[] { "LeftToeBase", "RightToeBase", "LeftFoot", "RightFoot", "LeftToe_End", "RightToe_End" }
            .Min(n => root.InverseTransformPoint(B(n).position).y);
        p.hips.y += 0.04f - low;
        return p;
    }

    AnimationClip BuildRun(AnimationClip source, float hipsZ)
    {
        var clip = new AnimationClip { frameRate = source.frameRate };
        foreach (var bind in AnimationUtility.GetCurveBindings(source))
        {
            var curve = AnimationUtility.GetEditorCurve(source, bind);
            if (bind.path.EndsWith(P + "Hips") && bind.propertyName == "m_LocalPosition.z")
                curve = AnimationCurve.Constant(0f, source.length, hipsZ); // no lugar: quem anda é o NavMeshAgent
            AnimationUtility.SetEditorCurve(clip, bind, curve);
        }
        SetLoop(clip, true);
        return clip;
    }

    /// <summary>Clipe do esqueleto do alvo: cada pose do Mixamo é retargetada antes de virar chave.</summary>
    AnimationClip MakeClip(List<(float time, Pose pose)> mixamoKeys, bool loop)
    {
        var keys = mixamoKeys.Select(k => (k.time, pose: Retarget(k.pose))).ToList();
        var clip = new AnimationClip { frameRate = 30f };
        for (int b = 0; b < tBones.Length; b++)
        {
            var cx = new AnimationCurve(); var cy = new AnimationCurve(); var cz = new AnimationCurve(); var cw = new AnimationCurve();
            Quaternion prev = keys[0].pose.rot[b];
            foreach (var k in keys)
            {
                var q = k.pose.rot[b];
                if (Quaternion.Dot(q, prev) < 0f) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                prev = q;
                cx.AddKey(k.time, q.x); cy.AddKey(k.time, q.y); cz.AddKey(k.time, q.z); cw.AddKey(k.time, q.w);
            }
            Set(clip, tPaths[b], "m_LocalRotation.x", cx);
            Set(clip, tPaths[b], "m_LocalRotation.y", cy);
            Set(clip, tPaths[b], "m_LocalRotation.z", cz);
            Set(clip, tPaths[b], "m_LocalRotation.w", cw);
        }
        string hp = AnimationUtility.CalculateTransformPath(tHips, tRoot);
        var px = new AnimationCurve(); var py = new AnimationCurve(); var pz = new AnimationCurve();
        foreach (var k in keys) { px.AddKey(k.time, k.pose.hips.x); py.AddKey(k.time, k.pose.hips.y); pz.AddKey(k.time, k.pose.hips.z); }
        Set(clip, hp, "m_LocalPosition.x", px);
        Set(clip, hp, "m_LocalPosition.y", py);
        Set(clip, hp, "m_LocalPosition.z", pz);
        clip.EnsureQuaternionContinuity();
        SetLoop(clip, loop);
        return clip;
    }

    static void Set(AnimationClip clip, string path, string prop, AnimationCurve curve) =>
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), prop), curve);

    static void SetLoop(AnimationClip clip, bool loop)
    {
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
    }

    static AnimationClip Save(AnimationClip clip, string name)
    {
        string path = $"{Folder}/{name}.anim";
        clip.name = name;
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(clip, existing);
            existing.name = name;
            EditorUtility.SetDirty(existing);
            return existing;
        }
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    /// <summary>Velocidade do pé de apoio num clipe no lugar (do alvo): quanto o pé corre para trás por meio ciclo.</summary>
    float StrideSpeed(AnimationClip clip)
    {
        const int n = 60;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        for (int i = 0; i < n; i++)
        {
            clip.SampleAnimation(tInst, clip.length * i / n);
            float z = tRoot.InverseTransformPoint(T("LeftToeBase").position).z;
            minZ = Mathf.Min(minZ, z); maxZ = Mathf.Max(maxZ, z);
        }
        return (maxZ - minZ) / (clip.length * 0.5f);
    }

    void BuildController(AnimationClip idle, AnimationClip walk, AnimationClip run, AnimationClip stare,
                         AnimationClip stalk, AnimationClip bang, AnimationClip grab)
    {
        // Reaproveita o asset (mesmo GUID: o Animator da cena continua apontando para ele) e limpa o conteúdo.
        var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (c == null) c = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        else
        {
            c.parameters = new AnimatorControllerParameter[0];
            var old = c.layers[0].stateMachine;
            foreach (var t in old.anyStateTransitions) old.RemoveAnyStateTransition(t);
            foreach (var s in old.states) old.RemoveState(s.state);
            foreach (var bt in AssetDatabase.LoadAllAssetsAtPath(ControllerPath).OfType<BlendTree>()) AssetDatabase.RemoveObjectFromAsset(bt);
        }
        c.AddParameter("Speed", AnimatorControllerParameterType.Float);
        c.AddParameter("Cadence", AnimatorControllerParameterType.Float);
        c.parameters = c.parameters.Select(p => { if (p.name == "Cadence") p.defaultFloat = 1f; return p; }).ToArray();
        c.AddParameter("Stare", AnimatorControllerParameterType.Bool);
        c.AddParameter("Stalk", AnimatorControllerParameterType.Bool);
        c.AddParameter("Caught", AnimatorControllerParameterType.Bool);
        c.AddParameter("Bang", AnimatorControllerParameterType.Trigger);

        var sm = c.layers[0].stateMachine;
        var loco = c.CreateBlendTreeInController("Locomotion", out var tree, 0);
        tree.blendParameter = "Speed";
        tree.useAutomaticThresholds = false;
        tree.AddChild(idle, 0f);
        tree.AddChild(walk, WalkSpeed);
        tree.AddChild(run, RunSpeed);
        loco.speedParameterActive = true;
        loco.speedParameter = "Cadence";
        sm.defaultState = loco;

        var sStare = sm.AddState("Stare"); sStare.motion = stare;
        var sStalk = sm.AddState("Stalk"); sStalk.motion = stalk; sStalk.speedParameterActive = true; sStalk.speedParameter = "Cadence";
        var sCatch = sm.AddState("Catch"); sCatch.motion = grab;
        var sBang = sm.AddState("Bang"); sBang.motion = bang;

        void To(AnimatorState from, AnimatorState to, float dur, string param, AnimatorConditionMode mode)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false; t.duration = dur;
            t.AddCondition(mode, 0f, param);
        }
        To(loco, sStare, 0.15f, "Stare", AnimatorConditionMode.If);
        To(sStare, loco, 0.2f, "Stare", AnimatorConditionMode.IfNot);
        To(loco, sStalk, 0.3f, "Stalk", AnimatorConditionMode.If);
        To(sStalk, loco, 0.3f, "Stalk", AnimatorConditionMode.IfNot);
        To(sCatch, loco, 0.2f, "Caught", AnimatorConditionMode.IfNot);

        var anyCatch = sm.AddAnyStateTransition(sCatch);
        anyCatch.hasExitTime = false; anyCatch.duration = 0.08f; anyCatch.canTransitionToSelf = false;
        anyCatch.AddCondition(AnimatorConditionMode.If, 0f, "Caught");

        var anyBang = sm.AddAnyStateTransition(sBang);
        anyBang.hasExitTime = false; anyBang.duration = 0.06f; anyBang.canTransitionToSelf = true;
        anyBang.AddCondition(AnimatorConditionMode.If, 0f, "Bang");
        var back = sBang.AddTransition(loco);
        back.hasExitTime = true; back.exitTime = 1f; back.duration = 0.15f;
    }
}
