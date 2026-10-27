using UnityEngine;
using UnityEngine.InputSystem;

/*
 * Script Name: PlayerMovement
 * Purpose:
 * Controls first-person player movement.
 *
 * Supported input:
 * - Keyboard movement with W, A, S, and D
 * - Controller movement with the left stick
 * - Keyboard sprint with Left Shift
 * - Controller sprint with the left stick button
 * - Keyboard jump with Space
 * - Controller jump with the south face button
 * - Keyboard crouch with Left Ctrl
 * - Controller crouch with the east face button
 *
 * Current movement behavior:
 * - Walking
 * - Sprinting
 * - Jumping
 * - Crouching
 * - Preventing standing beneath low ceilings
 * - Gravity
 *
 * TODO:
 * - Move input bindings to Input Actions for rebinding support.
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
     * Exposes the actual crouch state to other systems.
     *
     * CameraController can use this value instead of independently
     * checking keyboard or controller input.
     */
    public bool IsCrouching
    {
        get { return isCrouching; }
    }

    #endregion


    #region Unity Methods

    private void Awake()
    {
        // Automatically finds the CharacterController if no reference
        // was assigned manually in the Inspector.
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        // Store the normal standing dimensions.
        standingHeight = characterController.height;
        standingCenter = characterController.center;
    }


    private void Update()
    {
        // Process jump input.
        HandleJump();

        // Process crouching and standing.
        HandleCrouch();

        // Apply gravity.
        HandleGravity();

        // Calculate horizontal movement from all supported input devices.
        Vector3 horizontalMove = CalculateMovement();

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
        // Stores the final two-dimensional movement input.
        Vector2 movementInput = Vector2.zero;

        #region Keyboard Movement

        if (Keyboard.current != null)
        {
            // Forward.
            if (Keyboard.current.wKey.isPressed)
            {
                movementInput.y += 1f;
            }

            // Backward.
            if (Keyboard.current.sKey.isPressed)
            {
                movementInput.y -= 1f;
            }

            // Right.
            if (Keyboard.current.dKey.isPressed)
            {
                movementInput.x += 1f;
            }

            // Left.
            if (Keyboard.current.aKey.isPressed)
            {
                movementInput.x -= 1f;
            }
        }

        #endregion


        #region Gamepad Movement

        if (Gamepad.current != null)
        {
            // Reads analog movement from the controller's left stick.
            Vector2 gamepadMovement =
                Gamepad.current.leftStick.ReadValue();

            // Adds controller input to the movement input.
            movementInput += gamepadMovement;
        }

        #endregion


        /*
         * Clamp instead of normalizing.
         *
         * This preserves analog stick strength when the stick is only
         * partially tilted while still preventing diagonal movement
         * from exceeding full movement speed.
         */
        movementInput =
            Vector2.ClampMagnitude(
                movementInput,
                1f);

        // Convert 2D input into movement relative to the player's orientation.
        Vector3 moveDirection =
            transform.right * movementInput.x +
            transform.forward * movementInput.y;

        // Walking is the default movement speed.
        float currentSpeed = walkSpeed;

        // Crouching takes priority over sprinting.
        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (IsSprintHeld())
        {
            currentSpeed = sprintSpeed;
        }

        // Return the final horizontal movement velocity.
        return moveDirection * currentSpeed;
    }


    private bool IsSprintHeld()
    {
        bool keyboardSprint = false;
        bool gamepadSprint = false;

        // Left Shift provides keyboard sprint.
        if (Keyboard.current != null)
        {
            keyboardSprint =
                Keyboard.current.leftShiftKey.isPressed;
        }

        // Clicking and holding the left stick provides controller sprint.
        if (Gamepad.current != null)
        {
            gamepadSprint =
                Gamepad.current.leftStickButton.isPressed;
        }

        return keyboardSprint || gamepadSprint;
    }

    #endregion


    #region Jump Methods

    private void HandleJump()
    {
        bool jumpPressed = false;

        // Space provides keyboard jump input.
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            jumpPressed = true;
        }

        /*
         * buttonSouth represents the lower face button:
         *
         * Xbox = A
         * PlayStation = Cross
         */
        if (Gamepad.current != null &&
            Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            jumpPressed = true;
        }

        // Stop if neither input device requested a jump.
        if (!jumpPressed)
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
        bool crouchButtonHeld = false;

        // Left Ctrl provides keyboard crouch input.
        if (Keyboard.current != null &&
            Keyboard.current.leftCtrlKey.isPressed)
        {
            crouchButtonHeld = true;
        }

        /*
         * buttonEast represents the right face button:
         *
         * Xbox = B
         * PlayStation = Circle
         */
        if (Gamepad.current != null &&
            Gamepad.current.buttonEast.isPressed)
        {
            crouchButtonHeld = true;
        }

        // Holding either crouch input requests the crouched state.
        if (crouchButtonHeld)
        {
            isCrouching = true;
        }
        else
        {
            /*
             * Standing is only allowed if sufficient space
             * exists above the CharacterController.
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

        // Start from the original standing center.
        Vector3 targetCenter = standingCenter;

        // Calculate how much shorter the controller currently is.
        float heightDifference =
            standingHeight -
            characterController.height;

        /*
         * Move the center downward by half the height difference.
         *
         * This keeps the bottom of the CharacterController near
         * the floor as its height changes.
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
        // Calculate the extra vertical space required to stand.
        float extraHeightNeeded =
            standingHeight -
            characterController.height;

        // No physics check is needed if already effectively standing.
        if (extraHeightNeeded <= 0.01f)
        {
            return true;
        }

        // Find the CharacterController center in world space.
        Vector3 controllerCenter =
            transform.TransformPoint(
                characterController.center);

        // Find the current top of the CharacterController.
        float currentTop =
            controllerCenter.y +
            (characterController.height / 2f);

        // Begin the overhead check from the current top.
        Vector3 checkStart =
            new Vector3(
                controllerCenter.x,
                currentTop,
                controllerCenter.z);

        // Use a slightly smaller radius to avoid edge sensitivity.
        float checkRadius =
            Mathf.Max(
                0.01f,
                characterController.radius -
                headroomCheckPadding);

        // Check the additional space needed for the player to stand.
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

        // Standing is safe only when nothing blocks the required space.
        return !blocked;
    }

    #endregion


    #region Gravity Methods

    private void HandleGravity()
    {
        // Maintain slight downward pressure while grounded.
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
            // Apply gravity continuously while airborne.
            verticalVelocity +=
                gravity * Time.deltaTime;
        }
    }

    #endregion
}