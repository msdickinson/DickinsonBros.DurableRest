# DickinsonBros.DurableRest

Handles requests in a durable fashion

Features

* Ability to retry requests
* Timeouts
* Logs all requests redacted with meta data
* Telemetry for all calls


<a href="https://dev.azure.com/marksamdickinson/DickinsonBros/_build?definitionScope=%5CDickinsonBros.DurableRest">Builds</a>

<h2>Example Usage</h2>

```C#

var durableRestService = provider.GetRequiredService<IDurableRestService>();

{
    var restRequest = new RestRequest();
    restRequest.Method = Method.GET;
    restRequest.Resource = "todos/1";
    var baseURL = "https://jsonplaceholder.typicode.com/";
    var retrys = 3;

    var restResponse = await durableRestService.ExecuteAsync(restRequest, baseURL, retrys).ConfigureAwait(false);
}


{
    var restRequest = new RestRequest();
    restRequest.Method = Method.GET;
    restRequest.Resource = "todos/1";
    var baseURL = "https://jsonplaceholder.typicode.com/";
    var retrys = 3;

    var restResponse = await durableRestService.ExecuteAsync<Todo>(restRequest, baseURL, retrys).ConfigureAwait(false);
    Console.WriteLine("Content: " + restResponse.Content);
}

Console.WriteLine("Flush Telemetry");
await telemetryService.Flush().ConfigureAwait(false);
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

![Alt text](https://raw.githubusercontent.com/msdickinson/DickinsonBros.SQL/develop/elemetryRestSample.PNG)


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
```

