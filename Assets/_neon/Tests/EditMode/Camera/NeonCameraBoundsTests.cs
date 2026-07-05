using NUnit.Framework;

namespace BrainlessLabs.Neon.Tests
{
    public class NeonCameraBoundsTests
    {
        [Test]
        public void RightEdge_advances_left_bound_monotonically()
        {
            // rightBoundX=25, camHalfW=8, previously reached left=5 — span (20) exceeds
            // one view width (16), so this is the normal (non-degenerate) case.
            var rect = NeonCameraBounds.ConfinerRect(
                rightBoundX: 25f, camHalfWidth: 8f, camHalfHeight: 4.5f,
                centerY: 0f, previousLeftX: 5f);
            // left edge never retreats below previousLeftX
            Assert.GreaterOrEqual(rect.xMin, 5f);
            // right edge sits at the bound
            Assert.AreEqual(25f, rect.xMax, 0.001f);
        }

        [Test]
        public void RightEdge_is_at_least_one_view_wide()
        {
            // A degenerate bound narrower than the view must widen to >= 2*halfWidth
            var rect = NeonCameraBounds.ConfinerRect(
                rightBoundX: 3f, camHalfWidth: 8f, camHalfHeight: 4.5f,
                centerY: 0f, previousLeftX: 0f);
            Assert.GreaterOrEqual(rect.width, 16f - 0.001f);
        }

        [Test]
        public void Height_spans_two_half_heights_around_centerY()
        {
            var rect = NeonCameraBounds.ConfinerRect(
                rightBoundX: 20f, camHalfWidth: 8f, camHalfHeight: 4.5f,
                centerY: 2f, previousLeftX: 0f);
            Assert.AreEqual(2f - 4.5f, rect.yMin, 0.001f);
            Assert.AreEqual(2f + 4.5f, rect.yMax, 0.001f);
        }
    }
}
