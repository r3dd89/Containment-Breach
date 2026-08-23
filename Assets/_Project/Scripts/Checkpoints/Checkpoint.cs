using UnityEngine;

/*
 * Script Name: Checkpoint
 * Purpose:
 * Represents an individual checkpoint location in the scene.
 *
 * This script currently handles:
 * - Identifying a checkpoint
 * - Defining the player respawn position
 * - Defining the player respawn rotation
 * - Activating the checkpoint when the player enters its trigger
 *
 * The CheckpointManager is responsible for storing
 * which checkpoint is currently active.
 */

public class Checkpoint : MonoBehaviour
{
    #region Checkpoint Settings

    /*
     * REGION: Checkpoint Settings
     *
     * This region contains configuration values
     * that describe this checkpoint.
     */

    [Header("Checkpoint Settings")]

    [SerializeField]
    [Tooltip("A readable identifier used to distinguish this checkpoint from others.")]
    private string checkpointID = "Checkpoint_01";

    [SerializeField]
    [Tooltip("The Transform that defines where the player should respawn.")]
    private Transform respawnPoint;

    #endregion


    #region Runtime State

    /*
     * REGION: Runtime State
     *
     * This region stores information that changes
     * while the game is running.
     */

    [Header("Runtime State")]

    [SerializeField]
    [Tooltip("Shows whether this checkpoint has already been activated.")]
    private bool hasBeenActivated;

    #endregion


    #region Public Properties

    /*
     * REGION: Public Properties
     *
     * These properties allow other systems
     * to safely read checkpoint information.
     */

    public string CheckpointID
    {
        get { return checkpointID; }
    }

    public Vector3 RespawnPosition
    {
        get
        {
            if (respawnPoint != null)
            {
                return respawnPoint.position;
            }

            return transform.position;
        }
    }

    public Quaternion RespawnRotation
    {
        get
        {
            if (respawnPoint != null)
            {
                return respawnPoint.rotation;
            }

            return transform.rotation;
        }
    }

    public bool HasBeenActivated
    {
        get { return hasBeenActivated; }
    }

    #endregion


    #region Unity Methods

    /*
     * REGION: Unity Methods
     *
     * Unity automatically calls these methods.
     */

    private void Awake()
    {
        /*
         * Use this GameObject as the respawn location
         * if a separate Respawn Point was not assigned.
         */
        if (respawnPoint == null)
        {
            respawnPoint = transform;
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        /*
         * Ignore this trigger if the checkpoint
         * has already been activated.
         */
        if (hasBeenActivated)
        {
            return;
        }

        /*
         * Look for PlayerHealth on the entering object.
         *
         * This identifies the player without relying
         * on a hard-coded tag.
         */
        PlayerHealth playerHealth =
            other.GetComponent<PlayerHealth>();

        /*
         * Stop if the object entering the trigger
         * is not the player.
         */
        if (playerHealth == null)
        {
            return;
        }

        /*
         * Find the active CheckpointManager.
         */
        if (CheckpointManager.Instance == null)
        {
            return;
        }

        /*
         * Register this checkpoint
         * as the player's current active checkpoint.
         */
        CheckpointManager.Instance.ActivateCheckpoint(this);

        /*
         * Prevent repeated activation
         * every time the player enters this trigger.
         */
        hasBeenActivated = true;
    }

    #endregion


    // TODO: Add checkpoint activation visual feedback.
    // TODO: Add checkpoint activation sound.
    // TODO: Add optional checkpoint objective requirements.
}