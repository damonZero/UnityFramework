using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Framework.BuildPipeline.Plan;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// 构建配置事务系统 —— 负责所有会修改项目状态的操作的 snapshot/rollback。
    /// 涵盖：AssetConfig.asset、PlayerSettings defines、Android 签名。
    /// 原则：任何 Stage 需要修改上述对象时，必须先 snapshot 再修改。
    /// </summary>
    public class BuildConfigTransaction
    {
        private readonly List<ISnapshot> _snapshots = new List<ISnapshot>();
        private bool _committed = false;

        /// <summary>是否已经 commit（不可再 rollback）</summary>
        public bool IsCommitted => _committed;

        // ===== Snapshot 接口 =====

        private interface ISnapshot
        {
            void Restore();
            string Description { get; }
        }

        // ===== File Snapshot =====

        private class FileSnapshot : ISnapshot
        {
            private readonly string _path;
            private readonly byte[] _originalContent;
            public string Description => $"File: {_path}";

            public FileSnapshot(string path)
            {
                _path = path;
                _originalContent = File.Exists(path) ? File.ReadAllBytes(path) : null;
            }

            public void Restore()
            {
                if (_originalContent == null)
                {
                    if (File.Exists(_path))
                        File.Delete(_path);
                }
                else
                {
                    File.WriteAllBytes(_path, _originalContent);
                }
            }
        }

        // ===== Text Setting Snapshot =====

        private class TextSettingSnapshot : ISnapshot
        {
            private readonly string _key;
            private readonly string _original;
            private readonly Action<string> _setter;
            public string Description => $"Setting: {_key}";

            public TextSettingSnapshot(string key, string original, Action<string> setter)
            {
                _key = key; _original = original; _setter = setter;
            }

            public void Restore() => _setter(_original);
        }

        // ===== Bool Setting Snapshot =====

        private class BoolSettingSnapshot : ISnapshot
        {
            private readonly string _key;
            private readonly bool _original;
            private readonly Action<bool> _setter;
            public string Description => $"Setting: {_key}";

            public BoolSettingSnapshot(string key, bool original, Action<bool> setter)
            {
                _key = key; _original = original; _setter = setter;
            }

            public void Restore() => _setter(_original);
        }

        // ===== 公共 API =====

        /// <summary>快照一个文件（保存原始内容，供 rollback 恢复）</summary>
        public void SnapshotFile(string path)
        {
            string fullPath = Path.GetFullPath(path);
            _snapshots.Add(new FileSnapshot(fullPath));
            Debug.Log($"[Transaction] Snapshot file: {path}");
        }

        /// <summary>快照 PlayerSettings 字符串类设置</summary>
        public void SnapshotTextSetting(string key, Action<string> setter, Func<string> getter)
        {
            string original = getter();
            _snapshots.Add(new TextSettingSnapshot(key, original, setter));
            Debug.Log($"[Transaction] Snapshot setting: {key} = '{original}'");
        }

        /// <summary>快照 PlayerSettings bool 类设置</summary>
        public void SnapshotBoolSetting(string key, Action<bool> setter, Func<bool> getter)
        {
            bool original = getter();
            _snapshots.Add(new BoolSettingSnapshot(key, original, setter));
            Debug.Log($"[Transaction] Snapshot setting: {key} = {original}");
        }

        /// <summary>提交事务（标记为完成，放弃 rollback 能力）</summary>
        public void Commit()
        {
            _committed = true;
            _snapshots.Clear();
            Debug.Log("[Transaction] Committed");
        }

        /// <summary>回滚所有快照过的变更</summary>
        public void Rollback()
        {
            if (_committed)
            {
                Debug.Log("[Transaction] Already committed, rollback skipped");
                return;
            }

            Debug.Log($"[Transaction] Rolling back {_snapshots.Count} snapshot(s)...");

            // 反向恢复（最后改的先恢复）
            for (int i = _snapshots.Count - 1; i >= 0; i--)
            {
                try
                {
                    _snapshots[i].Restore();
                    Debug.Log($"[Transaction] Restored: {_snapshots[i].Description}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Transaction] Failed to restore {_snapshots[i].Description}: {ex.Message}");
                }
            }

            _snapshots.Clear();
            Debug.Log("[Transaction] Rollback complete");
        }

        /// <summary>回滚并输出差异报告</summary>
        public string RollbackAndReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Build Config Rollback Report ===");
            sb.AppendLine($"Snapshots: {_snapshots.Count}");
            foreach (var s in _snapshots)
                sb.AppendLine($"  - {s.Description}");
            Rollback();
            return sb.ToString();
        }

        // ===== 常用组合操作 =====

        /// <summary>快照 AssetConfig 资产文件</summary>
        public void SnapshotAssetConfig()
        {
            string configPath = "Assets/Resources/AssetConfig.asset";
            SnapshotFile(configPath);
        }

        /// <summary>快照 PlayerSettings Scripting Define Symbols</summary>
        public void SnapshotScriptingDefines(BuildTargetGroup targetGroup)
        {
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            SnapshotTextSetting(
                "ScriptingDefineSymbols",
                v => PlayerSettings.SetScriptingDefineSymbols(namedTarget, v),
                () => PlayerSettings.GetScriptingDefineSymbols(namedTarget));
        }

        /// <summary>快照 Android 签名设置</summary>
        public void SnapshotAndroidSigning()
        {
            SnapshotBoolSetting(
                "Android.useCustomKeystore",
                v => PlayerSettings.Android.useCustomKeystore = v,
                () => PlayerSettings.Android.useCustomKeystore);
        }
    }
}
