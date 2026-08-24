using UnityEngine;

/*
 * Script Name: EnemyStateMachine
 * Purpose:
 * Controls the enemy's current behavioral state
 * and manages transitions between AI states.
 *
 * This script currently handles:
 * - Tracking the current EnemyState
 * - Starting enemies in Patrol
 * - Switching from Patrol to Chase after target detection
 * - Switching from Chase to Attack when the target enters attack range
 * - Returning from Attack to Chase when the target moves away
 * - Switching to Search after losing the target
 * - Timing the Search state
 * - Switching from Search to Return
 * - Timing the Return state
 * - Returning to Patrol
 *
 * Individual behavior such as patrol routes,
 * search movement, investigation, and return navigation
 * remains separated from state-selection logic.
 */

public class EnemyStateMachine : MonoBehaviour
{
    #region Component References

    /*
     * REGION: Component References
     *
     * This region stores references to the enemy systems
     * used to determine state transitions.
     */

    [Header("Component References")]

    [SerializeField]
    [Tooltip("The EnemyController that stores target and EnemyData information.")]
    private EnemyController enemyController;

    [SerializeField]
    [Tooltip("The EnemyAttack component that reports whether the target is within attack range.")]
    private EnemyAttack enemyAttack;

    #endregion


    #region State Settings

    /*
     * REGION: State Settings
     *
     * These values control timing for states
     * that continue for a limited duration.
     */

    [Header("State Settings")]

