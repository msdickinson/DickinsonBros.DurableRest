using DickinsonBros.DurableRest.Abstractions;
using DickinsonBros.DurableRest.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace DickinsonBros.DurableRest.Tests.Extensions
{
    [TestClass]
    public class IServiceCollectionExtensionsTests
    {
        [TestMethod]
        public void AddDurableRestService_Should_Succeed()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            // Act
            serviceCollection.AddDurableRestService();

            // Assert
            Assert.IsTrue(serviceCollection.Any(serviceDefinition => serviceDefinition.ServiceType == typeof(IDurableRestService) &&
                                           serviceDefinition.ImplementationType == typeof(DurableRestService) &&
                                           serviceDefinition.Lifetime == ServiceLifetime.Singleton));

            Assert.IsTrue(serviceCollection.Any(serviceDefinition => serviceDefinition.ServiceType == typeof(IRestClientFactory) &&
                               serviceDefinition.ImplementationType == typeof(RestClientFactory) &&
                               serviceDefinition.Lifetime == ServiceLifetime.Singleton));

            Assert.IsTrue(serviceCollection.Any(serviceDefinition => serviceDefinition.ServiceType == typeof(IRestRequestFactory) &&
                               serviceDefinition.ImplementationType == typeof(IRestRequestFactory) &&
                               serviceDefinition.Lifetime == ServiceLifetime.Singleton));


        }
    }
}
