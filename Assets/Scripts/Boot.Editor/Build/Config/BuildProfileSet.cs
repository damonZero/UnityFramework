using System.Collections.Generic;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Profile 集合 —— ScriptableObject，作为 Odin Dashboard 的列表入口。
    /// 也可通过菜单创建多个预定义 Profile。
    /// </summary>
    [CreateAssetMenu(fileName = "BuildProfileSet", menuName = "KJ/Build Profile Set")]
    public class BuildProfileSet : ScriptableObject
    {
        [Tooltip("Profile 列表")]
        public List<BuildProfile> Profiles = new List<BuildProfile>();
    }
}
