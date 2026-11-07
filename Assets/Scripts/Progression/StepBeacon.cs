using System.Collections.Generic;
using UnityEngine;

namespace Jamlloween.Progression
{
    /// <summary>
    /// A story light that guides the player to an objective (the carousel, the generator, the Ferris wheel).
    /// It is lit from the step <see cref="from"/> until the step <see cref="until"/> starts (empty: to the end), so a
    /// debug jump shows the right lights. Lit, it lights the scenery around it (WorldBeacons.hlsl: computed in the
    /// shaders, seen through the fog from far away) and shows its <see cref="renderers"/> (bulbs, a little lamp).
    /// At most 48 lights (beacons, or each emitFrom point) are lit at once. The story lights go in first, then the decor
    /// lights (<see cref="decor"/>: the lamp posts), nearest first; decor lights far from the camera are not sent at all.
    /// When the 48 slots are full, the console shows one warning.
    /// With a <see cref="powerGroup"/>, the beacon is dark while that LightGroup is off (a blackout), in any step.
    /// Before <see cref="brokenUntil"/>, the beacon is broken (<see cref="brokenStyle"/>): sparks (dark, short bursts, only some
    /// bulbs take part) or a slow dim wave.
    /// </summary>
    [DefaultExecutionOrder(100)]   // after the director changes step
    public sealed class StepBeacon : MonoBehaviour
    {
        public enum Pattern { Steady, Blink, Flicker, Sputter, Driven }
        /// <summary>How a broken light behaves: Sparks (dark most of the time, short bursts) or Wave (dim, drifts slowly, never dark).</summary>
        public enum BrokenStyle { Sparks, Wave }

        const int Max = 48;   // = WORLD_BEACON_MAX em WorldBeacons.hlsl
        static readonly List<StepBeacon> all = new List<StepBeacon>();
        static readonly Vector4[] posRange = new Vector4[Max];
        static readonly Vector4[] colors = new Vector4[Max];
        static readonly Vector4[] spots = new Vector4[Max];
        static readonly int SpotId = Shader.PropertyToID("_BeaconSpot48");
        static int pushedFrame = -1;
        static readonly int CountId = Shader.PropertyToID("_BeaconCount");
        static readonly int FogId = Shader.PropertyToID("_BeaconFogDensity");
        static readonly int PosId = Shader.PropertyToID("_BeaconPosRange48");
        static readonly int ColorId = Shader.PropertyToID("_BeaconColor48");
        const int AmbientMax = 8;   // = WORLD_AMBIENT_MAX em WorldBeacons.hlsl
        static readonly Vector4[] ambientPos = new Vector4[AmbientMax];
        static readonly Vector4[] ambientColors = new Vector4[AmbientMax];
        static readonly int AmbientCountId = Shader.PropertyToID("_AmbientLightCount");
        static readonly int AmbientPosId = Shader.PropertyToID("_AmbientLightPosRange");
        static readonly int AmbientColorId = Shader.PropertyToID("_AmbientLightColor");
        static readonly int BulbLevelId = Shader.PropertyToID("_BulbLevel");
        static readonly int BulbBrokenId = Shader.PropertyToID("_BulbBroken");
        static readonly int BulbVariationId = Shader.PropertyToID("_BulbVariation");
        static readonly int BulbSpeedId = Shader.PropertyToID("_BulbSpeed");
        static readonly int BulbMinId = Shader.PropertyToID("_BulbMin");
        static readonly int BulbBrokenStyleId = Shader.PropertyToID("_BulbBrokenStyle");
        static readonly int SegmentAxisId = Shader.PropertyToID("_SegmentAxis");
        static readonly int SegmentCenterId = Shader.PropertyToID("_SegmentCenter");
        static readonly int SegmentsId = Shader.PropertyToID("_Segments");
        static readonly int BulbMaskId = Shader.PropertyToID("_BulbMask");

