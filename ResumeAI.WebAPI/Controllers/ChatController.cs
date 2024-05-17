using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ResumeAI.WebAPI.Models;
using ResumeAI.WebAPI.Services;

namespace ResumeAI.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class ChatController : ControllerBase
{
   private readonly ChatService _chatService;

   public ChatController(ChatService chatService)
   {
      _chatService = chatService;
   }
   
   [HttpPost("{chatId}")]
   public async Task<ActionResult<ChatResponseDto>> Prompt([FromRoute]Guid chatId, [FromBody]ChatRequestDto request, CancellationToken token = default)
   {
      var res = await _chatService.GenerateResponseAsync(chatId, request.Prompt, token);
      return Ok(new ChatResponseDto() { ResponseText = res });
   }
}