using System.Threading;
using System.Threading.Tasks;

namespace VisitorCounterApi.Services
{
    public interface IVisitorCounterService
    {
        Task<int> RegisterVisitAsync(CancellationToken cancellationToken = default);
    }
}