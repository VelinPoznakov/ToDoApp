using Microsoft.AspNetCore.Mvc;
using TodoApp.Models.SupportMessage;
using TodoApp.Services.Core.Contracts;
using TodoApp.Services.Dtos;

using static TodoApp.GCommon.ApplicationConstants;

namespace TodoApp.Areas.TodoMainApp.Controllers;

public class SupportMessageController: BaseController
{
    private readonly ISupportService _supportService;
    private readonly ILogger<SupportMessageController> _logger;

    public SupportMessageController(ISupportService supportService, ILogger<SupportMessageController> logger)
    {
        _supportService = supportService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        IEnumerable<AllSupportMessagesDto> supportMessagesDtos = await _supportService
            .GetAllSupportMessages();

        IEnumerable<AllSupportMessagesViewModel> result = supportMessagesDtos.Select(s =>
            new AllSupportMessagesViewModel()
            {
                

            });

    }
}