using UnityEngine;
using UnityEngine.AI;

namespace Circus
{
    /// <summary>
    /// State machine: Dormant, Patrol, Investigate, Chase (+ Observe, driven by the PhaseDirector, and ForceDoor).
    /// External systems tweak behaviour through SpeedMult / AttackDistance / Passive.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(ZatanPerception))]
    public class ZatanBrain : MonoBehaviour
    {
        public static ZatanBrain I { get; private set; }
        /// <summary>When true, the PhaseDirector decides when the Zatan wakes up.</summary>
        public static bool ExternalActivation;

        [SerializeField] Transform[] patrolRoute;
        [SerializeField] GameObject[] eyes;
        [SerializeField] string activationMachineId = "M2_zatan";
        [SerializeField] float waypointTolerance = 1.2f;
        [SerializeField] float lookAroundSpeed = 100f;
        [SerializeField] float hidingOpenDistance = 1.7f;
        [SerializeField] float doorReachDistance = 2.0f;
        [SerializeField] float doorSearchRadius = 7f;
        [SerializeField] float forceNoiseInterval = 1f;

        public ZatanState State { get; private set; } = ZatanState.Dormant;
        public float SpeedMult { get; set; } = 1f;
        /// <summary>Only starts a chase if the seen player is closer than this (BRINCAR).</summary>
        public float AttackDistance { get; set; } = float.PositiveInfinity;
        /// <summary>Never chases nor catches (OBSERVAR).</summary>
        public bool Passive { get; set; }
        public bool IsFrozenByGaze { get; private set; }
        public NavMeshAgent Agent => agent;
        public Vector3 InvestigatePos => investigatePos;
        public Door ForcingDoor => forcingDoor;
        public Vector3 HomePos => homePos;

        NavMeshAgent agent;
        ZatanPerception perc;
        int patrolIndex;
        Vector3 investigatePos;
        float stateTimer, lostTimer, forceTimer, forceNoiseTimer;
        Door forcingDoor;
        ZatanState resumeState;
        Vector3 homePos; Quaternion homeRot;
        bool lookingAround;

        void Awake()
        {
            I = this;
            agent = GetComponent<NavMeshAgent>();
            perc = GetComponent<ZatanPerception>();
            homePos = transform.position; homeRot = transform.rotation;
            agent.enabled = false;
            ZatanLocator.Transform = transform;
            SetEyes(false);
        }

        void OnDestroy() { if (I == this) I = null; ZatanLocator.IsActiveAgent = false; }

        void OnEnable()
        {
            EventBus.Noise += OnNoise;
            EventBus.MachineRepaired += OnMachineRepaired;
            EventBus.PlayerSpotted += OnSpotted;
        }

        void OnDisable()
        {
            EventBus.Noise -= OnNoise;
            EventBus.MachineRepaired -= OnMachineRepaired;
            EventBus.PlayerSpotted -= OnSpotted;
        }

        void OnMachineRepaired(string id)
        {
            if (id != activationMachineId) return;
            SetEyes(true);
            if (!ExternalActivation) Activate(ZatanState.Patrol);
        }

        public void SetEyes(bool on) { foreach (var e in eyes) if (e) e.SetActive(on); }

        // ------------------------------------------------------------------ state control

        public void Activate(ZatanState s)
        {
            SetVisible(true);
            if (!agent.enabled)
            {
                agent.enabled = true;
                agent.Warp(transform.position);
            }
            agent.isStopped = false;
            ZatanLocator.IsActiveAgent = true;
            if (s == ZatanState.Patrol) patrolIndex = NearestWaypoint();
            SetState(s);
        }

        public void GoDormant()
        {
            if (agent.enabled) agent.enabled = false;
            transform.SetPositionAndRotation(homePos, homeRot);
            ZatanLocator.IsActiveAgent = false;
            SetState(ZatanState.Dormant);
        }

        /// <summary>Observe mode: stands still where placed (PhaseDirector teleports him around).</summary>
        public void EnterObserve()
        {
            if (agent.enabled) agent.enabled = false;
            ZatanLocator.IsActiveAgent = false;
            SetState(ZatanState.Observe);
        }

        public void PlaceAt(Vector3 pos, Vector3 lookAt)
        {
            if (agent.enabled) agent.Warp(pos); else transform.position = pos;
            var d = lookAt - pos; d.y = 0;
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
        }

