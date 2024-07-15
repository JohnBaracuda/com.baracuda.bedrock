using System;
using System.Collections.Generic;
using Baracuda.Bedrock.PlayerLoop;
using Baracuda.Utilities;
using Baracuda.Utilities.Editor.Inspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using GUIUtility = Baracuda.Utilities.Editor.Helper.GUIUtility;
using Object = UnityEngine.Object;

namespace Baracuda.Bedrock.Editor.Windows
{
    public abstract class ProjectEditorWindow : OdinEditorWindow
    {
        #region Settings

        private FoldoutHandler Foldout { get; set; }
        private Vector2 _scrollPosition;
        private readonly List<Action> _headerInstructions = new(8);
        private readonly List<Action> _footerInstructions = new(8);
        private readonly List<Action> _instructions = new(32);
        private readonly List<UnityEditor.Editor> _editorCache = new(32);
        private bool _initialized;
        private bool? _drawOptions;
        private bool? _drawTitles;
        private bool? _saveSearchQuery;
        private string _searchQuery;
        private readonly List<(GUIContent name, string key)> _options = new();
        private string _currentTitle;

        #endregion


        #region Settings

        private bool DrawOptions
        {
            get => _drawOptions ??= UnityEditor.SessionState.GetBool(nameof(_drawOptions), false);
            set
            {
                _drawOptions = value;
                UnityEditor.SessionState.SetBool(nameof(_drawOptions), value);
            }
        }

        private bool DrawTitles
        {
            get => _drawTitles ??= UnityEditor.EditorPrefs.GetBool(nameof(_drawTitles), true);
            set
            {
                _drawTitles = value;
                UnityEditor.EditorPrefs.SetBool(nameof(_drawTitles), value);
            }
        }

        private bool SaveSearchQuery
        {
            get => _saveSearchQuery ??= UnityEditor.EditorPrefs.GetBool(nameof(_saveSearchQuery), true);
            set
            {
                _saveSearchQuery = value;
                UnityEditor.EditorPrefs.SetBool(nameof(_saveSearchQuery), value);
            }
        }

        private string SearchQuery
        {
            get
            {
                _searchQuery ??= SaveSearchQuery ? UnityEditor.EditorPrefs.GetString(nameof(_searchQuery), null) : null;
                return _searchQuery;
            }
            set
            {
                _searchQuery = value;
                if (SaveSearchQuery)
                {
                    UnityEditor.EditorPrefs.SetString(nameof(_searchQuery), _searchQuery);
                }
            }
        }

        #endregion


        #region Setup

        protected async override void OnEnable()
        {
            base.OnEnable();
            await Gameloop.DelayedCallAsync();
            InitializeEditor();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Foldout?.SaveState();
            _headerInstructions.Clear();
            _footerInstructions.Clear();
            _instructions.Clear();
            _editorCache.Clear();
            _options.Clear();
            _initialized = false;
        }

        private void InitializeEditor()
        {
            foreach (var editor in _editorCache)
            {
                DestroyImmediate(editor);
            }

            Foldout = new FoldoutHandler(GetType().Name)
            {
                DefaultState = false
            };

            _options.Clear();
            _instructions.Clear();
            _headerInstructions.Clear();
            SetupEditor();
            _initialized = true;
        }

        protected abstract void SetupEditor();

        #endregion


        #region GUI

        protected override void OnImGUI()
        {
            if (!_initialized)
            {
                return;
            }

            var foldoutStyle = FoldoutHandler.Style;
            DrawHeader();
            DrawBody();
            DrawFooter(foldoutStyle);
        }

