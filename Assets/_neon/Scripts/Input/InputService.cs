using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace BrainlessLabs.Neon
{
    public class InputService : IInputService, System.IDisposable
    {
        public static InputService Instance { get; private set; }

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
            Instance = this;
        }

        #region Static API (backward compatibility)

        public static bool PunchKeyDown(int playerId)
        {
            return Instance?._punch?.WasPressedThisFrame() ?? false;
        }

        public static bool KickKeyDown(int playerId)
        {
            return Instance?._kick?.WasPressedThisFrame() ?? false;
        }

        public static bool DefendKeyDown(int playerId)
        {
            return Instance?._defend?.IsPressed() ?? false;
        }

        public static bool GrabKeyDown(int playerId)
        {
            return Instance?._grab?.WasPressedThisFrame() ?? false;
        }

        public static bool JumpKeyDown(int playerId)
        {
            return Instance?._jump?.WasPressedThisFrame() ?? false;
        }

        public static Vector2 GetInputVector(int playerId)
        {
            return Instance?._move?.ReadValue<Vector2>() ?? Vector2.zero;
        }

        public static bool JoypadDirInputDetected(int playerId)
        {
            var value = Instance?._move?.ReadValue<Vector2>() ?? Vector2.zero;
            return value.x != 0 || value.y != 0;
        }

        #endregion

        #region IInputService

        bool IInputService.PunchKeyDown(int playerId) => PunchKeyDown(playerId);
        bool IInputService.KickKeyDown(int playerId) => KickKeyDown(playerId);
        bool IInputService.DefendKeyDown(int playerId) => DefendKeyDown(playerId);
        bool IInputService.GrabKeyDown(int playerId) => GrabKeyDown(playerId);
        bool IInputService.JumpKeyDown(int playerId) => JumpKeyDown(playerId);
        Vector2 IInputService.GetInputVector(int playerId) => GetInputVector(playerId);
        bool IInputService.JoypadDirInputDetected(int playerId) => JoypadDirInputDetected(playerId);

        #endregion

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

            if (Instance == this) Instance = null;
        }
    }
}
