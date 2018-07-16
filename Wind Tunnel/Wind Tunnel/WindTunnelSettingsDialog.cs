﻿using System.Collections.Generic;
using UnityEngine;

namespace KerbalWindTunnel
{
    public class WindTunnelSettings
    {
        public static bool UseCoefficients
        {
            get { return Instance.useCoefficients; }
            set
            {
                Instance.useCoefficients = value;
                settingsChanged = true;
            }
        }
        [Persistent]
        public bool useCoefficients = false;

        public static bool DefaultToMach
        {
            get { return Instance.defaultToMach; }
            set
            {
                Instance.defaultToMach = value;
                settingsChanged = true;
            }
        }
        [Persistent]
        public bool defaultToMach = false;

        public static bool StartMinimized
        {
            get { return Instance.startMinimized; }
            set
            {
                Instance.startMinimized = value;
                settingsChanged = true;
            }
        }
        [Persistent]
        public bool startMinimized = false;

        private static bool settingsChanged = false;
        private static bool settingsLoaded = false;

        internal static WindTunnelSettings Instance = new WindTunnelSettings();

        public static void InitializeSettings()
        {
            if (settingsLoaded)
                return;

            Instance.LoadSettingsFromFile();

            settingsLoaded = true;
        }
        private void LoadSettingsFromFile()
        {
            ConfigNode[] settingsNode = GameDatabase.Instance.GetConfigNodes("KerbalWindTunnelSettings");
            if (settingsNode.Length < 1)
            {
                Debug.Log("Kerbal Wind Tunnel Settings file note found.");
                // To trigger creating a settings file.
                settingsChanged = true;
                return;
            }
            ConfigNode.LoadObjectFromConfig(this, settingsNode[0]);
            Debug.Log(string.Format("KWTSettings: {0}, {1}, {2}", UseCoefficients, DefaultToMach, StartMinimized));
        }

        public static void SaveSettings()
        {
            Instance.SaveSettingsToFile();
        }
        private void SaveSettingsToFile()
        {
            if (!settingsChanged)
                return;

            ConfigNode data = ConfigNode.CreateConfigFromObject(this, 0, new ConfigNode("KerbalWindTunnelSettings"));

            ConfigNode save = new ConfigNode();
            save.AddNode(data);
            save.Save("GameData/WindTunnel/WindTunnelSettings.cfg");
        }
    }

    public partial class WindTunnelWindow
    {
        private PopupDialog settingsDialog;
        private PopupDialog SpawnDialog()
        {
            List<DialogGUIBase> dialog = new List<DialogGUIBase>
            {
                new DialogGUIToggle(WindTunnelSettings.UseCoefficients, "Lift, Drag as coefficients",
                    delegate (bool b) {
                        WindTunnelWindow.Instance.graphDirty = true;
                        WindTunnelWindow.Instance.graphRequested = false;
                        WindTunnelSettings.UseCoefficients = !WindTunnelSettings.UseCoefficients; }),
                new DialogGUIToggle(WindTunnelSettings.DefaultToMach, "Default to speed as Mach", delegate (bool b) { WindTunnelSettings.DefaultToMach = !WindTunnelSettings.DefaultToMach; }),
                new DialogGUIToggle(WindTunnelSettings.StartMinimized, "Start minimized", delegate (bool b) { WindTunnelSettings.StartMinimized = !WindTunnelSettings.StartMinimized; }),
                new DialogGUIButton("Accept", delegate { WindTunnelWindow.Instance.Visible = true; settingsDialog.Dismiss(); })
            };

            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new MultiOptionDialog("KWTSettings", "", "Kerbal Wind Tunnel Settings", UISkinManager.defaultSkin, dialog.ToArray()),
                false, UISkinManager.defaultSkin);
        }
    }
}