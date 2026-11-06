using HeyHeyThere.PSXCarnivalKit;
using Jamlloween.Flashlight;
using UnityEngine;

/// <summary>
/// Sons do próprio jogador, em 2D: passos (pela distância andada no chão), clique da lanterna e o som de pegar
/// um item. Os itens do kit (Pickup, como pilhas e fusível) tocam sozinhos pelo onPicked; os coletáveis do jogo
/// (ICollectible) tocam pelo PlayerInteractor, que chama PlayPickup.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerFoley : MonoBehaviour
{
    [Header("Passos")]
    [SerializeField] AudioClip[] steps;
    [Tooltip("Metros andados entre um passo e o próximo (andando e correndo).")]
    [SerializeField] Vector2 stride = new Vector2(0.75f, 1.05f);
    [Tooltip("Volume andando e correndo.")]
    [SerializeField] Vector2 stepVolume = new Vector2(0.45f, 0.7f);

    [Header("Lanterna")]
    [SerializeField] AudioClip flashlightClick;
    [SerializeField, Range(0f, 1f)] float flashlightVolume = 0.6f;

    [Header("Pegar item")]
    [SerializeField] AudioClip[] pickups;
    [SerializeField, Range(0f, 1f)] float pickupVolume = 0.7f;

    CharacterController body;
    LevelWalker walker;
    PlayerFlashlight flashlight;
    AudioSource stepSource, fxSource;   // separadas: o pitch de uma não muda o som da outra
    Vector3 lastPos;
    float walked;
    int lastStep = -1;

    void Awake()
    {
        body = GetComponent<CharacterController>();
        walker = GetComponent<LevelWalker>();
        flashlight = GetComponentInChildren<PlayerFlashlight>(true);
        stepSource = NewSource();
        fxSource = NewSource();
        lastPos = transform.position;
    }

    AudioSource NewSource()
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.spatialBlend = 0f;
        return s;
    }

    void Start()
    {
        foreach (var p in FindObjectsByType<Pickup>(FindObjectsInactive.Include))
            if (p.kind == Pickup.Kind.Item) p.onPicked.AddListener(_ => PlayPickup());
    }

    void OnEnable()
    {
        if (flashlight != null) flashlight.Toggled += OnFlashlight;
    }

    void OnDisable()
    {
        if (flashlight != null) flashlight.Toggled -= OnFlashlight;
    }

    void Update()
    {
        Vector3 pos = transform.position;
        Vector3 delta = pos - lastPos;
        lastPos = pos;
        delta.y = 0f;
        float moved = delta.magnitude;

        // Teleporte (checkpoint, esconderijo) ou parado/no ar: não conta como passo.
        if (moved > 1f || !body.isGrounded || (walker != null && !walker.enabled) || Time.deltaTime <= 0f) return;

        float speed = moved / Time.deltaTime;
        bool running = walker != null && speed > walker.walk * 1.25f;
        walked += moved;
        if (walked < (running ? stride.y : stride.x)) return;
        walked = 0f;
        PlayStep(running);
    }

    void PlayStep(bool running)
    {
        if (steps == null || steps.Length == 0) return;
        int i = Random.Range(0, steps.Length);
        if (steps.Length > 1 && i == lastStep) i = (i + 1) % steps.Length;   // nunca o mesmo passo duas vezes
        lastStep = i;
        stepSource.pitch = Random.Range(0.92f, 1.08f) * (running ? 1.05f : 1f);
        stepSource.PlayOneShot(steps[i], (running ? stepVolume.y : stepVolume.x) * Random.Range(0.85f, 1f));
    }

    void OnFlashlight(bool on)
    {
        if (flashlightClick == null) return;
        fxSource.pitch = on ? 1.05f : 0.9f;   // o mesmo clique: ligar mais agudo, desligar mais grave
        fxSource.PlayOneShot(flashlightClick, flashlightVolume);
    }

    public void PlayPickup()
    {
        if (pickups == null || pickups.Length == 0) return;
        fxSource.pitch = Random.Range(0.95f, 1.05f);
        fxSource.PlayOneShot(pickups[Random.Range(0, pickups.Length)], pickupVolume);
    }
}
