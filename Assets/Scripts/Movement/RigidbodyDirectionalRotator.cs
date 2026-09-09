using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class RigidbodyDirectionalRotator : IDirectionalRotator
    {
        private const float DirectionThreshold = 0.0001f;
        private readonly Rigidbody _body;
        private Vector3 _direction;

        public RigidbodyDirectionalRotator(Rigidbody body)
        {
            _body = body != null ? body : throw new ArgumentNullException(nameof(body));
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
            if (_body != null && _direction.sqrMagnitude > DirectionThreshold)
            {
                _body.MoveRotation(Quaternion.LookRotation(_direction, Vector3.up));
            }
        }
    }
}
