using UnityEngine;

// Events that cross systems (SPIKE2.md 2.1). Defined once here; only the orchestrator edits this file.
// Publish with Bus.Publish(new MachineDone { machineId = "M3", layoutId = "M3" }).

namespace Spike2.Core
{
    // ---------------------------------------------------------------- panel / machines
    public struct PanelOpened { public string machineId; public string layoutId; }
    public struct PanelClosed { public string machineId; public string layoutId; }
    public struct MachineStepDone { public string machineId; public string layoutId; public int step; public int stepCount; }
    /// <summary>Wrong slot/terminal: spark, 18 m noise, step resets.</summary>
    public struct MachineError { public string machineId; public string layoutId; public int step; public Vector3 pos; }
    /// <summary>A layout was completed (machine done, or a sub-layout such as the M4 freeze).</summary>
    public struct MachineDone { public string machineId; public string layoutId; }

    // ---------------------------------------------------------------- player, items, bottles, hiding, doors
    public struct ItemPicked { public string itemId; public Vector3 pos; }
    public struct ItemReturned { public string itemId; }
    public struct BottlePicked { public string pileId; public int held; }
    public struct BottleThrown { public Vector3 from; public Vector3 dir; }
    public struct BottleImpact { public Vector3 pos; public float radius; }
    public struct PlayerHid { public string spotId; public bool seenEntering; }
    public struct PlayerLeftHiding { public string spotId; }
    /// <summary>The Zatan opened a hiding spot (playerInside = caught there).</summary>
    public struct HidingOpened { public string spotId; public bool playerInside; }
    public struct DoorForceStarted { public string doorId; }
    /// <summary>End of a forcing attempt. broke = the door gave in; false = he gave up.</summary>
    public struct DoorForced { public string doorId; public bool broke; }
    public struct DoorLockChanged { public string doorId; public bool locked; }
    public struct ClipboardRaised { public bool up; }
    public struct TriggerEntered { public string triggerId; }
    public struct TriggerExited { public string triggerId; }

    // ---------------------------------------------------------------- zatan, director, cameras
    public struct ZatanStateChanged { public ZatanAgentState from; public ZatanAgentState to; }
    public struct ChaseStarted { public Vector3 zatanPos; public Vector3 playerPos; }
    /// <summary>how: "lost", "caught", "hid", "retreat", "scripted".</summary>
    public struct ChaseEnded { public float duration; public string how; }
    public struct CameraSpotted { public string cameraId; public Vector3 playerPos; }
    public struct ZoneBlackout { public string zoneId; public string circuitId; public float duration; public bool started; }
    /// <summary>kind: "interference", "sound", "cutout".</summary>
    public struct AmbushWarned { public Vector3 pos; public string kind; }
    /// <summary>withWarning=false is a design bug (logged by telemetry).</summary>
    public struct AmbushTriggered { public Vector3 pos; public bool withWarning; }
    public struct MenaceRetreat { public float seconds; }

    // ---------------------------------------------------------------- presentation
    /// <summary>speaker: "zatan" or "boss".</summary>
    public struct Announced { public string speaker; public string text; }
    public struct TarotRead { public string cardId; }

    // ---------------------------------------------------------------- acts, captures
    public struct ActStarted { public ActId act; }
    public struct ActEnded { public ActId act; }
    public struct BeatStarted { public ActId act; public string beat; }
    public struct PlayerCaught { public string cause; public Vector3 pos; public ActId act; }
    public struct CaptureStarted { public CaptureNumber number; public int attempt; }
    public struct CaptureResolved { public CaptureNumber number; public bool survived; public int attempts; }
}
