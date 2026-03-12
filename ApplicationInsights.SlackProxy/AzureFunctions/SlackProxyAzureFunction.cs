using System;
using SlackProxy.Configuration;
using SlackProxy.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Flurl.Http;
using SystemTextJsonHelpers;

namespace SlackProxy.AzureFunctions
{
    public class SlackProxyAzureFunction(ILogger log)
    {
        [Function(nameof(SlackProxyAzureFunction))]
        public async Task<SlackProxyResponse> ExecuteSlackProxy(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "slack-proxy")] HttpRequestData httpRequestData,
            CancellationToken cancellationToken
        )
        {
            //Read and Validate the Request Body...
            //The format of the Payloads from App insights can be found here: https://learn.microsoft.com/en-us/azure/azure-monitor/alerts/alerts-log-webhook
            var requestJson = await httpRequestData.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(requestJson))
                throw new ArgumentNullException(nameof(httpRequestData), "The expected Json WebHook payload is missing from the HttpRequest Body; Request body is null or empty.");
            else if (!requestJson.IsDuckTypedJson())
                throw new ArgumentNullException(nameof(httpRequestData), "The expected WebHook payload JSON is invalid; Request body must be a valid supported JSON Object " +
                    "from Application Insights or the supported Simplified Message model.");

            //Log the App insights Payload sent (for debugging)...
            log.LogInformation("Executing Slack Proxy triggered br Application Insights WebHook...");
            log.LogInformation($"APP INSIGHTS PAYLOAD:{Environment.NewLine}{{SlackProxyRequestBody}}", requestJson);

            //Parse the payload Json into our model dynamically resolving it from either:
            // - Application Insights JSON Payload
            // - Simplified Message Model (for non-AppInsights scenarios)
            var slackMessagePayload = AppInsightsWebHookPayload.Parse(requestJson)
                ?? SimplifiedSlackMessagePayload.Parse(requestJson)
                ?? throw new ArgumentNullException(nameof(httpRequestData), "The expected WebHook payload JSON is invalid; JSON could not be parsed as either a valid Applicaiton Insights or Simplified Message model.");

            //Load the Configuration values for our App...
            var slackProxyConfig = new SlackProxyConfig();
            var slackPayload = slackMessagePayload.BuildSlackMessagePayload();

            var slackChannelUri = slackMessagePayload?.SlackChannelWebHookUri
                    ?? slackProxyConfig.DefaultSlackChannelWebHookUri
                    ?? throw new ArgumentNullException(nameof(slackMessagePayload.SlackChannelWebHookUri), "No 'SlackChannelWebHookUri' custom property or 'DefaultSlackChannelWebHookUri' configuration value was specified.");

            //Post the message payload to Slack using the awesome Flurl Library!
            await slackChannelUri.PostJsonAsync(slackPayload, cancellationToken: cancellationToken).ConfigureAwait(false);

            //Return Successful status that will be returned as Json via the Functions.Worker.ResponseDataJsonMiddleware...
            return SlackProxyResponse.Successful();
        }
    }
}
