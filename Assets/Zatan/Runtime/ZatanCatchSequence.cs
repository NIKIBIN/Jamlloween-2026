using System;
using UnityEngine;
using UnityEngine.Events;

namespace Jamlloween.Zatan
{
    public enum ZatanCatchPhase { Idle, Grab, Shake, Bite, Black }

    /// <summary>The jump scare of a catch: the world goes black, he grabs the player, shakes them and bites their face. Then Finished.</summary>
    public sealed class ZatanCatchSequence : MonoBehaviour
    {
        static readonly int BlackoutId = Shader.PropertyToID("_ZatanBlackout");
        static readonly int ScareLightId = Shader.PropertyToID("_ZatanScareLight");

        [Header("Referências")]
        [SerializeField] ZatanConfig config;
        [SerializeField] ZatanBehaviourBrain brain;
        [SerializeField] ZatanTarget target;
        [Tooltip("Centro da boca aberta. Placeholder até o modelo final.")]
        [SerializeField] Transform mouth;
        [Tooltip("Dobradiça da mandíbula: gira em X. Placeholder até o modelo final.")]
        [SerializeField] Transform jaw;
        [Tooltip("Fundo da boca: só aparece durante a captura.")]
        [SerializeField] Renderer throat;

        [Header("Áudio")]
        [SerializeField] AudioSource oneShotSource;
        [SerializeField] AudioSource shakeSource;
        [SerializeField] AudioClip grabClip;
        [SerializeField] AudioClip shakeLoop;
        [SerializeField] AudioClip biteClip;

        [Header("Eventos")]
        [Tooltip("Depois do corte para preto. Ex.: Minigame.Exit para tirar o jogador de um minigame em que foi pego.")]
        [SerializeField] UnityEvent onFinished = new UnityEvent();

        public event Action Finished = delegate { };

        public ZatanCatchPhase Phase { get; private set; } = ZatanCatchPhase.Idle;
        public float Elapsed { get; private set; }

        Transform view;
        Camera viewCamera;
        Vector3 viewLocalPosition, viewStart, toZatan, mouthLocal;
        Quaternion viewLocalRotation, viewStartRotation, jawRestRotation, faceCamera, lookAtZatan;
        Color viewBackground;
        int viewCullingMask;
        float phaseTime, startDistance;
        bool bitten;

        void Awake()
        {
            if (config == null) throw new MissingReferenceException(nameof(config));
            if (brain == null) throw new MissingReferenceException(nameof(brain));
            if (target == null) throw new MissingReferenceException(nameof(target));
            if (mouth == null) throw new MissingReferenceException(nameof(mouth));
            if (jaw == null) throw new MissingReferenceException(nameof(jaw));
            if (throat == null) throw new MissingReferenceException(nameof(throat));
            if (oneShotSource == null) throw new MissingReferenceException(nameof(oneShotSource));
            if (shakeSource == null) throw new MissingReferenceException(nameof(shakeSource));
            if (grabClip == null) throw new MissingReferenceException(nameof(grabClip));
            if (shakeLoop == null) throw new MissingReferenceException(nameof(shakeLoop));
            if (biteClip == null) throw new MissingReferenceException(nameof(biteClip));

            viewCamera = target.View;
            view = viewCamera.transform;
            jawRestRotation = jaw.localRotation;
            mouthLocal = transform.InverseTransformPoint(mouth.position);
            shakeSource.clip = shakeLoop;
            shakeSource.loop = true;
            throat.enabled = false;
        }

        void OnEnable()
        {
            brain.PlayerCaught += Begin;
            Shader.SetGlobalVector(ScareLightId, config.scareLightColor);
            Shader.SetGlobalFloat(BlackoutId, 0f);
        }

        void OnDisable()
        {
            brain.PlayerCaught -= Begin;
            if (Phase != ZatanCatchPhase.Idle) Restore();
            Shader.SetGlobalFloat(BlackoutId, 0f);
        }

        void Begin()
        {
            viewLocalPosition = view.localPosition;
            viewLocalRotation = view.localRotation;
            viewStart = view.position;
            viewStartRotation = view.rotation;
            viewBackground = viewCamera.backgroundColor;
            viewCullingMask = viewCamera.cullingMask;

            toZatan = Vector3.ProjectOnPlane(mouth.position - viewStart, Vector3.up);
            startDistance = toZatan.magnitude;
            toZatan /= startDistance;
            lookAtZatan = Quaternion.LookRotation(toZatan, Vector3.up);
            faceCamera = Quaternion.LookRotation(-toZatan, Vector3.up);

            target.SetInputEnabled(false);
            viewCamera.backgroundColor = Color.black;
            bitten = false;
            Elapsed = 0f;
            EnterPhase(ZatanCatchPhase.Grab);
            oneShotSource.PlayOneShot(grabClip);
            shakeSource.Play();
        }

        void EnterPhase(ZatanCatchPhase next)
        {
            Phase = next;
            phaseTime = 0f;
        }

        // After the player's Update and the agent: nothing else moves the camera or him this frame.
        void LateUpdate()
        {
            if (Phase == ZatanCatchPhase.Idle) return;

            Elapsed += Time.deltaTime;
            phaseTime += Time.deltaTime;
            Shader.SetGlobalFloat(BlackoutId, Mathf.Clamp01(Elapsed / config.scareBlackoutFadeTime));

            switch (Phase)
            {
                case ZatanCatchPhase.Grab: UpdateGrab(); break;
                case ZatanCatchPhase.Shake: UpdateShake(); break;
                case ZatanCatchPhase.Bite: UpdateBite(); break;
                case ZatanCatchPhase.Black: UpdateBlack(); break;
            }
        }

