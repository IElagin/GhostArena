using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GhostArena
{
    public sealed class KeyboardDirectionController : Controller
    {
        private readonly IDirectionalMover _mover;
        private readonly Camera _camera;
        private readonly InputAction _moveAction;
        private Vector2 _inputVector;

        public KeyboardDirectionController(IDirectionalMover mover, Camera camera)
        {
            _mover = mover ?? throw new ArgumentNullException(nameof(mover));
            _camera = camera != null ? camera : throw new ArgumentNullException(nameof(camera));
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

        public override void Enable()
        {
            base.Enable();
            _moveAction.Enable();
        }

        public override void Disable()
        {
            if (IsDisposed)
            {
                return;
            }

            base.Disable();
            _moveAction.Disable();
            _inputVector = Vector2.zero;
            _mover.Stop();
        }

        protected override void OnDispose()
        {
            _moveAction.Dispose();
        }

        protected override void OnTick(float deltaTime)
        {
            _inputVector = Vector2.ClampMagnitude(_moveAction.ReadValue<Vector2>(), 1f);
            Vector3 cameraForward = _camera.transform.forward;
            Vector3 cameraRight = _camera.transform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();
            Vector3 direction = cameraRight * _inputVector.x + cameraForward * _inputVector.y;
            _mover.SetDirection(Vector3.ClampMagnitude(direction, 1f));
        }
    }
}
