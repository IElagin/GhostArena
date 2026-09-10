using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class RigidbodyDirectionalMover : IDirectionalMover
    {
        private readonly Rigidbody _body;
        private readonly float _movementSpeed;
        private Vector3 _direction;

        public RigidbodyDirectionalMover(Rigidbody body, float movementSpeed)
        {
            _body = body != null ? body : throw new ArgumentNullException(nameof(body));

            if (movementSpeed <= 0f || float.IsNaN(movementSpeed) || float.IsInfinity(movementSpeed))
            {
                throw new ArgumentOutOfRangeException(nameof(movementSpeed));
            }

            _movementSpeed = movementSpeed;
        }

        public Vector3 CurrentVelocity => _direction * _movementSpeed;

        public void SetDirection(Vector3 direction)
        {
            direction.y = 0f;
            _direction = Vector3.ClampMagnitude(direction, 1f);
        }

        public void Move(float fixedDeltaTime)
        {
            if (_body == null)
            {
                return;
            }

            Vector3 velocity = CurrentVelocity;
            velocity.y = _body.linearVelocity.y;
            _body.linearVelocity = velocity;
        }

        public void Stop()
        {
            _direction = Vector3.zero;

            if (_body != null)
            {
                _body.linearVelocity = Vector3.zero;
            }
        }
    }
}
