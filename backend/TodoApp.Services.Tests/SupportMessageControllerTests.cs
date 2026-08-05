using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TodoApp.Controllers;
using TodoApp.GCommon.Exceptions;
using TodoApp.Services.Core.Contracts;
using TodoApp.Services.Dtos;
using TodoApp.Web.ViewModels.SupportMessage;

namespace TodoApp.Services.Tests;

// Controller tests mock ISupportService and focus on model handling,
// the user id taken from the claims, and the redirect / error paths.
public class SupportMessageControllerTests
{
    private readonly Mock<ISupportService> _service = new();
    private readonly Guid _userId = Guid.NewGuid();

    private SupportMessageController CreateController()
    {
        var controller = new SupportMessageController(
            _service.Object,
            Mock.Of<ILogger<SupportMessageController>>());

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, _userId.ToString()) },
            "TestAuth");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        return controller;
    }

    private static CreateSupportMessageViewModel ValidModel() => new CreateSupportMessageViewModel
    {
        Title = "Cannot delete a group",
        Description = "When I press delete nothing happens and the page reloads."
    };

    // ===== GET =====

    [Fact]
    public void CreateGet_ReturnsViewWithEmptyModel()
    {
        var controller = CreateController();

        ViewResult view = Assert.IsType<ViewResult>(controller.CreateSupportMessage());
        Assert.IsType<CreateSupportMessageViewModel>(view.Model);
    }

    // ===== POST =====

    [Fact]
    public async Task CreatePost_InvalidModel_ReturnsViewAndNeverCreates()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError(nameof(CreateSupportMessageViewModel.Title), "required");

        CreateSupportMessageViewModel model = ValidModel();
        IActionResult result = await controller.CreateSupportMessage(model);

        ViewResult view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        _service.Verify(
            s => s.CreateSupportMessage(It.IsAny<Guid>(), It.IsAny<CreateSupportMessageDto>()),
            Times.Never);
    }

    [Fact]
    public async Task CreatePost_Valid_CreatesForSignedInUser_AndRedirectsToDashboard()
    {
        CreateSupportMessageDto? captured = null;
        Guid capturedUserId = Guid.Empty;

        _service.Setup(s => s.CreateSupportMessage(It.IsAny<Guid>(), It.IsAny<CreateSupportMessageDto>()))
                .Callback<Guid, CreateSupportMessageDto>((uid, dto) =>
                {
                    capturedUserId = uid;
                    captured = dto;
                })
                .Returns(Task.CompletedTask);

        var controller = CreateController();
        IActionResult result = await controller.CreateSupportMessage(ValidModel());

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Dashboard", redirect.ControllerName);
        Assert.Equal("TodoMainApp", redirect.RouteValues!["area"]);

        // The message must be attributed to the signed-in user, never to a
        // value coming from the form.
        Assert.Equal(_userId, capturedUserId);
        Assert.NotNull(captured);
        Assert.Equal("Cannot delete a group", captured!.Title);
        Assert.Equal("When I press delete nothing happens and the page reloads.", captured.Description);
    }

    [Fact]
    public async Task CreatePost_WhenPersistFails_ReturnsViewWithModelError()
    {
        _service.Setup(s => s.CreateSupportMessage(It.IsAny<Guid>(), It.IsAny<CreateSupportMessageDto>()))
                .ThrowsAsync(new DataPersistFail());

        var controller = CreateController();
        CreateSupportMessageViewModel model = ValidModel();
        IActionResult result = await controller.CreateSupportMessage(model);

        ViewResult view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task CreatePost_WhenUnexpectedError_ReturnsViewWithModelError()
    {
        _service.Setup(s => s.CreateSupportMessage(It.IsAny<Guid>(), It.IsAny<CreateSupportMessageDto>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

        var controller = CreateController();
        IActionResult result = await controller.CreateSupportMessage(ValidModel());

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }
}
