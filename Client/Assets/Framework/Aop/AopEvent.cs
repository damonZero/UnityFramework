using System;

namespace Framework.Aop
{
    /// <summary>
    /// AOP 性能事件数据模型。
    /// 注意：使用 public field 而非 property 是因为 UnityEngine.JsonUtility 不支持属性序列化。
    /// </summary>
    [Serializable]
    public sealed class AopEvent
    {
        public string SchemaVersion = "1.0.0";
        public string RunId;
        public string SpanId;
        public string ParentSpanId;
        public string Name;
        public string Category;
        public string StartedAtUtc;
        public long DurationTicks;
        public double DurationMs;
        public int ThreadId;
        public string Status;
        public string ExceptionType;
    }
}
