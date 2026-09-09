using System;

namespace GhostArena
{
    public sealed class CompositeController : Controller
    {
        private readonly Controller[] _controllers;

        public CompositeController(params Controller[] controllers)
        {
            _controllers = controllers != null
                ? (Controller[])controllers.Clone()
                : throw new ArgumentNullException(nameof(controllers));

            foreach (Controller controller in _controllers)
            {
                if (controller == null)
                {
                    throw new ArgumentException("Controllers cannot contain null.", nameof(controllers));
                }
            }
        }

        public override void Enable()
        {
            base.Enable();

            foreach (Controller controller in _controllers)
            {
                controller.Enable();
            }
        }

        public override void Disable()
        {
            if (IsDisposed)
            {
                return;
            }

            base.Disable();

            foreach (Controller controller in _controllers)
            {
                controller.Disable();
            }
        }

        protected override void OnDispose()
        {
            foreach (Controller controller in _controllers)
            {
                controller.Dispose();
            }
        }

        protected override void OnTick(float deltaTime)
        {
            foreach (Controller controller in _controllers)
            {
                controller.Tick(deltaTime);
            }
        }

        protected override void OnFixedTick(float fixedDeltaTime)
        {
            foreach (Controller controller in _controllers)
            {
                controller.FixedTick(fixedDeltaTime);
            }
        }

        protected override void OnLateTick(float deltaTime)
        {
            foreach (Controller controller in _controllers)
            {
                controller.LateTick(deltaTime);
            }
        }
    }
}
