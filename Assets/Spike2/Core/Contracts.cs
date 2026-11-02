using System;
using System.Collections.Generic;
using UnityEngine;

// Contracts between the spike 2 systems (SPIKE2.md 2.1). Only the orchestrator edits this file.
// Need a change? Write a local adapter, keep going, and file a "pedido de contrato" in your report.

namespace Spike2.Core
{
    public enum ActId { Act0 = 0, Act1 = 1, Act2 = 2, Act3 = 3, Act4 = 4, Final = 5, Ended = 6 }

    /// <summary>The Zatan's phase (spike 1 INTRO/OBSERVAR/BRINCAR/CAÇAR + SHOW). Set by the acts.</summary>
    public enum ZatanPhase { Intro, Observe, Play, Hunt, Show }

    public enum ZatanAgentState { Dormant, Observe, Patrol, Investigate, Chase, ForceDoor, SearchHiding, Scripted, Retreat, Showman }

    public enum PanelAction { Insert, Connect, Turn, Pull }

    public enum CaptureNumber { None, MagicBox, Silence }

    // ====================================================================== player

    /// <summary>The player rig (agent B). Stub wraps the spike 1 FirstPersonController if present.</summary>
    public interface IPlayer
    {
        bool Exists { get; }
        Transform Transform { get; }
        Vector3 Position { get; }
        /// <summary>Eye position (camera).</summary>
        Vector3 HeadPosition { get; }
        /// <summary>Chest position: target of the Zatan's and cameras' line-of-sight raycasts.</summary>
        Vector3 ChestPosition { get; }
        Vector3 Forward { get; }
        Camera Camera { get; }
        bool IsHidden { get; }
        bool IsRunning { get; }
        bool IsCrouching { get; }
        bool FlashlightOn { get; }
        /// <summary>True while a panel, the clipboard, a capture or a cutscene owns the input.</summary>
        bool IsBusy { get; }
        /// <summary>Movement off (look still works). Used by the magic box capture, cutscenes.</summary>
        bool MovementLocked { get; set; }
        /// <summary>Busy (service panel, scripted camera): no movement, no mouse look, cursor visible and free.
        /// The caller owns the camera while busy and must call SetBusy(false) when done.</summary>
        void SetBusy(bool busy);
        void Teleport(Vector3 pos, float yaw);
        void SetFlashlight(bool on);
    }

    /// <summary>Pocket items (ids, no visual inventory). Agent B.</summary>
    public interface IPocket
    {
        IReadOnlyCollection<string> Items { get; }
        bool Has(string itemId);
        /// <summary>Put straight in the pocket (setup/debug). Hides the world pickup if there is one.</summary>
        void Give(string itemId);
        /// <summary>Consume (e.g. inserted in a panel). False if not in the pocket.</summary>
        bool Take(string itemId);
        /// <summary>Remove from the pocket and put the pickup back at its origin (capture cost). Publishes ItemReturned.</summary>
        void ReturnToOrigin(string itemId);
        /// <summary>Show/hide the world pickup of an item.</summary>
        void SetPickupActive(string itemId, bool active);
        /// <summary>Move the world pickup (and its origin) — e.g. the horse head goes to the drawn candidate.</summary>
        void MovePickup(string itemId, Vector3 pos);
    }

    /// <summary>Bottles: pick (max 2), throw with left click. Agent B.</summary>
    public interface IThrowables
    {
        int Held { get; }
        int Max { get; }
        void Give(int count);
        void Clear();
        /// <summary>A loose pickable bottle at runtime (silence capture).</summary>
        GameObject SpawnLoose(Vector3 pos);
        /// <summary>Show/hide a bottle_pile from the map (garrafas_cabine appears after M2).</summary>
        void SetPileActive(string pileId, bool active);
    }

    /// <summary>
    /// A map door (spike 1 Door, owned by agent B). Get it with MapRegistry.Resolve&lt;IDoor&gt;("d_gerador").
    /// Forcing logic (timers) lives with whoever forces: the Zatan agent (IZatan.ForceDoor) or an act script.
    /// </summary>
    public interface IDoor
    {
        string Id { get; }
        bool IsOpen { get; }
        bool IsLocked { get; }
        bool Lockable { get; }
        /// <summary>Item needed to open (key_item), or null.</summary>
        string RequiredItem { get; }
        Vector3 Center { get; }
        /// <summary>The player is holding E on this door right now.</summary>
        bool IsHeldByPlayer { get; }
        /// <summary>Someone is forcing it: shows the progress bar on the door.</summary>
        bool BeingForced { get; set; }
        float ForceProgress01 { get; set; }
        void SetOpen(bool open, bool byZatan);
        void SetLocked(bool locked);
        /// <summary>Forced open: unlocked and open.</summary>
        void Break();
        /// <summary>Knock sound + noise. strength01 scales the volume.</summary>
        void Knock(float strength01);
    }

