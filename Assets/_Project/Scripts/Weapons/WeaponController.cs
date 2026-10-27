using UnityEngine;

/*
 * Script Name: WeaponController
 * Purpose:
 * Handles the player's basic weapon firing behavior.
 *
 * Current responsibility:
 * - Reads the Fire action from PlayerInputActions.
 * - Fires a ray from the player's camera.
 * - Detects valid targets.
 * - Applies damage to enemies that have EnemyHealth.
 *
 * TODO:
 * - Add ammunition.
 * - Add reloading.
 * - Add fire-rate control.
 * - Add muzzle flash.
 * - Add weapon audio.
 * - Add recoil.
 * - Add hit effects.
 * - Send gunshot noise into EnemyPerception.
 */

public class WeaponController : MonoBehaviour
{
    #region Component References

    [Header("Component References")]

    [SerializeField]
    [Tooltip("Camera used to determine the direction the weapon fires.")]
    private Camera playerCamera;

    #endregion


    #region Weapon Settings

    [Header("Weapon Settings")]

    [SerializeField]
    [Tooltip("Amount of damage dealt when a valid enemy is hit.")]
    private float damage = 25f;

    [SerializeField]
    [Tooltip("Maximum distance the weapon can hit a target.")]
    private float range = 100f;

    [SerializeField]
    [Tooltip("Layers that the weapon raycast is allowed to hit.")]
    private LayerMask hitMask = ~0;

    #endregion


    #region Input

    /*
     * Generated from:
     * Assets/_Project/Input/PlayerInputActions.inputactions
     */
    private PlayerInputActions playerInputActions;

    #endregion


    #region Unity Methods

    private void Awake()
    {
        // Automatically finds the Camera component if the reference
        // was not assigned manually in the Inspector.
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        // Create the generated Input Actions wrapper.
        playerInputActions = new PlayerInputActions();
    }


    private void OnEnable()
    {
        // Enable the Player action map so Fire can receive input.
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
        // Release resources owned by the generated Input Actions instance.
        if (playerInputActions != null)
        {
            playerInputActions.Dispose();
        }
    }


    private void Update()
    {
        // Fire once when the Fire action is initially pressed.
        if (playerInputActions.Player.Fire.WasPressedThisFrame())
        {
            FireWeapon();
        }
    }

    #endregion


    #region Weapon Methods

    private void FireWeapon()
    {
        // Stop safely if no camera is available.
        if (playerCamera == null)
        {
            return;
        }

        // The weapon fires from the center of the player's view.
        Vector3 rayOrigin =
            playerCamera.transform.position;

        Vector3 rayDirection =
            playerCamera.transform.forward;

        /*
         * Cast a ray forward from the camera.
         *
         * The first collider hit within range is returned in hitInfo.
         */
        if (Physics.Raycast(
            rayOrigin,
            rayDirection,
            out RaycastHit hitInfo,
            range,
            hitMask,
            QueryTriggerInteraction.Ignore))
        {
            // Search upward through the hit object's hierarchy
            // for an EnemyHealth component.
            EnemyHealth enemyHealth =
                hitInfo.collider.GetComponentInParent<EnemyHealth>();

            // Ignore hits that do not belong to an enemy.
            if (enemyHealth == null)
            {
                return;
            }

            // Apply weapon damage to the enemy.
            enemyHealth.TakeDamage(damage);
        }
    }

    #endregion
}