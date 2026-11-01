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

    void OnEnable() => interactAction.Enable();
    void OnDisable() => interactAction.Disable();

    void Update()
    {
        current = null;
        if (Physics.Raycast(viewCamera.position, viewCamera.forward, out var hit, interactDistance,
                interactMask, QueryTriggerInteraction.Collide))
        {
            current = hit.collider.GetComponentInParent<IInteractable>();
            if (current != null && string.IsNullOrEmpty(current.Prompt)) current = null;
        }

        if (promptLabel != null)
            promptLabel.text = current != null ? $"[E] {current.Prompt}" : string.Empty;

        if (current != null && interactAction.WasPressedThisFrame())
            current.Interact(this);
    }
}
