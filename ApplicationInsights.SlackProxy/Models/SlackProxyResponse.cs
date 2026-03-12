using System.Text.Json.Serialization;

namespace SlackProxy.Models
{
    public class SlackProxyResponse(bool isSuccessful, string message)
    {
        public static SlackProxyResponse Successful() => new(true, "The Slack Proxy execution was successful.");

        [JsonPropertyName("isSuccessful")]
        public bool IsSuccessful { get; } = isSuccessful;
        [JsonPropertyName("message")]
        public string Message { get; } = message;
    }
}
