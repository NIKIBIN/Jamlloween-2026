using UnityEngine;
using UnityEngine.InputSystem;

namespace Circus
{
    /// <summary>Draws the active repair session (progress, held key or key sequence).</summary>
    public class RepairMinigameUI : MonoBehaviour
    {
        [SerializeField] Color barColor = new Color(0.85f, 0.85f, 0.8f, 0.9f);
        [SerializeField] Color wrongColor = new Color(1f, 0.2f, 0.15f, 0.9f);

        static string Glyph(Key k) => k switch
        {
            Key.UpArrow => "↑", Key.DownArrow => "↓", Key.LeftArrow => "←", Key.RightArrow => "→", _ => "?"
        };

        static string Alt(Key k) => k switch
        {
            Key.UpArrow => "W", Key.DownArrow => "S", Key.LeftArrow => "A", Key.RightArrow => "D", _ => "?"
        };

        void OnGUI()
        {
            var m = RepairableMachine.Active;
            if (m == null || DebugPanelState.IsOpen) return;
            float s = UIStyle.Scale, w = Screen.width, h = Screen.height;
            float pw = 460 * s, ph = 150 * s;
            var r = new Rect((w - pw) / 2, h - ph - 60 * s, pw, ph);
            UIStyle.Fill(r, new Color(0, 0, 0, 0.6f));

            UIStyle.OsdText(new Rect(r.x, r.y + 6 * s, r.width, 28 * s), $"CONSERTANDO: {m.label.ToUpperInvariant()}", UIStyle.Center);

            var bar = new Rect(r.x + 20 * s, r.y + 42 * s, r.width - 40 * s, 14 * s);
            UIStyle.Fill(bar, new Color(1, 1, 1, 0.15f));
            UIStyle.Fill(new Rect(bar.x, bar.y, bar.width * m.Progress01, bar.height), m.WrongFlash > 0 ? wrongColor : barColor);

            string body;
            if (m.SessionVariant == RepairVariant.Hold)
            {
                body = Keyboard.current != null && Keyboard.current.eKey.isPressed ? "SEGURANDO [E]..." : "SEGURE [E]";
                body += "\n<size=" + Mathf.RoundToInt(14 * s) + ">[MOVER/ESC] SAIR</size>";
            }
            else
            {
                body = "";
                for (int i = 0; i < m.SeqKeys.Length; i++)
                {
                    string g = $"{Glyph(m.SeqKeys[i])}{Alt(m.SeqKeys[i])}";
                    body += i < m.SeqIndex ? $"<color=#666666>{g}</color> " : i == m.SeqIndex ? $"<b>[{g}]</b> " : $"{g} ";
                }
                body += $"\n<size={Mathf.RoundToInt(14 * s)}>ETAPA {m.SeqStage + 1}/{m.SequenceStages}   [ESC] SAIR</size>";
            }
            UIStyle.OsdText(new Rect(r.x, r.y + 64 * s, r.width, 80 * s), body, UIStyle.Center);
        }
    }
}
