using UnityEngine;

namespace Jamlloween.Zatan
{
    /// <summary>
    /// Procedural "possessed" spasm on top of the animation, violent on purpose: the head snaps to a new pose in a frame
    /// or two and freezes there for an irregular moment, the chest jerks, the jaw slams open and shut, the whole body
    /// shudders, and a fast motor tremor runs under it all. Used by the reveal before the first chase
    /// (ZatanDirector.RevealThenChase). Runs after the Animator and before ZatanHeadFollow (-40), so the eyes, beams and
    /// jaw follow the head's yaw (its roll and most of its pitch don't reach the eyes).
    /// Sound: a shake loop under the whole spasm, a snap on each big jerk and, now and then, sparks.
    /// </summary>
    [DefaultExecutionOrder(-45)]
    public sealed class ZatanTwitch : MonoBehaviour
    {
        [Tooltip("The rig's head bone.")]
        [SerializeField] Transform head;
        [Tooltip("A chest bone (spine_02): a jerk under the head.")]
        [SerializeField] Transform chest;
        [Tooltip("The jaw (HeadAnchor/Jaw): opens around its local X, like the catch sequence.")]
        [SerializeField] Transform jaw;
        [Tooltip("The model root (ModelRig): the whole body shudders. Empty: no shudder.")]
        [SerializeField] Transform body;

        [Header("Jerks")]
        [SerializeField] float headYaw = 70f;
        [SerializeField] float headPitch = 18f;
        [SerializeField] float headRoll = 30f;
        [SerializeField] float chestAngle = 16f;
        [SerializeField] float jawOpen = 50f;
        [Tooltip("Seconds each pose freezes before the next jerk (random in this range). Irregular = scarier.")]
        [SerializeField] Vector2 holdSeconds = new Vector2(0.04f, 0.3f);
        [Tooltip("How fast a jerk reaches its pose. Very high: it snaps in a frame or two.")]
        [SerializeField] float snap = 90f;

        [Header("Tremor")]
        [Tooltip("Fast vibration on top of the jerks (degrees).")]
        [SerializeField] float tremor = 3.5f;
        [SerializeField] float tremorSpeed = 38f;
        [Tooltip("Whole-body shudder (metres).")]
        [SerializeField] float bodyShake = 0.05f;

        [Header("Sound")]
        [Tooltip("Loop under the whole spasm (the catch's shake loop).")]
        [SerializeField] AudioClip shakeLoop;
        [SerializeField, Range(0f, 1f)] float shakeVolume = 0.7f;
        [Tooltip("On each big jerk (the head snap of the repair scare).")]
        [SerializeField] AudioClip[] snapClips = new AudioClip[0];
        [SerializeField, Range(0f, 1f)] float snapVolume = 0.8f;
        [Tooltip("Now and then, on a jerk: something breaking inside.")]
        [SerializeField] AudioClip[] sparkClips = new AudioClip[0];
        [SerializeField, Range(0f, 1f)] float sparkVolume = 0.6f;
        [SerializeField, Range(0f, 1f)] float sparkChance = 0.3f;
        [Tooltip("How much the sounds are placed in the world (0 = 2D, 1 = fully 3D).")]
        [SerializeField, Range(0f, 1f)] float spatial = 0.75f;

        Quaternion jawRest;
        Vector3 bodyRest;
        Vector3 headNow, headTo, chestNow, chestTo;
        float jawNow, jawTo, next, seed;
        // The Animator writes the bones every frame; if a bone has no curve it doesn't, and the spasm must not pile up.
        Quaternion headBase, chestBase, headWritten, chestWritten;

        public bool Playing { get; private set; }
        AudioSource loop, shots;

        void Awake()
        {
            if (head == null) throw new MissingReferenceException(nameof(head));
            if (chest == null) throw new MissingReferenceException(nameof(chest));
            if (jaw == null) throw new MissingReferenceException(nameof(jaw));
            jawRest = jaw.localRotation;
            if (body != null) bodyRest = body.localPosition;
            seed = Random.value * 100f;
            loop = NewSource(true);
            shots = NewSource(false);
        }

