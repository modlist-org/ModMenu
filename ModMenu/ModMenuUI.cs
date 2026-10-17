using MelonLoader;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ModMenu {
    [Serializable]
    public class ModMenuSettings {
        public KeyCode ToggleKey = KeyCode.F9;
        public bool RequireCtrl = true;
        public bool RequireAlt = false;
        public bool RequireShift = false;
    }

    public class ModMenuUI : MonoBehaviour {
        public static ModMenuUI? Instance { get; private set; }
        public static bool IsOpen { get; private set; }

        private ModMenuSettings _settings = new();
        private bool _isBindingKey = false;
        private Rect _windowRect = new(100, 100, 850, 620);
        private Vector2 _scrollPos = Vector2.zero;
        private string _filter = string.Empty;
        private int _tabId = 0;
        private readonly string[] _tabs = ["Mods", "Settings"];
        private readonly HashSet<string> _expandedMods = [];

        private void Awake() {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
            CenterWindow();
        }

        private void CenterWindow() {
            float w = Mathf.Min(850, Screen.width - 40);
            float h = Mathf.Min(620, Screen.height - 40);
            _windowRect = new((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
        }

        public static void Toggle() {
            IsOpen = !IsOpen;
        }

        public string GetShortcutString() {
            List<string> parts = [];
            if (_settings.RequireCtrl) parts.Add("Ctrl");
            if (_settings.RequireAlt) parts.Add("Alt");
            if (_settings.RequireShift) parts.Add("Shift");
            parts.Add(_settings.ToggleKey.ToString());
            return string.Join(" + ", parts);
        }

        private void Update() {
            if (_isBindingKey) return;

            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (ctrl == _settings.RequireCtrl &&
                alt == _settings.RequireAlt &&
                shift == _settings.RequireShift &&
                Input.GetKeyDown(_settings.ToggleKey)) {
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

        private void OnGUI() {
            if (!IsOpen) return;

            Drawer.Initialize();

            if (_isBindingKey) {
                Event e = Event.current;
                if (e.isKey && e.type == EventType.KeyDown) {
                    if (e.keyCode == KeyCode.Escape) {
                        _isBindingKey = false;
                        e.Use();
                    } else if (e.keyCode != KeyCode.None &&
                               e.keyCode != KeyCode.LeftControl && e.keyCode != KeyCode.RightControl &&
                               e.keyCode != KeyCode.LeftAlt && e.keyCode != KeyCode.RightAlt &&
                               e.keyCode != KeyCode.LeftShift && e.keyCode != KeyCode.RightShift) {
                        _settings.ToggleKey = e.keyCode;
                        _settings.RequireCtrl = e.control;
                        _settings.RequireAlt = e.alt;
                        _settings.RequireShift = e.shift;
                        _isBindingKey = false;
                        SaveSettings();
                        e.Use();
                    }
                }
            }

            if (Drawer.dimTexture is not null) {
                GUI.DrawTexture(new(0, 0, Screen.width, Screen.height), Drawer.dimTexture);
            }

            var oldSkin = GUI.skin;
            if (Drawer.customSkin is not null) {
                GUI.skin = Drawer.customSkin;
            }

            try {
                _windowRect = GUILayout.Window(
                    999901,
                    _windowRect,
                    DrawWindowContent,
                    "",
                    Drawer.darkWindow!,
                    GUILayout.Width(_windowRect.width),
                    GUILayout.Height(_windowRect.height)
                );

                Drawer.RenderTooltip();
            } finally {
                GUI.skin = oldSkin;
            }
        }

        private void DrawWindowContent(int windowId) {
            DrawHeader();

            GUILayout.Space(8);

            if (_tabId == 0) {
                DrawModsTab();
            } else {
                DrawSettingsTab();
            }

            GUILayout.FlexibleSpace();
            GUILayout.Space(6);

            DrawFooter();

            GUI.DragWindow(new(0, 0, Mathf.Max(0, _windowRect.width - 220), 45));
        }

        private void DrawHeader() {
            GUILayout.BeginHorizontal();

            GUILayout.Label("<b>Mod Menu</b>", Drawer.headerStyle, GUILayout.ExpandWidth(false));

            GUILayout.FlexibleSpace();

            for (int i = 0; i < _tabs.Length; i++) {
                var style = _tabId == i ? Drawer.tabButtonActive : Drawer.tabButton;
                if (GUILayout.Button(_tabs[i], style, GUILayout.Width(80), GUILayout.Height(26))) {
                    _tabId = i;
                }
            }

            GUILayout.EndHorizontal();
        }

        private void DrawModsTab() {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter:", GUILayout.ExpandWidth(false));
            _filter = GUILayout.TextField(_filter, Drawer.myTextField, GUILayout.Width(180));
            if (!string.IsNullOrEmpty(_filter)) {
                if (Drawer.Button(Drawer.Icon_X, GUILayout.Width(24), GUILayout.Height(24))) {
                    _filter = string.Empty;
                }
                Drawer.HoverTooltip("Clear filter");
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label($"<color=#888888>Loaded: {ModMenuApi.Mods.Count} mods</color>");
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            GUILayout.BeginHorizontal(Drawer.boxStyle);
            GUILayout.Space(28);
            GUILayout.Label("<b>Mod Name</b>", GUI.skin.label);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            var mods = ModMenuApi.Mods;
            _scrollPos = GUILayout.BeginScrollView(
                _scrollPos,
                false,
                true,
                GUIStyle.none,
                Drawer.verticalScrollbar!,
                GUILayout.ExpandHeight(true)
            );

            if (mods.Count == 0) {
                GUILayout.Space(20);
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

                GUILayout.BeginVertical(Drawer.boxStyle);
                GUILayout.BeginHorizontal();

                bool enabled = mod.Enabled;
                if (Drawer.DrawOnlyBool(ref enabled)) {
                    mod.Enabled = enabled;
                    if (!mod.Enabled && _expandedMods.Contains(mod.Id)) {
                        _expandedMods.Remove(mod.Id);
                        try { mod.OnHideGUI?.Invoke(); } catch { }
                    }
                    string name = string.IsNullOrEmpty(mod.DisplayName) ? mod.Id : mod.DisplayName;
                    MelonLogger.Msg($"{name} {(mod.Enabled ? "Enabled" : "Disabled")}");
                    try {
                        mod.OnToggle?.Invoke(mod.Enabled);
                    } catch (Exception ex) {
                        MelonLogger.Error($"Error toggling {mod.Id}: {ex}");
                    }
                }
                Drawer.HoverTooltip(mod.Enabled ? "Mod is Enabled (click to disable)" : "Mod is Disabled (click to enable)");

                GUILayout.Space(8);

                string nameTitle = string.IsNullOrEmpty(mod.DisplayName) ? mod.Id : mod.DisplayName;
                if (!mod.Enabled) {
                    GUILayout.Label($"<color=#777777><b>{nameTitle}</b></color>", GUI.skin.label, GUILayout.ExpandWidth(false));
                } else if (mod.OnGUI is not null) {
                    if (GUILayout.Button($"<b>{nameTitle}</b>", GUI.skin.label, GUILayout.ExpandWidth(false))) {
                        ToggleExpand(mod.Id);
                    }
                } else {
                    GUILayout.Label($"<b>{nameTitle}</b>", GUI.skin.label, GUILayout.ExpandWidth(false));
                }

                GUILayout.FlexibleSpace();

                if (!string.IsNullOrEmpty(mod.Version)) {
                    GUILayout.Label($"<color=#aaaaaa>{mod.Version}</color>", GUI.skin.label, GUILayout.ExpandWidth(false));
                    GUILayout.Space(8);
                }

                if (mod.OnGUI is not null && mod.Enabled) {
                    string arrow = isExpanded ? "▼" : "►";
                    if (Drawer.Button(arrow, GUILayout.Width(28), GUILayout.Height(22))) {
                        ToggleExpand(mod.Id);
                    }
                    Drawer.HoverTooltip(isExpanded ? "Close settings" : "Open settings");
                } else {
                    GUILayout.Space(28);
                }

                GUILayout.EndHorizontal();

                if (isExpanded && mod.Enabled && mod.OnGUI is not null) {
                    GUILayout.Space(6);
                    GUILayout.BeginVertical(Drawer.boxStyle);
                    try {
                        mod.OnGUI.Invoke();
                    } catch (Exception ex) {
                        GUILayout.Label($"<color=red>Error in mod OnGUI: {ex.Message}</color>");
                    }
                    GUILayout.EndVertical();
                }

                GUILayout.EndVertical();
                GUILayout.Space(3);
            }

            GUILayout.EndScrollView();
        }

        private void ToggleExpand(string id) {
            var mod = ModMenuApi.Get(id);
            if (mod is not null && !mod.Enabled) {
                _expandedMods.Remove(id);
                try { mod.OnHideGUI?.Invoke(); } catch { }
                return;
            }

            if (_expandedMods.Contains(id)) {
                _expandedMods.Remove(id);
                try { mod?.OnHideGUI?.Invoke(); } catch { }
            } else {
                _expandedMods.Add(id);
            }
        }

        private void DrawSettingsTab() {
            GUILayout.BeginVertical(Drawer.boxStyle);

            GUILayout.Label("<b>Menu Settings</b>", Drawer.headerStyle);
            GUILayout.Space(10);

            GUILayout.Label("<b>Toggle Menu Shortcut:</b>");
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();

            string keyText = _isBindingKey ? "Press any key... (Esc to cancel)" : $"Key: {GetShortcutString()}";
            GUIStyle bindBtnStyle = _isBindingKey ? Drawer.tabButtonActive! : Drawer.myButton!;

            if (GUILayout.Button(keyText, bindBtnStyle, GUILayout.Width(240), GUILayout.Height(30))) {
                _isBindingKey = !_isBindingKey;
            }

            if (!_isBindingKey) {
                GUILayout.Space(8);
                if (Drawer.Button("Reset Default", GUILayout.Width(110), GUILayout.Height(30))) {
                    _settings.ToggleKey = KeyCode.F9;
                    _settings.RequireCtrl = true;
                    _settings.RequireAlt = false;
                    _settings.RequireShift = false;
                    SaveSettings();
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            if (!_isBindingKey) {
                GUILayout.BeginHorizontal();
                if (Drawer.DrawBool("Require Ctrl", ref _settings.RequireCtrl)) {
                    SaveSettings();
                }
                GUILayout.Space(16);
                if (Drawer.DrawBool("Require Alt", ref _settings.RequireAlt)) {
                    SaveSettings();
                }
                GUILayout.Space(16);
                if (Drawer.DrawBool("Require Shift", ref _settings.RequireShift)) {
                    SaveSettings();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            } else {
                GUILayout.Label("<color=#FFCC00>Press any key (with Ctrl / Alt / Shift if desired) to bind, or Esc to cancel.</color>");
            }

            GUILayout.Space(16);

            GUILayout.Label("<b>Window:</b>");
            GUILayout.Space(4);
            if (Drawer.Button("Reset Window Position", GUILayout.Width(180), GUILayout.Height(28))) {
                CenterWindow();
            }

            GUILayout.EndVertical();
        }

        private void DrawFooter() {
            GUILayout.BeginHorizontal();

            if (Drawer.Button("Close", GUILayout.Width(80), GUILayout.Height(26))) {
                IsOpen = false;
            }

            GUILayout.Space(6);

            if (Drawer.Button("Save Settings", GUILayout.Width(120), GUILayout.Height(26))) {
                SaveSettings();
                SaveAllMods();
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label($"<color=#777777>{GetShortcutString()}</color>");

            GUILayout.EndHorizontal();
        }

        private void LoadSettings() {
            try {
                string dir = Path.Combine(Directory.GetCurrentDirectory(), "UserData");
                string file = Path.Combine(dir, "ModMenu.json");
                if (File.Exists(file)) {
                    string json = File.ReadAllText(file);
                    var loaded = JsonUtility.FromJson<ModMenuSettings>(json);
                    if (loaded is not null) {
                        _settings = loaded;
                    }
                }
            } catch (Exception ex) {
                Debug.LogWarning($"[ModMenu] Failed to load settings: {ex.Message}");
            }
        }

        private void SaveSettings() {
            try {
                string dir = Path.Combine(Directory.GetCurrentDirectory(), "UserData");
                if (!Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }
                string file = Path.Combine(dir, "ModMenu.json");
                string json = JsonUtility.ToJson(_settings, true);
                File.WriteAllText(file, json);
            } catch (Exception ex) {
                Debug.LogWarning($"[ModMenu] Failed to save settings: {ex.Message}");
            }
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
    }
}
