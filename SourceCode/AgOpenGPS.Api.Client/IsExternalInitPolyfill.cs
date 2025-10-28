// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill for IsExternalInit to enable C# 9.0 init setters in .NET Standard 2.0.
    /// This type is required by the C# compiler for init-only setters but is not included in .NET Standard 2.0.
    /// See: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/init
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