    [SerializeField]
    [Min(0f)]
    [Tooltip("How long the enemy searches after losing its target.")]
    private float searchDuration = 5f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("How long the temporary Return state remains active before returning to Patrol.")]
    private float returnDuration = 1f;

    #endregion


    #region Runtime State

    /*
     * REGION: Runtime State
     *
     * These values show the enemy's current AI state
     * while the game is running.
     */

    [Header("Runtime State")]

    [SerializeField]
    [Tooltip("The enemy's currently active behavioral state.")]
    private EnemyState currentState = EnemyState.Patrol;

    [SerializeField]
    [Tooltip("The amount of time the enemy has spent in its current state.")]
    private float stateTimer;

    [SerializeField]
    [Tooltip("The last state the enemy was using before the current state.")]
    private EnemyState previousState = EnemyState.Patrol;

    #endregion


    #region Public Properties

    /*
     * REGION: Public Properties
     *
     * These properties allow other enemy systems
     * to safely read state-machine information.
     */

    public EnemyState CurrentState
    {
        get { return currentState; }
    }

    public EnemyState PreviousState
    {
        get { return previousState; }
    }

    public float StateTimer
    {
        get { return stateTimer; }
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
         * Automatically find EnemyController
         * if it was not assigned manually.
         */
        if (enemyController == null)
        {
            enemyController = GetComponent<EnemyController>();
        }

        /*
         * Automatically find EnemyAttack
         * if it was not assigned manually.
         */
        if (enemyAttack == null)
        {
            enemyAttack = GetComponent<EnemyAttack>();
        }
    }


    private void Start()
    {
        /*
         * Initialize the enemy in Patrol state.
         */
        currentState = EnemyState.Patrol;

        /*
         * Initialize the previous state
         * to match the starting state.
         */
        previousState = EnemyState.Patrol;

        /*
         * Reset the state timer.
         */
        stateTimer = 0f;
    }


    private void Update()
    {
        /*
         * Stop state-machine logic if the required
         * EnemyController component is missing.
         */
        if (enemyController == null)
        {
            return;
        }

        /*
         * Track how long the current state
         * has been active.
         */
        stateTimer += Time.deltaTime;

        /*
         * Run transition logic for the active state.
         */
        EvaluateCurrentState();
    }

    #endregion


    #region State Evaluation Methods

    /*
     * REGION: State Evaluation Methods
     *
     * This region determines which transition rules
     * should be checked for the active state.
     */

    private void EvaluateCurrentState()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                EvaluatePatrolState();
                break;

            case EnemyState.Investigate:
                EvaluateInvestigateState();
                break;

            case EnemyState.Chase:
                EvaluateChaseState();
                break;

            case EnemyState.Search:
                EvaluateSearchState();
                break;

            case EnemyState.Attack:
                EvaluateAttackState();
                break;

            case EnemyState.Return:
                EvaluateReturnState();
                break;
        }
    }


    private void EvaluatePatrolState()
    {
        /*
         * A confirmed target immediately changes
         * the enemy from Patrol to Chase.
         */
        if (enemyController.HasDetectedTarget)
        {
            ChangeState(EnemyState.Chase);
        }

        // TODO: Add patrol-route behavior.
        // TODO: Transition to Investigate after suspicious sound or environmental activity.
    }


    private void EvaluateInvestigateState()
    {
        /*
         * A confirmed player detection takes priority
         * over investigation behavior.
         */
        if (enemyController.HasDetectedTarget)
        {
            ChangeState(EnemyState.Chase);

            return;
        }

        // TODO: Move toward the investigation location.
        // TODO: Search the immediate investigation area.
        // TODO: Return to Patrol if nothing is found.
    }


    private void EvaluateChaseState()
    {
        /*
         * Losing the current target causes the enemy
         * to begin searching.
         */
        if (!enemyController.HasDetectedTarget ||
            enemyController.CurrentTarget == null)
        {
            ChangeState(EnemyState.Search);

            return;
        }

        /*
         * If the target enters attack range,
         * switch from Chase to Attack.
         */
        if (enemyAttack != null &&
            enemyAttack.TargetInAttackRange)
        {
            ChangeState(EnemyState.Attack);
        }
    }


    private void EvaluateAttackState()
    {
        /*
         * Losing the target during combat
         * causes the enemy to begin searching.
         */
        if (!enemyController.HasDetectedTarget ||
            enemyController.CurrentTarget == null)
        {
            ChangeState(EnemyState.Search);

            return;
        }

        /*
         * If the target leaves attack range,
         * resume chasing.
         */
        if (enemyAttack != null &&
            !enemyAttack.TargetInAttackRange)
        {
            ChangeState(EnemyState.Chase);
        }
    }


    private void EvaluateSearchState()
    {
        /*
         * Reacquiring the player immediately
         * returns the enemy to Chase.
         */
        if (enemyController.HasDetectedTarget)
        {
            ChangeState(EnemyState.Chase);

            return;
        }

        /*
         * Continue searching until the configured
         * search duration expires.
         */
        if (stateTimer >= searchDuration)
        {
            ChangeState(EnemyState.Return);
        }

        // TODO: Store and search around the player's last known position.
        // TODO: Add randomized nearby search points.
        // TODO: Add search animation and sensory behavior.
    }


    private void EvaluateReturnState()
    {
        /*
         * Player detection interrupts the Return state
         * and immediately begins another chase.
         */
        if (enemyController.HasDetectedTarget)
        {
            ChangeState(EnemyState.Chase);

            return;
        }

        /*
         * The current temporary implementation
         * transitions back to Patrol after a short delay.
         */
        if (stateTimer >= returnDuration)
        {
            ChangeState(EnemyState.Patrol);
        }

        // TODO: Replace the temporary timer with navigation
        // back to the enemy's patrol route or home position.
    }

    #endregion


    #region State Transition Methods

    /*
     * REGION: State Transition Methods
     *
     * This region handles changing from one state
     * to another and resetting state-specific timing.
     */

    private void ChangeState(EnemyState newState)
    {
        /*
         * Ignore requests to enter the state
         * that is already active.
         */
        if (currentState == newState)
        {
            return;
        }

        /*
         * Store the state being exited.
         */
        previousState = currentState;

        /*
         * Assign the new active state.
         */
        currentState = newState;

        /*
         * Reset the timer whenever a new state begins.
         */
        stateTimer = 0f;
    }

    #endregion
}