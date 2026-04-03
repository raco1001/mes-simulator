using DotnetEngine.Application.ObjectType.Dto;
using DotnetEngine.Application.ObjectType.Handlers;
using DotnetEngine.Application.ObjectType.Ports.Driven;
using Moq;
using Xunit;

namespace DotnetEngine.Tests.Application.ObjectType;

public class ObjectTypeSchemaHandlersTests
{
    [Fact]
    public async Task CreateAsync_CreatesSchema()
    {
        var repo = new Mock<IObjectTypeSchemaRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<ObjectTypeSchemaDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ObjectTypeSchemaDto d, CancellationToken _) => d);
        var handler = new CreateObjectTypeSchemaCommandHandler(repo.Object);

        var result = await handler.CreateAsync(new CreateObjectTypeSchemaRequest
        {
            SchemaVersion = "v1",
            ObjectType = "freezer",
            DisplayName = "Freezer",
            Traits = new ObjectTraits
            {
                Persistence = Persistence.Durable,
                Dynamism = Dynamism.Dynamic,
                Cardinality = Cardinality.Enumerable
            },
            OwnProperties =
            [
                new PropertyDefinition
                {
                    Key = "targetTemp",
                    DataType = DataType.Number,
                    SimulationBehavior = SimulationBehavior.Settable,
                    Mutability = Mutability.Mutable
                }
            ]
        });

        Assert.Equal("freezer", result.ObjectType);
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToRepository_ReturnsTrueWhenDeleted()
    {
        var repo = new Mock<IObjectTypeSchemaRepository>();
        repo.Setup(r => r.DeleteAsync("freezer", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = new DeleteObjectTypeSchemaCommandHandler(repo.Object);

        var result = await handler.DeleteAsync("freezer");

        Assert.True(result);
        repo.Verify(r => r.DeleteAsync("freezer", It.IsAny<CancellationToken>()), Times.Once);
    }
}
