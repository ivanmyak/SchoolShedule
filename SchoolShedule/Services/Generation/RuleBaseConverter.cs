using SchoolShedule.Entities.Generation.Rules;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SchoolShedule.Services.Generation
{
    /// <summary>
    /// JSON-конвертер для сериализации и десериализации наследников RuleBase.
    /// </summary>
    public class RuleBaseConverter : JsonConverter<RuleBase>
    {
        /// <summary>
        /// Десериализует объект правила из JSON с учётом типа правила.
        /// </summary>
        public override RuleBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var typeName = root.GetProperty("RuleType").GetString();
            if (string.IsNullOrWhiteSpace(typeName))
                throw new JsonException("Отсутствует поле RuleType для десериализации RuleBase.");

            var type = Type.GetType(typeName);
            if (type == null)
                throw new JsonException($"Не удалось найти тип правила: {typeName}.");

            var json = root.GetRawText();
            return (RuleBase)JsonSerializer.Deserialize(json, type, options)!;
        }

        /// <summary>
        /// Сериализует объект правила в JSON с добавлением информации о типе.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, RuleBase value, JsonSerializerOptions options)
        {
            var typeName = value.GetType().AssemblyQualifiedName
                           ?? throw new JsonException("Не удалось определить тип правила для сериализации.");

            var json = JsonSerializer.Serialize(value, value.GetType(), options);
            using var doc = JsonDocument.Parse(json);

            writer.WriteStartObject();
            writer.WriteString("RuleType", typeName);

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                prop.WriteTo(writer);
            }

            writer.WriteEndObject();
        }
    }
}
