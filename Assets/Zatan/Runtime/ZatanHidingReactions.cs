using UnityEngine;

namespace Jamlloween.Zatan
{
    /// <summary>
    /// Zatan versus the trash cans. Saw the player get in: he goes straight for that can, tips it, stands still,
    /// turns his head to the player and goes after them (2 s by default). Only suspicious: once per search there is
    /// a chance he tips a random can nearby, the player's included; the same reaction then lasts 3 s.
    /// Runs before the eye lights in LateUpdate so the beams follow the turned head in the same frame.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class ZatanHidingReactions : MonoBehaviour
    {
        enum Reason { Seen, Suspicious }

        [SerializeField] ZatanConfig config;
        [SerializeField] ZatanBehaviourBrain brain;
        [SerializeField] ZatanPerception perception;
        [SerializeField] ZatanTarget target;
        [SerializeField] ZatanHidingRegistry registry;
        [Tooltip("The head that turns to look at the player after a tip (Cabeca_ctrl).")]
        [SerializeField] Transform head;

        /// <summary>Test hook: a fixed roll in [0, 1) for the next suspicious check instead of a random one.</summary>
        public float? RollOverride { get; set; }
        public ZatanHidingSpot TargetSpot { get; private set; }
        public float HeadYaw => headYaw;

        Reason reason;
        bool playerWasInside, rolledThisSearch, resumingSearch, headApplied;
        float reactionTime, stopTime, holdTime, lastSawTime = -999f, headYaw, headPitch;
        Quaternion headBase, headAppliedRotation;

        void Awake()
        {
            if (config == null) throw new MissingReferenceException(nameof(config));
            if (brain == null) throw new MissingReferenceException(nameof(brain));
            if (perception == null) throw new MissingReferenceException(nameof(perception));
            if (target == null) throw new MissingReferenceException(nameof(target));
            if (registry == null) throw new MissingReferenceException(nameof(registry));
            if (head == null) throw new MissingReferenceException(nameof(head));
        }

        void OnEnable()
        {
            registry.PlayerHid += OnPlayerHid;
            brain.StateChanged += OnStateChanged;
        }

        void OnDisable()
        {
            registry.PlayerHid -= OnPlayerHid;
            brain.StateChanged -= OnStateChanged;
            TargetSpot = null;
            headYaw = headPitch = 0f;
            headApplied = false;
        }

        void Update()
        {
            if (perception.CanSeePlayer) lastSawTime = Time.time;

            if (brain.State == ZatanBehaviourState.GoToCan) UpdateApproach();
            else if (brain.State == ZatanBehaviourState.TipReaction) UpdateReaction();
        }

        void UpdateApproach()
        {
            Vector3 toCan = TargetSpot.Position - transform.position;
            toCan.y = 0f;
            if (toCan.magnitude > config.tipReach)
            {
                // At the end of the path but still out of reach (something moved in the way): give up on this can.
                if (brain.ReachedDestination && toCan.magnitude > config.tipReach + 1f) GiveUp();
                return;
            }

            playerWasInside = TargetSpot.Tip(transform.position);
            bool seen = reason == Reason.Seen;
            stopTime = playerWasInside ? (seen ? config.seenStopTime : config.suspiciousStopTime) : config.emptyTipPause;
            holdTime = playerWasInside ? (seen ? config.seenHoldTime : config.suspiciousHoldTime) : 0f;
            reactionTime = 0f;
            brain.HoldForReaction();
        }

        void UpdateReaction()
        {
            reactionTime += Time.deltaTime;
            if (reactionTime < stopTime + holdTime) return;

            TargetSpot = null;
            if (playerWasInside) brain.ResumeChase(target.Position);
            else
            {
                resumingSearch = true;
                brain.ResumeSearch();
                resumingSearch = false;
            }
        }

        /// <summary>The head turns only in the hold part of the reaction, after the still part.</summary>
        bool LookingAtPlayer => brain.State == ZatanBehaviourState.TipReaction && playerWasInside && reactionTime >= stopTime;

        void LateUpdate()
        {
            float wantYaw = 0f, wantPitch = 0f;
            if (LookingAtPlayer)
            {
                Vector3 local = transform.InverseTransformPoint(target.ChestPosition) - transform.InverseTransformPoint(head.position);
                wantYaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -config.headMaxYaw, config.headMaxYaw);
                wantPitch = Mathf.Clamp(-Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -45f, 45f);
            }
            float step = config.headTurnSpeed * Time.deltaTime;
            headYaw = Mathf.MoveTowards(headYaw, wantYaw, step);
            headPitch = Mathf.MoveTowards(headPitch, wantPitch, step);

            // The animator writes the head every frame it animates it; when it didn't, start again from the pose
            // we turned last frame instead of stacking the turn on top of itself.
            headBase = headApplied && head.localRotation == headAppliedRotation ? headBase : head.localRotation;
            headApplied = !Mathf.Approximately(headYaw, 0f) || !Mathf.Approximately(headPitch, 0f);
            if (!headApplied) return;
            // Yaw and pitch around Zatan's own axes, not the bone's: rigs differ in how their head bone is oriented.
            head.localRotation = headBase;
            head.rotation = Quaternion.AngleAxis(headYaw, transform.up) * Quaternion.AngleAxis(headPitch, transform.right) * head.rotation;
            headAppliedRotation = head.localRotation;
        }

        void OnPlayerHid(ZatanHidingSpot spot)
        {
            bool inChase = brain.State == ZatanBehaviourState.Chase || brain.State == ZatanBehaviourState.Spotted;
            if (inChase && Time.time - lastSawTime <= config.hideSeenGrace) GoFor(spot, Reason.Seen);
            else if (brain.State == ZatanBehaviourState.Search && !rolledThisSearch) Roll();
            // Chasing without sight: he finds out when he reaches the area and starts searching.
        }

        void OnStateChanged(ZatanBehaviourState state)
        {
            if (state != ZatanBehaviourState.GoToCan && state != ZatanBehaviourState.TipReaction) TargetSpot = null;
            if (state != ZatanBehaviourState.Search || resumingSearch) return;

            rolledThisSearch = false;
            if (target.IsHidden) Roll();
        }

        void Roll()
        {
            rolledThisSearch = true;
            float roll = RollOverride ?? Random.value;
            RollOverride = null;
            if (roll >= config.tipChance) return;
            if (registry.TryPickStanding(perception.LastSeenPosition, config.tipSearchRadius, out ZatanHidingSpot spot))
                GoFor(spot, Reason.Suspicious);
        }

        /// <summary>Goes for the can if he can walk up to it; a can he can't reach is ignored.</summary>
        void GoFor(ZatanHidingSpot spot, Reason why)
        {
            Vector3 fromCan = transform.position - spot.Position;
            fromCan.y = 0f;
            Vector3 side = fromCan.sqrMagnitude > 0.0001f ? fromCan.normalized : -transform.forward;
            if (!brain.TryFindReachableNear(spot.Position, side, config.tipApproachDistance, out Vector3 stand)) return;
            TargetSpot = spot;
            reason = why;
            brain.HuntCan(stand);
        }

        void GiveUp()
        {
            TargetSpot = null;
            if (reason == Reason.Seen) brain.ResumeChase(perception.LastSeenPosition);
            else
            {
                resumingSearch = true;
                brain.ResumeSearch();
                resumingSearch = false;
            }
        }
    }
}
