using DickinsonBros.DateTime.Abstractions;
using DickinsonBros.DurableRest.Abstractions;
using DickinsonBros.DurableRest.Abstractions.Models;
using DickinsonBros.Guid.Abstractions;
using DickinsonBros.Logger.Abstractions;
using DickinsonBros.Stopwatch.Abstractions;
using DickinsonBros.Telemetry.Abstractions;
using DickinsonBros.Telemetry.Abstractions.Models;
using DickinsonBros.Test;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
        public const string BODY = "Body";
        public const string REQUEST_CONTENT = "RequestContent";
        public const string RESPONSE_CONTENT = "ResponseContent";
        public const string ELAPSED_MILLISECONDS = "ElapsedMilliseconds";
        public const string STATUS_CODE = "StatusCode";

        #region DataClass
        public class DataClass
        {
            public int UserId { get; set; }
            public int Id { get; set; }
            public string Title { get; set; }
            public bool Completed { get; set; }
        }
        #endregion

        #region ExecuteAsync

        [TestMethod]
        public async Task ExecuteAsync_ExistingCorrelationIdHeader_NewGuidNotCalledAndCorrelationServiceNotCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var retrys = 0;
                    var timeout = 30.0;
                    var expectedGuid = new System.Guid("2cce9f60-a582-480d-a0ea-4ea39dab6961");

                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative),
                    };

                    httpRequestMessage.Headers.Add(DurableRestService.CORRELATION_ID, "DemoId");

                    var httpResponseMessage = new HttpResponseMessage();

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponseMessage);

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };
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

                    //  Correlation
                    var correlationServiceMock = serviceProvider.GetMock<ICorrelationService>();
                    correlationServiceMock
                        .SetupGet(correlationService => correlationService.CorrelationId);

                    //  Guid
                    var guidServiceMock = serviceProvider.GetMock<IGuidService>();
                    guidServiceMock
                        .Setup(guidService => guidService.NewGuid());


                    //  DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(new System.DateTime(2020, 1, 1));

                    //  Stopwatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Start());

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Stop());

                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(100);

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync(httpClientMock, httpRequestMessage, retrys, timeout);

                    //Assert
                    guidServiceMock
                    .Verify(
                        guidService => guidService.NewGuid(),
                        Times.Never
                    );

                    correlationServiceMock
                    .VerifyGet(
                        correlationService => correlationService.CorrelationId,
                        Times.Never
                    );
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsync_NoCorrelationIdHeaderAndNoCorrelationServiceCorrelationId_NewGuidCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var retrys = 0;
                    var timeout = 30.0;
                    var expectedGuid = new System.Guid("2cce9f60-a582-480d-a0ea-4ea39dab6961");

                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative)
                    };

                    var httpResponseMessage = new HttpResponseMessage();

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponseMessage);

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };
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

                    //  DateTime
                    var guidServiceMock = serviceProvider.GetMock<IGuidService>();
                    guidServiceMock
                        .Setup(guidService => guidService.NewGuid())
                        .Returns(expectedGuid);


                    //  DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(new System.DateTime(2020, 1, 1));

                    //  Stopwatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Start());

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Stop());

                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(100);

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync(httpClientMock, httpRequestMessage, retrys, timeout);

                    //Assert
                    guidServiceMock
                    .Verify(
                        guidService => guidService.NewGuid(),
                        Times.Once
                    );
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsync_NoCorrelationIdHeaderAndHasCorrelationServiceCorrelationId_CorrelationServiceCorrelationIdCalledTwice()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var retrys = 0;
                    var timeout = 30.0;
                    var expectedCorrelationId = "2cce9f60-a582-480d-a0ea-4ea39dab6961";

                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative)
                    };

                    var httpResponseMessage = new HttpResponseMessage();

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponseMessage);

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };
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

                    //  Correlation
                    var correlationServiceMock = serviceProvider.GetMock<ICorrelationService>();
                    correlationServiceMock
                        .SetupGet(correlationService => correlationService.CorrelationId)
                        .Returns(expectedCorrelationId);


                    //  DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(new System.DateTime(2020, 1, 1));

                    //  Stopwatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Start());

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Stop());

                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(100);

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync(httpClientMock, httpRequestMessage, retrys, timeout);

                    //Assert
                    correlationServiceMock
                    .VerifyGet(
                        correlationService => correlationService.CorrelationId,
                        Times.Exactly(2)
                    );
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsync_VaildInput_LogsRequestRedacted()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var retrys = 0;
                    var timeout = 30.0;

                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative),
                        Content = new StringContent("{\"name\":\"John Doe\",\"age\":33}", Encoding.UTF8, "application/json")
                    };

                    var httpResponseMessage = new HttpResponseMessage()
                    {
                        Content = new StringContent(
@"{
  ""userId"": 0,
  ""id"": 0,
  ""title"": null,
  ""completed"": false
}"
                        , Encoding.UTF8, "application/json")
                    };

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponseMessage);

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };

                    var messagesObserved = new List<string>();
                    var propertiesObserved = new List<Dictionary<string, object>>();
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
                            messagesObserved.Add(message);
                            propertiesObserved.Add((Dictionary<string, object>)properties);
                        });

                    //  DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(new System.DateTime(2020, 1, 1));

                    //  Stopwatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Start());

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Stop());

                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(100);

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync(httpClientMock, httpRequestMessage, retrys, timeout);


                    //Assert
                    var index = messagesObserved.FindIndex(message => message == uutConcrete.DURABLE_REST_MESSAGE_REQUEST);
                    loggingServiceMock.Verify
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            messagesObserved[index],
                            propertiesObserved[index]
                        )
                    );
                    Assert.AreEqual(1, messagesObserved.Where(message => message == uutConcrete.DURABLE_REST_MESSAGE_REQUEST).Count());
                    Assert.AreEqual(uutConcrete.DURABLE_REST_MESSAGE_REQUEST, messagesObserved[index]);
                    Assert.AreEqual(httpClientMock.BaseAddress, propertiesObserved[index][BASEURL].ToString());
                    Assert.AreEqual(await httpRequestMessage.Content.ReadAsStringAsync(), propertiesObserved[index][REQUEST_CONTENT]);

                    //HttpClient Mutates httpRequestMessage, Thus hard coding expected value 
                    Assert.AreEqual("todos/", (string)propertiesObserved[index][RESOURCE]);

                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsync_VaildInput_StopWatchStartCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var retrys = 0;
                    var timeout = 30.0;

                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative)
                    };

                    var httpResponseMessage = new HttpResponseMessage();

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponseMessage);

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };
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

                    //  DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(new System.DateTime(2020, 1, 1));

                    //  Stopwatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Start());

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Stop());

                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(100);

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync(httpClientMock, httpRequestMessage, retrys, timeout);

                    //Assert
                    stopwatchServiceMock
                    .Verify(
                        stopwatchService => stopwatchService.Start(),
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
                    var retrys = 0;
                    var timeout = 30.0;

                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative)
                    };

                    var httpResponseMessage = new HttpResponseMessage();

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponseMessage);

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };
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

                    //  DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(new System.DateTime(2020, 1, 1));

                    //  Stopwatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Start());

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Stop());

                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(100);

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync(httpClientMock, httpRequestMessage, retrys, timeout);

                    //Assert
                    httpMessageHandlerMock.Protected().Verify(
                       "SendAsync",
                       Times.Exactly(1),
                       ItExpr.Is<HttpRequestMessage>(req => req == httpRequestMessage),
                       ItExpr.IsAny<CancellationToken>());
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsync_VaildInput_StopWatchStopCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var retrys = 0;
                    var timeout = 30.0;

                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative)
                    };

                    var httpResponseMessage = new HttpResponseMessage();

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponseMessage);

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };
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

                    //  DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(new System.DateTime(2020, 1, 1));

                    //  Stopwatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Start());

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Stop());

                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(100);

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync(httpClientMock, httpRequestMessage, retrys, timeout);

                    //Assert
                    stopwatchServiceMock
                    .Verify(
                        stopwatchService => stopwatchService.Stop(),
                        Times.Once
                    );
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsync_Timeout_Retry()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var retrys = 2;
                    var timeout = 0;

                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative)
                    };

                    var httpResponseMessage = new HttpResponseMessage()
                    {
                        StatusCode = System.Net.HttpStatusCode.BadRequest,
                    };

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ThrowsAsync(new OperationCanceledException());

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };

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

                    //  DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(new System.DateTime(2020, 1, 1));

                    //  Stopwatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Start());

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Stop());

                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(100);

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync(httpClientMock, httpRequestMessage, retrys, timeout);

                    //Assert
                    Assert.AreEqual(3, (int)propertiesObserved[ATTEMPTS]);
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsync_ResponseIsSuccessful_LogResponseRedacted()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var retrys = 0;
                    var timeout = 30.0;

                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative),
                        Content = new StringContent("{\"name\":\"John Doe\",\"age\":33}", Encoding.UTF8, "application/json")
                    };

                    var httpResponseMessage = new HttpResponseMessage()
                    {
                        StatusCode = System.Net.HttpStatusCode.OK,
                        Content = new StringContent(
@"{
  ""userId"": 0,
  ""id"": 0,
  ""title"": null,
  ""completed"": false
}"
                        , Encoding.UTF8, "application/json")
                    };

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponseMessage);

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };

                    var messagesObserved = new List<string>();
                    var propertiesObserved = new List<Dictionary<string, object>>();
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
                            messagesObserved.Add(message);
                            propertiesObserved.Add((Dictionary<string, object>)properties);
                        });

                    //  DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(new System.DateTime(2020, 1, 1));

                    //  Stopwatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Start());

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Stop());

                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(100);

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync(httpClientMock, httpRequestMessage, retrys, timeout);

                    //Assert
                    var index = messagesObserved.FindIndex(message => message == uutConcrete.DURABLE_REST_MESSAGE_RESPONSE);

                    loggingServiceMock.Verify
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            messagesObserved[index],
                            propertiesObserved[index]
                        )
                    );

                    Assert.AreEqual(1, messagesObserved.Where(message => message == uutConcrete.DURABLE_REST_MESSAGE_RESPONSE).Count());
                    Assert.AreEqual(uutConcrete.DURABLE_REST_MESSAGE_RESPONSE, messagesObserved[index]);
                    Assert.AreEqual(httpClientMock.BaseAddress, propertiesObserved[index][BASEURL].ToString());
                    Assert.AreEqual(await httpRequestMessage.Content.ReadAsStringAsync(), propertiesObserved[index][REQUEST_CONTENT]);
                    Assert.AreEqual(await httpResponseMessage.Content.ReadAsStringAsync(), propertiesObserved[index][RESPONSE_CONTENT]);
                    Assert.IsTrue((int)propertiesObserved[index][ELAPSED_MILLISECONDS] >= 0);
                    Assert.AreEqual(httpResponseMessage.StatusCode, propertiesObserved[index][STATUS_CODE]);

                    //HttpClient Mutates httpRequestMessage, Thus hard coding expected value 
                    Assert.AreEqual("/todos/", (string)propertiesObserved[index][RESOURCE]);

                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsync_ResponseIsNotSuccessful_LogResponseRedactedAsError()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var retrys = 0;
                    var timeout = 30.0;

                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative),
                        Content = new StringContent("{\"name\":\"John Doe\",\"age\":33}", Encoding.UTF8, "application/json")
                    };

                    var httpResponseMessage = new HttpResponseMessage()
                    {
                        StatusCode = System.Net.HttpStatusCode.InternalServerError,
                        Content = new StringContent(
@"{
  ""userId"": 0,
  ""id"": 0,
  ""title"": null,
  ""completed"": false
}"
                        , Encoding.UTF8, "application/json")
                    };

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponseMessage);

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };

                    var messagesObserved = new List<string>();
                    var propertiesObserved = new List<Dictionary<string, object>>();
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
                            messagesObserved.Add(message);
                            propertiesObserved.Add((Dictionary<string, object>)properties);
                        });

                    //  DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(new System.DateTime(2020, 1, 1));

                    //  Stopwatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Start());

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Stop());

                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(100);

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync(httpClientMock, httpRequestMessage, retrys, timeout);

                    //Assert
                    var index = messagesObserved.FindIndex(message => message == uutConcrete.DURABLE_REST_MESSAGE_RESPONSE);

                    loggingServiceMock.Verify
                    (
                        loggingService => loggingService.LogErrorRedacted
                        (
                            messagesObserved[index],
                            It.IsAny<Exception>(),
                            propertiesObserved[index]

                        )
                    );

                    Assert.AreEqual(1, messagesObserved.Where(message => message == uutConcrete.DURABLE_REST_MESSAGE_RESPONSE).Count());
                    Assert.AreEqual(uutConcrete.DURABLE_REST_MESSAGE_RESPONSE, messagesObserved[index]);
                    Assert.AreEqual(httpClientMock.BaseAddress, propertiesObserved[index][BASEURL].ToString());
                    Assert.AreEqual(await httpRequestMessage.Content.ReadAsStringAsync(), propertiesObserved[index][REQUEST_CONTENT]);
                    Assert.AreEqual(await httpResponseMessage.Content.ReadAsStringAsync(), propertiesObserved[index][RESPONSE_CONTENT]);
                    Assert.IsTrue((int)propertiesObserved[index][ELAPSED_MILLISECONDS] >= 0);
                    Assert.AreEqual(httpResponseMessage.StatusCode, propertiesObserved[index][STATUS_CODE]);

                    //HttpClient Mutates httpRequestMessage, Thus hard coding expected value 
                    Assert.AreEqual("/todos/", (string)propertiesObserved[index][RESOURCE]);

                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsync_FailedAndRetrys_AttemptsExpected()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var retrys = 2;
                    var timeout = 30.0;

                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative)
                    };

                    var httpResponseMessage = new HttpResponseMessage()
                    {
                        StatusCode = System.Net.HttpStatusCode.BadRequest,
                    };

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponseMessage);

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };

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

                    //  DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(new System.DateTime(2020, 1, 1));

                    //  Stopwatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Start());

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Stop());

                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(100);

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync(httpClientMock, httpRequestMessage, retrys, timeout);

                    //Assert
                    Assert.AreEqual(3, (int)propertiesObserved[ATTEMPTS]);
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsync_Runs_InsertTelemetry()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var retrys = 0;
                    var timeout = 30.0;

                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative),
                        Content = new StringContent("{\"name\":\"John Doe\",\"age\":33}", Encoding.UTF8, "application/json")
                    };

                    var httpResponseMessage = new HttpResponseMessage()
                    {

                        StatusCode = System.Net.HttpStatusCode.OK,
                        Content = new StringContent("{\"name\":\"Same Doe\",\"age\":35}", Encoding.UTF8, "application/json")
                    };

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponseMessage);

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };

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

                    //  DateTime
                    var dateTimeExpected = new System.DateTime(2020, 1, 1);
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(dateTimeExpected);

                    //  Stopwatch
                    var elapsedMillisecondsExpected = 100;
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(elapsedMillisecondsExpected);

                    //  Telemetry
                    var telemetryDataObserved = (TelemetryData)null;
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    telemetryServiceMock
                        .Setup(telemetryService => telemetryService.Insert(It.IsAny<TelemetryData>()))
                        .Callback<TelemetryData>((telemetryData) =>
                        {
                            telemetryDataObserved = telemetryData;
                        });

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync(httpClientMock, httpRequestMessage, retrys, timeout);

                    //Assert
                    telemetryServiceMock
                    .Verify(
                        telemetryService => telemetryService.Insert(It.IsAny<TelemetryData>()),
                        Times.Once
                    );

                    Assert.AreEqual(dateTimeExpected, telemetryDataObserved.DateTime);
                    Assert.AreEqual(elapsedMillisecondsExpected, telemetryDataObserved.ElapsedMilliseconds);
                    Assert.AreEqual($"{httpRequestMessage.Method} {httpRequestMessage.RequestUri}", telemetryDataObserved.Name);
                    Assert.AreEqual(TelemetryState.Successful, telemetryDataObserved.TelemetryState);
                    Assert.AreEqual(TelemetryType.Rest, telemetryDataObserved.TelemetryType);
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsync_RunsWithoutTelemtry_DoesNotInsertTelemetry()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var retrys = 0;
                    var timeout = 30.0;

                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative),
                        Content = new StringContent("{\"name\":\"John Doe\",\"age\":33}", Encoding.UTF8, "application/json")
                    };

                    var httpResponseMessage = new HttpResponseMessage()
                    {

                        StatusCode = System.Net.HttpStatusCode.OK,
                        Content = new StringContent("{\"name\":\"Same Doe\",\"age\":35}", Encoding.UTF8, "application/json")
                    };

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponseMessage);

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };

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

                    //  DateTime
                    var dateTimeExpected = new System.DateTime(2020, 1, 1);
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(dateTimeExpected);

                    //  Stopwatch
                    var elapsedMillisecondsExpected = 100;
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(elapsedMillisecondsExpected);

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync(httpClientMock, httpRequestMessage, retrys, timeout);

                    //Assert
                    //Nothing To Assert
                },
                serviceCollection => ConfigureServicesWithoutTelemtryService(serviceCollection)
            );
        }

        #endregion

        #region ExecuteAsyncOfT

        [TestMethod]
        public async Task ExecuteAsyncOfT_VaildInput_LogsRequestRedacted()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var retrys = 0;
                    var timeout = 30.0;

                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative),
                        Content = new StringContent("{\"name\":\"John Doe\",\"age\":33}", Encoding.UTF8, "application/json")
                    };

                    var httpResponseMessage = new HttpResponseMessage()
                    {
                        Content = new StringContent(
@"{
  ""userId"": 0,
  ""id"": 0,
  ""title"": null,
  ""completed"": false
}"
                        , Encoding.UTF8, "application/json")
                    };

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponseMessage);

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };

                    var messagesObserved = new List<string>();
                    var propertiesObserved = new List<Dictionary<string, object>>();
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
                            messagesObserved.Add(message);
                            propertiesObserved.Add((Dictionary<string, object>)properties);
                        });

                    //  DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(new System.DateTime(2020, 1, 1));

                    //  Stopwatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Start());

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Stop());

                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(100);

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync<DataClass>(httpClientMock, httpRequestMessage, retrys, timeout);


                    //Assert
                    var index = messagesObserved.FindIndex(message => message == uutConcrete.DURABLE_REST_MESSAGE_REQUEST);
                    loggingServiceMock.Verify
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            messagesObserved[index],
                            propertiesObserved[index]
                        )
                    );
                    Assert.AreEqual(1, messagesObserved.Where(message => message == uutConcrete.DURABLE_REST_MESSAGE_REQUEST).Count());
                    Assert.AreEqual(uutConcrete.DURABLE_REST_MESSAGE_REQUEST, messagesObserved[index]);
                    Assert.AreEqual(httpClientMock.BaseAddress, propertiesObserved[index][BASEURL].ToString());
                    Assert.AreEqual(await httpRequestMessage.Content.ReadAsStringAsync(), propertiesObserved[index][REQUEST_CONTENT]);

                    //HttpClient Mutates httpRequestMessage, Thus hard coding expected value 
                    Assert.AreEqual("todos/", (string)propertiesObserved[index][RESOURCE]);

                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }


        [TestMethod]
        public async Task ExecuteAsyncOfT_VaildInput_StopWatchStartCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var retrys = 0;
                    var timeout = 30.0;

                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative)
                    };

                    var httpResponseMessage = new HttpResponseMessage()
                    {
                        Content = new StringContent(
@"{
  ""userId"": 0,
  ""id"": 0,
  ""title"": null,
  ""completed"": false
}"
                        , Encoding.UTF8, "application/json")
                    };

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponseMessage);

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };
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

                    //  DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(new System.DateTime(2020, 1, 1));

                    //  Stopwatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Start());

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Stop());

                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(100);

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync<DataClass>(httpClientMock, httpRequestMessage, retrys, timeout);

                    //Assert
                    stopwatchServiceMock
                    .Verify(
                        stopwatchService => stopwatchService.Start(),
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
                    var retrys = 0;
                    var timeout = 30.0;

                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative)
                    };

                    var httpResponseMessage = new HttpResponseMessage()
                    {
                        Content = new StringContent(
@"{
  ""userId"": 0,
  ""id"": 0,
  ""title"": null,
  ""completed"": false
}"
                        , Encoding.UTF8, "application/json")
                    };

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponseMessage);

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };
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

                    //  DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(new System.DateTime(2020, 1, 1));

                    //  Stopwatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Start());

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Stop());

                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(100);

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync<DataClass>(httpClientMock, httpRequestMessage, retrys, timeout);

                    //Assert
                    httpMessageHandlerMock.Protected().Verify(
                       "SendAsync",
                       Times.Exactly(1),
                       ItExpr.Is<HttpRequestMessage>(req => req == httpRequestMessage),
                       ItExpr.IsAny<CancellationToken>());
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsyncOfT_VaildInput_StopWatchStopCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var retrys = 0;
                    var timeout = 30.0;

                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative)
                    };

                    var httpResponseMessage = new HttpResponseMessage()
                    {
                        Content = new StringContent(
@"{
  ""userId"": 0,
  ""id"": 0,
  ""title"": null,
  ""completed"": false
}"
                        , Encoding.UTF8, "application/json")
                    };

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponseMessage);

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };
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

                    //  DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(new System.DateTime(2020, 1, 1));

                    //  Stopwatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Start());

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Stop());

                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(100);

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync<DataClass>(httpClientMock, httpRequestMessage, retrys, timeout);

                    //Assert
                    stopwatchServiceMock
                    .Verify(
                        stopwatchService => stopwatchService.Stop(),
                        Times.Once
                    );
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsyncOfT_ResponseIsSuccessful_LogResponseRedacted()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var retrys = 0;
                    var timeout = 30.0;

                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative),
                        Content = new StringContent("{\"name\":\"John Doe\",\"age\":33}", Encoding.UTF8, "application/json")
                    };

                    var httpResponseMessage = new HttpResponseMessage()
                    {
                        StatusCode = System.Net.HttpStatusCode.OK,
                        Content = new StringContent(
@"{
  ""userId"": 0,
  ""id"": 0,
  ""title"": null,
  ""completed"": false
}"
                        , Encoding.UTF8, "application/json")
                    };

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponseMessage);

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };

                    var messagesObserved = new List<string>();
                    var propertiesObserved = new List<Dictionary<string, object>>();
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
                            messagesObserved.Add(message);
                            propertiesObserved.Add((Dictionary<string, object>)properties);
                        });

                    //  DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(new System.DateTime(2020, 1, 1));

                    //  Stopwatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Start());

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Stop());

                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(100);

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync<DataClass>(httpClientMock, httpRequestMessage, retrys, timeout);

                    //Assert
                    var index = messagesObserved.FindIndex(message => message == uutConcrete.DURABLE_REST_MESSAGE_RESPONSE);

                    loggingServiceMock.Verify
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            messagesObserved[index],
                            propertiesObserved[index]
                        )
                    );

                    Assert.AreEqual(1, messagesObserved.Where(message => message == uutConcrete.DURABLE_REST_MESSAGE_RESPONSE).Count());
                    Assert.AreEqual(uutConcrete.DURABLE_REST_MESSAGE_RESPONSE, messagesObserved[index]);
                    Assert.AreEqual(httpClientMock.BaseAddress, propertiesObserved[index][BASEURL].ToString());
                    Assert.AreEqual(await httpRequestMessage.Content.ReadAsStringAsync(), propertiesObserved[index][REQUEST_CONTENT]);
                    Assert.AreEqual(await httpResponseMessage.Content.ReadAsStringAsync(), propertiesObserved[index][RESPONSE_CONTENT]);
                    Assert.IsTrue((int)propertiesObserved[index][ELAPSED_MILLISECONDS] >= 0);
                    Assert.AreEqual(httpResponseMessage.StatusCode, propertiesObserved[index][STATUS_CODE]);

                    //HttpClient Mutates httpRequestMessage, Thus hard coding expected value 
                    Assert.AreEqual("/todos/", (string)propertiesObserved[index][RESOURCE]);

                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsyncOfT_ResponseIsNotSuccessful_LogResponseRedactedAsError()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var retrys = 0;
                    var timeout = 30.0;

                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative),
                        Content = new StringContent("{\"name\":\"John Doe\",\"age\":33}", Encoding.UTF8, "application/json")
                    };

                    var httpResponseMessage = new HttpResponseMessage()
                    {
                        StatusCode = System.Net.HttpStatusCode.InternalServerError,
                        Content = new StringContent(
@"{
  ""userId"": 0,
  ""id"": 0,
  ""title"": null,
  ""completed"": false
}"
                        , Encoding.UTF8, "application/json")
                    };

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponseMessage);

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };

                    var messagesObserved = new List<string>();
                    var propertiesObserved = new List<Dictionary<string, object>>();
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
                            messagesObserved.Add(message);
                            propertiesObserved.Add((Dictionary<string, object>)properties);
                        });

                    //  DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(new System.DateTime(2020, 1, 1));

                    //  Stopwatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Start());

                    stopwatchServiceMock
                    .Setup(stopwatchService => stopwatchService.Stop());

                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(100);

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync<DataClass>(httpClientMock, httpRequestMessage, retrys, timeout);

                    //Assert
                    var index = messagesObserved.FindIndex(message => message == uutConcrete.DURABLE_REST_MESSAGE_RESPONSE);

                    loggingServiceMock.Verify
                    (
                        loggingService => loggingService.LogErrorRedacted
                        (
                            messagesObserved[index],
                            It.IsAny<Exception>(),
                            propertiesObserved[index]

                        )
                    );

                    Assert.AreEqual(1, messagesObserved.Where(message => message == uutConcrete.DURABLE_REST_MESSAGE_RESPONSE).Count());
                    Assert.AreEqual(uutConcrete.DURABLE_REST_MESSAGE_RESPONSE, messagesObserved[index]);
                    Assert.AreEqual(httpClientMock.BaseAddress, propertiesObserved[index][BASEURL].ToString());
                    Assert.AreEqual(await httpRequestMessage.Content.ReadAsStringAsync(), propertiesObserved[index][REQUEST_CONTENT]);
                    Assert.AreEqual(await httpResponseMessage.Content.ReadAsStringAsync(), propertiesObserved[index][RESPONSE_CONTENT]);
                    Assert.IsTrue((int)propertiesObserved[index][ELAPSED_MILLISECONDS] >= 0);
                    Assert.AreEqual(httpResponseMessage.StatusCode, propertiesObserved[index][STATUS_CODE]);

                    //HttpClient Mutates httpRequestMessage, Thus hard coding expected value 
                    Assert.AreEqual("/todos/", (string)propertiesObserved[index][RESOURCE]);

                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }


        [TestMethod]
        public async Task ExecuteAsyncOfT_Runs_InsertTelemetry()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var retrys = 0;
                    var timeout = 30.0;

                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative),
                        Content = new StringContent("{\"name\":\"John Doe\",\"age\":33}", Encoding.UTF8, "application/json")
                    };
                    var httpResponseMessage = new HttpResponseMessage()
                    {
                        Content = new StringContent(
@"{
  ""userId"": 0,
  ""id"": 0,
  ""title"": null,
  ""completed"": false
}"
                        , Encoding.UTF8, "application/json")
                    };

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponseMessage);

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };

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

                    //  DateTime
                    var dateTimeExpected = new System.DateTime(2020, 1, 1);
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(dateTimeExpected);

                    //  Stopwatch
                    var elapsedMillisecondsExpected = 100;
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(elapsedMillisecondsExpected);

                    //  Telemetry
                    var telemetryDataObserved = (TelemetryData)null;
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    telemetryServiceMock
                        .Setup(telemetryService => telemetryService.Insert(It.IsAny<TelemetryData>()))
                        .Callback<TelemetryData>((telemetryData) =>
                        {
                            telemetryDataObserved = telemetryData;
                        });

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync<DataClass>(httpClientMock, httpRequestMessage, retrys, timeout);

                    //Assert
                    telemetryServiceMock
                    .Verify(
                        telemetryService => telemetryService.Insert(It.IsAny<TelemetryData>()),
                        Times.Once
                    );

                    Assert.AreEqual(dateTimeExpected, telemetryDataObserved.DateTime);
                    Assert.AreEqual(elapsedMillisecondsExpected, telemetryDataObserved.ElapsedMilliseconds);
                    Assert.AreEqual($"{httpRequestMessage.Method} {httpRequestMessage.RequestUri}", telemetryDataObserved.Name);
                    Assert.AreEqual(TelemetryState.Successful, telemetryDataObserved.TelemetryState);
                    Assert.AreEqual(TelemetryType.Rest, telemetryDataObserved.TelemetryType);
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task ExecuteAsyncOfT_Runs_ReturnsDataAsContentDeserialized()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup

                    //  Prams
                    var retrys = 0;
                    var timeout = 30.0;

                


                    //HTTP
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative),
                        Content = new StringContent("{\"name\":\"John Doe\",\"age\":33}", Encoding.UTF8, "application/json")
                    };
                    var httpResponseMessage = new HttpResponseMessage()
                    {
                        Content = new StringContent(
@"{
  ""userId"": 0,
  ""id"": 0,
  ""title"": null,
  ""completed"": false
}"
                        , Encoding.UTF8, "application/json")
                    };

                    var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

                    httpMessageHandlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponseMessage);

                    var httpClientMock = new HttpClient(httpMessageHandlerMock.Object)
                    {
                        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
                    };

                    var expectedResponse = new HttpResponse<DataClass>
                    {
                        HttpResponseMessage = httpResponseMessage,
                        Data = JsonSerializer.Deserialize<DataClass>(
                            await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false),
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true,
                            }
                        )
                    };

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

                    //  DateTime
                    var dateTimeExpected = new System.DateTime(2020, 1, 1);
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(dateTimeExpected);

                    //  Stopwatch
                    var elapsedMillisecondsExpected = 100;
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                        .Setup(stopwatchService => stopwatchService.ElapsedMilliseconds)
                        .Returns(elapsedMillisecondsExpected);

                    //  Telemetry
                    var telemetryDataObserved = (TelemetryData)null;
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    telemetryServiceMock
                        .Setup(telemetryService => telemetryService.Insert(It.IsAny<TelemetryData>()))
                        .Callback<TelemetryData>((telemetryData) =>
                        {
                            telemetryDataObserved = telemetryData;
                        });

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.ExecuteAsync<DataClass>(httpClientMock, httpRequestMessage, retrys, timeout);

                    //Assert
                    telemetryServiceMock
                    .Verify(
                        telemetryService => telemetryService.Insert(It.IsAny<TelemetryData>()),
                        Times.Once
                    );

                    Assert.AreEqual(observed.HttpResponseMessage, observed.HttpResponseMessage);
                    Assert.AreEqual(observed.Data, observed.Data);
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        #endregion

        #region InsertDurableRestResult

        [TestMethod]
        public async Task InsertDurableRestResult_StatusCodeIs2xx_InsertSuccessfulTelemetry()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //  DateTime
                    var dateTimeExpected = new System.DateTime(2020, 1, 1);
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(dateTimeExpected);

                    //  Telemetry
                    var nameExpected = "name";
                    var elapsedMillisecondsExpected = 100;
                    var statusCode = 200;

                    var telemetryDataObserved = (TelemetryData)null;
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    telemetryServiceMock
                        .Setup(telemetryService => telemetryService.Insert(It.IsAny<TelemetryData>()))
                        .Callback<TelemetryData>((telemetryData) =>
                        {
                            telemetryDataObserved = telemetryData;
                        });

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    uutConcrete.InsertDurableRestResult(nameExpected, statusCode, elapsedMillisecondsExpected);

                    //Assert
                    Assert.AreEqual(dateTimeExpected, telemetryDataObserved.DateTime);
                    Assert.AreEqual(elapsedMillisecondsExpected, telemetryDataObserved.ElapsedMilliseconds);
                    Assert.AreEqual(nameExpected, telemetryDataObserved.Name);
                    Assert.AreEqual(TelemetryState.Successful, telemetryDataObserved.TelemetryState);
                    Assert.AreEqual(TelemetryType.Rest, telemetryDataObserved.TelemetryType);

                    await Task.CompletedTask;
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task InsertDurableRestResult_StatusCodeIs4xx_InsertBadRequestTelemetry()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //  DateTime
                    var dateTimeExpected = new System.DateTime(2020, 1, 1);
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(dateTimeExpected);

                    //  Telemetry
                    var nameExpected = "name";
                    var elapsedMillisecondsExpected = 100;
                    var statusCode = 400;

                    var telemetryDataObserved = (TelemetryData)null;
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    telemetryServiceMock
                        .Setup(telemetryService => telemetryService.Insert(It.IsAny<TelemetryData>()))
                        .Callback<TelemetryData>((telemetryData) =>
                        {
                            telemetryDataObserved = telemetryData;
                        });

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    uutConcrete.InsertDurableRestResult(nameExpected, statusCode, elapsedMillisecondsExpected);

                    //Assert
                    Assert.AreEqual(dateTimeExpected, telemetryDataObserved.DateTime);
                    Assert.AreEqual(elapsedMillisecondsExpected, telemetryDataObserved.ElapsedMilliseconds);
                    Assert.AreEqual(nameExpected, telemetryDataObserved.Name);
                    Assert.AreEqual(TelemetryState.BadRequest, telemetryDataObserved.TelemetryState);
                    Assert.AreEqual(TelemetryType.Rest, telemetryDataObserved.TelemetryType);

                    await Task.CompletedTask;
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task InsertDurableRestResult_RequestIsNot2xxOr4xx_InsertFailedTelemetry()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //  DateTime
                    var dateTimeExpected = new System.DateTime(2020, 1, 1);
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                        .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                        .Returns(dateTimeExpected);

                    //  Telemetry
                    var nameExpected = "name";
                    var elapsedMillisecondsExpected = 100;
                    var statusCode = 500;

                    var telemetryDataObserved = (TelemetryData)null;
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    telemetryServiceMock
                        .Setup(telemetryService => telemetryService.Insert(It.IsAny<TelemetryData>()))
                        .Callback<TelemetryData>((telemetryData) =>
                        {
                            telemetryDataObserved = telemetryData;
                        });

                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    uutConcrete.InsertDurableRestResult(nameExpected, statusCode, elapsedMillisecondsExpected);

                    //Assert
                    Assert.AreEqual(dateTimeExpected, telemetryDataObserved.DateTime);
                    Assert.AreEqual(elapsedMillisecondsExpected, telemetryDataObserved.ElapsedMilliseconds);
                    Assert.AreEqual(nameExpected, telemetryDataObserved.Name);
                    Assert.AreEqual(TelemetryState.Failed, telemetryDataObserved.TelemetryState);
                    Assert.AreEqual(TelemetryType.Rest, telemetryDataObserved.TelemetryType);

                    await Task.CompletedTask;
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        #endregion

        #region CloneAsnyc

        [TestMethod]
        public async Task CloneAsync_HttpRequestMessage_ReturnsCloned()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    var httpRequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("todos/", UriKind.Relative),
                        Content = new StringContent(
@"{
  ""userId"": 0,
  ""id"": 0,
  ""title"": null,
  ""completed"": false
}"
                        , Encoding.UTF8, "application/json"),

                    };

                    httpRequestMessage.Headers.Add("1-Headers", "1Headers");
                    httpRequestMessage.Headers.Add("2-Headers", "2Headers");
                    httpRequestMessage.Properties.Add("1-Properties", "1Properties");
                    httpRequestMessage.Properties.Add("2-Properties", "2Properties");
                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.CloneAsync(httpRequestMessage).ConfigureAwait(false);
                    //Assert
                    Assert.AreEqual(await httpRequestMessage.Content.ReadAsStringAsync().ConfigureAwait(false), await observed.Content.ReadAsStringAsync().ConfigureAwait(false));
                    Assert.AreEqual(httpRequestMessage.Version, observed.Version);
                    Assert.AreEqual(httpRequestMessage.Properties.Count, observed.Properties.Count);
                    Assert.AreEqual(httpRequestMessage.Properties["1-Properties"], observed.Properties["1-Properties"]);
                    Assert.AreEqual(httpRequestMessage.Properties["2-Properties"], observed.Properties["2-Properties"]);
                    Assert.AreEqual(httpRequestMessage.Headers.Count(), observed.Headers.Count());
                    Assert.AreEqual(httpRequestMessage.Headers.First(e => e.Key == "1-Headers").Value.First(), observed.Headers.First(e => e.Key == "1-Headers").Value.First());
                    Assert.AreEqual(httpRequestMessage.Headers.First(e => e.Key == "2-Headers").Value.First(), observed.Headers.First(e => e.Key == "2-Headers").Value.First());

                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }

        [TestMethod]
        public async Task CloneAsync_HttpContent_ReturnsCloned()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    var content = new StringContent(
@"{
  ""userId"": 0,
  ""id"": 0,
  ""title"": null,
  ""completed"": false
}");
                    content.Headers.Add("1-Headers", "1Headers");
                    //  Durable Rest Service
                    var uut = serviceProvider.GetRequiredService<IDurableRestService>();
                    var uutConcrete = (DurableRestService)uut;

                    //Act
                    var observed = await uutConcrete.CloneAsync(content).ConfigureAwait(false);

                    //Assert
                    Assert.AreEqual(await content.ReadAsStringAsync().ConfigureAwait(false), await observed.ReadAsStringAsync().ConfigureAwait(false));
                    Assert.AreEqual(content.Headers.First(e=> e.Key == "1-Headers").Value.First(), observed.Headers.First(e => e.Key == "1-Headers").Value.First());
                },
                serviceCollection => ConfigureServices(serviceCollection)
            );
        }
        #endregion

        private IServiceCollection ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(Mock.Of<ICorrelationService>());
            serviceCollection.AddSingleton(Mock.Of<IGuidService>());
            serviceCollection.AddSingleton(Mock.Of<IDateTimeService>());
            serviceCollection.AddSingleton(Mock.Of<IStopwatchService>());
            serviceCollection.AddSingleton(Mock.Of<ITelemetryService>());
            serviceCollection.AddSingleton(Mock.Of<ILoggingService<DurableRestService>>());
            serviceCollection.AddSingleton<IDurableRestService, DurableRestService>();

            return serviceCollection;
        }

        private IServiceCollection ConfigureServicesWithoutTelemtryService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(Mock.Of<ICorrelationService>());
            serviceCollection.AddSingleton(Mock.Of<IGuidService>());
            serviceCollection.AddSingleton(Mock.Of<IDateTimeService>());
            serviceCollection.AddSingleton(Mock.Of<IStopwatchService>());
            serviceCollection.AddSingleton(Mock.Of<ILoggingService<DurableRestService>>());
            serviceCollection.AddSingleton<IDurableRestService, DurableRestService>();

            return serviceCollection;
        }
    }
}
