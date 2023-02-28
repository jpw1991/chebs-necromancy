using System;
using System.Linq;

namespace ChebsNecromancy
{
    public class InternalName : Attribute
    {
        public readonly string Name;
        public InternalName(string internalName) => Name = internalName;
        
        public static string GetName(Enum value)
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value);
            var field = type.GetField(name);
            var descriptionAttribute = field.GetCustomAttributes(typeof(InternalName), false)
                .FirstOrDefault() as InternalName;
            return descriptionAttribute?.Name ?? value.ToString();
        }
    }
}