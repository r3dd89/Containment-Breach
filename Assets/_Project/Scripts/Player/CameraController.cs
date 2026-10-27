using UnityEngine;
using UnityEngine.InputSystem;

/*
 * Script Name: CameraController
 * Purpose:
 * Controls the first-person player's camera using the PlayerInputActions asset.
 *
 * Supported input is defined in PlayerInputActions.inputactions.
 *
 * Current camera behavior:
 * - Mouse look
 * - Keyboard-only look using arrow keys
 * - Controller look using the right stick
 * - Horizontal player-body rotation
 * - Vertical camera rotation
 * - Vertical look clamping
 * - Cursor locking and hiding
 * - Camera crouch synchronization with PlayerMovement
 *
 * TODO:
 * - Add runtime sensitivity settings.
 * - Add invert-Y option.
 * - Add runtime input rebinding support.
 */

public class CameraController : MonoBehaviour
{
    #region Component References

    [Header("Component References")]

    [SerializeField]
    [Tooltip("The Player object that rotates left and right with the camera.")]
    private Transform playerBody;

    [SerializeField]
    [Tooltip("Provides the player's actual crouch state so the camera stays synchronized with movement.")]
    private PlayerMovement playerMovement;

    #endregion


    #region Mouse Look Settings

    [Header("Mouse Look Settings")]

    [SerializeField]
    [Tooltip("Controls how strongly mouse delta affects camera rotation.")]
    private float mouseSensitivity = 0.15f;

    #endregion


    #region Digital Look Settings

    [Header("Digital Look Settings")]

    [SerializeField]
    [Tooltip("Controls how quickly digital look input, such as arrow keys, rotates the camera.")]
    private float digitalLookSpeed = 120f;

    #endregion


    #region Controller Look Settings

    [Header("Controller Look Settings")]

    [SerializeField]
    [Tooltip("Controls how quickly controller right-stick input rotates the camera.")]
    private float controllerLookSpeed = 150f;

    #endregion


    #region Look Limits

    [Header("Look Limits")]

    [SerializeField]
    [Tooltip("The maximum angle the player can look upward or downward.")]
    private float verticalLookLimit = 85f;

    #endregion


    #region Crouch Camera Settings

    [Header("Crouch Camera Settings")]

    [SerializeField]
    [Tooltip("The camera's local Y position while the player is standing.")]
    private float standingCameraHeight = 0.75f;

    [SerializeField]
    [Tooltip("The camera's local Y position while the player is crouching.")]
    private float crouchingCameraHeight = 0.25f;

    [SerializeField]
    [Tooltip("How quickly the camera moves between standing and crouching height.")]
    private float crouchCameraSpeed = 8f;

    #endregion


    #region Cursor Settings

    [Header("Cursor Settings")]

    [SerializeField]
    [Tooltip("Locks and hides the mouse cursor when gameplay begins.")]
    private bool lockCursorOnStart = true;

    #endregion


    #region Input

    /*
     * PlayerInputActions is generated from:
     *
     * Assets/_Project/Input/PlayerInputActions.inputactions
     */
    private PlayerInputActions playerInputActions;

    #endregion


    #region Runtime Variables

    // Stores the current vertical camera rotation.
    private float verticalRotation;

    #endregion


    #region Unity Methods

    private void Awake()
    {
        // Automatically find PlayerMovement if it was not assigned.
        if (playerMovement == null &&
            playerBody != null)
        {
            playerMovement =
                playerBody.GetComponent<PlayerMovement>();
        }

        // Create the generated Input Actions wrapper.
        playerInputActions =
            new PlayerInputActions();
    }


    private void OnEnable()
    {
        // Enable the Player action map while this component is active.
        if (playerInputActions != null)
        {
            playerInputActions.Player.Enable();
        }
    }


    private void OnDisable()
    {
        // Disable input when this component is disabled.
        if (playerInputActions != null)
        {
            playerInputActions.Player.Disable();
        }
    }


    private void OnDestroy()
    {
        // Release Input Actions resources when this component is destroyed.
        if (playerInputActions != null)
        {
            playerInputActions.Dispose();
        }
    }


