using Checkbus.ApiService.Application.VehicleDocuments.Commands;
using Checkbus.ApiService.Application.VehicleDocuments.Queries;
using Checkbus.ApiService.Application.Interfaces.Storage;
using Checkbus.ApiService.Contracts;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Contracts
{
    // Colocated here rather than its own Contracts file: this task's allowed edit surface
    // only authorizes a new UploadVehicleDocumentRequest.cs file alongside
    // VehicleDocumentsController.cs. Same shape as ValidateDriverRequirementRequest.
    public sealed record ValidateVehicleDocumentRequest(bool Approved);
}

namespace Checkbus.ApiService.Controllers
{
    // Every action here is Administrador-only — unlike DriverRequirementsController, there
    // is no Chofer self-service branch at all for vehicle documents (odd/tasks/vehiculos.md
    // Constraint 4).
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleDocumentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public VehicleDocumentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = nameof(Role.Administrador))]
        [HttpGet("{vehicleId:guid}")]
        public async Task<IActionResult> GetDocuments(Guid vehicleId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetVehicleDocumentsQuery { VehicleId = vehicleId }, cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = nameof(Role.Administrador))]
        [HttpPost("{vehicleId:guid}/{type}/document")]
        public async Task<IActionResult> UploadDocument(
            Guid vehicleId,
            VehicleDocumentType type,
            [FromForm] UploadVehicleDocumentRequest request,
            CancellationToken cancellationToken)
        {
            await using var fileStream = request.File.OpenReadStream();

            var command = new UploadVehicleDocumentCommand
            {
                VehicleId = vehicleId,
                Type = type,
                FileStream = fileStream,
                ContentType = request.File.ContentType,
                FileSizeBytes = request.File.Length,
                OriginalFileName = request.File.FileName,
                ExpirationDate = request.ExpirationDate,
                IssueDate = request.IssueDate
            };

            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = nameof(Role.Administrador))]
        [HttpPut("{vehicleId:guid}/{type}/status")]
        public async Task<IActionResult> ValidateDocument(
            Guid vehicleId,
            VehicleDocumentType type,
            [FromBody] ValidateVehicleDocumentRequest request,
            CancellationToken cancellationToken)
        {
            var command = new ValidateVehicleDocumentCommand
            {
                VehicleId = vehicleId,
                Type = type,
                Approved = request.Approved
            };

            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = nameof(Role.Administrador))]
        [HttpGet("{vehicleId:guid}/{type}/document")]
        public async Task<IActionResult> DownloadDocument(
            Guid vehicleId,
            VehicleDocumentType type,
            [FromServices] IFileStorageService fileStorageService,
            CancellationToken cancellationToken)
        {
            var file = await _mediator.Send(
                new GetVehicleDocumentQuery { VehicleId = vehicleId, Type = type },
                cancellationToken);

            if (file.FileKey is null)
                return NotFound();

            var stream = await fileStorageService.OpenReadAsync(file.FileKey, cancellationToken);
            if (stream is null)
                return NotFound();

            return File(stream, file.FileContentType ?? "application/octet-stream");
        }

        [Authorize(Roles = nameof(Role.Administrador))]
        [HttpGet("expiring-count")]
        public async Task<IActionResult> GetExpiringCount(CancellationToken cancellationToken)
        {
            var count = await _mediator.Send(new GetExpiringVehicleDocumentsCountQuery(), cancellationToken);
            return Ok(count);
        }
    }
}
