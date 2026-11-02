using System.Linq;
using UnityEngine;

namespace Circus
{
    /// <summary>All machines repaired → get back to the trailer → "STOP ■" screen with the match time, then restart.</summary>
    public class ShiftEnd : MonoBehaviour
    {
        [SerializeField] string exitZone = "TR4_radio_patrao";
        [SerializeField] float restartAfter = 10f;

        bool allDone, ended;
        float endTime, matchSeconds;

        void OnEnable() => EventBus.MachineRepaired += OnRepaired;
        void OnDisable() => EventBus.MachineRepaired -= OnRepaired;

        void OnRepaired(string id)
        {
            if (allDone || RepairableMachine.All.Count == 0 || !RepairableMachine.All.All(m => m.IsRepaired)) return;
            allDone = true;
            EventBus.RaiseMessage("LISTA COMPLETA. VOLTE PARA O TRAILER.", 6f);
        }

        void Update()
        {
            if (ended)
            {
                if (Time.unscaledTime - endTime > restartAfter) GameState.I?.Restart();
                return;
            }
            if (!allDone || GameState.I == null || GameState.I.IsGameOver) return;
            var z = ZoneTrigger.Find(exitZone);
            if (z && z.PlayerInside)
            {
                ended = true;
                endTime = Time.unscaledTime;
                matchSeconds = Time.timeSinceLevelLoad;
                Debug.Log($"[ShiftEnd] Match time {matchSeconds:0}s");
                FirstPersonController.I.SetMode(FirstPersonController.Mode.Dead);
            }
        }

        void OnGUI()
        {
            if (!ended) return;
            GUI.depth = -60;
            UIStyle.Fill(new Rect(0, 0, Screen.width, Screen.height), Color.black);
            float h = Screen.height, s = UIStyle.Scale;
            UIStyle.OsdText(new Rect(0, h * 0.35f, Screen.width, 60 * s), "STOP ■", UIStyle.Big);
            UIStyle.OsdText(new Rect(0, h * 0.47f, Screen.width, 40 * s), "FIM DO TURNO", UIStyle.Center);
            UIStyle.OsdText(new Rect(0, h * 0.54f, Screen.width, 40 * s), $"DURAÇÃO DA FITA: {(int)(matchSeconds / 60)}:{(int)(matchSeconds % 60):00}", UIStyle.Center);
        }
    }
}
