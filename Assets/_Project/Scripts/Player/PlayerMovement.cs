using UnityEngine;
using UnityEngine.InputSystem;

/*
 * Script Name: PlayerMovement
 * Purpose:
 * Controls first-person player movement using the PlayerInputActions asset.
 *
 * Supported input is defined in PlayerInputActions.inputactions.
 *
 * Current movement behavior:
 * - Walking
 * - Sprinting
 * - Jumping
 * - Crouching
 * - Preventing standing beneath low ceilings
 * - Gravity
 *
 * Input Actions currently used:
 * - Move
 * - Jump
 * - Sprint
 * - Crouch
 *
 * TODO:
 * - Add runtime input rebinding support.
 * - Add configurable controller control schemes.
 */

public class PlayerMovement : MonoBehaviour
{
    #region Component References

    [Header("Component References")]

    [SerializeField]
    [Tooltip("The CharacterController component used to move the player.")]
    private CharacterController characterController;

    #endregion


    #region Movement Settings

    [Header("Movement Settings")]

    [SerializeField]
    [Tooltip("How fast the player moves while walking.")]
    private float walkSpeed = 5f;

    [SerializeField]
    [Tooltip("How fast the player moves while sprinting.")]
    private float sprintSpeed = 8f;

    #endregion


    #region Jump Settings

    [Header("Jump Settings")]

    [SerializeField]
    [Tooltip("How high the player jumps in Unity units.")]
    private float jumpHeight = 1.5f;

    #endregion


    #region Crouch Settings

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

    [Header("Headroom Settings")]

    [SerializeField]
    [Tooltip("Layers containing objects that should prevent the player from standing.")]
    private LayerMask headroomLayerMask = ~0;

    [SerializeField]
    [Tooltip("A small padding amount used during the overhead collision check.")]
    private float headroomCheckPadding = 0.05f;

    #endregion


    #region Gravity Settings

    [Header("Gravity Settings")]

    [SerializeField]
    [Tooltip("The downward acceleration applied to the player while airborne.")]
    private float gravity = -20f;

    [SerializeField]
    [Tooltip("A small downward force that keeps the player touching the ground.")]
    private float groundedForce = -2f;

    #endregion


    #region Input

    /*
     * PlayerInputActions is generated automatically from:
     *
     * Assets/_Project/Input/PlayerInputActions.inputactions
     *
     * The generated class provides strongly typed access to the
     * Player action map and its individual actions.
     */

    private PlayerInputActions playerInputActions;

    #endregion


    #region Runtime Variables

    // Stores the player's current vertical speed.
    private float verticalVelocity;

    // Stores the CharacterController's original standing height.
    private float standingHeight;

    // Stores the CharacterController's original standing center.
    private Vector3 standingCenter;

    // Tracks whether the player is currently crouching.
    private bool isCrouching;

    #endregion


    #region Public Properties

    /*
     * Exposes the actual crouch state to systems such as CameraController.
     *
     * This prevents the camera from independently interpreting crouch input.
     */
    public bool IsCrouching
    {
        get { return isCrouching; }
    }

    #endregion


    #region Unity Methods

    private void Awake()
    {
        // Automatically finds the CharacterController if the Inspector
        // reference was not assigned.
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        // Store the CharacterController's normal standing dimensions.
        standingHeight = characterController.height;
        standingCenter = characterController.center;

        // Create an instance of the generated Input Actions class.
        playerInputActions = new PlayerInputActions();
    }


    private void OnEnable()
    {
        /*
         * Input Actions must be enabled before they can receive input.
         *
         * Enabling the Player action map here also means input automatically
         * stops if this component becomes disabled.
         */
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
        // Releases resources owned by the generated Input Actions instance.
        if (playerInputActions != null)
        {
            playerInputActions.Dispose();
        }
    }


    private void Update()
    {
        // Process jump input.
        HandleJump();

        // Process crouching and standing.
        HandleCrouch();

        // Apply gravity.
        HandleGravity();

        // Calculate horizontal movement from the Move action.
        Vector3 horizontalMove =
            CalculateMovement();

        // Combine horizontal and vertical movement.
        Vector3 finalMovement =
            horizontalMove +
            (Vector3.up * verticalVelocity);

        /*
         * CharacterController.Move is called only once per frame.
         *
         * Multiple Move calls can interfere with CharacterController.isGrounded.
         */
        characterController.Move(
            finalMovement * Time.deltaTime);
    }

    #endregion


    #region Movement Methods

