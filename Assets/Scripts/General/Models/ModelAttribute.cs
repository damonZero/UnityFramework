using Attribute = global::System.Attribute;
using AttributeUsageAttribute = global::System.AttributeUsageAttribute;
using AttributeTargets = global::System.AttributeTargets;

namespace General
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ModelAttribute : Attribute
    {
    }
}