        private void DrawHeader()
        {
            FoldoutHandler.Style = FoldoutStyle.Dark;

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Space(8);
            SearchQuery = GUIUtility.SearchBar(SearchQuery);
            GUILayout.EndVertical();

            if (GUIUtility.ClearButton())
            {
                SearchQuery = string.Empty;
                GUI.FocusControl(null);
            }

            if (GUIUtility.OptionsButton())
            {
                DrawOptions = !DrawOptions;
            }

            if (GUIUtility.RefreshButton())
            {
                InitializeEditor();
            }

            GUILayout.EndHorizontal();

            if (DrawOptions)
            {
                DrawTitles = UnityEditor.EditorGUILayout.ToggleLeft("Show Titles", DrawTitles);
                SaveSearchQuery = UnityEditor.EditorGUILayout.ToggleLeft("Save Search Filter", SaveSearchQuery);

                foreach (var option in _options)
                {
                    var current = UnityEditor.EditorPrefs.GetBool(option.key);
                    var result = UnityEditor.EditorGUILayout.ToggleLeft(option.name, current);
                    UnityEditor.EditorPrefs.SetBool(option.key, result);
                }
            }

            foreach (var instruction in _headerInstructions)
            {
                try
                {
                    instruction();
                }
                catch (Exception exception)
                {
                    GUIUtility.DrawException(exception);
                }
            }

            GUIUtility.DrawLine();
        }

        private void DrawBody()
        {
            _scrollPosition = UnityEditor.EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var instruction in _instructions)
            {
                instruction();
            }

            UnityEditor.EditorGUILayout.Space();
            UnityEditor.EditorGUILayout.EndScrollView();
        }

        private void DrawFooter(FoldoutStyle foldoutStyle)
        {
            foreach (var instruction in _footerInstructions)
            {
                instruction();
            }

            FoldoutHandler.Style = foldoutStyle;
        }

        #endregion


        #region Editor Setup & Cache

        protected void AddHeaderInstruction(Action instruction)
        {
            _headerInstructions.AddUnique(instruction);
        }

        protected void AddFooterInstruction(Action instruction)
        {
            _footerInstructions.AddUnique(instruction);
        }

        protected void AddEditorGroup<T>(Func<List<T>> group, string editorTitle) where T : Object
        {
            (UnityEditor.Editor editor, string name)[] editors = null;

            Action init = () =>
            {
                var list = group();
                editors = new (UnityEditor.Editor editor, string name)[list.Count];
                for (var i = 0; i < list.Count; i++)
                {
                    var editor = UnityEditor.Editor.CreateEditor(list[i]);
                    _editorCache.Add(editor);
                    editors[i] = (editor, editor.target.name.Humanize());
                }
            };

            var foldout = new FoldoutHandler(typeof(T).Name)
            {
                DefaultState = false
            };

            var currentTitle = _currentTitle;
            _instructions.Add(() =>
            {
                if (SearchQuery.IsNotNullOrWhitespace()
                    && !editorTitle.ContainsIgnoreCase(SearchQuery)
                    && !currentTitle.ContainsIgnoreCase(SearchQuery))
                {
                    return;
                }
                var foldoutStyle = FoldoutHandler.Style;
                FoldoutHandler.Style = FoldoutStyle.Dark;
                if (Foldout[editorTitle])
                {
                    init?.Invoke();
                    init = null;
                    UnityEditor.EditorGUIUtility.wideMode = false;
                    UnityEditor.EditorGUIUtility.labelWidth = UnityEditor.EditorGUIUtility.currentViewWidth * 0.4f;
                    foreach (var (editor, displayName) in editors)
                    {
                        FoldoutHandler.Style = FoldoutStyle.Default;
                        if (foldout[displayName])
                        {
                            if (editor.serializedObject.targetObject == null)
                            {
                                UnityEditor.EditorGUILayout.HelpBox("Target is null!", UnityEditor.MessageType.Error);
                                return;
                            }

                            GUIUtility.Space();
                            UnityEditor.EditorGUI.indentLevel += 2;

                            FoldoutHandler.Style = FoldoutStyle.Default;
                            editor.serializedObject.Update();
                            editor.OnInspectorGUI();
                            editor.serializedObject.ApplyModifiedProperties();
                            FoldoutHandler.Style = foldoutStyle;
                            UnityEditor.EditorGUI.indentLevel -= 2;
                            GUIUtility.Space();
                            GUIUtility.DrawFullLine();
                        }
                    }

                    GUIUtility.Space();
                }

                foldout.SaveState();
            });
        }

