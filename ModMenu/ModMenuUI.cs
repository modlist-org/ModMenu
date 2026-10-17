using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModMenu {
    public class ModMenuUI : MonoBehaviour {
        public static ModMenuUI? Instance { get; private set; }
        public static bool IsOpen { get; private set; }

        private Rect _windowRect = new(100, 100, 850, 620);
        private Vector2 _scrollPos = Vector2.zero;
        private string _filter = string.Empty;
        private int _tabId = 0;
        private readonly string[] _tabs = ["Mods", "Settings"];
        private readonly HashSet<string> _expandedMods = [];

        private void Awake() {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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

        private void OnGUI() {
            if (!IsOpen) return;

            Drawer.Initialize();

            if (Drawer.dimTexture is not null) {
                GUI.DrawTexture(new(0, 0, Screen.width, Screen.height), Drawer.dimTexture);
            }

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
        }

        private void DrawWindowContent(int windowId) {
            GUI.DragWindow(new(0, 0, 10000, 75));

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
            // Search / Filter bar
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

            // Table Header
            GUILayout.BeginHorizontal(Drawer.boxStyle);
            GUILayout.Space(26);
            GUILayout.Label("<b>Mod Name</b>", GUILayout.Width(230));
            GUILayout.Label("<b>Version</b>", GUILayout.Width(75));
            GUILayout.Label("<b>Status</b>", GUILayout.Width(60));
            GUILayout.Label("<b>Settings</b>", GUILayout.Width(75));
            GUILayout.Label("<b>Author</b>", GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            // Mod items list
            var mods = ModMenuApi.Mods;
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, true, GUILayout.ExpandHeight(true));

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

                // Expand button
                string arrow = isExpanded ? "▼" : "►";
                if (Drawer.Button(arrow, GUILayout.Width(24), GUILayout.Height(22))) {
                    ToggleExpand(mod.Id);
                }

                // Name
                string nameTitle = string.IsNullOrEmpty(mod.DisplayName) ? mod.Id : mod.DisplayName;
                if (GUILayout.Button($"<b>{nameTitle}</b>", GUI.skin.label, GUILayout.Width(230))) {
                    ToggleExpand(mod.Id);
                }

                // Version
                GUILayout.Label(mod.Version, GUILayout.Width(75));

                // Toggle switch (Overlayer v3 style)
                bool enabled = mod.Enabled;
                if (Drawer.DrawOnlyBool(ref enabled)) {
                    mod.Enabled = enabled;
                    try {
                        mod.OnToggle?.Invoke(mod.Enabled);
                    } catch (Exception ex) {
                        Debug.LogError($"[ModMenu] Error toggling {mod.Id}: {ex}");
                    }
                }
                Drawer.HoverTooltip(mod.Enabled ? "Mod is Enabled (click to disable)" : "Mod is Disabled (click to enable)");

                GUILayout.Space(4);

                // Settings edit button
                if (mod.OnGUI is not null) {
                    string btnText = isExpanded ? "Hide" : "Edit";
                    if (Drawer.Button(btnText, GUILayout.Width(75), GUILayout.Height(22))) {
                        ToggleExpand(mod.Id);
                    }
                } else {
                    GUILayout.Label("-", GUILayout.Width(75));
                }

                // Author
                GUILayout.Label(mod.Author, GUILayout.ExpandWidth(true));

                GUILayout.EndHorizontal();

                // Expanded settings GUI
                if (isExpanded && mod.OnGUI is not null) {
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
            if (_expandedMods.Contains(id)) {
                _expandedMods.Remove(id);
                var mod = ModMenuApi.Get(id);
                try { mod?.OnHideGUI?.Invoke(); } catch { }
            } else {
                _expandedMods.Add(id);
            }
        }

        private void DrawSettingsTab() {
            GUILayout.BeginVertical(Drawer.boxStyle);
            GUILayout.Label("<b>Settings</b>", Drawer.headerStyle);
            GUILayout.Space(8);
            GUILayout.Label("• Shortcut: <b>Ctrl + F9</b> to toggle this menu.");
            GUILayout.Label("• Press <b>Escape</b> to close.");
            GUILayout.Label($"• Registered Mods: <b>{ModMenuApi.Mods.Count}</b>");
            GUILayout.Space(14);

            if (Drawer.Button("Reset Window Position", GUILayout.Width(180), GUILayout.Height(26))) {
                CenterWindow();
            }

            GUILayout.EndVertical();
        }

        private void DrawFooter() {
            GUILayout.BeginHorizontal();

            if (Drawer.Button("Close (Esc)", GUILayout.Width(100), GUILayout.Height(26))) {
                IsOpen = false;
            }

            GUILayout.Space(6);

            if (Drawer.Button("Save Settings", GUILayout.Width(120), GUILayout.Height(26))) {
                SaveAllMods();
            }

            GUILayout.FlexibleSpace();

            // Discord & GitHub links (Overlayer v3 style)
            if (Drawer.Button(Drawer.Icon_Discord, " Discord", GUILayout.Height(26))) {
                Application.OpenURL("https://discord.modlist.org/");
            }
            Drawer.HoverTooltip("Join the modlist.org Discord community");

            GUILayout.Space(4);

            if (Drawer.Button(Drawer.Icon_Github, " GitHub", GUILayout.Height(26))) {
                Application.OpenURL("https://github.com/modlist-org/ModMenu");
            }
            Drawer.HoverTooltip("Open ModMenu GitHub repository");

            GUILayout.Space(8);
            GUILayout.Label("<color=#777777>Ctrl + F9</color>");

            GUILayout.EndHorizontal();
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
