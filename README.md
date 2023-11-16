# ApplicationInsights.SlackProxy
This is a ready to go .NET integration application for Application Insights alert notifications in Slack via Azure Functions!

You are welcom to use it as-is or fork it and customize as needed. But as it stands the solution provides a flexible, generic, configuration driven soluition for reliably proxying
notifications from Application Insights Alert rules to Slack.

Released under the MIT license you are free to use and customize as needed....

### Give Star 🌟
**If you like this project and/or use it the please give it a Star 🌟 (c'mon it's free, and it'll help others find the project)!**

### [Buy me a Coffee ☕](https://www.buymeacoffee.com/cajuncoding)
*I'm happy to share with the community, but if you find this useful (e.g for professional use), and are so inclinded,
then I do love-me-some-coffee!*

<a href="https://www.buymeacoffee.com/cajuncoding" target="_blank">
<img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="41" width="174">
</a> 

## Getting Started...
All you need to do is create your Azure Function (consumption plan works great), publish/deploy the solution, and then configure your Application Insightes rules
to use a Webhook and point it to the Function App with a valid App Key (aka Token) such as:
Alert Rule Action Group -> Actions:
 - ActionType: `Webhook`
 - Name: `MySlackProxyFunctionApp`
 - Uri: `https://myslackproxyfunctionapp.azurewebsites.net/api/slack-proxy?code={{Az Func App Key/Token Goes Here}}`
 - Enable Common Alert Schema: `Yes`
   - *The application uses the common alert schema to retrieve details about the Alert for use within the Slack message. 
       For more info see here: https://learn.microsoft.com/en-us/azure/azure-monitor/alerts/alerts-common-schema*

## Configuration Options:
  - `DefaultSlackChannelWebHookUri`
    - You may set a default webhook uri for any or all messages to be sent to in Slack via Az Func configuration options.
    - If specified this will be used as the destination webhook url for any/all Slack notifications unless it is overwritten by
        configuration recieved in the Alert common schema as a Custom Property named `SlackChannelWebHookUri` (more info below).

## Application Insights Rule Options:

 The following properties are read from the App Insights common schema payload that is posted to the Function App via webhook. These values
 are used in the Slack message and/or may be used however you like if you customize the logic that builds the message:
  - `AlertRuleName`
    - Not currently used.
    - Retrieved from `data.essentials.alertRule` in the payload.
  - `AlertRuleDescription`
    - Used as the name of the rule triggering the notification.
    - Retrieved from `data.essentials.description` in the payload.
  - `Severity`
    - Used as the high level severity description in the Slack message and the icon used in the notification.
    - Also determines if an additional section is added denoting
        that errors have actually occurred if `Critical`, `Error`, or `Warning` are used.
    - Retrieved from `data.essentials.severity` in the payload and mapped to the Enum `AppInsightsSeverity`.
  - `LinkToFilteredSearchResultsUIUri`
    - Used to provide a convenient Link to the raw search results in the Slack Notification.
    - Retrieved from the first occurrence of `data.alertContext.codition.allOf[0]` in the payload.
  - `Custom Properties` - any or all of the following may be optionally specified and used in the Slack message by simply adding
     them to the Alert Rule as `Custom Properties` which are included in the Application Insights Common Schema paylaod when sent to the SlackProxy application.
    - `HeaderDescription`: A high level description used in the Slack message header.
    - `SearchQueryDescription`: A simple name description of the Search Query triggering the alert and used in the Slack Message for quick context.
    - `SlackChannelWebHookUri`: If specified this should be a well-formed Uri for the Slack Channel destination for which the mssage will be sent; 
        this will override the `DefaultSlackChannelWebHookUri` configuration value as noted above.
    - `AdditionalMessage`: One or more additional custom messages may be optionally included in the Slack message for this alert notification.
        If more than one is needed then simply append an index to the end (anything unique will work) such as `AdditionalMessage1`, `AdditionalMessage2`, etc.

    *Note: There are some additional properties are read from the App Insights Common Schema payload but aren't actually used in the current message building logic...*
