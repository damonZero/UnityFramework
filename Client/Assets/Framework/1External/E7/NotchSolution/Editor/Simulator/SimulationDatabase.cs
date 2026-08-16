using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace E7.NotchSolution.Editor
{
    /// <summary>
    ///     Please add more! [How to get device's safe area and cutouts](https://github.com/5argon/NotchSolution/issues/2).
    /// </summary>
    internal static class SimulationDatabase
    {
        private static Dictionary<string, SimulationDevice> db;
        internal static string[] KeyList;

        internal static void Refresh()
        {
            if (db == null)
            {
                db = new Dictionary<string, SimulationDevice>();
            }

            db.Clear();

            var deviceDirectory = new DirectoryInfo(NotchSimulatorUtility.DevicesFolder);
            if (!deviceDirectory.Exists)
            {
                return;
            }

            var deviceDefinitions = deviceDirectory.GetFiles("*.device.json");

            foreach (var deviceDefinition in deviceDefinitions)
            {
                SimulationDevice deviceInfo;
                using (var sr = deviceDefinition.OpenText())
                {
                    deviceInfo = JsonUtility.FromJson<SimulationDevice>(sr.ReadToEnd());
                }

                db.Add(deviceInfo.Meta.friendlyName, deviceInfo);
            }

            KeyList = db.Keys.ToArray();
        }

        internal static SimulationDevice Get(string device)
        {
            if (db == null)
            {
                Refresh();
            }

            return db.TryGetValue(device, out var found) ? found : default;
        }
        
        /// <summary>
        /// 获取设备的索引
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public static int GetIndex(string device)
        {
            for (int i = 0; i < KeyList.Length; i++)
            {
                if (KeyList[i] == device)
                {
                    return i;
                }
            }

            return -1;
        }

        internal static SimulationDevice ByIndex(int index)
        {
            if (KeyList == null)
            {
                Refresh();
            }

            return index < KeyList.Length ? db[KeyList[index]] : default;
        }
    }
}