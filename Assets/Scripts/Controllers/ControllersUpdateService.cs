using System;
using System.Collections.Generic;

namespace GhostArena
{
    public sealed class ControllersUpdateService : IDisposable
    {
        private readonly List<Controller> _controllers = new List<Controller>();
        private readonly HashSet<Controller> _registrations = new HashSet<Controller>();
        private bool _isDisposed;

        public int Count => _registrations.Count;

        public bool Add(Controller controller)
        {
            ThrowIfDisposed();

            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            if (_registrations.Add(controller) == false)
            {
                return false;
            }

            if (_controllers.Contains(controller) == false)
            {
                _controllers.Add(controller);
            }

            return true;
        }

        public bool Remove(Controller controller)
        {
            if (controller == null || _registrations.Remove(controller) == false)
            {
                return false;
            }

            controller.Disable();
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_isDisposed)
            {
                return;
            }

            for (int index = 0; index < _controllers.Count; index++)
            {
                Controller controller = _controllers[index];

                if (_registrations.Contains(controller))
                {
                    controller.Tick(deltaTime);
                }
            }

            RemoveUnregisteredControllers();
        }

        public void FixedTick(float fixedDeltaTime)
        {
            if (_isDisposed)
            {
                return;
            }

            for (int index = 0; index < _controllers.Count; index++)
            {
                Controller controller = _controllers[index];

                if (_registrations.Contains(controller))
                {
                    controller.FixedTick(fixedDeltaTime);
                }
            }

            RemoveUnregisteredControllers();
        }

        public void LateTick(float deltaTime)
        {
            if (_isDisposed)
            {
                return;
            }

            for (int index = 0; index < _controllers.Count; index++)
            {
                Controller controller = _controllers[index];

                if (_registrations.Contains(controller))
                {
                    controller.LateTick(deltaTime);
                }
            }

            RemoveUnregisteredControllers();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            foreach (Controller controller in _registrations)
            {
                controller.Disable();
            }

            _registrations.Clear();
            _controllers.Clear();
        }

        private void RemoveUnregisteredControllers()
        {
            for (int index = _controllers.Count - 1; index >= 0; index--)
            {
                if (_registrations.Contains(_controllers[index]) == false)
                {
                    _controllers.RemoveAt(index);
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ControllersUpdateService));
            }
        }
    }
}
