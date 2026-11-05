using UnityEngine;

namespace Jamlloween.Core
{
    /// <summary>Shared IMGUI styles with a monospaced OS font.</summary>
    public static class UIStyle
    {
        static Font _font;
        static GUIStyle _label, _big, _center, _prompt;
        static Texture2D _white;

        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.Load<Font>("Fonts/OSD");   // DejaVu Sans Mono (free license)
                if (_font == null) _font = GUI.skin.font;
                return _font;
            }
        }

        public static Texture2D White
        {
            get
            {
                if (_white == null) { _white = new Texture2D(1, 1); _white.SetPixel(0, 0, Color.white); _white.Apply(); }
                return _white;
            }
        }

        public static float Scale => Mathf.Max(0.75f, Screen.height / 720f);

        static GUIStyle Make(int size, TextAnchor anchor)
        {
            var s = new GUIStyle(GUI.skin.label) { font = Font, fontSize = Mathf.RoundToInt(size * Scale), alignment = anchor, wordWrap = true, richText = true };
            s.normal.textColor = Color.white;
            return s;
        }

        public static GUIStyle Label => _label ??= Make(18, TextAnchor.UpperLeft);
        public static GUIStyle Big => _big ??= Make(34, TextAnchor.MiddleCenter);
        public static GUIStyle Center => _center ??= Make(20, TextAnchor.MiddleCenter);
        public static GUIStyle Prompt => _prompt ??= Make(20, TextAnchor.MiddleCenter);

        public static void ResetCache() { _label = _big = _center = _prompt = null; }

        /// <summary>Text with a slight chromatic offset (red/cyan) like a VCR OSD.</summary>
        public static void OsdText(Rect r, string text, GUIStyle style, float alpha = 1f)
        {
            var old = style.normal.textColor;
            float o = Mathf.Max(1f, 1.5f * Scale);
            style.normal.textColor = new Color(1f, 0.15f, 0.15f, 0.55f * alpha);
            GUI.Label(new Rect(r.x - o, r.y, r.width, r.height), text, style);
            style.normal.textColor = new Color(0.1f, 0.9f, 1f, 0.55f * alpha);
            GUI.Label(new Rect(r.x + o, r.y, r.width, r.height), text, style);
            style.normal.textColor = new Color(1f, 1f, 1f, alpha);
            GUI.Label(r, text, style);
            style.normal.textColor = old;
        }

        public static void Fill(Rect r, Color c)
        {
            var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, White); GUI.color = old;
        }
    }
}
