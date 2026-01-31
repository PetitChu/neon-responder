using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// A custom attribute used to add helpful text to Unity properties in the Inspector.
    /// Specifies a message that can provide context, instructions, or additional information
    /// about the associated property.
    /// </summary>
    public class HelpAttribute : PropertyAttribute
    {
        public readonly string text;

        public HelpAttribute(string text)
        {
            this.text = text;
        }
    }
}