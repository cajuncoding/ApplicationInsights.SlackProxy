using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Nodes;
using SlackProxy.CustomExtensions;

namespace SlackProxy.Models
{
    public static class AppInsightsConstants
    {
        public const string SeverityPrefix = "Sev";
        public const string CriticalIcon = "🚨";
        public const string ErrorIcon = "‼️";
        public const string WarningIcon = "⚠️";
        public const string InformationIcon = "ℹ️";
        public static ImmutableArray<AppInsightsSeverity> WarningOrErrorSeverities = new[] { AppInsightsSeverity.Critical, AppInsightsSeverity.Error, AppInsightsSeverity.Warning }.ToImmutableArray();
    }

    public enum AppInsightsSeverity
    {
        [EnumMember(Value = "Trace")]
        Verbose	= 0,
        [EnumMember(Value = "Information")]
        Information = 1,
        [EnumMember(Value = "WARNING")]
        Warning = 2,
        [EnumMember(Value = "ERROR")]
        Error = 3,
        [EnumMember(Value = "CRITICAL")]
        Critical = 4,
    }

    public class AppInsightsWebHookPayload
    {
        public static AppInsightsWebHookPayload Parse(string payload)
            => new AppInsightsWebHookPayload(JsonObject.Parse(payload));

        public AppInsightsWebHookPayload(JsonNode json)
        {
            Json = json;

            var dataJson = json["data"];
            if (dataJson is null)
                return;
            
            var essentialsJson = dataJson["essentials"];
            
            AlertRuleName = essentialsJson["alertRule"].GetValue<string>();
            AlertRuleDescription = essentialsJson["description"].GetValue<string>();

            var severityText = essentialsJson["severity"].GetValue<string>();
            Severity = string.IsNullOrWhiteSpace(severityText) 
                ? AppInsightsSeverity.Warning 
                : (AppInsightsSeverity)Convert.ToInt32(severityText.Replace(AppInsightsConstants.SeverityPrefix, string.Empty));
            
            SeverityDescription = Severity.GetEnumMemberName();
            
            SeverityIcon = Severity switch
            {
                AppInsightsSeverity.Critical => AppInsightsConstants.CriticalIcon,
                AppInsightsSeverity.Error => AppInsightsConstants.ErrorIcon,
                AppInsightsSeverity.Warning => AppInsightsConstants.WarningIcon,
                _ => AppInsightsConstants.InformationIcon
            };

            var firstAllOfJson = dataJson["alertContext"]?["condition"]?["allOf"].AsArray()?.FirstOrDefault();

            SearchQueryText = firstAllOfJson?["searchQuery"].GetValue<string>();
            LinkToFilteredSearchResultsUIUri = firstAllOfJson?["linkToFilteredSearchResultsUI"].GetValue<Uri>();
            LinkToSearchResultsUIUri = firstAllOfJson?["linkToSearchResultsUI"].GetValue<Uri>();

            var customPropsJson = dataJson["customProperties"];

            //Support either Pascal Case or Camel Case in the custom prop names...
            HeaderDescription = (customPropsJson?["HeaderDescription"] ?? customPropsJson?["headerDescription"])?.GetValue<string>();
            SearchQueryDescription = (customPropsJson?["SearchQueryDescription"] ?? customPropsJson?["searchQueryDescription"])?.GetValue<string>();
            SlackChannelWebHookUri = customPropsJson?["SlackChannelWebHookUri"]?.GetValue<Uri>();
            AdditionalMessages = customPropsJson?.AsObject()?.ToArray()
                .Where(prop => 
                    prop.Value.GetValueKind() == JsonValueKind.String //This also means it is not JTokenType.Null!
                    && prop.Key.StartsWith("AdditionalMessage", StringComparison.OrdinalIgnoreCase)
                )
                .OrderBy(prop => prop.Key)
                .Select(prop => prop.Value.GetValue<string>())
                .ToArray();
            }

        public JsonNode Json { get; }
        public string HeaderDescription { get; }
        public AppInsightsSeverity Severity { get; }
        public string SeverityIcon { get; }
        public string SeverityDescription { get; }
        public string AlertRuleName { get; }
        public string AlertRuleDescription { get; }
        public string SearchQueryText { get; }
        public string SearchQueryDescription { get; }
        public Uri SlackChannelWebHookUri { get; }
        public string[] AdditionalMessages { get; }
        public Uri LinkToFilteredSearchResultsUIUri { get; }
        public Uri LinkToSearchResultsUIUri { get; }
    }
}
