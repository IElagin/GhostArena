using System;

namespace GhostArena
{
    public sealed class KillEnemiesCondition : IGameCondition
    {
        private readonly SessionStats _stats;
        private readonly int _target;
        private bool _isStarted;
        private bool _isDisposed;

        public KillEnemiesCondition(SessionStats stats, int target)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));

            if (target <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(target));
            }

            _target = target;
        }

        public event Action Satisfied;

        public bool IsSatisfied { get; private set; }

        public void Start()
        {
            ThrowIfDisposed();

            if (_isStarted)
            {
                return;
            }

            _isStarted = true;
            _stats.Changed += Evaluate;
            Evaluate();
        }

        public void Tick(float deltaTime)
        {
            if (_isStarted == false || _isDisposed)
            {
                return;
            }

            Evaluate();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (_isStarted)
            {
                _stats.Changed -= Evaluate;
            }
        }

        private void Evaluate()
        {
            if (IsSatisfied || _stats.Kills < _target)
            {
                return;
            }

            IsSatisfied = true;
            Satisfied?.Invoke();
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(KillEnemiesCondition));
            }
        }
    }
}
