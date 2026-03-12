using System;
using System.Collections.Immutable;

namespace SlackProxy.Models
{
    public static class MessageConstants
    {
        public const string SeverityPrefix = "Sev";
        public const string SuccessfulIcon = "✅";
        public const string InformationIcon = "ℹ️";
        public const string WarningIcon = "⚠️";
        public const string ErrorIcon = "‼️";
        public const string CriticalIcon = "🚨";
        
        public static ImmutableArray<AppInsightsSeverity> WarningOrErrorSeverities = new[] { 
            AppInsightsSeverity.Critical,
            AppInsightsSeverity.Error,
            AppInsightsSeverity.Warning
        }.ToImmutableArray();
    }
}
