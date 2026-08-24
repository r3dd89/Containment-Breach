using UnityEngine;

/*
 * Script Name: EnemyPatrolRoute
 * Purpose:
 * Stores and manages an ordered set of patrol points
 * for an enemy patrol route.
 *
 * This script currently handles:
 * - Storing patrol-point Transforms
 * - Tracking the current patrol-point index
 * - Returning the active patrol destination
 * - Advancing to the next patrol point
 * - Looping back to the beginning of the route
 *
 * Actual movement between patrol points is handled
 * by EnemyMovement.
 */

public class EnemyPatrolRoute : MonoBehaviour
{
    #region Patrol Point Settings

    /*
     * REGION: Patrol Point Settings
     *
     * This region stores the ordered patrol points
     * that make up the enemy's patrol route.
     */

    [Header("Patrol Point Settings")]

    [SerializeField]
    [Tooltip("Ordered list of patrol points the enemy should follow.")]
    private Transform[] patrolPoints;

    #endregion


    #region Runtime Patrol State

    /*
     * REGION: Runtime Patrol State
     *
     * These values track which patrol point
     * is currently active during gameplay.
     */

    [Header("Runtime Patrol State")]

    [SerializeField]
    [Tooltip("The index of the patrol point currently being used as the destination.")]
    private int currentPatrolIndex;

    #endregion


    #region Public Properties

    /*
     * REGION: Public Properties
     *
     * These properties allow other enemy systems
     * to safely read patrol-route information.
     */

    public bool HasPatrolPoints
    {
        get
        {
            return patrolPoints != null &&
                   patrolPoints.Length > 0;
        }
    }

    public Transform CurrentPatrolPoint
    {
        get
        {
            /*
             * Return null if no patrol points exist.
             */
            if (!HasPatrolPoints)
            {
                return null;
            }

            /*
             * Make sure the index remains valid.
             */
            currentPatrolIndex = Mathf.Clamp(
                currentPatrolIndex,
                0,
                patrolPoints.Length - 1
            );

            /*
             * Return the currently active patrol point.
             */
            return patrolPoints[currentPatrolIndex];
        }
    }

    public int CurrentPatrolIndex
    {
        get { return currentPatrolIndex; }
    }

    public int PatrolPointCount
    {
        get
        {
            if (patrolPoints == null)
            {
                return 0;
            }

            return patrolPoints.Length;
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
         * Start the patrol route
         * at the first patrol point.
         */
        currentPatrolIndex = 0;
    }

    #endregion


    #region Patrol Route Methods

    /*
     * REGION: Patrol Route Methods
     *
     * This region handles moving through
     * the ordered patrol-point list.
     */

    public void AdvanceToNextPatrolPoint()
    {
        /*
         * Stop if no patrol points exist.
         */
        if (!HasPatrolPoints)
        {
            return;
        }

        /*
         * Move to the next patrol-point index.
         */
        currentPatrolIndex++;

        /*
         * Loop back to the beginning
         * after reaching the end of the route.
         */
        if (currentPatrolIndex >= patrolPoints.Length)
        {
            currentPatrolIndex = 0;
        }
    }


    public void SetPatrolPointIndex(int newIndex)
    {
        /*
         * Stop if no patrol points exist.
         */
        if (!HasPatrolPoints)
        {
            return;
        }

        /*
         * Clamp the requested index
         * so it always stays inside the route.
         */
        currentPatrolIndex = Mathf.Clamp(
            newIndex,
            0,
            patrolPoints.Length - 1
        );
    }

    #endregion


    // TODO: Add optional random patrol-point selection.
    // TODO: Add ping-pong patrol mode if needed.
    // TODO: Add per-point wait times.
}