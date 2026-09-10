using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class GameMode : IDisposable
    {
        private readonly MatchFactory _matchFactory;
        private Controller _inputController;
        private bool _isDisposed;

        public GameMode(MatchFactory matchFactory)
        {
            _matchFactory = matchFactory ?? throw new ArgumentNullException(nameof(matchFactory));
        }

        public event Action MatchChanged;

        public MatchRuntime Current { get; private set; }

        public void BindInputController(Controller inputController)
        {
            if (_inputController != null)
            {
                throw new InvalidOperationException("Game mode input controller is already bound.");
            }

            _inputController = inputController ?? throw new ArgumentNullException(nameof(inputController));
            _inputController.Enable();
        }

        public void Start()
        {
            Restart();
        }

        public void Restart()
        {
            ThrowIfDisposed();
            TearDownMatch();
            MatchChanged?.Invoke();
            Time.timeScale = 1f;
            GameplaySettings settings = _matchFactory.CaptureConfiguration();
            Current = _matchFactory.Create(settings);
            Current.Session.StateChanged += OnSessionStateChanged;
            Current.Session.Start();
            MatchChanged?.Invoke();
        }

        public void RestartIfAllowed()
        {
            bool canRestart = Current == null
                || Current.Session.State == GameState.Finished
                || Current.Player.Health.IsAlive == false;

            if (canRestart)
            {
                Restart();
            }
        }

        public void TogglePause()
        {
            if (Current == null)
            {
                return;
            }

            switch (Current.Session.State)
            {
                case GameState.Running:
                    Current.Session.Pause();
                    break;

                case GameState.Paused:
                    Current.Session.Resume();
                    break;
            }
        }

        public void Tick(float deltaTime)
        {
            _inputController?.Tick(deltaTime);

            if (Current == null || Current.Session.State != GameState.Running)
            {
                return;
            }

            Current.Controllers.Tick(deltaTime);
            Current.Spawner.Tick(deltaTime);
            Current.Session.Tick(deltaTime);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            if (Current == null || Current.Session.State != GameState.Running)
            {
                return;
            }

            Current.Controllers.FixedTick(fixedDeltaTime);
        }

        public void LateTick(float deltaTime)
        {
            if (Current == null || Current.Session.State != GameState.Running)
            {
                return;
            }

            Current.Controllers.LateTick(deltaTime);
            Current.Session.ResolveConditions();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            Time.timeScale = 1f;
            _inputController?.Dispose();
            TearDownMatch();
            MatchChanged = null;
        }

        private void TearDownMatch()
        {
            if (Current == null)
            {
                return;
            }

            Current.Session.StateChanged -= OnSessionStateChanged;
            Current.Dispose();
            Current = null;
        }

        private void OnSessionStateChanged()
        {
            if (Current == null)
            {
                return;
            }

            bool isRunning = Current.Session.State == GameState.Running;
            Time.timeScale = isRunning ? 1f : 0f;
            Current.SetGameplayActive(isRunning);
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(GameMode));
            }
        }
    }
}
