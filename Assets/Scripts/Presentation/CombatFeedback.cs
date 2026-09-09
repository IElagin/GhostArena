using System;
using System.Collections.Generic;
using UnityEngine;

namespace GhostArena
{
    public sealed class CombatFeedback : MonoBehaviour
    {
        private const float PlayerDeathEffectHeight = 0.4f;
        private const float EnemyEffectHeight = 0.35f;

        [Header("Configuration")]
        [SerializeField] private CombatFeedbackConfig _config;

        [Header("Session")]
        [SerializeField] private Transform _effectRoot;

        [Header("Audio")]
        [SerializeField] private AudioSource _resultAudioSource;
        [SerializeField] private AudioSource[] _sfxVoices;

        private readonly Dictionary<Character, Action> _enemyHealthHandlers =
            new Dictionary<Character, Action>();
        private readonly HashSet<Projectile> _trackedProjectiles = new HashSet<Projectile>();
        private GameMode _gameMode;
        private GameSession _session;
        private Character _player;
        private Health _playerHealth;
        private EntityRegistry<Character> _enemies;
        private Weapon _weapon;
        private CombatFeedbackSettings _settings;
        private CombatAudioPlayer _audioPlayer;
        private CombatEffectsPlayer _effectsPlayer;
        private bool _isViewEnabled;

        public void Bind(GameMode gameMode)
        {
            if (gameMode == null)
            {
                throw new ArgumentNullException(nameof(gameMode));
            }

            if (ReferenceEquals(_gameMode, gameMode))
            {
                return;
            }

            DetachGameMode();
            _gameMode = gameMode;

            if (_isViewEnabled)
            {
                AttachGameMode();
            }
        }

        public void Unbind(GameMode gameMode)
        {
            if (ReferenceEquals(_gameMode, gameMode) == false)
            {
                return;
            }

            DetachGameMode();
            _gameMode = null;
        }

        private void OnEnable()
        {
            ValidateReferences();
            _audioPlayer = new CombatAudioPlayer(_resultAudioSource, _sfxVoices);
            _effectsPlayer = new CombatEffectsPlayer(this, _effectRoot);
            _isViewEnabled = true;
            AttachGameMode();
        }

        private void OnDisable()
        {
            _isViewEnabled = false;
            DetachGameMode();
            ClearTransientFeedback();
            _audioPlayer = null;
            _effectsPlayer = null;
        }

        private void Update()
        {
            if (_session == null || _session.State != GameState.Finished)
            {
                return;
            }

            _effectsPlayer.TickTerminal(Time.unscaledDeltaTime);
        }

        private void ValidateReferences()
        {
            if (_config == null)
            {
                throw new InvalidOperationException("Combat feedback config is not assigned.");
            }

            if (_effectRoot == null || _resultAudioSource == null)
            {
                throw new InvalidOperationException("Combat feedback scene references are not configured.");
            }

            if (_sfxVoices == null || _sfxVoices.Length == 0)
            {
                throw new InvalidOperationException("Combat feedback needs at least one SFX voice.");
            }

            foreach (AudioSource sfxVoice in _sfxVoices)
            {
                if (sfxVoice == null)
                {
                    throw new InvalidOperationException("Combat feedback SFX voices cannot contain null.");
                }
            }
        }

        private void RebindSession()
        {
            UnbindSession();
            ClearTransientFeedback();
            MatchRuntime current = _gameMode == null ? null : _gameMode.Current;

            if (current == null)
            {
                return;
            }

            _settings = _config.CreateSettings();
            _audioPlayer.Configure(_settings.SfxSourceGain, _settings.ResultSourceGain);
            _session = current.Session;
            _player = current.Player;
            _playerHealth = _player.Health;
            _enemies = current.Enemies;
            _weapon = current.Weapon;
            _session.StateChanged += OnSessionStateChanged;
            _session.Ended += OnSessionEnded;
            _playerHealth.Changed += OnPlayerHealthChanged;
            _playerHealth.Died += OnPlayerDied;
            _enemies.Added += OnEnemyAdded;
            _enemies.Removed += OnEnemyRemoved;
            _weapon.Shot += OnShot;

            foreach (Character enemy in _enemies.Items)
            {
                SubscribeEnemy(enemy);
            }

            _audioPlayer.SetPaused(_session.State == GameState.Paused);
        }

        private void UnbindSession()
        {
            if (_session != null)
            {
                _session.StateChanged -= OnSessionStateChanged;
                _session.Ended -= OnSessionEnded;
            }

            if (_playerHealth != null)
            {
                _playerHealth.Changed -= OnPlayerHealthChanged;
                _playerHealth.Died -= OnPlayerDied;
            }

            if (_enemies != null)
            {
                _enemies.Added -= OnEnemyAdded;
                _enemies.Removed -= OnEnemyRemoved;
            }

            if (_weapon != null)
            {
                _weapon.Shot -= OnShot;
            }

            Character[] subscribedEnemies = new Character[_enemyHealthHandlers.Count];
            _enemyHealthHandlers.Keys.CopyTo(subscribedEnemies, 0);

            foreach (Character enemy in subscribedEnemies)
            {
                UnsubscribeEnemy(enemy);
            }

            foreach (Projectile projectile in _trackedProjectiles)
            {
                if (projectile != null)
                {
                    projectile.Hit -= OnProjectileHit;
                }
            }

            _trackedProjectiles.Clear();
            _session = null;
            _player = null;
            _playerHealth = null;
            _enemies = null;
            _weapon = null;
        }

