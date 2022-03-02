namespace DeltaSight.Statistics;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// https://github.com/dotnet/runtime/issues/63436
/// </summary>
public class JsonHandleAsAttribute : JsonConverterAttribute
{
    private readonly Type _derivedType;

    public JsonHandleAsAttribute(Type derivedType) => _derivedType = derivedType;

    public override JsonConverter? CreateConverter(Type _) => new DerivedTypeConverter(_derivedType);
}

public class DerivedTypeConverter : JsonConverterFactory
{
    private readonly Type _derivedType;

    public DerivedTypeConverter(Type derivedType) => _derivedType = derivedType;

    public override bool CanConvert(Type typeToConvert)
        // NB check doesn't cover interface implementations which require more involved reflection.
        // Interface support left as exercise to the reader.
        => typeToConvert != _derivedType && typeToConvert.IsAssignableFrom(_derivedType);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(DerivedTypeConverter<,>).MakeGenericType(_derivedType, typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType, options)!;
    }
}

public class DerivedTypeConverter<TDerived, TBase> : JsonConverter<TBase>
    where TDerived : TBase
{
    private readonly JsonConverter<TDerived> _derivedConverter;

    public DerivedTypeConverter(JsonSerializerOptions jsonSerializerOptions)
    {
        _derivedConverter = (JsonConverter<TDerived>)jsonSerializerOptions.GetConverter(typeof(TDerived));
    }

    public override TBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Type-safe upcast to TBase
        return _derivedConverter.Read(ref reader, typeof(TDerived), options);
    }

    public override void Write(Utf8JsonWriter writer, TBase value, JsonSerializerOptions options)
    {
        // Downcast can fail at runtime!
        _derivedConverter.Write(writer, (TDerived)value!, options);
    }
}