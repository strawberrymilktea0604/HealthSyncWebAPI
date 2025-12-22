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
    public async Task GoogleLogin_ShouldReturnUnauthorized_WhenLoginFails()
    {
        // Arrange
        var request = new GoogleLoginRequest { Token = "invalid_token" };

        _mockAuthService
            .Setup(s => s.GoogleLoginAsync(request))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid Google token"));

        // Act
        var result = await _controller.GoogleLogin(request);

        // Assert
        var actionResult = (ActionResult<AuthResponse>)result;
        var unauthorizedResult = actionResult.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().NotBeNull();
        var type = unauthorizedResult.Value.GetType();
        var messageProperty = type.GetProperty("message");
        messageProperty.Should().NotBeNull();
        var messageValue = messageProperty.GetValue(unauthorizedResult.Value);
        messageValue.Should().Be("Invalid Google token");
    }

    [Fact]
    public async Task GoogleCallback_ShouldReturnBadRequest_WhenErrorProvided()
    {
        // Act
        var result = await _controller.GoogleCallback("", "access_denied");

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
        var type = badRequestResult.Value.GetType();
        var messageProperty = type.GetProperty("message");
        messageProperty.Should().NotBeNull();
        var messageValue = messageProperty.GetValue(badRequestResult.Value);
        messageValue.Should().Be("OAuth error: access_denied");
    }

    [Fact]
    public async Task GoogleCallback_ShouldReturnBadRequest_WhenNoCode()
    {
        // Act
        var result = await _controller.GoogleCallback("", null);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
        var type = badRequestResult.Value.GetType();
        var messageProperty = type.GetProperty("message");
        messageProperty.Should().NotBeNull();
        var messageValue = messageProperty.GetValue(badRequestResult.Value);
        messageValue.Should().Be("No authorization code received");
    }

    [Fact]
    public async Task GoogleCallback_ShouldReturnHtmlContent_WhenExchangeSucceeds()
    {
        // Arrange
        var code = "valid_code";
        _mockAuthService
            .Setup(s => s.ExchangeGoogleCodeAsync(code))
            .ReturnsAsync("user_id");

        // Act
        var result = await _controller.GoogleCallback(code, null);

        // Assert
        var contentResult = result.Should().BeOfType<ContentResult>().Subject;
        contentResult.ContentType.Should().Be("text/html");
        contentResult.Content.Should().Contain("Google Authorization Successful!");
    }

    [Fact]
    public async Task GoogleCallback_ShouldReturnHtmlError_WhenExchangeFails()
    {
        // Arrange
        var code = "invalid_code";
        _mockAuthService
            .Setup(s => s.ExchangeGoogleCodeAsync(code))
            .ThrowsAsync(new Exception("Exchange failed"));

        // Act
        var result = await _controller.GoogleCallback(code, null);

        // Assert
        var contentResult = result.Should().BeOfType<ContentResult>().Subject;
        contentResult.ContentType.Should().Be("text/html");
        contentResult.Content.Should().Contain("Error exchanging code");
        contentResult.Content.Should().Contain("Exchange failed");
    }

    [Fact]
    public async Task InitializeAdmin_ShouldReturnCreated_WhenInitializationSucceeds()
    {
        // Arrange
        var request = new InitializeAdminRequest
        {
            Email = "admin@example.com",
            Password = "AdminPass123!",
            FullName = "Admin User",
            InitializationKey = "secret_key"
        };
        var expectedResponse = new AuthResponse("access_token", "refresh_token", "1", "admin@example.com", "Admin", "Admin User", 0);

        _mockAuthService
            .Setup(s => s.HasAdminAccountAsync())
            .ReturnsAsync(false);
        _mockAuthService
            .Setup(s => s.InitializeAdminAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.InitializeAdmin(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(AuthController.InitializeAdmin));
        createdResult.Value.Should().NotBeNull();
        var type = createdResult.Value.GetType();
        var successProperty = type.GetProperty("success");
        successProperty.Should().NotBeNull();
        var successValue = successProperty.GetValue(createdResult.Value);
        successValue.Should().Be(true);
    }

    [Fact]
    public async Task InitializeAdmin_ShouldReturnForbidden_WhenAdminAlreadyExists()
    {
        // Arrange
        var request = new InitializeAdminRequest
        {
            Email = "admin@example.com",
            Password = "AdminPass123!",
            FullName = "Admin User",
            InitializationKey = "secret_key"
        };

        _mockAuthService
            .Setup(s => s.HasAdminAccountAsync())
            .ReturnsAsync(true);

        // Act
        var result = await _controller.InitializeAdmin(request);

        // Assert
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(403);
        statusCodeResult.Value.Should().NotBeNull();
        var type = statusCodeResult.Value.GetType();
        var successProperty = type.GetProperty("success");
        successProperty.Should().NotBeNull();
        var successValue = successProperty.GetValue(statusCodeResult.Value);
        successValue.Should().Be(false);
    }

    [Fact]
    public async Task Register_ShouldReturnInternalServerError_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        var request = new RegisterRequest("test@example.com", "Password123!", "Test User");

        _mockAuthService
            .Setup(s => s.RegisterAsync(request))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.Register(request);

        // Assert
        var actionResult = (ActionResult<AuthResponse>)result;
        var statusCodeResult = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().NotBeNull();
        var type = statusCodeResult.Value.GetType();
        var messageProperty = type.GetProperty("message");
        messageProperty.Should().NotBeNull();
        var messageValue = messageProperty.GetValue(statusCodeResult.Value);
        messageValue.Should().Be("An error occurred during registration");
    }

    [Fact]
    public async Task Login_ShouldReturnInternalServerError_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "Password123!");

        _mockAuthService
            .Setup(s => s.LoginAsync(request))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.Login(request);

        // Assert
        var actionResult = (ActionResult<AuthResponse>)result;
        var statusCodeResult = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().NotBeNull();
        var type = statusCodeResult.Value.GetType();
        var messageProperty = type.GetProperty("message");
        messageProperty.Should().NotBeNull();
        var messageValue = messageProperty.GetValue(statusCodeResult.Value);
        messageValue.Should().Be("An error occurred during login");
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnInternalServerError_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        var request = new RefreshTokenRequest("refresh_token");

        _mockAuthService
            .Setup(s => s.RefreshTokenAsync(request))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        var actionResult = (ActionResult<AuthResponse>)result;
        var statusCodeResult = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().NotBeNull();
        var type = statusCodeResult.Value.GetType();
        var messageProperty = type.GetProperty("message");
        messageProperty.Should().NotBeNull();
        var messageValue = messageProperty.GetValue(statusCodeResult.Value);
        messageValue.Should().Be("An error occurred during token refresh");
    }

    [Fact]
    public async Task GoogleLogin_ShouldReturnInternalServerError_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        var request = new GoogleLoginRequest { Token = "google_id_token" };

        _mockAuthService
            .Setup(s => s.GoogleLoginAsync(request))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.GoogleLogin(request);

        // Assert
        var actionResult = (ActionResult<AuthResponse>)result;
        var statusCodeResult = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().NotBeNull();
        var type = statusCodeResult.Value.GetType();
        var messageProperty = type.GetProperty("message");
        messageProperty.Should().NotBeNull();
        var messageValue = messageProperty.GetValue(statusCodeResult.Value);
        messageValue.Should().Be("An error occurred during Google login");
    }

    [Fact]
    public async Task InitializeAdmin_ShouldReturnUnauthorized_WhenInitializationKeyInvalid()
    {
        // Arrange
        var request = new InitializeAdminRequest
        {
            Email = "admin@example.com",
            Password = "AdminPass123!",
            FullName = "Admin User",
            InitializationKey = "invalid_key"
        };

        _mockAuthService
            .Setup(s => s.HasAdminAccountAsync())
            .ReturnsAsync(false);
        _mockAuthService
            .Setup(s => s.InitializeAdminAsync(request))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid initialization key"));

        // Act
        var result = await _controller.InitializeAdmin(request);

        // Assert
        var actionResult = (ActionResult<AuthResponse>)result;
        var unauthorizedResult = actionResult.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().NotBeNull();
        var type = unauthorizedResult.Value.GetType();
        var successProperty = type.GetProperty("success");
        successProperty.Should().NotBeNull();
        var successValue = successProperty.GetValue(unauthorizedResult.Value);
        successValue.Should().Be(false);
        var messageProperty = type.GetProperty("message");
        messageProperty.Should().NotBeNull();
        var messageValue = messageProperty.GetValue(unauthorizedResult.Value);
        messageValue.Should().Be("Invalid initialization key");
        var codeProperty = type.GetProperty("code");
        codeProperty.Should().NotBeNull();
        var codeValue = codeProperty.GetValue(unauthorizedResult.Value);
        codeValue.Should().Be("INVALID_INITIALIZATION_KEY");
    }

    [Fact]
    public async Task InitializeAdmin_ShouldReturnBadRequest_WhenInitializationFails()
    {
        // Arrange
        var request = new InitializeAdminRequest
        {
            Email = "admin@example.com",
            Password = "AdminPass123!",
            FullName = "Admin User",
            InitializationKey = "secret_key"
        };

        _mockAuthService
            .Setup(s => s.HasAdminAccountAsync())
            .ReturnsAsync(false);
        _mockAuthService
            .Setup(s => s.InitializeAdminAsync(request))
            .ThrowsAsync(new InvalidOperationException("Email already exists"));

        // Act
        var result = await _controller.InitializeAdmin(request);

        // Assert
        var actionResult = (ActionResult<AuthResponse>)result;
        var badRequestResult = actionResult.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
        var type = badRequestResult.Value.GetType();
        var successProperty = type.GetProperty("success");
        successProperty.Should().NotBeNull();
        var successValue = successProperty.GetValue(badRequestResult.Value);
        successValue.Should().Be(false);
        var messageProperty = type.GetProperty("message");
        messageProperty.Should().NotBeNull();
        var messageValue = messageProperty.GetValue(badRequestResult.Value);
        messageValue.Should().Be("Email already exists");
        var codeProperty = type.GetProperty("code");
        codeProperty.Should().NotBeNull();
        var codeValue = codeProperty.GetValue(badRequestResult.Value);
        codeValue.Should().Be("INITIALIZATION_ERROR");
    }

    [Fact]
    public async Task InitializeAdmin_ShouldReturnInternalServerError_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        var request = new InitializeAdminRequest
        {
            Email = "admin@example.com",
            Password = "AdminPass123!",
            FullName = "Admin User",
            InitializationKey = "secret_key"
        };

        _mockAuthService
            .Setup(s => s.HasAdminAccountAsync())
            .ReturnsAsync(false);
        _mockAuthService
            .Setup(s => s.InitializeAdminAsync(request))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.InitializeAdmin(request);

        // Assert
        var actionResult = (ActionResult<AuthResponse>)result;
        var statusCodeResult = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().NotBeNull();
        var type = statusCodeResult.Value.GetType();
        var successProperty = type.GetProperty("success");
        successProperty.Should().NotBeNull();
        var successValue = successProperty.GetValue(statusCodeResult.Value);
        successValue.Should().Be(false);
        var messageProperty = type.GetProperty("message");
        messageProperty.Should().NotBeNull();
        var messageValue = messageProperty.GetValue(statusCodeResult.Value);
        messageValue.Should().Be("An error occurred during admin initialization");
        var detailProperty = type.GetProperty("detail");
        detailProperty.Should().NotBeNull();
        var detailValue = detailProperty.GetValue(statusCodeResult.Value);
        detailValue.Should().Be("Unexpected error");
    }

    [Fact]
    public void GoogleLoginPage_ShouldReturnHtmlContent()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost", 5001);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        _mockConfiguration
            .Setup(c => c["GOOGLE_CLIENT_ID"])
            .Returns("test_client_id");

        // Act
        var result = _controller.GoogleLoginPage();

        // Assert
        var contentResult = result.Should().BeOfType<ContentResult>().Subject;
        contentResult.ContentType.Should().Be("text/html");
        contentResult.Content.Should().Contain("HealthSync - Google Login");
        contentResult.Content.Should().Contain("Login with Google");
        contentResult.Content.Should().Contain("test_client_id");
        contentResult.Content.Should().Contain("redirect_uri=https%3A%2F%2Flocalhost%3A5001%2Fapi%2Fv1%2Fauth%2Fgoogle%2Fcallback");
    }
    [Fact]
    public async Task CheckAdminStatus_ShouldReturnInternalServerError_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.HasAdminAccountAsync())
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.CheckAdminStatus();

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().NotBeNull();
        var type = statusCodeResult.Value.GetType();
        var successProperty = type.GetProperty("success");
        successProperty.Should().NotBeNull();
        var successValue = successProperty.GetValue(statusCodeResult.Value);
        successValue.Should().Be(false);
        var messageProperty = type.GetProperty("message");
        messageProperty.Should().NotBeNull();
        var messageValue = messageProperty.GetValue(statusCodeResult.Value);
        messageValue.Should().Be("An error occurred while checking admin status");
        var detailProperty = type.GetProperty("detail");
        detailProperty.Should().NotBeNull();
        var detailValue = detailProperty.GetValue(statusCodeResult.Value);
        detailValue.Should().Be("Database connection failed");
    }
}

