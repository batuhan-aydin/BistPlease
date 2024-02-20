using ValueVest.Worker.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly.Extensions.Http;
using Polly;
using Quartz;
using Polly.Retry;
using ValueVest.Worker.Core.Data;
using System.Collections.Frozen;
using ValueVest.Worker.Core.HttpClients;
using ValueVest.Worker.Core;

namespace ValueVest.Worker;

internal class Program
{
    static void Main(string[] args)
    {
        using IHost host = CreateHostBuilder(args).Build();
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            services.GetRequiredService<App>().Run(args).Wait();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    static IHostBuilder CreateHostBuilder(string[] strings)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                IConfiguration configuration = hostContext.Configuration;
                services.Configure<IsYatirimSettings>(configuration.GetSection(nameof(IsYatirimSettings)));
                var dbConnections = new Dictionary<DatabaseConnection, string>
                {
                    { DatabaseConnection.BistDb, configuration.GetConnectionString("BistDb") ?? throw new ArgumentNullException() },
                }.ToFrozenDictionary();
                services.AddSingleton<FrozenDictionary<DatabaseConnection, string>>(dbConnections);
                services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
                services.AddSingleton<App>();
                services.AddSingleton<IAngleSharpWrapper, AngleSharpWrapper>();
                services.AddQuartz(options =>
                {
                    options.ScheduleJob<ValuationsJob>(trigger => trigger
                        .ForJob(ValuationsJob.Key)
                        .WithIdentity(ValuationsJob.Key.ToString())
                        .WithSimpleSchedule(SimpleScheduleBuilder.RepeatHourlyForever()));
                });

                var financialsUrl = configuration.GetSection(nameof(IsYatirimSettings)).GetValue<string>("BaseFinancialsUrl");
                services.AddHttpClient<IIsInvestmentHttpClient, IsInvestmentHttpClient>(client =>
                {
                    client.BaseAddress = new Uri(financialsUrl ?? throw new ArgumentNullException(nameof(financialsUrl)));
                })
                 .AddPolicyHandler(GetRetryPolicy());
                });
    }

    private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
            .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

}