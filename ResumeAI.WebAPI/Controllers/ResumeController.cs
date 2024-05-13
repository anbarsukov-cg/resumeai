using Microsoft.AspNetCore.Mvc;
using ResumeAI.WebAPI.Models;
using ResumeAI.WebAPI.Services;

namespace ResumeAI.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class ResumeController: ControllerBase
{
    private ResumeService _resumeService;

    public ResumeController(ResumeService resumeService)
    {
        _resumeService = resumeService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ResumeDetailsDto>>> GetAll(CancellationToken token = default)
    {
        var list = await _resumeService.GetAllAsync(token);
        return Ok(list);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<ResumeDetailsDto>> GetById([FromRoute]Guid id, CancellationToken token = default)
    {
        var res = await _resumeService.GetByIdAsync(id, token);
        return res == null ? NotFound() : Ok(res);
    }
    
}