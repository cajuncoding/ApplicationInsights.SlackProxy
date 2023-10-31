using System;

namespace SlackProxy.Configuration

{
    public class SlackProxyConfig
    {
        public SlackProxyConfig()
        {
            DefaultSlackChannelWebHookUri = new Uri(
                Environment.GetEnvironmentVariable("DefaultSlackChannelWebHookUri")
                    ?? throw new ArgumentException("The [DefaultSlackChannelWebHookUri] configuration value is missing or null.")
            );
        }

        public Uri DefaultSlackChannelWebHookUri { get; }
    }
}
