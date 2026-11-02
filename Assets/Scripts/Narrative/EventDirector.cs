using UnityEngine;

namespace Circus
{
    /// <summary>
    /// Scripted events TR1–TR4 from circus_map.json, plus the door trap foretold by "O Enforcado".
    /// TR1 cabine vazia: M3 repaired + player enters the plaza zone → booth empties, short blackout, BRINCAR.
    /// TR2 luz pisca: after M2, first pass → ring lamps flicker 3 s, C2 turns to the player even broken.
    /// TR3 objeto movido: back at the spot 60 s after the first visit → the crate is 3 m away.
    /// TR4 rádio do patrão: entering the trailer after the first chase → the radio plays the boss laughing.
    /// </summary>
    public class EventDirector : MonoBehaviour
    {
        [SerializeField] string tr1 = "TR1_cabine_vazia", tr2 = "TR2_luz_pisca", tr3 = "TR3_objeto_movido", tr4 = "TR4_radio_patrao";
        [SerializeField] string doorTrapZone = "EV_control_room", doorTrapDoor = "D2_control_front";
        [SerializeField] Transform movableCrate;
        [SerializeField] Vector3 crateOffset = new Vector3(3f, 0f, 0f);
        [SerializeField] float tr3Delay = 60f;
        [SerializeField] float tr2FlickerSeconds = 3f;
        [SerializeField] string tr2Camera = "C2";
        [SerializeField] bool tr1Blackout = true;

        bool tr1Done, tr2Done, tr3Done, tr4Done, trapDone;
        float tr3FirstVisit = -1f;

        void Update()
        {
            var t = Tunables.I;
            var gs = GameState.I;
            if (!t.scriptedEvents || gs == null) return;

            // TR1
            var z1 = ZoneTrigger.Find(tr1);
            if (!tr1Done && z1 && z1.PlayerInside && RepairableMachine.IsDone("M3_generator"))
            {
                tr1Done = true;
                if (t.phaseDirector && gs.Phase == GamePhase.Observe)
                {
                    if (tr1Blackout && PowerGrid.I && PowerGrid.HasPower) { PowerGrid.I.NotifyLoadChanged(null); PowerGrid.I.TripOutage(); }
                    PhaseDirector.I?.EnterPlay();
                    EventBus.RaiseMessage("A cabine está vazia.", 4f);
                }
                EventBus.RaiseScriptedEvent(tr1);
            }

            // TR2
            var z2 = ZoneTrigger.Find(tr2);
            if (!tr2Done && z2 && z2.EnteredThisFrame && RepairableMachine.IsDone("M2_zatan"))
            {
                tr2Done = true;
                LightCircuit.Find("ring")?.Flicker(tr2FlickerSeconds);
                SecurityCamera.All.Find(c => c.id == tr2Camera)?.Glance(tr2FlickerSeconds + 2f);
                EventBus.RaiseScriptedEvent(tr2);
            }

            // TR3
            var z3 = ZoneTrigger.Find(tr3);
            if (!tr3Done && z3 && movableCrate)
            {
                if (z3.PlayerInside && tr3FirstVisit < 0f) tr3FirstVisit = Time.time;
                if (tr3FirstVisit >= 0f && !z3.PlayerInside && Time.time - tr3FirstVisit >= tr3Delay && !Visible(movableCrate.position))
                {
                    tr3Done = true;
                    movableCrate.position += crateOffset;
                    EventBus.RaiseScriptedEvent(tr3);
                }
            }

            // TR4
            var z4 = ZoneTrigger.Find(tr4);
            if (!tr4Done && z4 && z4.EnteredThisFrame && gs.HasBeenChased)
            {
                tr4Done = true;
                RadioMessages.I?.PlayLaugh();
                EventBus.RaiseScriptedEvent(tr4);
            }

            // Door trap (O Enforcado): first time inside the control room during BRINCAR/CAÇAR
            var zt = ZoneTrigger.Find(doorTrapZone);
            if (!trapDone && zt && zt.PlayerInside && (gs.Phase == GamePhase.Play || gs.Phase == GamePhase.Hunt) && t.lockableDoors)
            {
                var d = Door.All.Find(x => x.id == doorTrapDoor);
                if (d)
                {
                    trapDone = true;
                    d.SetOpen(false, true);
                    d.SetLocked(true);
                    EventBus.RaiseMessage("*BLAM* — a porta da frente trancou sozinha.", 4f);
                    EventBus.RaiseScriptedEvent("EV_door_trap");
                }
            }
        }

        public void Fire(string id)
        {
            switch (id)
            {
                case "TR1": tr1Done = true; PhaseDirector.I?.EnterPlay(); break;
                case "TR2": tr2Done = true; LightCircuit.Find("ring")?.Flicker(tr2FlickerSeconds); SecurityCamera.All.Find(c => c.id == tr2Camera)?.Glance(5f); break;
                case "TR3": if (movableCrate && !tr3Done) { tr3Done = true; movableCrate.position += crateOffset; } break;
                case "TR4": tr4Done = true; RadioMessages.I?.PlayLaugh(); break;
            }
        }

        static bool Visible(Vector3 p)
        {
            var fpc = FirstPersonController.I;
            if (!fpc) return false;
            var vp = fpc.Cam.WorldToViewportPoint(p);
            return vp.z > 0 && vp.x > 0 && vp.x < 1 && vp.y > 0 && vp.y < 1 && vp.z < 30f;
        }
    }
}
