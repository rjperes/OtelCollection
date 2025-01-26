using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace OtelCollection
{
    class Program
    {
        static void Main(string[] args)
        {
            using var forecastActivitySource = new ActivitySource("Forecast.ActivitySource");
            var forecastMeter = new Meter("forecast_meter", "1.0.0");
            var forecastCounter = forecastMeter.CreateCounter<int>("forecast_counter", description: "Counts the number of forecasts");

            var builder = WebApplication.CreateBuilder(args);

            builder.Logging.Configure(static options =>
            {
                options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId |
                                                  ActivityTrackingOptions.TraceId |
                                                  ActivityTrackingOptions.ParentId |
                                                  ActivityTrackingOptions.Baggage |
                                                  ActivityTrackingOptions.Tags;
            });

            builder.Services.AddOptions<OpenTelemetrySettings>()
                .BindConfiguration(nameof(OpenTelemetrySettings))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddScoped(sp => forecastCounter);
            builder.Services.AddSingleton(sp => forecastActivitySource);

            builder.Services.AddControllers();

            var settings = builder.Configuration.GetSection(nameof(OpenTelemetrySettings)).Get<OpenTelemetrySettings>();

            var protocol = string.IsNullOrWhiteSpace(settings!.Protocol) ? OtlpExportProtocol.Grpc : Enum.Parse<OtlpExportProtocol>(settings.Protocol, ignoreCase: true);
            var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl) ? new Uri("http://localhost:4317/") : new Uri(settings.BaseUrl);
            var metrics = !settings.Metrics.Any() ? new List<string> { forecastMeter.Name, "Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel" } : settings.Metrics;
            var sources = !settings.Sources.Any() ? new List<string> { forecastActivitySource.Name } : settings.Sources;
            var serviceName = string.IsNullOrWhiteSpace(settings.ServiceName) ? Environment.MachineName : settings.ServiceName;

            var telemetry = builder.Services.AddOpenTelemetry();

            telemetry.ConfigureResource(static options => options.AddService(Environment.MachineName));

            //telemetry.WithLogging();

            telemetry.WithMetrics(options => options.AddAspNetCoreInstrumentation()
                .AddMeter(forecastMeter.Name)
                .AddMeter("Microsoft.AspNetCore.Hosting")
                .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                .AddPrometheusExporter()
                .AddConsoleExporter());

            telemetry.WithTracing(options => options.AddAspNetCoreInstrumentation()
                .AddSource(forecastActivitySource.Name)
                .AddConsoleExporter());
          
            telemetry.UseOtlpExporter(/*protocol: OtlpExportProtocol.HttpProtobuf, baseUrl: new Uri("http://localhost:4318/")*/);

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            //app.UseAuthorization();

            app.MapControllers();

            app.MapPrometheusScrapingEndpoint(/*"/metrics"*/);

            app.Run();
        }
    }
}