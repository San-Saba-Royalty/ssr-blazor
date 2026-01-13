using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using SSRBusiness.Interfaces;
using Microsoft.Extensions.Logging;

namespace SSRBlazor.Controllers;

[Route("download")]
[ApiController]
public class DownloadController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly ILogger<DownloadController> _logger;

    public DownloadController(IFileService fileService, ILogger<DownloadController> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    [HttpGet("{fileId}")]
    public IActionResult Download(string fileId)
    {
        try
        {
            var (filePath, contentType, fileName) = _fileService.GetTempFile(fileId);
            
            // Open with FileShare.Read to allow concurrent reads if necessary
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(fileStream, contentType, fileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound("File not found or expired.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId}", fileId);
            return StatusCode(500, "Internal server error.");
        }
    }
}
