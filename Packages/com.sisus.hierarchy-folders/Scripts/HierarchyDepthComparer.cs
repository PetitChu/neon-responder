#if UNITY_EDITOR
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Sisus.HierarchyFolders
{
	public sealed class HierarchyDepthComparer : IComparer<HierarchyFolder>
	{
		public int Compare([NotNull] HierarchyFolder x, [NotNull] HierarchyFolder y)
		{
			return GetDepth(x).CompareTo(GetDepth(y));
		}

		private int GetDepth([NotNull] HierarchyFolder hierarchyFolder)
		{
			int depth = 0;
			for(var parent = hierarchyFolder.transform.parent; parent != null; parent = parent.parent)
			{
				depth++;
			}
			return depth;
		}
	}
}
#endif