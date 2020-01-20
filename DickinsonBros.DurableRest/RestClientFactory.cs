using DickinsonBros.DurableRest.Abstractions;
using RestSharp;
using System.Diagnostics.CodeAnalysis;

namespace DickinsonBros.DurableRest
{
    [ExcludeFromCodeCoverage]
    public class RestClientFactory : IRestClientFactory
    {
        public IRestClient Create(string baseURL) =>
            new RestClient(baseURL);
    }
}
