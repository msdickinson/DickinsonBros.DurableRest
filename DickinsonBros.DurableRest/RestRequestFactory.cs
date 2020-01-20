using DickinsonBros.DurableRest.Abstractions;
using RestSharp;
using System.Diagnostics.CodeAnalysis;

namespace DickinsonBros.DurableRest
{
    [ExcludeFromCodeCoverage]
    public class RestRequestFactory : IRestRequestFactory
    {
        public IRestRequest Create() =>
            new RestRequest();
    }
}
