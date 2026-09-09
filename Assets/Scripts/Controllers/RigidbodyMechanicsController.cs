using System;

namespace GhostArena
{
    public sealed class RigidbodyMechanicsController : Controller
    {
        private readonly IDirectionalMover _mover;
        private readonly IDirectionalRotator _rotator;

        public RigidbodyMechanicsController(IDirectionalMover mover, IDirectionalRotator rotator)
        {
            _mover = mover ?? throw new ArgumentNullException(nameof(mover));
            _rotator = rotator ?? throw new ArgumentNullException(nameof(rotator));
        }

        protected override void OnFixedTick(float fixedDeltaTime)
        {
            _mover.Move(fixedDeltaTime);
            _rotator.Rotate(fixedDeltaTime);
        }
    }
}
