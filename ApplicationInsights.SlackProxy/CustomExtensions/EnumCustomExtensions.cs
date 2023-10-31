#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace SlackProxy.CustomExtensions
{
    /// <summary>
    /// Adapted quick solution from Stack Overflow here:
    /// https://stackoverflow.com/a/69847593/7293142
    /// </summary>
    public static class EnumMemberNames
    {
        public static string? GetEnumMemberName<TEnum>(this TEnum value) where TEnum : struct, Enum
            => EnumAttributeCache<TEnum>.CachedNamesInternal.TryGetValue(value, out string? text) ? text : null;

        private static class EnumAttributeCache<TEnum> where TEnum : struct, Enum
        {
            public static readonly ImmutableDictionary<TEnum, string> CachedNamesInternal = LoadNames();

            private static ImmutableDictionary<TEnum, string> LoadNames()
            {
                return typeof(TEnum)
                    .GetTypeInfo()
                    .DeclaredFields
                    .Where(f => f.IsStatic && f.IsPublic && f.FieldType == typeof(TEnum))
                    .Select(f => (field: f, attrib: f.GetCustomAttribute<EnumMemberAttribute>()))
                    .Where(t => (t.attrib?.IsValueSetExplicitly ?? false) && !string.IsNullOrEmpty(t.attrib.Value))
                    .ToDictionary(
                        keySelector: t => (TEnum)t.field.GetValue(obj: null)!,
                        elementSelector: t => t.attrib!.Value!
                    )
                    .ToImmutableDictionary();
            }
        }
    }
}
