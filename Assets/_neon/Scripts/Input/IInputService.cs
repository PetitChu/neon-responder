using UnityEngine;

namespace BrainlessLabs.Neon
{
    public interface IInputService
    {
        bool PunchKeyDown(int playerId);
        bool KickKeyDown(int playerId);
        bool DefendKeyDown(int playerId);
        bool GrabKeyDown(int playerId);
        bool JumpKeyDown(int playerId);
        Vector2 GetInputVector(int playerId);
        bool JoypadDirInputDetected(int playerId);
    }
}
