using UnityEngine;

namespace Circus
{
    /// <summary>Placeholder audio: spatial Zatan footsteps, repair beeps, tape hiss (tracks interference), distorted carousel music.</summary>
    public class AudioDirector : MonoBehaviour
    {
        [SerializeField] AudioSource zatanSteps;
        [SerializeField] float stepInterval = 0.62f;
        [SerializeField] float stepVolume = 1f;
        [SerializeField] float musicMasksStepsFactor = 0.3f;
        [SerializeField] float hissBase = 0.05f, hissInterference = 0.35f;
        [SerializeField] float musicVolume = 0.8f;

        AudioClip step, creak, beep;
        AudioSource hiss, music, beepSrc;
        float stepTimer;

        void Awake()
        {
            step = ProceduralAudio.Footstep();
            creak = ProceduralAudio.Creak();
            beep = ProceduralAudio.Beep();

            hiss = gameObject.AddComponent<AudioSource>();
            hiss.clip = ProceduralAudio.Hiss(); hiss.loop = true; hiss.spatialBlend = 0f; hiss.volume = hissBase; hiss.Play();

            beepSrc = new GameObject("BeepSource").AddComponent<AudioSource>();
            beepSrc.transform.SetParent(transform);
            beepSrc.spatialBlend = 1f; beepSrc.rolloffMode = AudioRolloffMode.Linear; beepSrc.maxDistance = 20f;

            if (zatanSteps)
            {
                zatanSteps.spatialBlend = 1f; zatanSteps.rolloffMode = AudioRolloffMode.Linear;
                zatanSteps.minDistance = 2f; zatanSteps.maxDistance = 28f; zatanSteps.playOnAwake = false;
            }
        }

        void Start()
        {
            var car = CarouselController.I;
            if (car)
            {
                music = car.gameObject.AddComponent<AudioSource>();
                music.clip = ProceduralAudio.CarouselMusic(); music.loop = true;
                music.spatialBlend = 1f; music.rolloffMode = AudioRolloffMode.Linear; music.minDistance = 8f; music.maxDistance = 75f;
                music.volume = 0f; music.Play();
            }
        }

        void OnEnable()
        {
            RepairAudio.OnBeep += OnBeep;
            EventBus.PlayerCaught += OnCaught;
        }

        void OnDisable()
        {
            RepairAudio.OnBeep -= OnBeep;
            EventBus.PlayerCaught -= OnCaught;
        }

        void OnBeep(Vector3 pos, float progress)
        {
            beepSrc.transform.position = pos;
            beepSrc.pitch = 0.8f + progress * 0.8f;
            beepSrc.PlayOneShot(beep, 0.8f);
        }

        void OnCaught() { hiss.volume = 1f; }

        void Update()
        {
            if (GameState.I && GameState.I.IsGameOver) return;
            hiss.volume = hissBase + hissInterference * VHSGlobals.Total;

            var car = CarouselController.I;
            bool carOn = car && car.IsOn;
            if (music)
            {
                music.volume = Mathf.MoveTowards(music.volume, carOn ? musicVolume : 0f, Time.deltaTime * 0.8f);
                // warble: slow pitch drift + random dips, like a stretched tape
                music.pitch = 0.92f + 0.05f * Mathf.Sin(Time.time * 0.6f) + (Random.value < 0.01f ? -0.15f : 0f);
            }

            var z = ZatanBrain.I;
            if (zatanSteps && z && z.Agent && z.Agent.enabled && z.Agent.velocity.magnitude > 0.3f)
            {
                float speed = z.Agent.velocity.magnitude;
                stepTimer -= Time.deltaTime * Mathf.Max(0.6f, speed / 2.2f);
                if (stepTimer <= 0f)
                {
                    stepTimer = stepInterval;
                    float vol = stepVolume * (carOn ? musicMasksStepsFactor : 1f);
                    zatanSteps.pitch = Random.Range(0.85f, 1.05f);
                    zatanSteps.PlayOneShot(step, vol);
                    if (Random.value < 0.5f) zatanSteps.PlayOneShot(creak, vol * 0.7f);
                }
            }
        }
    }
}