        AudioSource NewSource(bool looping)
        {
            var src = new GameObject(looping ? "TwitchLoop" : "TwitchShots").AddComponent<AudioSource>();
            src.transform.SetParent(head, false);
            src.playOnAwake = false;
            src.loop = looping;
            src.spatialBlend = spatial;
            src.minDistance = 4f;
            src.maxDistance = 40f;
            src.rolloffMode = AudioRolloffMode.Linear;
            return src;
        }

        public void Play(bool on)
        {
            if (Playing == on) return;
            Playing = on;
            headNow = chestNow = Vector3.zero;
            jawNow = 0f;
            next = 0f;
            headWritten = chestWritten = new Quaternion(0f, 0f, 0f, 0f);
            if (on && shakeLoop != null)
            {
                loop.clip = shakeLoop;
                loop.volume = shakeVolume;
                loop.Play();
            }
            if (!on)
            {
                loop.Stop();
                jaw.localRotation = jawRest;
                if (body != null) body.localPosition = bodyRest;
            }
        }

        void LateUpdate()
        {
            if (!Playing) return;
            float now = Time.time;
            if (now >= next)
            {
                next = now + Random.Range(holdSeconds.x, holdSeconds.y);
                // Mostly big snaps, sometimes a small twitch: the rhythm never settles.
                bool isBig = Random.value < 0.7f;
                float big = isBig ? 1f : 0.35f;
                if (isBig) Shot(snapClips, snapVolume, Random.Range(0.85f, 1.2f));
                if (Random.value < sparkChance) Shot(sparkClips, sparkVolume, Random.Range(0.9f, 1.1f));
                headTo = new Vector3(Random.Range(-headPitch, headPitch), Random.Range(-headYaw, headYaw), Random.Range(-headRoll, headRoll)) * big;
                chestTo = new Vector3(Random.Range(-chestAngle, chestAngle), Random.Range(-chestAngle, chestAngle), Random.Range(-chestAngle, chestAngle)) * big;
                jawTo = Random.value < 0.55f ? jawOpen : Random.Range(0f, jawOpen * 0.1f);   // slams open or shut
            }
            float k = 1f - Mathf.Exp(-snap * Time.deltaTime);
            headNow = Vector3.Lerp(headNow, headTo, k);
            chestNow = Vector3.Lerp(chestNow, chestTo, k);
            jawNow = Mathf.Lerp(jawNow, jawTo, k);

            float ts = now * tremorSpeed;
            Vector3 shake = new Vector3(Mathf.PerlinNoise(ts, seed) - 0.5f, Mathf.PerlinNoise(seed, ts) - 0.5f, Mathf.PerlinNoise(ts, ts + seed) - 0.5f) * 2f;

            chestBase = chest.localRotation == chestWritten ? chestBase : chest.localRotation;
            headBase = head.localRotation == headWritten ? headBase : head.localRotation;
            chest.localRotation = chestWritten = chestBase * Quaternion.Euler(chestNow + shake * tremor * 0.6f);
            head.localRotation = headWritten = headBase * Quaternion.Euler(headNow + shake * tremor);
            jaw.localRotation = jawRest * Quaternion.Euler(jawNow + shake.x * tremor, 0f, 0f);
            if (body != null) body.localPosition = bodyRest + shake * bodyShake;
        }

        void Shot(AudioClip[] clips, float volume, float pitch)
        {
            if (clips == null || clips.Length == 0) return;
            var clip = clips[Random.Range(0, clips.Length)];
            if (clip == null) return;
            shots.pitch = pitch;   // a mesma fonte para todos: o tom muda a cada tranco
            shots.PlayOneShot(clip, volume);
        }
    }
}
