using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;
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
            => new AppInsightsWebHookPayload(JObject.Parse(payload));

        public AppInsightsWebHookPayload(JObject json)
        {
            Json = json;

            dynamic dataJson = json["data"];
            dynamic essentialsJson = dataJson?.essentials;
            
            AlertRuleName = essentialsJson?.alertRule;
            AlertRuleDescription = essentialsJson?.description;

            var severityText = (string)essentialsJson?.severity?.ToString();
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

            dynamic firstAllOfJson = dataJson?.alertContext?.condition?.allOf?[0];

            SearchQueryText = firstAllOfJson?.searchQuery;
            LinkToFilteredSearchResultsUIUri = firstAllOfJson?.linkToFilteredSearchResultsUI;
            LinkToSearchResultsUIUri = firstAllOfJson?.linkToSearchResultsUI;

            dynamic customPropsJson = dataJson?.customProperties;

            //Support either Pascal Case or Camel Case in the custom prop names...
            HeaderDescription = customPropsJson?.HeaderDescription ?? customPropsJson?.headerDescription;
            SearchQueryDescription = customPropsJson?.SearchQueryDescription ?? customPropsJson?.searchQueryDescription;
            SlackChannelWebHookUri = customPropsJson?.SlackChannelWebHookUri;
            AdditionalMessages = ((JObject)customPropsJson)?.Properties()
                .Where(prop => 
                    prop.Value.Type == JTokenType.String //This also means it is not JTokenType.Null!
                    && prop.Name.StartsWith("AdditionalMessage", StringComparison.OrdinalIgnoreCase)
                )
                .OrderBy(prop => prop.Name)
                .Select(prop => prop.Value.ToString())
                .ToArray();
            }

        public JObject Json { get; }
        public string HeaderDescription { get; }
        public AppInsightsSeverity Severity { get; }
        public string SeverityIcon { get; }
        public string SeverityDescription { get; }
        public string AlertRuleName { get; }
        public string AlertRuleDescription { get; }
        public string SearchQueryText { get; }
        public string SearchQueryDescription { get; }
        public Uri SlackChannelWebHookUri { get; }
        public string[]? AdditionalMessages { get; }
        public Uri LinkToFilteredSearchResultsUIUri { get; }
        public Uri LinkToSearchResultsUIUri { get; }
    }
}
