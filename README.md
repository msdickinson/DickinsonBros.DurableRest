# DickinsonBros.DurableRest
<a href="https://dev.azure.com/marksamdickinson/dickinsonbros/_build/latest?definitionId=33&amp;branchName=master"> <img alt="Azure DevOps builds (branch)" src="https://img.shields.io/azure-devops/build/marksamdickinson/DickinsonBros/33/master"> </a> <a href="https://dev.azure.com/marksamdickinson/dickinsonbros/_build/latest?definitionId=33&amp;branchName=master"> <img alt="Azure DevOps coverage (branch)" src="https://img.shields.io/azure-devops/coverage/marksamdickinson/dickinsonbros/33/master"> </a><a href="https://dev.azure.com/marksamdickinson/DickinsonBros/_release?_a=releases&view=mine&definitionId=16"> <img alt="Azure DevOps releases" src="https://img.shields.io/azure-devops/release/marksamdickinson/b5a46403-83bb-4d18-987f-81b0483ef43e/16/17"> </a><a href="https://www.nuget.org/packages/DickinsonBros.DurableRest/"><img src="https://img.shields.io/nuget/v/DickinsonBros.DurableRest"></a>

Handles requests in a durable fashion

Features

* Ability to retry requests
* Timeouts
* Logs all requests redacted with meta data
* Telemetry for all calls

<h2>Example Usage</h2>

```C#
{
    using var httpClient = new HttpClient
    {
        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
    };

    var httpRequestMessage = new HttpRequestMessage
    {
        Method = HttpMethod.Get,
        RequestUri = new Uri("todos/1", UriKind.Relative)
    };

    var retrys = 3;
    var timeoutInSeconds = 30;
    var restResponse = await durableRestService.ExecuteAsync(httpClient, httpRequestMessage, retrys, timeoutInSeconds).ConfigureAwait(false);
}

{
    using var httpClient = new HttpClient
    {
        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
    };

    var httpRequestMessage = new HttpRequestMessage
    {
        Method = HttpMethod.Get,
        RequestUri = new Uri("todos/1", UriKind.Relative)
    };

    var retrys = 3;
    var timeoutInSeconds = 30;

    var restResponse = await durableRestService.ExecuteAsync<Todo>(httpClient, httpRequestMessage, retrys, timeoutInSeconds).ConfigureAwait(false);

    Console.WriteLine("Content: " + await restResponse.HttpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false));
}

Console.WriteLine("Flush Telemetry");
await telemetryService.FlushAsync().ConfigureAwait(false);
```
    info: DickinsonBros.DurableRest.DurableRestService[1]
          DurableRest
          Attempts: 1
          BaseUrl: "https://jsonplaceholder.typicode.com/"
          Resource: todos/1
          Body:
          Content: {
            "userId": 1,
            "id": 1,
            "title": "delectus aut autem",
            "completed": false
          }
          ElapsedMilliseconds: 1104
          StatusCode: 200

    Content: {
      "userId": 1,
      "id": 1,
      "title": "delectus aut autem",
      "completed": false
    }
    Flush Telemetry
    info: DickinsonBros.DurableRest.DurableRestService[1]
          DurableRest
          Attempts: 1
          BaseUrl: "https://jsonplaceholder.typicode.com/"
          Resource: todos/1
          Body:
          Content: {
            "userId": 1,
            "id": 1,
            "title": "delectus aut autem",
            "completed": false
          }
          ElapsedMilliseconds: 153
          StatusCode: 200
      
Note: Logs can be redacted via configuration (see https://github.com/msdickinson/DickinsonBros.Redactor)

Telemetry generated when using DickinsonBros.Telemetry and connecting it to a configured database for ITelemetry 
See https://github.com/msdickinson/DickinsonBros.Telemetry on how to configure DickinsonBros.Telemetry and setup the database.

![Alt text](https://raw.githubusercontent.com/msdickinson/DickinsonBros.DurableRest/develop/TelemetryRestSample.PNG)


<h2>Setup</h2>

<h3>Add nuget references</h3>

    https://www.nuget.org/packages/DickinsonBros.DateTime
    https://www.nuget.org/packages/DickinsonBros.Stopwatch
    https://www.nuget.org/packages/DickinsonBros.Telemetry
    https://www.nuget.org/packages/DickinsonBros.Logger
    https://www.nuget.org/packages/DickinsonBros.Redactor

<h3>Create instance with dependency injection</h3>

<h4>Add appsettings.json File With Contents</h4>

Note: Runner Shows this with added steps to enypct Connection String

 ```json  
{
  "RedactorServiceOptions": {
    "PropertiesToRedact": [
      "Password"
    ],
    "RegexValuesToRedact": []
  },
  "TelemetryServiceOptions": {
    "ConnectionString": ""
  }
}
 ```    
<h4>Code</h4>

```c#

//ApplicationLifetime
using var applicationLifetime = new ApplicationLifetime();

//ServiceCollection
var serviceCollection = new ServiceCollection();

//Configure Options
var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", false)

var configuration = builder.Build();
serviceCollection.AddOptions();

services.AddSingleton<IApplicationLifetime>(applicationLifetime);

//Add DateTime Service
services.AddDateTimeService();

//Add Stopwatch Service
services.AddStopwatchService();

//Add Logging Service
services.AddLoggingService();

//Add Redactor
services.AddRedactorService();
services.Configure<RedactorServiceOptions>(_configuration.GetSection(nameof(RedactorServiceOptions)));

//Add Telemetry
services.AddTelemetryService();
services.Configure<TelemetryServiceOptions>(_configuration.GetSection(nameof(TelemetryServiceOptions)));

//Add SQLService
services.AddDurableRestService();

//Build Service Provider 
using (var provider = services.BuildServiceProvider())
{
  var sqlService = provider.GetRequiredService<IDurableRestService>();
  var telemetryService = provider.GetRequiredService<ITelemetryService>();
}

//Example above adds a proxy see example project
```

