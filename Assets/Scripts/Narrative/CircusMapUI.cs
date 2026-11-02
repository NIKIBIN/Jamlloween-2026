using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Circus
{
    /// <summary>
    /// Old printed paper map, baked from circus_map.json by the builder (texture + zone labels).
    /// Overlays at runtime: "VOCÊ ESTÁ AQUI" (position + heading) and the boss list machines with status.
    /// Shown full screen from the trailer wall board, or beside the boss list on Tab ("Mapa no Tab").
    /// </summary>
    public class CircusMapUI : MonoBehaviour
    {
        public static CircusMapUI I { get; private set; }
        public static bool TabMapVisible => I != null && Tunables.I.mapOnTab && TaskListUI.TabHeld
                                            && !DebugPanelState.IsOpen && !I.fullscreen;

        [System.Serializable]
        public struct Label { public string text; public Vector2 worldXZ; }

        [SerializeField] Texture2D mapTexture;
        [SerializeField] Vector2 worldMin = new Vector2(-42, -32);
        [SerializeField] Vector2 worldSize = new Vector2(84, 76);
        [SerializeField] Label[] labels;
        [SerializeField] Color ink = new Color(0.16f, 0.12f, 0.09f);
        [SerializeField] Color pending = new Color(0.75f, 0.08f, 0.05f);
        [SerializeField] Color repaired = new Color(0.10f, 0.42f, 0.16f);
        [SerializeField] Color you = new Color(0.85f, 0.05f, 0.05f);

        bool fullscreen;
        int openedFrame;
        GUIStyle inkStyle, inkSmall, inkTitle;

        void Awake() => I = this;
        void OnDestroy() { if (I == this) I = null; }

        public void OpenFullscreen()
        {
            fullscreen = true;
            openedFrame = Time.frameCount;
            var p = FirstPersonController.I;
            p.SetMode(FirstPersonController.Mode.Busy);
            p.LookLocked = true;
        }

        public void Close()
        {
            fullscreen = false;
            var p = FirstPersonController.I;
            if (p && p.CurrentMode == FirstPersonController.Mode.Busy) p.SetMode(FirstPersonController.Mode.Free);
        }

        void Update()
        {
            if (!fullscreen) return;
            var kb = Keyboard.current;
            var p = FirstPersonController.I;
            if (p == null || p.CurrentMode == FirstPersonController.Mode.Dead) { fullscreen = false; return; }
            if (kb != null && Time.frameCount != openedFrame && (kb.escapeKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame))
                Close();
        }

        void OnGUI()
        {
            if (mapTexture == null) return;
            float s = UIStyle.Scale, w = Screen.width, h = Screen.height;
            if (fullscreen)
            {
                GUI.depth = -40;
                UIStyle.Fill(new Rect(0, 0, w, h), new Color(0, 0, 0, 0.92f));
                float mh = h * 0.86f;
                var r = Fit(new Rect((w - mh * Aspect) / 2, h * 0.05f, mh * Aspect, mh));
                DrawMap(r);
                UIStyle.OsdText(new Rect(0, h - 40 * s, w, 30 * s), "[E/ESC] FECHAR MAPA", UIStyle.Center, 0.8f);
            }
            else if (TabMapVisible)
            {
                float mh = Mathf.Min(h * 0.8f, (w * 0.52f) / Aspect);
                var r = new Rect(w * 0.45f, 90 * s, mh * Aspect, mh);
                DrawMap(r);
            }
        }

        float Aspect => mapTexture.width / (float)mapTexture.height;
        Rect Fit(Rect r) => r;

        Vector2 ToScreen(Rect r, Vector2 xz)
        {
            float u = (xz.x - worldMin.x) / worldSize.x;
            float v = (xz.y - worldMin.y) / worldSize.y;
            return new Vector2(r.x + u * r.width, r.y + (1f - v) * r.height);
        }

        void EnsureStyles()
        {
            if (inkStyle != null) return;
            inkStyle = new GUIStyle(UIStyle.Center) { wordWrap = false, fontStyle = FontStyle.Bold };
            inkSmall = new GUIStyle(UIStyle.Label) { wordWrap = false, alignment = TextAnchor.MiddleLeft };
            inkTitle = new GUIStyle(UIStyle.Label) { wordWrap = false, fontStyle = FontStyle.Bold };
        }

        void InkLabel(Rect rect, string text, GUIStyle st, Color c, int size)
        {
            st.fontSize = size;
            st.normal.textColor = c;
            GUI.Label(rect, text, st);
        }

        void DrawMap(Rect r)
        {
            EnsureStyles();
            float k = r.height / 900f;   // text scale relative to map size
            UIStyle.Fill(new Rect(r.x + 6, r.y + 8, r.width, r.height), new Color(0, 0, 0, 0.5f)); // drop shadow
            GUI.DrawTexture(r, mapTexture);

            InkLabel(new Rect(r.x + 14 * k, r.y + 8 * k, r.width, 40 * k), "CIRCO — PLANTA DO TERRENO", inkTitle, ink, Mathf.RoundToInt(26 * k));
            InkLabel(new Rect(r.xMax - 90 * k, r.y + 10 * k, 80 * k, 60 * k), "N\n↑", inkStyle, ink, Mathf.RoundToInt(26 * k));

            foreach (var l in labels)
            {
                var p = ToScreen(r, l.worldXZ);
                InkLabel(new Rect(p.x - 150 * k, p.y - 14 * k, 300 * k, 28 * k), l.text, inkStyle, ink, Mathf.RoundToInt(19 * k));
            }

            foreach (var m in RepairableMachine.All.OrderBy(x => x.order))
            {
                var p = ToScreen(r, new Vector2(m.transform.position.x, m.transform.position.z));
                var c = m.IsRepaired ? repaired : pending;
                string tag = m.id.Substring(0, 2);
                InkLabel(new Rect(p.x - 12 * k, p.y - 14 * k, 24 * k, 28 * k), m.IsRepaired ? "✓" : "●", inkStyle, c, Mathf.RoundToInt(24 * k));
                InkLabel(new Rect(p.x + 12 * k, p.y - 13 * k, 200 * k, 26 * k), tag, inkSmall, c, Mathf.RoundToInt(17 * k));
            }

            var player = FirstPersonController.I;
            if (player)
            {
                var pp = ToScreen(r, new Vector2(player.transform.position.x, player.transform.position.z));
                float yaw = player.transform.eulerAngles.y;   // 0 = north (+Z) = up on the map
                var old = GUI.matrix;
                GUIUtility.RotateAroundPivot(yaw, pp);
                InkLabel(new Rect(pp.x - 20 * k, pp.y - 22 * k, 40 * k, 40 * k), "▲", inkStyle, you, Mathf.RoundToInt(34 * k));
                GUI.matrix = old;
                bool blink = Mathf.Repeat(Time.unscaledTime, 1f) < 0.7f;
                if (blink) InkLabel(new Rect(pp.x + 18 * k, pp.y - 30 * k, 260 * k, 26 * k), "VOCÊ ESTÁ AQUI", inkSmall, you, Mathf.RoundToInt(17 * k));
            }

            // legend
            float ly = r.yMax - 34 * k;
            InkLabel(new Rect(r.x + 14 * k, ly, 200 * k, 26 * k), "● PENDENTE", inkSmall, pending, Mathf.RoundToInt(17 * k));
            InkLabel(new Rect(r.x + 170 * k, ly, 220 * k, 26 * k), "✓ CONSERTADA", inkSmall, repaired, Mathf.RoundToInt(17 * k));
            InkLabel(new Rect(r.x + 350 * k, ly, 240 * k, 26 * k), "▲ VOCÊ", inkSmall, you, Mathf.RoundToInt(17 * k));
        }
    }
}
