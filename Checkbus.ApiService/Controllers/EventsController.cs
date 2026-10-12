using Checkbus.ApiService.Application.Events.Commands;
using Checkbus.ApiService.Application.Events.Queries;
using Checkbus.ApiService.Application.Interfaces.Storage;
using Checkbus.ApiService.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Contracts
{
    // Colocated here rather than its own Contracts file, same convention
    // UploadVehicleDocumentRequest/ValidateVehicleDocumentRequest already use on
    // VehicleDocumentsController.cs — this task's allowed edit surface only authorizes touching
    // EventsController.cs, not a new Contracts file.
    public sealed class UploadEventImageRequest
    {
        public required IFormFile File { get; set; }
    }
}

namespace Checkbus.ApiService.Controllers
{
    // Event is a shared catalog (no OrganizationId) — every action still requires
    // Planificador or Administrador, matching every other controller's raw-string
    // [Authorize(Roles = "...")] convention (odd/tasks/rutas-publicacion.md Constraint 6).
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EventsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = "Planificador,Administrador")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEventCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }

        [Authorize(Roles = "Planificador,Administrador")]
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetEventsQuery(), cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = "Planificador,Administrador")]
        [HttpPost("{id:guid}/image")]
        public async Task<IActionResult> UploadImage(
            Guid id,
            [FromForm] UploadEventImageRequest request,
            CancellationToken cancellationToken)
        {
            await using var fileStream = request.File.OpenReadStream();

            var command = new UploadEventImageCommand
            {
                EventId = id,
                FileStream = fileStream,
                ContentType = request.File.ContentType,
                FileSizeBytes = request.File.Length,
                OriginalFileName = request.File.FileName
            };

            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = "Planificador,Administrador")]
        [HttpGet("{id:guid}/image")]
        public async Task<IActionResult> DownloadImage(
            Guid id,
            [FromServices] IFileStorageService fileStorageService,
            CancellationToken cancellationToken)
        {
            var file = await _mediator.Send(new GetEventImageQuery { EventId = id }, cancellationToken);
            if (file.ImageKey is null)
                return NotFound();

            var stream = await fileStorageService.OpenReadAsync(file.ImageKey, cancellationToken);
            if (stream is null)
                return NotFound();

            return File(stream, file.ImageContentType ?? "application/octet-stream");
        }
    }
}
