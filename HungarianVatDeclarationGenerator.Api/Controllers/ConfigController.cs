using HungarianVatDeclarationGenerator.Api.Configuration;
using HungarianVatDeclarationGenerator.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HungarianVatDeclarationGenerator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class ConfigController(
    FileUploadSettings fileUploadSettings
) : ControllerBase
{
    private readonly FileUploadSettings _fileUploadSettings = fileUploadSettings
        ?? throw new ArgumentNullException(nameof(fileUploadSettings));

    /// <summary>
    /// Get client configuration settings.
    /// This endpoint does not require authentication as it only exposes non-sensitive upload limits.
    /// </summary>
    /// <returns>Configuration settings for the frontend client</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ClientConfig), StatusCodes.Status200OK)]
    public IActionResult GetConfig()
    {
        ClientConfig config = new()
        {
            MaxFileSizeBytes = _fileUploadSettings.MaxFileSizeBytes,
            AllowedExtensions = _fileUploadSettings.AllowedExtensions,
            RequestTimeoutSeconds = _fileUploadSettings.ProcessingTimeoutSeconds
        };

        return Ok(config);
    }
}
