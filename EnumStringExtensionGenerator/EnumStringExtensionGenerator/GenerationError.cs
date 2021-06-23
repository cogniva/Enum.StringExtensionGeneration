using System.Collections.Generic;

namespace EnumStringExtensionGenerator
{
    public enum GenerationErrorCode
    {
        UnspecifiedError,
        MissingRequiredDefaultPropertyName,
        DefaultPropertyNameInvalid,
    }

    public class GenerationError
    {
        public static class PropertyNames
        {
            public const string OriginalDefaultPropertyName = nameof(OriginalDefaultPropertyName);
            public const string ExceptionMessage = nameof(ExceptionMessage);
        }

        public string EnumTypeName { get; }
        public GenerationErrorCode ErrorCode { get; }
        public Dictionary<string, object> ExtraData { get; }

        public GenerationError(string enumTypeName, GenerationErrorCode errorCode)
            : this(enumTypeName, errorCode, null)
        {
        }
        public GenerationError(string enumTypeName, GenerationErrorCode errorCode, Dictionary<string, object> extraData)
        {
            EnumTypeName = enumTypeName;
            ErrorCode = errorCode;

            ExtraData = new Dictionary<string, object>(extraData ?? new Dictionary<string, object>());
        }

        public void AddData(string key, object value)
        {
            ExtraData[key] = value;
        }
    }

    public static class ErrorCodeExtensions
    {
        public static string Id(this GenerationErrorCode code)
        {
            return $"ESG{(int)code:0000}";
        }
    }
}