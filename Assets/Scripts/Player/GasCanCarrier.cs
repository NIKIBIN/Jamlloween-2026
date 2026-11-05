using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Fica no jogador (junto do LevelWalker). Carregando o galao, o galao balanca a cada passo. Por padrao o jogador anda e
/// corre na velocidade normal (o mapa e grande); speedMultiplier e canRun deixam o galao pesar, se precisar. A lanterna nao e afetada. Largar: a tecla configurada (padrao G). O galao fica no chao e pode ser pego de novo.
/// Dentro de um minigame o jogador tem os scripts desligados (inclusive este): nada acontece ate ele sair.
/// </summary>
public class GasCanCarrier : MonoBehaviour
{
    [SerializeField] HeyHeyThere.PSXCarnivalKit.LevelWalker walker;
    [Tooltip("Onde o galao fica na mao (camera do jogador).")]
    [SerializeField] Transform view;

    [Header("Movimento carregando")]
    [Tooltip("Fracao da velocidade normal de andar (e de correr, com canRun). 1 = sem lentidao.")]
    [SerializeField, Range(0.1f, 1f)] float speedMultiplier = 1f;
    [Tooltip("Desligado: carregando o galao o jogador so anda.")]
    [SerializeField] bool canRun = true;

    [Header("Soltar")]
    [SerializeField] Key dropKey = Key.G;
    [Tooltip("Folga entre a capsula do jogador e o galao largado. O galao cai encostado no pe, de lado.")]
    [SerializeField] float dropGap = 0.03f;

    [Header("Na mao")]
    [SerializeField] Vector3 heldPosition = new Vector3(0.32f, -0.55f, 0.62f);
    [SerializeField] Vector3 heldEuler = new Vector3(0f, -25f, 0f);

    [Header("Som de gasolina balancando")]
    [Tooltip("Vazio = som gerado em codigo.")]
    [SerializeField] AudioClip[] sloshClips;
    [SerializeField, Range(0f, 1f)] float sloshVolume = 0.2f;
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

    /// <summary>Larga o galao no chao, encostado no pe do jogador. Devolve o galao (ou null se nao carregava).</summary>
    public GasCan Drop()
    {
        if (!IsCarrying) return null;
        var can = Held;
        var body = GetComponent<CharacterController>();
        float ahead = (body != null ? body.radius + body.skinWidth : 0.3f) + CanHalfThickness + dropGap;
        Vector3 at = DropPoint(transform.position, transform.forward, ahead, out bool behind);   // antes do Release: o galao ainda esta sem collider
        Release(can);
        // De lado (+90): a face estreita do galao fica virada para o jogador, o mais perto possivel do pe.
        can.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, transform.eulerAngles.y + (behind ? -90f : 90f), 0f));
        return can;
    }

    const float KneeHeight = 0.5f;          // altura de onde se procura obstaculo e chao: abaixo de mesas e maquinas
    const float CanHalfThickness = 0.09f;   // SM_jerry_can tem 0.174 m de largura

    /// <summary>
    /// Ponto no chao para largar o galao, a 'ahead' metros do centro do jogador. Antes o galao ia 0.8 m a frente e o
    /// chao era procurado com um raio a 1.5 m de altura: colado no gerador, o ponto caia dentro do collider dele e o
    /// galao ia parar dentro da maquina. Agora: na frente se houver espaco na altura do joelho, senao atras; o chao e
    /// procurado a partir do joelho, sem subir em nada.
    /// </summary>
    public static Vector3 DropPoint(Vector3 feet, Vector3 forward, float ahead, out bool behind)
    {
        forward.y = 0f;
        forward = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward;
        Vector3 knee = feet + Vector3.up * KneeHeight;

        float Free(Vector3 dir) =>
            Physics.Raycast(knee, dir, out var hit, ahead + CanHalfThickness, ~0, QueryTriggerInteraction.Ignore)
                ? hit.distance - CanHalfThickness : ahead;
        float front = Free(forward);
        behind = false;
        if (front < ahead)
        {
            float back = Free(-forward);
            if (back > front) { forward = -forward; front = back; behind = true; }
        }

        Vector3 at = feet + forward * Mathf.Max(0f, Mathf.Min(ahead, front));
        if (Physics.Raycast(at + Vector3.up * KneeHeight, Vector3.down, out var ground, KneeHeight + 2f, ~0, QueryTriggerInteraction.Ignore))
            at.y = ground.point.y;
        else
            at.y = feet.y;   // sem chao embaixo: a altura dos pes
        return at;
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
