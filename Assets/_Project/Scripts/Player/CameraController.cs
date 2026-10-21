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
 * - Add controller/gamepad look support.
 * - Move camera and player input to Input Actions for remapping.
 */

public class CameraController : MonoBehaviour
{
    #region Component References

    [Header("Component References")]

    [SerializeField]
    [Tooltip("The Player object that rotates left and right with the camera.")]
    private Transform playerBody;

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

    private void Start()
    {
        // Lock and hide the mouse cursor when gameplay begins.
        if (lockCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Ensure the camera begins at the configured standing height.
        Vector3 startingPosition = transform.localPosition;
        startingPosition.y = standingCameraHeight;
        transform.localPosition = startingPosition;
    }


    private void Update()
    {
        // Mouse input and keyboard input both contribute to camera rotation.
        HandleLookInput();

        // Updates the camera height while crouching or standing.
        HandleCrouchCamera();
    }

    #endregion


    #region Look Input

    private void HandleLookInput()
    {
        // These values represent the final horizontal and vertical
        // look input for the current frame.
        float horizontalLook = 0f;
        float verticalLook = 0f;

        // Read mouse movement if a mouse is connected.
        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            horizontalLook +=
                mouseDelta.x * mouseSensitivity;

            verticalLook +=
                mouseDelta.y * mouseSensitivity;
        }

        // Read keyboard-only camera controls if a keyboard is connected.
        if (Keyboard.current != null)
        {
            // Arrow keys create a digital look direction.
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

            // Time.deltaTime makes keyboard rotation frame-rate independent.
            horizontalLook +=
                keyboardHorizontal *
                keyboardLookSpeed *
                Time.deltaTime;

            verticalLook +=
                keyboardVertical *
                keyboardLookSpeed *
                Time.deltaTime;
        }

        // Apply the combined look values.
        ApplyLookRotation(
            horizontalLook,
            verticalLook);
    }


    private void ApplyLookRotation(
        float horizontalLook,
        float verticalLook)
    {
        // Positive vertical input should look upward.
        verticalRotation -= verticalLook;

        // Prevent the camera from rotating too far upward or downward.
        verticalRotation = Mathf.Clamp(
            verticalRotation,
            -verticalLookLimit,
            verticalLookLimit);

        // Apply vertical rotation to the camera only.
        transform.localRotation =
            Quaternion.Euler(
                verticalRotation,
                0f,
                0f);

        // Apply horizontal rotation to the entire player body.
        if (playerBody != null)
        {
            playerBody.Rotate(
                Vector3.up * horizontalLook);
        }
    }

    #endregion


    #region Crouch Camera Methods

    private void HandleCrouchCamera()
    {
        // Stop if no keyboard is available.
        if (Keyboard.current == null)
        {
            return;
        }

        // Default to standing height.
        float targetHeight =
            standingCameraHeight;

        // Left Ctrl lowers the camera into crouch position.
        if (Keyboard.current.leftCtrlKey.isPressed)
        {
            targetHeight =
                crouchingCameraHeight;
        }

        // Get the current camera position.
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