using System.Collections.Generic;
using UnityEngine;

namespace Circus
{
    /// <summary>Subtitle line for EventBus.Message (radio, tarot, system messages).</summary>
    public class MessageUI : MonoBehaviour
    {
        [SerializeField] int maxLines = 3;

        struct Line { public string text; public float until; }
        readonly List<Line> lines = new List<Line>();

        void OnEnable() => EventBus.Message += OnMessage;
        void OnDisable() => EventBus.Message -= OnMessage;

        void OnMessage(string text, float seconds)
        {
            lines.Add(new Line { text = text, until = Time.time + seconds });
            while (lines.Count > maxLines) lines.RemoveAt(0);
        }

        void OnGUI()
        {
            lines.RemoveAll(l => Time.time > l.until);
            if (lines.Count == 0) return;
            float s = UIStyle.Scale, w = Screen.width, h = Screen.height;
            float y = h * 0.80f;
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                var size = UIStyle.Center.CalcHeight(new GUIContent(lines[i].text), w * 0.8f);
                var r = new Rect(w * 0.1f, y - size, w * 0.8f, size);
                UIStyle.Fill(new Rect(r.x, r.y - 2, r.width, r.height + 4), new Color(0, 0, 0, 0.55f));
                float a = Mathf.Clamp01(lines[i].until - Time.time);
                UIStyle.OsdText(r, lines[i].text, UIStyle.Center, a);
                y -= size + 8 * s;
            }
        }
    }
}
