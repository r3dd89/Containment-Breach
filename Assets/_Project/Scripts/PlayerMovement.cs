using UnityEngine;

// This namespace gives us access to Unity's newer Input System.
// We will use it to read keyboard input such as W, A, S, D, and Shift.
using UnityEngine.InputSystem;

/*
 * Script Name: PlayerMovement
 * Purpose:
 * Controls the basic first-person player movement.
 *
 * This script currently handles:
 * - Walking with W, A, S, and D
 * - Sprinting while holding Left Shift
 * - Gravity so the player stays on the ground
 *
 * This script uses Unity's CharacterController component
 * instead of a Rigidbody for player movement.
 */

public class PlayerMovement : MonoBehaviour
{
    #region Component References

    /*
     * REGION: Component References
     *
     * This region stores references to other Unity components
     * that this script needs in order to work.
     */

    [Header("Component References")]
    /*
     * HEADER: Component References
     *
     * This header keeps required Unity component references
     * grouped together in the Inspector.
     */

    [SerializeField]
    [Tooltip("The CharacterController component used to move the player.")]
    private CharacterController characterController;

    #endregion


    #region Movement Settings

    /*
     * REGION: Movement Settings
     *
     * This region contains the values that control how quickly
     * the player walks and sprints.
     */

    [Header("Movement Settings")]
    /*
     * HEADER: Movement Settings
     *
     * These values control the player's movement speed.
     * Because they are serialized, we can adjust them directly
     * in the Unity Inspector without editing the script.
     */

    [SerializeField]
    [Tooltip("How fast the player moves while walking.")]
    private float walkSpeed = 5f;

    [SerializeField]
    [Tooltip("How fast the player moves while holding the sprint key.")]
    private float sprintSpeed = 8f;

    #endregion


    #region Gravity Settings

    /*
     * REGION: Gravity Settings
     *
     * CharacterController does not automatically use gravity.
     * Because of that, we need to apply our own downward force.
     */

    [Header("Gravity Settings")]
    /*
     * HEADER: Gravity Settings
     *
     * These values control how strongly gravity pulls the player downward.
     */

    [SerializeField]
    [Tooltip("The downward acceleration applied to the player.")]
    private float gravity = -20f;

    [SerializeField]
    [Tooltip("Small downward force used while grounded to keep the player touching the floor.")]
    private float groundedForce = -2f;

    #endregion


    #region Private Variables

    /*
     * REGION: Private Variables
     *
     * These variables are used internally by the script while the game runs.
     * They do not need to be changed in the Inspector.
     */

    // Stores the player's current vertical speed caused by gravity.
    private float verticalVelocity;

    #endregion


    #region Unity Methods

    /*
     * REGION: Unity Methods
     *
     * Unity automatically calls methods such as Awake() and Update().
     *
     * Awake runs when the object is first loaded.
     * Update runs once every frame while the object is active.
     */

    private void Awake()
    {
        // Check whether the CharacterController reference was assigned manually.
        if (characterController == null)
        {
            // If it was not assigned, try to find the CharacterController
            // attached to the same GameObject as this script.
            characterController = GetComponent<CharacterController>();
        }
    }


    private void Update()
    {
        // Handle the player's horizontal movement every frame.
        HandleMovement();

        // Apply gravity every frame so the player stays on the ground.
        HandleGravity();
    }

    #endregion


    #region Movement Methods

    /*
     * REGION: Movement Methods
     *
     * This region contains the methods responsible for reading
     * keyboard input and moving the player.
     */

    private void HandleMovement()
    {
        // Start with no movement input.
        Vector2 movementInput = Vector2.zero;

        // Make sure a keyboard is currently available.
        if (Keyboard.current != null)
        {
            // Check whether the W key is being held.
            if (Keyboard.current.wKey.isPressed)
            {
                // W moves the player forward.
                movementInput.y += 1f;
            }

            // Check whether the S key is being held.
            if (Keyboard.current.sKey.isPressed)
            {
                // S moves the player backward.
                movementInput.y -= 1f;
            }

            // Check whether the D key is being held.
            if (Keyboard.current.dKey.isPressed)
            {
                // D moves the player to the right.
                movementInput.x += 1f;
            }

            // Check whether the A key is being held.
            if (Keyboard.current.aKey.isPressed)
            {
                // A moves the player to the left.
                movementInput.x -= 1f;
            }
        }

        // Prevent diagonal movement from being faster than straight movement.
        movementInput = movementInput.normalized;

        // Convert the horizontal input into a 3D movement direction.
        Vector3 moveDirection =
            transform.right * movementInput.x +
            transform.forward * movementInput.y;

        // Start by assuming the player is walking.
        float currentSpeed = walkSpeed;

        // Make sure the keyboard exists before checking Left Shift.
        if (Keyboard.current != null)
        {
            // Check whether the player is holding Left Shift.
            if (Keyboard.current.leftShiftKey.isPressed)
            {
                // If Shift is held, use the faster sprint speed.
                currentSpeed = sprintSpeed;
            }
        }

        // Move the CharacterController in the calculated direction.
        //
        // Time.deltaTime makes movement frame-rate independent.
        // This means the player should move at the same speed
        // whether the game is running at 30 FPS or 144 FPS.
        characterController.Move(
            moveDirection * currentSpeed * Time.deltaTime
        );
    }

    #endregion


    #region Gravity Methods

    /*
     * REGION: Gravity Methods
     *
     * This region handles the player's vertical movement caused by gravity.
     */

    private void HandleGravity()
    {
        // Check whether the CharacterController currently considers
        // the player to be touching the ground.
        if (characterController.isGrounded)
        {
            // If the player is grounded and still moving downward,
            // replace the falling velocity with a small downward force.
            if (verticalVelocity < 0f)
            {
                // This keeps the CharacterController firmly touching the ground.
                verticalVelocity = groundedForce;
            }
        }
        else
        {
            // If the player is not grounded,
            // increase the downward velocity using gravity.
            verticalVelocity += gravity * Time.deltaTime;
        }

        // Create a Vector3 that contains only vertical movement.
        Vector3 verticalMovement = Vector3.up * verticalVelocity;

        // Apply the vertical movement to the CharacterController.
        characterController.Move(
            verticalMovement * Time.deltaTime
        );
    }

    #endregion
}