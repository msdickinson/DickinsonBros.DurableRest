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
      
<b>Telemetry</b>

![Alt text](https://raw.githubusercontent.com/msdickinson/DickinsonBros.DurableRest/develop/TelemetryRestSample.PNG)

[Sample Runner](https://github.com/msdickinson/DickinsonBros.DurableRest/tree/master/DickinsonBros.DurableRest.Runner)
