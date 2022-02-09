using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions.MultiTenancy;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowInstances
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-instances/bulk/retry")]
    [Route("{tenant}/v{apiVersion:apiVersion}/workflow-instances/bulk/retry")]
    [Produces("application/json")]
    public class BulkRetry : Controller
    {
        private readonly IWorkflowInstanceStore _store;
        private readonly IWorkflowReviver _reviver;
        private readonly ITenantProvider _tenantProvider;

        public BulkRetry(IWorkflowInstanceStore store, IWorkflowReviver reviver, ITenantProvider tenantProvider)
        {
            _store = store;
            _reviver = reviver;
            _tenantProvider = tenantProvider;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [SwaggerOperation(
            Summary = "Retries a faulted workflow instance.",
            Description = "Retries a workflow instance.",
            OperationId = "WorkflowInstances.BulkRetry",
            Tags = new[] { "WorkflowInstances" })
        ]
        public async Task<IActionResult> Handle(BulkRetryWorkflowsRequest request, CancellationToken cancellationToken = default)
        {
            var workflowInstances = (await _store.FindManyAsync(new WorkflowInstanceIdsSpecification(request.WorkflowInstanceIds), cancellationToken: cancellationToken)).ToList();
            var faultedWorkflowInstances = workflowInstances.Where(x => x.WorkflowStatus == WorkflowStatus.Faulted).ToList();

            var tenant = _tenantProvider.GetCurrentTenant();

            foreach (var workflowInstance in faultedWorkflowInstances) 
                await _reviver.ReviveAndQueueAsync(workflowInstance, tenant, cancellationToken);

            return Ok(new
            {
                ScheduledWorkflowCount = faultedWorkflowInstances.Count
            });
        }
    }
}