        public void SetVisible(bool v)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = v;
            foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = v;
        }

        void SetState(ZatanState s)
        {
            if (s != ZatanState.Chase && s != ZatanState.Investigate) doorTarget = null;
            if (lookingAround) { agent.updateRotation = true; lookingAround = false; }
            State = s;
            stateTimer = 0f;
            lostTimer = 0f;
            if (s == ZatanState.Chase && GameState.I) GameState.I.HasBeenChased = true;
            EventBus.RaiseZatanState(s);
        }

        public void ForceState(ZatanState s)
        {
            switch (s)
            {
                case ZatanState.Dormant: GoDormant(); break;
                case ZatanState.Observe: EnterObserve(); break;
                case ZatanState.Investigate:
                    Activate(ZatanState.Patrol);
                    Investigate(FirstPersonController.I ? FirstPersonController.I.transform.position : transform.position);
                    break;
                default: Activate(s); break;
            }
        }

        public void Investigate(Vector3 pos)
        {
            if (!agent.enabled) return;
            doorTarget = null;
            investigatePos = NavMesh.SamplePosition(pos, out var hit, 4f, NavMesh.AllAreas) ? hit.position : pos;
            SetState(ZatanState.Investigate);
            agent.SetDestination(investigatePos);
        }

        // ------------------------------------------------------------------ senses

        void OnNoise(NoiseEvent e)
        {
            if (e.byZatan || !agent.enabled) return;
            if (State == ZatanState.Dormant || State == ZatanState.Observe || State == ZatanState.Chase || State == ZatanState.ForceDoor) return;
            if (e.source == NoiseSource.Carousel && !Tunables.I.carouselDistracts) return;
            float d = Vector3.Distance(transform.position, e.pos);
            if (d <= e.radius * Tunables.I.hearingMult) Investigate(e.pos);
        }

        void OnSpotted(Vector3 pos, string source)
        {
            if (!agent.enabled || State == ZatanState.Chase || State == ZatanState.ForceDoor) return;
            if (State == ZatanState.Dormant || State == ZatanState.Observe) return;
            Investigate(pos);
        }

        // ------------------------------------------------------------------ update

        void Update()
        {
            if (State == ZatanState.Dormant || State == ZatanState.Observe || !agent.enabled) return;
            var t = Tunables.I;
            var p = FirstPersonController.I;
            stateTimer += Time.deltaTime;

            float distToPlayer = p ? Vector3.Distance(transform.position, p.transform.position) : 999f;
            if (!Passive && perc.CanSeePlayer && distToPlayer <= AttackDistance && State != ZatanState.Chase && State != ZatanState.ForceDoor)
                SetState(ZatanState.Chase);

            bool doorHandled = (State == ZatanState.Chase || State == ZatanState.Investigate) && HandleLockedDoor();
            if (doorHandled && State != ZatanState.ForceDoor)
            {
                agent.speed = (State == ZatanState.Chase ? t.zatanChaseSpeed : t.zatanInvestigateSpeed) * SpeedMult;
                stateTimer = 0f;
            }
            else switch (State)
            {
                case ZatanState.Patrol: UpdatePatrol(t); break;
                case ZatanState.Investigate: UpdateInvestigate(t); break;
                case ZatanState.Chase: UpdateChase(t, p); break;
                case ZatanState.ForceDoor: UpdateForceDoor(t); break;
            }

            // "only moves when not observed" rule
            IsFrozenByGaze = t.moveOnlyWhenUnobserved && IsObservedByPlayer();
            agent.isStopped = IsFrozenByGaze || State == ZatanState.ForceDoor;
        }

        void UpdatePatrol(Tunables t)
        {
            agent.speed = t.zatanPatrolSpeed * SpeedMult;
            if (patrolRoute == null || patrolRoute.Length == 0) return;
            if (!agent.hasPath && !agent.pathPending) agent.SetDestination(patrolRoute[patrolIndex].position);
            if (!agent.pathPending && agent.remainingDistance < waypointTolerance)
            {
                patrolIndex = (patrolIndex + 1) % patrolRoute.Length;
                agent.SetDestination(patrolRoute[patrolIndex].position);
            }
        }

