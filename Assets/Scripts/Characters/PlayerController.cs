using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GhostArena
{
    public sealed class PlayerController : MonoBehaviour
    {
        private const float DirectionThreshold = 0.0001f;

        [SerializeField] private Rigidbody _body;
        [SerializeField] private ActorHealth _health;

        private Camera _gameplayCamera;
        private InputAction _moveAction;
        private float _movementSpeed;
        private bool _isGameplayActive;

        public ActorHealth Health => _health;

        public Vector2 InputVector { get; private set; }

        public Vector3 MovementDirection { get; private set; }

        public Vector3 FacingDirection { get; private set; } = Vector3.forward;

        public bool IsGameplayActive => _isGameplayActive;

        public bool CanReceiveDamage => _isGameplayActive && _health != null && _health.IsAlive;

        public float MovementSpeed => _movementSpeed;

        public void Initialize(Camera gameplayCamera, float movementSpeed)
        {
            if (_moveAction != null)
            {
                throw new InvalidOperationException("Player controller is already initialized.");
            }

            _gameplayCamera = gameplayCamera != null
                ? gameplayCamera
                : throw new ArgumentNullException(nameof(gameplayCamera));

            if (_body == null || _health == null)
            {
                throw new InvalidOperationException("Player controller references are not configured.");
            }

            if (movementSpeed <= 0f || float.IsNaN(movementSpeed) || float.IsInfinity(movementSpeed))
            {
                throw new ArgumentOutOfRangeException(nameof(movementSpeed));
            }

            _movementSpeed = movementSpeed;
            _body.useGravity = false;
            _body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            transform.forward = Vector3.forward;

            _moveAction = new InputAction("Move", InputActionType.Value);
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
        }

        public void SetGameplayActive(bool isActive)
        {
            _isGameplayActive = isActive && _health.IsAlive;

            if (_isGameplayActive)
            {
                _moveAction.Enable();
                return;
            }

            _moveAction.Disable();
            InputVector = Vector2.zero;
            MovementDirection = Vector3.zero;
            _body.linearVelocity = Vector3.zero;
        }

        private void FixedUpdate()
        {
            if (_isGameplayActive == false || _health.IsAlive == false)
            {
                return;
            }

            InputVector = Vector2.ClampMagnitude(_moveAction.ReadValue<Vector2>(), 1f);
            MovementDirection = GetCameraRelativeDirection(InputVector);

            if (MovementDirection.sqrMagnitude > DirectionThreshold)
            {
                FacingDirection = MovementDirection;
                transform.forward = FacingDirection;
            }

            Vector3 offset = MovementDirection * (_movementSpeed * Time.fixedDeltaTime);
            _body.MovePosition(_body.position + offset);
        }

        private void OnDestroy()
        {
            if (_moveAction == null)
            {
                return;
            }

            _moveAction.Disable();
            _moveAction.Dispose();
        }

        private Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            Vector3 cameraForward = _gameplayCamera.transform.forward;
            Vector3 cameraRight = _gameplayCamera.transform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 direction = cameraRight * input.x + cameraForward * input.y;
            return Vector3.ClampMagnitude(direction, 1f);
        }
    }
}
