using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Base de um minigame diegetico: o jogo acontece no proprio objeto 3D, sem tela por cima.
///
/// Fluxo: o jogador interage (E) -> a camera vai ate o viewPoint e o player e travado ->
/// o minigame roda em Tick() -> Esc/Q (ou Exit() de fora) devolve a camera e o controle.
/// O progresso vive nos campos do componente, entao sair no meio NAO reseta nada.
///
/// Reuso: arraste o prefab para qualquer cena e ligue os eventos onStarted / onExited / onCompleted.
/// Quem controla uma perseguicao pode chamar Exit() para tirar o jogador do minigame.
/// </summary>
public abstract class Minigame : MonoBehaviour, IInteractable
{
    public enum State { Idle, Entering, Playing, Exiting }

    [Header("Interacao")]
    [SerializeField] string prompt = "Consertar";

    [Header("Camera")]
    [Tooltip("Onde a camera do jogador fica durante o minigame. Olha na direcao +Z deste transform.")]
    [SerializeField] Transform viewPoint;
    [SerializeField] float enterTime = 0.7f;
    [SerializeField] float exitTime = 0.35f;
    [Tooltip("Tempo entre concluir e devolver o controle ao jogador.")]
    [SerializeField] float exitDelayAfterComplete = 0.9f;

    [Header("Durante o minigame")]
    [Tooltip("Objetos ligados so enquanto o minigame esta ativo (ex.: luz de trabalho).")]
    [SerializeField] GameObject[] activeWhilePlaying;
    [SerializeField] bool showExitHint = true;
    [Tooltip("Apertar uma tecla de movimento (WASD, setas, Shift ou o stick) sai do minigame, ideal para fugir de uma perseguicao.")]
    [SerializeField] bool exitOnMovementInput = true;
    [Tooltip("Duracao da volta da camera quando se sai por movimento. Menor que exitTime para devolver o controle rapido.")]
    [SerializeField] float quickExitTime = 0.2f;

    [Header("Eventos")]
    public UnityEvent onStarted;
    [Tooltip("Disparado sempre que o jogador volta ao controle, tenha concluido ou nao.")]
    public UnityEvent onExited;
    [Tooltip("Disparado uma unica vez, quando o minigame e concluido.")]
    public UnityEvent onCompleted;

    /// <summary>
    /// Opt-in gate for a whole scene: when set and it returns a text, no minigame can be entered and that text is
    /// shown as the prompt (ex.: "Precisa da lanterna"). Null or an empty text lets the player in.
    /// </summary>
    public static System.Func<string> EntryBlocker;
    /// <summary>Any minigame started / any minigame left. Lets scene-wide systems (the flashlight) follow the player in and out.</summary>
    public static event System.Action<Minigame> AnyStarted, AnyExited;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { EntryBlocker = null; AnyStarted = null; AnyExited = null; }

    static string BlockedBy => EntryBlocker?.Invoke();

    public State CurrentState { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool IsPlaying => CurrentState == State.Playing;
    public string Prompt
    {
        get
        {
            if (IsCompleted || CurrentState != State.Idle) return string.Empty;
            string blocked = BlockedBy;
            return string.IsNullOrEmpty(blocked) ? prompt : blocked;
        }
    }

    Transform camTransform, camParent;
    Camera cam;
    Vector3 camSavedPos;
    Quaternion camSavedRot;
    readonly List<Behaviour> lockedBehaviours = new List<Behaviour>();
    CursorLockMode previousLock;
    bool previousVisible;
    Coroutine moveRoutine;

    protected virtual void Awake() => SetPlayingObjects(false);

    public void Interact(PlayerInteractor interactor) => Begin(interactor);

    /// <summary>Entra no minigame. O interactor identifica o jogador (e a camera dele).</summary>
    public void Begin(PlayerInteractor interactor)
    {
        if (IsCompleted || CurrentState != State.Idle || viewPoint == null) return;
        if (!string.IsNullOrEmpty(BlockedBy)) return;

        camTransform = interactor.ViewCamera;
        cam = camTransform.GetComponent<Camera>();
        camParent = camTransform.parent;
        camSavedPos = camParent != null ? camTransform.localPosition : camTransform.position;
        camSavedRot = camParent != null ? camTransform.localRotation : camTransform.rotation;

        // Trava o jogador: desliga os scripts dele (movimento, interacao, etc.).
        lockedBehaviours.Clear();
        foreach (var b in interactor.GetComponents<Behaviour>())
            if (b.enabled) { lockedBehaviours.Add(b); b.enabled = false; }

        previousLock = Cursor.lockState;
        previousVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetPlayingObjects(true);
        OnBegin(interactor);
        CurrentState = State.Entering;
        onStarted?.Invoke();
        AnyStarted?.Invoke(this);
        StartMove(true, enterTime);
    }

    /// <summary>Sai do minigame mantendo o progresso. Seguro de chamar a qualquer momento.</summary>
    public void Exit() => ExitInternal(exitTime);

    void ExitInternal(float duration)
    {
        if (CurrentState == State.Idle || CurrentState == State.Exiting) return;
        CurrentState = State.Exiting;
        OnExitRequested();
        StartMove(false, duration);
    }

    /// <summary>Conclui o minigame. exitDelay controla quanto o jogador fica antes de voltar ao controle (padrao: exitDelayAfterComplete).</summary>
    protected void Complete(float exitDelay = -1f)
    {
        if (IsCompleted) return;
        IsCompleted = true;
        Debug.Log($"[Minigame] {name} concluido", this);
        onCompleted?.Invoke();
        StartCoroutine(ExitAfter(exitDelay >= 0f ? exitDelay : exitDelayAfterComplete));
    }

    /// <summary>Chamado a cada frame enquanto o minigame esta em andamento.</summary>
    protected abstract void Tick(Camera viewCamera);

    /// <summary>Chamado ao entrar, com o jogador que interagiu (para ler o inventario dele, por exemplo).</summary>
    protected virtual void OnBegin(PlayerInteractor interactor) { }

    /// <summary>Chamado quando o jogador pede para sair (use para soltar o que estiver na mao).</summary>
    protected virtual void OnExitRequested() { }

    /// <summary>Se false, ESC/Q e as teclas de movimento nao tiram o jogador do minigame (ex.: saida forcada por um jumpscare). Exit() chamado por codigo continua valendo.</summary>
    protected virtual bool PlayerCanExit => true;

    void Update()
    {
        if (CurrentState != State.Entering && CurrentState != State.Playing) return;

        var k = Keyboard.current;
        if (!PlayerCanExit)
        {
            if (CurrentState == State.Playing) Tick(cam);
            return;
        }
        if (k != null && (k.escapeKey.wasPressedThisFrame || k.qKey.wasPressedThisFrame))
        {
            Exit();
            return;
        }
        if (exitOnMovementInput && MovementPressed(k))
        {
            ExitInternal(quickExitTime);
            return;
        }

        if (CurrentState == State.Playing) Tick(cam);
    }

    bool stickWasActive;

    /// <summary>True no frame em que o jogador comeca a apertar uma tecla de movimento. Teclas ja seguradas ao entrar nao contam.</summary>
    bool MovementPressed(Keyboard k)
    {
        if (k != null && (k.wKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame
            || k.upArrowKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame
            || k.leftArrowKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame
            || k.leftShiftKey.wasPressedThisFrame || k.rightShiftKey.wasPressedThisFrame))
            return true;

        var pad = Gamepad.current;
        bool stickActive = pad != null && pad.leftStick.ReadValue().magnitude > 0.5f;
        bool pressed = stickActive && !stickWasActive;
        stickWasActive = stickActive;
        return pressed;
    }

    IEnumerator ExitAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Exit();
    }

