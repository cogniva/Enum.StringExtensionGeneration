using System.Collections.Generic;
using System.Linq;

namespace EnumStringExtensionGenerator
{
    /// <summary>
    /// Represents information about the mapping from enum values to static string properties (presumably on a
    /// resources class, but not necessarily) so that we can use that info to generate an extension method that will
    /// look up a relevant localised string for each value of an enum.
    /// </summary>
    public class RequestedLocalizationExtensionInfo
    {
        public RequestedLocalizationExtensionInfo(string enumTypeName,
            DefaultEnumLocalisation defaultCase,
            IEnumerable<EnumValueLocalisation> localisations)
        {
            EnumTypeName = enumTypeName;
            Default = defaultCase;

            var localisationList = localisations.ToList();

            var isOriginalDefaultValueInvalid = !string.IsNullOrWhiteSpace(defaultCase.OriginalPropertyName)
                                                && !defaultCase.HasReturnValue;

            // Gives a specific error message for the case where a default property name was supplied, but that
            // property doesn't exist
            if (isOriginalDefaultValueInvalid)
            {
                var error = new GenerationError(enumTypeName,
                    GenerationErrorCode.DefaultPropertyNameInvalid);
                var defaultPropertyName = defaultCase.OriginalPropertyName;
                error.AddData(GenerationError.PropertyNames.OriginalDefaultPropertyName, defaultPropertyName);
                _errors.Add(error);
            }

            var isDefaultMissingAndRequired =
                !localisationList.All(localisation => localisation.HasReturnValue)
                && !defaultCase.HasReturnValue;

            // If we don't have valid properties corresponding to all values, then we need to have a valid default
            // property to return. If we don't, we need to let the user know they've got something misconfigured.
            if (isDefaultMissingAndRequired)
            {
                _errors.Add(new GenerationError(enumTypeName, GenerationErrorCode.MissingRequiredDefaultPropertyName));
            }

            Localisations = localisationList.Where(localisation => localisation.HasReturnValue).ToList();
        }

        private readonly List<GenerationError> _errors = new();

        public string EnumTypeName { get; }
        public DefaultEnumLocalisation Default { get; }
        public IEnumerable<EnumValueLocalisation> Localisations { get; }

        public IEnumerable<GenerationError> Errors => _errors;
    }

    /// <summary>
    /// Represents information about what to do with the default case for an enum
    /// </summary>
    public record DefaultEnumLocalisation(string OriginalPropertyName, string ValueToReturn)
    {
        public bool HasReturnValue => !string.IsNullOrWhiteSpace(ValueToReturn);
    }

    /// <summary>
    /// Represents information about what to do for localisation of a specific enum value
    /// </summary>
    public record EnumValueLocalisation(string OriginalValueName, string ValueToReturn)
    {
        public bool HasReturnValue => !string.IsNullOrWhiteSpace(ValueToReturn);
    }
}