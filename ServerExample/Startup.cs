using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO;

namespace ServerExample
{
    public class Startup
    {
        public void ConfigureAppConfiguration(HostBuilderContext hostBuilder, IConfigurationBuilder configBuilder)
        {
            configBuilder.AddJsonFile(@"Config/appsettings.json", true, true);
        }

        public void ConfigureHostConfiguration(IConfigurationBuilder cb)
        {
            // https://github.com/aspnet/Hosting/issues/1440 - Override hosting env for generic host
            cb.AddEnvironmentVariables("ASPNETCORE_");
            cb.SetBasePath(Directory.GetCurrentDirectory());
        }

        public void ConfigureLogging(HostBuilderContext hostBuilder, ILoggingBuilder lb)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(@"Config/Serilog.json", false);

            if (hostBuilder.HostingEnvironment.IsDevelopment())
                configuration.AddUserSecrets<Startup>();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration.Build())
                .CreateLogger();

            lb.AddSerilog();
        }

        public void ConfigureServices(HostBuilderContext hostBuilder, IServiceCollection services)
        {
            services.AddOptions();
            services.AddSingleton<IHostedService, HostedService>();
            services.AddSingleton<TelemetryConfiguration>(o => TelemetryConfiguration.CreateDefault());
        }
    }
}
