using UnityEngine;
using UnityEngine.InputSystem;

namespace Circus
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        public enum Mode { Free, Busy, Hidden, Dead }

        public static FirstPersonController I { get; private set; }

        [SerializeField] Transform cameraPivot;
        [SerializeField] float pitchLimit = 85f;

        public Mode CurrentMode { get; private set; } = Mode.Free;
        public bool LookLocked { get; set; }
        public bool IsRunning { get; private set; }
        public bool IsMoving { get; private set; }
        public Camera Cam { get; private set; }
        public Vector3 HeadPosition => cameraPivot.position;
        public Vector3 Forward => cameraPivot.forward;

        CharacterController cc;
        float pitch, verticalVel;
        Vector2 hiddenLookCenter;

        void Awake()
        {
            I = this;
            cc = GetComponent<CharacterController>();
            Cam = cameraPivot.GetComponentInChildren<Camera>();
        }

        void OnEnable() => EventBus.PlayerCaught += OnCaught;
        void OnDisable() => EventBus.PlayerCaught -= OnCaught;
        void OnDestroy() { if (I == this) I = null; }

        void OnCaught() => SetMode(Mode.Dead);

        public void SetMode(Mode m)
        {
            CurrentMode = m;
            cc.enabled = m != Mode.Hidden;
            if (m == Mode.Free) LookLocked = false;
        }

        void Update()
        {
            var t = Tunables.I;
            bool uiOpen = DebugPanelState.IsOpen;
            bool cursorFree = uiOpen || CurrentMode == Mode.Dead;
            Cursor.lockState = cursorFree ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = cursorFree;

            var kb = Keyboard.current;
            var mouse = Mouse.current;

            if (!uiOpen && !LookLocked && CurrentMode != Mode.Dead && mouse != null)
            {
                Vector2 d = mouse.delta.ReadValue() * t.mouseSensitivity;
                pitch = Mathf.Clamp(pitch - d.y, -pitchLimit, pitchLimit);
                cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
                transform.Rotate(0f, d.x, 0f, Space.Self);
            }

            Vector3 move = Vector3.zero;
            IsRunning = false;
            if (CurrentMode == Mode.Free && !uiOpen && kb != null)
            {
                Vector2 input = Vector2.zero;
                if (kb.wKey.isPressed) input.y += 1;
                if (kb.sKey.isPressed) input.y -= 1;
                if (kb.dKey.isPressed) input.x += 1;
                if (kb.aKey.isPressed) input.x -= 1;
                input = Vector2.ClampMagnitude(input, 1f);
                IsRunning = kb.leftShiftKey.isPressed && input.sqrMagnitude > 0.01f;
                float speed = IsRunning ? t.runSpeed : t.walkSpeed;
                move = (transform.right * input.x + transform.forward * input.y) * speed;
            }
            IsMoving = move.sqrMagnitude > 0.01f;

            if (cc.enabled)
            {
                if (cc.isGrounded && verticalVel < 0f) verticalVel = -2f;
                verticalVel += t.gravity * Time.deltaTime;
                cc.Move((move + Vector3.up * verticalVel) * Time.deltaTime);
            }
        }

        public void Teleport(Vector3 pos, float? yaw = null)
        {
            bool was = cc.enabled;
            cc.enabled = false;
            transform.position = pos;
            if (yaw.HasValue) transform.rotation = Quaternion.Euler(0f, yaw.Value, 0f);
            cc.enabled = was;
        }

        public void SetPitch(float p)
        {
            pitch = p;
            cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }
    }
}
