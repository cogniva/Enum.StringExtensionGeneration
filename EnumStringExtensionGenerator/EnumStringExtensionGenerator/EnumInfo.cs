using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace EnumStringExtensionGenerator
{
    /// <summary>
    /// Represents information about the enum we intend to generate helpful extension methods for.
    /// </summary>
    public class EnumInfo
    {
        public EnumInfo(string enumTypeName, string namespaceName, Accessibility accessibility, RequestedLocalizationExtensionInfo requestedExtension)
        {
            EnumTypeName = enumTypeName;
            NamespaceName = namespaceName;
            Accessibility = accessibility;
            RequestedExtension = requestedExtension;
            _errors = new List<GenerationError>(requestedExtension?.Errors ?? new GenerationError[0]);
        }

        private readonly List<GenerationError> _errors;

        public string ExtensionClassName => $"{EnumTypeName}LocalizationExtensions";

        public string EnumTypeName { get; }
        public string NamespaceName { get; }
        public Accessibility Accessibility { get; }
        public RequestedLocalizationExtensionInfo RequestedExtension { get; }

        public IEnumerable<GenerationError> Errors => _errors;
    }
}