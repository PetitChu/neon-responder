using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

namespace BrainlessLabs.Neon.Editor
{
    /// <summary>
    /// Provides a custom editor for the <see cref="UnitSettings"/> script, enabling a tailored inspector view for Unity Editor.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UnitSettings))]
    internal class UnitSettingsEditor : UnityEditor.Editor
    {

        //the dictionary is static to prevent strange behavior when editing settings directly from the Project folder (which causes editor updates to reset these bools)
        public static Dictionary<string, bool> FoldOutList = new Dictionary<string, bool>
        {
            { "linkedComponentsFoldout", false },
            { "movementFoldout", false },
            { "jumpFoldout", false },
            { "attackDataFoldout", false },
            { "comboDataFoldout", false },
            { "knockDownFoldout", false },
            { "throwFoldout", false },
            { "defenceFoldout", false },
            { "grabFoldout", false },
            { "weaponFoldout", false },
            { "unitNameFoldout", false },
            { "fovFoldout", false },
        };

        //cache serialized properties fields (for multi-editing support)
        private SerializedProperty[] _properties;
        private HashSet<string> _linkedComponentFields = new HashSet<string> { "shadowPrefab", "weaponBone", "hitEffect", "hitBox", "spriteRenderer" };
        private HashSet<string> _movementFields = new HashSet<string> { "startDirection", "moveSpeed", "moveSpeedAir", "useAcceleration" };
        private HashSet<string> _accelerationFields = new HashSet<string> { "moveAcceleration", "moveDeceleration" };
        private HashSet<string> _jumpFields = new HashSet<string> { "jumpHeight", "jumpSpeed", "jumpGravity" };
        private HashSet<string> _attackDataFields = new HashSet<string> { "jumpPunch", "jumpKick", "grabPunch", "grabKick", "grabThrow", "groundPunch", "groundKick" };
        private HashSet<string> _comboDataFields = new HashSet<string> { "comboResetTime", "continueComboOnHit" };
        private HashSet<string> _knockdownFields = new HashSet<string> { "knockDownHeight", "knockDownDistance", "knockDownSpeed", "knockDownFloorTime", "hitOtherEnemiesDuringFall", "hitOtherEnemiesWhenFalling" };
        private HashSet<string> _throwFields = new HashSet<string> { "throwHeight", "throwDistance", "hitOtherEnemiesWhenThrown" };
        private HashSet<string> _defenceFieldsPlayer = new HashSet<string> { "canChangeDirWhileDefending", "rearDefenseEnabled" };
        private HashSet<string> _defenceFieldsEnemy = new HashSet<string> { "defendChance", "defendDuration", "rearDefenseEnabled" };
        private HashSet<string> _grabFields = new HashSet<string> { "grabAnimation", "grabPosition", "grabDuration" };
        private HashSet<string> _weaponFields = new HashSet<string> { "loseWeaponWhenHit", "loseWeaponWhenKnockedDown" };
        private HashSet<string> _unitNameFieldsPlayer = new HashSet<string> { "playerId", "unitName", "unitPortrait", "showNameInAllCaps" };
        private HashSet<string> _unitNameFieldsEnemy = new HashSet<string> { "unitName", "showNameInAllCaps", "unitPortrait", "loadRandomNameFromList" };
        private HashSet<string> _fovFields = new HashSet<string> { "enableFOV", "viewDistance", "viewAngle", "viewPosOffset", "showFOVCone", "targetInSight" };

        //icons
        private Texture2D _iconArrowClose;
        private Texture2D _iconArrowOpen;
        private Texture2D _iconInfo;

        //other
        private DIRECTION _prevDirection = DIRECTION.LEFT; //used to keep track of direction changes in the editor
        private string _newline = "\n\n";
        private string _space = "  ";

        private void OnEnable()
        {

            //load icons
            _iconArrowClose = Resources.Load<Texture2D>("IconArrowClose");
            _iconArrowOpen = Resources.Load<Texture2D>("IconArrowOpen");
            _iconInfo = Resources.Load<Texture2D>("IconInfo");

            //Get all serialized properties and cache them
            CacheSerializedProperties();
        }

        public override void OnInspectorGUI()
        {
            var settings = (UnitSettings)target;
            if (settings == null) return;

            //activate undo
            Undo.RecordObject(settings, "Undo change settings");

            //begin checking for changes
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            //draw main content
            MainContent(settings);

            //save changes
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
            }
        }

