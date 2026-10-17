using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModMenu {
    public class ModMenuUI : MonoBehaviour {
        public static ModMenuUI? Instance { get; private set; }
        public static bool IsOpen { get; private set; }

        private Rect _windowRect = new(100, 100, 800, 600);
        private Vector2 _scrollPos = Vector2.zero;
        private string _filter = string.Empty;
        private int _tabId = 0;
        private readonly string[] _tabs = ["Mods", "Settings"];
        private readonly HashSet<string> _expandedMods = [];

        private GUIStyle? _winStyle;
        private GUIStyle? _headerStyle;
        private GUIStyle? _btnStyle;
        private GUIStyle? _btnActiveStyle;
        private GUIStyle? _btnInactiveStyle;
        private GUIStyle? _boxStyle;
        private GUIStyle? _tabBtnStyle;
        private Texture2D? _winBgTex;
        private Texture2D? _boxBgTex;
        private Texture2D? _btnBgTex;
        private Texture2D? _btnActiveBgTex;
        private Texture2D? _btnInactiveBgTex;

        private void Awake() {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CenterWindow();
        }

        private void CenterWindow() {
            float w = Mathf.Min(800, Screen.width - 40);
            float h = Mathf.Min(600, Screen.height - 40);
            _windowRect = new((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
        }

        public static void Toggle() {
            IsOpen = !IsOpen;
        }

        private void Update() {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (ctrl && Input.GetKeyDown(KeyCode.F9)) {
                Toggle();
            }

            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) {
                IsOpen = false;
            }

            if (IsOpen) {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private Texture2D MakeTex(int width, int height, Color col) {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
                pix[i] = col;
            Texture2D result = new(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void InitStyles() {
            if (_winStyle is not null) return;

            _winBgTex = MakeTex(2, 2, new(0.12f, 0.12f, 0.12f, 0.96f));
            _boxBgTex = MakeTex(2, 2, new(0.18f, 0.18f, 0.18f, 0.90f));
            _btnBgTex = MakeTex(2, 2, new(0.28f, 0.28f, 0.28f, 1f));
            _btnActiveBgTex = MakeTex(2, 2, new(0.15f, 0.55f, 0.25f, 1f));
            _btnInactiveBgTex = MakeTex(2, 2, new(0.45f, 0.2f, 0.2f, 1f));

            _winStyle = new(GUI.skin.window) {
                normal = { background = _winBgTex, textColor = Color.white },
                onNormal = { background = _winBgTex, textColor = Color.white }
            };

            _headerStyle = new(GUI.skin.label) {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new(0.9f, 0.9f, 0.9f) },
                alignment = TextAnchor.MiddleLeft
            };

            _btnStyle = new(GUI.skin.button) {
                normal = { background = _btnBgTex, textColor = Color.white },
                hover = { background = _btnBgTex, textColor = Color.yellow },
                fontSize = 12
            };

            _tabBtnStyle = new(_btnStyle) {
                fontSize = 13,
                fixedHeight = 26
            };

            _btnActiveStyle = new(GUI.skin.button) {
                normal = { background = _btnActiveBgTex, textColor = Color.white },
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };

            _btnInactiveStyle = new(GUI.skin.button) {
                normal = { background = _btnInactiveBgTex, textColor = Color.white },
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };

            _boxStyle = new(GUI.skin.box) {
                normal = { background = _boxBgTex, textColor = Color.white }
            };
        }

        private void OnGUI() {
            if (!IsOpen) return;

            InitStyles();

            _windowRect = GUILayout.Window(
                999901,
                _windowRect,
                DrawWindowContent,
                "Mod Menu (Ctrl + F9)",
                _winStyle!,
                GUILayout.Width(_windowRect.width),
                GUILayout.Height(_windowRect.height)
            );
        }

        private void DrawWindowContent(int windowId) {
            GUI.DragWindow(new(0, 0, 10000, 24));

            GUILayout.BeginHorizontal();
            int newTab = GUILayout.Toolbar(_tabId, _tabs, _tabBtnStyle, GUILayout.Width(220));
            if (newTab != _tabId) _tabId = newTab;

            GUILayout.FlexibleSpace();

            if (_tabId == 0) {
                GUILayout.Label("Filter:", GUILayout.ExpandWidth(false));
                _filter = GUILayout.TextField(_filter, GUILayout.Width(140));
                if (GUILayout.Button("X", _btnStyle, GUILayout.Width(24))) {
                    _filter = string.Empty;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            if (_tabId == 0) {
                DrawModsTab();
            } else {
                DrawSettingsTab();
            }

            GUILayout.FlexibleSpace();
            GUILayout.Space(6);

            // Bottom bar
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Close (Esc)", _btnStyle, GUILayout.Width(100))) {
                IsOpen = false;
            }

            if (GUILayout.Button("Save Settings", _btnStyle, GUILayout.Width(120))) {
                SaveAllMods();
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("<color=#888888>Press Ctrl+F9 to toggle menu</color>");
            GUILayout.EndHorizontal();
        }

        private void DrawModsTab() {
            var mods = ModMenuApi.Mods;

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, true, GUILayout.ExpandHeight(true));

            GUILayout.BeginHorizontal(_boxStyle);
            GUILayout.Label("<b>Mod Name</b>", GUILayout.Width(220));
            GUILayout.Label("<b>Version</b>", GUILayout.Width(70));
            GUILayout.Label("<b>Status</b>", GUILayout.Width(65));
            GUILayout.Label("<b>Settings</b>", GUILayout.Width(75));
            GUILayout.Label("<b>Author</b>", GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            if (mods.Count == 0) {
                GUILayout.Space(10);
                GUILayout.Label("  No mods registered with ModMenu yet.", GUI.skin.label);
            }

            for (int i = 0; i < mods.Count; i++) {
                var mod = mods[i];
                if (!string.IsNullOrEmpty(_filter)) {
                    string f = _filter.ToLowerInvariant();
                    if (!mod.DisplayName.ToLowerInvariant().Contains(f) &&
                        !mod.Id.ToLowerInvariant().Contains(f)) {
                        continue;
                    }
                }

                bool isExpanded = _expandedMods.Contains(mod.Id);

                GUILayout.BeginVertical(_boxStyle);
                GUILayout.BeginHorizontal();

                string nameTitle = string.IsNullOrEmpty(mod.DisplayName) ? mod.Id : mod.DisplayName;
                if (GUILayout.Button(nameTitle, GUI.skin.label, GUILayout.Width(220))) {
                    ToggleExpand(mod.Id);
                }

                GUILayout.Label(mod.Version, GUILayout.Width(70));

                string statusText = mod.Enabled ? "ON" : "OFF";
                var statusStyle = mod.Enabled ? _btnActiveStyle : _btnInactiveStyle;
                if (GUILayout.Button(statusText, statusStyle, GUILayout.Width(65), GUILayout.Height(20))) {
                    mod.Enabled = !mod.Enabled;
                    try {
                        mod.OnToggle?.Invoke(mod.Enabled);
                    } catch (Exception ex) {
                        Debug.LogError($"[ModMenu] Error toggling {mod.Id}: {ex}");
                    }
                }

                string expandText = isExpanded ? "▼ Hide" : "► Edit";
                if (mod.OnGUI is not null) {
                    if (GUILayout.Button(expandText, _btnStyle, GUILayout.Width(75), GUILayout.Height(20))) {
                        ToggleExpand(mod.Id);
                    }
                } else {
                    GUILayout.Label("-", GUILayout.Width(75));
                }

                GUILayout.Label(mod.Author, GUILayout.ExpandWidth(true));

                GUILayout.EndHorizontal();

                if (isExpanded && mod.OnGUI is not null) {
                    GUILayout.Space(4);
                    GUILayout.BeginVertical(GUI.skin.box);
                    try {
                        mod.OnGUI.Invoke();
                    } catch (Exception ex) {
                        GUILayout.Label($"<color=red>Error in mod OnGUI: {ex.Message}</color>");
                    }
                    GUILayout.EndVertical();
                }

                GUILayout.EndVertical();
                GUILayout.Space(2);
            }

            GUILayout.EndScrollView();
        }

        private void ToggleExpand(string id) {
            if (_expandedMods.Contains(id)) {
                _expandedMods.Remove(id);
                var mod = ModMenuApi.Get(id);
                try { mod?.OnHideGUI?.Invoke(); } catch { }
            } else {
                _expandedMods.Add(id);
            }
        }

        private void DrawSettingsTab() {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("ModMenu Information", _headerStyle);
            GUILayout.Space(8);
            GUILayout.Label("Shortcut: <b>Ctrl + F9</b> opens/closes this menu.");
            GUILayout.Label("Escape key also closes this menu when open.");
            GUILayout.Label($"Loaded Mods: {ModMenuApi.Mods.Count}");
            GUILayout.Space(12);

            if (GUILayout.Button("Reset Window Position", _btnStyle, GUILayout.Width(180))) {
                CenterWindow();
            }

            GUILayout.EndVertical();
        }

        private void SaveAllMods() {
            foreach (var mod in ModMenuApi.Mods) {
                try {
                    mod.OnSaveGUI?.Invoke();
                } catch (Exception ex) {
                    Debug.LogError($"[ModMenu] Error saving settings for {mod.Id}: {ex}");
                }
            }
        }

        private void OnDestroy() {
            if (_winBgTex is not null) Destroy(_winBgTex);
            if (_boxBgTex is not null) Destroy(_boxBgTex);
            if (_btnBgTex is not null) Destroy(_btnBgTex);
            if (_btnActiveBgTex is not null) Destroy(_btnActiveBgTex);
            if (_btnInactiveBgTex is not null) Destroy(_btnInactiveBgTex);
        }
    }
}
