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
using System.Linq;
using DickinsonBros.Guid.Abstractions;

namespace DickinsonBros.DurableRest
{
    public class DurableRestService : IDurableRestService
    {
        internal readonly IServiceProvider _serviceProvider;
        internal readonly IDateTimeService _dateTimeService;
        internal readonly ITelemetryService _telemetryService;
        internal readonly ICorrelationService _correlationService;
        internal readonly IGuidService _guidService;
        internal readonly ILoggingService<DurableRestService> _loggingService;
        internal const string CORRELATION_ID = "X-Correlation-ID";
        internal readonly string DURABLE_REST_MESSAGE_REQUEST = $"{nameof(DurableRestService)} - Request";
        internal readonly string DURABLE_REST_MESSAGE_RESPONSE = $"{nameof(DurableRestService)} - Response";

        public DurableRestService
        (
            IServiceProvider serviceProvider,
            IGuidService guidService,
            IDateTimeService dateTimeService,
            ITelemetryService telemetryService,
            ICorrelationService correlationService,
            ILoggingService<DurableRestService> loggingService
        )
        {
            _correlationService = correlationService;
            _loggingService = loggingService;
            _dateTimeService = dateTimeService;
            _telemetryService = telemetryService;
            _guidService = guidService;
            _serviceProvider = serviceProvider;
        }

        public DurableRestService
        (
            IServiceProvider serviceProvider,
            IGuidService guidService,
            IDateTimeService dateTimeService,
            ICorrelationService correlationService,
            ILoggingService<DurableRestService> loggingService
        )
        {
            _correlationService = correlationService;
            _loggingService = loggingService;
            _dateTimeService = dateTimeService;
            _guidService = guidService;
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
                Data = (httpResponseMessage.IsSuccessStatusCode && httpResponseMessage.Content != null) ?  
                            JsonSerializer.Deserialize<T>(
                                await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false),
                                new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true,
                                })
                            : default
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

            if (!httpRequestMessage.Headers.Any(e => e.Key == CORRELATION_ID))
            {
                httpRequestMessage.Headers.Add
                (
                    CORRELATION_ID,
                    !string.IsNullOrWhiteSpace(_correlationService.CorrelationId) ? _correlationService.CorrelationId : _guidService.NewGuid().ToString() 
                );
            }

            do
            {
                await LogRequest(httpRequestMessage, httpClient, attempts).ConfigureAwait(false);
                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(timeoutInSeconds));
                stopwatchService.Start();

                try
                {
                    httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, cts.Token).ConfigureAwait(false);
                    stopwatchService.Stop();
                }
                catch (OperationCanceledException)
                {
                    httpResponseMessage = new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.GatewayTimeout
                    };
                    stopwatchService.Stop();
                }

                if (_telemetryService != null)
                {
                    InsertDurableRestResult
                    (
                        $"{httpRequestMessage.Method} {httpRequestMessage.RequestUri}",
                        (int)httpResponseMessage.StatusCode,
                        (int)stopwatchService.ElapsedMilliseconds
                    );
                }

                attempts++;

                await LogResponse(httpRequestMessage, httpResponseMessage, httpClient, attempts, (int)stopwatchService.ElapsedMilliseconds).ConfigureAwait(false);
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    break;
                }
                httpRequestMessage = await CloneAsync(httpRequestMessage).ConfigureAwait(false);
            } while (retrys >= attempts);

            return httpResponseMessage;
        }

        public async Task LogRequest
      (
          HttpRequestMessage httpRequestMessage,
          HttpClient httpClient,
          int attempts
      )
        {
            var properties = new Dictionary<string, object>
            {
                { "Attempts", attempts },
                { "BaseUrl", httpClient.BaseAddress },
                { "Resource", httpRequestMessage.RequestUri.OriginalString },
                { "RequestContent", httpRequestMessage.Content != null ? await httpRequestMessage.Content.ReadAsStringAsync().ConfigureAwait(false) : null },
            };

            _loggingService.LogInformationRedacted
            (
                DURABLE_REST_MESSAGE_REQUEST,
                properties
            );

        }

        public async Task LogResponse
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
                { "Resource", httpRequestMessage.RequestUri.LocalPath },
                { "RequestContent", httpRequestMessage.Content != null ? await httpRequestMessage.Content.ReadAsStringAsync().ConfigureAwait(false) : null },
                { "ResponseContent", httpResponseMessage?.Content != null ? await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false) : null },
                { "ElapsedMilliseconds", elapsedMilliseconds },
                { "StatusCode", httpResponseMessage?.StatusCode }
            };

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                _loggingService.LogErrorRedacted
                (
                    DURABLE_REST_MESSAGE_RESPONSE,
                    null,
                    properties
                );
                return;
            }

            _loggingService.LogInformationRedacted
            (
                DURABLE_REST_MESSAGE_RESPONSE,
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
