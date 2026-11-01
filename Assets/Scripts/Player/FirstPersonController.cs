using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controle em primeira pessoa: WASD anda, Shift corre, mouse olha.
/// Use SetInputEnabled(false) para travar o jogador (ex.: durante um minigame).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] float walkSpeed = 3.5f;
    [SerializeField] float runSpeed = 6.5f;
    [SerializeField] float acceleration = 12f;
    [SerializeField] float gravity = -20f;

    [Header("Camera")]
    [SerializeField] Transform cameraPivot;
    [SerializeField] float lookSensitivity = 0.1f;
    [SerializeField] float maxPitch = 85f;

    [Header("Head bob")]
    [SerializeField] float bobAmount = 0.04f;
    [SerializeField] float bobFrequency = 1.6f;

    CharacterController controller;
    InputAction moveAction, lookAction, sprintAction;
    Vector3 horizontalVelocity;
    float verticalVelocity, pitch, bobTimer, pivotBaseY;
    bool inputEnabled = true;

    public bool IsRunning { get; private set; }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraPivot == null && Camera.main != null) cameraPivot = Camera.main.transform.parent;
        pivotBaseY = cameraPivot.localPosition.y;

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");

        lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
        lookAction.AddBinding("<Gamepad>/rightStick").WithProcessor("scaleVector2(x=12,y=12)");

        sprintAction = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
        sprintAction.AddBinding("<Gamepad>/leftStickPress");
    }

    void OnEnable()
    {
        moveAction.Enable(); lookAction.Enable(); sprintAction.Enable();
        SetCursorLocked(true);
    }

    void OnDisable()
    {
        moveAction.Disable(); lookAction.Disable(); sprintAction.Disable();
        SetCursorLocked(false);
    }

    public void SetInputEnabled(bool value)
    {
        inputEnabled = value;
        SetCursorLocked(value);
    }

    static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void Update()
    {
        Vector2 move = inputEnabled ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector2 look = inputEnabled ? lookAction.ReadValue<Vector2>() : Vector2.zero;

        // Olhar
        transform.Rotate(0f, look.x * lookSensitivity, 0f);
        pitch = Mathf.Clamp(pitch - look.y * lookSensitivity, -maxPitch, maxPitch);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // Andar / correr (so corre indo pra frente)
        IsRunning = inputEnabled && sprintAction.IsPressed() && move.y > 0.1f;
        float speed = IsRunning ? runSpeed : walkSpeed;
        Vector3 target = (transform.right * move.x + transform.forward * move.y);
        if (target.sqrMagnitude > 1f) target.Normalize();
        target *= speed;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, acceleration * Time.deltaTime * speed);

        // Gravidade
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

        // Head bob
        bool moving = controller.isGrounded && horizontalVelocity.sqrMagnitude > 0.1f;
        bobTimer = moving ? bobTimer + Time.deltaTime * (IsRunning ? 1.5f : 1f) * bobFrequency * Mathf.PI * 2f : 0f;
        float bob = moving ? Mathf.Sin(bobTimer) * bobAmount * (IsRunning ? 1.5f : 1f) : 0f;
        Vector3 p = cameraPivot.localPosition;
        p.y = Mathf.Lerp(p.y, pivotBaseY + bob, Time.deltaTime * 14f);
        cameraPivot.localPosition = p;
    }
}
