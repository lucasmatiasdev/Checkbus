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

    public sealed class CreateSupportTicketRequest
    {
        public required SupportTicketCategory Category { get; set; }
        public required string Subject { get; set; }
        public required string Description { get; set; }

        public IFormFile? Attachment { get; set; }
    }
}

namespace Checkbus.ApiService.Controllers
{

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
        [HttpGet("check-duplicate")]
        public async Task<IActionResult> CheckDuplicate(
            [FromQuery] SupportTicketCategory category, CancellationToken cancellationToken)
        {
            var hasPossibleDuplicate = await _mediator.Send(
                new CheckDuplicateSupportTicketQuery { Category = category }, cancellationToken);
            return Ok(hasPossibleDuplicate);
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
