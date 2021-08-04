using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EnumStringExtensionGenerator
{
    public static class CompilationExtensions
    {
        /// <summary>
        /// Gets the name of the namespace this symbol appears in
        /// </summary>
        public static string GetContainingNamespaceName(this ISymbol symbol)
        {
            var containingNamespace = symbol.ContainingNamespace;
            List<string> namespaceNames = new();
            while (containingNamespace != null && !containingNamespace.IsGlobalNamespace)
            {
                namespaceNames.Add(containingNamespace.Name);
                containingNamespace = containingNamespace.ContainingNamespace;
            }

            namespaceNames.Reverse();
            return symbol.ContainingNamespace.IsGlobalNamespace ? "" : string.Join(".", namespaceNames);
        }

        /// <summary>
        /// Gets the name of the type, prefixed with its namespace (if any)
        /// </summary>
        public static string GetQualifiedName(this ITypeSymbol type)
        {
            var namespaceName = type.GetContainingNamespaceName();
            return string.IsNullOrWhiteSpace(namespaceName)
                ? type.Name
                : $"{namespaceName}.{type.Name}";
        }

        #region Methods to look up the resource class, its public properties, etc

        public static AttributeData GetLocalizationAttribute(this INamedTypeSymbol enumTypeSymbol)
        {
            static bool IsLocalizationGenerationAttribute(AttributeData attributeData)
            {
                if (attributeData.AttributeClass == null)
                    return false;

                return attributeData.AttributeClass.Name.Contains("GenerateLocalisationAttribute");
            }

            return enumTypeSymbol
                .GetAttributes()
                .FirstOrDefault(IsLocalizationGenerationAttribute);
        }

        /// <summary>
        /// Looks at the defined attribute and gets the name of the property to use for the default case
        /// </summary>
        public static string GetDefaultPropertyName(this AttributeData localizedAttribute)
        {
            var possibleDefaultName = localizedAttribute.NamedArguments.Any(pair => pair.Key == "DefaultPropertyName")
                ? localizedAttribute.NamedArguments.First(pair => pair.Key == "DefaultPropertyName").Value.Value
                : null;

            var defaultPropertyName = possibleDefaultName?.ToString();
            return string.IsNullOrWhiteSpace(defaultPropertyName) ? null : defaultPropertyName;
        }

        public static ITypeSymbol GetResourceManager(this AttributeData localizationAttribute)
        {
            return (ITypeSymbol)localizationAttribute.ConstructorArguments.First().Value;
        }

        public static IEnumerable<string> GetStaticStringPropertyNames(this ITypeSymbol resourceManager)
        {
            return resourceManager
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(item => item.IsStatic)
                .Where(item => item.Type.Name == nameof(String))
                .Select(item => item.Name)
                .ToList();
        }

        #endregion

        #region Methods to extract information about symbols and figure out what we want to do with them

        public static EnumInfo CreateEnumInfo(this INamedTypeSymbol typeSymbol)
        {
            var localizationAttribute = typeSymbol.GetLocalizationAttribute();
            var requestedExtension = localizationAttribute != null
                ? typeSymbol.CreateRequestedLocalizationExtensionInfo(localizationAttribute)
                : null;

            return new EnumInfo(typeSymbol.Name,
                typeSymbol.GetContainingNamespaceName(),
                typeSymbol.DeclaredAccessibility,
                requestedExtension);
        }

        private static RequestedLocalizationExtensionInfo CreateRequestedLocalizationExtensionInfo(this INamedTypeSymbol typeSymbol,
            AttributeData localizationAttribute)
        {
            ITypeSymbol resourceManager = localizationAttribute.GetResourceManager();
            var defaultName = localizationAttribute.GetDefaultPropertyName();
            var resourceManagerName = resourceManager.GetQualifiedName();
            var propertyNames = resourceManager.GetStaticStringPropertyNames().ToList();

            string MakeExpectedPropertyName(string enumValueName)
            {
                return $"{typeSymbol.Name}{enumValueName}Description";
            }

            bool DoesPropertyNameExist(string expectedPropertyName)
            {
                return !string.IsNullOrWhiteSpace(expectedPropertyName) && propertyNames.Contains(expectedPropertyName);
            }

            string GetValidQualifiedPropertyName(string expectedPropertyName)
            {
                return DoesPropertyNameExist(expectedPropertyName)
                    ? $"{resourceManagerName}.{expectedPropertyName}"
                    : null;
            }

            EnumValueLocalisation PrepareEnumValue(string enumValueName)
            {
                var expectedPropertyName = MakeExpectedPropertyName(enumValueName);
                var qualifiedPropertyName = GetValidQualifiedPropertyName(expectedPropertyName);
                return new(enumValueName, qualifiedPropertyName);
            }

            var localisations = typeSymbol.MemberNames.Select(PrepareEnumValue);
            var defaultCase = new DefaultEnumLocalisation(defaultName, GetValidQualifiedPropertyName(defaultName));

            return new RequestedLocalizationExtensionInfo(typeSymbol.Name,
                defaultCase,
                localisations);
        }

        public static INamedTypeSymbol GetEnumSymbol(this Compilation compilation, EnumDeclarationSyntax enumDeclarationSyntax)
        {
            var semanticModel = compilation.GetSemanticModel(enumDeclarationSyntax.SyntaxTree);
            var typeSymbol = semanticModel.GetDeclaredSymbol(enumDeclarationSyntax);
            return typeSymbol;
        }

        #endregion

        /// <summary>
        /// Utility method to simplify the reporting of an error
        /// </summary>
        public static Diagnostic CreateDiagnostic(this GenerationError error, Location location)
        {
            string code = error.ErrorCode.Id();

            switch (error.ErrorCode)
            {
                case GenerationErrorCode.UnspecifiedError:
                    return Diagnostic.Create(
                        code,
                        "String Generator",
                        $"Unexpected error generating extension helpers; error is {error.ExtraData[GenerationError.PropertyNames.ExceptionMessage]}",
                        defaultSeverity: DiagnosticSeverity.Error,
                        severity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        warningLevel: 0
                        );
                case GenerationErrorCode.MissingRequiredDefaultPropertyName:
                    return Diagnostic.Create(
                        code,
                        "String Generator",
                        $"Enum type {error.EnumTypeName} requires a default string for GetDescription and none is available; a method has been generated anyway but will likely result in runtime errors",
                        defaultSeverity: DiagnosticSeverity.Error,
                        severity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        warningLevel: 0,
                        location: location
                    );
                case GenerationErrorCode.DefaultPropertyNameInvalid:
                    object originalDefaultProperty =
                        error.ExtraData[GenerationError.PropertyNames.OriginalDefaultPropertyName];
                    return Diagnostic.Create(
                        code,
                        "String Generator",
                        $"Enum type {error.EnumTypeName} was given a default property of { originalDefaultProperty } for GetDescription, but that property wasn't found",
                        defaultSeverity: DiagnosticSeverity.Error,
                        severity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        warningLevel: 0,
                        location: location
                    );
                default:
                    throw new ArgumentException($"Unrecognised error type {error.ErrorCode}");
            }
        }
    }
}