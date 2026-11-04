using System;
using UnityEngine;
using UnityEngine.AI;

namespace Jamlloween.Zatan
{
    /// <summary>
    /// GoToCan and TipReaction belong to the hiding mechanic: he walks to a trash can, tips it, then reacts.
    /// Scripted belongs to the story beats (ZatanSafeRoom and others): an outside sequence moves him by hand.
    /// </summary>
    public enum ZatanBehaviourState { Patrol, Investigate, Spotted, Chase, Search, Caught, Vanished, GoToCan, TipReaction, Scripted }

    /// <summary>The general brain: spot, hear, chase, catch and lose the player. Reads its limits from the tension brain.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class ZatanBehaviourBrain : MonoBehaviour
    {
        const int PointAttempts = 12;
        const float SampleRadius = 4f;
        const float ArriveDistance = 0.8f;

        [SerializeField] ZatanConfig config;
        [SerializeField] ZatanTarget target;
        [SerializeField] ZatanTension tension;
        [SerializeField] ZatanPerception perception;
        [SerializeField] NavMeshAgent agent;
        [SerializeField] Renderer[] renderers;
        [SerializeField] Collider[] colliders;
        [Tooltip("Starts out of the world and stays out until a story beat calls Respawn or AppearAndChase (the game). Off = patrols from the start (test scenes).")]
        [SerializeField] bool startDormant;

        public event Action<ZatanBehaviourState> StateChanged = delegate { };
        public event Action PlayerCaught = delegate { };

        public ZatanBehaviourState State { get; private set; } = ZatanBehaviourState.Patrol;
        public bool IsPresent => State != ZatanBehaviourState.Vanished;
        /// <summary>After the player: chasing, going for the can they hid in, or reacting after tipping it.</summary>
        public bool IsHunting => State == ZatanBehaviourState.Chase || State == ZatanBehaviourState.GoToCan || State == ZatanBehaviourState.TipReaction || State == ZatanBehaviourState.Scripted;
        /// <summary>Out of the world until a story beat brings him back (he never reappears on his own).</summary>
        public bool IsDormant => State == ZatanBehaviourState.Vanished && dormant;
        /// <summary>A story chase: he always knows where the player is and never gives up on his own.</summary>
        public bool Relentless { get; private set; }
        public float DistanceToPlayer => Vector3.Distance(transform.position, target.Position);
        public float CurrentSpeedLimit => agent.speed;
        public float CurrentSpeed => agent.velocity.magnitude;
        /// <summary>Got to the end of his current path (or has none).</summary>
        public bool ReachedDestination => Arrived();

        /// <summary>Scripted events hold him in the world while they run.</summary>
        public bool VanishSuspended { get; set; }

        float stateTime, lostTime, dwellTimer, repathTimer, vanishTimer, vanishDuration, vanishReadyAt;
        float spotDuration;

        /// <summary>Scripted with his beams still white (calm): he only watches. Cleared when he leaves Scripted.</summary>
        public bool CalmEyes { get; private set; }

        /// <summary>Where he looks from (his head).</summary>
        public Vector3 EyePosition => perception.Eye.position;
        bool waitingAtPoint, dormant;

        void Awake()
        {
            if (config == null) throw new MissingReferenceException(nameof(config));
            if (target == null) throw new MissingReferenceException(nameof(target));
            if (tension == null) throw new MissingReferenceException(nameof(tension));
            if (perception == null) throw new MissingReferenceException(nameof(perception));
            if (agent == null) throw new MissingReferenceException(nameof(agent));
            if (renderers == null || renderers.Length == 0) throw new MissingReferenceException(nameof(renderers));
            if (colliders == null || colliders.Length == 0) throw new MissingReferenceException(nameof(colliders));

            agent.acceleration = config.acceleration;
            agent.angularSpeed = config.angularSpeed;
            agent.autoBraking = false;
            agent.stoppingDistance = 0f;
        }

        void OnEnable() => perception.Heard += OnHeard;

        void OnDisable() => perception.Heard -= OnHeard;

        void Start()
        {
            if (!agent.isOnNavMesh) throw new InvalidOperationException("Zatan must start on the NavMesh.");
            if (startDormant) Sleep();
            else EnterPatrol();
        }

        void Update()
        {
            stateTime += Time.deltaTime;
            repathTimer -= Time.deltaTime;

            bool hunting = State == ZatanBehaviourState.Patrol || State == ZatanBehaviourState.Investigate || State == ZatanBehaviourState.Search;
            if (hunting && perception.SightConfirmed) EnterSpotted();

            switch (State)
            {
                case ZatanBehaviourState.Patrol: UpdatePatrol(); break;
                case ZatanBehaviourState.Investigate: UpdateInvestigate(); break;
                case ZatanBehaviourState.Spotted: UpdateSpotted(); break;
                case ZatanBehaviourState.Chase: UpdateChase(); break;
                case ZatanBehaviourState.Search: UpdateSearch(); break;
                case ZatanBehaviourState.Vanished: UpdateVanished(); break;
                case ZatanBehaviourState.GoToCan: UpdateGoToCan(); break;
            }
        }

        // ------------------------------------------------------------------ states

        void UpdatePatrol()
        {
            agent.speed = config.PatrolSpeed(target.WalkSpeed);
            UpdateVanishCheck();
            if (State != ZatanBehaviourState.Patrol || !Arrived()) return;

            if (!waitingAtPoint)
            {
                waitingAtPoint = true;
                dwellTimer = UnityEngine.Random.Range(config.patrolDwellMin, config.patrolDwellMax);
            }
            dwellTimer -= Time.deltaTime;
            if (dwellTimer <= 0f) PickPatrolPoint();
        }

        void UpdateInvestigate()
        {
            agent.speed = config.InvestigateSpeed(target.WalkSpeed, target.RunSpeed);
            if (!Arrived()) { stateTime = 0f; return; }
            if (stateTime >= config.investigateLookTime) EnterPatrol();
        }

        /// <summary>Stands still and stares at the player, then the chase starts.</summary>
        void UpdateSpotted()
        {
            Vector3 toPlayer = Vector3.ProjectOnPlane(target.Position - transform.position, Vector3.up);
            if (toPlayer.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toPlayer), config.spotTurnSpeed * Time.deltaTime);
            if (stateTime < spotDuration) return;

            agent.isStopped = false;
            agent.updateRotation = true;
            repathTimer = 0f;
            SetState(ZatanBehaviourState.Chase);
        }