        [Header("When")]
        [Tooltip("Lit from the start of this step. Empty: from the start of the game.")]
        [SerializeField] ProgressionStep from;
        [Tooltip("Off from the start of this step. Empty: lit to the end.")]
        [SerializeField] ProgressionStep until;
        [Tooltip("LightGroup id that powers this beacon (e.g. park). While that group is off, the beacon is dark. Empty: no power needed.")]
        [SerializeField] string powerGroup = "";
        [Tooltip("Quebrado até o começo desta etapa (ver Broken Style). Vazio: nunca quebrado.")]
        [SerializeField] ProgressionStep brokenUntil;
        [Tooltip("Faíscas: apagada quase o tempo todo, com faíscas curtas, e só algumas lâmpadas participam (tentando funcionar). Onda: fraca, vai e volta devagar, nunca apaga.")]
        [SerializeField] BrokenStyle brokenStyle = BrokenStyle.Sparks;
        [Tooltip("Faíscas: menor e maior intervalo (s) entre duas faíscas.")]
        [SerializeField] Vector2 brokenGap = new Vector2(1f, 2.5f);
        [Tooltip("Força da luz no máximo da variação, quebrado (1 = a força consertado).")]
        [SerializeField, Range(0f, 1f)] float brokenLevel = 0.3f;
        [Tooltip("Brilho das lâmpadas no máximo da variação, quebrado (1 = o brilho consertado).")]
        [SerializeField, Range(0f, 1f)] float brokenBulbLevel = 0.4f;
        [Tooltip("Onda: mínimo da variação, quebrado, como parte do máximo (0.3 = cai até 30% do máximo).")]
        [SerializeField, Range(0f, 1f)] float brokenMin = 0.3f;
        [Tooltip("Onda: ciclos por segundo da variação, quebrado (0.4 = vai e volta a cada ~2,5 s).")]
        [SerializeField, Min(0.01f)] float brokenSpeed = 0.4f;
        [Tooltip("Funcionando: quanto a luz e algumas lâmpadas oscilam (0 = firme, 0.15 = até 15% mais fraca).")]
        [SerializeField, Range(0f, 0.5f)] float workingVariation = 0.12f;
        [Tooltip("Ciclos por segundo da oscilação funcionando.")]
        [SerializeField, Min(0.01f)] float workingSpeed = 0.6f;
        [Tooltip("Beacons with the same seed (not 0) flicker together, e.g. all the lights of one ride. 0: a random seed.")]
        [SerializeField] int flickerSeed;

        [Header("Light")]
        [Tooltip("Spot: a luz sai num cone para a frente (+Z) deste objeto, em vez de para todos os lados.")]
        [SerializeField] bool spot;
        [Tooltip("Abertura total do cone do spot, em graus.")]
        [SerializeField, Range(5f, 170f)] float spotAngle = 70f;
        [Tooltip("Luz de decoração (os postes): entra nas 48 vagas depois das luzes da história, a mais perto da câmera primeiro, e não é enviada longe da câmera.")]
        [SerializeField] bool decor;
        [Tooltip("Luz das barracas: não usa as 48 vagas. As 8 mais perto da câmera dividem vagas próprias e somem aos poucos na borda.")]
        [SerializeField] bool ambient;
        [SerializeField] Color color = new Color(1f, 0.8f, 0.45f);
        [SerializeField, Min(0f)] float intensity = 2f;
        [Tooltip("Metres the light reaches around the beacon.")]
        [SerializeField, Min(0.1f)] float range = 6f;
        [Tooltip("Queda suave, em metros (os postes): a luz cai como uma lâmpada de verdade e se espalha até o alcance, sem borda. 0: a poça compacta das luzes da história.")]
        [SerializeField, Min(0f)] float softness;
        [Tooltip("De onde a luz sai (as lâmpadas). Cada ponto é uma luz com a intensidade dividida entre eles. Vazio: sai deste objeto.")]
        [SerializeField] Transform[] emitFrom = new Transform[0];
        [SerializeField] Pattern pattern = Pattern.Steady;
        [Tooltip("Seconds of one on-off cycle (Blink).")]
        [SerializeField, Min(0.05f)] float blinkPeriod = 1f;
        [Tooltip("Share of the cycle that is on (Blink).")]
        [SerializeField, Range(0.05f, 0.95f)] float blinkOn = 0.5f;

