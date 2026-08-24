using UnityEngine;

/*
 * Script Name: EnemyStateMachine
 * Purpose:
 * Controls the current behavioral state of an enemy and determines
 * when the enemy should transition between AI states.
 *
 * Current state flow:
 * - Patrol
 * - Investigate
 * - Chase
 * - Attack
 * - Search
 * - Return
 *
 * The state machine decides WHAT behavior should be active.
 * EnemyMovement controls HOW the enemy physically moves for that state.
 */

public class EnemyStateMachine : MonoBehaviour
{
    #region Component References

    [Header("Component References")]

    [Tooltip("Provides access to the enemy's current target and detection state.")]
    [SerializeField] private EnemyController enemyController;

    [Tooltip("Provides information about whether the player is inside attack range.")]
    [SerializeField] private EnemyAttack enemyAttack;

    [Tooltip("Provides movement information such as investigation arrival and return-to-patrol status.")]
    [SerializeField] private EnemyMovement enemyMovement;

    [Tooltip("Provides non-visual perception information such as noise investigation targets.")]
    [SerializeField] private EnemyPerception enemyPerception;

    #endregion


    #region Investigation Settings

    [Header("Investigation Settings")]

    [Tooltip("Amount of time the enemy remains at the disturbance location after physically reaching it.")]
    [SerializeField] private float investigationDuration = 5f;

    #endregion


    #region Search Settings

    [Header("Search Settings")]

    [Tooltip("Amount of time the enemy remains in the Search state before returning to patrol.")]
    [SerializeField] private float searchDuration = 5f;

    #endregion


    #region Runtime Debug Information

    [Header("Runtime Debug Information")]

    [Tooltip("The enemy's currently active AI state.")]
    [SerializeField] private EnemyState currentState;

    [Tooltip("The AI state that was active immediately before the current state.")]
    [SerializeField] private EnemyState previousState;

    [Tooltip("Tracks total time spent in the current state.")]
    [SerializeField] private float stateTimer;

    [Tooltip("Tracks how long the enemy has remained at the investigation location after arriving.")]
    [SerializeField] private float investigationWaitTimer;

    #endregion


    #region Public Properties

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

    private void Awake()
    {
        // Finds EnemyController automatically if the Inspector reference was not assigned.
        if (enemyController == null)
        {
            enemyController = GetComponent<EnemyController>();
        }

        // Finds EnemyAttack automatically if needed.
        if (enemyAttack == null)
        {
            enemyAttack = GetComponent<EnemyAttack>();
        }

        // Finds EnemyMovement automatically if needed.
        if (enemyMovement == null)
        {
            enemyMovement = GetComponent<EnemyMovement>();
        }

        // Finds EnemyPerception automatically if needed.
        if (enemyPerception == null)
        {
            enemyPerception = GetComponent<EnemyPerception>();
        }
    }


    private void Start()
    {
        // Enemies begin in the normal patrol state.
        ChangeState(EnemyState.Patrol);
    }


    private void Update()
    {
        // Tracks total time spent in the current state.
        stateTimer += Time.deltaTime;

        // Runs transition logic for the active state.
        switch (currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrolState();
                break;

            case EnemyState.Investigate:
                UpdateInvestigateState();
                break;

            case EnemyState.Chase:
                UpdateChaseState();
                break;

            case EnemyState.Attack:
                UpdateAttackState();
                break;

            case EnemyState.Search:
                UpdateSearchState();
                break;

            case EnemyState.Return:
                UpdateReturnState();
                break;
        }
    }

    #endregion


    #region Patrol State

    private void UpdatePatrolState()
    {
        // Visual detection takes priority over hearing.
        if (enemyController != null &&
            enemyController.HasDetectedTarget &&
            enemyController.CurrentTarget != null)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        // A valid noise target causes the enemy to investigate.
        if (enemyPerception != null &&
            enemyPerception.HasInvestigationTarget)
        {
            ChangeState(EnemyState.Investigate);
        }
    }

