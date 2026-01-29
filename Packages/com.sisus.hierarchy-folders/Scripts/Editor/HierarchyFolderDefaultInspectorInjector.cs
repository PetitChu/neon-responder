#define HF_DISABLE_GAMEOBJECT_EDITOR

#if HF_DISABLE_GAMEOBJECT_EDITOR && UNITY_2018_2_OR_NEWER // Editor.finishedDefaultHeaderGUI doesn't exist in Unity versions older than 2018.2
using System;
using System.Collections.Generic;
using Sisus.HierarchyFolders.Prefabs;
using Sisus.Shared;
using UnityEditor;
using UnityEngine;
using Sisus.Shared.EditorOnly;
using Object = UnityEngine.Object;

namespace Sisus.HierarchyFolders
{
	[InitializeOnLoad]
	internal static class HierarchyFolderDefaultInspectorInjector
	{
		private static readonly GUIContent label = new("This is a hierarchy folder and can be used for organizing objects in the hierarchy.\n\nWhen a build is being made all members will be moved up the parent chain and the folder itself will be removed.");
		private static readonly int DefaultLayer = LayerMask.NameToLayer("Default");
		private static readonly HashSet<GameObject> inspectedHierarchyFolders = new();
		private static readonly List<HierarchyFolder> hierarchyFolders = new();
		private static bool isPrefabAssetOrOpenInPrefabStage;
		private static Texture2D folderIcon;
		private static readonly Rect changeColorRect = new(0f, 0f, 38f, 42f);
		private static readonly Rect iconRect = new(5f, 5f, 30f, 30f);
		private static Tool? restoreActiveTool;
		private static HierarchyFolderPreferences preferences;
		
		private static Color32 InspectorBackgroundColor => EditorGUIUtility.isProSkin ? new(60, 60, 60, 255) : new(203, 203, 203, 255);

		static HierarchyFolderDefaultInspectorInjector()
		{
			GameObjectHeader.BeforeHeaderGUI -= OnBeforeGameObjectHeaderGUI;
			GameObjectHeader.BeforeHeaderGUI += OnBeforeGameObjectHeaderGUI;
			GameObjectHeader.AfterHeaderGUI -= OnAfterGameObjectHeaderGUI;
			GameObjectHeader.AfterHeaderGUI += OnAfterGameObjectHeaderGUI;
			Selection.selectionChanged -= OnSelectionChanged;
			Selection.selectionChanged += OnSelectionChanged;
		}

		private static void OnBeforeGameObjectHeaderGUI(GameObject[] targets, Rect headerRect, bool headerIsSelected, bool supportsRichText)
		{
			int count = targets.Length;
			if(count == 0)
			{
				return;
			}

			isPrefabAssetOrOpenInPrefabStage = false;
			bool? setTagForChildren = null;

			hierarchyFolders.Clear();

			for(int i = targets.Length - 1; i >= 0; i--)
			{
				var gameObject = targets[0];
				if(!gameObject.TryGetComponent(out HierarchyFolder hierarchyFolder))
				{
					continue;
				}
				
				hierarchyFolders.Add(hierarchyFolder);

				if(Tools.current is not (Tool.Custom or Tool.None or Tool.View))
				{
					if(!restoreActiveTool.HasValue)
					{
						restoreActiveTool = Tools.current;

						foreach(var sceneViewObject in SceneView.sceneViews)
						{
							if(sceneViewObject is SceneView sceneView && sceneView)
							{
								sceneView.Repaint();
							}
						}
					}

					Tools.current = Tool.None;
				}

				if(!gameObject.CompareTag("Untagged"))
				{
					setTagForChildren ??= EditorUtility.DisplayDialog("Change Tag", "Do you want to set tag to " + gameObject.tag + " for all child objects?", "Yes, change children", "Cancel");
					SetTag(targets, setTagForChildren.Value);
				}

				if(!folderIcon)
				{
					var iconSizeWas = EditorGUIUtility.GetIconSize();
					EditorGUIUtility.SetIconSize(new(30f, 30f));
					var iconContent = EditorGUIUtility.IconContent("Folder Icon");
					folderIcon = iconContent?.image as Texture2D;
					EditorGUIUtility.SetIconSize(iconSizeWas);

					#if DEV_MODE
					Debug.Assert(folderIcon);
					#endif
				}

				// Set Hierarchy Folder icon while viewed in the Inspector
				EditorGUIUtility.SetIconForObject(gameObject, folderIcon);
				inspectedHierarchyFolders.Add(gameObject);

				bool isPrefabAsset;
				if(gameObject.IsPrefabAsset())
				{
					isPrefabAsset = true;
					isPrefabAssetOrOpenInPrefabStage = true;
				}
				else if(gameObject.IsOpenInPrefabStage())
				{
					isPrefabAsset = false;
					isPrefabAssetOrOpenInPrefabStage = true;
				}
				else
				{
					isPrefabAsset = false;
					isPrefabAssetOrOpenInPrefabStage = false;
				}

				// Don't hide transform in prefabs or prefab instances to avoid internal Unity exceptions.
				// We can still set NotEditable true to prevent the user from making modifications via the inspector.
				if(isPrefabAssetOrOpenInPrefabStage || gameObject.IsConnectedPrefabInstance())
				{
					HandlePrefabOrPrefabInstanceStateLocking(hierarchyFolder, isPrefabAsset);
				}
				else
				{
					HandleSceneObjectStateLocking(hierarchyFolder);
				}
			}

			if(hierarchyFolders.Count == 0)
			{
				return;
			}

			var yOffset = GetYOffset(targets[0]);

			var rect = changeColorRect;
			rect.y += yOffset;

			var setColor = ColorGUIUtility.DrawField(rect, hierarchyFolders[0].Color, showEyedropper:false, showAlpha:false);
			if(setColor != hierarchyFolders[0].Color)
			{
				Undo.RecordObjects(hierarchyFolders.ToArray(), "Hierarchy Folder Color");

				using(var serializedObject = new SerializedObject(hierarchyFolders.ToArray()))
				{
					using var colorProperty = serializedObject.FindProperty(nameof(HierarchyFolder.Color));
					colorProperty.colorValue = setColor;
					serializedObject.ApplyModifiedProperties();
				}

				EditorApplication.RepaintHierarchyWindow();
			}
			
			var backgroundRect = new Rect(40f, 33f + yOffset, Screen.width, 18f);
			GUI.Button(backgroundRect, GUIContent.none, EditorStyles.label);
		}

