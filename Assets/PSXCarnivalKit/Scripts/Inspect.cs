// Copyright 2026 heyheythere
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_PACKAGE
using UnityEngine.InputSystem;
#endif

namespace HeyHeyThere.PSXCarnivalKit
{
    /// <summary>
    /// The inspect view: an item held up close over the dimmed game, a copy of it drawn by a camera of
    /// its own, turned by dragging the mouse or with the arrow keys (or WASD), zoomed with the wheel; E
    /// plays its reveal (a folder opening) and back again; Esc, Enter or a right click puts it away.
    /// Its title over it, its text under it. The copy goes on <see cref="layer"/>, far off at
    /// <see cref="away"/>, out of the game's view.
    ///     inspect.Open(diary.gameObject);   // a Pickup: its title, text and reveal; or any prop
    /// </summary>
    public class Inspect : MonoBehaviour
    {
        public int layer = 31;
        public Vector3 away = new Vector3(0f, -1000f, 0f);
        public Color dim = new Color(0f, 0f, 0f, 0.8f);
        public float turnSpeed = 115f;  // degrees a second on the keys

        public bool Showing { get; private set; }
        public bool Revealed { get; private set; }
        public GameObject Item { get; private set; }  // the copy shown
        public float yaw, pitch, zoom = 1f;

        static readonly int RadialFogId = Shader.PropertyToID("_RadialFogEnabled");

        Camera cam;
        RenderTexture picture;
        Transform pivot;
        Animation anim;
        string reveal = "", title = "", text = "";
        float dist = 1f;
        bool wasLocked;
        int opened;  // the frame it opened on, whose key press opened it

        /// <summary>Holds <paramref name="source"/> up: a copy of it, with its Pickup's title, text and reveal; <paramref name="title"/> and <paramref name="text"/> stand in for them.</summary>
        public void Open(GameObject source, string title = "", string text = "")
        {
            Close();
            var pickup = source.GetComponent<Pickup>();
            this.title = title != "" ? title : pickup != null && pickup.title != "" ? pickup.title
                : (source.name.StartsWith("PF_") ? source.name.Substring(3) : source.name).Replace('_', ' ');
            this.text = text != "" ? text : pickup != null ? pickup.text : "";
            if (pivot == null)
            {
                pivot = new GameObject("Inspect Pivot").transform;
                cam = new GameObject("Inspect Camera").AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                cam.fieldOfView = 30f;
                cam.enabled = false;
            }
            pivot.position = away;
            pivot.gameObject.SetActive(false);  // the copy's scripts don't wake before they go
            Item = Instantiate(source, pivot);
            Item.SetActive(true);
            foreach (var p in Item.GetComponentsInChildren<Pickup>(true))
                DestroyImmediate(p);
            foreach (var c in Item.GetComponentsInChildren<Collider>(true))
                DestroyImmediate(c);
            foreach (var t in Item.GetComponentsInChildren<Transform>(true))
                if (t.name == "Glint")
                    DestroyImmediate(t.gameObject);
            foreach (var t in Item.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
            pivot.gameObject.SetActive(true);
            Item.transform.localPosition = Vector3.zero;
            Item.transform.localRotation = Quaternion.identity;
            anim = Item.GetComponent<Animation>();
            reveal = pickup != null && anim != null && anim[pickup.reveal] != null ? pickup.reveal : "";
            Revealed = false;

            var bounds = Measure();
            if (reveal != "")  // framed to hold it revealed too: a note unfolded, a box open
            {
                var s = anim[reveal];
                s.enabled = true;
                s.weight = 1f;
                s.time = s.length;
                anim.Sample();
                bounds.Encapsulate(Measure());
                s.time = 0f;
                anim.Sample();
                s.enabled = false;
            }
            Item.transform.position -= bounds.center - pivot.position;
            dist = bounds.extents.magnitude / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.3f;
            var deep = bounds.size.z > 2f * bounds.size.x;  // a gun or a blade pointing at you: turned side-on
            var flat = bounds.size.y < 0.35f * Mathf.Max(bounds.size.x, bounds.size.z) && !deep;
            yaw = deep ? 110f : 20f;
            pitch = flat ? 55f : 17f;  // what lies flat shown from above, as it was found
            zoom = 1f;
            cam.cullingMask = 1 << layer;
            if (Camera.main != null)
                Camera.main.cullingMask &= ~(1 << layer);
            wasLocked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = CursorLockMode.None;
            opened = Time.frameCount;
            Showing = true;
        }

        /// <summary>Plays its reveal, or back again.</summary>
        public void ToggleReveal()
        {
            if (reveal == "")
                return;
            Revealed = !Revealed;
            var s = anim[reveal];
            if (!anim.IsPlaying(reveal))
            {
                s.time = Revealed ? 0f : s.length;
                anim.Play(reveal);
            }
            s.time = Mathf.Clamp(s.time, 0f, s.length);
            s.speed = Revealed ? 1f : -1f;
            s.wrapMode = WrapMode.ClampForever;
        }

        public void Close()
        {
            if (Item != null)
                Destroy(Item);
            Item = null;
            if (Showing && wasLocked)
                Cursor.lockState = CursorLockMode.Locked;
            Showing = false;
        }

        Bounds Measure()
        {
            var bounds = new Bounds();
            var first = true;
            foreach (var r in Item.GetComponentsInChildren<Renderer>())
            {
                if (first) bounds = r.bounds; else bounds.Encapsulate(r.bounds);
                first = false;
            }
            return bounds;
        }

        void OnDestroy()
        {
            if (picture != null)
                picture.Release();
            if (pivot != null)
                Destroy(pivot.gameObject);
            if (cam != null)
                Destroy(cam.gameObject);
        }

        void LateUpdate()
        {
            if (!Showing)
                return;
            var keys = Keys();
            yaw -= (keys.x * turnSpeed * Time.unscaledDeltaTime) + Drag().x * 0.6f;
            pitch = Mathf.Clamp(pitch + keys.y * turnSpeed * Time.unscaledDeltaTime + Drag().y * 0.6f, -85f, 85f);
            zoom = Mathf.Clamp(zoom * Mathf.Pow(1.1f, Wheel()), 0.5f, 2.5f);
            if (Pressed(out var close) && Time.frameCount != opened)
            {
                if (close || reveal == "") Close(); else ToggleReveal();   // sem animacao de abrir, o E tambem continua
                if (!Showing) return;
            }
            pivot.rotation = Quaternion.Euler(pitch, 0f, 0f) * Quaternion.Euler(0f, yaw, 0f);
            cam.transform.SetPositionAndRotation(away + new Vector3(0f, 0f, -dist / zoom), Quaternion.identity);
            cam.nearClipPlane = dist * 0.02f;
            cam.farClipPlane = dist * 10f;
            if (picture == null || picture.width != Screen.width || picture.height != Screen.height)
            {
                if (picture != null) picture.Release();
                picture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point };
            }
            cam.targetTexture = picture;

            // The copy sits far from the world, so the radial fog (measured from the player's flashlight) and the night fog would
            // swallow it whole. Both are off only while this camera draws: the game behind it keeps its own.
            var radialFog = Shader.GetGlobalFloat(RadialFogId);
            var worldFog = RenderSettings.fog;
            Shader.SetGlobalFloat(RadialFogId, 0f);
            RenderSettings.fog = false;
            cam.Render();
            RenderSettings.fog = worldFog;
            Shader.SetGlobalFloat(RadialFogId, radialFog);
        }

