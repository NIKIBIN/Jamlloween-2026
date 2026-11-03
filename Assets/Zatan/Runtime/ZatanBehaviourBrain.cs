using System;
using UnityEngine;
using UnityEngine.AI;

namespace Jamlloween.Zatan
{
    public enum ZatanBehaviourState { Patrol, Investigate, Spotted, Chase, Search, Caught, Vanished }

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

        public event Action<ZatanBehaviourState> StateChanged = delegate { };
        public event Action PlayerCaught = delegate { };

        public ZatanBehaviourState State { get; private set; } = ZatanBehaviourState.Patrol;
        public bool IsPresent => State != ZatanBehaviourState.Vanished;
        public float DistanceToPlayer => Vector3.Distance(transform.position, target.Position);
        public float CurrentSpeedLimit => agent.speed;
        public float CurrentSpeed => agent.velocity.magnitude;

        /// <summary>Scripted events hold him in the world while they run.</summary>
        public bool VanishSuspended { get; set; }

        float stateTime, lostTime, dwellTimer, repathTimer, vanishTimer, vanishDuration, vanishReadyAt;
        bool waitingAtPoint;

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
            EnterPatrol();
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
            if (stateTime < config.spotStareTime) return;

            agent.isStopped = false;
            agent.updateRotation = true;
            repathTimer = 0f;
            SetState(ZatanBehaviourState.Chase);
        }

        void UpdateChase()
        {
            float distance = DistanceToPlayer;
            agent.speed = config.ChaseSpeed(distance, target.RunSpeed);

            lostTime = perception.CanSeePlayer ? 0f : lostTime + Time.deltaTime;
            if (lostTime >= tension.Tuning.LoseTime)
            {
                SetState(ZatanBehaviourState.Search);
                agent.SetDestination(RandomPointNear(perception.LastSeenPosition, config.searchRadius));
                return;
            }

            if (repathTimer <= 0f)
            {
                repathTimer = config.repathInterval;
                agent.SetDestination(perception.CanSeePlayer ? target.Position : perception.LastSeenPosition);
            }

            float reach = Arrived() ? config.catchDistance + config.corneredExtraReach : config.catchDistance;
            if (distance <= reach && ZatanLineOfSight.IsClear(perception.Eye.position, target.ChestPosition, transform, target.transform))
            {
                agent.isStopped = true;
                // The catch sequence owns his transform until Respawn.
                agent.updatePosition = false;
                agent.updateRotation = false;
                SetState(ZatanBehaviourState.Caught);
                PlayerCaught();
            }
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
            if (stateTime < vanishDuration) return;
            if (TryFindHiddenPatrolPoint(out Vector3 point)) Reappear(point);
        }

        // ------------------------------------------------------------------ transitions

        void SetState(ZatanBehaviourState next)
        {
            State = next;
            stateTime = 0f;
            lostTime = 0f;
            vanishTimer = 0f;
            waitingAtPoint = false;
            StateChanged(next);
        }

        void EnterSpotted()
        {
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
