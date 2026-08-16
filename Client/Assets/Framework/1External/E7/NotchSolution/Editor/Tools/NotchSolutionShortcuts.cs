using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace E7.NotchSolution.Editor
{
    public static class NotchSolutionShortcuts
    {
        private const string notchSolutionPrefPrefix = "Notch Solution/";
        internal const string toggleSimulationShortcut = notchSolutionPrefPrefix + "Toggle Notch Simulator";
        internal const string switchConfigurationShortcut = notchSolutionPrefPrefix + "Switch configuration";

        /// <summary>
        ///     Switch between narrowest and widest aspect specified in the preferences to validate design.
        ///     Switch to the narrowest if currently on neither aspects.
        /// </summary>
        [Shortcut(switchConfigurationShortcut, null, KeyCode.M, ShortcutModifiers.Alt)]
        internal static void SwitchConfiguration()
        {
            Settings.Instance.NextConfiguration();

            NotchSimulator.Redraw();
            NotchSimulator.UpdateAllMockups();
            NotchSimulator.UpdateSimulatorTargets();

            // Using shortcut to change aspect ratio actually will not proc the [ExecuteAlways] Update()
            // of adaptation components, unlike using the drop down.
            // But it mostly do so because we always have some uGUI components which indirectly cause
            // those updates on ratio change. While the scene with no uGUI at all maybe rare,
            // it never hurts to proc them manually.. just in case.

            EditorApplication.QueuePlayerLoopUpdate();
        }

        [Shortcut(toggleSimulationShortcut, null, KeyCode.N, ShortcutModifiers.Alt)]
        private static void ToggleSimulation()
        {
            var settings = Settings.Instance;
            settings.EnableSimulation = !settings.EnableSimulation;
            settings.Save();
            NotchSimulator.UpdateAllMockups();
            NotchSimulator.UpdateSimulatorTargets();
            NotchSimulator.Redraw();
        }

        /// <summary>
        /// �л�ģ�����豸
        /// </summary>
        /// <param name="device"></param>
        public static void SwitchSimulationDevice(string device)
        {
            int index = SimulationDatabase.GetIndex(device);
            if (index == -1) return;
            var settings = Settings.Instance;
            settings.ActiveConfiguration.DeviceIndex = index;
            NotchSolutionShortcuts.ToggleSimulation(true);
        }

        /// <summary>
        /// ����ģ����
        /// </summary>
        /// <param name="enableSimulation"></param>
        public static void ToggleSimulation(bool enableSimulation)
        {
            var settings = Settings.Instance;
            settings.EnableSimulation = enableSimulation;
            settings.Save();
            NotchSimulator.UpdateAllMockups();
            NotchSimulator.UpdateSimulatorTargets();
            NotchSimulator.Redraw();
        }

        /// <summary>
        /// 刷新模拟设置，
        /// </summary>
        public static void Refresh()
        {
            var settings = Settings.Instance;
            if (!settings.EnableSimulation) return;
            ToggleSimulation(true);
        }
    }
}