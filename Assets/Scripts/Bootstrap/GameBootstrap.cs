using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        private const int TargetFrameRate = 60;

        [Header("Configuration")]
        [SerializeField] private GameConfig _config;

        [Header("Scene")]
        [SerializeField] private ArenaScene _arenaScene;
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
                _arenaScene,
                controllersFactory);
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
                throw new InvalidOperationException("Game config is not assigned.");
            }

            if (_arenaScene == null || _gameLoop == null || _hudPresenter == null || _combatFeedback == null)
            {
                throw new InvalidOperationException("Gameplay scene references are not configured.");
            }

            _config.Validate();
            _arenaScene.Validate();
        }
    }
}
