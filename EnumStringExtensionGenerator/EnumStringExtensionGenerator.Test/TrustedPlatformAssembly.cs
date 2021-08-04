using System;
using System.Collections.Generic;
using System.Linq;

namespace EnumStringExtensionGenerator.Test
{
    // Adapted from https://stackoverflow.com/a/61321855
    public static class TrustedPlatformAssembly
    {
        public static string From(string shortDllName)
        {
            string dllString = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES").ToString();

            // The list is delimited with ; on Windows, but apparently with : on Linux, so we have to split on both.
            var dlls = dllString.Split(";:".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string dll = dlls.Single(d => d.Contains(shortDllName, StringComparison.OrdinalIgnoreCase));
            return dll;
        }

        public static IEnumerable<string> GetDllsToReference()
        {
            List<string> dllsToReturn = new List<string>
            {
                "mscorlib.dll",
                "system.runtime.dll",
                "System.Console.dll",
            };

            foreach (var dll in dllsToReturn)
            {
                yield return TrustedPlatformAssembly.From(dll);
            }
        }
    }
}