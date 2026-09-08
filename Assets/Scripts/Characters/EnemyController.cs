using System;
using UnityEngine;
using UnityEngine.AI;

namespace GhostArena
{
    public sealed class EnemyController : MonoBehaviour
    {
        private const int DestinationSelectionAttempts = 8;
        private const float DestinationSampleDistance = 1f;
        private const float DirectionThreshold = 0.0001f;

        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private Rigidbody _body;
        [SerializeField] private ActorHealth _health;

        private Vector2 _arenaHalfExtents;
        private NavMeshPath _navigationPath;
        private float _movementSpeed;
        private float _directionInterval;
        private float _directionTimeRemaining;
        private bool _isGameplayActive;
        private bool _isInitialized;

        public event Action<EnemyController> Died;
        public event Action<EnemyController> DirectionChanged;

        public ActorHealth Health => _health;

        public Vector3 Direction { get; private set; }

        public bool IsGameplayActive => _isGameplayActive;

        public float MovementSpeed => _movementSpeed;

        public float DirectionInterval => _directionInterval;

        public void Initialize(Vector2 arenaHalfExtents, EnemySettings settings)
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("Enemy controller is already initialized.");
            }

            if (_agent == null || _body == null || _health == null)
            {
                throw new InvalidOperationException("Enemy controller references are not configured.");
            }

            if (arenaHalfExtents.x <= 0f || arenaHalfExtents.y <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(arenaHalfExtents));
            }

            if (settings.MaximumHealth <= 0
                || settings.MovementSpeed <= 0f
                || float.IsNaN(settings.MovementSpeed)
                || float.IsInfinity(settings.MovementSpeed)
                || settings.DirectionInterval <= 0f
                || float.IsNaN(settings.DirectionInterval)
                || float.IsInfinity(settings.DirectionInterval)
                || settings.ContactDamage <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(settings));
            }

            if (_agent.isOnNavMesh == false)
            {
                throw new InvalidOperationException("Enemy navigation agent is not placed on a NavMesh.");
            }

            _body.useGravity = false;
            _body.isKinematic = true;
            _body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            _movementSpeed = settings.MovementSpeed;
            _directionInterval = settings.DirectionInterval;
            _agent.speed = _movementSpeed;
            _agent.updatePosition = true;
            _agent.updateRotation = true;
            _agent.isStopped = true;

            _isInitialized = true;
            _arenaHalfExtents = arenaHalfExtents;
            _navigationPath = new NavMeshPath();
            _health.Initialize(settings.MaximumHealth);
            _health.Died += OnDied;
            ContactDamage contactDamage = GetComponent<ContactDamage>();

            if (contactDamage == null)
            {
                throw new InvalidOperationException("Enemy has no ContactDamage component.");
            }

            contactDamage.Initialize(settings.ContactDamage);
            ChooseRandomDirection();
        }

        public void SetGameplayActive(bool isActive)
        {
            _isGameplayActive = isActive && _health.IsAlive;

            if (_agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = _isGameplayActive == false;
            }
        }

        private void Update()
        {
            if (_isGameplayActive == false || _health.IsAlive == false)
            {
                return;
            }

            _directionTimeRemaining -= Time.deltaTime;

            if (_directionTimeRemaining <= 0f)
            {
                ChooseRandomDirection();
            }
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.Died -= OnDied;
            }
        }

        private void OnDisable()
        {
            _isGameplayActive = false;

            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
            }
        }

        private void ChooseRandomDirection()
        {
            float minimumDestinationDistance = _movementSpeed * _directionInterval;
            float minimumDestinationDistanceSquared = Mathf.Max(
                minimumDestinationDistance * minimumDestinationDistance,
                DirectionThreshold);

            for (int attempt = 0; attempt < DestinationSelectionAttempts; attempt++)
            {
                Vector3 candidate = new Vector3(
                    UnityEngine.Random.Range(-_arenaHalfExtents.x, _arenaHalfExtents.x),
                    transform.position.y,
                    UnityEngine.Random.Range(-_arenaHalfExtents.y, _arenaHalfExtents.y));

                if (NavMesh.SamplePosition(
                        candidate,
                        out NavMeshHit hit,
                        DestinationSampleDistance,
                        _agent.areaMask) == false)
                {
                    continue;
                }

                Vector3 direction = hit.position - transform.position;
                direction.y = 0f;

                if (direction.sqrMagnitude < minimumDestinationDistanceSquared
                    || _agent.CalculatePath(hit.position, _navigationPath) == false
                    || _navigationPath.status != NavMeshPathStatus.PathComplete
                    || _agent.SetDestination(hit.position) == false)
                {
                    continue;
                }

                SetDirection(direction);
                return;
            }

            _directionTimeRemaining = _directionInterval;
        }

        private void SetDirection(Vector3 direction)
        {
            Direction = direction.normalized;
            _directionTimeRemaining = _directionInterval;
            DirectionChanged?.Invoke(this);
        }

        private void OnDied()
        {
            SetGameplayActive(false);
            Died?.Invoke(this);
        }
    }
}
