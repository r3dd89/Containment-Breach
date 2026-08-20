using UnityEngine;

// This namespace gives us access to Unity's newer Input System.
// We use it to read mouse movement and keyboard input.
using UnityEngine.InputSystem;

/*
 * Script Name: CameraController
 * Purpose:
 * Controls the first-person player's camera.
 *
 * This script currently handles:
 * - Looking left and right with the mouse
 * - Looking up and down with the mouse
 * - Rotating the player's body horizontally
 * - Preventing the camera from flipping upside down
 * - Locking and hiding the mouse cursor during gameplay
 * - Lowering the camera while the player crouches
 * - Raising the camera when the player stands again
 */

public class CameraController : MonoBehaviour
{
    #region Component References

    /*
     * REGION: Component References
     *
     * This region stores references to other Unity objects
     * that the camera needs in order to work.
     */

    [Header("Component References")]

    /*
     * HEADER: Component References
     *
     * This header groups together references to other objects
     * that must be connected to this script in the Inspector.
     */

    [SerializeField]
    [Tooltip("The Player object that rotates left and right with the camera.")]
    private Transform playerBody;

    #endregion


    #region Mouse Look Settings

    /*
     * REGION: Mouse Look Settings
     *
     * This region contains the settings that control
     * how the camera responds to mouse movement.
     */

    [Header("Mouse Look Settings")]

    /*
     * HEADER: Mouse Look Settings
     *
     * These values control mouse sensitivity
     * and how far the player can look upward or downward.
     */

    [SerializeField]
    [Tooltip("Controls how quickly the camera rotates when the mouse moves.")]
    private float mouseSensitivity = 0.15f;

    [SerializeField]
    [Tooltip("The maximum angle the player can look upward or downward.")]
    private float verticalLookLimit = 85f;

    #endregion


    #region Crouch Camera Settings

    /*
     * REGION: Crouch Camera Settings
     *
     * This region controls the camera's vertical position
     * while the player is standing or crouching.
     *
     * Lowering the camera helps make crouching feel
     * believable from the player's first-person perspective.
     */

    [Header("Crouch Camera Settings")]

    /*
     * HEADER: Crouch Camera Settings
     *
     * These values control the standing eye height,
     * crouching eye height, and how smoothly the camera
     * moves between those two positions.
     */

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

    /*
     * REGION: Cursor Settings
     *
     * This region controls what happens to the mouse cursor
     * while the player is controlling the camera.
     */

    [Header("Cursor Settings")]

    /*
     * HEADER: Cursor Settings
     *
     * This option determines whether the cursor
     * should automatically lock and disappear
     * when gameplay begins.
     */

    [SerializeField]
    [Tooltip("Locks and hides the mouse cursor when the game begins.")]
    private bool lockCursorOnStart = true;

    #endregion


    #region Private Variables

    /*
     * REGION: Private Variables
     *
     * These variables are used internally by the script
     * while the game is running.
     *
     * They do not need to be changed in the Inspector.
     */

    // Stores the current up/down camera rotation.
    private float verticalRotation = 0f;

    #endregion


    #region Unity Methods

    /*
     * REGION: Unity Methods
     *
     * Unity automatically calls these methods.
     *
     * Start runs once when gameplay begins.
     * Update runs once every frame.
     */

    private void Start()
    {
        // Check whether the cursor should automatically
        // lock when the game begins.
        if (lockCursorOnStart)
        {
            // Lock the cursor to the center of the Game window.
            Cursor.lockState = CursorLockMode.Locked;

            // Hide the cursor while the player is controlling the camera.
            Cursor.visible = false;
        }

        // Get the camera's current local position.
        Vector3 startingPosition = transform.localPosition;

        // Make sure the camera begins at the standing height
        // that we specified in the Inspector.
        startingPosition.y = standingCameraHeight;

        // Apply the starting camera position.
        transform.localPosition = startingPosition;
    }


    private void Update()
    {
        // Handle mouse-controlled camera rotation.
        HandleMouseLook();

        // Handle the camera moving up and down
        // when the player crouches or stands.
        HandleCrouchCamera();
    }

    #endregion


    #region Mouse Look Methods

    /*
     * REGION: Mouse Look Methods
     *
     * This region reads mouse movement and uses it
     * to rotate both the camera and the player body.
     */

    private void HandleMouseLook()
    {
        // Make sure a mouse is connected and available.
        if (Mouse.current == null)
        {
            // Stop this method if no mouse exists.
            return;
        }

        // Read how much the mouse moved during this frame.
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        // Calculate horizontal mouse movement.
        float mouseX =
            mouseDelta.x * mouseSensitivity;

        // Calculate vertical mouse movement.
        float mouseY =
            mouseDelta.y * mouseSensitivity;

        // Subtract the vertical mouse movement.
        //
        // This makes moving the mouse upward
        // cause the camera to look upward.
        verticalRotation -= mouseY;

        // Prevent the player from looking too far
        // upward or downward.
        verticalRotation = Mathf.Clamp(
            verticalRotation,
            -verticalLookLimit,
            verticalLookLimit
        );

        // Rotate the camera vertically.
        //
        // Only the camera rotates up and down.
        transform.localRotation = Quaternion.Euler(
            verticalRotation,
            0f,
            0f
        );

        // Check that the Player reference exists
        // before trying to rotate it.
        if (playerBody != null)
        {
            // Rotate the entire Player object left and right.
            //
            // This also changes the direction used
            // by PlayerMovement for forward movement.
            playerBody.Rotate(
                Vector3.up * mouseX
            );
        }
    }

    #endregion


    #region Crouch Camera Methods

    /*
     * REGION: Crouch Camera Methods
     *
     * This region moves the first-person camera downward
     * while crouching and upward while standing.
     */

    private void HandleCrouchCamera()
    {
        // Stop this method if Unity cannot detect a keyboard.
        if (Keyboard.current == null)
        {
            return;
        }

        // Start by assuming the player is standing.
        float targetHeight = standingCameraHeight;

        // Check whether the player is currently
        // holding the Left Ctrl key.
        if (Keyboard.current.leftCtrlKey.isPressed)
        {
            // If Ctrl is held, use the lower crouching height.
            targetHeight = crouchingCameraHeight;
        }

        // Get the camera's current local position.
        Vector3 currentPosition = transform.localPosition;

        // Smoothly move the camera's Y position
        // toward the desired standing or crouching height.
        currentPosition.y = Mathf.Lerp(
            currentPosition.y,
            targetHeight,
            crouchCameraSpeed * Time.deltaTime
        );

        // Apply the updated camera position.
        transform.localPosition = currentPosition;
    }

    #endregion
}