namespace SlackProxy.Models
{
    public interface ISlackMessagePayload
    {
        object BuildSlackMessagePayload();
        Uri SlackChannelWebHookUri { get; }
    }
}
