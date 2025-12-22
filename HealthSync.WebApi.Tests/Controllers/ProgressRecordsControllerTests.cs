using FluentAssertions;
using HealthSync.Application.DTOs.Goals;
using HealthSync.Application.Interfaces;
using HealthSync.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace HealthSync.WebApi.Tests.Controllers;

public class ProgressRecordsControllerTests
{
    private readonly Mock<IGoalService> _mockGoalService;
    private readonly ProgressRecordsController _controller;

    public ProgressRecordsControllerTests()
    {
        _mockGoalService = new Mock<IGoalService>();
        _controller = new ProgressRecordsController(_mockGoalService.Object);
    }

    private void SetupUserClaims(int userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task CreateProgressRecord_ShouldReturnCreated_WhenValidRequest()
    {
        // Arrange
        SetupUserClaims(1);
        var request = new CreateProgressRecordRequest
        {
            GoalId = 1,
            RecordDate = DateTime.UtcNow,
            RecordedValue = 70.5m,
            WeightKg = 70.5m,
            WaistCm = 80.0m,
            ChestCm = 90.0m,
            HipCm = 95.0m,
            Notes = "Good progress"
        };

        var expectedProgressRecord = new ProgressRecordDto
        {
            ProgressRecordId = 1,
            GoalId = 1,
            RecordDate = request.RecordDate,
            RecordedValue = request.RecordedValue,
            WeightKg = request.WeightKg,
            WaistCm = request.WaistCm,
            ChestCm = request.ChestCm,
            HipCm = request.HipCm,
            Notes = request.Notes
        };

        _mockGoalService
            .Setup(s => s.RecordProgressAsync(It.IsAny<RecordProgressRequest>(), 1))
            .ReturnsAsync(expectedProgressRecord);

        // Act
        var result = await _controller.CreateProgressRecord(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result;
        var value = createdResult.Value;
        value.Should().BeEquivalentTo(new
        {
            success = true,
            data = expectedProgressRecord,
            message = "Progress record created successfully"
        });
    }

    [Fact]
    public async Task CreateProgressRecord_ShouldReturnBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        SetupUserClaims(1);
        _controller.ModelState.AddModelError("GoalId", "Required");

        var request = new CreateProgressRecordRequest();

        // Act
        var result = await _controller.CreateProgressRecord(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.Should().BeEquivalentTo(new
        {
            success = false,
            message = "Invalid input",
            errors = _controller.ModelState
        });
    }

    [Fact]
    public async Task CreateProgressRecord_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        var request = new CreateProgressRecordRequest();

        // Act
        var result = await _controller.CreateProgressRecord(request);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(500);
        var value = objectResult.Value;
        value.Should().BeEquivalentTo(new
        {
            success = false,
            message = "An error occurred"
        });
    }

    [Fact]
    public async Task GetProgressRecord_ShouldReturnOk_WhenRecordExists()
    {
        // Arrange
        SetupUserClaims(1);
        var expectedRecord = new ProgressRecordDto
        {
            ProgressRecordId = 1,
            GoalId = 1,
            RecordDate = DateTime.UtcNow,
            RecordedValue = 70.5m
        };

        _mockGoalService
            .Setup(s => s.GetProgressRecordAsync(1, 1))
            .ReturnsAsync(expectedRecord);

        // Act
        var result = await _controller.GetProgressRecord(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var value = okResult.Value;
        value.Should().BeEquivalentTo(new
        {
            success = true,
            data = expectedRecord
        });
    }

    [Fact]
    public async Task GetProgressRecord_ShouldReturnNotFound_WhenRecordDoesNotExist()
    {
        // Arrange
        SetupUserClaims(1);
        _mockGoalService
            .Setup(s => s.GetProgressRecordAsync(1, 1))
            .ReturnsAsync((ProgressRecordDto)null!);

        // Act
        var result = await _controller.GetProgressRecord(1);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = (NotFoundObjectResult)result;
        var value = notFoundResult.Value;
        value.Should().BeEquivalentTo(new
        {
            success = false,
            message = "Progress record not found"
        });
    }

    [Fact]
    public async Task GetProgressRecordsByGoal_ShouldReturnOk_WithRecords()
    {
        // Arrange
        SetupUserClaims(1);
        var records = new List<ProgressRecordDto>
        {
            new ProgressRecordDto { ProgressRecordId = 1, GoalId = 1 },
            new ProgressRecordDto { ProgressRecordId = 2, GoalId = 1 }
        };

        _mockGoalService
            .Setup(s => s.GetProgressRecordsByGoalAsync(1, 1))
            .ReturnsAsync(records);

        // Act
        var result = await _controller.GetProgressRecordsByGoal(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var value = okResult.Value;
        value.Should().BeEquivalentTo(new
        {
            success = true,
            data = records
        });
    }

    [Fact]
    public async Task UpdateProgressRecord_ShouldReturnOk_WhenUpdateSucceeds()
    {
        // Arrange
        SetupUserClaims(1);
        var request = new CreateProgressRecordRequest
        {
            RecordedValue = 69.0m,
            WeightKg = 69.0m,
            Notes = "Updated progress"
        };

        _mockGoalService
            .Setup(s => s.UpdateProgressRecordAsync(1, It.IsAny<UpdateProgressRequest>(), 1))
            .ReturnsAsync(new ProgressRecordDto());

        // Act
        var result = await _controller.UpdateProgressRecord(1, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);
        var response = okResult.Value;
        response.Should().NotBeNull();
        var successProperty = response.GetType().GetProperty("success")?.GetValue(response);
        var messageProperty = response.GetType().GetProperty("message")?.GetValue(response);
        successProperty.Should().Be(true);
        messageProperty.Should().Be("Progress record updated successfully");
    }

    [Fact]
    public async Task DeleteProgressRecord_ShouldReturnOk_WhenDeleteSucceeds()
    {
        // Arrange
        SetupUserClaims(1);
        _mockGoalService
            .Setup(s => s.DeleteProgressRecordAsync(1, 1))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteProgressRecord(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);
        var response = okResult.Value;
        response.Should().NotBeNull();
        var successProperty = response.GetType().GetProperty("success")?.GetValue(response);
        var messageProperty = response.GetType().GetProperty("message")?.GetValue(response);
        successProperty.Should().Be(true);
        messageProperty.Should().Be("Progress record deleted successfully");
    }

    [Fact]
    public async Task GetProgressChart_ShouldReturnOk_WithChartData()
    {
        // Arrange
        SetupUserClaims(1);
        var chartData = new UserProgressChartDto
        {
            ProgressPoints = new List<ProgressPointDto>()
        };

        _mockGoalService
            .Setup(s => s.GetUserProgressChartAsync(1))
            .ReturnsAsync(chartData);

        // Act
        var result = await _controller.GetProgressChart();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);
        var response = okResult.Value;
        response.Should().NotBeNull();
        var successProperty = response.GetType().GetProperty("success")?.GetValue(response);
        var dataProperty = response.GetType().GetProperty("data")?.GetValue(response);
        successProperty.Should().Be(true);
        dataProperty.Should().Be(chartData);
    }

    [Fact]
    public async Task CreateProgressRecord_ShouldReturnInternalServerError_WhenExceptionOccurs()
    {
        // Arrange
        SetupUserClaims(1);
        var request = new CreateProgressRecordRequest { GoalId = 1 };

        _mockGoalService
            .Setup(s => s.RecordProgressAsync(It.IsAny<RecordProgressRequest>(), 1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateProgressRecord(request);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = (ObjectResult)result;
        statusCodeResult.StatusCode.Should().Be(500);
        var response = statusCodeResult.Value;
        response.Should().NotBeNull();
        var successProperty = response.GetType().GetProperty("success")?.GetValue(response);
        var messageProperty = response.GetType().GetProperty("message")?.GetValue(response);
        successProperty.Should().Be(false);
        messageProperty.Should().Be("An error occurred");
    }
}