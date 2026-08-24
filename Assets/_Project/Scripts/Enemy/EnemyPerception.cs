using UnityEngine;

/*
 * Script Name: EnemyPerception
 * Purpose:
 * Stores non-visual perception information used by enemy AI.
 *
 * Current responsibility:
 * - Receives a noise position.
 * - Stores that position as an investigation target.
 * - Tracks whether an active investigation target exists.
 *
 * Future responsibility:
 * - Hearing range checks.
 * - Noise intensity.
 * - Different sound categories.
 * - Alert propagation between nearby enemies.
 * - Environmental stimuli.
 */

public class EnemyPerception : MonoBehaviour
{
    #region Perception Settings

    [Header("Perception Settings")]

    [Tooltip("Maximum distance from which this enemy can react to a noise.")]
    [SerializeField] private float hearingRange = 12f;

    #endregion


    #region Runtime Perception Data

    [Header("Runtime Perception Data")]

    [Tooltip("True when the enemy currently has a noise location to investigate.")]
    [SerializeField] private bool hasInvestigationTarget;

    [Tooltip("World position of the most recently accepted noise.")]
    [SerializeField] private Vector3 investigationPosition;

    [Tooltip("Distance from the enemy to the most recently submitted noise.")]
    [SerializeField] private float distanceToNoise;

    #endregion


    #region Public Properties

    public bool HasInvestigationTarget
    {
        get { return hasInvestigationTarget; }
    }


    public Vector3 InvestigationPosition
    {
        get { return investigationPosition; }
    }


    public float HearingRange
    {
        get { return hearingRange; }
    }

    #endregion


    #region Public Methods

    public void RegisterNoise(Vector3 noisePosition)
    {
        // Calculates the distance between the enemy and the submitted noise.
        distanceToNoise = Vector3.Distance(
            transform.position,
            noisePosition);

        // Ignores sounds that occur outside this enemy's hearing range.
        if (distanceToNoise > hearingRange)
        {
            return;
        }

        // Stores the noise location so other AI systems can investigate it.
        investigationPosition = noisePosition;

        // Marks the perception data as active.
        hasInvestigationTarget = true;
    }


    public void ClearInvestigationTarget()
    {
        // Removes the currently active investigation target.
        hasInvestigationTarget = false;

        // Clears the stored position for easier Inspector debugging.
        investigationPosition = Vector3.zero;

        // Clears the previous distance value.
        distanceToNoise = 0f;
    }

    #endregion
}