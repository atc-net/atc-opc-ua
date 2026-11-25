namespace Atc.Opc.Ua.Tests.Options;

public sealed class OpcUaConstantsTests
{
    [Fact]
    public void ObjectsFolderNodeId_ShouldBeTheStacksObjectsFolder()
        => OpcUaConstants.ObjectsFolderNodeId.Should().Be(ObjectIds.ObjectsFolder.ToString());
}