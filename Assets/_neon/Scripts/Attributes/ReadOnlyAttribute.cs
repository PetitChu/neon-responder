using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// A custom property attribute used to mark fields in Unity scripts as read-only in the Inspector.
    /// This attribute is purely for UI purposes and does not enforce immutability or read-only behavior at runtime.
    /// </summary>
    public class ReadOnlyProperty : PropertyAttribute { }
}
