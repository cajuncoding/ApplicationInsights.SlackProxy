using System;
using System.Runtime.Serialization;
using System.Text.Json.Nodes;
using SlackProxy.CustomExtensions;
using SlackProxy.Helpers;
using SystemTextJsonHelpers;

namespace SlackProxy.Models
{
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

    public class AppInsightsWebHookPayload : ISlackMessagePayload
    {
        public static ISlackMessagePayload Parse(string jsonPayload)
            => jsonPayload.FromJsonTo<JsonObject>() is JsonObject json && IsValidAppInsightsPayload(json)
                ? new AppInsightsWebHookPayload(json)
                : null;

        private static bool IsValidAppInsightsPayload(JsonObject json)
            => json?["data"] is JsonObject dataJson
                && dataJson["essentials"]?["alertRule"] is not null
                && dataJson["customProperties"]?["headerDescription"] is not null;

        public AppInsightsWebHookPayload(JsonObject json)
        {
            Json = json;
            
            if (!IsValidAppInsightsPayload(json))
                return;

            var dataJson = json["data"];
            var essentialsJson = dataJson["essentials"];
            
            AlertRuleName = essentialsJson["alertRule"].GetValue<string>();
            AlertRuleDescription = essentialsJson["description"].GetValue<string>();

            var severityText = essentialsJson["severity"]?.GetValue<string>();
            var severityIntText = !string.IsNullOrWhiteSpace(severityText)
                ? severityText.Replace(MessageConstants.SeverityPrefix, string.Empty)
                : null;

            Severity = int.TryParse(severityIntText, out var severityIntValue)
                ? (AppInsightsSeverity)severityIntValue
                : AppInsightsSeverity.Warning;

            SeverityDescription = Severity.GetEnumMemberName();
            
            SeverityIcon = Severity switch
            {
                AppInsightsSeverity.Critical => MessageConstants.CriticalIcon,
                AppInsightsSeverity.Error => MessageConstants.ErrorIcon,
                AppInsightsSeverity.Warning => MessageConstants.WarningIcon,
                _ => MessageConstants.InformationIcon
            };

            var firstAllOfJson = dataJson["alertContext"]?["condition"]?["allOf"]?.AsArray()?.FirstOrDefault();

            SearchQueryText = firstAllOfJson?["searchQuery"]?.ValueSafely<string>();
            LinkToFilteredSearchResultsUIUri = firstAllOfJson?["linkToFilteredSearchResultsUI"]?.ValueSafely<Uri>();
            LinkToSearchResultsUIUri = firstAllOfJson?["linkToSearchResultsUI"]?.ValueSafely<Uri>();

            var customPropsJson = dataJson["customProperties"];

            //Support either Pascal Case or Camel Case in the custom prop names...
            HeaderDescription = customPropsJson?["headerDescription"]?.ValueSafely<string>();
            SearchQueryDescription = customPropsJson?["searchQueryDescription"]?.ValueSafely<string>();
            SlackChannelWebHookUri = customPropsJson?["slackChannelWebHookUri"]?.ValueSafely<Uri>();
            AdditionalMessages = customPropsJson
                ?.AsObject()
                ?.GetProperties(dataTypeFilter: JsonDataTypeFilter.String)
                .Where(prop => prop.Key.StartsWith("additionalMessage", StringComparison.OrdinalIgnoreCase))
                .OrderBy(prop => prop.Key)
                .Select(prop => prop.Value?.ValueSafely<string>())
                .ToArray() ?? Array.Empty<string>();
        }

        public JsonObject Json { get; }
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

        public object BuildSlackMessagePayload()
        {
            var queryDescriptionClause = SearchQueryDescription != null
                ? $"via *[{SearchQueryDescription}]* "
                : string.Empty;

            //Formulate the Alert/Warning message...
            var slackMessageBuilder = new SlackMessageBuilder()
                .AddHeader($"{SeverityIcon} [{SeverityDescription}] {HeaderDescription}")
                .AddSection($"An alert for *[{AlertRuleDescription}]* has been triggered.");

            if (MessageConstants.WarningOrErrorSeverities.Contains(Severity))
                slackMessageBuilder.AddSection($"Warnings or Errors have been reported {queryDescriptionClause} and there may be an issue that needs to be investigated.");

            //Append any additional messages that are configured as custom properties...
            foreach (var additionalMessage in AdditionalMessages)
                slackMessageBuilder.AddSection(additionalMessage);

            //Append the final link to AppInsights if available...
            if (LinkToFilteredSearchResultsUIUri != null)
                slackMessageBuilder.AddSection($"<{LinkToFilteredSearchResultsUIUri}|Click here for Alert Query results...>");

            return slackMessageBuilder.BuildPayload();
        }
    }
}
