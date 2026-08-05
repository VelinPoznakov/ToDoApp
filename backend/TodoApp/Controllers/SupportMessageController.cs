using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApp.GCommon.Exceptions;
using TodoApp.Services.Core.Contracts;
using TodoApp.Services.Dtos;
using TodoApp.Web.ViewModels.SupportMessage;

using static TodoApp.GCommon.ErrorMessages;

namespace TodoApp.Controllers;

[AutoValidateAntiforgeryToken]
public class SupportMessageController: Controller
{
    private readonly ISupportService _supportService;
    private readonly ILogger<SupportMessageController> _logger;

    public SupportMessageController(ISupportService supportService, ILogger<SupportMessageController> logger)
    {
        _supportService = supportService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public IActionResult CreateSupportMessage()
    {
        return View(new CreateSupportMessageViewModel());
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateSupportMessage([FromForm] CreateSupportMessageViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        
        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        CreateSupportMessageDto dto = new CreateSupportMessageDto()
        {
            Title = model.Title,
            Description = model.Description
        };

        try
        {
            await _supportService.CreateSupportMessage(userId, dto);
            
            return RedirectToAction(nameof(Index), "Dashboard", new { area = "TodoMainApp" });
        }
        catch (DataPersistFail e)
        {
            ModelState.AddModelError(string.Empty, DataPersistFailErrorMessage);
            _logger.LogError(e, string.Format(DataPersistFailErrorMessage, nameof(CreateSupportMessage)));

            return View(model);
        }
        catch (Exception e)
        {
            _logger.LogError(e, UnexpectedErrorMessage);
            ModelState.AddModelError(string.Empty, UnexpectedErrorMessage);

            return View(model);
        }
    }
}