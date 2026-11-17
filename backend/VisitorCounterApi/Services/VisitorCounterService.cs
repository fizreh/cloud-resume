using System.Threading;
using System.Threading.Tasks;
using VisitorCounterApi.Data;

namespace VisitorCounterApi.Services
{
    // Today this is thin. It is the place for future business rules
    // (for example, ignoring bots) without touching the function or the database code.
    public sealed class VisitorCounterService : IVisitorCounterService
    {
        private readonly IVisitorCounterRepository _repository;

        public VisitorCounterService(IVisitorCounterRepository repository)
        {
            _repository = repository;
        }

        public Task<int> RegisterVisitAsync(CancellationToken cancellationToken = default)
            => _repository.IncrementAndGetAsync(cancellationToken);
    }
}