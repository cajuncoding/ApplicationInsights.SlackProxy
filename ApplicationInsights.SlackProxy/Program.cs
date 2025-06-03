using Functions.Worker.ContextAccessor;
using Functions.Worker.ILoggerSupport;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

await host.RunAsync().ConfigureAwait(false);
