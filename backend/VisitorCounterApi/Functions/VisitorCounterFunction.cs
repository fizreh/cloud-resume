using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using VisitorCounterApi.Services;

namespace VisitorCounterApi.Functions
{
    // Only responsibility: receive the HTTP request and return an HTTP response.
    public class VisitorCounterFunction
    {
        private readonly IVisitorCounterService _service;
        private readonly ILogger<VisitorCounterFunction> _logger;

        public VisitorCounterFunction(IVisitorCounterService service, ILogger<VisitorCounterFunction> logger)
        {
            _service = service;
            _logger = logger;
        }

        [Function("VisitorCounter")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "visitor")] HttpRequest req,
            CancellationToken cancellationToken)
        {
            try
            {
                var count = await _service.RegisterVisitAsync(cancellationToken);
                return new OkObjectResult(new { count });
            }
            catch (Exception ex)
            {
                // Details go to the log only. Visitors get a generic message.
                _logger.LogError(ex, "Could not update the visitor count.");
                return ServerError();
            }
        }

        private static ObjectResult ServerError()
        {
            return new(new { error = "Could not update visitor count" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}