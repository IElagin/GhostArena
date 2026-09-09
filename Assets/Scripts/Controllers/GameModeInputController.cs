using System;
using UnityEngine.InputSystem;

namespace GhostArena
{
    public sealed class GameModeInputController : Controller
    {
        private const string PressOnlyInteraction = "Press(behavior=0)";
        private readonly GameMode _gameMode;
        private readonly InputAction _pauseAction;
        private readonly InputAction _restartAction;

        public GameModeInputController(GameMode gameMode)
        {
            _gameMode = gameMode ?? throw new ArgumentNullException(nameof(gameMode));
            _pauseAction = new InputAction(
                "Pause",
                InputActionType.Button,
                "<Keyboard>/escape",
                PressOnlyInteraction);
            _restartAction = new InputAction(
                "Restart",
                InputActionType.Button,
                "<Keyboard>/r",
                PressOnlyInteraction);
            _pauseAction.performed += OnPausePerformed;
            _restartAction.performed += OnRestartPerformed;
        }

        public override void Enable()
        {
            base.Enable();
            _pauseAction.Enable();
            _restartAction.Enable();
        }

        public override void Disable()
        {
            if (IsDisposed)
            {
                return;
            }

            base.Disable();
            _pauseAction.Disable();
            _restartAction.Disable();
        }

        protected override void OnDispose()
        {
            _pauseAction.performed -= OnPausePerformed;
            _restartAction.performed -= OnRestartPerformed;
            _pauseAction.Dispose();
            _restartAction.Dispose();
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            _gameMode.TogglePause();
        }

        private void OnRestartPerformed(InputAction.CallbackContext context)
        {
            _gameMode.RestartIfAllowed();
        }
    }
}
