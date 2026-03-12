using System;

namespace SlackProxy.Configuration

{
    public class SlackProxyConfig
    {
        public SlackProxyConfig()
        {
            DefaultSlackChannelWebHookUri = Uri.TryCreate(Environment.GetEnvironmentVariable("DefaultSlackChannelWebHookUri"), UriKind.Absolute, out var parsedUri)
                ? parsedUri
                : throw new ArgumentException("The [DefaultSlackChannelWebHookUri] configuration value is missing or null.");
        }

        public Uri DefaultSlackChannelWebHookUri { get; }
    }
}
