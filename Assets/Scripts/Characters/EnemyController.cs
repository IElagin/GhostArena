using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class EnemyController : MonoBehaviour
    {
        private const float DirectionThreshold = 0.0001f;
        private const string EnvironmentLayerName = "Environment";

        [SerializeField] private Rigidbody _body;
        [SerializeField] private ActorHealth _health;
        [SerializeField] private float _movementSpeed = 2f;
        [SerializeField] private float _directionInterval = 2f;

        private Vector2 _arenaHalfExtents;
        private float _directionTimeRemaining;
        private bool _isGameplayActive;
        private bool _isInitialized;

        public event Action<EnemyController> Died;
        public event Action<EnemyController> DirectionChanged;

        public ActorHealth Health => _health;

        public Vector3 Direction { get; private set; }

        public bool IsGameplayActive => _isGameplayActive;

        public void Initialize(Vector2 arenaHalfExtents, int maximumHealth)
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("Enemy controller is already initialized.");
            }

            if (_body == null || _health == null)
            {
                throw new InvalidOperationException("Enemy controller references are not configured.");
            }

            if (arenaHalfExtents.x <= 0f || arenaHalfExtents.y <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(arenaHalfExtents));
            }

            _isInitialized = true;
            _arenaHalfExtents = arenaHalfExtents;
            _body.useGravity = false;
            _body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            _health.Initialize(maximumHealth);
            _health.Died += OnDied;
            ChooseRandomDirection();
        }

        public void SetGameplayActive(bool isActive)
        {
            _isGameplayActive = isActive && _health.IsAlive;

            if (_isGameplayActive == false)
            {
                _body.linearVelocity = Vector3.zero;
            }
        }

        private void FixedUpdate()
        {
            if (_isGameplayActive == false || _health.IsAlive == false)
            {
                return;
            }

            _directionTimeRemaining -= Time.fixedDeltaTime;

            if (_directionTimeRemaining <= 0f)
            {
                ChooseRandomDirection();
            }

            Vector3 nextPosition = _body.position + Direction * (_movementSpeed * Time.fixedDeltaTime);

            if (IsOutsideArena(nextPosition))
            {
                ChooseInwardDirection();
                nextPosition = _body.position + Direction * (_movementSpeed * Time.fixedDeltaTime);
            }

            _body.MovePosition(nextPosition);
        }

        private void OnCollisionEnter(Collision collision)
        {
            int environmentLayer = LayerMask.NameToLayer(EnvironmentLayerName);

            if (_isGameplayActive && collision.gameObject.layer == environmentLayer)
            {
                ChooseInwardDirection();
            }
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.Died -= OnDied;
            }
        }

        private void ChooseRandomDirection()
        {
            Vector2 randomDirection = UnityEngine.Random.insideUnitCircle;

            if (randomDirection.sqrMagnitude <= DirectionThreshold)
            {
                randomDirection = Vector2.up;
            }

            SetDirection(new Vector3(randomDirection.x, 0f, randomDirection.y));
        }

        private void ChooseInwardDirection()
        {
            Vector3 inwardDirection = new Vector3(-_body.position.x, 0f, -_body.position.z).normalized;
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * 0.35f;
            Vector3 direction = inwardDirection + new Vector3(randomOffset.x, 0f, randomOffset.y);

            if (direction.sqrMagnitude <= DirectionThreshold)
            {
                direction = Vector3.forward;
            }

            SetDirection(direction);
        }

        private void SetDirection(Vector3 direction)
        {
            Direction = direction.normalized;
            _directionTimeRemaining = _directionInterval;
            transform.forward = Direction;
            DirectionChanged?.Invoke(this);
        }

        private bool IsOutsideArena(Vector3 position)
        {
            return Mathf.Abs(position.x) > _arenaHalfExtents.x
                || Mathf.Abs(position.z) > _arenaHalfExtents.y;
        }

        private void OnDied()
        {
            SetGameplayActive(false);
            Died?.Invoke(this);
        }
    }
}
