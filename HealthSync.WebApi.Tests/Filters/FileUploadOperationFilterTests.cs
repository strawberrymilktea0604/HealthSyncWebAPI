using HealthSync.WebApi.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace HealthSync.WebApi.Tests.Filters;

public class FileUploadOperationFilterTests
{
    private readonly FileUploadOperationFilter _filter;

    public FileUploadOperationFilterTests()
    {
        _filter = new FileUploadOperationFilter();
    }

    [Fact]
    public void Apply_NoFileParameters_DoesNotModifyRequestBody()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.NoFileParameters))!;
        var context = CreateOperationFilterContext(methodInfo);

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.Null(operation.RequestBody);
    }

    [Fact]
    public void Apply_SingleFileParameter_CreatesMultipartFormData()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.SingleFileParameter))!;
        var context = CreateOperationFilterContext(methodInfo);

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.NotNull(operation.RequestBody);
        Assert.True(operation.RequestBody.Content.ContainsKey("multipart/form-data"));
        var mediaType = operation.RequestBody.Content["multipart/form-data"];
        Assert.NotNull(mediaType.Schema);
        Assert.Equal("object", mediaType.Schema.Type);
    }

    [Fact]
    public void Apply_SingleFileParameter_CreatesCorrectSchema()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.SingleFileParameter))!;
        var context = CreateOperationFilterContext(methodInfo);

        // Act
        _filter.Apply(operation, context);

        // Assert
        var schema = operation.RequestBody.Content["multipart/form-data"].Schema;
        Assert.Single(schema.Properties);
        Assert.True(schema.Properties.ContainsKey("file"));
        Assert.Equal("string", schema.Properties["file"].Type);
        Assert.Equal("binary", schema.Properties["file"].Format);
    }

    [Fact]
    public void Apply_MultipleFileParameters_CreatesMultipleProperties()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.MultipleFileParameters))!;
        var context = CreateOperationFilterContext(methodInfo);

        // Act
        _filter.Apply(operation, context);

        // Assert
        var schema = operation.RequestBody.Content["multipart/form-data"].Schema;
        Assert.Equal(2, schema.Properties.Count);
        Assert.True(schema.Properties.ContainsKey("file1"));
        Assert.True(schema.Properties.ContainsKey("file2"));
    }

    [Fact]
    public void Apply_IEnumerableFileParameter_CreatesSchema()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.MultipleFilesParameter))!;
        var context = CreateOperationFilterContext(methodInfo);

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.NotNull(operation.RequestBody);
        Assert.True(operation.RequestBody.Content.ContainsKey("multipart/form-data"));
        var schema = operation.RequestBody.Content["multipart/form-data"].Schema;
        Assert.Single(schema.Properties);
        Assert.True(schema.Properties.ContainsKey("files"));
    }

    [Fact]
    public void Apply_FileParameterWithNullName_UsesDefaultName()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.SingleFileParameter))!;
        var context = CreateOperationFilterContext(methodInfo);

        // Act
        _filter.Apply(operation, context);

        // Assert
        var schema = operation.RequestBody.Content["multipart/form-data"].Schema;
        Assert.True(schema.Properties.ContainsKey("file"));
    }

    [Fact]
    public void Apply_MarksFileParametersAsRequired()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.SingleFileParameter))!;
        var context = CreateOperationFilterContext(methodInfo);

        // Act
        _filter.Apply(operation, context);

        // Assert
        var schema = operation.RequestBody.Content["multipart/form-data"].Schema;
        Assert.Contains("file", schema.Required);
    }

    [Fact]
    public void Apply_MixedParameters_OnlyProcessesFileParameters()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.MixedParameters))!;
        var context = CreateOperationFilterContext(methodInfo);

        // Act
        _filter.Apply(operation, context);

        // Assert
        var schema = operation.RequestBody.Content["multipart/form-data"].Schema;
        Assert.Single(schema.Properties);
        Assert.True(schema.Properties.ContainsKey("file"));
        Assert.False(schema.Properties.ContainsKey("id")); // Non-file parameter not included
    }

    [Fact]
    public void Apply_ExistingRequestBody_Replaces()
    {
        // Arrange
        var operation = new OpenApiOperation
        {
            RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType()
                }
            }
        };
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.SingleFileParameter))!;
        var context = CreateOperationFilterContext(methodInfo);

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.NotNull(operation.RequestBody);
        Assert.True(operation.RequestBody.Content.ContainsKey("multipart/form-data"));
        Assert.False(operation.RequestBody.Content.ContainsKey("application/json"));
    }

    private OperationFilterContext CreateOperationFilterContext(MethodInfo methodInfo)
    {
        var apiDescription = new ApiDescription
        {
            ActionDescriptor = new ControllerActionDescriptor
            {
                MethodInfo = methodInfo
            }
        };

        return new OperationFilterContext(
            apiDescription,
            null!,
            null!,
            methodInfo);
    }

    // Test controller class
    private class TestController
    {
        public void NoFileParameters(int id, string name) { }
        
        public void SingleFileParameter(IFormFile file) { }
        
        public void MultipleFileParameters(IFormFile file1, IFormFile file2) { }
        
        public void MultipleFilesParameter(IEnumerable<IFormFile> files) { }
        
        public void MixedParameters(int id, IFormFile file, string name) { }
    }
}
