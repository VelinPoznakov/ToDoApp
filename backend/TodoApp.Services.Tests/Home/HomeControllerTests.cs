using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Controllers;
using TodoApp.Models;

namespace TodoApp.Services.Tests.Home;

public class HomeControllerTests
{
    private static HomeController CreateController(bool authenticated = false)
    {
        ClaimsIdentity identity = authenticated
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "user") }, "TestAuth")
            : new ClaimsIdentity();

        return new HomeController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
    }

    // ===== Index =====

    [Fact]
    public void Index_Anonymous_ReturnsTheLandingPage()
    {
        Assert.IsType<ViewResult>(CreateController().Index());
    }

    [Fact]
    public void Index_Authenticated_RedirectsToTheDashboard()
    {
        IActionResult result = CreateController(authenticated: true).Index();

        RedirectToActionResult redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Dashboard", redirect.ControllerName);
        Assert.Equal("TodoMainApp", redirect.RouteValues!["area"]);
    }

    // ===== Error =====

    [Theory]
    [InlineData(400, "BadRequest")]
    [InlineData(404, "NotFound")]
    [InlineData(500, "ServerError")]
    public void Error_KnownStatusCode_ReturnsItsOwnPage(int statusCode, string expectedView)
    {
        IActionResult result = CreateController().Error(statusCode);

        Assert.Equal(expectedView, Assert.IsType<ViewResult>(result).ViewName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(403)]
    public void Error_OtherStatusCode_ReturnsTheDefaultErrorPage(int statusCode)
    {
        IActionResult result = CreateController().Error(statusCode);

        ViewResult view = Assert.IsType<ViewResult>(result);
        Assert.Null(view.ViewName);
        Assert.IsType<ErrorViewModel>(view.Model);
    }
}
