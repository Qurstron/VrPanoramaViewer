using Newtonsoft.Json;
using System;
using System.Globalization;
using UnityEngine;

public class StringColorConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Color);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            //string value = reader.Value?.ToString();
            //return EnumUtils.ParseEnum(type, NamingStrategy, value, !AllowIntegerValues);
            string value = reader.Value?.ToString();
            return QUtils.StringToColor(value);
        }

        throw new Exception("idk");
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        Color color = (Color)value;
        writer.WriteValue(QUtils.FormatHexColor(color));
    }
}
