using Jamlloween.Core;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Dispara um raycast do centro da camera e interage com IInteractable ao apertar E.
/// Desenha o prompt "[E] ..." no estilo OSD do jogo (UIStyle), o mesmo das falas do rádio.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] Transform viewCamera;
    [SerializeField] float interactDistance = 2.5f;
    [SerializeField] LayerMask interactMask = ~0;
    [Tooltip("Prompt único dos objetos interagíveis. Só os coletáveis (ICollectible) mostram o texto próprio (\"Pegar ...\").")]
    [SerializeField] string promptText = "Interagir";
    [Tooltip("Altura do centro do prompt, em fração da tela a partir do topo (0.5 = mira).")]
    [SerializeField, Range(0f, 1f)] float promptHeight = 0.575f;

    InputAction interactAction;
    IInteractable current;
    HeyHeyThere.PSXCarnivalKit.LevelWalker walker;
    string shown = string.Empty;

    void Awake()
    {
        if (viewCamera == null && Camera.main != null) viewCamera = Camera.main.transform;
    }

    public Transform ViewCamera => viewCamera;

    void OnEnable()
    {
        // Os itens do kit (fusível, pilhas) são achados pelo LevelWalker; o texto deles sai aqui, no mesmo
        // estilo dos outros prompts, em vez do OnGUI do kit. Fica no OnEnable (e não no Awake), com a ação do E, porque ele roda
        // de novo quando o Unity recompila com o jogo rodando.
        walker = GetComponent<HeyHeyThere.PSXCarnivalKit.LevelWalker>();
        if (walker != null) walker.DrawAimPrompt = false;
        if (interactAction == null)
        {
            interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
            interactAction.AddBinding("<Gamepad>/buttonSouth");
        }
        interactAction.Enable();
    }

    void OnDisable()
    {
        interactAction.Disable();
        current = null;
        shown = string.Empty;
    }

    void Update()
    {
        current = null;
        var hits = Physics.RaycastAll(viewCamera.position, viewCamera.forward, interactDistance,
            interactMask, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;   // ignora o proprio jogador
            current = hit.collider.GetComponentInParent<IInteractable>();
            if (current == null && hit.collider.isTrigger) continue;     // areas invisiveis (gatilhos de etapa, zonas) nao bloqueiam
            if (current != null && string.IsNullOrEmpty(current.Prompt)) current = null;
            break;                                                        // o primeiro objeto na frente decide
        }

        shown = current != null ? $"[E] {(current is ICollectible ? current.Prompt : promptText)}"
            : walker != null ? walker.AimPrompt : string.Empty;

        if (current != null && interactAction.WasPressedThisFrame())
            current.Interact(this);
    }

    void OnGUI()
    {
        if (string.IsNullOrEmpty(shown)) return;
        float w = Screen.width, h = Screen.height;
        float height = 40 * UIStyle.Scale;
        UIStyle.OsdText(new Rect(0f, h * promptHeight - height * 0.5f, w, height), shown, UIStyle.Prompt);
    }
}
