using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DickinsonBros.Logger.Abstractions;
using DickinsonBros.DurableRest.Abstractions;

namespace DickinsonBros.DurableRest
{
    public class DurableRestService : IDurableRestService
    {
        internal readonly ILoggingService<DurableRestService> _loggingService;
        internal readonly IRestClientFactory _restClientFactory;
        internal const string DurableRestMessage = "DurableRest";

        public DurableRestService
        (
            ILoggingService<DurableRestService> loggingService,
            IRestClientFactory restClientFactory
        )
        {
            _loggingService = loggingService;
            _restClientFactory = restClientFactory;
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
            var stopwatch = (Stopwatch)null;
            var attempts = 0;
            do
            {
                stopwatch = Stopwatch.StartNew();
                response = await client.ExecuteAsync<T>(restRequest);
                stopwatch.Stop();
                attempts++;

                if (response.IsSuccessful)
                {
                    break;
                }
            } while (retrys >= attempts);

            if(!response.IsSuccessful)
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
                        { "Request",  restRequest },
                        { "Content", response.Content },
                        { "ElapsedMilliseconds", stopwatch.ElapsedMilliseconds },
                        { "StatusCode", response.StatusCode }
                    }
                );
                return response;
            }
         
            _loggingService.LogInformationRedacted
            (
                DurableRestMessage,
                new Dictionary<string, object>
                {
                    { "Attempts", attempts },
                    { "BaseUrl", client.BaseUrl },
                    { "Resource", restRequest.Resource },
                    { "Request",  restRequest },
                    { "Content", response.Content },
                    { "ElapsedMilliseconds", stopwatch.ElapsedMilliseconds },
                    { "StatusCode", response.StatusCode }
                }
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
            var stopwatch = (Stopwatch)null;
            var attempts = 0;
            do
            {
                stopwatch = Stopwatch.StartNew();
                response = await client.ExecuteAsync(restRequest, restRequest.Method);
                stopwatch.Stop();
                attempts++;

                if (response.IsSuccessful)
                {
                    break;
                }

            } while (retrys >= attempts);

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
                        { "Request",  restRequest },
                        { "Content", response.Content },
                        { "ElapsedMilliseconds", stopwatch.ElapsedMilliseconds },
                        { "StatusCode", response.StatusCode }
                    }
                );
                return response;
            }

            _loggingService.LogInformationRedacted
            (
                DurableRestMessage,
                new Dictionary<string, object>
                {
                    { "Attempts", attempts },
                    { "BaseUrl", client.BaseUrl },
                    { "Resource", restRequest.Resource },
                    { "Request",  restRequest },
                    { "Content", response.Content },
                    { "ElapsedMilliseconds", stopwatch.ElapsedMilliseconds },
                    { "StatusCode", response.StatusCode }
                }
            );

            return response;
        }
    }

}
