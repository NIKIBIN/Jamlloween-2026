using HeyHeyThere.PSXCarnivalKit;
using Jamlloween.Flashlight;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jamlloween.Zatan.KitPlayer
{
    /// <summary>
    /// The PSX kit player hiding in trash cans: E on a standing can gets in, E again gets out. Inside, the camera
    /// peeks out of the gap under the lifted lid, the player can't move and the flashlight stays off (it comes back
    /// on when they climb out if it was on). Tipped over by Zatan, the player lands next to the can.
    /// </summary>
    public sealed class LevelWalkerHiding : MonoBehaviour
    {
        [SerializeField] ZatanConfig config;
        [SerializeField] ZatanTarget target;
        [SerializeField] LevelWalker walker;
        [SerializeField] CharacterController body;
        [SerializeField] Transform view;
        [SerializeField] PlayerFlashlight flashlight;
        [Tooltip("Other player components that read E or the view and must sleep while hidden (e.g. PlayerInteractor).")]
        [SerializeField] Behaviour[] disableWhileHidden;
        [SerializeField] float reach = 2.2f;
        [Tooltip("Degrees the hidden player can look left and right, and up and down, through the gap.")]
        [SerializeField] Vector2 peekRange = new Vector2(55f, 20f);
        [Tooltip("Fraction of the screen the inside of the can covers above and below the gap.")]
        [SerializeField, Range(0f, 0.45f)] float gapCover = 0.34f;

        public ZatanHidingSpot Spot { get; private set; }
        public bool IsHidden => Spot != null;
        public ZatanHidingSpot Aimed { get; private set; }

        Texture2D black;
        GUIStyle promptStyle;
        bool flashlightWasOn;
        float peekYaw, peekPitch, rootAboveGround, controlBackAt = -1f;
        int controlBackFrame = -1;

        void Awake()
        {
            if (config == null) throw new MissingReferenceException(nameof(config));
            if (target == null) throw new MissingReferenceException(nameof(target));
            if (walker == null) throw new MissingReferenceException(nameof(walker));
            if (body == null) throw new MissingReferenceException(nameof(body));
            if (view == null) throw new MissingReferenceException(nameof(view));
            if (flashlight == null) throw new MissingReferenceException(nameof(flashlight));
            if (disableWhileHidden == null) throw new MissingReferenceException(nameof(disableWhileHidden));
            black = Texture2D.blackTexture;
        }

        void OnDisable()
        {
            if (!IsHidden) return;
            ExitHiding();
            GiveControlBack();
        }

        void Update()
        {
            Keyboard keys = Keyboard.current;
            bool use = keys != null && keys.eKey.wasPressedThisFrame;

            if (controlBackFrame >= 0 && Time.frameCount >= controlBackFrame && Time.time >= controlBackAt) GiveControlBack();

            if (IsHidden)
            {
                // F still works on the flashlight component: inside the can it stays off.
                if (flashlight.IsOn) flashlight.SetOn(false);
                Peek();
                if (use) ExitHiding();
                return;
            }

            Aimed = null;
            if (controlBackFrame >= 0) return;
            if (Physics.Raycast(view.position, view.forward, out RaycastHit hit, reach, ~0, QueryTriggerInteraction.Ignore))
            {
                ZatanHidingSpot spot = hit.collider.GetComponentInParent<ZatanHidingSpot>();
                if (spot != null && spot.CanHide) Aimed = spot;
            }
            if (Aimed != null && use) Enter(Aimed);
        }

        /// <summary>Gets into a standing, empty can (also used by tests).</summary>
        public void Enter(ZatanHidingSpot spot)
        {
            if (IsHidden) throw new System.InvalidOperationException("Already hidden.");
            rootAboveGround = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit ground, 5f, ~0, QueryTriggerInteraction.Ignore)
                ? transform.position.y - ground.point.y : 0f;

            flashlightWasOn = flashlight.IsOn;
            flashlight.SetOn(false);
            SetControl(false);

            Transform peek = spot.ViewPoint;
            transform.rotation = Quaternion.Euler(0f, peek.eulerAngles.y, 0f);
            view.localRotation = Quaternion.identity;
            transform.position += peek.position - view.position;
            peekYaw = peekPitch = 0f;

            Spot = spot;
            spot.Ejected += OnEjected;
            target.SetHidingSpot(spot);
            spot.Enter();
        }

        /// <summary>Climbs out (also used by tests).</summary>
        public void ExitHiding()
        {
            ZatanHidingSpot spot = Spot;
            spot.Exit();
            Leave(spot.ExitPoint);
        }

        void OnEjected(Vector3 landing) => Leave(landing, config.ejectStun);

        void Leave(Vector3 standPoint, float stun = 0f)
        {
            Spot.Ejected -= OnEjected;
            Spot = null;
            target.SetHidingSpot(null);

            Vector3 feet = Physics.Raycast(standPoint + Vector3.up * 2f, Vector3.down, out RaycastHit ground, 6f, ~0, QueryTriggerInteraction.Ignore)
                ? ground.point : standPoint;
            transform.position = feet + Vector3.up * rootAboveGround;
            view.localRotation = Quaternion.identity;

            // Control comes back next frame at the earliest, so the E that got out doesn't also open what is in front.
            controlBackFrame = Time.frameCount + 1;
            controlBackAt = Time.time + stun;
        }

        void GiveControlBack()
        {
            controlBackFrame = -1;
            SetControl(true);
            if (flashlightWasOn) flashlight.SetOn(true);
        }

        void SetControl(bool on)
        {
            body.enabled = on;
            walker.enabled = on;
            foreach (Behaviour b in disableWhileHidden) b.enabled = on;
        }

        void Peek()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || Cursor.lockState != CursorLockMode.Locked) return;
            Vector2 d = mouse.delta.ReadValue() * 0.1f * walker.mouse;
            peekYaw = Mathf.Clamp(peekYaw + d.x, -peekRange.x, peekRange.x);
            peekPitch = Mathf.Clamp(peekPitch - d.y, -peekRange.y, peekRange.y);
            view.localRotation = Quaternion.Euler(peekPitch, peekYaw, 0f);
        }

        void OnGUI()
        {
            if (promptStyle == null)
                promptStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(16, Screen.height / 34) };

            float w = Screen.width, h = Screen.height;
            if (IsHidden)
            {
                // The inside of the can above and below the gap the player peeks through.
                GUI.DrawTexture(new Rect(0f, 0f, w, h * gapCover), black);
                GUI.DrawTexture(new Rect(0f, h * (1f - gapCover), w, h * gapCover), black);
                Prompt("[E] SAIR", h * (1f - gapCover) + 8f);
            }
            else if (Aimed != null) Prompt("[E] ESCONDER", h * 0.56f);
        }

        void Prompt(string text, float y)
        {
            var r = new Rect(0f, y, Screen.width, promptStyle.fontSize * 1.6f);
            promptStyle.normal.textColor = Color.black;
            GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), text, promptStyle);
            promptStyle.normal.textColor = Color.white;
            GUI.Label(r, text, promptStyle);
        }
    }
}
