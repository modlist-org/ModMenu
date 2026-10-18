using System;
using System.Collections.Generic;

namespace ModMenu {
    public class ModEntry {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public string Author { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;

        public Action<bool>? OnToggle { get; set; }
        public Action? OnGUI { get; set; }
        public Action? OnSaveGUI { get; set; }
        public Action? OnHideGUI { get; set; }
    }

    public static class ModMenuApi {
        private static readonly List<ModEntry> _mods = [];
        private static readonly object _lock = new();

        public static IReadOnlyList<ModEntry> Mods {
            get {
                lock (_lock) {
                    return [.. _mods];
                }
            }
        }

        public static bool IsModEnabled(string id) {
            if (string.IsNullOrEmpty(id)) return true;
            return !ModMenuUI.IsModDisabled(id);
        }

        public static void Register(ModEntry entry) {
            if (entry is null || string.IsNullOrEmpty(entry.Id)) return;
            lock (_lock) {
                _mods.RemoveAll(m => m.Id == entry.Id);

                if (ModMenuUI.IsModDisabled(entry.Id)) {
                    entry.Enabled = false;
                    try {
                        entry.OnToggle?.Invoke(false);
                    } catch (Exception ex) {
                        MelonLoader.MelonLogger.Error($"[ModMenu] Error applying disabled state to {entry.Id}: {ex}");
                    }
                }

                _mods.Add(entry);
            }
        }

        public static void Unregister(string id) {
            if (string.IsNullOrEmpty(id)) return;
            lock (_lock) {
                _mods.RemoveAll(m => m.Id == id);
            }
        }

        public static ModEntry? Get(string id) {
            lock (_lock) {
                return _mods.Find(m => m.Id == id);
            }
        }
    }
}
