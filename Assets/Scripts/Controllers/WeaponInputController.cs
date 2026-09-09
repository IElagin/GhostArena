using System;
using UnityEngine.InputSystem;

namespace GhostArena
{
    public sealed class WeaponInputController : Controller
    {
        private const string PressOnlyInteraction = "Press(behavior=0)";
        private readonly Weapon _weapon;
        private readonly InputAction _fireAction;

        public WeaponInputController(Weapon weapon)
        {
            _weapon = weapon ?? throw new ArgumentNullException(nameof(weapon));
            _fireAction = new InputAction(
                "Fire",
                InputActionType.Button,
                "<Keyboard>/space",
                PressOnlyInteraction);
            _fireAction.performed += OnFirePerformed;
        }

        public override void Enable()
        {
            base.Enable();
            _fireAction.Enable();
        }

        public override void Disable()
        {
            if (IsDisposed)
            {
                return;
            }

            base.Disable();
            _fireAction.Disable();
        }

        protected override void OnDispose()
        {
            _fireAction.performed -= OnFirePerformed;
            _fireAction.Dispose();
        }

        private void OnFirePerformed(InputAction.CallbackContext context)
        {
            _weapon.TryFire();
        }
    }
}
