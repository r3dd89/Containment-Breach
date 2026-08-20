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
 * - Preventing the player from standing when something is above them
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

    [SerializeField]
    [Tooltip("The CharacterController component used to move the player.")]
    private CharacterController characterController;

    #endregion


    #region Movement Settings

    /*
     * REGION: Movement Settings
     *
     * This region contains values that control
     * walking and sprinting.
     */

    [Header("Movement Settings")]

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

    [SerializeField]
    [Tooltip("How high the player jumps in Unity units.")]
    private float jumpHeight = 1.5f;

    #endregion


    #region Crouch Settings

    /*
     * REGION: Crouch Settings
     *
     * This region contains values that control
     * crouching and crouched movement.
     */

    [Header("Crouch Settings")]

    [SerializeField]
    [Tooltip("How tall the CharacterController becomes while crouching.")]
    private float crouchHeight = 1f;

    [SerializeField]
    [Tooltip("How fast the player moves while crouching.")]
    private float crouchSpeed = 2.5f;

    [SerializeField]
    [Tooltip("How quickly the CharacterController transitions between standing and crouching.")]
    private float crouchTransitionSpeed = 8f;

    #endregion


    #region Headroom Settings

    /*
     * REGION: Headroom Settings
     *
     * This region controls how the player checks
     * whether there is enough room to stand.
     *
     * For example:
     * - Under a low ceiling
     * - Under a pipe
     * - Inside a vent
     *
     * If there is not enough space,
     * the player remains crouched.
     */

    [Header("Headroom Settings")]

    [SerializeField]
    [Tooltip("Layers containing objects that should prevent the player from standing.")]
    private LayerMask headroomLayerMask = ~0;

    [SerializeField]
    [Tooltip("A small padding amount used during the overhead collision check.")]
    private float headroomCheckPadding = 0.05f;

    #endregion


    #region Gravity Settings

    /*
     * REGION: Gravity Settings
     *
     * CharacterController does not automatically use gravity.
     * Because of that, gravity must be calculated manually.
     */

    [Header("Gravity Settings")]

    [SerializeField]
    [Tooltip("The downward acceleration applied to the player while airborne.")]
    private float gravity = -20f;

    [SerializeField]
    [Tooltip("A small downward force that keeps the player touching the ground.")]
    private float groundedForce = -2f;

    #endregion


    #region Private Variables

    /*
     * REGION: Private Variables
     *
     * These variables are used internally while the game runs.
     * They do not need to be changed in the Inspector.
     */

    // Stores the player's current vertical speed.
    //
    // Positive = moving upward.
    // Negative = moving downward.
    private float verticalVelocity;

    // Stores the CharacterController's original standing height.
    private float standingHeight;

    // Stores the CharacterController's original standing center.
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
        // Check whether the CharacterController
        // was manually assigned in the Inspector.
        if (characterController == null)
        {
            // If it was not assigned,
            // automatically find it on this GameObject.
            characterController = GetComponent<CharacterController>();
        }

        // Save the CharacterController's normal standing height.
        //
        // We need this so the player knows what size
        // to return to after crouching.
        standingHeight = characterController.height;

        // Save the CharacterController's normal center position.
        standingCenter = characterController.center;
    }


    private void Update()
    {
        // 1. Check whether the player pressed Jump.
        HandleJump();

        // 2. Handle crouching and standing.
        HandleCrouch();

        // 3. Apply gravity.
        HandleGravity();

        // 4. Calculate horizontal movement.
        Vector3 horizontalMove = CalculateMovement();

        // 5. Combine horizontal movement
        // with vertical movement.
        Vector3 finalMovement =
            horizontalMove +
            (Vector3.up * verticalVelocity);

        // 6. Move the CharacterController.
        //
        // We use ONE Move() call per frame because
        // multiple CharacterController.Move() calls
        // can interfere with isGrounded.
        characterController.Move(
            finalMovement * Time.deltaTime
        );
    }

    #endregion


    #region Movement Methods

    /*
     * REGION: Movement Methods
     *
     * This region reads WASD input and calculates
     * the player's horizontal movement.
     */

    private Vector3 CalculateMovement()
    {
        // Start with no movement input.
        Vector2 movementInput = Vector2.zero;

        // Make sure Unity can detect a keyboard.
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

        // Normalize the input.
        //
        // Without this, holding two movement keys
        // such as W + D would make diagonal movement faster.
        movementInput = movementInput.normalized;

        // Convert the 2D keyboard input
        // into a 3D movement direction.
        //
        // transform.right = player's right direction.
        // transform.forward = player's forward direction.
        Vector3 moveDirection =
            transform.right * movementInput.x +
            transform.forward * movementInput.y;

        // Start with normal walking speed.
        float currentSpeed = walkSpeed;

        // Crouching takes priority over sprinting.
        //
        // This prevents the player from sprinting
        // while crouched.
        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (Keyboard.current != null &&
                 Keyboard.current.leftShiftKey.isPressed)
        {
            // Use sprint speed while Left Shift is held.
            currentSpeed = sprintSpeed;
        }

        // Return the final horizontal movement velocity.
        return moveDirection * currentSpeed;
    }

    #endregion


    #region Jump Methods

    /*
     * REGION: Jump Methods
     *
     * This region handles the player's jump input
     * and calculates the required upward velocity.
     */

    private void HandleJump()
    {
        // Stop if Unity cannot detect a keyboard.
        if (Keyboard.current == null)
        {
            return;
        }

        // Check whether Space was pressed during this frame.
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            // Only allow the player to jump while grounded.
            if (characterController.isGrounded)
            {
                /*
                 * Calculate the upward velocity needed
                 * to reach the desired jump height.
                 *
                 * Formula:
                 *
                 * velocity = sqrt(height * -2 * gravity)
                 */

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
     * This region handles:
     *
     * - Crouching
     * - Standing
     * - Changing CharacterController height
     * - Preventing the player from standing
     *   when something is above them
     */

    private void HandleCrouch()
    {
        // Stop if Unity cannot detect a keyboard.
        if (Keyboard.current == null)
        {
            return;
        }

        // Check whether Left Ctrl is currently being held.
        bool crouchButtonHeld =
            Keyboard.current.leftCtrlKey.isPressed;

        /*
         * If Ctrl is held,
         * the player should crouch.
         */
        if (crouchButtonHeld)
        {
            isCrouching = true;
        }
        else
        {
            /*
             * If Ctrl is released,
             * the player is trying to stand.
             *
             * Before allowing that,
             * check whether enough space exists above them.
             */

            if (CanStandUp())
            {
                // Nothing is blocking the player.
                // Allow standing.
                isCrouching = false;
            }
            else
            {
                // Something is above the player.
                // Keep the player crouched.
                isCrouching = true;
            }
        }

        // Decide which height the CharacterController
        // should move toward.
        float targetHeight;

        if (isCrouching)
        {
            // Use the shorter crouching height.
            targetHeight = crouchHeight;
        }
        else
        {
            // Return to full standing height.
            targetHeight = standingHeight;
        }

        /*
         * Smoothly transition the controller height.
         *
         * Lerp prevents the CharacterController
         * from instantly snapping between heights.
         */
        characterController.height = Mathf.Lerp(
            characterController.height,
            targetHeight,
            crouchTransitionSpeed * Time.deltaTime
        );

        // Start with the normal standing center.
        Vector3 targetCenter = standingCenter;

        /*
         * Calculate how much shorter the controller
         * currently is compared with standing height.
         */
        float heightDifference =
            standingHeight - characterController.height;

        /*
         * Move the CharacterController's center downward
         * by half of the height difference.
         *
         * This keeps the bottom of the collider
         * close to the ground while crouching.
         */
        targetCenter.y =
            standingCenter.y - (heightDifference / 2f);

        // Apply the new center position.
        characterController.center = targetCenter;
    }


    private bool CanStandUp()
    {
        /*
         * METHOD: CanStandUp
         *
         * Checks the space above the player's head.
         *
         * Returns:
         *
         * true  = enough room exists to stand.
         * false = something is blocking the player.
         */

        // Calculate how much additional height
        // the controller needs in order to stand.
        float extraHeightNeeded =
            standingHeight - characterController.height;

        /*
         * If the controller is already almost
         * completely standing, there is no reason
         * to run an overhead physics check.
         */
        if (extraHeightNeeded <= 0.01f)
        {
            return true;
        }

        /*
         * Find the center of the CharacterController
         * in world space.
         */
        Vector3 controllerCenter =
            transform.TransformPoint(
                characterController.center
            );

        /*
         * Find the current top of the CharacterController.
         *
         * Half of the controller's height extends
         * above its center.
         */
        float currentTop =
            controllerCenter.y +
            (characterController.height / 2f);

        /*
         * This is the starting point for
         * the overhead collision check.
         */
        Vector3 checkStart = new Vector3(
            controllerCenter.x,
            currentTop,
            controllerCenter.z
        );

        /*
         * Use a sphere slightly smaller
         * than the CharacterController radius.
         *
         * The padding prevents the check
         * from being overly sensitive around edges.
         */
        float checkRadius =
            Mathf.Max(
                0.01f,
                characterController.radius - headroomCheckPadding
            );

        /*
         * Cast the sphere upward through the space
         * the player's head and upper body would occupy
         * while returning to standing height.
         */
        bool blocked = Physics.SphereCast(
            checkStart,
            checkRadius,
            Vector3.up,
            out RaycastHit hit,
            extraHeightNeeded + headroomCheckPadding,
            headroomLayerMask,
            QueryTriggerInteraction.Ignore
        );

        /*
         * If the SphereCast hit something,
         * blocked will be true.
         *
         * We return the opposite:
         *
         * blocked = true  -> CanStandUp = false
         * blocked = false -> CanStandUp = true
         */
        return !blocked;
    }

    #endregion


    #region Gravity Methods

    /*
     * REGION: Gravity Methods
     *
     * This region applies gravity to the player
     * while they are airborne.
     */

    private void HandleGravity()
    {
        // Check whether the CharacterController
        // is currently touching the ground.
        if (characterController.isGrounded)
        {
            /*
             * If the player is grounded
             * and still has downward velocity,
             * replace it with a small downward force.
             */
            if (verticalVelocity < 0f)
            {
                // This helps keep the player
                // firmly touching the ground.
                verticalVelocity = groundedForce;
            }
        }
        else
        {
            /*
             * If the player is airborne,
             * continuously add gravity.
             */
            verticalVelocity +=
                gravity * Time.deltaTime;
        }
    }

    #endregion
}