        [Tooltip("Shortest and longest seconds between two dropouts (Sputter).")]
        [SerializeField] Vector2 sputterGap = new Vector2(1.2f, 3.5f);
        [Header("Shown while lit")]
        [Tooltip("Bulbs or a lamp, on while the beacon is lit (and blinking with it).")]
        [SerializeField] Renderer[] renderers = new Renderer[0];
        [Tooltip("On: the renderers always show and only their glow changes (Beacon Bulb: a dead bulb when dark). Off: they hide when dark.")]
        [SerializeField] bool bulbsStayVisible;
        [Tooltip("Lâmpadas numa fileira reta (as barracas): quantas são. 0: um anel (carrossel, roda-gigante).")]
        [SerializeField, Min(0)] int rowBulbs;
        [Tooltip("Fileira: quais lâmpadas acendem, um bit por lâmpada (bit 0 = a primeira). -1: todas. As outras ficam queimadas.")]
        [SerializeField] int litBulbs = -1;

        [Tooltip("Thin fog of every beacon's light, per metre from the camera. 0.012: about 30% left at 100 m.")]
        [SerializeField, Min(0f)] float fogDensity = 0.012f;

        float seed, level;
        MaterialPropertyBlock block;

        /// <summary>Broken now (before <see cref="brokenUntil"/>).</summary>
        public bool Broken { get; private set; }

        /// <summary>Lit now (in its step range and powered), whatever the blink phase.</summary>
        public bool Lit { get; private set; }

        /// <summary>Padrão Driven: o nível (0..1) vem de outro script, a cada quadro (ex.: o clarão das faíscas do painel do Zatan).</summary>
        public float DrivenLevel { get; set; }

        /// <summary>Todas as luzes ativas na cena (para o apagão em sequência do final).</summary>
        public static IReadOnlyList<StepBeacon> All => all;

        /// <summary>LightGroup que alimenta esta luz (vazio: não depende da energia do parque).</summary>
        public string PowerGroup => powerGroup;

        /// <summary>Já caiu de vez (depois de <see cref="FailPower"/>).</summary>
        public bool Failed => failStart >= 0f && Time.time >= failStart + failSeconds;

        float failStart = -1f, failSeconds;

        /// <summary>A energia desta luz falha: ela engasga por <paramref name="flickerSeconds"/> e apaga de vez.</summary>
        public void FailPower(float flickerSeconds)
        {
            if (failStart >= 0f) return;
            failStart = Time.time;
            failSeconds = Mathf.Max(0f, flickerSeconds);
        }

        public void SetSteady() => pattern = Pattern.Steady;
        public void SetBlink() => pattern = Pattern.Blink;
        public void SetFlicker() => pattern = Pattern.Flicker;

        void Awake() => seed = flickerSeed != 0 ? flickerSeed * 7.31f : Random.value * 100f;

        static float Hash(float n) => Mathf.Repeat(Mathf.Sin(n * 12.9898f + 1.7f) * 43758.5453f, 1f);

        /// <summary>
        /// A faulty circuit trying to work: dark most of the time (on for about 10%). Bursts come at irregular gaps, last
        /// 0.15 to 0.8 s, stutter on and off, and reach only 60 to 100% of the peak. About one gap in five has no burst. 0..1.
        /// </summary>
        float BrokenSparks(float t)
        {
            int index = Mathf.FloorToInt(t / brokenGap.y);
            float start = index * brokenGap.y;
            float offset = Hash(seed + index) * (brokenGap.y - brokenGap.x);
            float local = t - start - offset;
            if (local < 0f || Hash(seed * 1.37f + index) < 0.2f) return 0f;
            float burst = Mathf.Lerp(0.15f, 0.8f, Hash(seed * 2.11f + index));
            if (local > burst) return 0f;
            int tick = Mathf.FloorToInt(local * 22f);
            if (Hash(seed * 3.7f + index * 31f + tick) < 0.35f) return 0f;   // the stutter inside a burst
            return Mathf.Lerp(0.6f, 1f, Hash(seed * 5.3f + index * 17f + tick));
        }

