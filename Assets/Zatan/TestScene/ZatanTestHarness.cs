using HeyHeyThere.PSXCarnivalKit;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jamlloween.Zatan.TestScene
{
    /// <summary>
    /// Zatan_Test only. F5 scripted event, F6 objective done, F7 auto-run away from Zatan (measures the chase gap),
    /// F8 tension 0, F9 tension 100, F10 Zatan off/on (to test the minigames in peace). A catch restarts when its
    /// sequence ends. Also draws the overlay.
    /// </summary>
    public sealed class ZatanTestHarness : MonoBehaviour
    {
        [SerializeField] ZatanBehaviourBrain brain;
        [SerializeField] ZatanTension tension;
        [SerializeField] ZatanPerception perception;
        [SerializeField] LevelWalkerZatanTarget target;
        [SerializeField] LevelWalker walker;
        [SerializeField] CharacterController body;
        [SerializeField] Transform zatanSpawn;
        [SerializeField] ZatanCatchSequence catchSequence;

        public bool AutoRun { get; set; }
        public float MinChaseDistance { get; private set; } = float.PositiveInfinity;
        public float MaxChaseSpeed { get; private set; }
        public int Catches { get; private set; }
        public bool ZatanEnabled => brain.gameObject.activeSelf;

        Vector3 playerStart;

        void Awake()
        {
            if (brain == null) throw new MissingReferenceException(nameof(brain));
            if (tension == null) throw new MissingReferenceException(nameof(tension));
            if (perception == null) throw new MissingReferenceException(nameof(perception));
            if (target == null) throw new MissingReferenceException(nameof(target));
            if (walker == null) throw new MissingReferenceException(nameof(walker));
            if (body == null) throw new MissingReferenceException(nameof(body));
            if (zatanSpawn == null) throw new MissingReferenceException(nameof(zatanSpawn));
            if (catchSequence == null) throw new MissingReferenceException(nameof(catchSequence));
            playerStart = body.transform.position;
        }

        void OnEnable()
        {
            brain.PlayerCaught += OnCaught;
            catchSequence.Finished += Restart;
        }

        void OnDisable()
        {
            brain.PlayerCaught -= OnCaught;
            catchSequence.Finished -= Restart;
        }

        void Update()
        {
            Keyboard keys = Keyboard.current;
            if (keys != null)
            {
                if (keys.f5Key.wasPressedThisFrame) tension.RaiseScriptedEvent();
                if (keys.f6Key.wasPressedThisFrame) tension.CompleteObjective();
                if (keys.f7Key.wasPressedThisFrame) AutoRun = !AutoRun;
                if (keys.f8Key.wasPressedThisFrame) tension.SetValue(0f);
                if (keys.f9Key.wasPressedThisFrame) tension.SetValue(ZatanTension.Max);
                if (keys.f10Key.wasPressedThisFrame) ToggleZatan();
            }

            if (!ZatanEnabled || brain.State == ZatanBehaviourState.Caught) return;

            if (brain.State == ZatanBehaviourState.Chase)
            {
                MinChaseDistance = Mathf.Min(MinChaseDistance, brain.DistanceToPlayer);
                MaxChaseSpeed = Mathf.Max(MaxChaseSpeed, brain.CurrentSpeed);
            }

            if (AutoRun)
            {
                Vector3 away = Vector3.ProjectOnPlane(body.transform.position - brain.transform.position, Vector3.up).normalized;
                body.Move(away * (walker.run * Time.deltaTime));
            }
        }

        void OnCaught()
        {
            Catches++;
            AutoRun = false;
        }

        void Restart()
        {
            body.enabled = false;
            body.transform.position = playerStart;
            body.enabled = true;
            brain.Respawn(zatanSpawn.position);
        }

        /// <summary>
        /// Off: the whole Zatan object goes inactive, so his AI, sounds, eye lights and catch stop with it.
        /// On: back at the spawn point, patrolling. Ignored during a catch sequence.
        /// </summary>
        [ContextMenu("Toggle Zatan")]
        public void ToggleZatan()
        {
            if (brain.State == ZatanBehaviourState.Caught) return;
            bool on = !ZatanEnabled;
            AutoRun = false;
            brain.gameObject.SetActive(on);
            if (on) brain.Respawn(zatanSpawn.position);
        }

        [ContextMenu("Reset Measurements")]
        public void ResetMeasurements()
        {
            MinChaseDistance = float.PositiveInfinity;
            MaxChaseSpeed = 0f;
        }

        void OnGUI()
        {
            if (!ZatanEnabled)
            {
                GUI.Box(new Rect(10f, 10f, 330f, 58f), GUIContent.none);
                GUI.Label(new Rect(20f, 16f, 310f, 20f), "ZATAN DESLIGADO (minigames sem perigo)");
                if (GUI.Button(new Rect(20f, 38f, 310f, 24f), "F10  Ligar Zatan")) ToggleZatan();
                return;
            }
            if (brain.State == ZatanBehaviourState.Caught) return;

            ZatanTuning tuning = tension.Tuning;
            GUI.Box(new Rect(10f, 10f, 330f, 270f), GUIContent.none);
            GUI.Box(new Rect(20f, 18f, 310f, 16f), GUIContent.none);
            Color previous = GUI.color;
            GUI.color = Color.Lerp(Color.green, Color.red, tension.Normalized);
            GUI.DrawTexture(new Rect(21f, 19f, 308f * tension.Normalized, 14f), Texture2D.whiteTexture);
            GUI.color = previous;

            string text =
                $"Tensão {tension.Value:0.0}  |  {brain.State}\n" +
                $"Distância {brain.DistanceToPlayer:0.0} m  vel {brain.CurrentSpeed:0.00} / limite {brain.CurrentSpeedLimit:0.00}\n" +
                $"Jogador anda {target.WalkSpeed:0.0} corre {target.RunSpeed:0.0}  atual {target.HorizontalVelocity.magnitude:0.0}\n" +
                $"Visão {tuning.VisionRange:0.0} m / {tuning.VisionAngle:0}°  audição x{tuning.HearingMult:0.00}\n" +
                $"Patrulha {tuning.PatrolMinRadius:0}-{tuning.PatrolMaxRadius:0} m  perde em {tuning.LoseTime:0.0} s\n" +
                $"Passos {tuning.FootstepVolume:0.00}  interior {tension.PlayerIndoors}  sem chase {tension.TimeSinceChase:0} s\n" +
                $"Vê jogador {perception.CanSeePlayer}  jogador vê {perception.PlayerSeesZatan}\n" +
                $"Menor distância {MinChaseDistance:0.00}  capturas {Catches}\n" +
                "F5 evento  F6 objetivo  F7 auto-corrida  F8 tensão 0  F9 tensão 100";
            GUI.Label(new Rect(20f, 38f, 310f, 205f), text);
            if (GUI.Button(new Rect(20f, 248f, 310f, 24f), "F10  Desligar Zatan")) ToggleZatan();
        }
    }
}
