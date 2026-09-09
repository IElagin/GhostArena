using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GhostArena
{
    public sealed class HudPresenter : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private readonly List<VisualElement> _healthDots = new List<VisualElement>();
        private GameMode _gameMode;
        private GameplaySettings _settings;
        private GameSession _session;
        private SessionStats _stats;
        private Health _playerHealth;
        private bool _isViewEnabled;
        private VisualElement _healthRow;
        private VisualElement _pauseOverlay;
        private VisualElement _resultOverlay;
        private VisualElement _fallenCard;
        private Label _objectiveLabel;
        private Label _timeLabel;
        private Label _killsLabel;
        private Label _spawnsLabel;
        private Label _pauseStatsLabel;
        private Label _resultHeadingLabel;
        private Label _resultReasonLabel;
        private Label _resultStatsLabel;
        private Label _fallenReasonLabel;
        private Button _pauseButton;
        private Button _resumeButton;
        private Button _pauseRestartButton;
        private Button _resultRestartButton;
        private Button _fallenRestartButton;

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
            if (_document == null)
            {
                throw new InvalidOperationException("HUD references are not configured.");
            }

            _isViewEnabled = true;
            BindElements();
            SubscribeButtons();
            AttachGameMode();
        }

        private void OnDisable()
        {
            _isViewEnabled = false;
            DetachGameMode();
            UnsubscribeButtons();
        }

        private void BindElements()
        {
            VisualElement root = _document.rootVisualElement;
            _healthRow = RequireElement<VisualElement>(root, "health-row");
            _pauseOverlay = RequireElement<VisualElement>(root, "pause-overlay");
            _resultOverlay = RequireElement<VisualElement>(root, "result-overlay");
            _fallenCard = RequireElement<VisualElement>(root, "fallen-card");
            _objectiveLabel = RequireElement<Label>(root, "objective-label");
            _timeLabel = RequireElement<Label>(root, "time-label");
            _killsLabel = RequireElement<Label>(root, "kills-label");
            _spawnsLabel = RequireElement<Label>(root, "spawns-label");
            _pauseStatsLabel = RequireElement<Label>(root, "pause-stats-label");
            _resultHeadingLabel = RequireElement<Label>(root, "result-heading-label");
            _resultReasonLabel = RequireElement<Label>(root, "result-reason-label");
            _resultStatsLabel = RequireElement<Label>(root, "result-stats-label");
            _fallenReasonLabel = RequireElement<Label>(root, "fallen-reason-label");
            _pauseButton = RequireElement<Button>(root, "pause-button");
            _resumeButton = RequireElement<Button>(root, "resume-button");
            _pauseRestartButton = RequireElement<Button>(root, "pause-restart-button");
            _resultRestartButton = RequireElement<Button>(root, "result-restart-button");
            _fallenRestartButton = RequireElement<Button>(root, "fallen-restart-button");
        }

        private void SubscribeButtons()
        {
            _pauseButton.clicked += OnPauseClicked;
            _resumeButton.clicked += OnResumeClicked;
            _pauseRestartButton.clicked += OnRestartClicked;
            _resultRestartButton.clicked += OnRestartClicked;
            _fallenRestartButton.clicked += OnRestartClicked;
        }

        private void UnsubscribeButtons()
        {
            if (_pauseButton == null)
            {
                return;
            }

            _pauseButton.clicked -= OnPauseClicked;
            _resumeButton.clicked -= OnResumeClicked;
            _pauseRestartButton.clicked -= OnRestartClicked;
            _resultRestartButton.clicked -= OnRestartClicked;
            _fallenRestartButton.clicked -= OnRestartClicked;
        }

        private void RebindSession()
        {
            UnbindSession();
            MatchRuntime current = _gameMode == null ? null : _gameMode.Current;

            if (current == null)
            {
                HideAllCards();
                return;
            }

            _settings = current.Settings;
            _session = current.Session;
            _stats = _session.Stats;
            _playerHealth = current.Player.Health;
            RebuildHealthDots();
            _session.StateChanged += OnSessionStateChanged;
            _stats.Changed += OnStatsChanged;
            _playerHealth.Changed += OnHealthChanged;
            RefreshAll();
        }

        private void UnbindSession()
        {
            if (_session != null)
            {
                _session.StateChanged -= OnSessionStateChanged;
            }

            if (_stats != null)
            {
                _stats.Changed -= OnStatsChanged;
            }

            if (_playerHealth != null)
            {
                _playerHealth.Changed -= OnHealthChanged;
            }

            _session = null;
            _stats = null;
            _playerHealth = null;
            _settings = default;
        }

        private void RefreshAll()
        {
            RefreshObjective();
            RefreshHealth();
            RefreshStats();
            RefreshCards();
        }

        private void RefreshObjective()
        {
            _objectiveLabel.text = _settings.WinRule == WinRule.SurviveTime
                ? "ПРОДЕРЖИТЕСЬ " + Mathf.CeilToInt(_settings.SurviveDuration) + " СЕК"
                : "ПОБЕДИТЕ " + _settings.KillTarget + " ДУХОВ";
        }

        private void RefreshHealth()
        {
            int currentHealth = _playerHealth == null ? 0 : _playerHealth.Current;

            for (int index = 0; index < _healthDots.Count; index++)
            {
                _healthDots[index].EnableInClassList("hp-dot--empty", index >= currentHealth);
            }
        }

        private void RebuildHealthDots()
        {
            foreach (VisualElement healthDot in _healthDots)
            {
                healthDot.RemoveFromHierarchy();
            }

            _healthDots.Clear();

            for (int index = 0; index < _playerHealth.Maximum; index++)
            {
                VisualElement healthDot = new VisualElement
                {
                    name = "hp-dot-" + (index + 1),
                    pickingMode = PickingMode.Ignore
                };
                healthDot.AddToClassList("hp-dot");
                _healthRow.Add(healthDot);
                _healthDots.Add(healthDot);
            }
        }

        private void RefreshStats()
        {
            if (_stats == null)
            {
                return;
            }

            string formattedTime = FormatTime(_stats.Elapsed);
            _timeLabel.text = "ВРЕМЯ  " + formattedTime;
            _killsLabel.text = "ДУХИ  " + _stats.Kills;
            bool showSpawns = _settings.LoseRule == LoseRule.TotalSpawnsExceeded;
            _spawnsLabel.text = "ПОЯВИЛОСЬ  " + _stats.TotalSpawned
                + "    ПРЕДЕЛ  " + _settings.TotalSpawnsLimit;
            SetVisible(_spawnsLabel, showSpawns);
            string summary = "Время " + formattedTime + "   •   Духов побеждено " + _stats.Kills;
            _pauseStatsLabel.text = summary;
            _resultStatsLabel.text = summary;
        }

        private void RefreshCards()
        {
            if (_session == null)
            {
                HideAllCards();
                return;
            }

            bool isPaused = _session.State == GameState.Paused;
            bool isFinished = _session.State == GameState.Finished;
            bool isFallenRunning = _session.State == GameState.Running
                && _settings.LoseRule == LoseRule.TotalSpawnsExceeded
                && _playerHealth.IsAlive == false;
            SetVisible(_pauseOverlay, isPaused);
            SetVisible(_resultOverlay, isFinished);
            SetVisible(_fallenCard, isFallenRunning);

            if (isPaused)
            {
                _resumeButton.Focus();
            }
            else if (isFinished)
            {
                RefreshResult();
                _resultRestartButton.Focus();
            }
            else if (isFallenRunning)
            {
                RefreshFallenReason();
                _fallenRestartButton.Focus();
            }
            else
            {
                ClearButtonFocus();
            }
        }

        private void RefreshResult()
        {
            bool isVictory = _session.Result == GameResult.Victory;
            _resultHeadingLabel.text = isVictory ? "ПОБЕДА" : "ПОРАЖЕНИЕ";
            _resultHeadingLabel.EnableInClassList("result-heading--victory", isVictory);
            _resultReasonLabel.text = isVictory ? GetVictoryReason() : GetDefeatReason();
        }

        private string GetVictoryReason()
        {
            if (_settings.WinRule == WinRule.SurviveTime)
            {
                int duration = Mathf.CeilToInt(_settings.SurviveDuration);
                return "Вы продержались " + duration + " секунд.";
            }

            return "Побеждено " + _settings.KillTarget + " духов.";
        }

        private string GetDefeatReason()
        {
            if (_settings.LoseRule == LoseRule.PlayerDeath)
            {
                return "Герой погас в призрачной тьме.";
            }

            int losingSpawn = _settings.TotalSpawnsLimit + 1;
            return "Появился " + losingSpawn + "-й дух — предел "
                + _settings.TotalSpawnsLimit + " превышен.";
        }

        private void RefreshFallenReason()
        {
            int losingSpawn = _settings.TotalSpawnsLimit + 1;

            if (_settings.WinRule == WinRule.KillEnemies)
            {
                _fallenReasonLabel.text = "Матч продолжается: победа — "
                    + _settings.KillTarget + " духов, поражение — появление "
                    + losingSpawn + "-го духа.";
                return;
            }

            _fallenReasonLabel.text = "Матч продолжается до появления "
                + losingSpawn + "-го духа.";
        }

        private void HideAllCards()
        {
            SetVisible(_pauseOverlay, false);
            SetVisible(_resultOverlay, false);
            SetVisible(_fallenCard, false);
        }

        private void ClearButtonFocus()
        {
            _pauseButton.Blur();
            _resumeButton.Blur();
            _pauseRestartButton.Blur();
            _resultRestartButton.Blur();
            _fallenRestartButton.Blur();
        }

        private void OnMatchChanged()
        {
            ClearButtonFocus();
            RebindSession();
        }

        private void OnSessionStateChanged()
        {
            RefreshStats();
            RefreshCards();
        }

        private void OnStatsChanged()
        {
            RefreshStats();
        }

        private void OnHealthChanged()
        {
            RefreshHealth();
            RefreshCards();
        }

        private void OnPauseClicked()
        {
            _pauseButton.Blur();
            _gameMode.TogglePause();
        }

        private void OnResumeClicked()
        {
            _resumeButton.Blur();
            _gameMode.TogglePause();
        }

        private void OnRestartClicked()
        {
            ClearButtonFocus();
            _gameMode.Restart();
        }

        private void AttachGameMode()
        {
            if (_gameMode == null)
            {
                HideAllCards();
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

        private static T RequireElement<T>(VisualElement root, string elementName)
            where T : VisualElement
        {
            T element = root.Q<T>(elementName);

            if (element == null)
            {
                throw new InvalidOperationException("HUD element is missing: " + elementName);
            }

            return element;
        }

        private static void SetVisible(VisualElement element, bool isVisible)
        {
            if (element != null)
            {
                element.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private static string FormatTime(float elapsed)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(elapsed));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return minutes.ToString("00") + ":" + seconds.ToString("00");
        }
    }
}
