using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DickinsonBros.DurableRest.Runner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var durableRestService = new DurableRestService(null);

            var baseURI = "";
            var resource = "";
            var headers = new Dictionary<string, string>
            {

            };
            var prams = new Dictionary<string, string>
            {

            };
            var body = "";
            var method = RestSharp.Method.POST;
            var retrys = 3;
            var timeoutInMilliseconds = 30000;

            var result = await durableRestService.ExecuteAsync<object>
            (
                baseURI,
                resource,
                headers,
                prams,
                body,
                method,
                retrys,
                timeoutInMilliseconds
            );

            Console.WriteLine("Hello World!");
        }
    }
}
