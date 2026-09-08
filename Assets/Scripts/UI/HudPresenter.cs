using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GhostArena
{
    public sealed class HudPresenter : MonoBehaviour
    {
        private const int HealthDotCount = 3;

        [SerializeField] private GameBootstrap _bootstrap;
        [SerializeField] private UIDocument _document;

        private readonly VisualElement[] _healthDots = new VisualElement[HealthDotCount];
        private GameSession _session;
        private SessionStats _stats;
        private ActorHealth _playerHealth;
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

        public GameSession BoundSession => _session;

        private void OnEnable()
        {
            if (_bootstrap == null || _document == null)
            {
                throw new InvalidOperationException("HUD references are not configured.");
            }

            BindElements();
            SubscribeButtons();
            _bootstrap.SessionChanged += OnSessionChanged;
            RebindSession();
        }

        private void OnDisable()
        {
            if (_bootstrap != null)
            {
                _bootstrap.SessionChanged -= OnSessionChanged;
            }

            UnsubscribeButtons();
            UnbindSession();
        }

        private void BindElements()
        {
            VisualElement root = _document.rootVisualElement;
            _healthDots[0] = RequireElement<VisualElement>(root, "hp-dot-1");
            _healthDots[1] = RequireElement<VisualElement>(root, "hp-dot-2");
            _healthDots[2] = RequireElement<VisualElement>(root, "hp-dot-3");
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
            _session = _bootstrap.Session;

            if (_session == null || _bootstrap.Player == null)
            {
                HideAllCards();
                return;
            }

            _stats = _session.Stats;
            _playerHealth = _bootstrap.Player.Health;
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
            _objectiveLabel.text = _bootstrap.WinRule == WinRule.SurviveTime
                ? "ПРОДЕРЖИТЕСЬ " + Mathf.CeilToInt(_bootstrap.SurviveDuration) + " СЕК"
                : "ПОБЕДИТЕ " + _bootstrap.KillTarget + " ДУХОВ";
        }

        private void RefreshHealth()
        {
            int currentHealth = _playerHealth == null ? 0 : _playerHealth.Current;

            for (int index = 0; index < _healthDots.Length; index++)
            {
                _healthDots[index].EnableInClassList("hp-dot--empty", index >= currentHealth);
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
            bool showSpawns = _bootstrap.LoseRule == LoseRule.TotalSpawnsExceeded;
            _spawnsLabel.text = "ПОЯВИЛОСЬ  " + _stats.TotalSpawned
                + "    ПРЕДЕЛ  " + _bootstrap.TotalSpawnsLimit;
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
                && _bootstrap.LoseRule == LoseRule.TotalSpawnsExceeded
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
            if (_bootstrap.WinRule == WinRule.SurviveTime)
            {
                int duration = Mathf.CeilToInt(_bootstrap.SurviveDuration);
                return "Вы продержались " + duration + " секунд.";
            }

            return "Побеждено " + _bootstrap.KillTarget + " духов.";
        }

        private string GetDefeatReason()
        {
            if (_bootstrap.LoseRule == LoseRule.PlayerDeath)
            {
                return "Герой погас в призрачной тьме.";
            }

            int losingSpawn = _bootstrap.TotalSpawnsLimit + 1;
            return "Появился " + losingSpawn + "-й дух — предел "
                + _bootstrap.TotalSpawnsLimit + " превышен.";
        }

        private void RefreshFallenReason()
        {
            int losingSpawn = _bootstrap.TotalSpawnsLimit + 1;

            if (_bootstrap.WinRule == WinRule.KillEnemies)
            {
                _fallenReasonLabel.text = "Матч продолжается: победа — "
                    + _bootstrap.KillTarget + " духов, поражение — появление "
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

        private void OnSessionChanged()
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
            _bootstrap.TogglePause();
        }

        private void OnResumeClicked()
        {
            _resumeButton.Blur();
            _bootstrap.TogglePause();
        }

        private void OnRestartClicked()
        {
            ClearButtonFocus();
            _bootstrap.Restart();
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
