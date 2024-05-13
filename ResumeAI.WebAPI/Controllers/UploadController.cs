
using Microsoft.AspNetCore.Mvc;
using ResumeAI.WebAPI.Models;
using ResumeAI.WebAPI.Services;

namespace ResumeAI.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class UploadController : ControllerBase
{
    private ResumeService _resumeService;

    public UploadController(ResumeService resumeService)
    {
        _resumeService = resumeService;
    }

    [HttpPost]
    public async Task<ActionResult<ResumeDetailsDto>> UploadFile([FromForm] IEnumerable<IFormFile> files)
    {
        var file = files.First();
        var imported = await _resumeService.ImportResumeFromStream(file.OpenReadStream());
        
        return Created("", imported);
    }
}