    /// <summary>Clipboard on TAB (agent B). Page 1: boss list + Zatan lines + pocket. Page 2: manual.</summary>
    public interface IClipboard
    {
        bool IsUp { get; }
        /// <summary>False until the player picks it up in the trailer (beat 0.2).</summary>
        bool Available { get; set; }
        void AddBossLine(string lineId, string text);
        void SetLineDone(string lineId, bool done);
        /// <summary>Handwritten line in dark red (one per act, SPIKE2.md 5.5).</summary>
        void AddZatanLine(string text);
        /// <summary>Page 2 shows the service manual of this machine (layout symbols).</summary>
        void SetManual(string machineId);
        void Clear();
    }

    // ====================================================================== panel

    /// <summary>A service panel (agent C). Get with Services.Get&lt;IPanelSystem&gt;().Get("M3").</summary>
    public interface IMachine
    {
        string Id { get; }
        string LayoutId { get; }
        Transform Transform { get; }
        bool IsDone { get; }
        /// <summary>Player is inside the panel view.</summary>
        bool IsOpen { get; }
        int CurrentStep { get; }
        int StepCount { get; }
        /// <summary>False: [E] does nothing (and shows no prompt).</summary>
        bool Interactable { get; set; }
        /// <summary>Non-null: opening the panel shows only this message (e.g. "TRAVA: CAVALO 6").</summary>
        string BlockedMessage { get; set; }
        /// <summary>Swap the layout (e.g. M4 boot → M4 freeze → M4 boot). Publishes MachineDone(layout) when a layout completes.</summary>
        void SetLayout(string layoutId, bool resetProgress = true);
        void CompleteCurrentStep();
        /// <summary>Mark done. publish=false for world setup (no beats react).</summary>
        void ForceComplete(bool publish = true);
        /// <summary>Current step back to its start (capture cost, error).</summary>
        void ResetStep();
        void ResetAll();
        /// <summary>Kick the player out of the panel view.</summary>
        void Close();
    }

    public interface IPanelSystem
    {
        /// <summary>Never null: unknown ids get a null machine.</summary>
        IMachine Get(string machineId);
        IReadOnlyList<IMachine> All { get; }
        /// <summary>The panel the player is in, or null.</summary>
        IMachine Current { get; }
        /// <summary>Runtime machine on any object (magic box capture).</summary>
        IMachine Create(GameObject host, string machineId, string layoutId);
        /// <summary>Service manual of a machine's current layout for the clipboard's page 2: one line per step with
        /// the same symbols the panel shows (e.g. "1. ENCAIXAR ◇ FUSÍVEL"). Empty if unknown.</summary>
        string ManualFor(string machineId);
    }

    // ====================================================================== zatan

    /// <summary>The Zatan agent (agent D). Knows only what it perceives.</summary>
    public interface IZatan
    {
        bool Exists { get; }
        ZatanAgentState State { get; }
        ZatanPhase Phase { get; }
        Transform Transform { get; }
        Vector3 Position { get; }
        bool CanSeePlayer { get; }
        /// <summary>Walking agent (not Dormant, not hidden).</summary>
        bool IsActive { get; }
        /// <summary>Can capture on contact (off during scripted beats, god mode is separate).</summary>
        bool CatchEnabled { get; set; }
        /// <summary>1 = normal footsteps; the carousel music sets 0.2.</summary>
        float FootstepVolume { get; set; }
        /// <summary>Act 1 rule: only moves when not observed.</summary>
        bool MoveOnlyWhenUnobserved { get; set; }
        void SetPhase(ZatanPhase phase);
        /// <summary>Become a walking agent at pos (Patrol).</summary>
        void Activate(Vector3 pos, float yaw);
        /// <summary>Dormant: parked (in the booth) or hidden, no AI.</summary>
        void Deactivate();
        /// <summary>Raw warp. Callers must make sure it is out of view (prefer IZatanDirector.RepositionOutOfSight).</summary>
        void Teleport(Vector3 pos, float yaw);
        void SetVisible(bool visible);
        void SetPatrol(IList<string> waypointIds);
        void Investigate(Vector3 pos);
        /// <summary>Someone told him where the player is (camera, director). He investigates there.</summary>
        void ReportPlayerPosition(Vector3 pos, string source);
        /// <summary>Force a door; onEnd(true) = broke in, false = gave up (player held it) or interrupted.</summary>
        void ForceDoor(string doorId, Action<bool> onEnd = null);
        /// <summary>Walk away to `to` and leave the player alone for `seconds` (Retreat).</summary>
        void Retreat(Vector3 to, float seconds);
        /// <summary>Stop the AI: the caller drives with GoTo/LookAt/SetEyes/Teleport until EndScripted.</summary>
        void BeginScripted();
        void EndScripted(ZatanAgentState next = ZatanAgentState.Patrol);
        void GoTo(Vector3 pos, float speed, Action onArrive = null);
        /// <summary>Turn the head (separate from the body) toward a point.</summary>
        void LookAt(Vector3 worldPos);
        void SetEyes(bool on);
    }