        protected void AddTitle(string titleName, bool drawLine = true)
        {
            _currentTitle = titleName;
            _instructions.Add(() =>
            {
                if (!DrawTitles)
                {
                    return;
                }
                if (SearchQuery.IsNotNullOrWhitespace() && !titleName.ContainsIgnoreCase(SearchQuery))
                {
                    return;
                }

                if (drawLine)
                {
                    if (Event.current.type != EventType.Repaint)
                    {
                        var lastRect = GUILayoutUtility.GetLastRect();
                        UnityEditor.EditorGUI.DrawRect(
                            new Rect(0, lastRect.y, UnityEditor.EditorGUIUtility.currentViewWidth, 1),
                            new Color(0f, 0f, 0f, 0.3f));
                    }
                }

                UnityEditor.EditorGUILayout.Space();
                GUILayout.Label(titleName, new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16
                });
                UnityEditor.EditorGUILayout.Space(3);
            });
        }

        protected void AddEditor<T>(Func<T> getFunc, string editorTitle) where T : Object
        {
            var editor = default(UnityEditor.Editor);

            _instructions.Add(() =>
            {
                if (SearchQuery.IsNotNullOrWhitespace() && !editorTitle.ContainsIgnoreCase(SearchQuery))
                {
                    return;
                }
                var foldoutStyle = FoldoutHandler.Style;
                FoldoutHandler.Style = FoldoutStyle.Dark;
                if (Foldout[editorTitle])
                {
                    editor ??= UnityEditor.Editor.CreateEditor(getFunc());
                    if (editor == null)
                    {
                        UnityEditor.EditorGUILayout.HelpBox("Target is null!", UnityEditor.MessageType.Error);
                        return;
                    }
                    GUIUtility.Space();
                    FoldoutHandler.Style = FoldoutStyle.Default;
                    UnityEditor.EditorGUIUtility.wideMode = false;
                    UnityEditor.EditorGUI.indentLevel++;
                    UnityEditor.EditorGUIUtility.labelWidth = UnityEditor.EditorGUIUtility.currentViewWidth * 0.4f;
                    editor.serializedObject.Update();
                    editor.OnInspectorGUI();
                    editor.serializedObject.ApplyModifiedProperties();
                    UnityEditor.EditorGUI.indentLevel--;
                    FoldoutHandler.Style = foldoutStyle;
                    GUIUtility.Space();
                    GUIUtility.DrawFullLine();
                }
            });
        }

        protected void AddOptionalEditorGroup<T>(Func<List<T>> group, string editorTitle, string optionName,
            string tooltip = null, bool showByDefault = false) where T : Object
        {
            (UnityEditor.Editor editor, string name)[] editors = null;

            Action init = () =>
            {
                var list = group();
                editors = new (UnityEditor.Editor editor, string name)[list.Count];
                for (var i = 0; i < list.Count; i++)
                {
                    var editor = UnityEditor.Editor.CreateEditor(list[i]);
                    _editorCache.Add(editor);
                    editors[i] = (editor, editor.target.name.Humanize());
                }
            };

            var optionKey = $"custom_editor_{optionName}";
            AddOptions((new GUIContent(optionName, tooltip), optionKey));
            if (!UnityEditor.EditorPrefs.HasKey(optionKey))
            {
                UnityEditor.EditorPrefs.SetBool(optionKey, showByDefault);
            }

            var currentTitle = _currentTitle;
            _instructions.Add(() =>
            {
                if (!UnityEditor.EditorPrefs.GetBool(optionKey, showByDefault))
                {
                    return;
                }
                if (SearchQuery.IsNotNullOrWhitespace()
                    && !editorTitle.ContainsIgnoreCase(SearchQuery)
                    && !currentTitle.ContainsIgnoreCase(SearchQuery))
                {
                    return;
                }
                var foldoutStyle = FoldoutHandler.Style;
                FoldoutHandler.Style = FoldoutStyle.Dark;
                if (Foldout[editorTitle])
                {
                    init?.Invoke();
                    init = null;
                    UnityEditor.EditorGUIUtility.wideMode = false;
                    UnityEditor.EditorGUIUtility.labelWidth = UnityEditor.EditorGUIUtility.currentViewWidth * 0.4f;
                    foreach (var (editor, displayName) in editors)
                    {
                        FoldoutHandler.Style = FoldoutStyle.Default;
                        if (Foldout[displayName])
                        {
                            if (editor.serializedObject.targetObject == null)
                            {
                                UnityEditor.EditorGUILayout.HelpBox("Target is null!", UnityEditor.MessageType.Error);
                                return;
                            }
                            GUIUtility.Space();
                            UnityEditor.EditorGUI.indentLevel += 2;
                            editor.serializedObject.Update();
                            editor.OnInspectorGUI();
                            editor.serializedObject.ApplyModifiedProperties();
                            UnityEditor.EditorGUI.indentLevel -= 2;
                            GUIUtility.Space();
                            GUIUtility.DrawFullLine();
                        }
                    }
                    GUIUtility.Space();
                }
                FoldoutHandler.Style = foldoutStyle;
                Foldout.SaveState();
            });
        }

        protected void __AddEditor<T>(T target, string editorTitle) where T : Object
        {
            if (target == null)
            {
                return;
            }

            var editor = UnityEditor.Editor.CreateEditor(target);
            _editorCache.Add(editor);

            if (editor != null)
            {
                _instructions.Add(() =>
                {
                    if (SearchQuery.IsNotNullOrWhitespace() && !editorTitle.ContainsIgnoreCase(SearchQuery))
                    {
                        return;
                    }
                    var foldoutStyle = FoldoutHandler.Style;
                    FoldoutHandler.Style = FoldoutStyle.Dark;
                    if (Foldout[editorTitle])
                    {
                        if (editor.serializedObject.targetObject == null)
                        {
                            UnityEditor.EditorGUILayout.HelpBox("Target is null!", UnityEditor.MessageType.Error);
                            return;
                        }

                        GUIUtility.Space();
                        FoldoutHandler.Style = FoldoutStyle.Default;
                        UnityEditor.EditorGUIUtility.wideMode = false;
                        UnityEditor.EditorGUI.indentLevel++;
                        UnityEditor.EditorGUIUtility.labelWidth = UnityEditor.EditorGUIUtility.currentViewWidth * 0.4f;
                        editor.serializedObject.Update();
                        editor.OnInspectorGUI();
                        editor.serializedObject.ApplyModifiedProperties();
                        UnityEditor.EditorGUI.indentLevel--;
                        FoldoutHandler.Style = foldoutStyle;
                        GUIUtility.Space();
                        GUIUtility.DrawFullLine();
                    }
                });
            }
            else
            {
                _instructions.Add(() =>
                {
                    if (SearchQuery.IsNotNullOrWhitespace() && !editorTitle.ContainsIgnoreCase(SearchQuery))
                    {
                        return;
                    }
                    if (Foldout[editorTitle])
                    {
                        UnityEditor.EditorGUIUtility.wideMode = false;
                        UnityEditor.EditorGUI.indentLevel++;
                        UnityEditor.EditorGUIUtility.labelWidth = UnityEditor.EditorGUIUtility.currentViewWidth * 0.4f;
                        UnityEditor.EditorGUILayout.HelpBox(
                            $"{editorTitle} is null! Did you forget to assign the variable in the Configurations Prefab in the Resources folder?",
                            UnityEditor.MessageType.Error);
                        UnityEditor.EditorGUI.indentLevel--;
                        GUIUtility.Space();
                        GUIUtility.DrawFullLine();
                    }
                });
            }
        }

        protected void AddOptionalEditor<T>(Func<T> target, string editorTitle, string optionName,
            string tooltip = null,
            bool showByDefault = false) where T : Object
        {
            if (target() == null)
            {
                return;
            }

            var editor = UnityEditor.Editor.CreateEditor(target());
            _editorCache.Add(editor);

            var optionKey = $"custom_editor_{optionName}";
            AddOptions((new GUIContent(optionName, tooltip), optionKey));
            if (!UnityEditor.EditorPrefs.HasKey(optionKey))
            {
                UnityEditor.EditorPrefs.SetBool(optionKey, showByDefault);
            }

            if (editor != null)
            {
                _instructions.Add(() =>
                {
                    if (!UnityEditor.EditorPrefs.GetBool(optionKey, showByDefault))
                    {
                        return;
                    }
                    if (SearchQuery.IsNotNullOrWhitespace() && !editorTitle.ContainsIgnoreCase(SearchQuery))
                    {
                        return;
                    }

                    var foldoutStyle = FoldoutHandler.Style;
                    FoldoutHandler.Style = FoldoutStyle.Dark;
                    if (Foldout[editorTitle])
                    {
                        if (editor.serializedObject.targetObject == null)
                        {
                            UnityEditor.EditorGUILayout.HelpBox("Target is null!", UnityEditor.MessageType.Error);
                            return;
                        }

                        GUIUtility.Space();
                        if (GUILayout.Button("Select"))
                        {
                            UnityEditor.Selection.activeObject = editor.serializedObject.targetObject;
                            UnityEditor.EditorGUIUtility.PingObject(editor.serializedObject.targetObject);
                        }
                        FoldoutHandler.Style = FoldoutStyle.Default;
                        UnityEditor.EditorGUIUtility.wideMode = false;
                        UnityEditor.EditorGUI.indentLevel++;
                        UnityEditor.EditorGUIUtility.labelWidth = UnityEditor.EditorGUIUtility.currentViewWidth * 0.4f;
                        editor.serializedObject.Update();
                        editor.OnInspectorGUI();
                        editor.serializedObject.ApplyModifiedProperties();
                        UnityEditor.EditorGUI.indentLevel--;
                        FoldoutHandler.Style = foldoutStyle;
                        GUIUtility.Space();
                        GUIUtility.DrawFullLine();
                    }
                });
            }
            else
            {
                _instructions.Add(() =>
                {
                    if (!UnityEditor.EditorPrefs.GetBool(optionKey, showByDefault))
                    {
                        return;
                    }
                    if (SearchQuery.IsNotNullOrWhitespace() && !editorTitle.ContainsIgnoreCase(SearchQuery))
                    {
                        return;
                    }

                    if (Foldout[editorTitle])
                    {
                        UnityEditor.EditorGUIUtility.wideMode = false;
                        UnityEditor.EditorGUI.indentLevel++;
                        UnityEditor.EditorGUIUtility.labelWidth = UnityEditor.EditorGUIUtility.currentViewWidth * 0.4f;
                        UnityEditor.EditorGUILayout.HelpBox(
                            $"{editorTitle} is null! Did you forget to assign the variable in the Configurations Prefab in the Resources folder?",
                            UnityEditor.MessageType.Error);
                        UnityEditor.EditorGUI.indentLevel--;
                        GUIUtility.Space();
                        GUIUtility.DrawFullLine();
                    }
                });
            }
        }

        protected void AddOptionalEditor<T>(T target, string editorTitle, string optionName, string tooltip = null,
            bool showByDefault = false) where T : Object
        {
            var editor = UnityEditor.Editor.CreateEditor(target);
            _editorCache.Add(editor);

            var optionKey = $"custom_editor_{optionName}";
            AddOptions((new GUIContent(optionName, tooltip), optionKey));
            if (!UnityEditor.EditorPrefs.HasKey(optionKey))
            {
                UnityEditor.EditorPrefs.SetBool(optionKey, showByDefault);
            }

            if (editor != null)
            {
                _instructions.Add(() =>
                {
                    if (!UnityEditor.EditorPrefs.GetBool(optionKey, showByDefault))
                    {
                        return;
                    }
                    if (SearchQuery.IsNotNullOrWhitespace() && !editorTitle.ContainsIgnoreCase(SearchQuery))
                    {
                        return;
                    }

                    var foldoutStyle = FoldoutHandler.Style;
                    FoldoutHandler.Style = FoldoutStyle.Dark;
                    if (Foldout[editorTitle])
                    {
                        if (editor.serializedObject.targetObject == null)
                        {
                            UnityEditor.EditorGUILayout.HelpBox("Target is null!", UnityEditor.MessageType.Error);
                            return;
                        }

                        GUIUtility.Space();
                        if (GUILayout.Button("Select"))
                        {
                            UnityEditor.Selection.activeObject = editor.serializedObject.targetObject;
                            UnityEditor.EditorGUIUtility.PingObject(editor.serializedObject.targetObject);
                        }
                        FoldoutHandler.Style = FoldoutStyle.Default;
                        UnityEditor.EditorGUIUtility.wideMode = false;
                        UnityEditor.EditorGUI.indentLevel++;
                        UnityEditor.EditorGUIUtility.labelWidth = UnityEditor.EditorGUIUtility.currentViewWidth * 0.4f;
                        editor.serializedObject.Update();
                        editor.OnInspectorGUI();
                        editor.serializedObject.ApplyModifiedProperties();
                        UnityEditor.EditorGUI.indentLevel--;
                        FoldoutHandler.Style = foldoutStyle;
                        GUIUtility.Space();
                        GUIUtility.DrawFullLine();
                    }
                });
            }
            else
            {
                _instructions.Add(() =>
                {
                    if (!UnityEditor.EditorPrefs.GetBool(optionKey, showByDefault))
                    {
                        return;
                    }
                    if (SearchQuery.IsNotNullOrWhitespace() && !editorTitle.ContainsIgnoreCase(SearchQuery))
                    {
                        return;
                    }

                    if (Foldout[editorTitle])
                    {
                        UnityEditor.EditorGUIUtility.wideMode = false;
                        UnityEditor.EditorGUI.indentLevel++;
                        UnityEditor.EditorGUIUtility.labelWidth = UnityEditor.EditorGUIUtility.currentViewWidth * 0.4f;
                        UnityEditor.EditorGUILayout.HelpBox(
                            $"{editorTitle} is null! Did you forget to assign the variable in the Configurations Prefab in the Resources folder?",
                            UnityEditor.MessageType.Error);
                        UnityEditor.EditorGUI.indentLevel--;

                        GUIUtility.Space();
                        GUIUtility.DrawFullLine();
                    }
                });
            }
        }

        protected void AddOptionalTitle(string titleName, string optionName, string tooltip = null,
            bool showByDefault = false, bool drawLine = true)
        {
            var optionKey = $"custom_editor_{optionName}";
            AddOptions((new GUIContent(optionName, tooltip), optionKey));
            if (!UnityEditor.EditorPrefs.HasKey(optionKey))
            {
                UnityEditor.EditorPrefs.SetBool(optionKey, showByDefault);
            }

            _instructions.Add(() =>
            {
                if (!UnityEditor.EditorPrefs.GetBool(optionKey, showByDefault))
                {
                    return;
                }

                if (!DrawTitles)
                {
                    return;
                }

                if (SearchQuery.IsNotNullOrWhitespace() && !titleName.ContainsIgnoreCase(SearchQuery))
                {
                    return;
                }

                if (drawLine)
                {
                    var lastRect = GUILayoutUtility.GetLastRect();
                    UnityEditor.EditorGUI.DrawRect(
                        new Rect(0, lastRect.y, UnityEditor.EditorGUIUtility.currentViewWidth, 1),
                        new Color(0f, 0f, 0f, 0.3f));
                }

                UnityEditor.EditorGUILayout.Space();
                GUILayout.Label(titleName, new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16
                });
                UnityEditor.EditorGUILayout.Space(3);
            });
        }

        protected void AddInstruction(Action instruction)
        {
            _instructions.Add(instruction);
        }

        protected void AddInstruction(Action instruction, string editorTitle)
        {
            _instructions.Add(() =>
            {
                if (SearchQuery.IsNotNullOrWhitespace() && !editorTitle.ContainsIgnoreCase(SearchQuery))
                {
                    return;
                }
                if (Foldout[editorTitle])
                {
                    UnityEditor.EditorGUILayout.Space();
                    UnityEditor.EditorGUIUtility.wideMode = false;
                    UnityEditor.EditorGUI.indentLevel++;
                    instruction();
                    UnityEditor.EditorGUI.indentLevel--;

                    GUIUtility.Space();
                    GUIUtility.DrawFullLine();
                }
            });
        }

        private void AddOptions((GUIContent content, string key) option)
        {
            var key = option.key;
            foreach (var (_, elementKey) in _options)
            {
                if (string.Equals(key, elementKey, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            _options.AddUnique(option);
        }

        #endregion
    }
}