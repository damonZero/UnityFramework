// Polyfill: Unity's .NET Standard 2.1 / .NET Framework scripting backends do not ship
// System.Runtime.CompilerServices.IsExternalInit, which is required by C# 9 'init' accessors.
// Define it here so BootStartupLogEntry's init-only properties compile under Unity 2022.3.
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}