    private void Start()
    {
        // Lock and hide the cursor when gameplay begins.
        if (lockCursorOnStart)
        {
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible =
                false;
        }

        // Ensure the camera begins at standing height.
        Vector3 startingPosition =
            transform.localPosition;

        startingPosition.y =
            standingCameraHeight;

        transform.localPosition =
            startingPosition;
    }


    private void Update()
    {
        // Process camera rotation from the Look action.
        HandleLookInput();

        // Keep camera height synchronized with actual crouch state.
        HandleCrouchCamera();
    }

    #endregion


    #region Look Input

    private void HandleLookInput()
    {
        /*
         * Read the Look action as a Vector2.
         *
         * Current bindings:
         * - Mouse Delta
         * - Arrow-key 2D Vector
         * - Gamepad Right Stick
         */
        Vector2 lookInput =
            playerInputActions.Player.Look.ReadValue<Vector2>();

        float horizontalLook;
        float verticalLook;

        /*
         * Input Actions combines all bindings into one action value.
         *
         * Mouse delta behaves differently from stick/digital input:
         * mouse values are already frame-based pixel deltas, while
         * stick and keyboard values represent directional strength.
         *
         * The active control is checked so each device can use
         * an appropriate sensitivity model.
         */
        InputControl activeControl =
            playerInputActions.Player.Look.activeControl;

        bool usingMouse =
            activeControl != null &&
            activeControl.device is Mouse;

        bool usingGamepad =
            activeControl != null &&
            activeControl.device is Gamepad;

        if (usingMouse)
        {
            // Mouse delta does not need Time.deltaTime.
            horizontalLook =
                lookInput.x *
                mouseSensitivity;

            verticalLook =
                lookInput.y *
                mouseSensitivity;
        }
        else if (usingGamepad)
        {
            // Right-stick input requires frame-rate-independent scaling.
            horizontalLook =
                lookInput.x *
                controllerLookSpeed *
                Time.deltaTime;

            verticalLook =
                lookInput.y *
                controllerLookSpeed *
                Time.deltaTime;
        }
        else
        {
            // Arrow-key look uses digital directional input.
            horizontalLook =
                lookInput.x *
                digitalLookSpeed *
                Time.deltaTime;

            verticalLook =
                lookInput.y *
                digitalLookSpeed *
                Time.deltaTime;
        }

        ApplyLookRotation(
            horizontalLook,
            verticalLook);
    }


    private void ApplyLookRotation(
        float horizontalLook,
        float verticalLook)
    {
        // Positive vertical input should look upward.
        verticalRotation -=
            verticalLook;

        // Prevent excessive upward or downward rotation.
        verticalRotation =
            Mathf.Clamp(
                verticalRotation,
                -verticalLookLimit,
                verticalLookLimit);

        // Apply vertical rotation to the camera only.
        transform.localRotation =
            Quaternion.Euler(
                verticalRotation,
                0f,
                0f);

        // Apply horizontal rotation to the entire Player object.
        if (playerBody != null)
        {
            playerBody.Rotate(
                Vector3.up *
                horizontalLook);
        }
    }

    #endregion


    #region Crouch Camera Methods

    private void HandleCrouchCamera()
    {
        // Standing height is the default.
        float targetHeight =
            standingCameraHeight;

        /*
         * Camera height follows the actual gameplay crouch state
         * instead of reading an input binding directly.
         *
         * This keeps camera position correct even when headroom
         * prevents the player from standing.
         */
        if (playerMovement != null &&
            playerMovement.IsCrouching)
        {
            targetHeight =
                crouchingCameraHeight;
        }

        // Read the current local camera position.
        Vector3 currentPosition =
            transform.localPosition;

        // Smoothly move toward the desired height.
        currentPosition.y =
            Mathf.Lerp(
                currentPosition.y,
                targetHeight,
                crouchCameraSpeed *
                Time.deltaTime);

        // Apply the updated camera position.
        transform.localPosition =
            currentPosition;
    }

    #endregion
}