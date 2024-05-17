using Microsoft.AspNetCore.Mvc;
using ResumeAI.WebAPI.Models;
using ResumeAI.WebAPI.Services;

namespace ResumeAI.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class ResumeController: ControllerBase
{
    private ResumeImportService _resumeImportService;

    public ResumeController(ResumeImportService resumeImportService)
    {
        _resumeImportService = resumeImportService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ResumeDetailsDto>>> GetAll(CancellationToken token = default)
    {
        var list = await _resumeImportService.GetAllAsync(token);
        return Ok(list);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<ResumeDetailsDto>> GetById([FromRoute]Guid id, CancellationToken token = default)
    {
        var res = await _resumeImportService.GetByIdAsync(id, token);
        return res == null ? NotFound() : Ok(res);
    }
    
}