    #endregion


    #region Investigate State

    private void UpdateInvestigateState()
    {
        // Seeing the player during an investigation immediately starts Chase.
        if (enemyController != null &&
            enemyController.HasDetectedTarget &&
            enemyController.CurrentTarget != null)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        // Stops investigation if the perception target no longer exists.
        if (enemyPerception == null ||
            !enemyPerception.HasInvestigationTarget)
        {
            ChangeState(EnemyState.Return);
            return;
        }

        // Travel time does NOT count toward the investigation wait time.
        if (enemyMovement == null ||
            !enemyMovement.HasReachedInvestigationPosition)
        {
            investigationWaitTimer = 0f;
            return;
        }

        // The enemy has physically reached the disturbance location.
        // Only now does the investigation timer begin.
        investigationWaitTimer += Time.deltaTime;

        // Remain at the location until the investigation duration finishes.
        if (investigationWaitTimer < investigationDuration)
        {
            return;
        }

        // Clears the completed noise target so Patrol does not immediately
        // re-enter Investigate after returning.
        enemyPerception.ClearInvestigationTarget();

        // Return to the patrol route after investigating the disturbance.
        ChangeState(EnemyState.Return);
    }

    #endregion


    #region Chase State

    private void UpdateChaseState()
    {
        // Losing the player causes the enemy to search the last known position.
        if (enemyController == null ||
            !enemyController.HasDetectedTarget ||
            enemyController.CurrentTarget == null)
        {
            ChangeState(EnemyState.Search);
            return;
        }

        // Enter Attack once the player reaches attack range.
        if (enemyAttack != null &&
            enemyAttack.TargetInAttackRange)
        {
            ChangeState(EnemyState.Attack);
        }
    }

    #endregion


    #region Attack State

    private void UpdateAttackState()
    {
        // Losing the player while attacking begins Search.
        if (enemyController == null ||
            !enemyController.HasDetectedTarget ||
            enemyController.CurrentTarget == null)
        {
            ChangeState(EnemyState.Search);
            return;
        }

        // Resume Chase if the player moves outside attack range.
        if (enemyAttack != null &&
            !enemyAttack.TargetInAttackRange)
        {
            ChangeState(EnemyState.Chase);
        }
    }

    #endregion


    #region Search State

    private void UpdateSearchState()
    {
        // Reacquiring the player immediately interrupts Search.
        if (enemyController != null &&
            enemyController.HasDetectedTarget &&
            enemyController.CurrentTarget != null)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        // Search currently uses a fixed duration.
        if (stateTimer >= searchDuration)
        {
            ChangeState(EnemyState.Return);
        }
    }

    #endregion


    #region Return State

    private void UpdateReturnState()
    {
        // Visual detection always takes priority.
        if (enemyController != null &&
            enemyController.HasDetectedTarget &&
            enemyController.CurrentTarget != null)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        // A new noise can interrupt Return.
        if (enemyPerception != null &&
            enemyPerception.HasInvestigationTarget)
        {
            ChangeState(EnemyState.Investigate);
            return;
        }

        // Return finishes only after EnemyMovement reports that the
        // patrol route has physically been reached.
        if (enemyMovement != null &&
            enemyMovement.HasReturnedToPatrolRoute)
        {
            ChangeState(EnemyState.Patrol);
        }
    }

    #endregion


    #region State Transition Methods

    private void ChangeState(EnemyState newState)
    {
        // Prevents unnecessary transitions into the currently active state.
        if (currentState == newState)
        {
            return;
        }

        // Stores the previous state for Inspector debugging.
        previousState = currentState;

        // Activates the requested state.
        currentState = newState;

        // Resets the general state timer.
        stateTimer = 0f;

        // Investigation waiting must begin fresh whenever a state changes.
        investigationWaitTimer = 0f;
    }

    #endregion
}