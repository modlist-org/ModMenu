using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(ModMenu.ModMenuPlugin), "ModMenu", "1.1.0", "modlist.org")]

namespace ModMenu {
    public class ModMenuPlugin : MelonPlugin {
        public static ModMenuPlugin? Instance { get; private set; }
        private GameObject? _uiHost;

        public override void OnInitializeMelon() {
            Instance = this;
            _uiHost = new("ModMenu_Host");
            Object.DontDestroyOnLoad(_uiHost);
            _uiHost.AddComponent<ModMenuUI>();
            MelonLogger.Msg("Ctrl+F9 to open");
        }

        public override void OnDeinitializeMelon() {
            if (_uiHost is not null) {
                Object.Destroy(_uiHost);
                _uiHost = null;
            }
            Instance = null;
        }
    }
}
