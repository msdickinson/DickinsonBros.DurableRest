using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DickinsonBros.Logger.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using DickinsonBros.Stopwatch.Abstractions;
using DickinsonBros.DateTime.Abstractions;
using DickinsonBros.Telemetry.Abstractions;
using DickinsonBros.Telemetry.Abstractions.Models;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.IO;
using DickinsonBros.DurableRest.Abstractions;
using DickinsonBros.DurableRest.Abstractions.Models;
using System.Net;

namespace DickinsonBros.DurableRest
{
    public class DurableRestService : IDurableRestService
    {
        internal readonly IServiceProvider _serviceProvider;
        internal readonly IDateTimeService _dateTimeService;
        internal readonly ITelemetryService _telemetryService;
        internal readonly ILoggingService<DurableRestService> _loggingService;
        internal const string DurableRestMessage = "DurableRest";

        public DurableRestService
        (
            IServiceProvider serviceProvider,
            IDateTimeService dateTimeService,
            ITelemetryService telemetryService,
            ILoggingService<DurableRestService> loggingService
        )
        {
            _loggingService = loggingService;
            _dateTimeService = dateTimeService;
            _telemetryService = telemetryService;
            _serviceProvider = serviceProvider;
        }

        public DurableRestService
        (
            IServiceProvider serviceProvider,
            IDateTimeService dateTimeService,
            ILoggingService<DurableRestService> loggingService
        )
        {
            _loggingService = loggingService;
            _dateTimeService = dateTimeService;
            _serviceProvider = serviceProvider;
        }

        public async Task<HttpResponse<T>> ExecuteAsync<T>
        (
            HttpClient httpClient,
            HttpRequestMessage httpRequestMessage,
            int retrys,
            double timeoutInSeconds
        )
        {
            var httpResponseMessage = await ExecuteAsync(httpClient, httpRequestMessage, retrys, timeoutInSeconds).ConfigureAwait(false);

            return new HttpResponse<T>
            {
                HttpResponseMessage = httpResponseMessage,
                Data = JsonSerializer.Deserialize<T>(
                    httpResponseMessage.Content != null ? await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false) : null,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    }
                )
            };
        }

        public async Task<HttpResponseMessage> ExecuteAsync
        (
            HttpClient httpClient,
            HttpRequestMessage httpRequestMessage,
            int retrys,
            double timeoutInSeconds
        )
        {
            var stopwatchService = _serviceProvider.GetRequiredService<IStopwatchService>();
            var attempts = 0;

            HttpResponseMessage httpResponseMessage = null;
            do
            {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(timeoutInSeconds));
                stopwatchService.Start();

                try
                {
                    httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, cts.Token).ConfigureAwait(false);
                    stopwatchService.Stop();
                }
                catch(OperationCanceledException ex)
                {
                    stopwatchService.Stop();
                }

                if (_telemetryService != null)
                {
                    InsertDurableRestResult
                    (
                        $"{httpRequestMessage.Method} {httpRequestMessage.RequestUri}",
                        httpResponseMessage != null ? (int)httpResponseMessage.StatusCode : (int)HttpStatusCode.RequestTimeout,
                        (int)stopwatchService.ElapsedMilliseconds
                    );
                }

                attempts++;

                if (httpResponseMessage == null || httpResponseMessage.IsSuccessStatusCode)
                {
                    break;
                }
                httpRequestMessage = await CloneAsync(httpRequestMessage).ConfigureAwait(false);
            } while (retrys >= attempts);

            await LogDurableRestResult(httpRequestMessage, httpResponseMessage, httpClient, attempts, (int)stopwatchService.ElapsedMilliseconds).ConfigureAwait(false);

            return httpResponseMessage;
        }


        public async Task LogDurableRestResult
        (
            HttpRequestMessage httpRequestMessage,
            HttpResponseMessage httpResponseMessage,
            HttpClient httpClient,
            int attempts,
            int elapsedMilliseconds
        )
        {
            var properties = new Dictionary<string, object>
            {
                { "Attempts", attempts },
                { "BaseUrl", httpClient.BaseAddress },
                { "Resource", httpRequestMessage.RequestUri.AbsolutePath },
                { "RequestContent", httpRequestMessage.Content != null ? await httpRequestMessage.Content.ReadAsStringAsync().ConfigureAwait(false) : null },
                { "ResponseContent", httpResponseMessage.Content != null ? await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false) : null },
                { "ElapsedMilliseconds", elapsedMilliseconds },
                { "StatusCode", httpResponseMessage?.StatusCode }
            };

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                _loggingService.LogErrorRedacted
                (
                    DurableRestMessage,
                    null,
                    properties
                );
                return;
            }

            _loggingService.LogInformationRedacted
            (
                DurableRestMessage,
                properties
            );

        }

        public void InsertDurableRestResult(string name, int statusCode, int elapsedMilliseconds)
        {
            var telemetryState = statusCode switch
            {
                int _ when (statusCode >= 200 && statusCode < 300) => TelemetryState.Successful,
                int _ when (statusCode >= 400 && statusCode < 500) => TelemetryState.BadRequest,
                _ => TelemetryState.Failed
            };

            _telemetryService.Insert(new TelemetryData
            {
                DateTime = _dateTimeService.GetDateTimeUTC(),
                ElapsedMilliseconds = elapsedMilliseconds,
                Name = name,
                TelemetryState = telemetryState,
                TelemetryType = TelemetryType.Rest
            });
        }

        internal async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Content = await CloneAsync(request.Content).ConfigureAwait(false),
                Version = request.Version
            };
            foreach (KeyValuePair<string, object> prop in request.Properties)
            {
                clone.Properties.Add(prop);
            }
            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }

        internal async Task<HttpContent> CloneAsync(HttpContent content)
        {
            if (content == null) return null;

            var ms = new MemoryStream();
            await content.CopyToAsync(ms).ConfigureAwait(false);
            ms.Position = 0;

            var clone = new StreamContent(ms);
            foreach (KeyValuePair<string, IEnumerable<string>> header in content.Headers)
            {
                clone.Headers.Add(header.Key, header.Value);
            }
            return clone;
        }
    }

}
