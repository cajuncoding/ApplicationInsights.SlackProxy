using System;
using System.Net;
using SlackProxy.Configuration;
using SlackProxy.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Flurl.Http;

namespace SlackProxy.AzureFunctions
{
    public class SlackProxyAzureFunction(ILogger log)
    {
        [Function(nameof(SlackProxyAzureFunction))]
        public async Task<HttpResponseData> ExecuteSlackProxy(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "slack-proxy")] HttpRequestData httpRequestData
        )
        {
            //Read and Validate the Request Body...
            //The format of the Payloads from App insights can be found here: https://learn.microsoft.com/en-us/azure/azure-monitor/alerts/alerts-log-webhook
            var requestBody = await httpRequestData.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(requestBody))
                throw new ArgumentNullException(nameof(httpRequestData), "The expected Json WebHook payload is missing from the HttpRequest Body; Request body is null or empty.");

            //Log the App insights Payload sent (for debugging)...
            log.LogInformation("Executing Slack Proxy triggered br Application Insights WebHook...");
            log.LogInformation($"APP INSIGHTS PAYLOAD:{Environment.NewLine}{{RequestBody}}", requestBody);

            //Parse the payload Json into our model...
            var appInsightsPayload = AppInsightsWebHookPayload.Parse(requestBody);

            //Load the Configuration values for our App...
            var slackProxyConfig = new SlackProxyConfig();
            var slackPayload = BuildSlackMessagePayload(appInsightsPayload);

            var slackChannelUri = appInsightsPayload?.SlackChannelWebHookUri 
                  ?? slackProxyConfig.DefaultSlackChannelWebHookUri 
                  ?? throw new ArgumentNullException(nameof(appInsightsPayload.SlackChannelWebHookUri), "No 'SlackChannelWebHookUri' custom property or 'DefaultSlackChannelWebHookUri' configuration value was specified.");

            //Post the message payload to Slack using the awesome Flurl Library!
            await slackChannelUri.PostJsonAsync(slackPayload).ConfigureAwait(false);

            var httpResponseData = httpRequestData.CreateResponse(HttpStatusCode.OK);
            httpResponseData.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            await httpResponseData.WriteStringAsync($"{nameof(SlackProxyAzureFunction)} execution was successful.").ConfigureAwait(false);
            return httpResponseData;
        }

        private static object BuildSlackMessagePayload(AppInsightsWebHookPayload appInsightsPayload)
        {
            var queryDescriptionClause = appInsightsPayload.SearchQueryDescription != null
                ? $"via *[{appInsightsPayload.SearchQueryDescription}]* "
                : string.Empty;

            //Formulate the Alert/Warning message...
            var slackMessageBuilder = new SlackMessageBuilder()
                .AddHeader($"{appInsightsPayload.SeverityIcon} [{appInsightsPayload.SeverityDescription}] {appInsightsPayload.HeaderDescription}")
                .AddSection($"An alert for *[{appInsightsPayload.AlertRuleDescription}]* has been triggered.");
            
            if (AppInsightsConstants.WarningOrErrorSeverities.Contains(appInsightsPayload.Severity))
                slackMessageBuilder.AddSection($"Warnings or Errors have been reported {queryDescriptionClause} and there may be an issue that needs to be investigated.");

            //Append any additional messages that are configured as custom properties...
            foreach (var additionalMessage in appInsightsPayload.AdditionalMessages)
                slackMessageBuilder.AddSection(additionalMessage);

            //Append the final link to AppInsights if available...
            if (appInsightsPayload.LinkToFilteredSearchResultsUIUri != null)
                slackMessageBuilder.AddSection($"<{appInsightsPayload.LinkToFilteredSearchResultsUIUri}|Click here for Alert Query results...>");

            return slackMessageBuilder.BuildPayload();
        }
    }
}
