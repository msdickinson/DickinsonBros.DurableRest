//using RestSharp;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Threading.Tasks;
//using DickinsonBros.Logger.Abstractions;
//namespace DickinsonBros.DurableRest
//{
//    public class DurableRestService
//    {
//        internal ILoggingService<DurableRestService> _loggingService;
//        public DurableRestService(ILoggingService<DurableRestService> loggingService)
//        {
//            _loggingService = loggingService;
//        }

//        public async Task<IRestResponse<T>> ExecuteAsync<T>
//        (
//            string baseURI,
//            string resource,
//            Dictionary<string, string> headers,
//            Dictionary<string, string> prams,
//            object body,
//            Method method,
//            int retrys,
//            int timeoutInMilliseconds
//        )
//        {
//            IRestClient
//            var client = new RestClient(baseURI);
//            //var request = new RestRequest();

//            //request.Resource = resource;

//            //foreach (var header in headers)
//            //{
//            //    request.AddHeader(header.Key, header.Value);
//            //}

//            //foreach (var pram in prams)
//            //{
//            //    request.AddParameter(pram.Key, pram.Value);
//            //}

//            //request.AddJsonBody(body);

//            //request.Method = method;
//            //request.Timeout = timeoutInMilliseconds;

//            var response = (IRestResponse<T>)null;
//            var stopwatch = (Stopwatch)null;
//            var retryCount = 0;
//            do
//            {
//                stopwatch = Stopwatch.StartNew();
//                response = await client.ExecuteTaskAsync<T>(request);
//                stopwatch.Stop();

//                retryCount++;

//                if (response.IsSuccessful)
//                {
//                    break;
//                }
//            } while (retrys >= retryCount);

//            if (!response.IsSuccessful)
//            {
//                _loggingService.LogErrorRedacted
//                (
//                    "DurableRest",
//                    response.ErrorException,
//                    new Dictionary<string, object>
//                    {
//                        { "retry", retryCount },
//                        { "baseURI", baseURI },
//                        { "resource", resource },
//                        { "response.ErrorException", retryCount },
//                        { "Request",  response.Request },
//                        { "Content", response.Content },
//                        { "ElapsedMilliseconds", stopwatch.ElapsedMilliseconds },
//                        { "StatusCode", response.StatusCode },
//                    }
//                );
//                return response;
//            }

//            _loggingService.LogInformationRedacted
//            (
//                "DurableRest",
//                new Dictionary<string, object>
//                {
//                    { "retry", retryCount },
//                    { "baseURI", baseURI },
//                    { "resource", resource },
//                    { "response.ErrorException", retryCount },
//                    { "Request",  response.Request },
//                    { "Content", response.Content },
//                    { "ElapsedMilliseconds", stopwatch.ElapsedMilliseconds },
//                    { "StatusCode", response.StatusCode },
//                }
//            );

//            return response;
//        }
//    }

//}
