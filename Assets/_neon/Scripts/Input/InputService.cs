using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace BrainlessLabs.Neon
{
    public class InputService : IInputService, System.IDisposable
    {
        private readonly PlayerControls _playerInput;
        private readonly InputAction _move;
        private readonly InputAction _punch;
        private readonly InputAction _kick;
        private readonly InputAction _defend;
        private readonly InputAction _grab;
        private readonly InputAction _jump;

        public InputService()
        {
            _playerInput = new PlayerControls();

            _move = _playerInput.Player.Move;
            _punch = _playerInput.Player.Punch;
            _kick = _playerInput.Player.Kick;
            _defend = _playerInput.Player.Defend;
            _grab = _playerInput.Player.Grab;
            _jump = _playerInput.Player.Jump;

            _move.Enable();
            _punch.Enable();
            _kick.Enable();
            _defend.Enable();
            _grab.Enable();
            _jump.Enable();

            InputSystem.onDeviceChange += OnDeviceChange;
        }

        public bool PunchKeyDown(int playerId)
        {
            return _punch?.WasPressedThisFrame() ?? false;
        }

        public bool KickKeyDown(int playerId)
        {
            return _kick?.WasPressedThisFrame() ?? false;
        }

        public bool DefendKeyDown(int playerId)
        {
            return _defend?.IsPressed() ?? false;
        }

        public bool GrabKeyDown(int playerId)
        {
            return _grab?.WasPressedThisFrame() ?? false;
        }

        public bool JumpKeyDown(int playerId)
        {
            return _jump?.WasPressedThisFrame() ?? false;
        }

        public Vector2 GetInputVector(int playerId)
        {
            return _move?.ReadValue<Vector2>() ?? Vector2.zero;
        }

        public bool JoypadDirInputDetected(int playerId)
        {
            var value = _move?.ReadValue<Vector2>() ?? Vector2.zero;
            return value.x != 0 || value.y != 0;
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added)
                InputUser.PerformPairingWithDevice(device, InputUser.all[0],
                    InputUserPairingOptions.ForceNoPlatformUserAccountSelection);
        }

        public void Dispose()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;

            _move.Disable();
            _punch.Disable();
            _kick.Disable();
            _defend.Disable();
            _grab.Disable();
            _jump.Disable();

            _playerInput.Dispose();
        }
    }
}
