using System;

namespace GhostArena
{
    public sealed class TransformRotationController : Controller
    {
        private readonly IDirectionalRotator _rotator;

        public TransformRotationController(IDirectionalRotator rotator)
        {
            _rotator = rotator ?? throw new ArgumentNullException(nameof(rotator));
        }

        protected override void OnTick(float deltaTime)
        {
            _rotator.Rotate(deltaTime);
        }
    }
}
