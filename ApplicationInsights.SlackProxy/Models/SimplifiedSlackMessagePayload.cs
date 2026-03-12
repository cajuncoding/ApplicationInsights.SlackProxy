using System;
using System.Runtime.Serialization;
using SlackProxy.CustomExtensions;
using SlackProxy.Helpers;
using SystemTextJsonHelpers;

namespace SlackProxy.Models
{
    public enum SlackMessageStatus
    {
        [EnumMember(Value = "Information")]
        Information = 0,
        [EnumMember(Value = "Successful")]
        Successful = 1,
        [EnumMember(Value = "WARNING")]
        Warning = 2,
        [EnumMember(Value = "ERROR")]
        Error = 3,
        [EnumMember(Value = "CRITICAL")]
        Critical = 4,
    }

    public class SimplifiedSlackMessagePayload : ISlackMessagePayload
    {
        public static ISlackMessagePayload Parse(string jsonPayload)
            => jsonPayload.FromJsonTo<SimplifiedSlackMessagePayload>() is {} simplifiedPayload && IsValidSimplifiedMessagePayload(simplifiedPayload)
                ? simplifiedPayload
                : null;

        private static bool IsValidSimplifiedMessagePayload(SimplifiedSlackMessagePayload simplifiedPayload)
            => !string.IsNullOrWhiteSpace(simplifiedPayload.HeaderDescription)
                && simplifiedPayload.Status is not null;

        public string HeaderDescription { get; set; }
        
        //NOTE: Status is Nullable so that we can safely detect if the Payload specified a value for it or not.
        //      The Relaxe Json parsing will set to `null` on any failure to parse a valid value.
        public SlackMessageStatus? Status { get; set; } = null;
        
        public string StatusIcon => Status switch
        {
            null => null,
            SlackMessageStatus.Successful => MessageConstants.SuccessfulIcon,
            SlackMessageStatus.Critical => MessageConstants.CriticalIcon,
            SlackMessageStatus.Error => MessageConstants.ErrorIcon,
            SlackMessageStatus.Warning => MessageConstants.WarningIcon,
            _ => MessageConstants.InformationIcon
        };

        public string StatusDescription => Status?.GetEnumMemberName();

        public string PrimaryMessage { get; } = string.Empty;
        public string[] AdditionalMessages { get; set; } = Array.Empty<string>();
        public Uri SlackChannelWebHookUri { get; set; } = null;
        public Uri ReferencedLinkUri { get; set; } = null;
        public string ReferencedLinkDescription { get; set; } = null;

        public object BuildSlackMessagePayload()
        {
            //Formulate the Alert/Warning message...
            var slackMessageBuilder = new SlackMessageBuilder()
                .AddHeader($"{StatusIcon} [{StatusDescription}] {HeaderDescription}")
                .AddSection(PrimaryMessage);

            //Append any additional messages that are configured as custom properties...
            foreach (var additionalMessage in (AdditionalMessages ?? []))
                slackMessageBuilder.AddSection(additionalMessage);

            //Append the final link to AppInsights if available...
            if (ReferencedLinkUri is not null)
            {
                var referenceLinkDescreiptions = string.IsNullOrWhiteSpace(ReferencedLinkDescription) 
                    ? "Click for additional details..."
                    : ReferencedLinkDescription;

                slackMessageBuilder.AddSection($"<{ReferencedLinkUri}|{referenceLinkDescreiptions}>");
            }

            return slackMessageBuilder.BuildPayload();
        }
    }
}
