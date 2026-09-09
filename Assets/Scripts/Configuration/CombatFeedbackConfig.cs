using System;
using UnityEngine;

namespace GhostArena
{
    [CreateAssetMenu(fileName = "CombatFeedbackConfig", menuName = "Ghost Arena/Combat Feedback Config")]
    public sealed class CombatFeedbackConfig : ScriptableObject
    {
        [Header("Audio clips")]
        [Tooltip("Played for each accepted player shot.")]
        [SerializeField] private AudioClip _shotClip;
        [Tooltip("Played when a projectile damages a living enemy.")]
        [SerializeField] private AudioClip _enemyHitClip;
        [Tooltip("Played when an enemy dies.")]
        [SerializeField] private AudioClip _enemyDeathClip;
        [Tooltip("Played when the player loses health.")]
        [SerializeField] private AudioClip _playerHurtClip;
        [Tooltip("Played when an enemy is added to the session registry.")]
        [SerializeField] private AudioClip _spawnClip;
        [Tooltip("Played when the session ends in victory.")]
        [SerializeField] private AudioClip _victoryClip;
        [Tooltip("Played when the session ends in defeat.")]
        [SerializeField] private AudioClip _defeatClip;

        [Header("Cue gain")]
        [Tooltip("One-shot gain for the shot cue.")]
        [Range(0f, 1f)] [SerializeField] private float _shotGain = 0.8f;
        [Tooltip("One-shot gain for the enemy-hit cue.")]
        [Range(0f, 1f)] [SerializeField] private float _enemyHitGain = 0.9f;
        [Tooltip("One-shot gain for the enemy-death cue.")]
        [Range(0f, 1f)] [SerializeField] private float _enemyDeathGain = 0.9f;
        [Tooltip("One-shot gain for the player-hurt cue.")]
        [Range(0f, 1f)] [SerializeField] private float _playerHurtGain = 0.95f;
        [Tooltip("One-shot gain for the enemy-spawn cue.")]
        [Range(0f, 1f)] [SerializeField] private float _spawnGain = 0.75f;
        [Tooltip("One-shot gain for the victory cue.")]
        [Range(0f, 1f)] [SerializeField] private float _victoryGain = 0.95f;
        [Tooltip("One-shot gain for the defeat cue.")]
        [Range(0f, 1f)] [SerializeField] private float _defeatGain = 0.95f;

        [Header("Source gain")]
        [Tooltip("Master volume applied to each pooled SFX AudioSource at session bind.")]
        [Range(0f, 1f)] [SerializeField] private float _sfxSourceGain = 0.25f;
        [Tooltip("Master volume applied to the result AudioSource at session bind.")]
        [Range(0f, 1f)] [SerializeField] private float _resultSourceGain = 0.5f;

        [Header("Effects")]
        [Tooltip("Particle prefab created when an enemy spawns.")]
        [SerializeField] private ParticleSystem _spawnEffectPrefab;
        [Tooltip("Particle prefab created when a projectile hits an enemy.")]
        [SerializeField] private ParticleSystem _hitEffectPrefab;
        [Tooltip("Particle prefab created when an actor dies.")]
        [SerializeField] private ParticleSystem _deathEffectPrefab;
        [Tooltip("Seconds before a temporary material flash is restored.")]
        [SerializeField] private float _flashDuration = 0.09f;
        [Tooltip("Enemy base color during a damage flash.")]
        [SerializeField] private Color _enemyFlashColor = new Color(1f, 0.86f, 0.58f, 1f);
        [Tooltip("Enemy HDR emission color during a damage flash.")]
        [ColorUsage(true, true)]
        [SerializeField] private Color _enemyFlashEmission = new Color(1.2f, 0.55f, 0.12f, 1f);
        [Tooltip("Player base color during a damage flash.")]
        [SerializeField] private Color _playerFlashColor = new Color(1f, 0.54f, 0.49f, 1f);
        [Tooltip("Player HDR emission color during a damage flash.")]
        [ColorUsage(true, true)]
        [SerializeField] private Color _playerFlashEmission = new Color(1.1f, 0.18f, 0.12f, 1f);

        public CombatFeedbackSettings CreateSettings()
        {
            Validate();

            return new CombatFeedbackSettings(
                _shotClip,
                _enemyHitClip,
                _enemyDeathClip,
                _playerHurtClip,
                _spawnClip,
                _victoryClip,
                _defeatClip,
                _shotGain,
                _enemyHitGain,
                _enemyDeathGain,
                _playerHurtGain,
                _spawnGain,
                _victoryGain,
                _defeatGain,
                _sfxSourceGain,
                _resultSourceGain,
                _spawnEffectPrefab,
                _hitEffectPrefab,
                _deathEffectPrefab,
                _flashDuration,
                _enemyFlashColor,
                _enemyFlashEmission,
                _playerFlashColor,
                _playerFlashEmission);
        }

