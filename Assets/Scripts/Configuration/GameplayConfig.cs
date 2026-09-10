using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GhostArena
{
    [CreateAssetMenu(fileName = "GameplayConfig", menuName = "Ghost Arena/Gameplay Config")]
    public sealed class GameplayConfig : ScriptableObject
    {
        [Header("Match rules")]
        [Tooltip("Condition used to win the match.")]
        [SerializeField] private WinRule _winRule = WinRule.SurviveTime;
        [Tooltip("Seconds required by the Survive Time rule.")]
        [SerializeField] private float _surviveDuration = 60f;
        [Tooltip("Kills required by the Kill Enemies rule.")]
        [SerializeField] private int _killTarget = 10;
        [Tooltip("Additional loss rule. Player death always ends the match immediately.")]
        [SerializeField] private LoseRule _loseRule = LoseRule.PlayerDeath;
        [Tooltip("The Alive Enemies rule loses when the number of living enemies exceeds this limit.")]
        [FormerlySerializedAs("_totalSpawnsLimit")]
        [SerializeField] private int _enemyLimit = 25;

        [Header("Player")]
        [Tooltip("Player health at the start of each session.")]
        [SerializeField] private int _playerMaximumHealth = 3;
        [Tooltip("Scaled seconds of protection from all enemy contacts after an accepted contact hit.")]
        [SerializeField] private float _playerContactGracePeriod = 0.65f;
        [Tooltip("Player movement speed in world units per second.")]
        [SerializeField] private float _playerMovementSpeed = 5f;

        [Header("Enemy")]
        [Tooltip("Health assigned to every enemy spawned in the session.")]
        [SerializeField] private int _enemyMaximumHealth = 2;
        [Tooltip("NavMesh movement speed assigned to every enemy.")]
        [SerializeField] private float _enemyMovementSpeed = 2f;
        [Tooltip("Seconds between attempts to choose a new random destination.")]
        [SerializeField] private float _enemyDirectionInterval = 2f;
        [Tooltip("Damage dealt when an active enemy first touches the player.")]
        [SerializeField] private int _contactDamage = 1;
        [Tooltip("Seconds between enemy spawns.")]
        [SerializeField] private float _spawnInterval = 3f;

        [Header("Projectile")]
        [Tooltip("Projectile movement speed in world units per second.")]
        [SerializeField] private float _projectileMovementSpeed = 14f;
        [Tooltip("Damage dealt by a projectile hit.")]
        [SerializeField] private int _projectileDamage = 1;
        [Tooltip("Seconds before an unconsumed projectile is removed.")]
        [SerializeField] private float _projectileLifetime = 2f;

        public GameplaySettings CreateSettings()
        {
            Validate();

            return new GameplaySettings(
                _winRule,
                _surviveDuration,
                _killTarget,
                _loseRule,
                _enemyLimit,
                _playerMaximumHealth,
                _playerContactGracePeriod,
                _playerMovementSpeed,
                new EnemySettings(
                    _enemyMaximumHealth,
                    _enemyMovementSpeed,
                    _enemyDirectionInterval,
                    _contactDamage),
                _spawnInterval,
                new ProjectileSettings(
                    _projectileMovementSpeed,
                    _projectileDamage,
                    _projectileLifetime));
        }

        private void Validate()
        {
            if (Enum.IsDefined(typeof(WinRule), _winRule) == false)
            {
                throw new InvalidOperationException("Gameplay config has an unknown win rule.");
            }

            if (Enum.IsDefined(typeof(LoseRule), _loseRule) == false)
            {
                throw new InvalidOperationException("Gameplay config has an unknown lose rule.");
            }

            ValidatePositiveFinite(_surviveDuration, "Survive duration");
            ValidatePositive(_killTarget, "Kill target");
            ValidatePositive(_enemyLimit, "Alive enemy limit");
            ValidatePositive(_playerMaximumHealth, "Player maximum health");
            ValidatePositiveFinite(_playerContactGracePeriod, "Player contact grace period");
            ValidatePositiveFinite(_playerMovementSpeed, "Player movement speed");
            ValidatePositive(_enemyMaximumHealth, "Enemy maximum health");
            ValidatePositiveFinite(_enemyMovementSpeed, "Enemy movement speed");
            ValidatePositiveFinite(_enemyDirectionInterval, "Enemy direction interval");
            ValidatePositive(_contactDamage, "Contact damage");
            ValidatePositiveFinite(_spawnInterval, "Spawn interval");
            ValidatePositiveFinite(_projectileMovementSpeed, "Projectile movement speed");
            ValidatePositive(_projectileDamage, "Projectile damage");
            ValidatePositiveFinite(_projectileLifetime, "Projectile lifetime");
        }

        private static void ValidatePositive(int value, string name)
        {
            if (value <= 0)
            {
                throw new InvalidOperationException(name + " must be positive.");
            }
        }

        private static void ValidatePositiveFinite(float value, string name)
        {
            if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new InvalidOperationException(name + " must be finite and positive.");
            }
        }
    }

    public readonly struct GameplaySettings
    {
        public GameplaySettings(
            WinRule winRule,
            float surviveDuration,
            int killTarget,
            LoseRule loseRule,
            int enemyLimit,
            int playerMaximumHealth,
            float playerContactGracePeriod,
            float playerMovementSpeed,
            EnemySettings enemy,
            float spawnInterval,
            ProjectileSettings projectile)
        {
            WinRule = winRule;
            SurviveDuration = surviveDuration;
            KillTarget = killTarget;
            LoseRule = loseRule;
            EnemyLimit = enemyLimit;
            PlayerMaximumHealth = playerMaximumHealth;
            PlayerContactGracePeriod = playerContactGracePeriod;
            PlayerMovementSpeed = playerMovementSpeed;
            Enemy = enemy;
            SpawnInterval = spawnInterval;
            Projectile = projectile;
        }

        public WinRule WinRule { get; }

        public float SurviveDuration { get; }

        public int KillTarget { get; }

        public LoseRule LoseRule { get; }

        public int EnemyLimit { get; }

        public int PlayerMaximumHealth { get; }

        public float PlayerContactGracePeriod { get; }

        public float PlayerMovementSpeed { get; }

        public EnemySettings Enemy { get; }

        public float SpawnInterval { get; }

        public ProjectileSettings Projectile { get; }
    }

    public readonly struct EnemySettings
    {
        public EnemySettings(
            int maximumHealth,
            float movementSpeed,
            float directionInterval,
            int contactDamage)
        {
            MaximumHealth = maximumHealth;
            MovementSpeed = movementSpeed;
            DirectionInterval = directionInterval;
            ContactDamage = contactDamage;
        }

        public int MaximumHealth { get; }

        public float MovementSpeed { get; }

        public float DirectionInterval { get; }

        public int ContactDamage { get; }
    }

    public readonly struct ProjectileSettings
    {
        public ProjectileSettings(float movementSpeed, int damage, float lifetime)
        {
            MovementSpeed = movementSpeed;
            Damage = damage;
            Lifetime = lifetime;
        }

        public float MovementSpeed { get; }

        public int Damage { get; }

        public float Lifetime { get; }

        internal bool IsValid => MovementSpeed > 0f
            && float.IsNaN(MovementSpeed) == false
            && float.IsInfinity(MovementSpeed) == false
            && Damage > 0
            && Lifetime > 0f
            && float.IsNaN(Lifetime) == false
            && float.IsInfinity(Lifetime) == false;
    }
}
