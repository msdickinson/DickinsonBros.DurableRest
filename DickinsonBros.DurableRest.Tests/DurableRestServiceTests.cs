using DickinsonBros.DurableRest.Abstractions;
using DickinsonBros.Logger.Abstractions;
using DickinsonBros.Test;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DickinsonBros.DurableRest.Tests
{
    [TestClass]
    public class DurableRestServiceTests : BaseTest
    {
        public const string ATTEMPTS = "Attempts";
        public const string BASEURL = "BaseUrl";
        public const string RESOURCE = "Resource";
        public const string REQUEST = "Request";
        public const string CONTENT = "Content";
        public const string ELAPSED_MILLISECONDS = "ElapsedMilliseconds";
        public const string STATUS_CODE = "StatusCode";

        #region DataClass
        public class DataClass
        {
            string sample { get; set; }
        }
        #endregion

        #region LoginAsync

        [TestMethod]
        public async Task ExecuteAsyncOfT_VaildInput_RestClientFactoryCreateCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    IRestRequest restRequest = new RestRequest();
                    string baseURL = "https://www.demo.com/";
                    int retrys = 0;

                    //  Rest Response
                    var restResponseMock = serviceProvider.GetMock<IRestResponse<DataClass>>();
                    restResponseMock
                        .SetupGet(restResponse => restResponse.IsSuccessful)
                        .Returns(true);
                    restResponseMock.Object.ErrorException = null;
                    restResponseMock.Object.Request = new RestRequest();
                    restResponseMock.Object.Content = "";
                    restResponseMock.Object.StatusCode = System.Net.HttpStatusCode.OK;

                    //  Rest Client
                    var restClientMock = serviceProvider.GetMock<IRestClient>();
                    restClientMock
                        .SetupGet(restClient => restClient.BaseUrl)
                        .Returns(new Uri(baseURL));
                    restClientMock
                             .Setup
                     (
                         restClientFactory => restClientFactory.ExecuteAsync<DataClass>
                         (
                             It.IsAny<IRestRequest>(), It.IsAny<CancellationToken>()
                         )
                     )
                     .ReturnsAsync
                     (
                        restResponseMock.Object
                     );

                    //  Rest Client Factory
                    var restClientFactoryMock = serviceProvider.GetMock<IRestClientFactory>();
                    restClientFactoryMock
                        .Setup
                        (
                            restClientFactory => restClientFactory.Create
                            (
                                It.IsAny<string>()
                            )
                        )
                        .Returns
                        (
                            restClientMock.Object
                        );

                    //  Logging
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<DurableRestService>>();
                    loggingServiceMock.Setup
                    (
                        loggingService => loggingService.LogErrorRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<Exception>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    );

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync<DataClass>(restRequest, baseURL, retrys);

                    //Assert
                    restClientFactoryMock
                    .Verify(
                        restClientFactory => restClientFactory.Create
                        (
                            baseURL
                        ),
                        Times.Once
                    );
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsyncOfT_VaildInput_RestClientExecuteAsyncCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    IRestRequest restRequest = new RestRequest();
                    string baseURL = "https://www.demo.com/";
                    int retrys = 0;

                    //  Rest Response
                    var restResponseMock = serviceProvider.GetMock<IRestResponse<DataClass>>();
                    restResponseMock
                        .SetupGet(restResponse => restResponse.IsSuccessful)
                        .Returns(true);
                    restResponseMock.Object.ErrorException = null;
                    restResponseMock.Object.Request = new RestRequest();
                    restResponseMock.Object.Content = "";
                    restResponseMock.Object.StatusCode = System.Net.HttpStatusCode.OK;

                    //  Rest Client
                    var restClientMock = serviceProvider.GetMock<IRestClient>();
                    restClientMock
                        .SetupGet(restClient => restClient.BaseUrl)
                        .Returns(new Uri(baseURL));
                    restClientMock
                             .Setup
                     (
                         restClientFactory => restClientFactory.ExecuteAsync<DataClass>
                         (
                             It.IsAny<IRestRequest>(), It.IsAny<CancellationToken>()
                         )
                     )
                     .ReturnsAsync
                     (
                        restResponseMock.Object
                     );

                    //  Rest Client Factory
                    var restClientFactoryMock = serviceProvider.GetMock<IRestClientFactory>();
                    restClientFactoryMock
                        .Setup
                        (
                            restClientFactory => restClientFactory.Create
                            (
                                It.IsAny<string>()
                            )
                        )
                        .Returns
                        (
                            restClientMock.Object
                        );

                    //  Logging
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<DurableRestService>>();
                    loggingServiceMock.Setup
                    (
                        loggingService => loggingService.LogErrorRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<Exception>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    );

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync<DataClass>(restRequest, baseURL, retrys);

                    //Assert
                    restClientMock
                    .Verify
                    (
                        restClientFactory => restClientFactory.ExecuteAsync<DataClass>
                        (
                            restRequest,
                            It.IsAny<CancellationToken>()
                        ),
                        Times.Once
                    );
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsyncOfT_RequestIsSuccessful_LogInformationRedacted()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    IRestRequest restRequest = new RestRequest();
                    restRequest.Resource = "resoure";
                    string baseURL = "https://www.demo.com/";
                    int retrys = 0;

                    //  Rest Response
                    var content = "Content";
                    var statusCode = System.Net.HttpStatusCode.OK;
                    var restResponseMock = serviceProvider.GetMock<IRestResponse<DataClass>>();
                    restResponseMock
                        .SetupGet(restResponse => restResponse.IsSuccessful)
                        .Returns(true);
                    restResponseMock.Object.ErrorException = null;
                    restResponseMock.Object.Request = new RestRequest();
                    restResponseMock.Object.Content = content;
                    restResponseMock.Object.StatusCode = statusCode;

                    //  Rest Client
                    var restClientMock = serviceProvider.GetMock<IRestClient>();
                    restClientMock
                        .SetupGet(restClient => restClient.BaseUrl)
                        .Returns(new Uri(baseURL));
                    restClientMock
                             .Setup
                     (
                         restClientFactory => restClientFactory.ExecuteAsync<DataClass>
                         (
                             It.IsAny<IRestRequest>(), It.IsAny<CancellationToken>()
                         )
                     )
                     .ReturnsAsync
                     (
                        restResponseMock.Object
                     );

                    //  Rest Client Factory
                    var restClientFactoryMock = serviceProvider.GetMock<IRestClientFactory>();
                    restClientFactoryMock
                        .Setup
                        (
                            restClientFactory => restClientFactory.Create
                            (
                                It.IsAny<string>()
                            )
                        )
                        .Returns
                        (
                            restClientMock.Object
                        );

                    //  Logging
                    string messageObserved = null;
                    Dictionary<string, object> propertiesObserved = null;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<DurableRestService>>();
                    loggingServiceMock
                        .Setup
                        (
                            loggingService => loggingService.LogInformationRedacted
                            (
                                It.IsAny<string>(),
                                It.IsAny<IDictionary<string, object>>()
                            )
                        )
                        .Callback<string, IDictionary<string, object>>((message, properties) =>
                        {
                            messageObserved = message;
                            propertiesObserved = (Dictionary<string, object>)properties;
                        });

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync<DataClass>(restRequest, baseURL, retrys);

                    //Assert
                    loggingServiceMock.Verify
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    );

                    Assert.AreEqual(DurableRestService.DurableRestMessage, messageObserved);
                    Assert.AreEqual(1, (int)propertiesObserved[ATTEMPTS]);
                    Assert.AreEqual(baseURL, propertiesObserved[BASEURL].ToString());
                    Assert.AreEqual(restRequest.Resource, (string)propertiesObserved[RESOURCE]);
                    Assert.AreEqual(restRequest, propertiesObserved[REQUEST]);
                    Assert.AreEqual(content, (string)propertiesObserved[CONTENT]);
                    Assert.IsTrue((long)propertiesObserved[ELAPSED_MILLISECONDS] >= 0);
                    Assert.AreEqual(statusCode, propertiesObserved[STATUS_CODE]);
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsyncOfT_RequestIsNotSuccessful_LogErrorRedacted()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    IRestRequest restRequest = new RestRequest();
                    restRequest.Resource = "resoure";
                    string baseURL = "https://www.demo.com/";
                    int retrys = 1;

                    //  Rest Response
                    var content = "Content";
                    var statusCode = System.Net.HttpStatusCode.BadRequest;
                    var errorException = new Exception("Bad Request Fail");
                    var restResponseMock = serviceProvider.GetMock<IRestResponse<DataClass>>();
                    restResponseMock
                        .SetupGet(restResponse => restResponse.IsSuccessful)
                        .Returns(false);
                    restResponseMock.Object.ErrorException = null;
                    restResponseMock.Object.Request = new RestRequest();
                    restResponseMock.Object.Content = content;
                    restResponseMock.Object.StatusCode = statusCode;
                    restResponseMock.Object.ErrorException = errorException;

                    //  Rest Client
                    var restClientMock = serviceProvider.GetMock<IRestClient>();
                    restClientMock
                        .SetupGet(restClient => restClient.BaseUrl)
                        .Returns(new Uri(baseURL));
                    restClientMock
                             .Setup
                     (
                         restClientFactory => restClientFactory.ExecuteAsync<DataClass>
                         (
                             It.IsAny<IRestRequest>(), It.IsAny<CancellationToken>()
                         )
                     )
                     .ReturnsAsync
                     (
                        restResponseMock.Object
                     );

                    //  Rest Client Factory
                    var restClientFactoryMock = serviceProvider.GetMock<IRestClientFactory>();
                    restClientFactoryMock
                        .Setup
                        (
                            restClientFactory => restClientFactory.Create
                            (
                                It.IsAny<string>()
                            )
                        )
                        .Returns
                        (
                            restClientMock.Object
                        );

                    //  Logging
                    string messageObserved = null;
                    var exceptionObserved = (Exception)null;
                    Dictionary<string, object> propertiesObserved = null;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<DurableRestService>>();
                    loggingServiceMock
                        .Setup
                        (
                            loggingService => loggingService.LogErrorRedacted
                            (
                                It.IsAny<string>(),
                                It.IsAny<Exception>(),
                                It.IsAny<IDictionary<string, object>>()
                            )
                        )
                        .Callback<string, Exception, IDictionary<string, object>>((message, exception, properties) =>
                        {
                            messageObserved = message;
                            exceptionObserved = exception;
                            propertiesObserved = (Dictionary<string, object>)properties;
                        });

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync<DataClass>(restRequest, baseURL, retrys);

                    //Assert
                    loggingServiceMock.Verify
                    (
                        loggingService => loggingService.LogErrorRedacted
                        (
                            messageObserved,
                            exceptionObserved,
                            propertiesObserved
                        )
                    );

                    Assert.AreEqual(DurableRestService.DurableRestMessage, messageObserved);
                    Assert.AreEqual(errorException, exceptionObserved);
                    Assert.AreEqual(2, (int)propertiesObserved[ATTEMPTS]);
                    Assert.AreEqual(baseURL, propertiesObserved[BASEURL].ToString());
                    Assert.AreEqual(restRequest.Resource, (string)propertiesObserved[RESOURCE]);
                    Assert.AreEqual(restRequest, propertiesObserved[REQUEST]);
                    Assert.AreEqual(content, (string)propertiesObserved[CONTENT]);
                    Assert.IsTrue((long)propertiesObserved[ELAPSED_MILLISECONDS] >= 0);
                    Assert.AreEqual(statusCode, propertiesObserved[STATUS_CODE]);
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsync_VaildInput_RestClientFactoryCreateCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var method = Method.POST;

                    IRestRequest restRequest = new RestRequest
                    {
                        Method = method
                    };

                    string baseURL = "https://www.demo.com/";
                    int retrys = 0;

                    //  Rest Response
                    var restResponseMock = serviceProvider.GetMock<IRestResponse>();
                    var statusCode = System.Net.HttpStatusCode.OK;

                    restResponseMock
                        .SetupGet(restResponse => restResponse.IsSuccessful)
                        .Returns(true);
                    restResponseMock.Object.ErrorException = null;
                    restResponseMock.Object.Request = new RestRequest();
                    restResponseMock.Object.Content = "";
                    restResponseMock.Object.StatusCode = statusCode;

                    //  Rest Client
                    var restClientMock = serviceProvider.GetMock<IRestClient>();
                    restClientMock
                        .SetupGet(restClient => restClient.BaseUrl)
                        .Returns(new Uri(baseURL));
                    restClientMock
                             .Setup
                     (
                         restClient => restClient.ExecuteAsync
                         (
                             It.IsAny<IRestRequest>(),
                             It.IsAny<Method>(),
                             It.IsAny<CancellationToken>()
                         )
                     )
                     .ReturnsAsync
                     (
                        restResponseMock.Object
                     );

                    //  Rest Client Factory
                    var restClientFactoryMock = serviceProvider.GetMock<IRestClientFactory>();
                    restClientFactoryMock
                        .Setup
                        (
                            restClientFactory => restClientFactory.Create
                            (
                                It.IsAny<string>()
                            )
                        )
                        .Returns
                        (
                            restClientMock.Object
                        );

                    //  Logging
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<DurableRestService>>();
                    loggingServiceMock.Setup
                    (
                        loggingService => loggingService.LogErrorRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<Exception>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    );

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync(restRequest, baseURL, retrys);

                    //Assert
                    restClientFactoryMock
                    .Verify(
                        restClientFactory => restClientFactory.Create
                        (
                            baseURL
                        ),
                        Times.Once
                    );
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsync_VaildInput_RestClientExecuteAsyncCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var method = Method.POST;

                    IRestRequest restRequest = new RestRequest
                    {
                        Method = method
                    };
                    string baseURL = "https://www.demo.com/";
                    int retrys = 0;

                    //  Rest Response
                    var restResponseMock = serviceProvider.GetMock<IRestResponse>();
                    var statusCode = System.Net.HttpStatusCode.OK;
                    restResponseMock
                        .SetupGet(restResponse => restResponse.IsSuccessful)
                        .Returns(true);
                    restResponseMock.Object.ErrorException = null;
                    restResponseMock.Object.Request = new RestRequest();
                    restResponseMock.Object.Content = "";
                    restResponseMock.Object.StatusCode = statusCode;

                    //  Rest Client
                    var restClientMock = serviceProvider.GetMock<IRestClient>();
                    restClientMock
                        .SetupGet(restClient => restClient.BaseUrl)
                        .Returns(new Uri(baseURL));
                    restClientMock
                             .Setup
                     (
                         restClient => restClient.ExecuteAsync
                         (
                             It.IsAny<IRestRequest>(),
                             It.IsAny<Method>(),
                             It.IsAny<CancellationToken>()
                         )
                     )
                     .ReturnsAsync
                     (
                        restResponseMock.Object
                     );

                    //  Rest Client Factory
                    var restClientFactoryMock = serviceProvider.GetMock<IRestClientFactory>();
                    restClientFactoryMock
                        .Setup
                        (
                            restClientFactory => restClientFactory.Create
                            (
                                It.IsAny<string>()
                            )
                        )
                        .Returns
                        (
                            restClientMock.Object
                        );

                    //  Logging
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<DurableRestService>>();
                    loggingServiceMock.Setup
                    (
                        loggingService => loggingService.LogErrorRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<Exception>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    );

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync(restRequest, baseURL, retrys);

                    //Assert
                    restClientMock
                    .Verify
                    (
                        restClientFactory => restClientFactory.ExecuteAsync
                        (
                            restRequest,
                            method,
                            It.IsAny<CancellationToken>()
                        ),
                        Times.Once
                    );
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsync_RequestIsSuccessful_LogInformationRedacted()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var method = Method.POST;

                    IRestRequest restRequest = new RestRequest
                    {
                        Method = method
                    };
                    restRequest.Resource = "resoure";
                    string baseURL = "https://www.demo.com/";
                    int retrys = 0;

                    //  Rest Response
                    var content = "Content";
                    var statusCode = System.Net.HttpStatusCode.OK;
                    var restResponseMock = serviceProvider.GetMock<IRestResponse>();
                    restResponseMock
                        .SetupGet(restResponse => restResponse.IsSuccessful)
                        .Returns(true);
                    restResponseMock.Object.ErrorException = null;
                    restResponseMock.Object.Request = new RestRequest();
                    restResponseMock.Object.Content = content;
                    restResponseMock.Object.StatusCode = statusCode;

                    //  Rest Client
                    var restClientMock = serviceProvider.GetMock<IRestClient>();
                    restClientMock
                        .SetupGet(restClient => restClient.BaseUrl)
                        .Returns(new Uri(baseURL));
                    restClientMock
                             .Setup
                     (
                         restClient => restClient.ExecuteAsync
                         (
                             It.IsAny<IRestRequest>(),
                             It.IsAny<Method>(),
                             It.IsAny<CancellationToken>()
                         )
                     )
                     .ReturnsAsync
                     (
                        restResponseMock.Object
                     );

                    //  Rest Client Factory
                    var restClientFactoryMock = serviceProvider.GetMock<IRestClientFactory>();
                    restClientFactoryMock
                        .Setup
                        (
                            restClientFactory => restClientFactory.Create
                            (
                                It.IsAny<string>()
                            )
                        )
                        .Returns
                        (
                            restClientMock.Object
                        );

                    //  Logging
                    string messageObserved = null;
                    Dictionary<string, object> propertiesObserved = null;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<DurableRestService>>();
                    loggingServiceMock
                        .Setup
                        (
                            loggingService => loggingService.LogInformationRedacted
                            (
                                It.IsAny<string>(),
                                It.IsAny<IDictionary<string, object>>()
                            )
                        )
                        .Callback<string, IDictionary<string, object>>((message, properties) =>
                        {
                            messageObserved = message;
                            propertiesObserved = (Dictionary<string, object>)properties;
                        });

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync(restRequest, baseURL, retrys);

                    //Assert
                    loggingServiceMock.Verify
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    );

                    Assert.AreEqual(DurableRestService.DurableRestMessage, messageObserved);
                    Assert.AreEqual(1, (int)propertiesObserved[ATTEMPTS]);
                    Assert.AreEqual(baseURL, propertiesObserved[BASEURL].ToString());
                    Assert.AreEqual(restRequest.Resource, (string)propertiesObserved[RESOURCE]);
                    Assert.AreEqual(restRequest, propertiesObserved[REQUEST]);
                    Assert.AreEqual(content, (string)propertiesObserved[CONTENT]);
                    Assert.IsTrue((long)propertiesObserved[ELAPSED_MILLISECONDS] >= 0);
                    Assert.AreEqual(statusCode, propertiesObserved[STATUS_CODE]);
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsync_RequestIsNotSuccessful_LogErrorRedacted()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var method = Method.POST;

                    IRestRequest restRequest = new RestRequest
                    {
                        Method = method
                    };
                    restRequest.Resource = "resoure";
                    string baseURL = "https://www.demo.com/";
                    int retrys = 1;

                    //  Rest Response
                    var content = "Content";
                    var statusCode = System.Net.HttpStatusCode.BadRequest;
                    var errorException = new Exception("Bad Request Fail");
                    var restResponseMock = serviceProvider.GetMock<IRestResponse>();
                    restResponseMock
                        .SetupGet(restResponse => restResponse.IsSuccessful)
                        .Returns(false);
                    restResponseMock.Object.ErrorException = null;
                    restResponseMock.Object.Request = new RestRequest();
                    restResponseMock.Object.Content = content;
                    restResponseMock.Object.StatusCode = statusCode;
                    restResponseMock.Object.ErrorException = errorException;

                    //  Rest Client
                    var restClientMock = serviceProvider.GetMock<IRestClient>();
                    restClientMock
                        .SetupGet(restClient => restClient.BaseUrl)
                        .Returns(new Uri(baseURL));
                    restClientMock
                             .Setup
                     (
                         restClient => restClient.ExecuteAsync
                         (
                             It.IsAny<IRestRequest>(),
                             It.IsAny<Method>(),
                             It.IsAny<CancellationToken>()
                         )
                     )
                     .ReturnsAsync
                     (
                        restResponseMock.Object
                     );

                    //  Rest Client Factory
                    var restClientFactoryMock = serviceProvider.GetMock<IRestClientFactory>();
                    restClientFactoryMock
                        .Setup
                        (
                            restClientFactory => restClientFactory.Create
                            (
                                It.IsAny<string>()
                            )
                        )
                        .Returns
                        (
                            restClientMock.Object
                        );

                    //  Logging
                    string messageObserved = null;
                    var exceptionObserved = (Exception)null;
                    Dictionary<string, object> propertiesObserved = null;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<DurableRestService>>();
                    loggingServiceMock
                        .Setup
                        (
                            loggingService => loggingService.LogErrorRedacted
                            (
                                It.IsAny<string>(),
                                It.IsAny<Exception>(),
                                It.IsAny<IDictionary<string, object>>()
                            )
                        )
                        .Callback<string, Exception, IDictionary<string, object>>((message, exception, properties) =>
                        {
                            messageObserved = message;
                            exceptionObserved = exception;
                            propertiesObserved = (Dictionary<string, object>)properties;
                        });

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync(restRequest, baseURL, retrys);

                    //Assert
                    loggingServiceMock.Verify
                    (
                        loggingService => loggingService.LogErrorRedacted
                        (
                            messageObserved,
                            exceptionObserved,
                            propertiesObserved
                        )
                    );

                    Assert.AreEqual(DurableRestService.DurableRestMessage, messageObserved);
                    Assert.AreEqual(errorException, exceptionObserved);
                    Assert.AreEqual(2, (int)propertiesObserved[ATTEMPTS]);
                    Assert.AreEqual(baseURL, propertiesObserved[BASEURL].ToString());
                    Assert.AreEqual(restRequest.Resource, (string)propertiesObserved[RESOURCE]);
                    Assert.AreEqual(restRequest, propertiesObserved[REQUEST]);
                    Assert.AreEqual(content, (string)propertiesObserved[CONTENT]);
                    Assert.IsTrue((long)propertiesObserved[ELAPSED_MILLISECONDS] >= 0);
                    Assert.AreEqual(statusCode, propertiesObserved[STATUS_CODE]);
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }


        #endregion

        private IServiceCollection ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(Mock.Of<IRestResponse<DataClass>>());
            serviceCollection.AddSingleton(Mock.Of<IRestResponse>());
            serviceCollection.AddSingleton(Mock.Of<IRestClient>());
            serviceCollection.AddSingleton(Mock.Of<IRestClientFactory>());
            serviceCollection.AddSingleton(Mock.Of<ILoggingService<DurableRestService>>());
            serviceCollection.AddSingleton<IDurableRestService, DurableRestService>();

            return serviceCollection;
        }

    }
}
