using UnityEngine;

// This namespace gives us access to Unity's newer Input System.
// We use it to read keyboard input such as W, A, S, D, Shift, Space, and Ctrl.
using UnityEngine.InputSystem;

/*
 * Script Name: PlayerMovement
 * Purpose:
 * Controls the basic first-person player movement.
 *
 * This script currently handles:
 * - Walking with W, A, S, and D
 * - Sprinting while holding Left Shift
 * - Jumping with Space
 * - Crouching while holding Left Ctrl
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
     * This region stores references to Unity components
     * that this script needs in order to work.
     */

    [Header("Component References")]
    /*
     * HEADER: Component References
     *
     * This header groups together required Unity component references
     * inside the Inspector.
     */

    [SerializeField]
    [Tooltip("The CharacterController component used to move the player.")]
    private CharacterController characterController;

    #endregion


    #region Movement Settings

    /*
     * REGION: Movement Settings
     *
     * This region contains values that control walking and sprinting.
     */

    [Header("Movement Settings")]
    /*
     * HEADER: Movement Settings
     *
     * These values control the player's normal walking
     * and sprinting speeds.
     */

    [SerializeField]
    [Tooltip("How fast the player moves while walking.")]
    private float walkSpeed = 5f;

    [SerializeField]
    [Tooltip("How fast the player moves while holding Left Shift.")]
    private float sprintSpeed = 8f;

    #endregion


    #region Jump Settings

    /*
     * REGION: Jump Settings
     *
     * This region contains the value that controls
     * how high the player can jump.
     */

    [Header("Jump Settings")]
    /*
     * HEADER: Jump Settings
     *
     * This value controls the player's jump height.
     */

    [SerializeField]
    [Tooltip("How high the player jumps in Unity units.")]
    private float jumpHeight = 1.5f;

    #endregion


    #region Crouch Settings

    /*
     * REGION: Crouch Settings
     *
     * This region contains the values that control
     * how the player crouches and moves while crouched.
     */

    [Header("Crouch Settings")]
    /*
     * HEADER: Crouch Settings
     *
     * These values control the player's crouched height,
     * crouched movement speed, and how smoothly the player
     * transitions between standing and crouching.
     */

    [SerializeField]
    [Tooltip("How tall the CharacterController becomes while crouching.")]
    private float crouchHeight = 1f;

    [SerializeField]
    [Tooltip("How fast the player moves while crouching.")]
    private float crouchSpeed = 2.5f;

    [SerializeField]
    [Tooltip("How quickly the player transitions between standing and crouching.")]
    private float crouchTransitionSpeed = 8f;

    #endregion


    #region Gravity Settings

    /*
     * REGION: Gravity Settings
     *
     * CharacterController does not automatically use gravity.
     * Because of that, this script applies gravity manually.
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
    [Tooltip("A small downward force used while grounded to keep the player touching the floor.")]
    private float groundedForce = -2f;

    #endregion


    #region Private Variables

    /*
     * REGION: Private Variables
     *
     * These variables are used internally while the game is running.
     * They do not need to be adjusted in the Inspector.
     */

    // Stores the player's current vertical speed.
    // Positive values move the player upward.
    // Negative values move the player downward.
    private float verticalVelocity;

    // Stores the CharacterController's normal standing height.
    private float standingHeight;

    // Stores the CharacterController's normal standing center position.
    private Vector3 standingCenter;

    // Tracks whether the player is currently crouching.
    private bool isCrouching;

    #endregion


    #region Unity Methods

    /*
     * REGION: Unity Methods
     *
     * Unity automatically calls these methods.
     *
     * Awake runs when the object is first loaded.
     * Update runs once every frame.
     */

    private void Awake()
    {
        // Check whether the CharacterController reference
        // was already assigned manually in the Inspector.
        if (characterController == null)
        {
            // If not, automatically find the CharacterController
            // attached to the same GameObject.
            characterController = GetComponent<CharacterController>();
        }

        // Save the player's original standing height.
        // We need this later so the player can return
        // to the correct size after crouching.
        standingHeight = characterController.height;

        // Save the player's original CharacterController center.
        // This lets us correctly reposition the controller
        // while changing its height.
        standingCenter = characterController.center;
    }


    private void Update()
    {
        // 1. Check whether the player pressed the jump button.
        HandleJump();

        // 2. Check whether the player is crouching.
        HandleCrouch();

        // 3. Apply gravity and calculate vertical velocity.
        HandleGravity();

        // 4. Calculate horizontal movement from WASD input.
        Vector3 horizontalMove = CalculateMovement();

        // 5. Combine horizontal movement with vertical movement.
        //
        // Keeping all movement inside one CharacterController.Move()
        // call helps keep ground detection more reliable.
        Vector3 finalMovement =
            horizontalMove +
            (Vector3.up * verticalVelocity);

        // 6. Move the player.
        //
        // Time.deltaTime keeps movement frame-rate independent.
        characterController.Move(
            finalMovement * Time.deltaTime
        );
    }

    #endregion


    #region Movement Methods

    /*
     * REGION: Movement Methods
     *
     * This region reads keyboard input and calculates
     * the player's horizontal movement velocity.
     */

    private Vector3 CalculateMovement()
    {
        // Start with no movement input.
        Vector2 movementInput = Vector2.zero;

        // Make sure a keyboard is currently available.
        if (Keyboard.current != null)
        {
            // W moves forward.
            if (Keyboard.current.wKey.isPressed)
            {
                movementInput.y += 1f;
            }

            // S moves backward.
            if (Keyboard.current.sKey.isPressed)
            {
                movementInput.y -= 1f;
            }

            // D moves right.
            if (Keyboard.current.dKey.isPressed)
            {
                movementInput.x += 1f;
            }

            // A moves left.
            if (Keyboard.current.aKey.isPressed)
            {
                movementInput.x -= 1f;
            }
        }

        // Normalize the input so moving diagonally
        // is not faster than moving straight.
        movementInput = movementInput.normalized;

        // Convert the 2D keyboard input into a 3D movement direction.
        //
        // transform.right represents the player's right direction.
        // transform.forward represents the player's forward direction.
        Vector3 moveDirection =
            transform.right * movementInput.x +
            transform.forward * movementInput.y;

        // Start with the normal walking speed.
        float currentSpeed = walkSpeed;

        // If the player is crouching,
        // use the slower crouch movement speed.
        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        // Otherwise, check whether the player is holding Left Shift.
        else if (Keyboard.current != null &&
                 Keyboard.current.leftShiftKey.isPressed)
        {
            // If Shift is held, use sprint speed.
            currentSpeed = sprintSpeed;
        }

        // Return the final horizontal velocity.
        return moveDirection * currentSpeed;
    }

    #endregion


    #region Jump Methods

    /*
     * REGION: Jump Methods
     *
     * This region handles jump input and calculates
     * the upward velocity needed to reach the desired jump height.
     */

    private void HandleJump()
    {
        // Stop this method if Unity cannot detect a keyboard.
        if (Keyboard.current == null)
        {
            return;
        }

        // Check whether the Space key was pressed during this frame.
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            // Only allow jumping while grounded.
            if (characterController.isGrounded)
            {
                // Calculate the upward velocity needed
                // to reach the jump height set in the Inspector.
                //
                // Formula:
                // velocity = square root of (jump height * -2 * gravity)
                verticalVelocity = Mathf.Sqrt(
                    jumpHeight * -2f * gravity
                );
            }
        }
    }

    #endregion


    #region Crouch Methods

    /*
     * REGION: Crouch Methods
     *
     * This region handles crouch input and changes
     * the CharacterController's height and center position.
     */

    private void HandleCrouch()
    {
        // Stop this method if Unity cannot detect a keyboard.
        if (Keyboard.current == null)
        {
            return;
        }

        // The player crouches while holding Left Ctrl.
        isCrouching = Keyboard.current.leftCtrlKey.isPressed;

        // Decide which height the CharacterController
        // should move toward.
        float targetHeight;

        // Check whether the player is currently crouching.
        if (isCrouching)
        {
            // Use the shorter crouching height.
            targetHeight = crouchHeight;
        }
        else
        {
            // Return to the normal standing height.
            targetHeight = standingHeight;
        }

        // Smoothly transition the CharacterController's height
        // toward the target height.
        characterController.height = Mathf.Lerp(
            characterController.height,
            targetHeight,
            crouchTransitionSpeed * Time.deltaTime
        );

        // Start with the normal standing center position.
        Vector3 targetCenter = standingCenter;

        // Calculate how much shorter the controller currently is
        // compared with its normal standing height.
        float heightDifference =
            standingHeight - characterController.height;

        // Move the center downward by half of the height difference.
        //
        // This helps keep the bottom of the CharacterController
        // near the ground instead of shrinking upward from the middle.
        targetCenter.y =
            standingCenter.y - (heightDifference / 2f);

        // Apply the adjusted center position.
        characterController.center = targetCenter;
    }

    #endregion


    #region Gravity Methods

    /*
     * REGION: Gravity Methods
     *
     * This region handles vertical movement caused by gravity.
     */

    private void HandleGravity()
    {
        // Check whether the player is standing on the ground.
        if (characterController.isGrounded)
        {
            // If the player is grounded and still moving downward,
            // replace the falling velocity with a small downward force.
            if (verticalVelocity < 0f)
            {
                // This helps keep the CharacterController
                // firmly touching the ground.
                verticalVelocity = groundedForce;
            }
        }
        else
        {
            // Gravity should affect the player while airborne.
            //
            // This allows the player to rise after jumping
            // and eventually fall back toward the ground.
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    #endregion
}