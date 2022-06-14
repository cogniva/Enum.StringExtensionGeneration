using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace EnumStringExtensionGenerator.Test
{
    /// <summary>
    /// A collection of extension methods to somewhat simplify the creation of source code for use in testing.
    /// </summary>
    /// <remarks>
    /// These are intended to be very simple, fairly brute-force tools, aiming primarily at making the scenario clear
    /// rather than being perfectly-designed source generation tools themselves.
    /// </remarks>
    public static class SampleSourceCodeGenerator
    {
        public const string ExtensionNamespace = "EnumStringExtensionGenerator";

        #region Generation of enum classes

        public static string MakeEnumWithValues(this string enumName, params string[] valueNames)
        {
            return $@"public enum {enumName}
{{
{ string.Join(",\n", valueNames).Indent()}
}}";
        }

        public static string WithLocalisation(this string code, string localisationNamespace, string localisationClassName, string defaultName = null, string resourceNameFormat = null)
        {
            string defaultPropertyNameParam = string.IsNullOrWhiteSpace(defaultName)
                ? ""
                : $@", DefaultPropertyName=""{defaultName}""";
            string resourceNameFormatParam = string.IsNullOrWhiteSpace(resourceNameFormat)
                ? ""
                : $@", ResourceNameFormat=""{resourceNameFormat}""";

            return
                $@"[WithLiteralLocalisation(typeof({localisationClassName.WithNamespacePrefix(localisationNamespace)}){defaultPropertyNameParam}{resourceNameFormatParam})]
{code}";

        }

        #endregion

        #region Generation of a mock that works as a resource class

        public static string MakeLocalisedStrings(this string className, params (string PropertyName, string ReturnValue)[] properties)
        {
            static string MakePropertyLine((string PropertyName, string ReturnValue) property)
            {
                return $@"public static string {property.PropertyName} => ""{property.ReturnValue}"";";
            }

            return $@"public class {className}
{{
{string.Join("\n", properties.Select(MakePropertyLine)).Indent()}
}}
";
        }

        #endregion

        #region Generation of pairs of property name & return value, for use in creating a mock of a resource class

        public static Func<string, (string Name, string ReturnValue)> PairGenerator(this string enumName)
        {
            return valueName => MakePair($"{enumName}{{0}}Description", valueName);
        }

        public static Func<string, (string Name, string ReturnValue)> PairGenerator(this string enumName,
            string resourceNameFormat)
        {
            return valueName => (string.Format(resourceNameFormat, enumName, valueName), valueName);
        }

        private static (string Name, string ReturnValue) MakePair(string enumFormat, string valueName)
        {
            return (string.Format(enumFormat, valueName), valueName);
        }

        #endregion

        #region Creating a test class to actually use the generated code

        public static string CallEnumValues(this string enumName, params string[] enumValues)
        {
            static string LogSingleValue(string enumName, string enumValue)
            {
                return @$"    System.Console.WriteLine(""Value of {enumName}.{enumValue} is {{0}}"", {enumName}.{enumValue}.GetDescription());";
            }

            return string.Join("\n",
                enumValues.Select(enumValue => LogSingleValue(enumName, enumValue)))
                .Indent();
        }

        public static string CreateCallingFile(string enumNamespace, string enumName, params string[] enumValues)
        {
            var callingFile = $@"public class TestingClass 
{{
    public static void Main(string[] args)
    {{
{CallEnumValues(enumName, enumValues)}
    }}
}}"
                .InNamespace("Testing")
                .Using("System");
            if (!string.IsNullOrWhiteSpace(enumNamespace))
                callingFile = callingFile.Using(enumNamespace);
            return callingFile;
        }

        #endregion

        public static string UsingExtensionNamespace(this string file)
        {
            return file.Using(ExtensionNamespace);
        }

        public static string Using(this string file, params string[] namespaces)
        {
            return $@"{string.Join("\n", namespaces.Select(namespaceName => $"using {namespaceName};"))}
{file}";
        }

        public static string WithNamespacePrefix(this string typeName, string namespaceName)
        {
            return string.IsNullOrWhiteSpace(namespaceName) ? typeName : $"{namespaceName}.{typeName}";
        }
    }
}