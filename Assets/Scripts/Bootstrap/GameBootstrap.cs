using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class GameBootstrap : MonoBehaviour
    {
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
        [SerializeField] private GameLoop _gameLoop;
        [SerializeField] private HudPresenter _hudPresenter;
        [SerializeField] private CombatFeedback _combatFeedback;

        private GameMode _gameMode;

        private void Awake()
        {
            ValidateCompositionReferences();
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
            ControllersFactory controllersFactory = new ControllersFactory();
            MatchFactory matchFactory = new MatchFactory(
                _config,
                controllersFactory,
                _playerPrefab,
                _enemyPrefab,
                _projectilePrefab,
                _playerSpawn,
                _enemySpawns,
                _gameplayCamera,
                _arenaHalfExtents);
            _gameMode = new GameMode(matchFactory);
            _gameMode.BindInputController(controllersFactory.CreateGameModeInput(_gameMode));
            _gameLoop.Initialize(_gameMode);
            _hudPresenter.Bind(_gameMode);
            _combatFeedback.Bind(_gameMode);
        }

        private void Start()
        {
            _gameMode.Start();
        }

        private void OnDestroy()
        {
            if (_gameMode == null)
            {
                return;
            }

            _hudPresenter.Unbind(_gameMode);
            _combatFeedback.Unbind(_gameMode);
            _gameMode.Dispose();
            _gameMode = null;
        }

        private void ValidateCompositionReferences()
        {
            if (_config == null)
            {
                throw new InvalidOperationException("Gameplay config is not assigned.");
            }

            if (_playerPrefab == null || _enemyPrefab == null || _projectilePrefab == null)
            {
                throw new InvalidOperationException("Gameplay prefabs are not configured.");
            }

            if (_playerSpawn == null || _gameplayCamera == null || _gameLoop == null
                || _hudPresenter == null || _combatFeedback == null)
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
        }
    }
}
