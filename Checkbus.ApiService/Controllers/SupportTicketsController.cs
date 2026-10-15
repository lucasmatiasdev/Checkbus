using Checkbus.ApiService.Application.Interfaces.Storage;
using Checkbus.ApiService.Application.SupportTickets.Commands;
using Checkbus.ApiService.Application.SupportTickets.Queries;
using Checkbus.ApiService.Contracts;
using Checkbus.ApiService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Contracts
{
    // Colocated here rather than its own Contracts file, same convention
    // UploadEventImageRequest uses on EventsController.cs — this task's allowed edit surface
    // only authorizes touching SupportTicketsController.cs, not a new Contracts file.
    public sealed class CreateSupportTicketRequest
    {
        public required SupportTicketCategory Category { get; set; }
        public required string Subject { get; set; }
        public required string Description { get; set; }
        public required SupportTicketPriority Priority { get; set; }

        // Unlike UploadEventImageRequest.File, the attachment here is optional — a ticket may
        // have none at all.
        public IFormFile? Attachment { get; set; }
    }
}

namespace Checkbus.ApiService.Controllers
{
    // Organization-side only (odd/tasks/soporte-tickets.md) — every action requires
    // Administrador or Planificador, same raw-string [Authorize(Roles = "...")] convention as
    // EventsController. The future internal Checkbus staff panel (cross-tenant ticket
    // management) is explicitly out of scope and does not exist on this controller.
    [Route("api/[controller]")]
    [ApiController]
    public class SupportTicketsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SupportTicketsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = "Administrador,Planificador")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateSupportTicketRequest request, CancellationToken cancellationToken)
        {
            Stream? attachmentStream = null;

            try
            {
                if (request.Attachment is not null)
                {
                    attachmentStream = request.Attachment.OpenReadStream();
                }

                var command = new CreateSupportTicketCommand
                {
                    Category = request.Category,
                    Subject = request.Subject,
                    Description = request.Description,
                    Priority = request.Priority,
                    AttachmentStream = attachmentStream,
                    AttachmentContentType = request.Attachment?.ContentType,
                    AttachmentFileName = request.Attachment?.FileName
                };

                var result = await _mediator.Send(command, cancellationToken);
                return StatusCode(StatusCodes.Status201Created, result);
            }
            finally
            {
                if (attachmentStream is not null)
                {
                    await attachmentStream.DisposeAsync();
                }
            }
        }

        [Authorize(Roles = "Administrador,Planificador")]
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMySupportTicketsQuery(), cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = "Administrador,Planificador")]
        [HttpGet("{id:guid}/attachment")]
        public async Task<IActionResult> GetAttachment(
            Guid id,
            [FromServices] IFileStorageService fileStorageService,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSupportTicketAttachmentQuery { TicketId = id }, cancellationToken);
            if (result is null)
                return NotFound();

            var stream = await fileStorageService.OpenReadAsync(result.AttachmentKey, cancellationToken);
            if (stream is null)
                return NotFound();

            return File(stream, result.AttachmentContentType ?? "application/octet-stream", result.AttachmentFileName);
        }
    }
}
