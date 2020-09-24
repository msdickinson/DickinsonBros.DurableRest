using DickinsonBros.DateTime.Extensions;
using DickinsonBros.DurableRest.Abstractions;
using DickinsonBros.DurableRest.Extensions;
using DickinsonBros.DurableRest.Runner.Models;
using DickinsonBros.DurableRest.Runner.Models.Models;
using DickinsonBros.DurableRest.Runner.Services;
using DickinsonBros.DurableRest.Runner.Services.JsonPlaceHolderProxy;
using DickinsonBros.DurableRest.Runner.Services.JsonPlaceHolderProxy.Models;
using DickinsonBros.Encryption.Certificate.Extensions;
using DickinsonBros.Encryption.Certificate.Models;
using DickinsonBros.Guid.Abstractions;
using DickinsonBros.Guid.Extensions;
using DickinsonBros.Logger.Abstractions;
using DickinsonBros.Logger.Extensions;
using DickinsonBros.Redactor.Extensions;
using DickinsonBros.Redactor.Models;
using DickinsonBros.Stopwatch.Extensions;
using DickinsonBros.Telemetry.Abstractions;
using DickinsonBros.Telemetry.Extensions;
using DickinsonBros.Telemetry.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace DickinsonBros.DurableRest.Runner
{
    class Program
    {
        IConfiguration _configuration;
        async static Task Main()
        {
            await new Program().DoMain();
        }
        async Task DoMain()
        {
            try
            {
                var services = InitializeDependencyInjection();
                ConfigureServices(services);
                using var provider = services.BuildServiceProvider();
                var telemetryService = provider.GetRequiredService<ITelemetryService>();
                var durableRestService = provider.GetRequiredService<IDurableRestService>();
                var guidService = provider.GetRequiredService<IGuidService>();
                var jsonPlaceHolderProxyService = provider.GetRequiredService<IJsonPlaceHolderProxyService>();
                var correlationService = provider.GetRequiredService<ICorrelationService>();
                var hostApplicationLifetime = provider.GetService<IHostApplicationLifetime>();

                await Task.WhenAll
                (
                    Request(correlationService, guidService, durableRestService),
                    RequestOfT(correlationService, guidService, durableRestService),
                    RequestUsingProxy(jsonPlaceHolderProxyService, correlationService, guidService)
                ).ConfigureAwait(false);

                Console.WriteLine("Flush Telemetry");
                await telemetryService.FlushAsync().ConfigureAwait(false);

                hostApplicationLifetime.StopApplication();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.WriteLine("End...");
            }
        }

        private async Task Request(ICorrelationService correlationService, IGuidService guidService, IDurableRestService durableRestService)
        {
            correlationService.CorrelationId = guidService.NewGuid().ToString();

            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
            };

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("todos/1", UriKind.Relative)
            };

            var retrys = 3;
            var timeoutInSeconds = 30;
            var restResponse = await durableRestService.ExecuteAsync(httpClient, httpRequestMessage, retrys, timeoutInSeconds).ConfigureAwait(false);
        }

        private async Task RequestOfT(ICorrelationService correlationService, IGuidService guidService, IDurableRestService durableRestService)
        {
            correlationService.CorrelationId = guidService.NewGuid().ToString();

            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
            };

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("todos/1", UriKind.Relative)
            };

            var retrys = 3;
            var timeoutInSeconds = 30;

            var restResponse = await durableRestService.ExecuteAsync<Todo>(httpClient, httpRequestMessage, retrys, timeoutInSeconds).ConfigureAwait(false);

            Console.WriteLine("Content: " + await restResponse.HttpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false));
        }

        private async Task RequestUsingProxy(IJsonPlaceHolderProxyService jsonPlaceHolderProxyService, ICorrelationService correlationService, IGuidService guidService)
        {
            correlationService.CorrelationId = guidService.NewGuid().ToString();
            await jsonPlaceHolderProxyService.GetTodosAsync(new GetTodosRequest
            {
                Items = 2
            }).ConfigureAwait(false);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddLogging(cfg => cfg.AddConsole());

            services.AddSingleton<IHostApplicationLifetime, HostApplicationLifetime>();
            services.AddDateTimeService();
            services.AddGuidService();
            services.AddStopwatchService();
            services.AddLoggingService();
            services.AddRedactorService();
            services.AddConfigurationEncryptionService();
            services.AddTelemetryService();
            services.AddDurableRestService();

            //Add Proxy (JsonPlaceHolderProxy)
            services.AddHttpClient<IJsonPlaceHolderProxyService, JsonPlaceHolderProxyService>(client =>
            {
                client.BaseAddress = new Uri(_configuration[$"{nameof(JsonPlaceHolderProxyOptions)}:{nameof(JsonPlaceHolderProxyOptions.BaseURL)}"]);
            });
            services.Configure<JsonPlaceHolderProxyOptions>(_configuration.GetSection(nameof(JsonPlaceHolderProxyOptions)));
        }

        IServiceCollection InitializeDependencyInjection()
        {
            var aspnetCoreEnvironment = Environment.GetEnvironmentVariable("BUILD_CONFIGURATION");
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{aspnetCoreEnvironment}.json", true);
            _configuration = builder.Build();
            var services = new ServiceCollection();
            services.AddSingleton(_configuration);
            return services;
        }
    }
}

