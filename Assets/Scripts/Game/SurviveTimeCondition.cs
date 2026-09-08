using System;

namespace GhostArena
{
    public sealed class SurviveTimeCondition : IGameCondition
    {
        private readonly SessionStats _stats;
        private readonly Health _health;
        private readonly float _duration;
        private bool _isStarted;
        private bool _isDisposed;

        public SurviveTimeCondition(SessionStats stats, Health health, float duration)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _health = health ?? throw new ArgumentNullException(nameof(health));

            if (duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration))
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            _duration = duration;
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
            _health.Changed += Evaluate;
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
                _health.Changed -= Evaluate;
            }
        }

        private void Evaluate()
        {
            bool wasSatisfied = IsSatisfied;
            IsSatisfied = _stats.Elapsed >= _duration && _health.IsAlive;

            if (IsSatisfied && wasSatisfied == false)
            {
                Satisfied?.Invoke();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SurviveTimeCondition));
            }
        }
    }
}
