using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace EnumStringExtensionGenerator
{
    public static class SourceGenerationHelperExtensions
    {
        public static string Indent(this string code)
        {
            Regex newline = new Regex(@"^(.*)$", RegexOptions.Multiline);
            var lines = ((IEnumerable<Match>) newline.Matches(code)).Select(match => "    " + match.Groups[0]).ToList();
            return string.Join("\n", lines);
        }

        public static string InNamespace(this string code, string namespaceName)
        {
            return string.IsNullOrWhiteSpace(namespaceName) 
                ? code 
                : $@"namespace {namespaceName}
{{
{code.Indent()}
}}";
        }

        /// <summary>
        /// Generates a string containing the body of a file that provides extension methods for a given enum
        /// </summary>
        public static string GenerateExtensionClassFile(this EnumInfo enumInfo)
        {
            var enumHelperExtensionSourceFile = $@"using System;

{GetAccessibilityText(enumInfo.Accessibility)} static class {enumInfo.ExtensionClassName}
{{
{enumInfo.LiteralValueLookups.GenerateExtensionMethod() }
}}"
                .InNamespace(enumInfo.NamespaceName);

            return enumHelperExtensionSourceFile;
        }

        /// <summary>
        /// Generates the source code for an extension method that can be used to look up an appropriate localised
        /// string based on an enum value.
        /// </summary>
        public static string GenerateExtensionMethod(this EnumValueLocalisationLookupInfo extensionInfo)
        {
            string GenerateCase(EnumValueLocalisation localisationCase)
            {
                return 
$@"            case {extensionInfo.EnumTypeName}.{localisationCase.OriginalValueName}:
                discoveredValue = {localisationCase.ValueToReturn};
                break;";
            }

            static string GenerateDefault(DefaultEnumLocalisation defaultCase)
            {
                return defaultCase.HasReturnValue
                    ? $@"discoveredValue = {defaultCase.ValueToReturn};
                break;"
                    : @"throw new ArgumentException(""Provided value was invalid"", nameof(valueToLookup));";
            }

            string GenerateReturn()
            {
                return extensionInfo.HasFormatting
                    ? "return string.Format(discoveredValue, detail);"
                    : "return discoveredValue;";
            }

            string parameter = extensionInfo.HasFormatting ? ", string detail" : "";


            return $@"
    public static string {extensionInfo.MethodName}(this {extensionInfo.EnumTypeName} valueToLookup{parameter})
    {{
        string discoveredValue = null;
        switch (valueToLookup)
        {{
{
    string.Join("\n", extensionInfo.Localisations.Select(GenerateCase))
}
            default:
                { GenerateDefault(extensionInfo.Default) }
        }}
        { GenerateReturn() }
    }}
";
        }
        private static string GetAccessibilityText(Accessibility declaredAccessibility)
        {
            string accessibility;
            switch (declaredAccessibility)
            {
                case Accessibility.Internal:
                    accessibility = "internal";
                    break;
                case Accessibility.Public:
                    accessibility = "public";
                    break;
                default:
                    accessibility = "";
                    break;
            }

            accessibility = string.IsNullOrWhiteSpace(accessibility)
                ? ""
                : accessibility + " ";
            return accessibility;
        }
    }
}