        void UpdateChase()
        {
            float distance = DistanceToPlayer;
            agent.speed = config.ChaseSpeed(distance, target.RunSpeed);

            if (Relentless) perception.NotePlayerAt(target.Position);
            lostTime = perception.CanSeePlayer || Relentless ? 0f : lostTime + Time.deltaTime;
            if (lostTime >= tension.Tuning.LoseTime)
            {
                SetState(ZatanBehaviourState.Search);
                agent.SetDestination(RandomPointNear(perception.LastSeenPosition, config.searchRadius));
                return;
            }

            if (repathTimer <= 0f)
            {
                repathTimer = config.repathInterval;
                agent.SetDestination(perception.CanSeePlayer || Relentless ? target.Position : perception.LastSeenPosition);
            }

            float reach = Arrived() ? config.catchDistance + config.corneredExtraReach : config.catchDistance;
            if (!target.IsHidden && distance <= reach && ZatanLineOfSight.IsClear(perception.Eye.position, target.ChestPosition, transform, target.transform))
            {
                agent.isStopped = true;
                // The catch sequence owns his transform until Respawn.
                agent.updatePosition = false;
                agent.updateRotation = false;
                SetState(ZatanBehaviourState.Caught);
                PlayerCaught();
            }
        }

        /// <summary>On the way to a trash can. If the player climbs out where he can see them, he chases them instead.</summary>
        void UpdateGoToCan()
        {
            agent.speed = config.ChaseSpeed(DistanceToPlayer, target.RunSpeed);
            if (!target.IsHidden && perception.CanSeePlayer) SetState(ZatanBehaviourState.Chase);
        }

        void UpdateSearch()
        {
            agent.speed = config.InvestigateSpeed(target.WalkSpeed, target.RunSpeed);
            if (stateTime >= config.searchTime) { EnterPatrol(); return; }
            if (Arrived()) agent.SetDestination(RandomPointNear(perception.LastSeenPosition, config.searchRadius));
        }

        void UpdateVanishCheck()
        {
            bool canVanish = !VanishSuspended && Time.time >= vanishReadyAt && DistanceToPlayer >= config.vanishMinDistance && !perception.PlayerSeesZatan;
            vanishTimer = canVanish ? vanishTimer + Time.deltaTime : 0f;
            if (vanishTimer >= config.vanishDelay) Vanish();
        }

        void UpdateVanished()
        {
            if (dormant || stateTime < vanishDuration) return;
            if (TryFindHiddenPatrolPoint(out Vector3 point)) Reappear(point);
        }

        // ------------------------------------------------------------------ transitions

        void SetState(ZatanBehaviourState next)
        {
            State = next;
            if (next != ZatanBehaviourState.Scripted) CalmEyes = false;
            stateTime = 0f;
            lostTime = 0f;
            vanishTimer = 0f;
            waitingAtPoint = false;
            StateChanged(next);
        }

