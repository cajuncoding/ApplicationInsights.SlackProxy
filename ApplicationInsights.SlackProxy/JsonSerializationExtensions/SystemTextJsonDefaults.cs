using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SystemTextJsonExtensions.GlobalDefaults
{
    public static class SystemTextJsonDefaults
    {
        public const string EmptyJsonObject = "{}";

        //Provide a Maestro facade over the Nuget package we use to help support Global Json Options management....
        public static JsonSerializerOptions DefaultSerializerOptions { get; set; } = new JsonSerializerOptions();

        public static readonly JsonSerializerOptions RelaxedCaseInsensitiveCamelCaseDefaults = new Func<JsonSerializerOptions>(() =>
        {
            var options = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
            };

            //Add Converters that will help provide more relaxed parsing (similar to Newtonsoft.Json)...
            var converters = options.Converters;
            converters.Add(new JsonRelaxedDateTimeConverter());
            converters.Add(new JsonRelaxedDateTimeOffsetConverter());
            converters.Add(new JsonRelaxedBooleanStringConverter());
            converters.Add(new JsonRelaxedUriStringConverter());

            return options;
        }).Invoke();

        public static void InitializeRelaxedCaseInsensitiveCamelCaseDefaults()
        {
            //System.Text.Json -- Configure loose default settings for Flurl with SystemTextJson (e.g. Camel Case, Case-insensitivity, Don't write nulls, etc.)...
            //NOTE: System.Text.Json is still missing support for Global changes to default Serialization settings...
            //      So we use this open source solution to provide an elegant workaround (available on Nuget): https://github.com/FetchGoods/Text.Json.Extensions/tree/master
            //      More info. on Stack Overflow here: https://stackoverflow.com/q/58331479/7293142
            DefaultSerializerOptions = RelaxedCaseInsensitiveCamelCaseDefaults;
        }
    }
}
