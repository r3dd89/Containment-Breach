using UnityEngine;
using UnityEngine.InputSystem;

/*
 * Script Name: CameraController
 * Purpose:
 * Controls the first-person player's camera.
 *
 * Supported input:
 * - Mouse look
 * - Keyboard-only look using the arrow keys
 * - Controller look using the right stick
 *
 * Current camera behavior:
 * - Looking left and right
 * - Looking up and down
 * - Rotating the player's body horizontally
 * - Preventing the camera from flipping upside down
 * - Locking and hiding the mouse cursor during gameplay
 * - Lowering the camera while the player crouches
 * - Raising the camera when the player stands again
 *
 * TODO:
 * - Move camera and player input to Input Actions for rebinding support.
 * - Add configurable controller sensitivity options.
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
    [Tooltip("Controls how quickly the camera rotates when the mouse moves.")]
    private float mouseSensitivity = 0.15f;

    #endregion


    #region Keyboard Look Settings

    [Header("Keyboard Look Settings")]

    [SerializeField]
    [Tooltip("Controls how quickly the camera rotates when the arrow keys are used.")]
    private float keyboardLookSpeed = 120f;

    #endregion


    #region Controller Look Settings

    [Header("Controller Look Settings")]

    [SerializeField]
    [Tooltip("Controls how quickly the camera rotates when the controller right stick is used.")]
    private float controllerLookSpeed = 150f;

    [SerializeField]
    [Tooltip("Ignores very small right-stick movement to reduce controller stick drift.")]
    [Range(0f, 1f)]
    private float controllerLookDeadzone = 0.15f;

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
    [Tooltip("Locks and hides the mouse cursor when the game begins.")]
    private bool lockCursorOnStart = true;

    #endregion


    #region Runtime Variables

    // Stores the current vertical camera rotation.
    private float verticalRotation = 0f;

    #endregion


    #region Unity Methods

    private void Awake()
    {
        // Automatically finds PlayerMovement on the Player object
        // if it was not assigned manually in the Inspector.
        if (playerMovement == null &&
            playerBody != null)
        {
            playerMovement =
                playerBody.GetComponent<PlayerMovement>();
        }
    }


    private void Start()
    {
        // Lock and hide the mouse cursor when gameplay begins.
        if (lockCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Ensure the camera begins at the configured standing height.
        Vector3 startingPosition =
            transform.localPosition;

        startingPosition.y =
            standingCameraHeight;

        transform.localPosition =
            startingPosition;
    }


    private void Update()
    {
        // Mouse, keyboard, and controller input all contribute
        // to camera rotation.
        HandleLookInput();

        // Keeps camera height synchronized with the player's
        // actual crouch state.
        HandleCrouchCamera();
    }

    #endregion


    #region Look Input

    private void HandleLookInput()
    {
        // Stores the final horizontal and vertical look input
        // for the current frame.
        float horizontalLook = 0f;
        float verticalLook = 0f;

        #region Mouse Look

        // Read mouse movement if a mouse is connected.
        if (Mouse.current != null)
        {
            Vector2 mouseDelta =
                Mouse.current.delta.ReadValue();

            horizontalLook +=
                mouseDelta.x *
                mouseSensitivity;

            verticalLook +=
                mouseDelta.y *
                mouseSensitivity;
        }

        #endregion


        #region Keyboard Look

        // Read arrow-key camera controls if a keyboard is connected.
        if (Keyboard.current != null)
        {
            float keyboardHorizontal = 0f;
            float keyboardVertical = 0f;

            if (Keyboard.current.leftArrowKey.isPressed)
            {
                keyboardHorizontal -= 1f;
            }

            if (Keyboard.current.rightArrowKey.isPressed)
            {
                keyboardHorizontal += 1f;
            }

            if (Keyboard.current.upArrowKey.isPressed)
            {
                keyboardVertical += 1f;
            }

            if (Keyboard.current.downArrowKey.isPressed)
            {
                keyboardVertical -= 1f;
            }

            // Time.deltaTime keeps digital keyboard look
            // frame-rate independent.
            horizontalLook +=
                keyboardHorizontal *
                keyboardLookSpeed *
                Time.deltaTime;

            verticalLook +=
                keyboardVertical *
                keyboardLookSpeed *
                Time.deltaTime;
        }

        #endregion


        #region Controller Look

        // Read right-stick look input if a controller is connected.
        if (Gamepad.current != null)
        {
            Vector2 controllerLook =
                Gamepad.current.rightStick.ReadValue();

            /*
             * Ignore tiny stick movement.
             *
             * This helps prevent camera drift from controllers
             * whose sticks do not rest perfectly at zero.
             */
            if (controllerLook.magnitude <
                controllerLookDeadzone)
            {
                controllerLook =
                    Vector2.zero;
            }

            // Controller stick values represent direction rather than
            // pixel movement, so Time.deltaTime is required.
            horizontalLook +=
                controllerLook.x *
                controllerLookSpeed *
                Time.deltaTime;

            verticalLook +=
                controllerLook.y *
                controllerLookSpeed *
                Time.deltaTime;
        }

        #endregion


        // Apply all supported look input through one shared method.
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

        // Prevent the camera from rotating too far up or down.
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
        // Default to standing height.
        float targetHeight =
            standingCameraHeight;

        /*
         * Use the actual crouch state from PlayerMovement.
         *
         * This keeps the camera synchronized with both:
         * - Left Ctrl keyboard crouch
         * - Controller crouch input
         *
         * It also respects the headroom system that can force
         * the player to remain crouched under an obstruction.
         */
        if (playerMovement != null &&
            playerMovement.IsCrouching)
        {
            targetHeight =
                crouchingCameraHeight;
        }

        // Get the camera's current local position.
        Vector3 currentPosition =
            transform.localPosition;

        // Smoothly transition toward the desired camera height.
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