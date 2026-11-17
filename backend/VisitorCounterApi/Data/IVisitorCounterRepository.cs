using System.Threading;
using System.Threading.Tasks;

namespace VisitorCounterApi.Data
{
    public interface IVisitorCounterRepository
    {
        Task<int> IncrementAndGetAsync(CancellationToken cancellationToken = default);
    }
}