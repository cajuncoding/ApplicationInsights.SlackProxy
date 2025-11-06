using System;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SystemTextJsonExtensions.GlobalDefaults;

namespace SystemTextJsonExtensions
{
    public static class SystemTextJsonExtensions
    {
        public static bool IsDuckTypedJson(this string jsonText)
        {
            if (string.IsNullOrWhiteSpace(jsonText))
                return false;

            var text = jsonText.Trim();
            return
                (text.StartsWith('{') && text.EndsWith('}')) //For object
                || (text.StartsWith('[') && text.EndsWith(']')); //For array
        }

        public static string EscapeJsonForLogging(this string json)
            => json.IsDuckTypedJson() 
                ? json.EscapeCharacters(('{', "{{"), ('}', "}}"))
                : json;

        public static T FromJsonTo<T>(this string json, JsonSerializerOptions options = null)
            => !string.IsNullOrWhiteSpace(json) 
                ? JsonSerializer.Deserialize<T>(json, options ?? SystemTextJsonDefaults.DefaultSerializerOptions)
                : default;

        public static string ToJson(this object value, JsonSerializerOptions options = null)
            => JsonSerializer.Serialize(value, options ?? SystemTextJsonDefaults.DefaultSerializerOptions);

        public static string ToJson<T>(this T value, JsonSerializerOptions options = null)
            => JsonSerializer.Serialize<T>(value, options ?? SystemTextJsonDefaults.DefaultSerializerOptions);

        public static JsonNode ToJsonNode(this object obj, JsonSerializerOptions options = null) => obj is not null
            ? JsonSerializer.SerializeToNode(obj, options ?? SystemTextJsonDefaults.DefaultSerializerOptions)
            : null;

        public static T FromJsonTo<T>(this JsonNode jsonNode, JsonSerializerOptions options = null)
            => jsonNode.Deserialize<T>(options ?? SystemTextJsonDefaults.DefaultSerializerOptions);

        public static string ToJsonIndented(this object obj, JsonSerializerOptions options = null)
        {
            if (obj is null) return null;

            //NOTE: This will use the SystemTextJsonDefaults.DefaultSerializerOptions as defined in the Application Root Startup!
            var jsonWriteIndentedOptions = new JsonSerializerOptions(options ?? SystemTextJsonDefaults.DefaultSerializerOptions) { WriteIndented = true };
            return obj.ToJson(jsonWriteIndentedOptions);
        }

        public static T GetConvertedValue<T>(this JsonNode node, JsonSerializerOptions options = null)
            => node is not null
                // Use JsonSerializer to deserialize the node into the target type with full support for all configured converters!
                ? JsonSerializer.Deserialize<T>(node, options ?? SystemTextJsonDefaults.DefaultSerializerOptions)
                : default;

        public static string EscapeCharacters(this string input, params (char findChar, string replacementString)[] replacements)
        {
            if (string.IsNullOrEmpty(input) || replacements == null || replacements.Length == 0)
                return input;

            // Estimate worst-case expansion: assume every char is replaced with the longest replacement
            int maxExpansion = replacements.Max(r => r.replacementString.Length);
            var stringBuilder = new StringBuilder(input.Length * maxExpansion);

            foreach (char ch in input)
            {
                bool replaced = false;
                foreach (var (findChar, replacementString) in replacements)
                {
                    if (ch == findChar)
                    {
                        stringBuilder.Append(replacementString);
                        replaced = true;
                        break;
                    }
                }

                if (!replaced)
                    stringBuilder.Append(ch);
            }

            return stringBuilder.ToString();
        }
    }

    #region Custom Converters for System.Text.Json

    //NOTE: We use Macross.Json.Extensions to greatly simplify providing simple value converters such as this...
    public class JsonRelaxedDateTimeConverter() : JsonDelegatedStringConverter<DateTime>(
        value => DateTime.Parse(value),
        value => value.ToString("s", CultureInfo.InvariantCulture)
    );

    //NOTE: We use Macross.Json.Extensions to greatly simplify providing simple value converters such as this...
    public class JsonRelaxedDateTimeOffsetConverter() : JsonDelegatedStringConverter<DateTimeOffset>(
        value => DateTimeOffset.Parse(value),
        value => value.ToString("s", CultureInfo.InvariantCulture)
    );

    //NOTE: We use Macross.Json.Extensions to greatly simplify providing simple value converters such as this...
    public class JsonRelaxedUriStringConverter() : JsonDelegatedStringConverter<Uri>(
        value => string.IsNullOrEmpty(value) ? null : new Uri(value),
        value => value?.ToString()
    );

    /// <summary>
    /// A relaxed Boolean value converter for System.Text.Json that works with bool or string bool (e.g. 'true', 'false) case-insensitive values.
    /// Inspired and adapted from original StackOverflow source here: https://stackoverflow.com/a/75089641/7293142
    /// Enhanced to have streamlined code, support case-insensitive matching, and improved exception messages.
    ///
    /// Taken directly from Original @CajunCoding's Gist Source: https://gist.github.com/cajuncoding/00896396fdeddabdd661aca8524165d1
    /// 
    /// </summary>
    public class JsonRelaxedBooleanStringConverter() : JsonConverter<bool>
    {
        private static readonly JsonException BooleanParsingException = new("The boolean property could not be read as a valid boolean" +
                                                                            " json value or parsed from boolean string value (e.g. 'true'/'false').");
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.String => reader.GetString() switch
            {
                //NOTE: string.Equals() is null safe...
                var value when string.Equals(value, bool.TrueString, StringComparison.OrdinalIgnoreCase) => true,
                var value when string.Equals(value, bool.FalseString, StringComparison.OrdinalIgnoreCase) => false,
                _ => throw BooleanParsingException
            },
            _ => throw BooleanParsingException
        };

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) => writer.WriteBooleanValue(value);
    }

    #endregion
}
