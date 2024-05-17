
using Microsoft.AspNetCore.Mvc;
using ResumeAI.WebAPI.Models;
using ResumeAI.WebAPI.Services;

namespace ResumeAI.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class UploadController : ControllerBase
{
    private ResumeImportService _resumeImportService;

    public UploadController(ResumeImportService resumeImportService)
    {
        _resumeImportService = resumeImportService;
    }

    [HttpPost]
    public async Task<ActionResult<ResumeDetailsDto>> UploadFile([FromForm] IEnumerable<IFormFile> files)
    {
        var file = files.First();
        var imported = await _resumeImportService.ImportResumeFromStream(file.OpenReadStream());
        
        return Created("", imported);
    }
}