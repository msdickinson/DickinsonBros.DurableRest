using DickinsonBros.DurableRest.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DickinsonBros.DurableRest.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddDurableRestService(this IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<IDurableRestService, DurableRestService>();
            return serviceCollection;
        }
    }
}
