using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using JetBrains.Annotations;
using Sisus.Shared.EditorOnly;

namespace Sisus.HierarchyFolders
{
	[InitializeOnLoad]
	public static class HierarchyFolderIconDrawer
	{
		private static readonly Color backgroundDark = new Color32(56, 56, 56, 255);
		private static readonly Color backgroundDarkMouseovered = new Color32(68, 68, 68, 255);
		private static readonly Color backgroundLightMouseovered = new Color32(178, 178, 178, 255);
		private static readonly Color backgroundLight = new Color32(200, 200, 200, 255);
		private static readonly Color backgroundDarkSelectedFocused = new Color32(44, 93, 135, 255);
		private static readonly Color backgroundDarkSelectedUnfocused = new Color32(77, 77, 77, 255);
		private static readonly Color backgroundLightSelectedFocused = new Color32(58, 114, 176, 255);
		private static readonly Color backgroundLightSelectedUnfocused = new Color32(174, 174, 174, 255);

		private static Texture prefabFolderIconOpen;
		private static Texture prefabFolderIconClosed;
		private static Texture prefabVariantFolderIconOpen;
		private static Texture prefabVariantFolderIconClosed;
		private static Texture defaultIconOpen;
		private static Texture defaultIconClosed;
		private static Texture prefabOverlayAdded;

		static HierarchyFolderIconDrawer() => Initialize();

		[DidReloadScripts, UsedImplicitly]
		private static void OnScriptsReloaded() => Initialize();

		private static void Initialize()
		{
			var preferences = HierarchyFolderPreferences.Get();
			UpdateIcons(preferences);
			ResubscribeToEvents(preferences);
		}

		private static void UpdateIcons(HierarchyFolderPreferences preferences)
		{
			if(EditorApplication.isUpdating)
			{
				EditorApplication.delayCall += () => UpdateIcons(HierarchyFolderPreferences.Get());

				if(!preferences)
				{
					return;
				}
			}

			var icon = preferences.Icon(HierarchyIconType.PrefabRoot);
			prefabFolderIconOpen = icon.open;
			prefabFolderIconClosed = icon.closed;

			icon = preferences.Icon(HierarchyIconType.PrefabVariantRoot);
			prefabVariantFolderIconOpen = icon.open;
			prefabVariantFolderIconClosed = icon.closed;

			defaultIconOpen = EditorGUIUtility.IconContent("FolderOpened Icon").image;
			defaultIconClosed = EditorGUIUtility.IconContent("Folder Icon").image;
			prefabOverlayAdded = EditorGUIUtility.IconContent("PrefabOverlayAdded Icon").image;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange playModeState)
		{
			if(playModeState == PlayModeStateChange.ExitingPlayMode)
			{
				ResubscribeToEvents(HierarchyFolderPreferences.Get());
			}
		}

		public static void ResubscribeToEvents(HierarchyFolderPreferences preferences)
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

			Undo.undoRedoPerformed -= OnUndoRedoPerformed;
			Undo.undoRedoPerformed += OnUndoRedoPerformed;

			#if UNITY_6000_4_OR_NEWER
			EditorApplication.hierarchyWindowItemByEntityIdOnGUI
			#else
			EditorApplication.hierarchyWindowItemOnGUI
			#endif
				-= HandleDrawIconAndDoubleClickToSelectChildren;

			#if UNITY_6000_4_OR_NEWER
			EditorApplication.hierarchyWindowItemByEntityIdOnGUI
			#else
			EditorApplication.hierarchyWindowItemOnGUI
			#endif
				-= HandleDrawIcon;

			#if UNITY_6000_4_OR_NEWER
			EditorApplication.hierarchyWindowItemByEntityIdOnGUI
			#else
			EditorApplication.hierarchyWindowItemOnGUI
			#endif
				-= HandleDoubleClickToSelectChildren;

			if(!preferences)
			{
				return;
			}

			preferences.onPreferencesChanged -= ResubscribeToEvents;
			preferences.onPreferencesChanged += ResubscribeToEvents;

			if(preferences.enableHierarchyIcons)
			{
				if(preferences.doubleClickSelectsChildrens)
				{
					#if UNITY_6000_4_OR_NEWER
					EditorApplication.hierarchyWindowItemByEntityIdOnGUI
					#else
					EditorApplication.hierarchyWindowItemOnGUI
					#endif
						+= HandleDrawIconAndDoubleClickToSelectChildren;
				}
				else
				{
					#if UNITY_6000_4_OR_NEWER
					EditorApplication.hierarchyWindowItemByEntityIdOnGUI
					#else
					EditorApplication.hierarchyWindowItemOnGUI
					#endif
						+= HandleDrawIcon;
				}
			}
			else if(preferences.doubleClickSelectsChildrens)
			{
				#if UNITY_6000_4_OR_NEWER
				EditorApplication.hierarchyWindowItemByEntityIdOnGUI
				#else
				EditorApplication.hierarchyWindowItemOnGUI
				#endif
					+= HandleDoubleClickToSelectChildren;
			}
		}