        void UpdateGrab()
        {
            float t = Mathf.Clamp01(phaseTime / config.scareGrabTime);
            float eased = 1f - (1f - t) * (1f - t);

            Vector3 viewPosition = viewStart + Vector3.up * (config.scareLift * eased);
            view.SetPositionAndRotation(viewPosition, Quaternion.Slerp(viewStartRotation, lookAtZatan, eased));
            PlaceZatan(viewPosition, Mathf.Lerp(startDistance, config.scareHoldDistance, eased), config.scareHoldFaceRaise, Vector3.zero, Quaternion.identity);

            if (t >= 1f) EnterPhase(ZatanCatchPhase.Shake);
        }

        void UpdateShake()
        {
            float t = Mathf.Clamp01(phaseTime / config.scareShakeTime);
            SetJaw(config.scareJawHoldAngle * Mathf.Clamp01(t * 8f));
            ApplyHeldPose(Mathf.Clamp01(t * 8f), config.scareHoldDistance, config.scareHoldFaceRaise, 0f);
            if (t < 1f) return;
            throat.enabled = true; // Only the open mouth needs it: closed, it would peek out under the head.
            EnterPhase(ZatanCatchPhase.Bite);
        }

        void UpdateBite()
        {
            float t = Mathf.Clamp01(phaseTime / config.scareBiteTime);
            float open = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / config.scareBiteOpenFraction));
            float lunge = Mathf.InverseLerp(config.scareBiteOpenFraction, config.scareBiteImpactFraction, t);
            lunge *= lunge;

            SetJaw(Mathf.Lerp(config.scareJawHoldAngle, config.scareJawOpenAngle, open));

            if (!bitten && t >= config.scareBiteImpactFraction)
            {
                bitten = true;
                shakeSource.Stop();
                oneShotSource.PlayOneShot(biteClip);
            }

            float kick = bitten ? config.scareImpactKick * (1f - Mathf.InverseLerp(config.scareBiteImpactFraction, 1f, t)) : 0f;
            ApplyHeldPose(
                config.scareBiteShake,
                Mathf.Lerp(config.scareHoldDistance, config.scareBiteDistance, lunge),
                Mathf.Lerp(config.scareHoldFaceRaise, 0f, lunge),
                kick);

            if (t >= 1f)
            {
                viewCamera.cullingMask = 0; // Total black: not even him.
                EnterPhase(ZatanCatchPhase.Black);
            }
        }

        void UpdateBlack()
        {
            if (phaseTime < config.scareBlackTime) return;
            Restore();
            Finished();
            onFinished.Invoke();
        }

        void ApplyHeldPose(float shake, float distance, float faceRaise, float kick)
        {
            float time = Elapsed * config.scareShakeFrequency;
            Vector3 shift = new Vector3(Noise(time, 0f), Noise(time, 10f), Noise(time, 20f));
            Vector3 twist = new Vector3(Noise(time, 30f), Noise(time, 40f), Noise(time, 50f));

            Vector3 viewOffset = lookAtZatan * (shift * (config.scareShakePosition * shake));
            Quaternion viewTwist = Quaternion.Euler(
                twist.x * config.scareShakeTilt * shake + kick,
                twist.y * config.scareShakeTilt * shake,
                twist.z * config.scareShakeRoll * shake);
            Vector3 viewPosition = viewStart + Vector3.up * config.scareLift + viewOffset;
            view.SetPositionAndRotation(viewPosition, lookAtZatan * viewTwist);

            // He jerks against the camera, so the two feel locked together.
            Vector3 bodyOffset = -viewOffset.normalized * (config.scareShakeBodyPosition * shake);
            Quaternion bodyTwist = Quaternion.Euler(-twist.x * config.scareShakeBodyTilt * shake, -twist.y * config.scareShakeBodyTilt * shake, -twist.z * config.scareShakeBodyTilt * shake);
            PlaceZatan(viewPosition, distance, faceRaise, bodyOffset, bodyTwist);
        }

        void PlaceZatan(Vector3 viewPosition, float distance, float faceRaise, Vector3 offset, Quaternion twist)
        {
            Quaternion rotation = faceCamera * twist;
            Vector3 anchor = mouthLocal + Vector3.up * faceRaise;
            transform.SetPositionAndRotation(viewPosition + toZatan * distance + offset - rotation * anchor, rotation);
        }

        void SetJaw(float angle) => jaw.localRotation = jawRestRotation * Quaternion.Euler(angle, 0f, 0f);

        void Restore()
        {
            Phase = ZatanCatchPhase.Idle;
            view.SetLocalPositionAndRotation(viewLocalPosition, viewLocalRotation);
            viewCamera.backgroundColor = viewBackground;
            viewCamera.cullingMask = viewCullingMask;
            jaw.localRotation = jawRestRotation;
            throat.enabled = false;
            shakeSource.Stop();
            Shader.SetGlobalFloat(BlackoutId, 0f);
            target.SetInputEnabled(true);
        }

        static float Noise(float time, float seed) => Mathf.PerlinNoise(time, seed) * 2f - 1f;
    }
}
