using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhostArena
{
    public sealed class CombatFeedback : MonoBehaviour
    {
        private const float PlayerDeathEffectHeight = 0.4f;
        private const float EnemyEffectHeight = 0.35f;
        private const float ProjectileHitEffectHeight = 0.45f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Header("Configuration")]
        [SerializeField] private CombatFeedbackConfig _config;

        [Header("Session")]
        [SerializeField] private GameBootstrap _bootstrap;
        [SerializeField] private Transform _effectRoot;

        [Header("Audio")]
        [SerializeField] private AudioSource _resultAudioSource;
        [SerializeField] private AudioSource[] _sfxVoices;

        private readonly Dictionary<EnemyController, Action> _enemyHealthHandlers =
            new Dictionary<EnemyController, Action>();
        private readonly HashSet<Projectile> _trackedProjectiles = new HashSet<Projectile>();
        private readonly Dictionary<GameObject, FlashState> _flashes =
            new Dictionary<GameObject, FlashState>();
        private readonly List<ParticleSystem> _activeEffects = new List<ParticleSystem>();
        private GameSession _session;
        private GameSession _settingsSession;
        private ActorHealth _playerHealth;
        private EntityRegistry<EnemyController> _enemies;
        private PlayerShooter _shooter;
        private CombatFeedbackSettings _settings;
        private CombatFeedbackSettings _preparedSettings;
        private bool _hasPreparedSettings;
        private int _nextSfxVoice;

        public GameSession BoundSession => _session;

        public CombatFeedbackConfig Config => _config;

        public CombatFeedbackSettings Settings => _settings;

        private void OnEnable()
        {
            ValidateReferences();
            RebindSession();
            _bootstrap.SessionStarting += OnSessionStarting;
            _bootstrap.SessionChanged += OnSessionChanged;
        }

        private void OnDisable()
        {
            if (_bootstrap != null)
            {
                _bootstrap.SessionStarting -= OnSessionStarting;
                _bootstrap.SessionChanged -= OnSessionChanged;
            }

            _preparedSettings = default;
            _hasPreparedSettings = false;
            UnbindSession();
            ClearTransientFeedback();
            AudioListener.pause = false;
        }

        private void Update()
        {
            if (_session == null || _session.State != GameState.Finished)
            {
                return;
            }

            for (int index = _activeEffects.Count - 1; index >= 0; index--)
            {
                ParticleSystem effect = _activeEffects[index];

                if (effect == null)
                {
                    _activeEffects.RemoveAt(index);
                    continue;
                }

                effect.Simulate(Time.unscaledDeltaTime, true, false, false);

                if (effect.IsAlive(true) == false)
                {
                    effect.gameObject.SetActive(false);
                    Destroy(effect.gameObject);
                    _activeEffects.RemoveAt(index);
                }
            }
        }

        private void ValidateReferences()
        {
            if (_config == null)
            {
                throw new InvalidOperationException("Combat feedback config is not assigned.");
            }

            if (_bootstrap == null || _effectRoot == null || _resultAudioSource == null)
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
            GameSession session = _bootstrap.Session;
            CombatFeedbackSettings settings;

            if (_hasPreparedSettings)
            {
                settings = _preparedSettings;
                _preparedSettings = default;
                _hasPreparedSettings = false;
            }
            else if (session != null && ReferenceEquals(_settingsSession, session))
            {
                settings = _settings;
            }
            else
            {
                settings = _config.CreateSettings();
            }

            UnbindSession();
            ClearTransientFeedback();
            _settings = settings;
            _settingsSession = session;
            ApplySourceGains();
            _session = session;

            if (_session == null || _bootstrap.Player == null || _bootstrap.Enemies == null
                || _bootstrap.Shooter == null)
            {
                return;
            }

            _playerHealth = _bootstrap.Player.Health;
            _enemies = _bootstrap.Enemies;
            _shooter = _bootstrap.Shooter;
            _session.StateChanged += OnSessionStateChanged;
            _session.Ended += OnSessionEnded;
            _playerHealth.Changed += OnPlayerHealthChanged;
            _playerHealth.Died += OnPlayerDied;
            _enemies.Added += OnEnemyAdded;
            _enemies.Removed += OnEnemyRemoved;
            _shooter.Shot += OnShot;

            foreach (EnemyController enemy in _enemies.Items)
            {
                SubscribeEnemy(enemy);
            }

            AudioListener.pause = _session.State == GameState.Paused;
        }

        private void ApplySourceGains()
        {
            _resultAudioSource.volume = _settings.ResultSourceGain;

            foreach (AudioSource sfxVoice in _sfxVoices)
            {
                sfxVoice.volume = _settings.SfxSourceGain;
            }
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

            if (_shooter != null)
            {
                _shooter.Shot -= OnShot;
            }

            EnemyController[] subscribedEnemies = new EnemyController[_enemyHealthHandlers.Count];
            _enemyHealthHandlers.Keys.CopyTo(subscribedEnemies, 0);

            foreach (EnemyController enemy in subscribedEnemies)
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
            _playerHealth = null;
            _enemies = null;
            _shooter = null;
        }

        private void SubscribeEnemy(EnemyController enemy)
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

        private void UnsubscribeEnemy(EnemyController enemy)
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
            StopAllCoroutines();
            RestoreAllFlashes();

            foreach (ParticleSystem effect in _activeEffects)
            {
                if (effect != null)
                {
                    effect.gameObject.SetActive(false);
                    Destroy(effect.gameObject);
                }
            }

            _activeEffects.Clear();
            ClearAudio();
        }

        private void ClearAudio()
        {
            _resultAudioSource.Stop();

            foreach (AudioSource sfxVoice in _sfxVoices)
            {
                sfxVoice.Stop();
            }

            _nextSfxVoice = 0;
        }

        private void RestoreAllFlashes()
        {
            foreach (FlashState flashState in _flashes.Values)
            {
                RestoreFlash(flashState);
            }

            _flashes.Clear();
        }

        private void Flash(GameObject actor, Color flashColor, Color flashEmission)
        {
            if (actor == null)
            {
                return;
            }

            if (_flashes.TryGetValue(actor, out FlashState flashState))
            {
                StopCoroutine(flashState.Coroutine);
            }
            else
            {
                flashState = CaptureFlashState(actor);

                if (flashState.Slots.Count == 0)
                {
                    return;
                }

                _flashes.Add(actor, flashState);
            }

            ApplyFlash(flashState, flashColor, flashEmission);
            flashState.Coroutine = StartCoroutine(RestoreFlashAfterDelay(actor, flashState));
        }

        private static FlashState CaptureFlashState(GameObject actor)
        {
            FlashState flashState = new FlashState();

            foreach (Renderer renderer in actor.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;

                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];

                    if (material == null)
                    {
                        continue;
                    }

                    bool hasBaseColor = material.HasProperty(BaseColorId);
                    bool hasEmission = material.HasProperty(EmissionColorId);

                    if (hasBaseColor == false && hasEmission == false)
                    {
                        continue;
                    }

                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block, materialIndex);
                    Color baseColor = hasBaseColor
                        ? block.HasColor(BaseColorId)
                            ? block.GetColor(BaseColorId)
                            : material.GetColor(BaseColorId)
                        : Color.clear;
                    Color emissionColor = hasEmission
                        ? block.HasColor(EmissionColorId)
                            ? block.GetColor(EmissionColorId)
                            : material.GetColor(EmissionColorId)
                        : Color.clear;
                    flashState.Slots.Add(new MaterialSlotState(
                        renderer,
                        materialIndex,
                        block,
                        hasBaseColor,
                        baseColor,
                        hasEmission,
                        emissionColor));
                }
            }

            return flashState;
        }

        private static void ApplyFlash(
            FlashState flashState,
            Color flashColor,
            Color flashEmission)
        {
            foreach (MaterialSlotState slot in flashState.Slots)
            {
                if (slot.Renderer == null)
                {
                    continue;
                }

                if (slot.HasBaseColor)
                {
                    slot.Block.SetColor(BaseColorId, flashColor);
                }

                if (slot.HasEmission)
                {
                    slot.Block.SetColor(EmissionColorId, flashEmission);
                }

                slot.Renderer.SetPropertyBlock(slot.Block, slot.MaterialIndex);
            }
        }

        private static void RestoreFlash(FlashState flashState)
        {
            foreach (MaterialSlotState slot in flashState.Slots)
            {
                if (slot.Renderer == null)
                {
                    continue;
                }

                if (slot.HasBaseColor)
                {
                    slot.Block.SetColor(BaseColorId, slot.BaseColor);
                }

                if (slot.HasEmission)
                {
                    slot.Block.SetColor(EmissionColorId, slot.EmissionColor);
                }

                slot.Renderer.SetPropertyBlock(slot.Block, slot.MaterialIndex);
            }
        }

        private IEnumerator RestoreFlashAfterDelay(GameObject actor, FlashState flashState)
        {
            float elapsed = 0f;

            while (elapsed < _settings.FlashDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            RestoreFlash(flashState);
            _flashes.Remove(actor);
        }

        private void PlayEffect(ParticleSystem effectPrefab, Vector3 position)
        {
            for (int index = _activeEffects.Count - 1; index >= 0; index--)
            {
                if (_activeEffects[index] == null)
                {
                    _activeEffects.RemoveAt(index);
                }
            }

            ParticleSystem effect = Instantiate(
                effectPrefab,
                position,
                Quaternion.identity,
                _effectRoot);
            _activeEffects.Add(effect);
            effect.Play(true);
            float lifetime = effect.main.duration + effect.main.startLifetime.constantMax;
            Destroy(effect.gameObject, lifetime);
        }

        private void PlayCue(AudioClip clip, float volume)
        {
            AudioSource sfxVoice = _sfxVoices[_nextSfxVoice];
            _nextSfxVoice = (_nextSfxVoice + 1) % _sfxVoices.Length;
            sfxVoice.Stop();
            sfxVoice.PlayOneShot(clip, volume);
        }

        private void OnSessionChanged()
        {
            RebindSession();
        }

        private void OnSessionStarting()
        {
            CombatFeedbackSettings settings = _config.CreateSettings();
            _preparedSettings = settings;
            _hasPreparedSettings = true;
        }

        private void OnSessionStateChanged()
        {
            AudioListener.pause = _session != null && _session.State == GameState.Paused;
        }

        private void OnSessionEnded(GameResult result)
        {
            AudioListener.pause = false;
            StopAllCoroutines();
            RestoreAllFlashes();
            _resultAudioSource.Stop();
            AudioClip resultClip = result == GameResult.Victory
                ? _settings.VictoryClip
                : _settings.DefeatClip;
            float resultGain = result == GameResult.Victory
                ? _settings.VictoryGain
                : _settings.DefeatGain;
            _resultAudioSource.PlayOneShot(resultClip, resultGain);
        }

        private void OnPlayerHealthChanged()
        {
            PlayCue(_settings.PlayerHurtClip, _settings.PlayerHurtGain);
            Flash(
                _bootstrap.Player.gameObject,
                _settings.PlayerFlashColor,
                _settings.PlayerFlashEmission);
        }

        private void OnPlayerDied()
        {
            PlayEffect(
                _settings.DeathEffectPrefab,
                _bootstrap.Player.transform.position + Vector3.up * PlayerDeathEffectHeight);
        }

        private void OnEnemyAdded(EnemyController enemy)
        {
            SubscribeEnemy(enemy);
            PlayEffect(
                _settings.SpawnEffectPrefab,
                enemy.transform.position + Vector3.up * EnemyEffectHeight);
            PlayCue(_settings.SpawnClip, _settings.SpawnGain);
        }

        private void OnEnemyRemoved(EnemyController enemy)
        {
            UnsubscribeEnemy(enemy);
        }

        private void OnEnemyHealthChanged(EnemyController enemy)
        {
            if (enemy == null || enemy.Health.IsAlive == false)
            {
                return;
            }

            PlayCue(_settings.EnemyHitClip, _settings.EnemyHitGain);
            Flash(enemy.gameObject, _settings.EnemyFlashColor, _settings.EnemyFlashEmission);
        }

        private void OnEnemyDied(EnemyController enemy)
        {
            PlayEffect(
                _settings.DeathEffectPrefab,
                enemy.transform.position + Vector3.up * EnemyEffectHeight);
            PlayCue(_settings.EnemyDeathClip, _settings.EnemyDeathGain);
        }

        private void OnShot(Projectile projectile)
        {
            _trackedProjectiles.RemoveWhere(trackedProjectile => trackedProjectile == null);
            PlayCue(_settings.ShotClip, _settings.ShotGain);

            if (projectile != null && _trackedProjectiles.Add(projectile))
            {
                projectile.Hit += OnProjectileHit;
            }
        }

        private void OnProjectileHit(Projectile projectile, EnemyController enemy)
        {
            projectile.Hit -= OnProjectileHit;
            _trackedProjectiles.Remove(projectile);

            if (enemy != null)
            {
                PlayEffect(
                    _settings.HitEffectPrefab,
                    enemy.transform.position + Vector3.up * ProjectileHitEffectHeight);
            }
        }

        private sealed class FlashState
        {
            public readonly List<MaterialSlotState> Slots = new List<MaterialSlotState>();
            public Coroutine Coroutine;
        }

        private sealed class MaterialSlotState
        {
            public MaterialSlotState(
                Renderer renderer,
                int materialIndex,
                MaterialPropertyBlock block,
                bool hasBaseColor,
                Color baseColor,
                bool hasEmission,
                Color emissionColor)
            {
                Renderer = renderer;
                MaterialIndex = materialIndex;
                Block = block;
                HasBaseColor = hasBaseColor;
                BaseColor = baseColor;
                HasEmission = hasEmission;
                EmissionColor = emissionColor;
            }

            public Renderer Renderer { get; }

            public int MaterialIndex { get; }

            public MaterialPropertyBlock Block { get; }

            public bool HasBaseColor { get; }

            public Color BaseColor { get; }

            public bool HasEmission { get; }

            public Color EmissionColor { get; }
        }
    }
}
