using FluentAssertions;
using HealthSync.Application.DTOs.Auth;
using HealthSync.Application.Features.Auth.Interfaces;
using HealthSync.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace HealthSync.WebApi.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _mockConfiguration = new Mock<IConfiguration>();

        _controller = new AuthController(_mockAuthService.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task Register_ShouldReturnOk_WhenRegistrationSucceeds()
    {
        // Arrange
        var request = new RegisterRequest("test@example.com", "Password123!", "Test User");
        var expectedResponse = new AuthResponse("access_token", "refresh_token", "1", "test@example.com", "Customer", "Test User", 0);

        _mockAuthService
            .Setup(s => s.RegisterAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AuthResponse>().Subject;
        response.AccessToken.Should().Be("access_token");
        response.RefreshToken.Should().Be("refresh_token");
        response.Email.Should().Be("test@example.com");
        response.Role.Should().Be("Customer");
        response.FullName.Should().Be("Test User");
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenEmailAlreadyExists()
    {
        // Arrange
        var request = new RegisterRequest("existing@example.com", "Password123!", "Test User");

        _mockAuthService
            .Setup(s => s.RegisterAsync(request))
            .ThrowsAsync(new InvalidOperationException("Email already registered"));

        // Act
        var result = await _controller.Register(request);

        // Assert
        var actionResult = (ActionResult<AuthResponse>)result;
        var badRequestResult = actionResult.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
        badRequestResult.Value.GetType().ToString().Should().Contain("AnonymousType");
        var dict = badRequestResult.Value as System.Collections.Generic.IDictionary<string, object>;
        if (dict == null)
        {
            // Try reflection
            var type = badRequestResult.Value.GetType();
            var messageProperty = type.GetProperty("message");
            messageProperty.Should().NotBeNull();
            var messageValue = messageProperty.GetValue(badRequestResult.Value);
            messageValue.Should().Be("Email already registered");
        }
        else
        {
            dict.Should().ContainKey("message");
            dict["message"].Should().Be("Email already registered");
        }
    }

    [Fact]
    public async Task Login_ShouldReturnOk_WhenLoginSucceeds()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "Password123!");
        var expectedResponse = new AuthResponse("access_token", "refresh_token", "1", "test@example.com", "Customer", "Test User", 10);

        _mockAuthService
            .Setup(s => s.LoginAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AuthResponse>().Subject;
        response.AccessToken.Should().Be("access_token");
        response.RefreshToken.Should().Be("refresh_token");
        response.Email.Should().Be("test@example.com");
        response.Role.Should().Be("Customer");
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenCredentialsInvalid()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "WrongPassword");

        _mockAuthService
            .Setup(s => s.LoginAsync(request))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

        // Act
        var result = await _controller.Login(request);

        // Assert
        var actionResult = (ActionResult<AuthResponse>)result;
        var unauthorizedResult = actionResult.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().NotBeNull();
        var type = unauthorizedResult.Value.GetType();
        var messageProperty = type.GetProperty("message");
        messageProperty.Should().NotBeNull();
        var messageValue = messageProperty.GetValue(unauthorizedResult.Value);
        messageValue.Should().Be("Invalid credentials");
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnOk_WhenRefreshSucceeds()
    {
        // Arrange
        var request = new RefreshTokenRequest("refresh_token");
        var expectedResponse = new AuthResponse("new_access_token", "new_refresh_token", "1", "test@example.com", "Customer", "Test User", 10);

        _mockAuthService
            .Setup(s => s.RefreshTokenAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AuthResponse>().Subject;
        response.AccessToken.Should().Be("new_access_token");
        response.RefreshToken.Should().Be("new_refresh_token");
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnUnauthorized_WhenTokenInvalid()
    {
        // Arrange
        var request = new RefreshTokenRequest("invalid_refresh_token");

        _mockAuthService
            .Setup(s => s.RefreshTokenAsync(request))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid refresh token"));

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        var actionResult = (ActionResult<AuthResponse>)result;
        var unauthorizedResult = actionResult.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().NotBeNull();
        var type = unauthorizedResult.Value.GetType();
        var messageProperty = type.GetProperty("message");
        messageProperty.Should().NotBeNull();
        var messageValue = messageProperty.GetValue(unauthorizedResult.Value);
        messageValue.Should().Be("Invalid refresh token");
    }

    [Fact]
    public async Task GoogleLogin_ShouldReturnOk_WhenLoginSucceeds()
    {
        // Arrange
        var request = new GoogleLoginRequest { Token = "google_id_token" };
        var expectedResponse = new AuthResponse("access_token", "refresh_token", "1", "test@example.com", "Customer", "Test User", 0);

        _mockAuthService
            .Setup(s => s.GoogleLoginAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GoogleLogin(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AuthResponse>().Subject;
        response.AccessToken.Should().Be("access_token");
        response.RefreshToken.Should().Be("refresh_token");
    }

    [Fact]
    public void GoogleLoginPage_ShouldReturnHtmlContent()
    {
        // Arrange
        _mockConfiguration
            .Setup(c => c["GOOGLE_CLIENT_ID"])
            .Returns("test_client_id");

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost", 5001);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = _controller.GoogleLoginPage();

        // Assert
        var contentResult = result.Should().BeOfType<ContentResult>().Subject;
        contentResult.ContentType.Should().Be("text/html");
        contentResult.Content.Should().Contain("HealthSync Google Login");
        contentResult.Content.Should().Contain("Login with Google");
    }
}