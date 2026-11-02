using System.Collections.Generic;
using UnityEngine;

namespace Circus
{
    /// <summary>
    /// Broken (slow idle sweep) until the monitors (M4) are repaired and there is power.
    /// Active + "câmeras rastreiam" flag: turns to follow the player within range; after cameraReportTime
    /// seconds watching, reports the player position to the Zatan (EventBus.PlayerSpotted).
    /// </summary>
    public class SecurityCamera : MonoBehaviour, PowerGrid.IConsumer
    {
        public static readonly List<SecurityCamera> All = new List<SecurityCamera>();

        public string id;
        [SerializeField] string activationMachineId = "M4_monitors";
        [SerializeField] Camera feedCamera;
        [SerializeField] Renderer led;
        [SerializeField] Material ledOff, ledOn;
        [SerializeField] float sweepAngle = 35f;
        [SerializeField] float brokenSweepSpeed = 0.15f;
        [SerializeField] float activeSweepSpeed = 0.35f;
        [SerializeField] float powerCost = 0.25f;
        [SerializeField] int feedWidth = 256, feedHeight = 192;

        public bool Activated { get; private set; }
        public bool IsWorking => Activated && PowerGrid.HasPower;
        public float WatchTime { get; private set; }
        public bool SeesPlayer { get; private set; }
        public RenderTexture Feed { get; private set; }
        public float PowerCost => Activated ? powerCost : 0f;

        Quaternion baseRot;
        float sweepPhase, cooldown, glanceUntil;
        bool ledState, ledApplied;

        void Awake()
        {
            baseRot = transform.rotation;
            sweepPhase = Random.value * 10f;
            if (feedCamera) feedCamera.enabled = false;
        }

        void OnEnable()
        {
            All.Add(this);
            PowerGrid.Consumers.Add(this);
            EventBus.MachineRepaired += OnRepaired;
        }

        void OnDisable()
        {
            All.Remove(this);
            PowerGrid.Consumers.Remove(this);
            EventBus.MachineRepaired -= OnRepaired;
            if (Feed) { Feed.Release(); Destroy(Feed); }
        }

        void OnRepaired(string mid) { if (mid == activationMachineId) Activated = true; }
        public void SetActivated(bool on) => Activated = on;
        public void ForceOff() { }

        /// <summary>Scripted: snap toward the player for a while even when broken (TR2).</summary>
        public void Glance(float seconds) => glanceUntil = Time.time + seconds;

        public RenderTexture EnsureFeed()
        {
            if (Feed == null)
            {
                Feed = new RenderTexture(feedWidth, feedHeight, 16) { name = "Feed_" + id, filterMode = FilterMode.Point };
                if (feedCamera) feedCamera.targetTexture = Feed;
            }
            return Feed;
        }

        public void SetFeedActive(bool on)
        {
            if (!feedCamera) return;
            if (on) EnsureFeed();
            feedCamera.enabled = on;
        }

        void Update()
        {
            var t = Tunables.I;
            var p = FirstPersonController.I;
            SeesPlayer = false;
            bool tracking = IsWorking && t.camerasTrack;
            bool glancing = Time.time < glanceUntil;

            Vector3 target = p ? p.HeadPosition - Vector3.up * 0.3f : Vector3.zero;
            if ((tracking || glancing) && p && CanSee(target, t))
            {
                SeesPlayer = tracking;
                var look = Quaternion.LookRotation(target - transform.position);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, t.cameraTurnSpeed * Time.deltaTime);
            }
            else if (glancing && p)
            {
                var look = Quaternion.LookRotation(target - transform.position);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, t.cameraTurnSpeed * 2f * Time.deltaTime);
            }
            else
            {
                sweepPhase += Time.deltaTime * (IsWorking ? activeSweepSpeed : brokenSweepSpeed);
                var sweep = baseRot * Quaternion.Euler(0, Mathf.Sin(sweepPhase) * sweepAngle, 0);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, sweep, 30f * Time.deltaTime);
            }

            cooldown -= Time.deltaTime;
            if (SeesPlayer)
            {
                WatchTime += Time.deltaTime;
                if (WatchTime >= t.cameraReportTime && cooldown <= 0f)
                {
                    EventBus.RaisePlayerSpotted(p.transform.position, id);
                    EventBus.RaiseMessage($"CAM {id.Substring(1).PadLeft(2, '0')} — REC ●", 1.5f);
                    cooldown = t.cameraReportCooldown;
                    WatchTime = 0f;
                }
            }
            else WatchTime = Mathf.Max(0f, WatchTime - Time.deltaTime * 2f);

            bool on = IsWorking && (SeesPlayer || Mathf.Repeat(Time.time + sweepPhase, 1.2f) < 0.6f);
            if (!ledApplied || on != ledState)
            {
                ledApplied = true; ledState = on;
                if (led) led.sharedMaterial = on ? ledOn : ledOff;
            }
        }

        bool CanSee(Vector3 target, Tunables t)
        {
            if (HidingSpot.Occupied != null) return false;
            var to = target - transform.position;
            if (to.magnitude > t.cameraRange) return false;
            // field of view measured from the idle direction so the camera cannot spin 360°
            if (Vector3.Angle(baseRot * Vector3.forward, to) > t.cameraFov * 0.5f + sweepAngle) return false;
            if (Physics.Raycast(transform.position + to.normalized * 0.4f, to.normalized, out var hit, to.magnitude, ~0, QueryTriggerInteraction.Ignore))
                return hit.collider.GetComponentInParent<FirstPersonController>() != null;
            return true;
        }

        /// <summary>0..1, strongest camera currently watching the player.</summary>
        public static float MaxWatch01()
        {
            float m = 0f;
            foreach (var c in All) if (c.SeesPlayer) m = Mathf.Max(m, 0.4f + 0.6f * Mathf.Clamp01(c.WatchTime / Tunables.I.cameraReportTime));
            return m;
        }
    }
}
