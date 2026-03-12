using Functions.Worker.ContextAccessor;
using Functions.Worker.ILoggerSupport;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SystemTextJsonHelpers;

var host = Host
    .CreateDefaultBuilder()
    .ConfigureFunctionsWorkerDefaults(app =>
    {
        app.UseFunctionContextAccessor();
    })
    .ConfigureServices((ctx, services) =>
    {
        services
            .AddFunctionContextAccessor()
            //Register ILogger for full support of the non-generic ILogger interface, which makes the code
            //  much more portable so DI classes can use any logger for any category/function with no generic coupling!
            .AddFunctionILoggerSupport()
            .AddApplicationInsightsTelemetryWorkerService()
            .ConfigureFunctionsApplicationInsights();
    })
    .Build();

//System.Text.Json -- Configure relaxed/loose default settings for Flurl with SystemTextJson (e.g. Camel Case, Case-insensitivity, Don't write nulls, etc.)...
//NOTE: System.Text.Json is still missing support for Global changes to default Serialization settings...
//      So we use the SystemTextJson helpers library -- to provide an elegant workaround that we configure here to use Relaxed Web Defaults!
SystemTextJsonDefaults.ConfigureDefaults(SystemTextJsonDefaults.CreateRelaxedJsonSerializerOptions(allowWritingNullValues: false));

await host.RunAsync().ConfigureAwait(false);
