using DickinsonBros.DurableRest.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace DickinsonBros.DurableRest.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddDurableRestService(this IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<IDurableRestService, DurableRestService>();
            serviceCollection.TryAddSingleton<IRestClientFactory, RestClientFactory>();
            serviceCollection.TryAddSingleton<IRestRequestFactory, RestRequestFactory>();
            return serviceCollection;
        }
    }
}
