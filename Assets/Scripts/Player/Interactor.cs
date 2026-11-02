using UnityEngine;
using UnityEngine.InputSystem;

namespace Circus
{
    /// <summary>Camera raycast, shows a short prompt, E to interact, hold Q for secondary.</summary>
    [RequireComponent(typeof(FirstPersonController))]
    public class Interactor : MonoBehaviour
    {
        FirstPersonController fpc;
        IInteractable current;
        IHoldInteractable currentHold;
        float holdTimer;

        public FirstPersonController Player => fpc;

        void Awake() => fpc = GetComponent<FirstPersonController>();

        void Update()
        {
            current = null; currentHold = null;
            if (fpc.CurrentMode != FirstPersonController.Mode.Free || DebugPanelState.IsOpen) { holdTimer = 0f; return; }

            var cam = fpc.Cam.transform;
            if (Physics.Raycast(cam.position, cam.forward, out var hit, Tunables.I.interactDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                foreach (var mb in hit.collider.GetComponentsInParent<MonoBehaviour>())
                {
                    if (current == null && mb is IInteractable i && mb.enabled && i.CanInteract) current = i;
                    if (currentHold == null && mb is IHoldInteractable h && mb.enabled && h.CanHold) currentHold = h;
                }
            }

            var kb = Keyboard.current;
            if (kb == null) return;
            if (current != null && kb.eKey.wasPressedThisFrame) current.Interact(this);

            if (currentHold != null && kb.qKey.isPressed)
            {
                holdTimer += Time.deltaTime;
                if (holdTimer >= currentHold.HoldTime) { currentHold.HoldComplete(this); holdTimer = 0f; }
            }
            else holdTimer = 0f;
        }

        void OnGUI()
        {
            if (current == null && currentHold == null) return;
            string text = "";
            if (current != null) text += $"[E] {current.Prompt}";
            if (currentHold != null)
            {
                if (text.Length > 0) text += "    ";
                text += $"[Q] {currentHold.HoldPrompt}";
                if (holdTimer > 0f) text += $" {Mathf.RoundToInt(100f * holdTimer / currentHold.HoldTime)}%";
            }
            float w = Screen.width, h = Screen.height;
            UIStyle.OsdText(new Rect(0, h * 0.62f, w, 40 * UIStyle.Scale), text, UIStyle.Prompt);
            // crosshair dot
            UIStyle.Fill(new Rect(w / 2 - 2, h / 2 - 2, 4, 4), new Color(1, 1, 1, 0.7f));
        }
    }
}
