using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class TransformDirectionalRotator : IDirectionalRotator
    {
        private const float DirectionThreshold = 0.0001f;
        private readonly Transform _transform;
        private readonly float _rotationSpeed;
        private Vector3 _direction;

        public TransformDirectionalRotator(Transform transform, float rotationSpeed)
        {
            _transform = transform != null ? transform : throw new ArgumentNullException(nameof(transform));

            if (rotationSpeed <= 0f || float.IsNaN(rotationSpeed) || float.IsInfinity(rotationSpeed))
            {
                throw new ArgumentOutOfRangeException(nameof(rotationSpeed));
            }

            _rotationSpeed = rotationSpeed;
        }

        public void SetDirection(Vector3 direction)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude > DirectionThreshold)
            {
                _direction = direction.normalized;
            }
        }

        public void Rotate(float deltaTime)
        {
            if (_transform != null && _direction.sqrMagnitude > DirectionThreshold)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_direction, Vector3.up);
                _transform.rotation = Quaternion.RotateTowards(
                    _transform.rotation,
                    targetRotation,
                    _rotationSpeed * deltaTime);
            }
        }
    }
}
