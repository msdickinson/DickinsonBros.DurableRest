using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using DickinsonBros.DurableRest.Runner.Services.JsonPlaceHolderProxy.Models;
using DickinsonBros.DurableRest.Runner.Models.Models;
using DickinsonBros.DurableRest.Abstractions;
using DickinsonBros.DurableRest.Abstractions.Models;

namespace DickinsonBros.DurableRest.Runner.Services.JsonPlaceHolderProxy
{
    public class JsonPlaceHolderProxyService : IJsonPlaceHolderProxyService
    {
        internal readonly JsonPlaceHolderProxyOptions _options;
        internal readonly IDurableRestService _durableRestService;
        internal readonly HttpClient _httpClient;

        public JsonPlaceHolderProxyService(IDurableRestService durableRestService, HttpClient httpClient, IOptions<JsonPlaceHolderProxyOptions> options)
        {
            _durableRestService = durableRestService;
            _options = options.Value;
            _httpClient = httpClient;
        }

        public async Task<HttpResponse<Todo>> GetTodosAsync(GetTodosRequest getTodosRequest)
        {
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{_options.GetTodosResource}{getTodosRequest.Items}", UriKind.Relative)
            };

            return await _durableRestService.ExecuteAsync<Todo>(_httpClient, httpRequestMessage, _options.GetTodosRetrys, _options.GetTodosTimeoutInSeconds).ConfigureAwait(false);
        }
    }
}
