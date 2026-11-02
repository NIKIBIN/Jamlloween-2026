using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Circus
{
    /// <summary>"Lista do patrão": hold Tab to see the machines in repair order and their status.</summary>
    public class TaskListUI : MonoBehaviour
    {
        /// <summary>Debug/tests: behave as if Tab were held.</summary>
        public static bool ForceTab;
        public static bool TabHeld => ForceTab || (Keyboard.current != null && Keyboard.current.tabKey.isPressed);

        [SerializeField] string title = "LISTA DO PATRÃO — TURNO DA NOITE";

        void OnGUI()
        {
            var kb = Keyboard.current;
            if (!TabHeld || DebugPanelState.IsOpen) return;
            float s = UIStyle.Scale, w = Screen.width;
            float pw = 520 * s;
            var machines = RepairableMachine.All.OrderBy(m => m.order).ThenBy(m => m.id).ToList();
            float ph = (70 + 30 * machines.Count) * s;
            float x = CircusMapUI.TabMapVisible ? Mathf.Max(10 * s, w * 0.44f - pw - 10 * s) : (w - pw) / 2;
            var r = new Rect(x, 90 * s, pw, ph);
            UIStyle.Fill(r, new Color(0.02f, 0.02f, 0.03f, 0.82f));
            UIStyle.OsdText(new Rect(r.x + 16 * s, r.y + 10 * s, r.width, 30 * s), title, UIStyle.Label);
            float y = r.y + 46 * s;
            foreach (var m in machines)
            {
                string box = m.IsRepaired ? "[X]" : m.Progress01 > 0f ? $"[{Mathf.RoundToInt(m.Progress01 * 100),2}%]" : "[ ]";
                string line = $"{box} {m.order}. {m.label.ToUpperInvariant(),-22} {m.zone.ToUpperInvariant()}";
                UIStyle.OsdText(new Rect(r.x + 16 * s, y, r.width, 28 * s), line, UIStyle.Label, m.IsRepaired ? 0.5f : 1f);
                y += 30 * s;
            }
        }
    }
}
