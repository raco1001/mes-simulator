using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.LinkType.Dto;
using DotnetEngine.Application.LinkType.Ports.Driven;
using DotnetEngine.Application.ObjectType.Dto;
using DotnetEngine.Application.ObjectType.Ports.Driven;
using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Relationship.Handlers;
using DotnetEngine.Application.Relationship.Ports.Driven;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetEngine.Tests.Application.Relationship;

public class CreateRelationshipCommandHandlerTests
{
    [Fact]
    public async Task CreateAsync_SeedsPropertiesFromLinkTypeSchema()
    {
        var relRepo = new Mock<IRelationshipRepository>();
        relRepo.Setup(r => r.AddAsync(It.IsAny<RelationshipDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RelationshipDto dto, CancellationToken _) => dto);

        var linkRepo = new Mock<ILinkTypeSchemaRepository>();
        linkRepo.Setup(r => r.GetByLinkTypeAsync("Supplies", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LinkTypeSchemaDto
            {
                SchemaVersion = "v1",
                LinkType = "Supplies",
                DisplayName = "Supplies",
                Direction = LinkDirection.Directed,
                Temporality = LinkTemporality.Durable,
                Properties =
                [
                    new PropertyDefinition
                    {
                        Key = "ratio",
                        DataType = DataType.Number,
                        SimulationBehavior = SimulationBehavior.Settable,
                        Mutability = Mutability.Mutable,
                        BaseValue = 0.5,
                        Required = false
                    }
                ],
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

        var sut = new CreateRelationshipCommandHandler(
            relRepo.Object,
            assetRepository: null,
            objectTypeSchemaRepository: null,
            linkTypeSchemaRepository: linkRepo.Object,
            mappingValidator: null,
            logger: null);

        var result = await sut.CreateAsync(new CreateRelationshipRequest
        {
            FromAssetId = "a1",
            ToAssetId = "a2",
            RelationshipType = "Supplies",
            Properties = new Dictionary<string, object> { ["custom"] = 1 }
        });

        Assert.Equal(0.5, result.Properties["ratio"]);
        Assert.Equal(1, result.Properties["custom"]);
    }

    [Fact]
    public async Task CreateAsync_PassesMappingsToSavedDto()
    {
        RelationshipDto? captured = null;
        var relRepo = new Mock<IRelationshipRepository>();
        relRepo.Setup(r => r.AddAsync(It.IsAny<RelationshipDto>(), It.IsAny<CancellationToken>()))
            .Callback<RelationshipDto, CancellationToken>((dto, _) => captured = dto)
            .ReturnsAsync((RelationshipDto dto, CancellationToken _) => dto);

        var sut = new CreateRelationshipCommandHandler(relRepo.Object);

        var mappings = new[]
        {
            new PropertyMapping("temperature", "heat", "value * 1.5")
        };
        var result = await sut.CreateAsync(new CreateRelationshipRequest
        {
            FromAssetId = "a1",
            ToAssetId = "a2",
            RelationshipType = "Supplies",
            Mappings = mappings
        });

        Assert.NotNull(captured);
        Assert.Single(captured!.Mappings);
        Assert.Equal("temperature", captured.Mappings[0].FromProperty);
        Assert.Equal("heat", captured.Mappings[0].ToProperty);
        Assert.Equal("value * 1.5", captured.Mappings[0].TransformRule);
        Assert.Single(result.Mappings);
        Assert.Equal("temperature", result.Mappings[0].FromProperty);
    }

    [Fact]
    public async Task CreateAsync_WhenConstraintMismatched_LogsWarningOnly()
    {
        var relRepo = new Mock<IRelationshipRepository>();
        relRepo.Setup(r => r.AddAsync(It.IsAny<RelationshipDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RelationshipDto dto, CancellationToken _) => dto);

        var assetRepo = new Mock<IAssetRepository>();
        assetRepo.Setup(r => r.GetByIdAsync("from", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetDto { Id = "from", Type = "freezer", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        assetRepo.Setup(r => r.GetByIdAsync("to", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetDto { Id = "to", Type = "battery", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });

        var objectRepo = new Mock<IObjectTypeSchemaRepository>();
        objectRepo.Setup(r => r.GetByObjectTypeAsync("freezer", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ObjectTypeSchemaDto
            {
                SchemaVersion = "v1",
                ObjectType = "freezer",
                DisplayName = "freezer",
                Traits = new ObjectTraits { Persistence = Persistence.Durable, Dynamism = Dynamism.Dynamic, Cardinality = Cardinality.Singular },
                OwnProperties = [],
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        objectRepo.Setup(r => r.GetByObjectTypeAsync("battery", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ObjectTypeSchemaDto
            {
                SchemaVersion = "v1",
                ObjectType = "battery",
                DisplayName = "battery",
                Traits = new ObjectTraits { Persistence = Persistence.Transient, Dynamism = Dynamism.Static, Cardinality = Cardinality.Singular },
                OwnProperties = [],
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

        var linkRepo = new Mock<ILinkTypeSchemaRepository>();
        linkRepo.Setup(r => r.GetByLinkTypeAsync("Contains", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LinkTypeSchemaDto
            {
                SchemaVersion = "v1",
                LinkType = "Contains",
                DisplayName = "Contains",
                Direction = LinkDirection.Hierarchical,
                Temporality = LinkTemporality.Permanent,
                FromConstraint = new LinkConstraint
                {
                    AllowedObjectTypes = ["generator"]
                },
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

        var logger = new Mock<ILogger<CreateRelationshipCommandHandler>>();
        var sut = new CreateRelationshipCommandHandler(
            relRepo.Object,
            assetRepo.Object,
            objectRepo.Object,
            linkRepo.Object,
            mappingValidator: null,
            logger.Object);

        var result = await sut.CreateAsync(new CreateRelationshipRequest
        {
            FromAssetId = "from",
            ToAssetId = "to",
            RelationshipType = "Contains"
        });

        Assert.Equal("Contains", result.RelationshipType);
        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("constraint mismatch")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
