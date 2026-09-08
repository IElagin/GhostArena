using System;

namespace GhostArena
{
    public sealed class GameSession : IDisposable
    {
        private readonly IGameCondition _winCondition;
        private readonly IGameCondition _loseCondition;
        private bool _hasPendingResolution;
        private bool _isSubscribed;
        private bool _isDisposed;

        public GameSession(
            SessionStats stats,
            IGameCondition winCondition,
            IGameCondition loseCondition)
        {
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _winCondition = winCondition ?? throw new ArgumentNullException(nameof(winCondition));
            _loseCondition = loseCondition ?? throw new ArgumentNullException(nameof(loseCondition));
            State = GameState.Ready;
        }

        public event Action StateChanged;
        public event Action<GameResult> Ended;

        public SessionStats Stats { get; }

        public GameState State { get; private set; }

        public GameResult? Result { get; private set; }

        public void Start()
        {
            ThrowIfDisposed();

            if (State != GameState.Ready)
            {
                return;
            }

            Subscribe();
            _winCondition.Start();
            _loseCondition.Start();
            SetState(GameState.Running);
        }

        public void Tick(float deltaTime)
        {
            if (State != GameState.Running || _isDisposed)
            {
                return;
            }

            Stats.Advance(deltaTime);
            _winCondition.Tick(deltaTime);
            _loseCondition.Tick(deltaTime);
        }

        public void ResolveConditions()
        {
            if (State != GameState.Running || _isDisposed || _hasPendingResolution == false)
            {
                return;
            }

            _hasPendingResolution = false;

            if (_loseCondition.IsSatisfied)
            {
                Finish(GameResult.Defeat);
                return;
            }

            if (_winCondition.IsSatisfied)
            {
                Finish(GameResult.Victory);
            }
        }

        public void Pause()
        {
            if (State == GameState.Running && _isDisposed == false)
            {
                SetState(GameState.Paused);
            }
        }

        public void Resume()
        {
            if (State == GameState.Paused && _isDisposed == false)
            {
                SetState(GameState.Running);
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            Unsubscribe();
            _winCondition.Dispose();

            if (ReferenceEquals(_winCondition, _loseCondition) == false)
            {
                _loseCondition.Dispose();
            }
        }

        private void Subscribe()
        {
            _winCondition.Satisfied += OnConditionSatisfied;
            _loseCondition.Satisfied += OnConditionSatisfied;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (_isSubscribed == false)
            {
                return;
            }

            _winCondition.Satisfied -= OnConditionSatisfied;
            _loseCondition.Satisfied -= OnConditionSatisfied;
            _isSubscribed = false;
        }

        private void OnConditionSatisfied()
        {
            _hasPendingResolution = true;
        }

        private void Finish(GameResult result)
        {
            if (State == GameState.Finished)
            {
                return;
            }

            Result = result;
            SetState(GameState.Finished);
            Ended?.Invoke(result);
        }

        private void SetState(GameState state)
        {
            State = state;
            StateChanged?.Invoke();
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(GameSession));
            }
        }
    }
}
