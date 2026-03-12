using System.Collections.Generic;

namespace SlackProxy.Models
{
    public enum SlackBlockType { Header, Section }

    public class SlackMessageBuilder
    {
        public SlackMessageBuilder AddHeader(string text) 
            => AddBlock(SlackBlockType.Header, text);

        public SlackMessageBuilder AddSection(string text, bool isMarkdown = true, bool forceSection = false)
            => AddBlock(SlackBlockType.Section, text, isMarkdown);

        private SlackMessageBuilder AddBlock(SlackBlockType type, string text, bool isMarkdown = true, bool forceSection = false)
        {
            if (!string.IsNullOrWhiteSpace(text) || forceSection)
            {
                Blocks.Add(new
                {
                    type = type.ToString().ToLower(),
                    text = new
                    {
                        type = type == SlackBlockType.Header || !isMarkdown ? "plain_text" : "mrkdwn",
                        text = text
                    }
                });
            }

            return this;
        }

        public List<dynamic> Blocks { get; } = new ();

        public object BuildPayload() => new { blocks = Blocks };
    }
}
