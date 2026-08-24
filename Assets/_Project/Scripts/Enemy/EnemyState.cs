/*
 * Script Name: EnemyState
 * Purpose:
 * Defines the possible behavioral states used by enemy AI.
 *
 * This enum does not contain AI behavior itself.
 *
 * It provides a shared list of named states that other enemy
 * systems can use when deciding how an enemy should behave.
 *
 * Keeping the states in one definition helps prevent
 * inconsistent state names across different enemy scripts.
 */

public enum EnemyState
{
    #region Core States

    /*
     * STATE: Patrol
     *
     * The enemy follows its normal patrol behavior
     * and has not detected anything suspicious.
     */
    Patrol,


    /*
     * STATE: Investigate
     *
     * The enemy noticed a suspicious event,
     * such as a sound or possible player activity,
     * and moves toward the source to investigate.
     */
    Investigate,


    /*
     * STATE: Chase
     *
     * The enemy has confirmed the player as a target
     * and actively pursues the player.
     */
    Chase,


    /*
     * STATE: Search
     *
     * The enemy lost direct contact with the player
     * but continues searching around the player's
     * last known position.
     */
    Search,


    /*
     * STATE: Attack
     *
     * The player is within the enemy's attack range
     * and the enemy can perform combat behavior.
     */
    Attack,


    /*
     * STATE: Return
     *
     * The enemy has finished searching or pursuing
     * and returns toward its normal patrol area.
     */
    Return

    #endregion
}