        void OnGUI()
        {
            if (!Showing || picture == null)
                return;
            float w = Screen.width, h = Screen.height, px = Mathf.Max(14f, h / 30f);
            var was = GUI.color;
            GUI.color = dim;
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = was;
            GUI.DrawTexture(new Rect(0, text != "" ? -h * 0.08f : 0f, w, h), picture, ScaleMode.StretchToFill, true);
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, wordWrap = true, fontSize = (int)(px * 1.5f) };
            style.normal.textColor = new Color(0.96f, 0.86f, 0.6f);
            GUI.Label(new Rect(0, h * 0.05f, w, px * 2f), title, style);
            style.fontSize = (int)px;
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(w * 0.15f, h * 0.74f, w * 0.7f, h * 0.18f), text, style);
            style.fontSize = (int)px;
            style.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
            GUI.Label(new Rect(0, h - px * 2.2f, w, px * 1.4f), reveal != "" ? "[E] ABRIR / FECHAR   [ESC] CONTINUAR" : "[E] CONTINUAR", style);
        }

#if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_PACKAGE
        static Vector2 Keys()
        {
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float Key(UnityEngine.InputSystem.Controls.KeyControl a, UnityEngine.InputSystem.Controls.KeyControl b) => a.isPressed || b.isPressed ? 1f : 0f;
            return new Vector2(Key(k.rightArrowKey, k.dKey) - Key(k.leftArrowKey, k.aKey), Key(k.downArrowKey, k.sKey) - Key(k.upArrowKey, k.wKey));
        }

        static Vector2 Drag() => Mouse.current != null && Mouse.current.leftButton.isPressed ? Mouse.current.delta.ReadValue() * 0.5f : Vector2.zero;
        static float Wheel() => Mouse.current == null ? 0f : Mathf.Sign(Mouse.current.scroll.ReadValue().y) * (Mouse.current.scroll.ReadValue().y != 0f ? 1f : 0f);

        static bool Pressed(out bool close)
        {
            var k = Keyboard.current;
            close = k != null && (k.escapeKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.qKey.wasPressedThisFrame)
                    || Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            return close || k != null && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame);
        }
#else
        static Vector2 Keys()
        {
            float Key(KeyCode a, KeyCode b) => Input.GetKey(a) || Input.GetKey(b) ? 1f : 0f;
            return new Vector2(Key(KeyCode.RightArrow, KeyCode.D) - Key(KeyCode.LeftArrow, KeyCode.A), Key(KeyCode.DownArrow, KeyCode.S) - Key(KeyCode.UpArrow, KeyCode.W));
        }

        static Vector2 Drag() => Input.GetMouseButton(0) ? new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 5f : Vector2.zero;
        static float Wheel() => Input.mouseScrollDelta.y == 0f ? 0f : Mathf.Sign(Input.mouseScrollDelta.y);

        static bool Pressed(out bool close)
        {
            close = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                    || Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1);
            return close || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space);
        }
#endif
    }
}
