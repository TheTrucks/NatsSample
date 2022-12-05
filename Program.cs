using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NatsWriters
{
    internal class Program
    {
        static async Task Main()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("settings.json")
                .Build();
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices(servs => ConfigureServices(servs, config));
            var host = builder.Build();

            await host.RunAsync();
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration config)
        {
            //services.Configure<TestDataWriterOptions>(config.GetSection("NatsReader"));
            //services.AddSingleton<INatsOutput, ConsoleNatsOutput>();
            //services.AddSingleton<IDataWriter<TestDataStruct>, TestDataWriter>();
            //services.AddHostedService<Runner>();

            services.Configure<DbDataInputOptions>(config.GetSection("PsqlNatsInput"));
            services.Configure<JNatsWriterOptions>(config.GetSection("JNatsWriter"));
            services.AddSingleton<INatsOutput, ConsoleNatsOutput>();
            services.AddSingleton<IAsyncNatsDataInput<TestDataStruct>, DbDataInput>();
            services.AddSingleton<IDataWriter<TestDataStruct>, JNatsDataWriter>();
            services.AddHostedService<JRunner>();
        }
    }
}