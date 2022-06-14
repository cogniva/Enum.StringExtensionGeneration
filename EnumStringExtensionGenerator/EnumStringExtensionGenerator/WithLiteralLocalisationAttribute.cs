using System;
using System.Collections.Generic;
using System.Text;

namespace EnumStringExtensionGenerator
{
    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = true)]
    public class WithLiteralLocalisationAttribute : Attribute
    {
        public Type ResourceManager { get; }
        public string DefaultPropertyName { get; set; }
        public string ResourceNameFormat { get; set; }

        public WithLiteralLocalisationAttribute(Type resourceManager)
        {
            ResourceManager = resourceManager;
        }
    }

    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = true)]
    public class WithFormattedLocalisationAttribute : Attribute
    {
        public Type ResourceManager { get; }
        public string DefaultPropertyName { get; set; }
        public string ResourceNameFormat { get; set; }

        public WithFormattedLocalisationAttribute(Type resourceManager)
        {
            ResourceManager = resourceManager;
        }
    }
}