        private void MainContent(UnitSettings settings)
        {

            //unit type
            DrawPropertyField("unitType");

            //linked components
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(_space + "Linked Components", GetArrow(FoldOutList["linkedComponentsFoldout"])), FoldOutStyle(), GUILayout.ExpandWidth(true), GUILayout.Height(30))) FoldOutList["linkedComponentsFoldout"] = !FoldOutList["linkedComponentsFoldout"];
            if (GUILayout.Button(new GUIContent("", GetInfoIcon()), FoldOutStyle(), GUILayout.Width(50), GUILayout.Height(30))) ShowInfo(0, settings.unitType);
            EditorGUILayout.EndHorizontal();

            if (FoldOutList["linkedComponentsFoldout"])
            {
                EditorGUI.indentLevel++;
                DrawPropertyFields(_linkedComponentFields);
                EditorGUI.indentLevel--;
            }

            //movement Settings
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(_space + "Movement Settings", GetArrow(FoldOutList["movementFoldout"])), FoldOutStyle(), GUILayout.ExpandWidth(true), GUILayout.Height(30))) FoldOutList["movementFoldout"] = !FoldOutList["movementFoldout"];
            if (GUILayout.Button(new GUIContent("", GetInfoIcon()), FoldOutStyle(), GUILayout.Width(50), GUILayout.Height(30))) ShowInfo(1, settings.unitType);
            EditorGUILayout.EndHorizontal();
            if (FoldOutList["movementFoldout"])
            {
                EditorGUI.indentLevel++;
                DrawPropertyFields(_movementFields);

                //rotate unit in the level
                if (_prevDirection != settings.startDirection)
                {
                    settings.transform.localRotation = (settings.startDirection == DIRECTION.LEFT) ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
                    _prevDirection = settings.startDirection;
                }

                if (settings.useAcceleration)
                {
                    EditorGUI.indentLevel++;
                    DrawPropertyFields(_accelerationFields);
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }

            //jump Settings
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(_space + "Jump Settings", GetArrow(FoldOutList["jumpFoldout"])), FoldOutStyle(), GUILayout.ExpandWidth(true), GUILayout.Height(30))) FoldOutList["jumpFoldout"] = !FoldOutList["jumpFoldout"];
            if (GUILayout.Button(new GUIContent("", GetInfoIcon()), FoldOutStyle(), GUILayout.Width(50), GUILayout.Height(30))) ShowInfo(2, settings.unitType);
            EditorGUILayout.EndHorizontal();
            if (FoldOutList["jumpFoldout"])
            {
                EditorGUI.indentLevel++;
                DrawPropertyFields(_jumpFields);
                EditorGUI.indentLevel--;
            }

            //attack Data (Player)
            if (settings.unitType == UNITTYPE.PLAYER)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent(_space + "Attack Data", GetArrow(FoldOutList["attackDataFoldout"])), FoldOutStyle(), GUILayout.ExpandWidth(true), GUILayout.Height(30))) FoldOutList["attackDataFoldout"] = !FoldOutList["attackDataFoldout"];
                if (GUILayout.Button(new GUIContent("", GetInfoIcon()), FoldOutStyle(), GUILayout.Width(50), GUILayout.Height(30))) ShowInfo(3, settings.unitType);
                EditorGUILayout.EndHorizontal();

                //show attack data
                if (FoldOutList["attackDataFoldout"])
                {
                    EditorGUI.indentLevel++;
                    foreach (string attack in _attackDataFields) ShowAttackData(GetPropertyByName(attack), false);
                    EditorGUI.indentLevel--;
                }

                //combo Data
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent(_space + "Combo Data", GetArrow(FoldOutList["comboDataFoldout"])), FoldOutStyle(), GUILayout.ExpandWidth(true), GUILayout.Height(30))) FoldOutList["comboDataFoldout"] = !FoldOutList["comboDataFoldout"];
                if (GUILayout.Button(new GUIContent("", GetInfoIcon()), FoldOutStyle(), GUILayout.Width(50), GUILayout.Height(30))) ShowInfo(4, settings.unitType);
                EditorGUILayout.EndHorizontal();

                if (FoldOutList["comboDataFoldout"])
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.Space(5);
                    ShowHeader("Combo Settings");
                    DrawPropertyFields(_comboDataFields);
                    EditorGUILayout.Space(5);
                    ShowHeader("Combo List");
                    ShowComboData(settings.comboData);
                    EditorGUI.indentLevel--;
                }
            }

            //attack Data (Enemy)
            if (settings.unitType == UNITTYPE.ENEMY)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent(_space + "Attack Data", GetArrow(FoldOutList["attackDataFoldout"])), FoldOutStyle(), GUILayout.ExpandWidth(true), GUILayout.Height(30))) FoldOutList["attackDataFoldout"] = !FoldOutList["attackDataFoldout"];
                if (GUILayout.Button(new GUIContent("", GetInfoIcon()), FoldOutStyle(), GUILayout.Width(50), GUILayout.Height(30))) ShowInfo(3, settings.unitType);
                EditorGUILayout.EndHorizontal();

                if (FoldOutList["attackDataFoldout"])
                {
                    EditorGUI.indentLevel++;
                    if (settings.unitType == UNITTYPE.ENEMY) DrawPropertyField("enemyPauseBeforeAttack");
                    EditorGUILayout.Space(5);
                    ShowHeader("Enemy Attack List");
                    ShowEnemyAttackData();
                    EditorGUI.indentLevel--;
                }
            }

            //knockDown Settings
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(_space + "KnockDown Settings", GetArrow(FoldOutList["knockDownFoldout"])), FoldOutStyle(), GUILayout.ExpandWidth(true), GUILayout.Height(30))) FoldOutList["knockDownFoldout"] = !FoldOutList["knockDownFoldout"];
            if (GUILayout.Button(new GUIContent("", GetInfoIcon()), FoldOutStyle(), GUILayout.Width(50), GUILayout.Height(30))) ShowInfo(5, settings.unitType);
            EditorGUILayout.EndHorizontal();

            if (FoldOutList["knockDownFoldout"])
            {
                EditorGUI.indentLevel++;
                DrawPropertyField("canBeKnockedDown");
                if (settings.canBeKnockedDown) DrawPropertyFields(_knockdownFields);
                EditorGUI.indentLevel--;
            }

            //throw Settings
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(_space + "Throw Settings", GetArrow(FoldOutList["throwFoldout"])), FoldOutStyle(), GUILayout.ExpandWidth(true), GUILayout.Height(30))) FoldOutList["throwFoldout"] = !FoldOutList["throwFoldout"];
            if (GUILayout.Button(new GUIContent("", GetInfoIcon()), FoldOutStyle(), GUILayout.Width(50), GUILayout.Height(30))) ShowInfo(6, settings.unitType);
            EditorGUILayout.EndHorizontal();

            if (FoldOutList["throwFoldout"])
            {
                EditorGUI.indentLevel++;
                DrawPropertyFields(_throwFields);
                EditorGUI.indentLevel--;
            }

            //defence Settings
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(_space + "Defence Settings", GetArrow(FoldOutList["defenceFoldout"])), FoldOutStyle(), GUILayout.ExpandWidth(true), GUILayout.Height(30))) FoldOutList["defenceFoldout"] = !FoldOutList["defenceFoldout"];
            if (GUILayout.Button(new GUIContent("", GetInfoIcon()), FoldOutStyle(), GUILayout.Width(50), GUILayout.Height(30))) ShowInfo(7, settings.unitType);
            EditorGUILayout.EndHorizontal();

            if (FoldOutList["defenceFoldout"])
            {
                EditorGUI.indentLevel++;
                if (settings.unitType == UNITTYPE.ENEMY) DrawPropertyFields(_defenceFieldsEnemy);
                else if (settings.unitType == UNITTYPE.PLAYER) DrawPropertyFields(_defenceFieldsPlayer);
                EditorGUI.indentLevel--;
            }

            //grab Settings 
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(_space + "Grab Settings", GetArrow(FoldOutList["grabFoldout"])), FoldOutStyle(), GUILayout.ExpandWidth(true), GUILayout.Height(30))) FoldOutList["grabFoldout"] = !FoldOutList["grabFoldout"];
            if (GUILayout.Button(new GUIContent("", GetInfoIcon()), FoldOutStyle(), GUILayout.Width(50), GUILayout.Height(30))) ShowInfo(8, settings.unitType);
            EditorGUILayout.EndHorizontal();

            if (FoldOutList["grabFoldout"])
            {
                EditorGUI.indentLevel++;
                if (settings.unitType == UNITTYPE.PLAYER) DrawPropertyFields(_grabFields);
                if (settings.unitType == UNITTYPE.ENEMY) DrawPropertyField("canBeGrabbed");
                EditorGUI.indentLevel--;
            }

            //weapon Settings
            if (settings.unitType == UNITTYPE.PLAYER)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent(_space + "Weapon Settings", GetArrow(FoldOutList["weaponFoldout"])), FoldOutStyle(), GUILayout.ExpandWidth(true), GUILayout.Height(30))) FoldOutList["weaponFoldout"] = !FoldOutList["weaponFoldout"];
                if (GUILayout.Button(new GUIContent("", GetInfoIcon()), FoldOutStyle(), GUILayout.Width(50), GUILayout.Height(30))) ShowInfo(9, settings.unitType);
                EditorGUILayout.EndHorizontal();

                if (FoldOutList["weaponFoldout"])
                {
                    EditorGUI.indentLevel++;
                    DrawPropertyFields(_weaponFields);
                    EditorGUI.indentLevel--;
                }
            }

            //unit Name
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(_space + "Unit Name & Portrait", GetArrow(FoldOutList["unitNameFoldout"])), FoldOutStyle(), GUILayout.ExpandWidth(true), GUILayout.Height(30))) FoldOutList["unitNameFoldout"] = !FoldOutList["unitNameFoldout"];
            if (GUILayout.Button(new GUIContent("", GetInfoIcon()), FoldOutStyle(), GUILayout.Width(50), GUILayout.Height(30))) ShowInfo(10, settings.unitType);
            EditorGUILayout.EndHorizontal();

            if (FoldOutList["unitNameFoldout"])
            {
                EditorGUI.indentLevel++;
                if (settings.unitType == UNITTYPE.PLAYER) DrawPropertyFields(_unitNameFieldsPlayer);
                if (settings.unitType == UNITTYPE.ENEMY)
                {
                    DrawPropertyFields(_unitNameFieldsEnemy);
                    if (settings.loadRandomNameFromList) DrawPropertyField("unitNamesList");
                }

                EditorGUI.indentLevel--;
            }

            //fov Settings
            if (settings.unitType == UNITTYPE.ENEMY)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent(_space + "Field Of View Settings", GetArrow(FoldOutList["fovFoldout"])), FoldOutStyle(), GUILayout.ExpandWidth(true), GUILayout.Height(30))) FoldOutList["fovFoldout"] = !FoldOutList["fovFoldout"];
                if (GUILayout.Button(new GUIContent("", GetInfoIcon()), FoldOutStyle(), GUILayout.Width(50), GUILayout.Height(30))) ShowInfo(11, settings.unitType);
                EditorGUILayout.EndHorizontal();
                if (FoldOutList["fovFoldout"])
                {
                    EditorGUI.indentLevel++;
                    DrawPropertyFields(_fovFields);
                    EditorGUI.indentLevel--;
                }
            }
        }

        //visualize AttackData
        private void ShowEnemyAttackData()
        {

            //access serializedProperty enemyAttackList
            SerializedProperty enemyAttackListProperty = serializedObject.FindProperty("enemyAttackList");

            //check if this list has elements
            if (enemyAttackListProperty != null && enemyAttackListProperty.isArray)
            {

                //show message when there are no items
                if (enemyAttackListProperty.arraySize == 0) EditorGUILayout.LabelField("No Enemy Attack Data Available");

                //show list with attack data
                for (int i = 0; i < enemyAttackListProperty.arraySize; i++)
                {
                    SerializedProperty attackDataProperty = enemyAttackListProperty.GetArrayElementAtIndex(i);
                    ShowAttackData(attackDataProperty, true);
                }

                //footer buttons
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(" ", GUILayout.Width(17));

                //button -
                if (enemyAttackListProperty.arraySize > 0)
                {
                    //only show + button when there is at least 1 (or more) item(s)
                    if (GUILayout.Button("-", SmallButtonStyle())) enemyAttackListProperty.DeleteArrayElementAtIndex(enemyAttackListProperty.arraySize - 1); //remove last item
                }

                //button +
                if (GUILayout.Button("+", SmallButtonStyle(), GUILayout.Width(25))) enemyAttackListProperty.InsertArrayElementAtIndex(enemyAttackListProperty.arraySize);

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(10);
            }
        }

        //visualize the combo data
        private void ShowComboData(List<Combo> comboList)
        {
            if (comboList.Count == 0) EditorGUILayout.LabelField("No Combo Data Available");

            //create list of combos
            foreach (Combo combo in comboList)
            {

                combo.foldout = EditorGUILayout.Foldout(combo.foldout, combo.comboName, true);
                if (combo.foldout)
                {

                    //name field
                    combo.comboName = EditorGUILayout.TextField("Combo Name:", combo.comboName);

                    //attack sequence title
                    ShowHeader("Attack Sequence");

                    //list of attacks
                    if (combo.attackSequence.Count == 0) EditorGUILayout.LabelField("This combo does not have any attacks listed");
                    foreach (AttackData data in combo.attackSequence)
                    {
                        EditorGUI.indentLevel++;
                        data.foldout = EditorGUILayout.Foldout(data.foldout, data.name, true);
                        if (data.foldout)
                        {
                            data.name = EditorGUILayout.TextField("Attack Name:", data.name);
                            data.damage = EditorGUILayout.IntField("Damage", data.damage);
                            data.sfx = EditorGUILayout.TextField("Sfx (on hit)", data.sfx);
                            data.animationState = EditorGUILayout.TextField("Animation State", data.animationState);
                            data.attackType = (ATTACKTYPE)EditorGUILayout.EnumPopup("Attack Type", data.attackType);
                            data.knockdown = EditorGUILayout.Toggle("Knockdown", data.knockdown);
                            GUILayout.Space(10);
                        }

                        EditorGUI.indentLevel--;
                    }

                    //footer buttons
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(" ", GUILayout.Width(17));

                    //button -
                    if (comboList.Count > 0)
                        if (GUILayout.Button("-", SmallButtonStyle()))
                            combo.attackSequence.RemoveAt(combo.attackSequence.Count - 1);

                    //button +
                    if (GUILayout.Button("+", SmallButtonStyle(), GUILayout.Width(25))) combo.attackSequence.Add(new AttackData("[New Attack]", 0, null, ATTACKTYPE.NONE, false));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(10);
                }
            }

            //combo footer buttons
            EditorGUILayout.Space(15);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Combo", GUILayout.Width(200), GUILayout.Height(25))) comboList.Add(new Combo());
            if (comboList.Count > 0)
                if (GUILayout.Button("Remove Combo", GUILayout.Width(200), GUILayout.Height(25)))
                    comboList.RemoveAt(comboList.Count - 1);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        //visualize attack data
        private void ShowAttackData(SerializedProperty property, bool showName)
        {
            EditorGUI.indentLevel++;

            SerializedProperty foldout = property.FindPropertyRelative("foldout");
            SerializedProperty nameProp = property.FindPropertyRelative("name");

            //set name of foldout item
            if (showName)
            {
                string foldoutLabel = nameProp != null ? nameProp.stringValue : ObjectNames.NicifyVariableName(property.name);
                foldout.boolValue = EditorGUILayout.Foldout(foldout.boolValue, new GUIContent(foldoutLabel), true);
            }
            else
            {
                foldout.boolValue = EditorGUILayout.Foldout(foldout.boolValue, property.name, true);
            }

            if (foldout.boolValue)
            {
                SerializedProperty damageProp = property.FindPropertyRelative("damage");
                SerializedProperty animationStateProp = property.FindPropertyRelative("animationState");
                SerializedProperty sfxProp = property.FindPropertyRelative("sfx");
                SerializedProperty attackTypeProp = property.FindPropertyRelative("attackType");
                SerializedProperty knockdownProp = property.FindPropertyRelative("knockdown");

                if (showName) EditorGUILayout.PropertyField(nameProp, new GUIContent("Attack Name:"));
                EditorGUILayout.PropertyField(damageProp, new GUIContent("Damage"));
                EditorGUILayout.PropertyField(animationStateProp, new GUIContent("Animation State"));
                EditorGUILayout.PropertyField(sfxProp, new GUIContent("sfx"));
                EditorGUILayout.PropertyField(attackTypeProp, new GUIContent("Attack Type"));
                EditorGUILayout.PropertyField(knockdownProp, new GUIContent("Knockdown"));
                GUILayout.Space(10);
            }

            EditorGUI.indentLevel--;
        }

        //caches all serialized properties (for multi editing support)
        private void CacheSerializedProperties()
        {
            var targetType = typeof(UnitSettings);
            var fields = targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _properties = new SerializedProperty[fields.Length];
            for (int i = 0; i < fields.Length; i++) _properties[i] = serializedObject.FindProperty(fields[i].Name);
        }

        //draw a list of property fields
        public void DrawPropertyFields(HashSet<string> propertyHash)
        {
            foreach (var property in _properties)
            {
                if (property != null && propertyHash.Contains(property.name))
                {

                    //check if the property is of type Sprite
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is Sprite)
                    {

                        //draw the sprite field with a thumbnail icon
                        property.objectReferenceValue = (Sprite)EditorGUILayout.ObjectField(
                            new GUIContent(ObjectNames.NicifyVariableName(property.name)),
                            property.objectReferenceValue,
                            typeof(Sprite),
                            allowSceneObjects: false);

                    }
                    else
                    {

                        //draw the field normally for other types
                        EditorGUILayout.PropertyField(property, new GUIContent(ObjectNames.NicifyVariableName(property.name)));
                    }
                }
            }
        }

        //returns cached property
        public SerializedProperty GetPropertyByName(string propertyName)
        {
            foreach (var property in _properties)
                if (property != null && property.name == propertyName)
                    return property;
            return null;
        }

        //draw one property field
        public void DrawPropertyField(string propertyName)
        {
            DrawPropertyFields(new HashSet<string> { propertyName });
        }

        //header
        private void ShowHeader(string label)
        {
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.wordWrap = true;
            style.richText = true;
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = 13;
            style.richText = true;
            style.padding = new RectOffset(16, 0, 4, 0);
            GUILayout.Label(label, style);
        }

        //GUIStyle for foldout buttons
        private GUIStyle FoldOutStyle()
        {
            bool isDarkMode = EditorGUIUtility.isProSkin;
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.alignment = TextAnchor.MiddleLeft;
            style.fixedHeight = 32;
            style.stretchWidth = true;
            style.padding = new RectOffset(12, 10, 0, 0);
            style.margin = new RectOffset(0, 0, 5, 5);
            style.normal.background = MakeTex(1, 1, new Color(1f, 1f, 1f, isDarkMode ? 0.1f : 0.2f));
            style.normal.textColor = isDarkMode ? Color.white : Color.black;
            return style;
        }

        //GUIStyle for small + - Buttons
        private GUIStyle SmallButtonStyle()
        {
            bool isDarkMode = EditorGUIUtility.isProSkin;
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.fixedHeight = 22;
            style.fixedWidth = 22;
            style.fontSize = 18;
            style.padding = new RectOffset(2, 2, 2, 2);
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.background = MakeTex(1, 1, new Color(1f, 1f, 1f, isDarkMode ? 0.1f : 0.2f));
            style.normal.textColor = isDarkMode ? Color.white : Color.black;
            return style;
        }

        //creates a background texture for foldout button
        private Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        //returns a proper arrow icon
        private Texture2D GetArrow(bool isFoldedOut)
        {
            if (_iconArrowClose == null || _iconArrowOpen == null) return null;
            else return isFoldedOut ? _iconArrowOpen : _iconArrowClose;
        }

        //returns a proper arrow icon
        private Texture2D GetInfoIcon()
        {
            if (_iconInfo == null) return null;
            else return _iconInfo;
        }

        //shortcut to highlight items
        private string HighlightItem(string label, int size = 13)
        {
            return "<b><size=" + size + "><color=#FFFFFF>" + label + "</color></size></b>";
        }

        //shows documentation when the user presses the ? icon
        public void ShowInfo(int id, UNITTYPE unitType)
        {
            string title = "";
            string content = "";

            switch (id)
            {
                case 0:
                    title = "Linked Components";
                    content = "This section contains links to several components and external references used by this unit. Below is a description of each item:" + _newline;
                    content += HighlightItem("Shadow Prefab: ") + "A shadow sprite positioned at the base of the unit. (Optional)" + _newline;
                    content += HighlightItem("Weapon Bone: ") + "A transform that represents the position of the unit's hand. When a weapon is picked up, it will be parented to the weapon bone." + _newline;
                    content += HighlightItem("Hit Effect: ") + "An effect displayed when the unit is hit. (Optional)" + _newline;
                    content += HighlightItem("Hitbox: ") + "A link to a sprite (red box) representing the hit area during an attack animation." + _newline;
                    content += HighlightItem("Sprite Renderer: ") + "A Link to the sprite of this unit." + _newline;
                    break;

                case 1:
                    title = "Movement Settings";
                    content = "These values define how fast a unit moves around a level. Here�s what each term means:" + _newline;
                    content += HighlightItem("Start Direction: ") + "The direction the unit faces at the beginning of a level." + _newline;
                    content += HighlightItem("Move Speed: ") + "The unit's running speed when moving across the level." + _newline;
                    content += HighlightItem("Move Speed Air: ") + "The unit�s speed while in the air during a jump." + _newline;
                    content += HighlightItem("Use Acceleration: ") + "Option to enable or disable gradual speed changes." + _newline;
                    content += HighlightItem("Move Acceleration: ") + "The rate at which the unit's speed increases when accelerating." + _newline;
                    content += HighlightItem("Move Deceleration: ") + "The rate at which the unit's speed decreases when slowing down." + _newline;
                    break;

                case 2:
                    title = "Jump Settings";
                    content = "These values define the jump behaviour of a unit." + _newline;
                    content += HighlightItem("Jump Height: ") + "The height of a jump" + _newline;
                    content += HighlightItem("Jump Speed: ") + "The speed of the jump simulation" + _newline;
                    content += HighlightItem("Gravity: ") + "The strength of gravitational force applied to the character during a jump." + _newline;
                    break;

                case 3:
                    title = "Attack Data";
                    content = "This section provides a list of attack details, where you can modify data such as damage, animation, attack type, and other data for each attack." + _newline;
                    content += HighlightItem("Damage: ") + "The amount of Health Points subtracted from the enemy's health bar." + _newline;
                    content += HighlightItem("Animation State: ") + "The animation state that needs to be played on this unit's Animator component." + _newline;
                    content += HighlightItem("Attack Type: ") + "The attack type of this attack." + _newline;
                    content += HighlightItem("Knockdown: ") + "Indicates if a successful hit causes the enemy to be knocked down." + _newline;
                    break;

                case 4:
                    title = "Combo Settings";
                    content = "The combo section allows you to configure and manage sequential attacks. Here, you can set up a series of moves that will be executed in a specific order, creating a combo." + _newline;
                    content += HighlightItem("Combo Reset Time: ") + "If the player presses a button within this time window, it will count as part of the combo sequence." + _newline;
                    content += HighlightItem("Continue Combo On Hit: ") + "Option to only proceed with the combo if the attack connects; otherwise, restart the combo sequence." + _newline;
                    content += HighlightItem("Attack Sequence") + "\n";

                    content += "For each combo attack you can modify data such as damage, animation, attack type:" + _newline;
                    content += HighlightItem("Attack Name: ") + "The name of this attack." + _newline;
                    content += HighlightItem("Damage: ") + "The amount of Health Points subtracted from the enemy's health bar." + _newline;
                    content += HighlightItem("Animation State: ") + "The animation state that needs to be played on this unit's Animator component." + _newline;
                    content += HighlightItem("Attack Type: ") + "The attack type of this attack." + _newline;
                    content += HighlightItem("Knockdown: ") + "Indicates if a successful hit causes the enemy to be knocked down." + _newline;
                    break;

                case 5:
                    title = "Knockdown Settings";
                    content = "These values determine how a unit behaves when knocked down:" + _newline;
                    content += HighlightItem("Can Be Knocked Down: ") + "Whether or not this unit can be knocked down." + _newline;
                    content += HighlightItem("Knockdown Height: ") + "The height to which a unit is propelled upward when knocked down." + _newline;
                    content += HighlightItem("Knockdown Distance: ") + "The distance a unit is pushed backward during a knockdown." + _newline;
                    content += HighlightItem("Knockdown Speed: ") + "The speed of the Knockdown simulation." + _newline;
                    content += HighlightItem("Knockdown Floor Time: ") + "The duration a unit remains on the ground before getting back up." + _newline;
                    break;

                case 6:
                    title = "Throw Settings";
                    content = "These values determine how a unit behaves when being thrown:" + _newline;
                    content += HighlightItem("Throw Height: ") + "The height to which a unit is propelled upward when being thrown." + _newline;
                    content += HighlightItem("Throw Distance: ") + "The distance a unit travels while in the air after being thrown." + _newline;
                    break;

                case 7:
                    title = "Defence Settings";
                    content = "These values determine how a unit behaves while defending:" + _newline;
                    if (unitType == UNITTYPE.PLAYER) content += HighlightItem("Can Change Dir While Defending: ") + "Enable or disable the ability for the player to change direction while holding the defence button." + _newline;
                    if (unitType == UNITTYPE.ENEMY) content += HighlightItem("Defend Chance: ") + "The probability (0 - 100) that the enemy will successfully defend against an incoming attack." + _newline;
                    if (unitType == UNITTYPE.ENEMY) content += HighlightItem("Defend Duration: ") + "The amount of time an enemy remains in the defence state after initiating defense." + _newline;
                    content += HighlightItem("Rear Defense Enabled: ") + "Determines whether this unit can defend against attacks coming from behind." + _newline;
                    break;

                case 8:
                    title = "Grab Settings";
                    content = "These values determine how a player behaves when grabbing and holding an enemy:" + _newline;
                    content += HighlightItem("Grab Animation: ") + "The name of the animation state that contains the Grab Animation in this unit's Animator component" + _newline;
                    content += HighlightItem("Grab Position: ") + "The position this unit moves to while grabbing, relative to the enemy it is holding." + _newline;
                    content += HighlightItem("Grab Duration: ") + "The duration of the grab, before this unit and it's target return back to normal." + _newline;
                    break;

                case 9:
                    title = "Weapon Settings";
                    content = "\n";
                    content += HighlightItem("Lose Weapon When Hit: ") + "Specifies whether the unit should drop the currently equipped weapon when hit." + _newline;
                    content += HighlightItem("Lose Weapon When Knocked Down: ") + "Determines whether the unit retains or drops the currently equipped weapon when knocked down." + _newline;
                    break;

                case 10:
                    title = "Unit Name & Portrait";
                    content = "\n";
                    if (unitType == UNITTYPE.ENEMY) content += HighlightItem("Load Random Name From List: ") + "Option to load a random enemy name from a txt file." + _newline;
                    if (unitType == UNITTYPE.PLAYER) content += HighlightItem("Player Id: ") + "A variable used to identify this unit." + _newline;
                    if (unitType == UNITTYPE.PLAYER) content += HighlightItem("Unit Name: ") + "The unit's name as shown in the top left corner near the health bar." + _newline;
                    if (unitType == UNITTYPE.ENEMY) content += HighlightItem("Unit Name: ") + "The unit's name as shown in the top near the enemy health bar." + _newline;
                    content += HighlightItem("Show name in all caps: ") + "Determines whether the name should be displayed in capital letters." + _newline;
                    content += HighlightItem("Unit Portrait: ") + "The unit portrait sprite is a small icon displayed at the top, near the health bar." + _newline;

                    break;
                case 11:
                    title = "Field Of View Settings";
                    content = "These values determine whether an enemy detects the player when they enter its Field Of View (FOV)." + _newline;
                    content += HighlightItem("Enable FOV: ") + "Enable or disable the Field Of View. When disabled a unit always spots the player by default." + _newline;
                    content += HighlightItem("View Distance: ") + "How far this unit can see." + _newline;
                    content += HighlightItem("View Angle: ") + "How wide this unit can see." + _newline;
                    content += HighlightItem("View position Offset: ") + "The starting position (eye level) of the view cone, " + _newline;
                    content += HighlightItem("Show FOV Cone in Editor: ") + "Useful for debugging, this option displays the field of view cone in the Unity Editor." + _newline;
                    content += HighlightItem("Target in Sight: ") + "A read-only value for debugging that indicates whether the target has been spotted." + _newline;
                    break;

            }

            CustomWindow.ShowWindow(title, content, new Vector2(600, 500));
        }
    }
}