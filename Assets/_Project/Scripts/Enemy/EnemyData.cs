using UnityEngine;

/*
 * Script Name: EnemyData
 * Purpose:
 * Stores reusable configuration data for an enemy type.
 *
 * This script is a ScriptableObject.
 *
 * That means it does NOT need to be attached directly
 * to an enemy GameObject.
 *
 * Instead, Unity can create EnemyData assets inside the Project window.
 *
 * Example assets:
 * - EnemyData_Brute
 * - EnemyData_Spitter
 * - EnemyData_FastMutant
 *
 * Each asset can store different values for:
 * - Health
 * - Movement speed
 * - Attack damage
 * - Detection distance
 * - Attack distance
 *
 * Later, our enemy AI scripts can read from these assets
 * instead of hard-coding values into every enemy prefab.
 */

[CreateAssetMenu(
    fileName = "NewEnemyData",
    menuName = "Containment Breach/Enemies/Enemy Data"
)]
public class EnemyData : ScriptableObject
{
    #region Identity Settings

    /*
     * REGION: Identity Settings
     *
     * This region stores basic information
     * used to identify the enemy type.
     */

    [Header("Identity Settings")]

    [SerializeField]
    [Tooltip("The internal name used to identify this enemy type.")]
    private string enemyName = "New Enemy";

    [SerializeField]
    [TextArea(2, 5)]
    [Tooltip("A short description of the enemy and its intended gameplay role.")]
    private string description = "Enemy description.";

    #endregion


    #region Health Settings

    /*
     * REGION: Health Settings
     *
     * This region stores values related
     * to the enemy's health and survivability.
     */

    [Header("Health Settings")]

    [SerializeField]
    [Min(1f)]
    [Tooltip("The maximum amount of health this enemy has.")]
    private float maxHealth = 100f;

    #endregion


    #region Movement Settings

    /*
     * REGION: Movement Settings
     *
     * This region contains values that control
     * how quickly the enemy moves.
     */

    [Header("Movement Settings")]

    [SerializeField]
    [Min(0f)]
    [Tooltip("The enemy's normal movement speed.")]
    private float moveSpeed = 3.5f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("The enemy's faster movement speed when aggressively pursuing the player.")]
    private float chaseSpeed = 5f;

    #endregion


    #region Detection Settings

    /*
     * REGION: Detection Settings
     *
     * These values will eventually be used
     * by the enemy AI perception system.
     */

    [Header("Detection Settings")]

    [SerializeField]
    [Min(0f)]
    [Tooltip("How far away the enemy can detect the player.")]
    private float detectionRange = 15f;

    [SerializeField]
    [Range(0f, 360f)]
    [Tooltip("The enemy's horizontal field of view in degrees.")]
    private float fieldOfView = 120f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("How far away the enemy can continue tracking the player after detecting them.")]
    private float loseTargetRange = 20f;

    #endregion


    #region Combat Settings

    /*
     * REGION: Combat Settings
     *
     * This region stores the enemy's basic combat values.
     */

    [Header("Combat Settings")]

    [SerializeField]
    [Min(0f)]
    [Tooltip("How much damage the enemy deals with a successful attack.")]
    private float attackDamage = 20f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("How close the enemy must be to perform an attack.")]
    private float attackRange = 2f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("How many seconds must pass between enemy attacks.")]
    private float attackCooldown = 1.5f;

    #endregion


    #region Public Properties

    /*
     * REGION: Public Properties
     *
     * These properties allow other scripts
     * to READ the enemy's configuration values.
     *
     * Other scripts cannot directly overwrite
     * these values during normal gameplay.
     *
     * This helps keep EnemyData responsible
     * for storing configuration while gameplay
     * scripts handle actual enemy behavior.
     */

    public string EnemyName
    {
        get { return enemyName; }
    }

    public string Description
    {
        get { return description; }
    }

    public float MaxHealth
    {
        get { return maxHealth; }
    }

    public float MoveSpeed
    {
        get { return moveSpeed; }
    }

    public float ChaseSpeed
    {
        get { return chaseSpeed; }
    }

    public float DetectionRange
    {
        get { return detectionRange; }
    }

    public float FieldOfView
    {
        get { return fieldOfView; }
    }

    public float LoseTargetRange
    {
        get { return loseTargetRange; }
    }

    public float AttackDamage
    {
        get { return attackDamage; }
    }

    public float AttackRange
    {
        get { return attackRange; }
    }

    public float AttackCooldown
    {
        get { return attackCooldown; }
    }

    #endregion
}