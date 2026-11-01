#define MACHINES
#define PICKUPS
// Copyright 2026 heyheythere
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_PACKAGE
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace HeyHeyThere.PSXCarnivalKit
{
    /// <summary>
    /// The demo level's player: WASD or the arrows and the mouse, Shift runs, E opens what is in
    /// front (its clip plays to half way and waits there, open; E again plays the rest, shut), Esc
    /// frees the mouse and a click takes it back. Either input handling works.
#if MACHINES
    /// E uses a Machine in front: the part the view is on, taking what it needs from the items carried.
#endif
#if PICKUPS
    /// E takes a Pickup in front into the items carried and holds it up in the Inspect view, saves at a
    /// save point and opens storage.
#endif
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class LevelWalker : MonoBehaviour
    {
#if UNITY_6000_6_OR_NEWER  // 6.6 deprecates the sort-mode overloads, the only ones 6.0 and 6.3 have
        static T[] All<T>(bool inactive = false) where T : Object =>
            FindObjectsByType<T>(inactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
#else
        static T[] All<T>(bool inactive = false) where T : Object =>
            FindObjectsByType<T>(inactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#endif

        public Transform view;
        public float walk = 2.2f, run = 4f, mouse = 2f, reach = 2f;

        CharacterController body;
        Vector3 start;
        float pitch, fall;
        readonly List<AnimationState> opening = new List<AnimationState>();
#if MACHINES || PICKUPS
        public List<string> items = new List<string>();
        string note = "";
        float noteLeft;
#endif
#if PICKUPS
        public Inspect inspect;
#endif

        void Start()
        {
            body = GetComponent<CharacterController>();
            start = transform.position;
            Cursor.lockState = CursorLockMode.Locked;
#if MACHINES
            foreach (var m in All<Machine>(true))
            {
                m.onRefused.AddListener(why => Say(why == "power" ? "No power" : why == "code" ? "Wrong code" : $"It needs the {why.Replace('_', ' ')}"));
                m.onPicked.AddListener(it => Say($"Picked up the {it.Replace('_', ' ')}"));
            }
#endif
#if PICKUPS
            foreach (var p in All<Pickup>(true))
            {
                var at = p;
                p.onRefused.AddListener(why => Say($"It needs the {why.Replace('_', ' ')}"));
                p.onPicked.AddListener(it => Say($"Picked up the {(at.title != "" ? at.title.ToLowerInvariant() : it.Replace('_', ' '))}"));
                p.onSaved.AddListener(() => Say("The game is saved" + (at.takes != "" ? $" ({items.FindAll(i => i == at.takes).Count} {at.takes.Replace('_', ' ')} left)" : "")));
            }
#endif
        }

        void Update()
        {
#if PICKUPS
            if (inspect != null && inspect.Showing)
                return;
#endif
            if (Free())
                Cursor.lockState = CursorLockMode.None;
            else if (Click())
                Cursor.lockState = CursorLockMode.Locked;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                var d = Look() * mouse;
                transform.Rotate(0f, d.x, 0f);
                pitch = Mathf.Clamp(pitch - d.y, -80f, 80f);
                view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
            var m = Vector2.ClampMagnitude(Move(), 1f) * (Running() ? run : walk);
            fall = body.isGrounded ? -1f : fall - 9.8f * Time.deltaTime;
            body.Move((transform.right * m.x + transform.forward * m.y + Vector3.up * fall) * Time.deltaTime);
            if (transform.position.y < -20f)
            {
                body.enabled = false;
                transform.position = start;
                body.enabled = true;
            }
            if (Use())
                Open();
            for (var i = opening.Count - 1; i >= 0; i--)
                if (opening[i].time >= opening[i].length / 2f)
                {
                    opening[i].speed = 0f;
                    opening.RemoveAt(i);
                }
        }

        /// <summary>
        /// What the view is on: the first collider it meets, or, met inside that one's box, a smaller box centred
        /// in it and no longer (a key in its desk's box, a keypad's key in its panel's); the floor under a
        /// pickup, as thin as it is, never wins.
        /// </summary>
        Collider Aimed()
        {
            var hits = Physics.RaycastAll(view.position, view.forward, reach).Where(h => !h.collider.transform.IsChildOf(transform))
                .OrderBy(h => h.distance).ToArray();
            if (hits.Length == 0)
                return null;
            var outer = hits[0].collider;
            var best = outer;
            if (!(outer is BoxCollider))
                return best;
            var box = outer.bounds;
            box.Expand(0.02f);
            var least = Volume(outer.bounds);
            foreach (var h in hits.Skip(1))
            {
                if (!box.Contains(h.point))
                    break;
                var b = h.collider.bounds;
                if (h.collider is BoxCollider && Volume(b) < least && box.Contains(b.center) && Longest(b) <= Longest(outer.bounds))
                {
                    best = h.collider;
                    least = Volume(b);
                }
            }
            return best;
        }

        static float Volume(Bounds b) => b.size.x * b.size.y * b.size.z;

        static float Longest(Bounds b) => Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));

        void Open()
        {
            var aimed = Aimed();
            if (aimed == null)
                return;
#if PICKUPS
            var pickup = aimed.GetComponentInParent<Pickup>();
            if (pickup != null)
            {
                if (pickup.Use(items, aimed.name) && pickup.kind == Pickup.Kind.Item && inspect != null)
                    inspect.Open(pickup.gameObject);
                return;
            }
#endif
#if MACHINES
            var machine = aimed.GetComponentInParent<Machine>();
            if (machine != null)
            {
                machine.Use(items, aimed.name);
                return;
            }
#endif
            var anim = aimed.GetComponentInParent<Animation>();
            if (anim == null)
                return;
            foreach (AnimationState state in anim)
            {
                if (state.wrapMode == WrapMode.Loop)
                {
                    if (anim.isPlaying) anim.Stop(); else anim.Play(state.name);
                }
                else if (state.enabled && state.speed == 0f)
                    state.speed = 1f;
                else if (!anim.isPlaying)
                {
                    anim.Play(state.name);
                    state.speed = 1f;
                    opening.Add(state);
                }
                return;
            }
        }

#if MACHINES || PICKUPS
        void Say(string text)
        {
            note = text;
            noteLeft = 2.5f;
        }

        void OnGUI()
        {
#if PICKUPS
            if (inspect != null && inspect.Showing)
                return;
#endif
            GUI.Label(new Rect(Screen.width / 2f - 4f, Screen.height / 2f - 10f, 20f, 20f), "+");
            noteLeft -= Time.unscaledDeltaTime;
            if (noteLeft > 0f)
                GUI.Label(new Rect(Screen.width / 2f - 150f, Screen.height * 0.8f, 300f, 24f), note, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
            var carried = items.GroupBy(it => it).Select(g => g.Count() > 1 ? $"{g.Key} x{g.Count()}" : g.Key).ToList();
            GUI.Label(new Rect(Screen.width - 220f, Screen.height - 30f - 20f * carried.Count, 200f, 20f * carried.Count + 10f),
                      string.Join("\n", carried).Replace('_', ' '));
        }

#endif
#if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_PACKAGE
        static Vector2 Move()
        {
            var k = Keyboard.current;
            return k == null ? Vector2.zero : new Vector2(Key(k.dKey, k.rightArrowKey) - Key(k.aKey, k.leftArrowKey),
                                                          Key(k.wKey, k.upArrowKey) - Key(k.sKey, k.downArrowKey));
        }

        static float Key(KeyControl a, KeyControl b) => a.isPressed || b.isPressed ? 1f : 0f;
        static bool Running() => Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
        static bool Use() => Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        static bool Free() => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        static bool Click() => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        static Vector2 Look() => Mouse.current == null ? Vector2.zero : Mouse.current.delta.ReadValue() * 0.1f; // as the old Mouse X and Y
#else
        static Vector2 Move() => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        static bool Running() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        static bool Use() => Input.GetKeyDown(KeyCode.E);
        static bool Free() => Input.GetKeyDown(KeyCode.Escape);
        static bool Click() => Input.GetMouseButtonDown(0);
        static Vector2 Look() => new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#endif
    }
}
