using UnityEngine;

namespace BrainlessLabs.Neon
{

    public class HelpAttribute : PropertyAttribute
    {
        public string text;

        public HelpAttribute(string text)
        {
            this.text = text;
        }
    }
}