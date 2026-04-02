using System.Text.Json;
using DotnetEngine.Application.Recommendation.Dto;
using DotnetEngine.Application.Recommendation.Ports.Driven;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Ports.Driving;
using DotnetEngine.Presentation.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DotnetEngine.Tests.Presentation;

public class RecommendationControllerTests
{
    private static PipelineRecommendationDto MakeRecommendation(string id, string status = "pending")
    {
        var suggestedActionJson = JsonSerializer.Deserialize<JsonElement>("""
            {"triggerAssetId":"battery-1","patch":{"properties":{"chargeRate":500}}}
        """);

        return new PipelineRecommendationDto
        {
            RecommendationId = id,
            ObjectId = "battery-1",
            ObjectType = "Battery",
            Severity = "warning",
            Category = "energy",
            Title = "Charge up",
            Description = "Increase charge rate",
            SuggestedAction = suggestedActionJson,
            AnalysisBasis = JsonSerializer.Deserialize<JsonElement>("{}"),
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    [Fact]
    public async Task List_ReturnsOkWithRecommendations()
    {
        var client = new Mock<IPipelineRecommendationClient>();
        client.Setup(c => c.ListAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { MakeRecommendation("r1") });

        var controller = new RecommendationController(
            client.Object,
            Mock.Of<IRunSimulationCommand>(),
            Mock.Of<IRecommendationAppliedPublisher>());

        var result = await controller.List(null, null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<PipelineRecommendationDto>>(ok.Value);
        Assert.Single(items);
        Assert.Equal("r1", items[0].RecommendationId);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var client = new Mock<IPipelineRecommendationClient>();
        client.Setup(c => c.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PipelineRecommendationDto?)null);

        var controller = new RecommendationController(
            client.Object,
            Mock.Of<IRunSimulationCommand>(),
            Mock.Of<IRecommendationAppliedPublisher>());

        var result = await controller.GetById("missing", CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Apply_ExecutesFullFlow_Approved_Simulation_Applied_Publish()
    {
        var rec = MakeRecommendation("r1", "pending");
        var appliedRec = MakeRecommendation("r1", "applied");

        var client = new Mock<IPipelineRecommendationClient>();
        client.Setup(c => c.GetByIdAsync("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(rec);
        client.Setup(c => c.UpdateStatusAsync("r1", "approved", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeRecommendation("r1", "approved"));
        client.Setup(c => c.UpdateStatusAsync("r1", "applied", It.IsAny<CancellationToken>()))
            .ReturnsAsync(appliedRec);

        var runCommand = new Mock<IRunSimulationCommand>();
        runCommand.Setup(c => c.RunAsync(It.IsAny<RunSimulationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RunResult { Success = true, RunId = "run-apply-1", Message = "ok" });

        var publisher = new Mock<IRecommendationAppliedPublisher>();

        var controller = new RecommendationController(client.Object, runCommand.Object, publisher.Object);
        var result = await controller.Apply("r1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.Contains("\"success\":true", json);
        Assert.Contains("\"runId\":\"run-apply-1\"", json);

        var sequence = new MockSequence();
        client.Verify(c => c.UpdateStatusAsync("r1", "approved", It.IsAny<CancellationToken>()), Times.Once);
        runCommand.Verify(c => c.RunAsync(It.Is<RunSimulationRequest>(r => r.TriggerAssetId == "battery-1"), It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.UpdateStatusAsync("r1", "applied", It.IsAny<CancellationToken>()), Times.Once);
        publisher.Verify(p => p.PublishAsync(
            "r1",
            "battery-1",
            It.Is<IReadOnlyDictionary<string, object?>>(d => d.ContainsKey("chargeRate")),
            "run-apply-1",
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Apply_ReturnsNotFound_WhenRecommendationMissing()
    {
        var client = new Mock<IPipelineRecommendationClient>();
        client.Setup(c => c.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PipelineRecommendationDto?)null);

        var controller = new RecommendationController(
            client.Object,
            Mock.Of<IRunSimulationCommand>(),
            Mock.Of<IRecommendationAppliedPublisher>());

        var result = await controller.Apply("missing", CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
