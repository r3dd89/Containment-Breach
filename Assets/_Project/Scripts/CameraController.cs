using UnityEngine;

// This namespace gives us access to Unity's newer Input System.
// We will use it to read mouse movement.
using UnityEngine.InputSystem;

/*
 * Script Name: CameraController
 * Purpose:
 * Controls the first-person camera using mouse movement.
 *
 * This script currently handles:
 * - Looking left and right
 * - Looking up and down
 * - Rotating the player body horizontally
 * - Clamping vertical camera rotation so the player cannot flip upside down
 *
 * This script is intended for an FPS-style player.
 */

public class CameraController : MonoBehaviour
{
    #region Component References

    /*
     * REGION: Component References
     *
     * This region stores references to other objects/components
     * that the camera system needs in order to work.
     */

    [Header("Component References")]
    /*
     * HEADER: Component References
     *
     * This header groups together the Unity object references
     * that must be assigned in the Inspector.
     */

    [SerializeField]
    [Tooltip("The Player object that should rotate left and right with the camera.")]
    private Transform playerBody;

    #endregion


    #region Mouse Look Settings

    /*
     * REGION: Mouse Look Settings
     *
     * This region contains values that control how responsive
     * the camera feels when the player moves the mouse.
     */

    [Header("Mouse Look Settings")]
    /*
     * HEADER: Mouse Look Settings
     *
     * These values control the mouse sensitivity
     * and the maximum vertical look angle.
     */

    [SerializeField]
    [Tooltip("Controls how quickly the camera rotates when the mouse moves.")]
    private float mouseSensitivity = 0.15f;

    [SerializeField]
    [Tooltip("The maximum angle the player can look upward or downward.")]
    private float verticalLookLimit = 85f;

    #endregion


    #region Cursor Settings

    /*
     * REGION: Cursor Settings
     *
     * This region controls whether the mouse cursor
     * is locked and hidden while the game is running.
     */

    [Header("Cursor Settings")]
    /*
     * HEADER: Cursor Settings
     *
     * This option lets us decide whether the cursor
     * should automatically lock when gameplay starts.
     */

    [SerializeField]
    [Tooltip("Locks and hides the mouse cursor when the game begins.")]
    private bool lockCursorOnStart = true;

    #endregion


    #region Private Variables

    /*
     * REGION: Private Variables
     *
     * These variables are used internally while the game is running.
     * They do not need to be edited in the Inspector.
     */

    // Stores the current up/down camera rotation.
    private float verticalRotation = 0f;

    #endregion


    #region Unity Methods

    /*
     * REGION: Unity Methods
     *
     * Unity automatically calls methods such as Start() and Update().
     *
     * Start runs once when gameplay begins.
     * Update runs once every frame.
     */

    private void Start()
    {
        // Check whether the cursor should be locked when the game starts.
        if (lockCursorOnStart)
        {
            // Lock the cursor to the center of the Game window.
            Cursor.lockState = CursorLockMode.Locked;

            // Hide the cursor while gameplay is active.
            Cursor.visible = false;
        }
    }


    private void Update()
    {
        // Handle mouse-controlled camera rotation every frame.
        HandleMouseLook();
    }

    #endregion


    #region Mouse Look Methods

    /*
     * REGION: Mouse Look Methods
     *
     * This region contains the logic responsible for
     * reading mouse movement and rotating the camera/player.
     */

    private void HandleMouseLook()
    {
        // Make sure a mouse is currently available.
        if (Mouse.current == null)
        {
            // Stop this method if there is no mouse.
            return;
        }

        // Read how much the mouse moved during this frame.
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        // Multiply horizontal movement by sensitivity.
        float mouseX = mouseDelta.x * mouseSensitivity;

        // Multiply vertical movement by sensitivity.
        float mouseY = mouseDelta.y * mouseSensitivity;

        // Subtract mouseY because moving the mouse upward
        // should make the camera look upward.
        verticalRotation -= mouseY;

        // Prevent the player from looking too far up or down.
        verticalRotation = Mathf.Clamp(
            verticalRotation,
            -verticalLookLimit,
            verticalLookLimit
        );

        // Apply the vertical rotation directly to the camera.
        transform.localRotation = Quaternion.Euler(
            verticalRotation,
            0f,
            0f
        );

        // Rotate the entire player body left and right.
        playerBody.Rotate(
            Vector3.up * mouseX
        );
    }

    #endregion
}