        private void Validate()
        {
            if (_shotClip == null || _enemyHitClip == null || _enemyDeathClip == null
                || _playerHurtClip == null || _spawnClip == null
                || _victoryClip == null || _defeatClip == null)
            {
                throw new InvalidOperationException("Combat feedback audio clips are not configured.");
            }

            if (_spawnEffectPrefab == null || _hitEffectPrefab == null || _deathEffectPrefab == null)
            {
                throw new InvalidOperationException("Combat feedback effect prefabs are not configured.");
            }

            ValidateGain(_shotGain, "Shot gain");
            ValidateGain(_enemyHitGain, "Enemy hit gain");
            ValidateGain(_enemyDeathGain, "Enemy death gain");
            ValidateGain(_playerHurtGain, "Player hurt gain");
            ValidateGain(_spawnGain, "Spawn gain");
            ValidateGain(_victoryGain, "Victory gain");
            ValidateGain(_defeatGain, "Defeat gain");
            ValidateGain(_sfxSourceGain, "SFX source gain");
            ValidateGain(_resultSourceGain, "Result source gain");

            if (_flashDuration <= 0f || float.IsNaN(_flashDuration) || float.IsInfinity(_flashDuration))
            {
                throw new InvalidOperationException("Flash duration must be finite and positive.");
            }

            ValidateColor(_enemyFlashColor, "Enemy flash color");
            ValidateColor(_enemyFlashEmission, "Enemy flash emission");
            ValidateColor(_playerFlashColor, "Player flash color");
            ValidateColor(_playerFlashEmission, "Player flash emission");
        }

        private static void ValidateGain(float value, string name)
        {
            if (value < 0f || value > 1f || float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new InvalidOperationException(name + " must be between zero and one.");
            }
        }

        private static void ValidateColor(Color value, string name)
        {
            bool isFinite = float.IsNaN(value.r) == false && float.IsInfinity(value.r) == false
                && float.IsNaN(value.g) == false && float.IsInfinity(value.g) == false
                && float.IsNaN(value.b) == false && float.IsInfinity(value.b) == false
                && float.IsNaN(value.a) == false && float.IsInfinity(value.a) == false;

            if (isFinite == false)
            {
                throw new InvalidOperationException(name + " must contain finite channels.");
            }
        }
    }

    public readonly struct CombatFeedbackSettings
    {
        public CombatFeedbackSettings(
            AudioClip shotClip,
            AudioClip enemyHitClip,
            AudioClip enemyDeathClip,
            AudioClip playerHurtClip,
            AudioClip spawnClip,
            AudioClip victoryClip,
            AudioClip defeatClip,
            float shotGain,
            float enemyHitGain,
            float enemyDeathGain,
            float playerHurtGain,
            float spawnGain,
            float victoryGain,
            float defeatGain,
            float sfxSourceGain,
            float resultSourceGain,
            ParticleSystem spawnEffectPrefab,
            ParticleSystem hitEffectPrefab,
            ParticleSystem deathEffectPrefab,
            float flashDuration,
            Color enemyFlashColor,
            Color enemyFlashEmission,
            Color playerFlashColor,
            Color playerFlashEmission)
        {
            ShotClip = shotClip;
            EnemyHitClip = enemyHitClip;
            EnemyDeathClip = enemyDeathClip;
            PlayerHurtClip = playerHurtClip;
            SpawnClip = spawnClip;
            VictoryClip = victoryClip;
            DefeatClip = defeatClip;
            ShotGain = shotGain;
            EnemyHitGain = enemyHitGain;
            EnemyDeathGain = enemyDeathGain;
            PlayerHurtGain = playerHurtGain;
            SpawnGain = spawnGain;
            VictoryGain = victoryGain;
            DefeatGain = defeatGain;
            SfxSourceGain = sfxSourceGain;
            ResultSourceGain = resultSourceGain;
            SpawnEffectPrefab = spawnEffectPrefab;
            HitEffectPrefab = hitEffectPrefab;
            DeathEffectPrefab = deathEffectPrefab;
            FlashDuration = flashDuration;
            EnemyFlashColor = enemyFlashColor;
            EnemyFlashEmission = enemyFlashEmission;
            PlayerFlashColor = playerFlashColor;
            PlayerFlashEmission = playerFlashEmission;
        }

        public AudioClip ShotClip { get; }
        public AudioClip EnemyHitClip { get; }
        public AudioClip EnemyDeathClip { get; }
        public AudioClip PlayerHurtClip { get; }
        public AudioClip SpawnClip { get; }
        public AudioClip VictoryClip { get; }
        public AudioClip DefeatClip { get; }
        public float ShotGain { get; }
        public float EnemyHitGain { get; }
        public float EnemyDeathGain { get; }
        public float PlayerHurtGain { get; }
        public float SpawnGain { get; }
        public float VictoryGain { get; }
        public float DefeatGain { get; }
        public float SfxSourceGain { get; }
        public float ResultSourceGain { get; }
        public ParticleSystem SpawnEffectPrefab { get; }
        public ParticleSystem HitEffectPrefab { get; }
        public ParticleSystem DeathEffectPrefab { get; }
        public float FlashDuration { get; }
        public Color EnemyFlashColor { get; }
        public Color EnemyFlashEmission { get; }
        public Color PlayerFlashColor { get; }
        public Color PlayerFlashEmission { get; }
    }
}
