using System.Reflection;
using System.Runtime.InteropServices;

namespace AxKit.Tests;

/// <summary>
/// Every function the bundle declares from a Windows library is there under the name it is declared with: asked of
/// Windows itself, from the built assemblies (the review of the sessions of 2026-11-25). NativeMenu declared
/// PostMessage with exact spelling, which user32 has only as PostMessageW, and it failed only when a form field's
/// templates were opened in an NVDA pass, as an unexpected error. With exact spelling the name must be there as
/// written; without it, as written or with the W or A of a wide or narrow version, as the runtime looks it up.
/// </summary>
public class NativeEntryPointTests
{
    [Fact]
    public void Every_declared_windows_function_is_found_under_its_name()
    {
        var missing = new List<string>();
        var checkedCount = 0;
        foreach (var file in Directory.GetFiles(AppContext.BaseDirectory, "Ax*.dll"))
        {
            if (Path.GetFileName(file).Contains(".Tests", StringComparison.Ordinal))
            {
                continue;
            }

            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFrom(file);
            }
            catch (BadImageFormatException)
            {
                continue;
            }

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = [.. ex.Types.OfType<Type>()];
            }

            foreach (var type in types)
            {
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (method.GetCustomAttribute<DllImportAttribute>() is not { } import)
                    {
                        continue;
                    }

                    checkedCount++;
                    var name = string.IsNullOrEmpty(import.EntryPoint) ? method.Name : import.EntryPoint!;
                    if (!NativeLibrary.TryLoad(import.Value, typeof(NativeEntryPointTests).Assembly, DllImportSearchPath.System32, out var library))
                    {
                        missing.Add($"{type.FullName}.{method.Name}: the library {import.Value} does not load");
                        continue;
                    }

                    var found = NativeLibrary.TryGetExport(library, name, out _)
                        || (!import.ExactSpelling && (NativeLibrary.TryGetExport(library, name + "W", out _) || NativeLibrary.TryGetExport(library, name + "A", out _)));
                    if (!found)
                    {
                        missing.Add($"{type.FullName}.{method.Name}: {import.Value} has no {name}{(import.ExactSpelling ? " (exact spelling: write the W of the wide version)" : string.Empty)}");
                    }
                }
            }
        }

        Assert.True(checkedCount > 100, $"Only {checkedCount} declarations were found: are the bundle's assemblies beside the tests?");
        Assert.True(missing.Count == 0, "Windows functions not found under their declared name:\n" + string.Join("\n", missing));
    }
}
