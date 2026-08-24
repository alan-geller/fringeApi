using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

sealed class DateTimeConverter : JsonConverter<DateTime>
{
	private const string InputFormat = "yyyy-MM-dd HH:mm:ss";

	public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		string? value = reader.GetString();

		if (string.IsNullOrWhiteSpace(value))
		{
			throw new JsonException("Expected a non-empty datetime string.");
		}

		if (DateTime.TryParseExact(value, InputFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
		{
			return result;
		}

		throw new JsonException($"Could not parse datetime '{value}' with format {InputFormat}.");
	}

	public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString(InputFormat, CultureInfo.InvariantCulture));
	}
}
