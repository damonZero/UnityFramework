using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Stage 注册表 —— 管理所有 Stage 的注册、排序和依赖验证。
    /// </summary>
    public static class BuildStageRegistry
    {
        private static readonly Dictionary<string, IBuildStage> StagesById = new Dictionary<string, IBuildStage>();
        private static readonly List<IBuildStage> OrderedStages = new List<IBuildStage>();
        private static bool _initialized = false;

        /// <summary>获取按 Order 排列的所有已注册 Stage</summary>
        public static IReadOnlyList<IBuildStage> GetAll()
        {
            EnsureInitialized();
            return OrderedStages;
        }

        /// <summary>根据 ID 查找 Stage</summary>
        public static IBuildStage GetById(string id)
        {
            EnsureInitialized();
            StagesById.TryGetValue(id, out var stage);
            return stage;
        }

        /// <summary>注册一个 Stage</summary>
        public static void Register(IBuildStage stage)
        {
            if (StagesById.ContainsKey(stage.Id))
            {
                Debug.LogWarning($"[BuildStageRegistry] Duplicate stage ID: {stage.Id}");
                return;
            }
            StagesById[stage.Id] = stage;
            OrderedStages.Add(stage);
            OrderedStages.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        /// <summary>注册多个 Stage</summary>
        public static void RegisterAll(params IBuildStage[] stages)
        {
            foreach (var stage in stages)
                Register(stage);
        }

        /// <summary>验证依赖完整性：所有 DependsOn 引用的 Stage 必须存在，且必须在本 Stage 之前</summary>
        public static List<string> ValidateDependencies()
        {
            EnsureInitialized();
            var errors = new List<string>();
            foreach (var stage in OrderedStages)
            {
                foreach (string depId in stage.DependsOn)
                {
                    if (!StagesById.TryGetValue(depId, out var depStage))
                    {
                        errors.Add($"Stage '{stage.Id}' depends on '{depId}' which is not registered");
                        continue;
                    }
                    if (depStage.Order >= stage.Order)
                    {
                        errors.Add($"Stage '{stage.Id}' (order={stage.Order}) depends on '{depId}' "
                            + $"(order={depStage.Order}), but dependency must run before dependent");
                    }
                }
            }
            return errors;
        }

        /// <summary>清除所有注册（测试用）</summary>
        public static void Clear()
        {
            StagesById.Clear();
            OrderedStages.Clear();
            _initialized = false;
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            // 注册所有 Stage（通过反射扫描或显式注册）
            RegisterAll(
                new P0_PlanStage(),
                new P1_PreflightStage(),
                new P2_GenerateStage(),
                new P3_HybridCLRStage(),
                new P4_BuildAssetStage(),
                new P5_ApplyConfigStage(),
                new P6_BuildPlayerStage(),
                new P7_VerifyStage(),
                new P8_SmokeStage(),
                new P9_ReportStage()
            );
        }
    }
}
