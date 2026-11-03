using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Dispara um raycast do centro da camera e interage com IInteractable ao apertar E.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] Transform viewCamera;
    [SerializeField] float interactDistance = 2.5f;
    [SerializeField] LayerMask interactMask = ~0;
    [SerializeField] TMP_Text promptLabel;

    InputAction interactAction;
    IInteractable current;

    void Awake()
    {
        if (viewCamera == null && Camera.main != null) viewCamera = Camera.main.transform;
        interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
        interactAction.AddBinding("<Gamepad>/buttonSouth");
    }

    public Transform ViewCamera => viewCamera;

    void OnEnable() => interactAction.Enable();

    void OnDisable()
    {
        interactAction.Disable();
        current = null;
        if (promptLabel != null) promptLabel.text = string.Empty;
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

        if (promptLabel != null)
            promptLabel.text = current != null ? $"[E] {current.Prompt}" : string.Empty;

        if (current != null && interactAction.WasPressedThisFrame())
            current.Interact(this);
    }
}
