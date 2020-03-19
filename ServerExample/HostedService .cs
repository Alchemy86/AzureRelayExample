using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Relay;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ServerExample
{
    public class HostedService : IHostedService
    {
        private readonly ILogger<HostedService> logger;
        private HybridConnectionListener listener;
        private TelemetryConfiguration configuration;
        public HostedService(ILogger<HostedService> logger, TelemetryConfiguration telemetryConfiguration)
        {
            this.logger = logger;
            this.configuration = telemetryConfiguration;
        }

        private const string RelayNamespace = "ag-relay.servicebus.windows.net";

        // replace {HybridConnectionName} with the name of your hybrid connection
        private const string ConnectionName = "ag-hybridconnectionboui";

        // replace {SAKKeyName} with the name of your Shared Access Policies key, which is RootManageSharedAccessKey by default
        private const string KeyName = "RootManageSharedAccessKey";

        // replace {SASKey} with the primary key of the namespace you saved earlier
        private const string Key = "sIjwfQAfumiFpER4WxhR95nH8uQLgMP8iC2Xer9geXM=";

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            configuration.InstrumentationKey = "ed24685e-d783-4306-bdff-be8887d21cd7";
            configuration.TelemetryInitializers.Add(new HttpDependenciesParsingTelemetryInitializer());

            var telemetryClient = new TelemetryClient(configuration);
            telemetryClient.TrackTrace("Hello World!");
            logger.LogInformation("HERE");

            var module = new DependencyTrackingTelemetryModule();

            // prevent Correlation Id to be sent to certain endpoints. You may add other domains as needed.
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.windows.net");
            //...

            // enable known dependency tracking, note that in future versions, we will extend this list. 
            // please check default settings in https://github.com/Microsoft/ApplicationInsights-dotnet-server/blob/develop/Src/DependencyCollector/DependencyCollector/ApplicationInsights.config.install.xdt

            module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
            module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.EventHubs");
            //....

            // initialize the module
            module.Initialize(configuration);

            var cts = new CancellationTokenSource();

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(KeyName, Key);
            listener = new HybridConnectionListener(new Uri(string.Format("sb://{0}/{1}", RelayNamespace, ConnectionName)), tokenProvider);

            // Subscribe to the status events.
            listener.Connecting += (o, e) => { logger.LogInformation("Connecting"); };
            listener.Offline += (o, e) => { logger.LogInformation("Offline"); };
            listener.Online += (o, e) => { logger.LogInformation("Online"); };

            using (InitializeDependencyTracking(configuration))
            {
                // Provide an HTTP request handler
                listener.RequestHandler = (context) =>
                {
                    telemetryClient.TrackTrace("Hello World!");
                    // Do something with context.Request.Url, HttpMethod, Headers, InputStream...
                    context.Response.StatusCode = HttpStatusCode.OK;
                    context.Response.StatusDescription = "OK, This is pretty neat";
                    using (var sw = new StreamWriter(context.Response.OutputStream))
                    {
                        sw.WriteLine("hello! I have come from the server");
                    }
                    logger.LogInformation("Server Responded");
                    logger.LogInformation(context.TrackingContext.Address);
                    // The context MUST be closed here
                    context.Response.Close();

                    using (var httpClient = new HttpClient())
                    {
                        // Http dependency is automatically tracked!
                        httpClient.GetAsync("https://google.com").Wait();
                    }


                    // before exit, flush the remaining data
                    telemetryClient.Flush();

                    // flush is not blocking so wait a bit
                    Task.Delay(5000).Wait();
                };
            }

            // Opening the listener establishes the control channel to
            // the Azure Relay service. The control channel is continuously 
            // maintained, and is reestablished when connectivity is disrupted.
            await listener.OpenAsync();
            logger.LogInformation("Server listening");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await listener.CloseAsync();
            logger.LogInformation("Server Stopped");
        }

        static DependencyTrackingTelemetryModule InitializeDependencyTracking(TelemetryConfiguration configuration)
        {
            var module = new DependencyTrackingTelemetryModule();

            // prevent Correlation Id to be sent to certain endpoints. You may add other domains as needed.
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.windows.net");
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.chinacloudapi.cn");
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.cloudapi.de");
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.usgovcloudapi.net");
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("localhost");
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("127.0.0.1");

            // enable known dependency tracking, note that in future versions, we will extend this list. 
            // please check default settings in https://github.com/microsoft/ApplicationInsights-dotnet-server/blob/develop/WEB/Src/DependencyCollector/DependencyCollector/ApplicationInsights.config.install.xdt

            module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
            module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.EventHubs");

            // initialize the module
            module.Initialize(configuration);

            return module;
        }
    }
}