        /// <summary>Circuito com defeito: a luz vai e volta devagar entre <see cref="brokenMin"/> e 1, sem apagar. 0..1.</summary>
        float BrokenWave(float t) => Mathf.Lerp(brokenMin, 1f, Mathf.SmoothStep(0f, 1f, Mathf.PerlinNoise(t * brokenSpeed, seed)));

        /// <summary>Funcionando: oscilação pequena, de 1 até 1 - <see cref="workingVariation"/>.</summary>
        float WorkingWave(float t) => 1f - workingVariation * Mathf.PerlinNoise(t * workingSpeed, seed + 13.7f);

        /// <summary>
        /// A tired circuit: on most of the time, with a soft hum. Dropouts come at irregular gaps, last 0.1 to 0.45 s and
        /// stutter on and off. About one gap in four has no dropout. Not the broken mode: see <see cref="BrokenWave"/>. 0..1.
        /// </summary>
        float Sputter(float t)
        {
            float hum = Mathf.Lerp(0.8f, 1f, Mathf.PerlinNoise(t * 3f, seed));
            int index = Mathf.FloorToInt(t / sputterGap.y);
            float offset = Hash(seed + index) * (sputterGap.y - sputterGap.x);
            float local = t - index * sputterGap.y - offset;
            if (local < 0f || Hash(seed * 1.37f + index) < 0.25f) return hum;
            float drop = Mathf.Lerp(0.1f, 0.45f, Hash(seed * 2.11f + index));
            if (local > drop) return hum;
            int tick = Mathf.FloorToInt(local * 18f);
            return Hash(seed * 3.7f + index * 31f + tick) < 0.6f ? Mathf.Lerp(0f, 0.15f, Hash(seed + tick)) : hum * 0.7f;
        }

        void OnEnable() => all.Add(this);

