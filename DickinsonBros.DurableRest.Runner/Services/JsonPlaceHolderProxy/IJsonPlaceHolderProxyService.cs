using DickinsonBros.DurableRest.Abstractions.Models;
using DickinsonBros.DurableRest.Runner.Models.Models;
using DickinsonBros.DurableRest.Runner.Services.JsonPlaceHolderProxy.Models;
using System.Threading.Tasks;

namespace DickinsonBros.DurableRest.Runner.Services.JsonPlaceHolderProxy
{
    public interface IJsonPlaceHolderProxyService
    {
        Task<HttpResponse<Todo>> GetTodosAsync(GetTodosRequest getTodosRequest);
    }
}