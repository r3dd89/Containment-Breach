using UnityEngine;

/*
 * Script Name: CheckpointManager
 * Purpose:
 * Tracks the player's currently active checkpoint
 * and provides respawn information to other game systems.
 *
 * This script currently handles:
 * - Creating a globally accessible CheckpointManager instance
 * - Preventing duplicate manager objects
 * - Storing the currently active checkpoint
 * - Providing the active respawn position
 * - Providing the active respawn rotation
 */

public class CheckpointManager : MonoBehaviour
{
    #region Singleton

    /*
     * REGION: Singleton
     *
     * This region provides a single global reference
     * to the active CheckpointManager instance.
     *
     * Other scripts can access this manager through:
     *
     * CheckpointManager.Instance
     */

    public static CheckpointManager Instance { get; private set; }

    #endregion


    #region Runtime Checkpoint State

    /*
     * REGION: Runtime Checkpoint State
     *
     * This region stores information about
     * the player's currently active checkpoint.
     */

    [Header("Runtime Checkpoint State")]

    [SerializeField]
    [Tooltip("The checkpoint currently registered as the player's active checkpoint.")]
    private Checkpoint activeCheckpoint;

    #endregion


    #region Public Properties

    /*
     * REGION: Public Properties
     *
     * These properties allow other systems
     * to safely read checkpoint information.
     */

    public Checkpoint ActiveCheckpoint
    {
        get { return activeCheckpoint; }
    }

    public bool HasActiveCheckpoint
    {
        get { return activeCheckpoint != null; }
    }

    public Vector3 ActiveRespawnPosition
    {
        get
        {
            /*
             * Return the active checkpoint's
             * configured respawn position.
             */
            if (activeCheckpoint != null)
            {
                return activeCheckpoint.RespawnPosition;
            }

            /*
             * Return Vector3.zero if no checkpoint
             * has been activated yet.
             *
             * GameManager will eventually decide
             * how to handle this fallback case.
             */
            return Vector3.zero;
        }
    }

    public Quaternion ActiveRespawnRotation
    {
        get
        {
            /*
             * Return the active checkpoint's
             * configured respawn rotation.
             */
            if (activeCheckpoint != null)
            {
                return activeCheckpoint.RespawnRotation;
            }

            /*
             * Return an identity rotation
             * if no checkpoint has been activated.
             */
            return Quaternion.identity;
        }
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
         * Assign this object as the active instance
         * if no CheckpointManager currently exists.
         */
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            /*
             * Destroy duplicate CheckpointManager objects.
             */
            Destroy(gameObject);
        }
    }

    #endregion


    #region Checkpoint Methods

    /*
     * REGION: Checkpoint Methods
     *
     * This region manages checkpoint activation.
     */

    public void ActivateCheckpoint(Checkpoint checkpoint)
    {
        /*
         * Ignore invalid checkpoint references.
         */
        if (checkpoint == null)
        {
            return;
        }

        /*
         * Store the checkpoint as the currently
         * active player checkpoint.
         */
        activeCheckpoint = checkpoint;
    }

    #endregion


    // TODO: Store the player's initial spawn location as a fallback.
    // TODO: Add checkpoint persistence to the save system.
    // TODO: Add support for restoring checkpoint-specific world state.
}