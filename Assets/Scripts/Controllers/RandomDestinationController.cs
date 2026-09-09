using System;
using UnityEngine;
using UnityEngine.AI;

namespace GhostArena
{
    public sealed class RandomDestinationController : Controller
    {
        private const int DestinationSelectionAttempts = 8;
        private const float DestinationSampleDistance = 1f;
        private readonly IDestinationMover _mover;
        private readonly Transform _transform;
        private readonly Vector2 _arenaHalfExtents;
        private readonly float _directionInterval;
        private readonly float _minimumDistanceSquared;
        private float _timeRemaining;

        public RandomDestinationController(
            IDestinationMover mover,
            Transform transform,
            Vector2 arenaHalfExtents,
            float directionInterval,
            float minimumDistance)
        {
            _mover = mover ?? throw new ArgumentNullException(nameof(mover));
            _transform = transform != null ? transform : throw new ArgumentNullException(nameof(transform));

            if (arenaHalfExtents.x <= 0f || arenaHalfExtents.y <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(arenaHalfExtents));
            }

            if (directionInterval <= 0f || float.IsNaN(directionInterval)
                || float.IsInfinity(directionInterval))
            {
                throw new ArgumentOutOfRangeException(nameof(directionInterval));
            }

            if (minimumDistance <= 0f || float.IsNaN(minimumDistance)
                || float.IsInfinity(minimumDistance))
            {
                throw new ArgumentOutOfRangeException(nameof(minimumDistance));
            }

            _arenaHalfExtents = arenaHalfExtents;
            _directionInterval = directionInterval;
            _minimumDistanceSquared = minimumDistance * minimumDistance;
        }

        public override void Disable()
        {
            base.Disable();
            _mover.Stop();
        }

        protected override void OnTick(float deltaTime)
        {
            _timeRemaining -= deltaTime;

            if (_timeRemaining <= 0f)
            {
                ChooseDestination();
            }
        }

        private void ChooseDestination()
        {
            for (int attempt = 0; attempt < DestinationSelectionAttempts; attempt++)
            {
                Vector3 candidate = new Vector3(
                    UnityEngine.Random.Range(-_arenaHalfExtents.x, _arenaHalfExtents.x),
                    _transform.position.y,
                    UnityEngine.Random.Range(-_arenaHalfExtents.y, _arenaHalfExtents.y));

                if (NavMesh.SamplePosition(
                        candidate,
                        out NavMeshHit hit,
                        DestinationSampleDistance,
                        _mover.AreaMask) == false)
                {
                    continue;
                }

                Vector3 direction = hit.position - _transform.position;
                direction.y = 0f;

                if (direction.sqrMagnitude < _minimumDistanceSquared
                    || _mover.TrySetDestination(hit.position) == false)
                {
                    continue;
                }

                break;
            }

            _timeRemaining = _directionInterval;
        }
    }
}
