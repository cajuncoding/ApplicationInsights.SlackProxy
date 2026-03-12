using Azure.Core.Serialization;
using Functions.Worker.ContextAccessor;
using Functions.Worker.HttpResponseDataJsonMiddleware;
using Functions.Worker.ILoggerSupport;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using SystemTextJsonHelpers;

var host = Host
    .CreateDefaultBuilder()
    .ConfigureFunctionsWorkerDefaults(app =>
    {
        app
            .UseFunctionContextAccessor()
            //Map all exceptions to BadRequest Http Responses, and allow the JSON Middleware 
            //  to convert the Exception to a standardized JSON friendly result.
            .UseJsonResponses((exc) => (HttpStatusCode.BadRequest, exc));
    })
    .ConfigureServices((ctx, services) =>
    {
        //System.Text.Json -- Configure relaxed/loose default settings for Flurl with SystemTextJson (e.g. Camel Case, Case-insensitivity, etc.)...
        //NOTE: System.Text.Json is still missing support for Global changes to default Serialization settings...
        //      So we use the SystemTextJson helpers library -- to provide an elegant workaround that we configure here to use Relaxed Web Defaults!
        SystemTextJsonDefaults.ConfigureRelaxedWebDefaults();

        services
            .AddFunctionContextAccessor()
            //Register ILogger for full support of the non-generic ILogger interface, which makes the code
            //  much more portable so DI classes can use any logger for any category/function with no generic coupling!
            .AddFunctionILoggerSupport()
            .AddApplicationInsightsTelemetryWorkerService()
            .ConfigureFunctionsApplicationInsights()
            .Configure<WorkerOptions>(options =>
            {
                //Configure the Azure Functions Worker serializer to use the same Default Serializer Options as we've already initialized
                //  for our SystemTextJsonHelpers library and object extension methods...
                options.Serializer = new JsonObjectSerializer(SystemTextJsonDefaults.DefaultSerializerOptions);
            });
    })
    .Build();

await host.RunAsync().ConfigureAwait(false);
