using UnityEngine;
using UnityEngine.InputSystem;

/*
 * Script Name: NoiseEmitter
 * Purpose:
 * Creates a simple test noise event that can be received by nearby enemies.
 *
 * Current use:
 * - Press the N key during Play Mode.
 * - Find nearby EnemyPerception components.
 * - Send the noise position to enemies inside the configured radius.
 *
 * TODO:
 * Replace temporary keyboard testing with gameplay-driven noise events
 * such as footsteps, gunshots, doors, machinery, and thrown objects.
 */

public class NoiseEmitter : MonoBehaviour
{
    #region Noise Settings

    [Header("Noise Settings")]

    [Tooltip("Maximum distance from this object that enemies can receive the emitted test noise.")]
    [SerializeField] private float noiseRadius = 12f;

    #endregion


    #region Test Settings

    [Header("Test Settings")]

    [Tooltip("Allows the temporary test noise to be emitted by pressing the N key during Play Mode.")]
    [SerializeField] private bool enableTestInput = true;

    #endregion


    #region Unity Methods

    private void Update()
    {
        // Stops temporary keyboard testing when test input is disabled.
        if (!enableTestInput)
        {
            return;
        }

        // Protects against keyboard input being unavailable.
        if (Keyboard.current == null)
        {
            return;
        }

        // Emits a test noise from this object's current world position.
        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            EmitNoise();
        }
    }

    #endregion


    #region Public Methods

    public void EmitNoise()
    {
        // Finds every collider located inside the configured noise radius.
        Collider[] nearbyColliders =
            Physics.OverlapSphere(
                transform.position,
                noiseRadius);

        // Checks each nearby collider for an enemy perception system.
        foreach (Collider nearbyCollider in nearbyColliders)
        {
            EnemyPerception enemyPerception =
                nearbyCollider.GetComponentInParent<EnemyPerception>();

            // Ignores objects that are not part of an enemy.
            if (enemyPerception == null)
            {
                continue;
            }

            // Sends this object's position to the enemy as a disturbance
            // that can trigger investigation behavior.
            enemyPerception.RegisterNoise(transform.position);
        }
    }

    #endregion


    #region Editor Visualization

    private void OnDrawGizmosSelected()
    {
        // Displays the noise radius in the Scene view while this object is selected.
        Gizmos.DrawWireSphere(
            transform.position,
            noiseRadius);
    }

    #endregion
}