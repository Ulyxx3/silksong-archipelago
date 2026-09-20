using System;
using System.Collections.Generic;
using UnityEngine;
using SilksongArchipelago.Networking;

namespace SilksongArchipelago.UI
{
    /// <summary>
    /// In-game HUD and interactive overlay for Archipelago connection and event logging.
    /// Toggleable with F2 (configurable).
    /// </summary>
    public class ArchipelagoUI : MonoBehaviour
    {
        public bool IsVisible { get; set; } = false;
        public KeyCode ToggleKey { get; set; } = KeyCode.F2;

        public string Host { get; set; } = "localhost";
        public string Port { get; set; } = "38281";
        public string SlotName { get; set; } = "Hornet";
        public string Password { get; set; } = "";

        private Rect _windowRect = new(20, 20, 360, 480);
        private readonly List<(string Message, Color TextColor, float Time)> _logs = new();
        private const int MaxLogs = 30;

        private GUIStyle? _titleStyle;
        private GUIStyle? _headerStyle;
        private GUIStyle? _labelStyle;
        private GUIStyle? _statusStyle;
        private GUIStyle? _toastStyle;

        public void AddLog(string message, Color? color = null)
        {
            Color c = color ?? Color.white;
            _logs.Add((message, c, Time.time));
            if (_logs.Count > MaxLogs)
            {
                _logs.RemoveAt(0);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
            {
                IsVisible = !IsVisible;
            }
        }

        private void InitStyles()
        {
            if (_titleStyle != null)
                return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.82f, 0.45f) } // Silksong Gold
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = Color.white }
            };

            _statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            _toastStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = Color.white }
            };
        }

        private void OnGUI()
        {
            InitStyles();

            // Draw in-game toast notifications when menu is closed or open
            DrawToasts();

            if (!IsVisible)
                return;

            _windowRect = GUI.Window(98765, _windowRect, DrawWindow, "Silksong Archipelago");
        }

        private void DrawWindow(int windowId)
        {
            var client = SilksongArchipelagoPlugin.Instance?.ArchipelagoClient;

            GUILayout.BeginVertical();

            // Header
            GUILayout.Label("ARCHIPELAGO MULTIWORLD", _titleStyle);
            GUILayout.Space(5);

            // Status indicator
            GUILayout.BeginHorizontal();
            if (client != null && client.IsConnected)
            {
                _statusStyle!.normal.textColor = Color.green;
                GUILayout.Label($"● Connected to {client.CurrentHost}:{client.CurrentPort}", _statusStyle);
            }
            else
            {
                _statusStyle!.normal.textColor = new Color(1f, 0.35f, 0.35f);
                GUILayout.Label("● Disconnected", _statusStyle);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // Connection inputs
            GUILayout.Label("Host / Server Address:", _headerStyle);
            Host = GUILayout.TextField(Host);

            GUILayout.Label("Port:", _headerStyle);
            Port = GUILayout.TextField(Port);

            GUILayout.Label("Slot Name (Player):", _headerStyle);
            SlotName = GUILayout.TextField(SlotName);

            GUILayout.Label("Password (Optional):", _headerStyle);
            Password = GUILayout.PasswordField(Password, '*');

            GUILayout.Space(8);

            // Buttons
            GUILayout.BeginHorizontal();
            if (client == null || !client.IsConnected)
            {
                if (GUILayout.Button("Connect", GUILayout.Height(28)))
                {
                    if (int.TryParse(Port, out int portNum))
                    {
                        AddLog($"Connecting to {Host}:{portNum} as '{SlotName}'...", Color.yellow);
                        client?.Connect(Host, portNum, SlotName, string.IsNullOrEmpty(Password) ? null : Password);
                    }
                    else
                    {
                        AddLog("Invalid port number.", Color.red);
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Disconnect", GUILayout.Height(28)))
                {
                    client.Disconnect();
                    AddLog("Disconnected from server.", Color.yellow);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Live event logs
            GUILayout.Label("Activity Log:", _headerStyle);
            GUILayout.BeginVertical(GUI.skin.box);
            for (int i = Math.Max(0, _logs.Count - 6); i < _logs.Count; i++)
            {
                var entry = _logs[i];
                _labelStyle!.normal.textColor = entry.TextColor;
                GUILayout.Label(entry.Message, _labelStyle);
            }
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.Label($"Press [{ToggleKey}] to hide menu", _labelStyle);

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        private void DrawToasts()
        {
            float now = Time.time;
            float yOffset = 20;

            for (int i = _logs.Count - 1; i >= Math.Max(0, _logs.Count - 3); i--)
            {
                var entry = _logs[i];
                float elapsed = now - entry.Time;
                if (elapsed > 5.0f)
                    continue; // Expired toast

                float alpha = Mathf.Clamp01((5.0f - elapsed) / 1.0f);
                Color prevColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);

                float width = 340;
                float height = 30;
                Rect toastRect = new(Screen.width - width - 20, yOffset, width, height);

                _toastStyle!.normal.textColor = entry.TextColor;
                GUI.Box(toastRect, $" {entry.Message}", _toastStyle);

                yOffset += 35;
                GUI.color = prevColor;
            }
        }
    }
}
