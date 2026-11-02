using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Circus
{
    /// <summary>After M4: watch any camera feed (arrows switch, Esc leaves). Black and white, noisy, "CAM 0X" / "REC ●".</summary>
    [RequireComponent(typeof(RepairableMachine))]
    public class MonitorStation : MonoBehaviour, IInteractable
    {
        public static MonitorStation Viewing { get; private set; }

        [SerializeField] Material feedMaterial;

        RepairableMachine machine;
        SecurityCamera[] cams;
        int index;
        int openedFrame;

        void Awake() => machine = GetComponent<RepairableMachine>();
        void OnDisable() { if (Viewing == this) Close(); }

        public bool CanInteract => machine.IsRepaired && Viewing == null;
        public string Prompt => PowerGrid.HasPower ? "VER MONITORES" : "MONITORES (SEM ENERGIA)";

        public void Interact(Interactor who)
        {
            if (!PowerGrid.HasPower) return;
            cams = SecurityCamera.All.OrderBy(c => c.id).ToArray();
            if (cams.Length == 0) return;
            Viewing = this;
            openedFrame = Time.frameCount;
            FirstPersonController.I.SetMode(FirstPersonController.Mode.Busy);
            FirstPersonController.I.LookLocked = true;
            Select(index);
        }

        void Select(int i)
        {
            foreach (var c in cams) c.SetFeedActive(false);
            index = (i + cams.Length) % cams.Length;
            cams[index].SetFeedActive(true);
        }

        void Close()
        {
            if (cams != null) foreach (var c in cams) if (c) c.SetFeedActive(false);
            Viewing = null;
            var p = FirstPersonController.I;
            if (p && p.CurrentMode == FirstPersonController.Mode.Busy) p.SetMode(FirstPersonController.Mode.Free);
        }

        void Update()
        {
            if (Viewing != this) return;
            var kb = Keyboard.current;
            var p = FirstPersonController.I;
            if (!PowerGrid.HasPower || p == null || p.CurrentMode == FirstPersonController.Mode.Dead) { Close(); return; }
            if (kb == null || DebugPanelState.IsOpen) return;
            if (kb.escapeKey.wasPressedThisFrame || (kb.eKey.wasPressedThisFrame && Time.frameCount != openedFrame)) { Close(); return; }
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) Select(index + 1);
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Select(index - 1);
        }

        void OnGUI()
        {
            if (Viewing != this || cams == null) return;
            float w = Screen.width, h = Screen.height, s = UIStyle.Scale;
            UIStyle.Fill(new Rect(0, 0, w, h), Color.black);
            float fh = h * 0.86f, fw = fh * 4f / 3f;
            var r = new Rect((w - fw) / 2, (h - fh) / 2, fw, fh);
            var cam = cams[index];
            if (Event.current.type == EventType.Repaint && cam.Feed)
            {
                if (feedMaterial) { feedMaterial.SetFloat("_FeedTime", Time.time); Graphics.DrawTexture(r, cam.Feed, feedMaterial); }
                else GUI.DrawTexture(r, cam.Feed);
            }
            string camLabel = $"CAM {cam.id.Substring(1).PadLeft(2, '0')}";
            UIStyle.OsdText(new Rect(r.x + 20 * s, r.y + 14 * s, 300 * s, 40 * s), camLabel, UIStyle.Big);
            if (Mathf.Repeat(Time.time, 1f) < 0.6f)
                UIStyle.OsdText(new Rect(r.xMax - 200 * s, r.y + 14 * s, 180 * s, 40 * s), "REC ●", UIStyle.Big);
            UIStyle.OsdText(new Rect(r.x, r.yMax - 40 * s, r.width, 30 * s), "[←/→] TROCAR   [ESC] SAIR", UIStyle.Center, 0.8f);
        }
    }
}
