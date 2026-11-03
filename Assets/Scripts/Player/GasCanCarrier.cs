using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Fica no jogador (junto do LevelWalker). Carregando o galao ele anda mais devagar, nao corre e o galao balanca a cada
/// passo. A lanterna nao e afetada. Largar: a tecla configurada (padrao G). O galao fica no chao e pode ser pego de novo.
/// Dentro de um minigame o jogador tem os scripts desligados (inclusive este): nada acontece ate ele sair.
/// </summary>
public class GasCanCarrier : MonoBehaviour
{
    [SerializeField] HeyHeyThere.PSXCarnivalKit.LevelWalker walker;
    [Tooltip("Onde o galao fica na mao (camera do jogador).")]
    [SerializeField] Transform view;

    [Header("Movimento carregando")]
    [Tooltip("Fracao da velocidade normal de andar.")]
    [SerializeField, Range(0.1f, 1f)] float speedMultiplier = 0.8f;
    [SerializeField] bool canRun;

    [Header("Soltar")]
    [SerializeField] Key dropKey = Key.G;
    [Tooltip("Distancia a frente do jogador onde o galao cai.")]
    [SerializeField] float dropDistance = 0.8f;

    [Header("Na mao")]
    [SerializeField] Vector3 heldPosition = new Vector3(0.3f, -0.42f, 0.55f);
    [SerializeField] Vector3 heldEuler = new Vector3(0f, -25f, 0f);

    [Header("Som de gasolina balancando")]
    [Tooltip("Vazio = som gerado em codigo.")]
    [SerializeField] AudioClip[] sloshClips;
    [SerializeField, Range(0f, 1f)] float sloshVolume = 0.6f;
    [Tooltip("Metros andados entre um balanco e o proximo.")]
    [SerializeField] float stepDistance = 0.85f;

    public GasCan Held { get; private set; }
    public bool IsCarrying => Held != null;

    Transform heldHome;
    Vector3 heldHomeScale;
    float baseWalk, baseRun, walked;
    Vector3 lastPos;
    AudioSource source;
    AudioClip[] fallback;
    bool visible = true;

    void Awake()
    {
        if (walker == null) walker = GetComponent<HeyHeyThere.PSXCarnivalKit.LevelWalker>();
        if (view == null && walker != null) view = walker.view;
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    public void PickUp(GasCan can)
    {
        if (IsCarrying || can == null) return;
        Held = can;
        can.SetHeld(true);
        heldHome = can.transform.parent;
        heldHomeScale = can.transform.localScale;
        can.transform.SetParent(view, false);
        can.transform.localPosition = heldPosition;
        can.transform.localRotation = Quaternion.Euler(heldEuler);

        baseWalk = walker.walk;
        baseRun = walker.run;
        walker.walk = baseWalk * speedMultiplier;
        walker.run = canRun ? baseRun * speedMultiplier : walker.walk;
        walked = 0f;
        lastPos = transform.position;
        Jamlloween.Progression.ProgressionDirector.ShowText($"[{dropKey}] largar o galão");
    }

    /// <summary>Larga o galao no chao a frente do jogador. Devolve o galao (ou null se nao carregava).</summary>
    public GasCan Drop()
    {
        if (!IsCarrying) return null;
        var can = Held;
        Release(can);
        Vector3 at = transform.position + transform.forward * dropDistance;
        if (Physics.Raycast(at + Vector3.up * 1.5f, Vector3.down, out var hit, 4f, ~0, QueryTriggerInteraction.Ignore)) at = hit.point;
        else at.y = transform.position.y - 0.9f;   // sem chao embaixo: usa a altura dos pes
        can.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
        return can;
    }

    /// <summary>Tira o galao da mao sem larga-lo em lugar nenhum (quem chama o posiciona). Restaura a velocidade.</summary>
    public GasCan TakeFromHands()
    {
        if (!IsCarrying) return null;
        var can = Held;
        Release(can);
        return can;
    }

    void Release(GasCan can)
    {
        can.transform.SetParent(heldHome, true);
        can.transform.localScale = heldHomeScale;
        can.SetHeld(false);
        can.gameObject.SetActive(true);
        Held = null;
        visible = true;
        walker.walk = baseWalk;
        walker.run = baseRun;
    }

    /// <summary>O minigame esconde o galao da mao enquanto mostra o dele (o galao inclinando).</summary>
    public void SetHeldVisible(bool value)
    {
        visible = value;
        if (Held != null) Held.gameObject.SetActive(value);
    }

    void Update()
    {
        if (!IsCarrying) return;

        var k = Keyboard.current;
        if (k != null && k[dropKey].wasPressedThisFrame)
        {
            Drop();
            return;
        }

        // Balanco a cada passo: pela distancia andada no plano.
        Vector3 p = transform.position;
        Vector3 d = p - lastPos;
        d.y = 0f;
        lastPos = p;
        walked += d.magnitude;
        if (walked >= stepDistance)
        {
            walked = 0f;
            PlaySlosh();
        }
    }

    void PlaySlosh()
    {
        AudioClip clip;
        if (sloshClips != null && sloshClips.Length > 0) clip = sloshClips[Random.Range(0, sloshClips.Length)];
        else
        {
            if (fallback == null || fallback.Length == 0) fallback = new[] { FuelAudio.Slosh(3), FuelAudio.Slosh(4), FuelAudio.Slosh(6) };
            clip = fallback[Random.Range(0, fallback.Length)];
        }
        source.pitch = Random.Range(0.92f, 1.08f);
        source.PlayOneShot(clip, sloshVolume);
    }
}
