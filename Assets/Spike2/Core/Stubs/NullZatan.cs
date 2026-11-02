using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spike2.Core.Stubs
{
    /// <summary>No Zatan in the world. Calls are logged; callbacks complete immediately so beats don't hang.</summary>
    public class NullZatan : IZatan
    {
        public bool Exists => false;
        public ZatanAgentState State { get; private set; } = ZatanAgentState.Dormant;
        public ZatanPhase Phase { get; private set; } = ZatanPhase.Intro;
        public Transform Transform => null;
        public Vector3 Position { get; private set; } = new Vector3(0f, 0f, 1000f);
        public bool CanSeePlayer => false;
        public bool IsActive => false;
        public bool CatchEnabled { get; set; } = true;
        public float FootstepVolume { get; set; } = 1f;
        public bool MoveOnlyWhenUnobserved { get; set; }

        public void SetPhase(ZatanPhase phase) => Phase = phase;
        public void Activate(Vector3 pos, float yaw) { Position = pos; State = ZatanAgentState.Patrol; }
        public void Deactivate() => State = ZatanAgentState.Dormant;
        public void Teleport(Vector3 pos, float yaw) => Position = pos;
        public void SetVisible(bool visible) { }
        public void SetPatrol(IList<string> waypointIds) { }
        public void Investigate(Vector3 pos) => NullServices.Log($"zatan investigate {pos}");
        public void ReportPlayerPosition(Vector3 pos, string source) => NullServices.Log($"zatan told player at {pos} by {source}");
        public void ForceDoor(string doorId, Action<bool> onEnd = null) { NullServices.Log($"zatan force {doorId}"); onEnd?.Invoke(false); }
        public void Retreat(Vector3 to, float seconds) { }
        public void BeginScripted() => State = ZatanAgentState.Scripted;
        public void EndScripted(ZatanAgentState next = ZatanAgentState.Patrol) => State = next;
        public void GoTo(Vector3 pos, float speed, Action onArrive = null) { Position = pos; onArrive?.Invoke(); }
        public void LookAt(Vector3 worldPos) { }
        public void SetEyes(bool on) { }
    }

    public class NullZatanDirector : IZatanDirector
    {
        public bool Enabled { get; set; }
        public float Menace => 0f;
        public bool Suspended { get; set; }
        public void ConfigureAct(int act) { }
        public void Approach() { }
        public void RepositionOutOfSight(Vector3 pos, float yaw, Action onDone = null)
        {
            Services.Get<IZatan>().Teleport(pos, yaw);
            onDone?.Invoke();
        }
        public void Ambush(Vector3 pos, Action onDone = null)
        {
            Services.Get<IAmbushWarning>().Warn(pos);
            RepositionOutOfSight(pos, 0f, onDone);
        }
        public void ForceRetreat(float seconds) { }
        public bool TryBlackout() => false;
        public bool IsVisibleToPlayer(Vector3 worldPos)
        {
            var cam = Services.Get<IPlayer>().Camera;
            if (!cam) return false;
            var v = cam.WorldToViewportPoint(worldPos);
            return v.z > 0f && v.x > 0f && v.x < 1f && v.y > 0f && v.y < 1f;
        }
    }

    /// <summary>Forwards to spike 1 SecurityCamera when present.</summary>
    public class NullCameraNetwork : ICameraNetwork
    {
        readonly List<string> ids = new List<string>();
        readonly List<string> seeing = new List<string>();

        public IReadOnlyList<string> CameraIds
        {
            get { ids.Clear(); foreach (var c in Circus.SecurityCamera.All) ids.Add(c.id); return ids; }
        }
        public bool IsOn(string cameraId) { var c = Find(cameraId); return c && c.Activated; }
        public void SetOn(string cameraId, bool on) { var c = Find(cameraId); if (c) c.SetActivated(on); }
        public IReadOnlyList<string> SeeingPlayer
        {
            get { seeing.Clear(); foreach (var c in Circus.SecurityCamera.All) if (c.SeesPlayer) seeing.Add(c.id); return seeing; }
        }
        public bool ReportsToZatan { get; set; }
        public void AllLookAtPlayer(bool on) { }
        static Circus.SecurityCamera Find(string id) => Circus.SecurityCamera.All.Find(c => c.id == id) ?? MapRegistry.Get<Circus.SecurityCamera>(id);
    }
}