        void EnterSpotted() => EnterSpotted(config.spotStareTime);

        void EnterSpotted(float stare)
        {
            spotDuration = stare;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.updateRotation = false;
            SetState(ZatanBehaviourState.Spotted);
        }

        void EnterPatrol()
        {
            SetState(ZatanBehaviourState.Patrol);
            PickPatrolPoint();
        }

        // ------------------------------------------------------------------ hiding mechanic (driven by ZatanHidingReactions)

        /// <summary>A point next to <paramref name="target"/> he can really walk to, or false if there is none.</summary>
        public bool TryFindReachableNear(Vector3 target, Vector3 preferredSide, float distance, out Vector3 point)
        {
            const int Sides = 8;
            float start = Mathf.Atan2(preferredSide.z, preferredSide.x);
            for (int i = 0; i < Sides; i++)
            {
                // Try his own side first, then alternate left and right around the target.
                int step = (i + 1) / 2 * (i % 2 == 0 ? 1 : -1);
                float angle = start + step * Mathf.PI * 2f / Sides;
                Vector3 candidate = target + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, SampleRadius * 0.5f, agent.areaMask) && HasCompletePath(transform.position, hit.position))
                {
                    point = hit.position;
                    return true;
                }
            }
            point = default;
            return false;
        }

        /// <summary>Walks, at chase speed, to a reachable point next to a trash can.</summary>
        public void HuntCan(Vector3 standPoint)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            SetState(ZatanBehaviourState.GoToCan);
            agent.SetDestination(standPoint);
        }

        /// <summary>Stands still after tipping a can. No catch while he reacts.</summary>
        public void HoldForReaction()
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
            SetState(ZatanBehaviourState.TipReaction);
        }

        /// <summary>Back to the chase, knowing where the player is.</summary>
        public void ResumeChase(Vector3 playerPosition)
        {
            perception.NotePlayerAt(playerPosition);
            agent.isStopped = false;
            agent.updateRotation = true;
            repathTimer = 0f;
            SetState(ZatanBehaviourState.Chase);
        }

        /// <summary>Back to searching around where he last saw the player.</summary>
        public void ResumeSearch()
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            SetState(ZatanBehaviourState.Search);
            agent.SetDestination(RandomPointNear(perception.LastSeenPosition, config.searchRadius));
        }

        void OnHeard(Vector3 position)
        {
            if (State != ZatanBehaviourState.Patrol && State != ZatanBehaviourState.Investigate && State != ZatanBehaviourState.Search) return;
            if (State != ZatanBehaviourState.Investigate) SetState(ZatanBehaviourState.Investigate);
            agent.SetDestination(position);
        }

        void Vanish()
        {
            SetState(ZatanBehaviourState.Vanished);
            vanishDuration = Mathf.Lerp(config.vanishDurationCalm, config.vanishDurationTense, tension.Normalized);
            agent.isStopped = true;
            agent.ResetPath();
            perception.Sensing = false;
            SetPresence(false);
        }

        void Reappear(Vector3 point)
        {
            vanishReadyAt = Time.time + config.vanishCooldown;
            Respawn(point);
        }

        /// <summary>Back in the world at this point, patrolling again. Also used after a catch.</summary>
        public void Respawn(Vector3 point)
        {
            dormant = false;
            Relentless = false;
            agent.updatePosition = true;
            agent.updateRotation = true;
            if (!agent.Warp(point)) throw new InvalidOperationException($"Zatan could not be placed on the NavMesh at {point}.");
            agent.isStopped = false;
            SetPresence(true);
            perception.Sensing = true;
            EnterPatrol();
        }

        void SetPresence(bool present)
        {
            foreach (Renderer r in renderers) r.enabled = present;
            foreach (Collider c in colliders) c.enabled = present;
        }

        // ------------------------------------------------------------------ story beats (driven by ZatanDirector and ZatanSafeRoom)

        /// <summary>Leaves the world and stays out: no senses, no sounds, no reappearing until Respawn or AppearAndChase.</summary>
        public void Sleep()
        {
            Relentless = false;
            agent.updatePosition = true;
            agent.updateRotation = true;
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            perception.Sensing = false;
            SetPresence(false);
            dormant = true;
            SetState(ZatanBehaviourState.Vanished);
        }

        /// <summary>Appears at this point already knowing where the player is: stares for a moment, then a chase he never gives up.</summary>
        public void AppearAndChase(Vector3 point)
        {
            Respawn(point);
            Vector3 toPlayer = Vector3.ProjectOnPlane(target.Position - transform.position, Vector3.up);
            if (toPlayer.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(toPlayer);
            perception.NotePlayerAt(target.Position);
            Relentless = true;
            EnterSpotted();
        }

        /// <summary>
        /// Hands him to an outside sequence: he only moves where it tells him. No catch, no vanish, no state changes from his senses.
        /// With calmEyes his beams stay white (he only watches); otherwise they show the hunt colour.
        /// </summary>
        public void BeginScripted(bool calmEyes = false)
        {
            Relentless = false;
            agent.isStopped = false;
            agent.updateRotation = true;
            CalmEyes = calmEyes;
            SetState(ZatanBehaviourState.Scripted);
        }

        /// <summary>He has seen the player: stares for this long (beams turn to the hunt colour), then a chase he never gives up.</summary>
        public void SpotThenChase(float stareSeconds)
        {
            if (stareSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(stareSeconds));
            agent.updatePosition = true;
            perception.NotePlayerAt(target.Position);
            Relentless = true;
            EnterSpotted(stareSeconds);
        }

        /// <summary>Scripted only: walks to the point at this speed.</summary>
        public void ScriptedMoveTo(Vector3 point, float speed)
        {
            if (State != ZatanBehaviourState.Scripted) throw new InvalidOperationException("ScriptedMoveTo needs BeginScripted first.");
            agent.speed = speed;
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.SetDestination(point);
        }

        /// <summary>Scripted only: stands still and turns toward the point (call every frame while turning).</summary>
        public void ScriptedFace(Vector3 point)
        {
            if (State != ZatanBehaviourState.Scripted) throw new InvalidOperationException("ScriptedFace needs BeginScripted first.");
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.updateRotation = false;
            Vector3 to = Vector3.ProjectOnPlane(point - transform.position, Vector3.up);
            if (to.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), config.spotTurnSpeed * Time.deltaTime);
        }

        /// <summary>A random NavMesh point within the radius (his own position if none is found).</summary>
        public Vector3 PointNear(Vector3 center, float radius) => RandomPointNear(center, radius);

        /// <summary>
        /// Story finale: he appears at this point (it can be off the NavMesh, e.g. outside a Ferris wheel gondola),
        /// facing the player, and the catch sequence starts at once.
        /// </summary>
        public void ScriptedCatch(Vector3 position)
        {
            dormant = false;
            Relentless = false;
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            agent.updatePosition = false;
            agent.updateRotation = false;
            Vector3 toPlayer = Vector3.ProjectOnPlane(target.Position - position, Vector3.up);
            transform.SetPositionAndRotation(position, toPlayer.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(toPlayer) : transform.rotation);
            perception.Sensing = false;
            SetPresence(true);
            SetState(ZatanBehaviourState.Caught);
            PlayerCaught();
        }

        // ------------------------------------------------------------------ navigation

        bool Arrived() => !agent.pathPending && (!agent.hasPath || agent.remainingDistance <= ArriveDistance);

        void PickPatrolPoint()
        {
            waitingAtPoint = false;
            for (int i = 0; i < PointAttempts; i++)
            {
                if (TrySamplePatrolRing(out Vector3 point) && HasCompletePath(transform.position, point))
                {
                    agent.SetDestination(point);
                    return;
                }
            }
            // The ring is off the NavMesh (player at the edge of the map): walk toward the player instead.
            agent.SetDestination(RandomPointNear(target.Position, tension.Tuning.PatrolMinRadius));
        }

        bool TrySamplePatrolRing(out Vector3 point)
        {
            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            float radius = UnityEngine.Random.Range(tension.Tuning.PatrolMinRadius, tension.Tuning.PatrolMaxRadius);
            Vector3 candidate = target.Position + new Vector3(direction.x, 0f, direction.y) * radius;
            bool found = NavMesh.SamplePosition(candidate, out NavMeshHit hit, SampleRadius, agent.areaMask);
            point = hit.position;
            return found;
        }

        bool TryFindHiddenPatrolPoint(out Vector3 point)
        {
            for (int i = 0; i < PointAttempts; i++)
            {
                if (TrySamplePatrolRing(out point)
                    && !perception.IsVisibleToPlayer(point + Vector3.up)
                    && HasCompletePath(point, target.Position))
                    return true;
            }
            point = default;
            return false;
        }

        bool HasCompletePath(Vector3 from, Vector3 to)
        {
            var path = new NavMeshPath();
            return NavMesh.CalculatePath(from, to, agent.areaMask, path) && path.status == NavMeshPathStatus.PathComplete;
        }

        Vector3 RandomPointNear(Vector3 center, float radius)
        {
            for (int i = 0; i < PointAttempts; i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * radius;
                if (NavMesh.SamplePosition(center + new Vector3(offset.x, 0f, offset.y), out NavMeshHit hit, SampleRadius, agent.areaMask))
                    return hit.position;
            }
            return transform.position;
        }
    }
}
