#if NETSTANDARD2_1
// Polyfill for record types on .NET Standard 2.1
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif
