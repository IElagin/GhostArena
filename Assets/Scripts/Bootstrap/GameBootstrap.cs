using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GhostArena
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        private const string PressOnlyInteraction = "Press(behavior=0)";
        private const int TargetFrameRate = 60;

        [Header("Configuration")]
        [SerializeField] private GameplayConfig _config;

        [Header("Prefabs")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private GameObject _enemyPrefab;
        [SerializeField] private GameObject _projectilePrefab;

        [Header("Scene")]
        [SerializeField] private Transform _playerSpawn;
        [SerializeField] private Transform[] _enemySpawns;
        [SerializeField] private Camera _gameplayCamera;
        [SerializeField] private Vector2 _arenaHalfExtents = new Vector2(8.3f, 6.3f);

        private Transform _runtimeRoot;
        private InputAction _pauseAction;
        private InputAction _restartAction;
        private GameplaySettings _settings;
        private bool _isTearingDown;

        public event Action SessionChanged;

        public GameSession Session { get; private set; }

        public PlayerController Player { get; private set; }

        public EntityRegistry<EnemyController> Enemies { get; private set; }

        public EnemySpawner Spawner { get; private set; }

        public PlayerShooter Shooter { get; private set; }

        public GameplayConfig Config => _config;

        public GameplaySettings Settings => _settings;

        public WinRule WinRule => _settings.WinRule;

        public LoseRule LoseRule => _settings.LoseRule;

        public float SurviveDuration => _settings.SurviveDuration;

        public int KillTarget => _settings.KillTarget;

        public int TotalSpawnsLimit => _settings.TotalSpawnsLimit;

        public void Restart()
        {
            StartSession();
        }

        public void TogglePause()
        {
            if (Session == null)
            {
                return;
            }

            switch (Session.State)
            {
                case GameState.Running:
                    Session.Pause();
                    break;

                case GameState.Paused:
                    Session.Resume();
                    break;
            }
        }

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
            CaptureSessionConfiguration();
            CreateGlobalInputActions();
        }

        private void Start()
        {
            StartSession();
        }

        private void Update()
        {
            if (Session == null || Session.State != GameState.Running)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            Spawner.Tick(deltaTime);
            Session.Tick(deltaTime);
        }

        private void LateUpdate()
        {
            Session?.ResolveConditions();
        }

        private void OnDestroy()
        {
            TearDownSession();
            DisposeGlobalInputActions();
        }

        private void CreateGlobalInputActions()
        {
            _pauseAction = new InputAction(
                "Pause",
                InputActionType.Button,
                "<Keyboard>/escape",
                PressOnlyInteraction);
            _restartAction = new InputAction(
                "Restart",
                InputActionType.Button,
                "<Keyboard>/r",
                PressOnlyInteraction);
            _pauseAction.performed += OnPausePerformed;
            _restartAction.performed += OnRestartPerformed;
            _pauseAction.Enable();
            _restartAction.Enable();
        }

        private void DisposeGlobalInputActions()
        {
            if (_pauseAction != null)
            {
                _pauseAction.performed -= OnPausePerformed;
                _pauseAction.Disable();
                _pauseAction.Dispose();
            }

            if (_restartAction != null)
            {
                _restartAction.performed -= OnRestartPerformed;
                _restartAction.Disable();
                _restartAction.Dispose();
            }
        }

        private void StartSession()
        {
            SessionConfiguration configuration = CaptureSessionConfiguration();
            TearDownSession();
            Time.timeScale = 1f;
            _settings = configuration.Gameplay;

            _runtimeRoot = new GameObject("Session Runtime").transform;

            GameObject playerObject = Instantiate(
                _playerPrefab,
                _playerSpawn.position,
                _playerSpawn.rotation,
                _runtimeRoot);
            Player = playerObject.GetComponent<PlayerController>();
            Shooter = playerObject.GetComponent<PlayerShooter>();

            if (Player == null || Shooter == null || Player.Health == null)
            {
                throw new InvalidOperationException("Player prefab is missing gameplay components.");
            }

            Player.Health.Initialize(_settings.PlayerMaximumHealth);
            Player.Initialize(_gameplayCamera, _settings.PlayerMovementSpeed);
            playerObject.GetComponentInChildren<GhostVisual>(true).Initialize(configuration.PlayerVisual);

            Enemies = new EntityRegistry<EnemyController>();
            SessionStats stats = new SessionStats();
            IGameCondition winCondition = ConditionFactory.CreateWin(
                _settings.WinRule,
                stats,
                Player.Health.Model,
                _settings.SurviveDuration,
                _settings.KillTarget);
            IGameCondition loseCondition = ConditionFactory.CreateLose(
                _settings.LoseRule,
                stats,
                Player.Health.Model,
                _settings.TotalSpawnsLimit);
            Session = new GameSession(stats, winCondition, loseCondition);
            Session.StateChanged += OnSessionStateChanged;
            Player.Health.Died += OnPlayerDied;

            Spawner = new EnemySpawner(
                _enemyPrefab,
                _enemySpawns,
                _runtimeRoot,
                _arenaHalfExtents,
                _settings.SpawnInterval,
                _settings.Enemy,
                configuration.EnemyVisual);
            Spawner.Spawned += OnEnemySpawned;
            Shooter.Initialize(_projectilePrefab, _runtimeRoot, _settings.Projectile);

            Session.Start();
            SessionChanged?.Invoke();
        }

        private void TearDownSession()
        {
            if (_isTearingDown)
            {
                return;
            }

            _isTearingDown = true;
            Time.timeScale = 1f;

            if (Player != null)
            {
                Player.SetGameplayActive(false);
            }

            if (Shooter != null)
            {
                Shooter.SetGameplayActive(false);
            }

            if (_runtimeRoot != null)
            {
                _runtimeRoot.gameObject.SetActive(false);
            }

            if (Spawner != null)
            {
                Spawner.Spawned -= OnEnemySpawned;
                Spawner.Dispose();
            }

            if (Player != null && Player.Health != null)
            {
                Player.Health.Died -= OnPlayerDied;
            }

            if (Enemies != null)
            {
                EnemyController[] enemies = new EnemyController[Enemies.Count];

                for (int index = 0; index < Enemies.Count; index++)
                {
                    enemies[index] = Enemies.Items[index];
                }

                foreach (EnemyController enemy in enemies)
                {
                    enemy.Died -= OnEnemyDied;

                    if (Enemies.Remove(enemy) && Session != null)
                    {
                        Session.Stats.RecordRemoval(false);
                    }
                }
            }

            if (Session != null)
            {
                Session.StateChanged -= OnSessionStateChanged;
                Session.Dispose();
            }

            if (_runtimeRoot != null)
            {
                Destroy(_runtimeRoot.gameObject);
            }

            Session = null;
            Player = null;
            Enemies = null;
            Spawner = null;
            Shooter = null;
            _runtimeRoot = null;
            _isTearingDown = false;
        }

        private void OnEnemySpawned(EnemyController enemy)
        {
            enemy.Died += OnEnemyDied;

            if (Enemies.Add(enemy) == false)
            {
                enemy.Died -= OnEnemyDied;
                Destroy(enemy.gameObject);
                return;
            }

            Session.Stats.RecordSpawn();
            enemy.SetGameplayActive(Session.State == GameState.Running);
        }

        private void OnEnemyDied(EnemyController enemy)
        {
            enemy.Died -= OnEnemyDied;

            if (Enemies.Remove(enemy) == false)
            {
                return;
            }

            Session.Stats.RecordRemoval(true);
            Destroy(enemy.gameObject);
        }

        private void OnPlayerDied()
        {
            Player.SetGameplayActive(false);
            Shooter.SetGameplayActive(false);
        }

        private void OnSessionStateChanged()
        {
            bool isRunning = Session.State == GameState.Running;
            Time.timeScale = isRunning ? 1f : 0f;

            bool canControlPlayer = isRunning && Player.Health.IsAlive;
            Player.SetGameplayActive(canControlPlayer);
            Shooter.SetGameplayActive(canControlPlayer);

            foreach (EnemyController enemy in Enemies.Items)
            {
                enemy.SetGameplayActive(isRunning);
            }
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            TogglePause();
        }

        private void OnRestartPerformed(InputAction.CallbackContext context)
        {
            bool canRestart = Session != null
                && (Session.State == GameState.Finished || Player.Health.IsAlive == false);

            if (canRestart)
            {
                Restart();
            }
        }

        private SessionConfiguration CaptureSessionConfiguration()
        {
            if (_config == null)
            {
                throw new InvalidOperationException("Gameplay config is not assigned.");
            }

            if (_playerPrefab == null || _enemyPrefab == null || _projectilePrefab == null)
            {
                throw new InvalidOperationException("Gameplay prefabs are not configured.");
            }

            if (_playerSpawn == null || _gameplayCamera == null)
            {
                throw new InvalidOperationException("Gameplay scene references are not configured.");
            }

            if (_enemySpawns == null || _enemySpawns.Length == 0)
            {
                throw new InvalidOperationException("Enemy spawn points are not configured.");
            }

            foreach (Transform spawnPoint in _enemySpawns)
            {
                if (spawnPoint == null)
                {
                    throw new InvalidOperationException("Enemy spawn points cannot contain null.");
                }
            }

            if (_arenaHalfExtents.x <= 0f || _arenaHalfExtents.y <= 0f
                || float.IsNaN(_arenaHalfExtents.x) || float.IsInfinity(_arenaHalfExtents.x)
                || float.IsNaN(_arenaHalfExtents.y) || float.IsInfinity(_arenaHalfExtents.y))
            {
                throw new InvalidOperationException("Arena half extents must be finite and positive.");
            }

            if (_playerPrefab.GetComponent<PlayerController>() == null
                || _playerPrefab.GetComponent<PlayerShooter>() == null)
            {
                throw new InvalidOperationException("Player prefab is missing gameplay components.");
            }

            if (_enemyPrefab.GetComponent<EnemyController>() == null
                || _enemyPrefab.GetComponent<ContactDamage>() == null)
            {
                throw new InvalidOperationException("Enemy prefab is missing gameplay components.");
            }

            if (_projectilePrefab.GetComponent<Projectile>() == null)
            {
                throw new InvalidOperationException("Projectile prefab has no Projectile component.");
            }

            GhostVisual playerVisual = _playerPrefab.GetComponentInChildren<GhostVisual>(true);
            GhostVisual enemyVisual = _enemyPrefab.GetComponentInChildren<GhostVisual>(true);

            if (playerVisual == null || enemyVisual == null)
            {
                throw new InvalidOperationException("Character prefabs are missing GhostVisual components.");
            }

            return new SessionConfiguration(
                _config.CreateSettings(),
                playerVisual.CreateSettings(),
                enemyVisual.CreateSettings());
        }

        private readonly struct SessionConfiguration
        {
            public SessionConfiguration(
                GameplaySettings gameplay,
                GhostVisualSettings playerVisual,
                GhostVisualSettings enemyVisual)
            {
                Gameplay = gameplay;
                PlayerVisual = playerVisual;
                EnemyVisual = enemyVisual;
            }

            public GameplaySettings Gameplay { get; }

            public GhostVisualSettings PlayerVisual { get; }

            public GhostVisualSettings EnemyVisual { get; }
        }
    }
}