		private static void OnSelectionChanged()
		{
			var selectedGameObjects = Selection.gameObjects;

			if(Tools.current is Tool.None && restoreActiveTool.HasValue && !Array.Exists(selectedGameObjects, x => x.IsHierarchyFolder()))
			{
				Tools.current = restoreActiveTool.Value;
				restoreActiveTool = null;

				foreach(var sceneViewObject in SceneView.sceneViews)
				{
					if(sceneViewObject is SceneView sceneView && sceneView)
					{
						sceneView.Repaint();
					}
				}
			}

			if(inspectedHierarchyFolders.Count == 0)
			{
				return;
			}

			foreach(var previouslyInspectedHierarchyFolder in inspectedHierarchyFolders)
			{
				if(previouslyInspectedHierarchyFolder
					&& Array.IndexOf(selectedGameObjects, previouslyInspectedHierarchyFolder) == -1)
				{
					// Unset Hierarchy Folder icon, when not viewed in the Inspector, so that it doesn't clutter the scene view
					EditorGUIUtility.SetIconForObject(previouslyInspectedHierarchyFolder, null);
				}
			}

			inspectedHierarchyFolders.Clear();
		}

		private static void OnAfterGameObjectHeaderGUI(GameObject[] targets, Rect headerRect, bool headerIsSelected, bool supportsRichText)
		{
			if(hierarchyFolders.Count == 0)
			{
				return;
			}

			if(!preferences)
			{
				preferences = HierarchyFolderPreferences.Get();
			}

			label.text = isPrefabAssetOrOpenInPrefabStage ? preferences.prefabInfoBoxText : preferences.infoBoxText;
			EditorGUILayout.LabelField(label, EditorStyles.helpBox);

			var yOffset = GetYOffset(targets[0]);
			var drawIconRect = iconRect;
			drawIconRect.y += yOffset;

			var colorWas = GUI.color;
			GUI.color = hierarchyFolders[0].Color;
			GUI.DrawTexture(drawIconRect, preferences.Icon(HierarchyIconType.Default).closed);
			GUI.color = colorWas;

			var backgroundRect = new Rect(40f, yOffset + 33f, Screen.width, 18f);
			EditorGUI.DrawRect(backgroundRect, InspectorBackgroundColor);

			var titleRect = new Rect(66f, yOffset + 32f, Screen.width * 0.5f - 25f, 18f);
			GUI.Label(titleRect, "Hierarchy Folder", EditorStyles.boldLabel);

			if(preferences.activeToggle is HierarchyFolderPreferences.ActiveToggle.Hidden)
			{
				var hideActiveToggleRect = new Rect(42f, yOffset + 8f, 18f, 18f);
				EditorGUI.DrawRect(hideActiveToggleRect, InspectorBackgroundColor);
			}

			hierarchyFolders.Clear();
		}

		private static void SetTag(Object[] targets, bool setForChildren)
		{
			foreach(var target in targets)
			{
				if(target is not GameObject gameObject || !gameObject.IsHierarchyFolder())
				{
					continue;
				}

				if(setForChildren)
				{
					gameObject.SetTagForAllChildren(gameObject.tag);
				}

				gameObject.tag = "Untagged";
			}
		}

		private static void HandlePrefabOrPrefabInstanceStateLocking(HierarchyFolder hierarchyFolder, bool isPrefabAsset)
		{
			var transform = hierarchyFolder.transform;
			transform.hideFlags = HierarchyFolderUtility.GetHierarchyFolderTransformHideFlags(isPrefabAssetOrInstance: true);
			var gameObject = transform.gameObject;
			if(gameObject.layer != DefaultLayer)
			{
				gameObject.layer = DefaultLayer;
			}

			hierarchyFolder.hideFlags = HideFlags.HideInInspector;

			if(!isPrefabAsset)
			{
				return;
			}

			if(HierarchyFolderUtility.HasSupernumeraryComponents(hierarchyFolder))
			{
				HierarchyFolderUtility.UnmakeHierarchyFolder(gameObject, hierarchyFolder);
				return;
			}

			HierarchyFolderUtility.ResetTransformStateWithoutAffectingChildren(transform, false);
		}

		private static void HandleSceneObjectStateLocking(HierarchyFolder hierarchyFolder)
		{
			var transform = hierarchyFolder.transform;
			transform.hideFlags = HierarchyFolderUtility.GetHierarchyFolderTransformHideFlags(isPrefabAssetOrInstance: false);
			hierarchyFolder.hideFlags = HideFlags.HideInInspector;

			if(transform.gameObject.layer != DefaultLayer)
			{
				transform.gameObject.layer = DefaultLayer;
			}
		}
		
		private static float GetYOffset(GameObject target)
		{
			if(PrefabUtility.IsPartOfPrefabAsset(target))
			{
				return 47f;
			}

			return 0f;
		}
	}
}
#endif