using System;
using System.Collections.Generic;
using System.Text;

namespace EnumStringExtensionGenerator
{
    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = true)]
    public class GenerateLocalisationAttribute : Attribute
    {
        public Type ResourceManager { get; }
        public string DefaultPropertyName { get; set; }

        public GenerateLocalisationAttribute(Type resourceManager)
        {
            ResourceManager = resourceManager;
        }
    }
}