        private void SubscribeEnemy(Character enemy)
        {
            if (enemy == null || _enemyHealthHandlers.ContainsKey(enemy))
            {
                return;
            }

            Action healthHandler = () => OnEnemyHealthChanged(enemy);
            _enemyHealthHandlers.Add(enemy, healthHandler);
            enemy.Health.Changed += healthHandler;
            enemy.Died += OnEnemyDied;
        }

        private void UnsubscribeEnemy(Character enemy)
        {
            if (ReferenceEquals(enemy, null)
                || _enemyHealthHandlers.TryGetValue(enemy, out Action healthHandler) == false)
            {
                return;
            }

            if (enemy != null && enemy.Health != null)
            {
                enemy.Health.Changed -= healthHandler;
                enemy.Died -= OnEnemyDied;
            }

            _enemyHealthHandlers.Remove(enemy);
        }

        private void ClearTransientFeedback()
        {
            _effectsPlayer?.Clear();
            _audioPlayer?.Clear();
        }

        private void OnMatchChanged()
        {
            RebindSession();
        }

        private void OnSessionStateChanged()
        {
            _audioPlayer.SetPaused(_session != null && _session.State == GameState.Paused);
        }

        private void OnSessionEnded(GameResult result)
        {
            _audioPlayer.SetPaused(false);
            _effectsPlayer.RestoreFlashes();
            AudioClip resultClip = result == GameResult.Victory
                ? _settings.VictoryClip
                : _settings.DefeatClip;
            float resultGain = result == GameResult.Victory
                ? _settings.VictoryGain
                : _settings.DefeatGain;
            _audioPlayer.PlayResult(resultClip, resultGain);
        }

        private void OnPlayerHealthChanged()
        {
            _audioPlayer.Play(_settings.PlayerHurtClip, _settings.PlayerHurtGain);
            _effectsPlayer.Flash(
                _player.gameObject,
                _settings.PlayerFlashColor,
                _settings.PlayerFlashEmission,
                _settings.FlashDuration);
        }

        private void OnPlayerDied()
        {
            _effectsPlayer.Play(
                _settings.DeathEffectPrefab,
                _player.transform.position + Vector3.up * PlayerDeathEffectHeight);
        }

        private void OnEnemyAdded(Character enemy)
        {
            SubscribeEnemy(enemy);
            _effectsPlayer.Play(
                _settings.SpawnEffectPrefab,
                enemy.transform.position + Vector3.up * EnemyEffectHeight);
            _audioPlayer.Play(_settings.SpawnClip, _settings.SpawnGain);
        }

        private void OnEnemyRemoved(Character enemy)
        {
            UnsubscribeEnemy(enemy);
        }

        private void OnEnemyHealthChanged(Character enemy)
        {
            if (enemy == null || enemy.Health.IsAlive == false)
            {
                return;
            }

            _audioPlayer.Play(_settings.EnemyHitClip, _settings.EnemyHitGain);
            _effectsPlayer.Flash(
                enemy.gameObject,
                _settings.EnemyFlashColor,
                _settings.EnemyFlashEmission,
                _settings.FlashDuration);
        }

        private void OnEnemyDied(Character enemy)
        {
            _effectsPlayer.Play(
                _settings.DeathEffectPrefab,
                enemy.transform.position + Vector3.up * EnemyEffectHeight);
            _audioPlayer.Play(_settings.EnemyDeathClip, _settings.EnemyDeathGain);
        }

        private void OnShot(Projectile projectile)
        {
            _trackedProjectiles.RemoveWhere(trackedProjectile => trackedProjectile == null);
            _audioPlayer.Play(_settings.ShotClip, _settings.ShotGain);

            if (projectile != null && _trackedProjectiles.Add(projectile))
            {
                projectile.Hit += OnProjectileHit;
            }
        }

        private void OnProjectileHit(Projectile projectile, Vector3 hitPosition)
        {
            projectile.Hit -= OnProjectileHit;
            _trackedProjectiles.Remove(projectile);
            _effectsPlayer.Play(_settings.HitEffectPrefab, hitPosition);
        }

        private void AttachGameMode()
        {
            if (_gameMode == null)
            {
                return;
            }

            _gameMode.MatchChanged += OnMatchChanged;
            RebindSession();
        }

        private void DetachGameMode()
        {
            if (_gameMode != null)
            {
                _gameMode.MatchChanged -= OnMatchChanged;
            }

            UnbindSession();
        }
    }
}
