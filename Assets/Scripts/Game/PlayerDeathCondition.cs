using System;

namespace GhostArena
{
    public sealed class PlayerDeathCondition : IGameCondition
    {
        private readonly Health _health;
        private bool _isStarted;
        private bool _isDisposed;

        public PlayerDeathCondition(Health health)
        {
            _health = health ?? throw new ArgumentNullException(nameof(health));
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
            _health.Died += Evaluate;
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
                _health.Died -= Evaluate;
            }
        }

        private void Evaluate()
        {
            if (IsSatisfied || _health.IsAlive)
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
                throw new ObjectDisposedException(nameof(PlayerDeathCondition));
            }
        }
    }
}
