using System;

namespace GhostArena
{
    public sealed class VelocityFacingController : Controller
    {
        private readonly IVelocitySource _velocitySource;
        private readonly IDirectionalRotator _rotator;

        public VelocityFacingController(IVelocitySource velocitySource, IDirectionalRotator rotator)
        {
            _velocitySource = velocitySource ?? throw new ArgumentNullException(nameof(velocitySource));
            _rotator = rotator ?? throw new ArgumentNullException(nameof(rotator));
        }

        protected override void OnTick(float deltaTime)
        {
            _rotator.SetDirection(_velocitySource.CurrentVelocity);
        }
    }
}
