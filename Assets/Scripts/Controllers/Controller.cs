using System;

namespace GhostArena
{
    public abstract class Controller : IDisposable
    {
        private bool _isDisposed;

        public bool IsEnabled { get; private set; }

        protected bool IsDisposed => _isDisposed;

        public virtual void Enable()
        {
            ThrowIfDisposed();
            IsEnabled = true;
        }

        public virtual void Disable()
        {
            IsEnabled = false;
        }

        public void Tick(float deltaTime)
        {
            if (IsEnabled == false || _isDisposed)
            {
                return;
            }

            OnTick(deltaTime);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            if (IsEnabled == false || _isDisposed)
            {
                return;
            }

            OnFixedTick(fixedDeltaTime);
        }

        public void LateTick(float deltaTime)
        {
            if (IsEnabled == false || _isDisposed)
            {
                return;
            }

            OnLateTick(deltaTime);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            Disable();
            OnDispose();
            _isDisposed = true;
        }

        protected virtual void OnTick(float deltaTime)
        {
        }

        protected virtual void OnFixedTick(float fixedDeltaTime)
        {
        }

        protected virtual void OnLateTick(float deltaTime)
        {
        }

        protected virtual void OnDispose()
        {
        }

        protected void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
