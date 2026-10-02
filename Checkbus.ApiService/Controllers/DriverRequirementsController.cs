using Checkbus.ApiService.Application.DriverRequirements.Commands;
using Checkbus.ApiService.Application.DriverRequirements.Queries;
using Checkbus.ApiService.Application.Interfaces.Storage;
using Checkbus.ApiService.Contracts;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DriverRequirementsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DriverRequirementsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize]
        [HttpGet("{targetUserId:guid}")]
        public async Task<IActionResult> GetRequirements(Guid targetUserId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetDriverRequirementsQuery { TargetUserId = targetUserId }, cancellationToken);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("{targetUserId:guid}/{type}/document")]
        public async Task<IActionResult> UploadDocument(
            Guid targetUserId,
            DriverRequirementType type,
            [FromForm] UploadDriverRequirementDocumentRequest request,
            CancellationToken cancellationToken)
        {
            await using var fileStream = request.File.OpenReadStream();

            var command = new UploadDriverRequirementDocumentCommand
            {
                TargetUserId = targetUserId,
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

        [Authorize]
        [HttpPut("{targetUserId:guid}/{type}/status")]
        public async Task<IActionResult> ValidateDocument(
            Guid targetUserId,
            DriverRequirementType type,
            [FromBody] ValidateDriverRequirementRequest request,
            CancellationToken cancellationToken)
        {
            var command = new ValidateDriverRequirementCommand
            {
                TargetUserId = targetUserId,
                Type = type,
                Approved = request.Approved
            };

            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [Authorize]
        [HttpGet("{targetUserId:guid}/{type}/document")]
        public async Task<IActionResult> DownloadDocument(
            Guid targetUserId,
            DriverRequirementType type,
            [FromServices] IFileStorageService fileStorageService,
            CancellationToken cancellationToken)
        {
            var file = await _mediator.Send(
                new GetDriverRequirementDocumentQuery { TargetUserId = targetUserId, Type = type },
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
            var count = await _mediator.Send(new GetExpiringDriverRequirementsCountQuery(), cancellationToken);
            return Ok(count);
        }
    }
}