    /// <summary>The director layer (agent D): always knows where the player is; sends the Zatan near, never on top.</summary>
    public interface IZatanDirector
    {
        bool Enabled { get; set; }
        /// <summary>0..100.</summary>
        float Menace { get; }
        /// <summary>True: no menace-driven events (scripted beats running).</summary>
        bool Suspended { get; set; }
        /// <summary>Anchors and patrol from zatan.act_zones, and the tools allowed from this act on.</summary>
        void ConfigureAct(int act);
        /// <summary>Send the Zatan to a point 12–25 m from the player, out of view.</summary>
        void Approach();
        /// <summary>Teleport only when both the Zatan and pos are out of the player's view (and of any watched monitor). onDone when done.</summary>
        void RepositionOutOfSight(Vector3 pos, float yaw, Action onDone = null);
        /// <summary>Warn (IAmbushWarning, ≥ 2 s) then place the Zatan at pos.</summary>
        void Ambush(Vector3 pos, Action onDone = null);
        void ForceRetreat(float seconds);
        /// <summary>Announce, then black out the player's zone (respects the minimum interval). False if not allowed now.</summary>
        bool TryBlackout();
        bool IsVisibleToPlayer(Vector3 worldPos);
    }

    /// <summary>The security camera network (agent D; spike 1 SecurityCamera underneath).</summary>
    public interface ICameraNetwork
    {
        IReadOnlyList<string> CameraIds { get; }
        bool IsOn(string cameraId);
        void SetOn(string cameraId, bool on);
        /// <summary>Cameras that see the player right now.</summary>
        IReadOnlyList<string> SeeingPlayer { get; }
        /// <summary>Report the player to the Zatan after 2 s in view (on from act 3).</summary>
        bool ReportsToZatan { get; set; }
        /// <summary>End of act 3: every camera turns to the player.</summary>
        void AllLookAtPlayer(bool on);
    }

    // ====================================================================== presentation

    /// <summary>Speakers and radio (agent E). Subtitles OSD style; the Zatan's prefixed with ♪.</summary>
    public interface IAnnouncer
    {
        bool IsSpeaking { get; }
        /// <summary>Zatan as the showman: nearest speaker + second nearest (echo).</summary>
        void Zatan(string text, Action onDone = null);
        /// <summary>The boss on the radio (voice + subtitle).</summary>
        void Boss(string text, Action onDone = null);
        void Laugh();
        /// <summary>"marcha", "carrossel"...</summary>
        void PlayMusic(string cueId);
        void StopMusic();
        /// <summary>Hot/cold: volume and clarity 0..1 of the current music.</summary>
        void SetMusicMix(float volume01, float clarity01);
    }

    /// <summary>Ambush warning (agent E: cutouts; interference/sound). Called by the director ≥ 2 s before.</summary>
    public interface IAmbushWarning
    {
        /// <summary>Warn about an ambush at pos. Returns true if a cutout was used. Publishes AmbushWarned.</summary>
        bool Warn(Vector3 ambushPos);
    }

    // ====================================================================== captures

    /// <summary>Captures (wave 2). The Zatan agent calls Capture(); the stub falls back to the spike 1 restart.</summary>
    public interface ICaptureDirector
    {
        bool InCapture { get; }
        /// <summary>true (default) = capture is a life-or-death number; false = restart the act.</summary>
        bool NumberMode { get; set; }
        /// <summary>Final: capture = death.</summary>
        bool DeathMode { get; set; }
        /// <summary>The player was caught. cause: "chase", "hiding", "scripted"...</summary>
        void Capture(string cause);
        void Force(CaptureNumber number);
    }
}
