using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using Quartz;
using System.Collections.Frozen;
using ValueVest.Worker.Core.Data;
using ValueVest.Worker.Jobs;
using ValueVest.Worker.Models;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;

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
				services.Configure<DataSources>(configuration.GetSection(nameof(DataSources)));
				var dbConnections = new Dictionary<DatabaseConnection, string>
                {
                    { DatabaseConnection.BistDb, configuration.GetConnectionString("BistDb") ?? throw new ArgumentNullException() },
                }.ToFrozenDictionary();
                services.AddSingleton(dbConnections);
                services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
                services.AddSingleton<App>();
                services.AddQuartz(options =>
                {
                    options.ScheduleJob<CompaniesJob>(trigger => trigger
                        .ForJob(CompaniesJob.Key)
                        .WithIdentity(CompaniesJob.Key.ToString())
                        //.StartAt(new DateTimeOffset())
                        .WithSimpleSchedule(SimpleScheduleBuilder.RepeatHourlyForever()));
                });
				
				services.AddHttpClient();
                services.AddFusionCache()
                .WithCysharpMemoryPackSerializer()
                .WithDistributedCache(new RedisCache(new RedisCacheOptions { Configuration = "CONNECTION STRING" }))
				.WithBackplane(new RedisBackplane(new RedisBackplaneOptions { Configuration = "CONNECTION STRING" }));
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