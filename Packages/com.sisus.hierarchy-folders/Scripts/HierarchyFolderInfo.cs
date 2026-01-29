#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sisus.HierarchyFolders
{
	internal sealed class HierarchyFolderInfo
	{
		public HierarchyFolder hierarchyFolder;
		public Color color;
		public HierarchyIconType iconType;
		
		public HierarchyFolderInfo(HierarchyFolder hierarchyFolder)
		{
			this.hierarchyFolder = hierarchyFolder;
			color = hierarchyFolder.Color;
			color.a = 1f;

			var gameObject = hierarchyFolder.gameObject;
			if(PrefabUtility.IsAnyPrefabInstanceRoot(gameObject))
			{
				if(PrefabUtility.IsAddedGameObjectOverride(gameObject))
				{
					iconType = PrefabUtility.IsPartOfVariantPrefab(gameObject)
						? HierarchyIconType.PrefabVariantAddition
						: HierarchyIconType.PrefabAddition;
				}
				else
				{
					iconType = PrefabUtility.IsPartOfVariantPrefab(gameObject)
						? HierarchyIconType.PrefabVariantRoot
						: HierarchyIconType.PrefabRoot;
				}
			}
			else
			{
				iconType = PrefabUtility.IsAddedGameObjectOverride(gameObject)
					? HierarchyIconType.GameObjectAddition
					: HierarchyIconType.Default;
			}
		}
	}
}
#endif