        void OnDisable()
        {
            all.Remove(this);
            if (all.Count == 0) { Shader.SetGlobalFloat(CountId, 0f); Shader.SetGlobalFloat(AmbientCountId, 0f); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { all.Clear(); pushedFrame = -1; warnedFull = false; }

        bool Powered() => string.IsNullOrEmpty(powerGroup) || LightGroup.IsPoweredAt(powerGroup, transform.position);

        bool BeforeStep(ProgressionStep step)
        {
            var d = ProgressionDirector.Instance;
            if (step == null || d == null) return false;
            int i = d.IndexOf(step);
            return i >= 0 && d.CurrentIndex < i;
        }

        bool InRange()
        {
            var d = ProgressionDirector.Instance;
            if (d == null) return from == null;
            int now = d.CurrentIndex;
            int start = from == null ? 0 : d.IndexOf(from);
            int end = until == null ? int.MaxValue : d.IndexOf(until);
            if (start < 0) return false;
            if (end < 0) end = int.MaxValue;
            return now >= start && now < end;
        }

        void Update()
        {
            Lit = InRange() && Powered();
            Broken = BeforeStep(brokenUntil);
            float t = Time.time;
            switch (pattern)
            {
                case Pattern.Blink: level = Mathf.Repeat(t / blinkPeriod, 1f) < blinkOn ? 1f : 0f; break;
                case Pattern.Flicker:
                    float n = Mathf.PerlinNoise(t * 9f, seed);
                    level = n > 0.32f ? Mathf.Lerp(0.55f, 1f, n) : 0f;
                    break;
                case Pattern.Sputter: level = Sputter(t); break;
                case Pattern.Driven: level = Mathf.Clamp01(DrivenLevel); break;
                default: level = 1f; break;
            }
            if (!Broken && pattern != Pattern.Driven) level *= WorkingWave(t);
            float bulb = level;
            if (Broken) { float f = brokenStyle == BrokenStyle.Sparks ? BrokenSparks(t) : BrokenWave(t); level = f * brokenLevel; bulb = f * brokenBulbLevel; }
            if (failStart >= 0f)
            {
                // Falha de energia: engasga (cada vez mais escuro) e apaga de vez.
                float k = failSeconds <= 0f ? 1f : Mathf.Clamp01((t - failStart) / failSeconds);
                float stutter = k >= 1f || Hash(seed * 3.7f + Mathf.Floor(t * 14f)) < 0.35f + 0.6f * k ? 0f : 1f - 0.7f * k;
                level *= stutter; bulb *= stutter;
            }
            if (!Lit) { level = 0f; bulb = 0f; }
            // Sparks: a dark bulb stays visible as dead glass while broken, so the ring does not vanish between bursts.
            bool show = bulbsStayVisible || bulb > 0f;
            block ??= new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (r.enabled != show) r.enabled = show;
                r.GetPropertyBlock(block);
                // The ring of bulbs turns around the thinnest side of its mesh (the carousel: Y; the Ferris wheel: Z).
                // A row runs along the longest side, one segment per bulb.
                if (r.TryGetComponent(out MeshFilter mf) && mf.sharedMesh != null)
                {
                    Bounds mb = mf.sharedMesh.bounds;
                    Vector3 e = mb.size;
                    if (rowBulbs > 0)
                    {
                        int axis = e.x >= e.y && e.x >= e.z ? 0 : (e.y >= e.z ? 1 : 2);
                        block.SetFloat(SegmentAxisId, 3 + axis);
                        block.SetVector(SegmentCenterId, new Vector4(mb.center.x, mb.center.y, mb.center.z, e[axis]));
                        block.SetFloat(SegmentsId, rowBulbs);
                    }
                    else
                    {
                        float axis = e.x <= e.y && e.x <= e.z ? 0f : (e.y <= e.z ? 1f : 2f);
                        block.SetFloat(SegmentAxisId, axis);
                        block.SetVector(SegmentCenterId, mb.center);
                    }
                }
                block.SetFloat(BulbMaskId, rowBulbs > 0 ? litBulbs : -1f);
                // The bulbs flicker with the light pool; while broken, the shader only picks which bulbs stay dead.
                block.SetFloat(BulbLevelId, bulb);
                block.SetFloat(BulbBrokenId, Broken ? 1f : 0f);
                block.SetFloat(BulbVariationId, workingVariation);
                block.SetFloat(BulbSpeedId, Broken ? brokenSpeed : workingSpeed);
                block.SetFloat(BulbMinId, brokenMin);
                block.SetFloat(BulbBrokenStyleId, brokenStyle == BrokenStyle.Sparks ? 0f : 1f);
                r.SetPropertyBlock(block);
            }
        }

        /// <summary>Sends this beacon's light (one per emitFrom point, or one from itself), scaled by <paramref name="weight"/>.</summary>
        void PushLights(ref int n, ref int wanted, float weight)
        {
            if (weight <= 0f) return;
            if (emitFrom.Length == 0)
            {
                wanted++;
                Push(ref n, transform.position, range, softness, color * (intensity * level * weight), SpotVector());
                return;
            }
            Color share = color * (intensity * level * weight / emitFrom.Length);
            foreach (var point in emitFrom)
            {
                if (point == null)
                {
                    // Only this point is skipped; the other lights of the scene still show.
                    if (!warnedEmpty) { warnedEmpty = true; Debug.LogWarning($"[StepBeacon] '{name}': um ponto de 'emitFrom' está vazio.", this); }
                    continue;
                }
                wanted++;
                Push(ref n, point.position, range, softness, share, SpotVector());
            }
        }

        /// <summary>Direção e abertura do spot para o shader; zero = luz para todos os lados.</summary>
        Vector4 SpotVector()
        {
            if (!spot) return Vector4.zero;
            Vector3 f = transform.forward;
            return new Vector4(f.x, f.y, f.z, Mathf.Cos(spotAngle * 0.5f * Mathf.Deg2Rad));
        }

        static void Push(ref int n, Vector3 p, float range, float soft, Color c, Vector4 spotDir)
        {
            if (n >= Max) return;
            posRange[n] = new Vector4(p.x, p.y, p.z, range);
            colors[n] = new Vector4(c.r, c.g, c.b, soft);
            spots[n] = spotDir;
            n++;
        }

        // Decor lights farther than this (plus their range) from the camera are not sent; they fade out over the last metres.
        const float DecorCullDistance = 60f, DecorFade = 10f;
        static readonly List<(float dist, StepBeacon b)> decorNear = new List<(float, StepBeacon)>();
        static bool warnedFull;
        bool warnedEmpty;

        void LateUpdate()
        {
            if (pushedFrame == Time.frameCount) return;
            pushedFrame = Time.frameCount;
            var cam = Camera.main;
            Vector3 eye = cam != null ? cam.transform.position : Vector3.zero;
            int n = 0, wanted = 0;

            // 1. The story lights: always sent, they guide the player from far away.
            foreach (var b in all)
                if (!b.ambient && !b.decor && b.level > 0f && b.intensity > 0f) b.PushLights(ref n, ref wanted, 1f);

            // 2. The decor lights (lamp posts): nearest first, only near the camera.
            decorNear.Clear();
            foreach (var b in all)
            {
                if (!b.decor || b.ambient || b.level <= 0f || b.intensity <= 0f) continue;
                float d = Vector3.Distance(eye, b.transform.position);
                if (d < DecorCullDistance + b.range) decorNear.Add((d, b));
            }
            decorNear.Sort((x, y) => x.dist.CompareTo(y.dist));
            foreach (var (d, b) in decorNear)
                b.PushLights(ref n, ref wanted, Mathf.Clamp01((DecorCullDistance + b.range - d) / DecorFade));

            if (wanted > Max && !warnedFull)
            {
                warnedFull = true;
                Debug.LogWarning($"[StepBeacon] {wanted} luzes pedidas, só {Max} cabem (WORLD_BEACON_MAX). As mais longe da câmera ficaram de fora.");
            }
            Shader.SetGlobalFloat(CountId, n);
            Shader.SetGlobalFloat(FogId, fogDensity);
            Shader.SetGlobalVectorArray(PosId, posRange);
            Shader.SetGlobalVectorArray(ColorId, colors);
            Shader.SetGlobalVectorArray(SpotId, spots);
            PushAmbient();
        }

        // The ambient lights: the nearest to the camera take the slots. Each one fades out before the first one left out
        // (or before ambientFadeEnd), so a light never pops in or out at full strength.
        static readonly List<(float dist, StepBeacon b)> near = new List<(float, StepBeacon)>();
        const float AmbientFadeEnd = 40f, AmbientBlend = 8f;

        static void PushAmbient()
        {
            near.Clear();
            var cam = Camera.main;
            Vector3 eye = cam != null ? cam.transform.position : Vector3.zero;
            foreach (var b in all)
                if (b.ambient && b.level > 0f && b.intensity > 0f)
                {
                    float d = Vector3.Distance(eye, b.transform.position);
                    if (d < AmbientFadeEnd) near.Add((d, b));
                }
            near.Sort((x, y) => x.dist.CompareTo(y.dist));
            float cut = near.Count > AmbientMax ? near[AmbientMax].dist : AmbientFadeEnd;
            int n = 0;
            for (int i = 0; i < near.Count && n < AmbientMax; i++)
            {
                var (d, b) = near[i];
                float w = Mathf.Clamp01((cut - d) / AmbientBlend);
                if (w <= 0f) continue;
                Vector3 p = b.transform.position;
                ambientPos[n] = new Vector4(p.x, p.y, p.z, b.range);
                Color c = b.color * (b.intensity * b.level * w);
                ambientColors[n] = new Vector4(c.r, c.g, c.b, 0f);
                n++;
            }
            Shader.SetGlobalFloat(AmbientCountId, n);
            Shader.SetGlobalVectorArray(AmbientPosId, ambientPos);
            Shader.SetGlobalVectorArray(AmbientColorId, ambientColors);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = color;
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
