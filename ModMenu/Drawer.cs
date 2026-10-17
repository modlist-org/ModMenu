using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ModMenu {
    public static class ResourceManager {
        public static byte[] GetResourceBytes(string resourceName) {
            Assembly assembly = typeof(ResourceManager).Assembly;
            string fullName = $"ModMenu.Resources.{resourceName}";

            using Stream? stream = assembly.GetManifestResourceStream(fullName);
            if (stream is null) return [];

            using MemoryStream ms = new();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        public static Texture2D? LoadTexture(string resourceName) {
            byte[] bytes = GetResourceBytes(resourceName);
            if (bytes.Length == 0) return null;

            Texture2D texture = new(1, 1, TextureFormat.RGBA32, false);
            texture.LoadImage(bytes);
            return texture;
        }
    }

    public static class Drawer {
        public static Texture2D? veryjittengray;
        public static Texture2D? jittengray;
        public static Texture2D? tfgray;
        public static Texture2D? gray;
        public static Texture2D? dulgray;
        public static Texture2D? outlineimg;
        public static Texture2D? black;
        public static Texture2D? dimTexture;

        public static Texture2D? Icon_Active;
        public static Texture2D? Icon_Inactive;
        public static Texture2D? Icon_X;

        public static Texture2D? scrollTrack;
        public static Texture2D? scrollThumb;
        public static Texture2D? scrollThumbHover;

        public static GUIStyle? myButton;
        public static GUIStyle? myTextField;
        public static GUIStyle? darkWindow;
        public static GUIStyle? boxStyle;
        public static GUIStyle? tabButton;
        public static GUIStyle? tabButtonActive;
        public static GUIStyle? headerStyle;
        public static GUIStyle? badgeStyle;
        public static GUIStyle? verticalScrollbar;
        public static GUIStyle? verticalScrollbarThumb;
        public static GUISkin? customSkin;

        public static string? CurrentTooltip;
        private static bool _isInited = false;

        public static void Initialize() {
            if (_isInited) return;

            dulgray = CreateColorTexture(new(0.40f, 0.40f, 0.40f, 1f));
            gray = CreateColorTexture(new(0.30f, 0.30f, 0.30f, 1f));
            jittengray = CreateColorTexture(new(0.15f, 0.15f, 0.15f, 0.95f));
            tfgray = CreateColorTexture(new(0.20f, 0.20f, 0.20f, 1f));
            veryjittengray = CreateColorTexture(new(0.11f, 0.11f, 0.11f, 0.98f));
            black = CreateColorTexture(Color.black);
            dimTexture = CreateColorTexture(new(0f, 0f, 0f, 0.65f));

            outlineimg = ResourceManager.LoadTexture("RGUIoutline.png");
            if (outlineimg is not null) outlineimg.filterMode = FilterMode.Point;

            Icon_Active = ResourceManager.LoadTexture("active.png");
            Icon_Inactive = ResourceManager.LoadTexture("inactive.png");
            Icon_X = ResourceManager.LoadTexture("x.png");

            scrollTrack = CreateColorTexture(new(0.08f, 0.08f, 0.08f, 0.7f));
            scrollThumb = CreateColorTexture(new(0.32f, 0.32f, 0.32f, 0.9f));
            scrollThumbHover = CreateColorTexture(new(0.48f, 0.48f, 0.48f, 1f));

            verticalScrollbar = new(GUI.skin.verticalScrollbar) {
                normal = { background = scrollTrack },
                fixedWidth = 8,
                margin = new(2, 2, 0, 0)
            };

            verticalScrollbarThumb = new(GUI.skin.verticalScrollbarThumb) {
                normal = { background = scrollThumb },
                hover = { background = scrollThumbHover },
                active = { background = scrollThumbHover },
                fixedWidth = 8
            };

            myButton = new(GUI.skin.button) {
                normal = { background = gray, textColor = Color.white },
                hover = { background = dulgray, textColor = Color.yellow },
                active = { background = dulgray, textColor = Color.white },
                fontSize = 12
            };

            myTextField = new(GUI.skin.textField) {
                normal = { background = tfgray, textColor = Color.white },
                focused = { background = tfgray, textColor = Color.white },
                hover = { background = tfgray, textColor = Color.white },
                fontSize = 12,
                padding = new(6, 6, 4, 4)
            };

            darkWindow = new(GUI.skin.window) {
                normal = { background = outlineimg ?? veryjittengray, textColor = Color.white },
                onNormal = { background = outlineimg ?? veryjittengray, textColor = Color.white },
                border = new(2, 2, 2, 2),
                padding = new(12, 12, 10, 10)
            };

            boxStyle = new(GUI.skin.box) {
                normal = { background = jittengray, textColor = Color.white },
                border = new(1, 1, 1, 1),
                padding = new(8, 8, 6, 6)
            };

            tabButton = new(myButton) {
                fontSize = 13,
                fixedHeight = 26
            };

            tabButtonActive = new(tabButton) {
                normal = { background = dulgray, textColor = new(0.4f, 0.85f, 1f) }
            };

            headerStyle = new(GUI.skin.label) {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleLeft
            };

            badgeStyle = new(GUI.skin.box) {
                normal = { background = tfgray, textColor = new(0.85f, 0.85f, 0.85f) },
                fontSize = 11,
                padding = new(6, 6, 2, 2)
            };

            customSkin = UnityEngine.Object.Instantiate(GUI.skin);
            customSkin.window = darkWindow;
            customSkin.button = myButton;
            customSkin.textField = myTextField;
            customSkin.box = boxStyle;
            customSkin.verticalScrollbar = verticalScrollbar;
            customSkin.verticalScrollbarThumb = verticalScrollbarThumb;
            customSkin.verticalScrollbarUpButton = GUIStyle.none;
            customSkin.verticalScrollbarDownButton = GUIStyle.none;
            customSkin.horizontalScrollbar = new(GUI.skin.horizontalScrollbar) {
                normal = { background = scrollTrack },
                fixedHeight = 8,
                margin = new(0, 0, 2, 2)
            };
            customSkin.horizontalScrollbarThumb = new(GUI.skin.horizontalScrollbarThumb) {
                normal = { background = scrollThumb },
                hover = { background = scrollThumbHover },
                active = { background = scrollThumbHover },
                fixedHeight = 8
            };
            customSkin.horizontalScrollbarLeftButton = GUIStyle.none;
            customSkin.horizontalScrollbarRightButton = GUIStyle.none;

            _isInited = true;
        }

        private static Texture2D CreateColorTexture(Color col) {
            Texture2D tex = new(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }

        public static bool Button(string label, params GUILayoutOption[] options) =>
            GUILayout.Button(label, myButton, options);

        public static bool Button(Texture2D? icon, string text, params GUILayoutOption[] options) =>
            icon is not null ? GUILayout.Button(new GUIContent(text, icon), myButton, options) : GUILayout.Button(text, myButton, options);

        public static bool Button(Texture2D? icon, params GUILayoutOption[] options) =>
            icon is not null ? GUILayout.Button(icon, myButton, options) : false;

        public static bool DrawBool(string label, ref bool value) {
            bool prev = value;
            GUILayout.BeginHorizontal();

            var old = GUI.backgroundColor;
            GUI.backgroundColor = Color.clear;
            GUIStyle toggleSkin = new(GUI.skin.button) {
                margin = new(0, 0, 3, 0),
                padding = new(0, 0, 0, 0)
            };

            Texture2D? icon = value ? Icon_Active : Icon_Inactive;
            if (icon is not null && GUILayout.Button(icon, toggleSkin, GUILayout.Width(20), GUILayout.Height(20))) {
                value = !value;
            }

            GUI.backgroundColor = old;

            if (GUILayout.Button(label, GUI.skin.label)) {
                value = !value;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            return prev != value;
        }

        public static bool DrawOnlyBool(ref bool value) {
            bool prev = value;
            var old = GUI.backgroundColor;
            GUI.backgroundColor = Color.clear;
            GUIStyle toggleSkin = new(GUI.skin.button) {
                margin = new(0, 0, 2, 0),
                padding = new(0, 0, 0, 0)
            };

            Texture2D? icon = value ? Icon_Active : Icon_Inactive;
            if (icon is not null) {
                if (GUILayout.Button(icon, toggleSkin, GUILayout.Width(20), GUILayout.Height(20))) {
                    value = !value;
                }
            } else {
                value = GUILayout.Toggle(value, "");
            }

            GUI.backgroundColor = old;
            return prev != value;
        }

        public static bool DrawString(string label, ref string value) {
            string prev = value;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(100));
            value = GUILayout.TextField(value, myTextField);
            GUILayout.EndHorizontal();
            return prev != value;
        }

        public static bool HoverTooltip(string tooltip) {
            Rect lastRect = GUILayoutUtility.GetLastRect();
            bool hover = lastRect.Contains(Event.current.mousePosition);
            if (hover) {
                CurrentTooltip = tooltip;
            }
            return hover;
        }

        public static void RenderTooltip() {
            if (string.IsNullOrEmpty(CurrentTooltip)) return;

            Vector2 mousePos = Event.current.mousePosition;
            GUIContent content = new(CurrentTooltip);
            float maxWidth = 400f;
            float width = Mathf.Min(GUI.skin.label.CalcSize(content).x + 16, maxWidth);
            float height = GUI.skin.label.CalcHeight(content, width) + 12;

            Rect pos = new(mousePos.x + 12, mousePos.y + 12, width, height);
            if (pos.x + pos.width > Screen.width) pos.x = Screen.width - pos.width - 8;
            if (pos.y + pos.height > Screen.height) pos.y = Screen.height - pos.height - 8;

            GUI.Box(pos, "", darkWindow);
            GUI.Label(new(pos.x + 8, pos.y + 6, width - 16, height - 12), CurrentTooltip, GUI.skin.label);

            CurrentTooltip = null;
        }
    }
}