        void UpdateInvestigate(Tunables t)
        {
            agent.speed = t.zatanInvestigateSpeed * SpeedMult;
            if (agent.pathPending) return;
            if (agent.remainingDistance > waypointTolerance && agent.hasPath) { stateTimer = 0f; return; }
            // arrived: look around
            if (!lookingAround) { lookingAround = true; agent.updateRotation = false; agent.ResetPath(); }
            transform.Rotate(0f, Mathf.Sin(stateTimer * 1.7f) * lookAroundSpeed * Time.deltaTime, 0f);
            if (stateTimer > t.investigateLookTime)
            {
                patrolIndex = NearestWaypoint();
                SetState(ZatanState.Patrol);
                agent.ResetPath();
            }
        }

        void UpdateChase(Tunables t, FirstPersonController p)
        {
            agent.speed = t.zatanChaseSpeed * SpeedMult;
            var hide = HidingSpot.Occupied;
            if (hide != null && hide.ZatanSawEntry)
            {
                agent.SetDestination(hide.transform.position);
                if (Vector3.Distance(Flat(transform.position), Flat(hide.transform.position)) < hidingOpenDistance + hide.transform.localScale.x * 0.5f)
                    hide.Open();
                return;
            }
            if (perc.CanSeePlayer)
            {
                lostTimer = 0f;
                agent.SetDestination(p.transform.position);
            }
            else
            {
                lostTimer += Time.deltaTime;
                agent.SetDestination(perc.LastSeenPos);
                if (lostTimer > t.losePlayerTime) Investigate(perc.LastSeenPos);
            }
        }

        Door doorTarget;

        /// <summary>Path blocked by a locked door near the goal: walk to that door, then force it.</summary>
        bool HandleLockedDoor()
        {
            if (doorTarget != null)
            {
                if (!doorTarget.IsLocked) { doorTarget = null; return false; }
                if (Vector3.Distance(Flat(transform.position), Flat(doorTarget.Center)) < doorReachDistance)
                {
                    forcingDoor = doorTarget; doorTarget = null;
                    forceTimer = 0f; forceNoiseTimer = 0f;
                    resumeState = State;
                    SetState(ZatanState.ForceDoor);
                }
                return true;
            }
            if (agent.pathPending || agent.pathStatus != NavMeshPathStatus.PathPartial) return false;
            Vector3 goal = State == ZatanState.Chase ? perc.LastSeenPos : investigatePos;
            Door best = null; float bestD = doorSearchRadius;
            foreach (var d in Door.All)
            {
                if (!d.IsLocked) continue;
                float dd = Vector3.Distance(Flat(d.Center), Flat(goal));
                if (dd < bestD) { bestD = dd; best = d; }
            }
            if (best == null) return false;
            doorTarget = best;
            agent.SetDestination(best.Center);
            return true;
        }

        void UpdateForceDoor(Tunables t)
        {
            if (forcingDoor == null || !forcingDoor.IsLocked) { Resume(); return; }
            var d = forcingDoor.Center - transform.position; d.y = 0;
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 360 * Time.deltaTime);
            forceTimer += Time.deltaTime;
            forceNoiseTimer -= Time.deltaTime;
            if (forceNoiseTimer <= 0f)
            {
                forceNoiseTimer = forceNoiseInterval;
                NoiseSystem.Emit(forcingDoor.Center, t.doorForceNoiseRadius, NoiseSource.Door, true);
            }
            if (forceTimer >= t.doorForceTime) { forcingDoor.Break(); Resume(); }
        }

        void Resume()
        {
            forcingDoor = null;
            if (resumeState == ZatanState.Chase) SetState(ZatanState.Chase);
            else Investigate(investigatePos);
        }

        bool IsObservedByPlayer()
        {
            var p = FirstPersonController.I;
            if (!p || HidingSpot.Occupied != null) return false;
            var cam = p.Cam;
            Vector3 center = transform.position + Vector3.up * 1.2f;
            Vector3 vp = cam.WorldToViewportPoint(center);
            if (vp.z <= 0 || vp.x < -0.05f || vp.x > 1.05f || vp.y < -0.05f || vp.y > 1.05f) return false;
            if (vp.z > 40f) return false;
            return perc.HasLineOfSight(cam.transform.position, center, p.transform, transform);
        }

        int NearestWaypoint()
        {
            if (patrolRoute == null || patrolRoute.Length == 0) return 0;
            int best = 0; float bd = float.MaxValue;
            for (int i = 0; i < patrolRoute.Length; i++)
            {
                float d = Vector3.Distance(transform.position, patrolRoute[i].position);
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
    }
}
