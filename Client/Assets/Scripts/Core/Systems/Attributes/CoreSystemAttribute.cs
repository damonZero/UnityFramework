using Attribute = global::System.Attribute;
using AttributeUsageAttribute = global::System.AttributeUsageAttribute;
using AttributeTargets = global::System.AttributeTargets;

namespace Core.Systems.Attributes
{
    /// <summary>
    /// Marks a Core architecture system for automatic container registration.
    /// Business code should use Model, not System.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CoreSystemAttribute : Attribute
    {
    }
}
