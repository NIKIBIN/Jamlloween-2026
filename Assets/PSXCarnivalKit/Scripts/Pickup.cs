// Copyright 2026 heyheythere
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HeyHeyThere.PSXCarnivalKit
{
    /// <summary>
    /// A pickup: on an item prop's prefab, what a use of it gives, takes or opens. <see cref="kind"/>:
    /// Item: picked up, <see cref="amount"/> of <see cref="item"/> into the user's items (one entry each), and gone.
    /// Save: a save point; each use takes one of what it <see cref="takes"/> (an ink ribbon), plays
    /// <see cref="clip"/> and fires onSaved; with nothing to take, onRefused.
    /// Storage: an item box; a use opens it (the clip forward), the next shuts it (back).
    /// Its <see cref="title"/> and <see cref="text"/> are what Inspect shows, <see cref="reveal"/> a clip it
    /// plays there (a folder opening). An item glints until it is picked up, unless <see cref="glint"/> is off.
    ///     key.onPicked.AddListener(it => Debug.Log("got " + it));
    ///     key.Use(items);  // items: the player's, a List of item names
    /// </summary>
    public class Pickup : MonoBehaviour
    {
        public enum Kind { Item, Save, Storage }
        [Serializable] public class BoolEvent : UnityEvent<bool> { }
        [Serializable] public class StringEvent : UnityEvent<string> { }

        public Kind kind;
        public string item = "";
        public int amount = 1;
        public string title = "";
        [TextArea(2, 6)] public string text = "";
        public string reveal = "", clip = "", takes = "";
        public bool glint = true;

        [Tooltip("A use did something: the part used")] public StringEvent onUsed = new StringEvent();
        public StringEvent onPicked = new StringEvent();
        public UnityEvent onSaved = new UnityEvent();
        public BoolEvent onOpened = new BoolEvent();
        [Tooltip("The item it wants")] public StringEvent onRefused = new StringEvent();

        public bool Live { get; private set; }  // picked up, saved at, or open
        public int Saves { get; private set; }

        Animation anim;
        readonly Dictionary<string, float> at = new Dictionary<string, float>();
        string moving = "";
        float to, time;
        Transform spark;

        void Awake()
        {
            anim = GetComponent<Animation>();
            if (glint && kind == Kind.Item)
                spark = Star();
        }

        /// <summary>Uses it, carrying <paramref name="items"/>: an item goes into them, a save takes from them. Whether it did anything.</summary>
        public bool Use(List<string> items, string part = "")
        {
            items = items ?? new List<string>();
            switch (kind)
            {
                case Kind.Item:
                    for (var i = 0; i < amount; i++)
                        items.Add(item);
                    Live = true;
                    onPicked.Invoke(item);
                    gameObject.SetActive(false);
                    break;
                case Kind.Save:
                    if (takes != "" && !items.Remove(takes))
                    {
                        onRefused.Invoke(takes);
                        return false;
                    }
                    Saves++;
                    Live = true;
                    Play(clip, Len(clip));
                    onSaved.Invoke();
                    break;
                case Kind.Storage:
                    if (moving == clip && Has(clip))
                        return false;
                    Live = !Live;
                    Play(clip, Live ? Len(clip) : 0f);
                    onOpened.Invoke(Live);
                    break;
            }
            onUsed.Invoke(part);
            return true;
        }

        /// <summary>Its clip where it is going, at once.</summary>
        public void Settle()
        {
            if (moving == "")
                return;
            var c = moving;
            moving = "";
            Pose(c, kind == Kind.Save ? 0f : to);
        }

        void Update()
        {
            time += Time.deltaTime;
            if (spark != null)  // a glint every two seconds, a star that swells and fades
            {
                var t = Mathf.Repeat(time + transform.position.x * 0.7f, 2f);
                var s = Mathf.Sin(Mathf.Clamp01(t / 0.5f) * Mathf.PI);
                var cam = Camera.main;
                spark.gameObject.SetActive(glint && s > 0.02f);
                spark.localScale = Vector3.one * Mathf.Max(0.01f, s) / Mathf.Max(0.001f, transform.lossyScale.x);
                if (cam != null)
                    spark.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, t * 86f);
            }
            if (moving == "")
                return;
            at.TryGetValue(moving, out var now);
            now = Mathf.MoveTowards(now, to, Time.deltaTime);
            Pose(moving, now);
            if (now == to)
            {
                if (kind == Kind.Save && moving == clip)
                    Pose(clip, 0f);
                moving = "";
            }
        }

        void Play(string c, float target)
        {
            if (!Has(c))
                return;
            moving = c;
            to = target;
        }

        /// <summary>The clip's curves at t seconds, sampled alone onto its parts.</summary>
        void Pose(string c, float t)
        {
            if (!Has(c))
                return;
            at[c] = t;
            var s = anim[c];
            s.enabled = true;
            s.weight = 1f;
            s.time = t;
            anim.Sample();
            s.enabled = false;
        }

        bool Has(string c) => !string.IsNullOrEmpty(c) && anim != null && anim[c] != null;

        float Len(string c) => Has(c) ? anim[c].length : 0f;

        /// <summary>A four-pointed star over it, drawn here, that faces the camera.</summary>
        Transform Star()
        {
            var img = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 16; y++)
                for (var x = 0; x < 16; x++)
                {
                    var d = new Vector2(Mathf.Abs(x - 7.5f), Mathf.Abs(y - 7.5f));
                    img.SetPixel(x, y, new Color(1f, 1f, 0.9f, Mathf.Clamp01(1f - d.x * d.y / 6f - d.magnitude / 9f)));
                }
            img.Apply();
            var bounds = new Bounds(transform.position, Vector3.zero);
            foreach (var r in GetComponentsInChildren<Renderer>())
                bounds.Encapsulate(r.bounds);
            var go = new GameObject("Glint", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(bounds.center.x, bounds.max.y + 0.03f, bounds.center.z);
            go.GetComponent<SpriteRenderer>().sprite = Sprite.Create(img, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f / (16 * 0.0075f));
            return go.transform;
        }
    }
}
