using UnityHFSM;

namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Base transition class that always allows transition.
    /// </summary>
    public class ApplicationTransition : TransitionBase
    {
        public ApplicationTransition(string from, string to, bool forceInstantly = false)
            : base(from, to, forceInstantly)
        {
        }

        public override bool ShouldTransition() => true;

        public override void Init() { }
        public override void BeforeTransition() { }
        public override void AfterTransition() { }
    }
}