    private Vector3 CalculateMovement()
    {
        /*
         * Read the Move action as a Vector2.
         *
         * The Input Actions asset decides which physical controls feed
         * this value:
         *
         * Keyboard:
         * - W
         * - A
         * - S
         * - D
         *
         * Gamepad:
         * - Left Stick
         */
        Vector2 movementInput =
            playerInputActions.Player.Move.ReadValue<Vector2>();

        /*
         * Clamp the input instead of normalizing it.
         *
         * This prevents diagonal keyboard movement from being faster while
         * preserving partial analog-stick movement on a controller.
         */
        movementInput =
            Vector2.ClampMagnitude(
                movementInput,
                1f);

        // Convert the two-dimensional input into world movement
        // relative to the Player object's orientation.
        Vector3 moveDirection =
            transform.right * movementInput.x +
            transform.forward * movementInput.y;

        // Walking is the default speed.
        float currentSpeed =
            walkSpeed;

        // Crouching takes priority over sprinting.
        if (isCrouching)
        {
            currentSpeed =
                crouchSpeed;
        }
        else if (IsSprintHeld())
        {
            currentSpeed =
                sprintSpeed;
        }

        // Return the final horizontal movement velocity.
        return moveDirection * currentSpeed;
    }


    private bool IsSprintHeld()
    {
        /*
         * The Sprint action currently receives input from:
         *
         * Keyboard:
         * - Left Shift
         *
         * Gamepad:
         * - Left Stick Press
         *
         * IsPressed() works for either binding without the movement
         * script needing to know which device generated the input.
         */
        return playerInputActions.Player.Sprint.IsPressed();
    }

    #endregion


    #region Jump Methods

    private void HandleJump()
    {
        /*
         * WasPressedThisFrame() becomes true only on the frame
         * the Jump action is initially pressed.
         *
         * Current bindings:
         * - Space
         * - Gamepad Button South
         */
        if (!playerInputActions.Player.Jump.WasPressedThisFrame())
        {
            return;
        }

        // Jumping is only allowed while grounded.
        if (!characterController.isGrounded)
        {
            return;
        }

        /*
         * Calculate the upward velocity required to reach
         * the configured jump height.
         *
         * Formula:
         *
         * velocity = sqrt(height * -2 * gravity)
         */
        verticalVelocity =
            Mathf.Sqrt(
                jumpHeight * -2f * gravity);
    }

    #endregion


    #region Crouch Methods

    private void HandleCrouch()
    {
        /*
         * Read the Crouch action instead of checking physical devices.
         *
         * Current bindings:
         * - Left Ctrl
         * - Gamepad Button East
         */
        bool crouchButtonHeld =
            playerInputActions.Player.Crouch.IsPressed();

        // Holding the crouch action requests the crouched state.
        if (crouchButtonHeld)
        {
            isCrouching = true;
        }
        else
        {
            /*
             * When crouch is released, standing is only allowed
             * if enough space exists above the player.
             */
            if (CanStandUp())
            {
                isCrouching = false;
            }
            else
            {
                isCrouching = true;
            }
        }

        // Select the appropriate CharacterController height.
        float targetHeight =
            isCrouching
                ? crouchHeight
                : standingHeight;

        // Smoothly transition between standing and crouching height.
        characterController.height =
            Mathf.Lerp(
                characterController.height,
                targetHeight,
                crouchTransitionSpeed * Time.deltaTime);

        // Begin from the normal standing center.
        Vector3 targetCenter =
            standingCenter;

        // Calculate how much shorter the controller currently is.
        float heightDifference =
            standingHeight -
            characterController.height;

        /*
         * Move the controller center downward by half the height change.
         *
         * This keeps the bottom of the CharacterController close
         * to the ground during the transition.
         */
        targetCenter.y =
            standingCenter.y -
            (heightDifference / 2f);

        // Apply the adjusted center.
        characterController.center =
            targetCenter;
    }


    private bool CanStandUp()
    {
        // Calculate how much additional vertical space is required.
        float extraHeightNeeded =
            standingHeight -
            characterController.height;

        // Skip the physics check if already effectively standing.
        if (extraHeightNeeded <= 0.01f)
        {
            return true;
        }

        // Find the CharacterController's current center in world space.
        Vector3 controllerCenter =
            transform.TransformPoint(
                characterController.center);

        // Find the top of the current CharacterController.
        float currentTop =
            controllerCenter.y +
            (characterController.height / 2f);

        // Begin the overhead check at the current top.
        Vector3 checkStart =
            new Vector3(
                controllerCenter.x,
                currentTop,
                controllerCenter.z);

        // Use a slightly smaller radius to prevent edge sensitivity.
        float checkRadius =
            Mathf.Max(
                0.01f,
                characterController.radius -
                headroomCheckPadding);

        /*
         * Check the space the CharacterController needs in order
         * to expand back to standing height.
         */
        bool blocked =
            Physics.SphereCast(
                checkStart,
                checkRadius,
                Vector3.up,
                out RaycastHit hit,
                extraHeightNeeded +
                headroomCheckPadding,
                headroomLayerMask,
                QueryTriggerInteraction.Ignore);

        // Standing is allowed only when the required space is clear.
        return !blocked;
    }

    #endregion


    #region Gravity Methods

    private void HandleGravity()
    {
        // Maintain a small downward force while grounded.
        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0f)
            {
                verticalVelocity =
                    groundedForce;
            }
        }
        else
        {
            // Continuously apply gravity while airborne.
            verticalVelocity +=
                gravity * Time.deltaTime;
        }
    }

    #endregion
}