    void StartMove(bool entering, float duration)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveCamera(entering, duration));
    }

    void GetHomePose(out Vector3 pos, out Quaternion rot)
    {
        if (camParent != null)
        {
            pos = camParent.TransformPoint(camSavedPos);
            rot = camParent.rotation * camSavedRot;
        }
        else { pos = camSavedPos; rot = camSavedRot; }
    }

    /// <summary>Onde a camera fica ao entrar. Sobrescreva para escolher o enquadramento conforme o estado.</summary>
    protected virtual Transform EntryViewPoint => viewPoint;

    /// <summary>Move a camera suavemente para outro enquadramento durante o minigame.</summary>
    protected void GlideView(Transform target, float duration)
    {
        if (CurrentState != State.Playing || camTransform == null || target == null) return;
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(GlideRoutine(target, duration));
    }

    IEnumerator GlideRoutine(Transform target, float duration)
    {
        Vector3 p0 = camTransform.position;
        Quaternion r0 = camTransform.rotation;
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(duration, 0.01f))
        {
            float k = Mathf.SmoothStep(0f, 1f, t);
            camTransform.SetPositionAndRotation(Vector3.Lerp(p0, target.position, k), Quaternion.Slerp(r0, target.rotation, k));
            yield return null;
        }
        camTransform.SetPositionAndRotation(target.position, target.rotation);
    }

    IEnumerator MoveCamera(bool entering, float duration)
    {
        Vector3 startPos = camTransform.position;
        Quaternion startRot = camTransform.rotation;
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(duration, 0.01f))
        {
            Vector3 endPos; Quaternion endRot;
            if (entering) { var v = EntryViewPoint; endPos = v.position; endRot = v.rotation; }
            else GetHomePose(out endPos, out endRot);
            float k = Mathf.SmoothStep(0f, 1f, t);
            camTransform.SetPositionAndRotation(Vector3.Lerp(startPos, endPos, k), Quaternion.Slerp(startRot, endRot, k));
            yield return null;
        }

        if (entering)
        {
            var final = EntryViewPoint;
            camTransform.SetPositionAndRotation(final.position, final.rotation);
            CurrentState = State.Playing;
            yield break;
        }

        // Devolve a camera e o controle ao jogador.
        if (camParent != null) { camTransform.localPosition = camSavedPos; camTransform.localRotation = camSavedRot; }
        else camTransform.SetPositionAndRotation(camSavedPos, camSavedRot);
        foreach (var b in lockedBehaviours) if (b != null) b.enabled = true;
        lockedBehaviours.Clear();
        Cursor.lockState = previousLock;
        Cursor.visible = previousVisible;
        SetPlayingObjects(false);
        CurrentState = State.Idle;
        onExited?.Invoke();
        AnyExited?.Invoke(this);
    }

    void SetPlayingObjects(bool value)
    {
        if (activeWhilePlaying == null) return;
        foreach (var go in activeWhilePlaying) if (go != null) go.SetActive(value);
    }

    void OnGUI()
    {
        if (!showExitHint || CurrentState != State.Playing || !PlayerCanExit) return;
        float h = Screen.height;
        Circus.UIStyle.OsdText(new Rect(0, h * 0.92f, Screen.width, 30 * Circus.UIStyle.Scale), "[ESC] SAIR", Circus.UIStyle.Prompt, 0.9f);
    }

    void OnDrawGizmosSelected()
    {
        if (viewPoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(viewPoint.position, 0.03f);
        Gizmos.DrawRay(viewPoint.position, viewPoint.forward * 0.4f);
    }
}
