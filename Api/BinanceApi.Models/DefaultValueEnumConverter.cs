using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BinanceApi.Models
{
    public class DefaultValueEnumConverter : StringEnumConverter
    {
        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object value,
            JsonSerializer serializer)
        {
            try
            {
                return base.ReadJson(reader, objectType, value, serializer);
            }
            catch (JsonSerializationException)
            {
                if (!(Attribute.GetCustomAttribute(objectType, typeof(DefaultValueAttribute)) is DefaultValueAttribute
                        defaultValueAttribute))
                {
                    throw;
                }

                var defaultValue = defaultValueAttribute.Value;
                if (defaultValue.GetType() != objectType)
                {
                    throw new JsonSerializationException(
                        $"Default value type ({defaultValue.GetType()}) doesn't match enum type ({objectType})");
                }

                return defaultValue;
            }
        }
    }
}