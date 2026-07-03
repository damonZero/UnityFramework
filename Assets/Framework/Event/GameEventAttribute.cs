using Attribute = global::System.Attribute;
using AttributeTargets = global::System.AttributeTargets;
using AttributeUsageAttribute = global::System.AttributeUsageAttribute;

namespace Framework.Event
{
    [AttributeUsage(AttributeTargets.Struct, Inherited = false)]
    public sealed class GameEventAttribute : Attribute
    {
    }
}