		private static void OnUndoRedoPerformed()
		{
			foreach(var sceneViewObject in SceneView.sceneViews)
			{
				if(sceneViewObject is SceneView sceneView && sceneView)
				{
					sceneView.Repaint();
				}
			}
		}

		private static void HandleDrawIconAndDoubleClickToSelectChildren
		(
			#if UNITY_6000_4_OR_NEWER
			EntityId entityId,
			#else
			int entityId,
			#endif
			Rect itemRect
		)
		{
			switch(Event.current.type)
			{
				case EventType.Repaint:
					DrawIcon(entityId, itemRect);
					return;
				case EventType.MouseDown:
					if(Event.current.clickCount == 2 && itemRect.Contains(Event.current.mousePosition))
					{
						SelectChildrenIfIsHierarchyFolder(entityId);
					}

					return;
			}
		}

		private static void HandleDrawIcon
		(
			#if UNITY_6000_4_OR_NEWER
			EntityId entityId,
			#else
			int entityId,
			#endif
			Rect itemRect
		)
		{
			if(Event.current.type == EventType.Repaint)
			{
				DrawIcon(entityId, itemRect);
			}
		}

		private static void HandleDoubleClickToSelectChildren
		(
			#if UNITY_6000_4_OR_NEWER
			EntityId entityId,
			#else
			int entityId,
			#endif
			Rect itemRect
		)
		{
			if(Event.current.clickCount == 2 && Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
			{
				SelectChildrenIfIsHierarchyFolder(entityId);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void DrawIcon(int instanceId, Rect itemRect)
		{
			if(!HierarchyFolder.infos.TryGetValue(instanceId, out var info) || !info.hierarchyFolder)
			{
				return;
			}

			var draggedItemId = HierarchyWindowUtility.GetDraggedItemId();
			var isSomeItemBeingDragged = draggedItemId != -1;
			var isSomeItemBeingRenamed = !isSomeItemBeingDragged && HierarchyWindowUtility.GetItemBeingRenamedId() != -1;
			var isDraggedOrSelected = !isSomeItemBeingRenamed && HierarchyWindowUtility.IsDraggedOrSelected(instanceId);
			var isFocused = HierarchyWindowUtility.IsHierarchyWindowFocused();
			var isMouseovered = !isSomeItemBeingDragged && itemRect.Contains(Event.current.mousePosition);

			var backgroundColor = EditorGUIUtility.isProSkin switch
			{
				true when isDraggedOrSelected => isFocused ? backgroundDarkSelectedFocused : backgroundDarkSelectedUnfocused,
				true when isMouseovered => backgroundDarkMouseovered,
				true => backgroundDark,
				false when isDraggedOrSelected => isFocused ? backgroundLightSelectedFocused : backgroundLightSelectedUnfocused,
				false when isMouseovered => backgroundLightMouseovered,
				_ => backgroundLight
			};

			var expanded = HierarchyWindowUtility.IsExpanded(instanceId);

			Texture icon = info.iconType switch
			{
				HierarchyIconType.PrefabRoot or HierarchyIconType.PrefabAddition => expanded ? prefabFolderIconOpen : prefabFolderIconClosed,
				HierarchyIconType.PrefabVariantRoot or HierarchyIconType.PrefabVariantAddition => expanded ? prefabVariantFolderIconOpen : prefabVariantFolderIconClosed,
				_ => expanded ? defaultIconOpen : defaultIconClosed,
			};

			if(!icon)
			{
				icon = expanded ? defaultIconOpen : defaultIconClosed;
			}

			var iconRect = itemRect;
			iconRect.width = itemRect.height;
			EditorGUI.DrawRect(iconRect, backgroundColor);
			var colorWas = GUI.color;
			GUI.color = info.color;
			GUI.DrawTexture(iconRect, icon);
			GUI.color = colorWas;
			
			if(info.iconType is HierarchyIconType.GameObjectAddition or HierarchyIconType.PrefabAddition or HierarchyIconType.PrefabVariantAddition)
			{
				GUI.DrawTexture(iconRect, prefabOverlayAdded);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void SelectChildrenIfIsHierarchyFolder
		(
			#if UNITY_6000_4_OR_NEWER
			EntityId entityId
			#else
			int entityId
			#endif
		)
		{
			var gameObject =
				#if UNITY_6000_3_OR_NEWER
				EditorUtility.EntityIdToObject(entityId)
				#else
				EditorUtility.InstanceIDToObject(entityId)
				#endif
				as GameObject;
			if(gameObject && gameObject.transform.childCount > 0 && gameObject.IsHierarchyFolder())
			{
				Event.current.Use();
				var children = gameObject.GetChildren(false);

				HierarchyWindowUtility.SetExpandedRecursive(entityId, true);
				for(int n = children.Length - 1; n >= 0; n--)
				{
					HierarchyWindowUtility.SetExpandedRecursive(children[n].GetInstanceID(), true);
				}

				Selection.objects = children;
			}
		}
	}
}