using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DickinsonBros.Logger.Abstractions;
namespace DickinsonBros.DurableRest
{
    public class DurableRestService
    {
        internal ILoggingService<DurableRestService> _loggingService;
        public DurableRestService(ILoggingService<DurableRestService> loggingService)
        {
            _loggingService = loggingService;
        }

        public async Task<IRestResponse<T>> ExecuteAsync<T>
        (
            IRestClient client,
            IRestRequest restRequest,
            int retrys
        )
        {
            var response = (IRestResponse<T>)null;
            var stopwatch = (Stopwatch)null;
            var attempts = 0;
            do
            {
                stopwatch = Stopwatch.StartNew();
                response = await client.ExecuteTaskAsync<T>(restRequest);
                stopwatch.Stop();

                if (response.IsSuccessful)
                {
                    break;
                }

                attempts++;
            } while (retrys >= attempts);

            //Save Data To SQL To be used in Reports

            _loggingService.LogErrorRedacted
            (
                "DurableRest",
                response.ErrorException,
                new Dictionary<string, object>
                {
                    { "Attempts", attempts },
                    { "BaseUrl", client.BaseUrl },
                    { "Resource", restRequest.Resource },
                    { "Request",  response.Request },
                    { "Content", response.Content },
                    { "ElapsedMilliseconds", stopwatch.ElapsedMilliseconds },
                    { "StatusCode", response.StatusCode }
                }
            );
            return response;
        }

        public async Task<IRestResponse> ExecuteAsync
    (
        IRestClient client,
        IRestRequest restRequest,
        int retrys
    )
        {
            var response = (IRestResponse)null;
            var stopwatch = (Stopwatch)null;
            var attempts = 0;
            do
            {
                stopwatch = Stopwatch.StartNew();
                response = await client.ExecuteTaskAsync(restRequest);
                stopwatch.Stop();

                if (response.IsSuccessful)
                {
                    break;
                }

                attempts++;
            } while (retrys >= attempts);

            //Save Data To SQL To be used in Reports

            _loggingService.LogErrorRedacted
            (
                "DurableRest",
                response.ErrorException,
                new Dictionary<string, object>
                {
                    { "Attempts", attempts },
                    { "BaseUrl", client.BaseUrl },
                    { "Resource", restRequest.Resource },
                    { "Request",  response.Request },
                    { "Content", response.Content },
                    { "ElapsedMilliseconds", stopwatch.ElapsedMilliseconds },
                    { "StatusCode", response.StatusCode }
                }
            );
            return response;
        }
    }

}
