using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Circus
{
    /// <summary>
    /// Wardrobe / curtained stall. E to enter and leave. Inside, the Zatan cannot see the player,
    /// unless he saw the player going in: then he walks up and opens it.
    /// </summary>
    public class HidingSpot : MonoBehaviour, IInteractable
    {
        public static readonly List<HidingSpot> All = new List<HidingSpot>();
        public static HidingSpot Occupied { get; private set; }

        public string id;
        [SerializeField] float eyeHeight = 1.55f;
        [SerializeField] float slitHeight = 0.12f;

        public bool ZatanSawEntry { get; private set; }
        Vector3 entryPos;
        float entryYaw;
        int enteredFrame;

        void OnEnable() => All.Add(this);
        void OnDisable() { All.Remove(this); if (Occupied == this) Occupied = null; }

        public bool CanInteract => Occupied == null;
        public string Prompt => "ESCONDER";

        public void Interact(Interactor who)
        {
            var p = FirstPersonController.I;
            entryPos = p.transform.position;
            entryYaw = p.transform.eulerAngles.y;
            ZatanSawEntry = ZatanPerception.I != null && ZatanPerception.I.SeenRecently(Tunables.I.sawHidingGrace);
            Occupied = this;
            enteredFrame = Time.frameCount;
            p.SetMode(FirstPersonController.Mode.Hidden);
            // stand inside, looking out toward where the player came from
            var lookDir = entryPos - transform.position; lookDir.y = 0;
            float yaw = lookDir.sqrMagnitude > 0.01f ? Quaternion.LookRotation(lookDir).eulerAngles.y : entryYaw;
            p.Teleport(new Vector3(transform.position.x, transform.position.y - transform.localScale.y / 2 + eyeHeight - 1.65f, transform.position.z), yaw);
            p.SetPitch(0);
        }

        void Update()
        {
            if (Occupied != this) return;
            var kb = Keyboard.current;
            var p = FirstPersonController.I;
            if (p == null || p.CurrentMode == FirstPersonController.Mode.Dead) { Occupied = null; return; }
            if (kb != null && kb.eKey.wasPressedThisFrame && Time.frameCount != enteredFrame && !DebugPanelState.IsOpen) Exit();
        }

        public void Exit()
        {
            if (Occupied != this) return;
            Occupied = null;
            ZatanSawEntry = false;
            var p = FirstPersonController.I;
            p.Teleport(entryPos);
            p.SetMode(FirstPersonController.Mode.Free);
        }

        /// <summary>The Zatan opens the door.</summary>
        public void Open()
        {
            if (Occupied != this) return;
            Exit();
            GameState.I?.Caught();
        }

        void OnGUI()
        {
            if (Occupied != this) return;
            float w = Screen.width, h = Screen.height, slit = h * slitHeight;
            var c = new Color(0.01f, 0.01f, 0.01f, 0.97f);
            UIStyle.Fill(new Rect(0, 0, w, h / 2 - slit / 2), c);
            UIStyle.Fill(new Rect(0, h / 2 + slit / 2, w, h / 2 - slit / 2), c);
            UIStyle.OsdText(new Rect(0, h * 0.75f, w, 30 * UIStyle.Scale), "ESCONDIDO   [E] SAIR", UIStyle.Prompt, 0.8f);
        }
    }
}
