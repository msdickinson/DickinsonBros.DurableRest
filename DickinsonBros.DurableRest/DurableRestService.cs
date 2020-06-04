using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DickinsonBros.Logger.Abstractions;
using DickinsonBros.DurableRest.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using DickinsonBros.Stopwatch.Abstractions;
using DickinsonBros.DateTime.Abstractions;
using DickinsonBros.Telemetry.Abstractions;
using DickinsonBros.Telemetry.Abstractions.Models;

namespace DickinsonBros.DurableRest
{


    public class DurableRestService : IDurableRestService
    {
        internal readonly IServiceProvider _serviceProvider;
        internal readonly IDateTimeService _dateTimeService;
        internal readonly ITelemetryService _telemetryService;
        internal readonly ILoggingService<DurableRestService> _loggingService;
        internal readonly IRestClientFactory _restClientFactory;
        internal const string DurableRestMessage = "DurableRest";

        public DurableRestService
        (
            IServiceProvider serviceProvider,
            IDateTimeService dateTimeService,
            ITelemetryService telemetryService,
            ILoggingService<DurableRestService> loggingService,
            IRestClientFactory restClientFactory
        )
        {
            _loggingService = loggingService;
            _dateTimeService = dateTimeService;
            _telemetryService = telemetryService;
            _restClientFactory = restClientFactory;
            _serviceProvider = serviceProvider;
        }
 
        public async Task<IRestResponse<T>> ExecuteAsync<T>
        (
            IRestRequest restRequest,
            string baseURL,
            int retrys
        )
        {
            var client =  _restClientFactory.Create(baseURL);
            var response = (IRestResponse<T>)null;
            var stopwatchService = _serviceProvider.GetRequiredService<IStopwatchService>();
            var attempts = 0;

            do
            {
                stopwatchService.Start();
                response = await client.ExecuteAsync<T>(restRequest);
                stopwatchService.Stop();
                attempts++;

                if (response.IsSuccessful)
                {
                    break;
                }
            } while (retrys >= attempts);

            LogDurableRestResult(restRequest, response, attempts, client, (int)stopwatchService.ElapsedMilliseconds);
            InsertDurableRestResult
            (
                $"{Enum.GetName(typeof(Method), restRequest.Method)} {client.BaseUrl}{restRequest.Resource}",
                (int)response.StatusCode,
                (int)stopwatchService.ElapsedMilliseconds
            );

            return response;
        }

        public async Task<IRestResponse> ExecuteAsync
        (
            IRestRequest restRequest,
            string baseURL,
            int retrys
        )
        {
            var client = _restClientFactory.Create(baseURL);
            var response = (IRestResponse)null;
            var stopwatchService = _serviceProvider.GetRequiredService<IStopwatchService>();
            var attempts = 0;

            do
            {
                stopwatchService.Start();
                response = await client.ExecuteAsync(restRequest, restRequest.Method);
                stopwatchService.Stop();
                attempts++;

                if (response.IsSuccessful)
                {
                    break;
                }

            } while (retrys >= attempts);

            LogDurableRestResult(restRequest, response, attempts, client, (int)stopwatchService.ElapsedMilliseconds);
            InsertDurableRestResult
            (
                $"{Enum.GetName(typeof(Method), restRequest.Method)} {client.BaseUrl}{restRequest.Resource}",
                (int)response.StatusCode,
                (int)stopwatchService.ElapsedMilliseconds
            );

            return response;
        }

        public void LogDurableRestResult<T>(IRestRequest restRequest, IRestResponse<T> response, int attempts, IRestClient client, int elapsedMilliseconds)
        {
            if (!response.IsSuccessful)
            {
                _loggingService.LogErrorRedacted
                (
                    DurableRestMessage,
                    response.ErrorException,
                    new Dictionary<string, object>
                    {
                        { "Attempts", attempts },
                        { "BaseUrl", client.BaseUrl },
                        { "Resource", restRequest.Resource },
                        { "Body",  restRequest.Body },
                        { "Content", response.Content },
                        { "ElapsedMilliseconds", elapsedMilliseconds },
                        { "StatusCode", response.StatusCode }
                    }
                );
                return;
            }

            _loggingService.LogInformationRedacted
              (
                  DurableRestMessage,
                  new Dictionary<string, object>
                  {
                        { "Attempts", attempts },
                        { "BaseUrl", client.BaseUrl },
                        { "Resource", restRequest.Resource },
                        { "Body",  restRequest.Body },
                        { "Content", response.Content },
                        { "ElapsedMilliseconds", elapsedMilliseconds },
                        { "StatusCode", response.StatusCode }
                  }
              );

        }
        public void LogDurableRestResult(IRestRequest restRequest, IRestResponse response, int attempts, IRestClient client, int elapsedMilliseconds)
        {
            if (!response.IsSuccessful)
            {
                _loggingService.LogErrorRedacted
                (
                    DurableRestMessage,
                    response.ErrorException,
                    new Dictionary<string, object>
                    {
                        { "Attempts", attempts },
                        { "BaseUrl", client.BaseUrl },
                        { "Resource", restRequest.Resource },
                        { "Body",  restRequest.Body },
                        { "Content", response.Content },
                        { "ElapsedMilliseconds", elapsedMilliseconds },
                        { "StatusCode", response.StatusCode }
                    }
                );
                return;
            }

            _loggingService.LogInformationRedacted
              (
                  DurableRestMessage,
                  new Dictionary<string, object>
                  {
                        { "Attempts", attempts },
                        { "BaseUrl", client.BaseUrl },
                        { "Resource", restRequest.Resource },
                        { "Body",  restRequest.Body },
                        { "Content", response.Content },
                        { "ElapsedMilliseconds", elapsedMilliseconds },
                        { "StatusCode", response.StatusCode }
                  }
              );

        }
        public void InsertDurableRestResult(string name, int statusCode, int elapsedMilliseconds)
        {
            var telemetryState = statusCode switch
            {
                int sc when (statusCode >= 200 && statusCode < 300) => TelemetryState.Successful,
                int sc when (statusCode >= 400 && statusCode < 500) => TelemetryState.BadRequest,
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


    }

}
