namespace Atc.Opc.Ua.Tests.Differs;

public sealed class NodeTreeDifferTests
{
    [Fact]
    public void Diff_Should_Handle_Nulls()
    {
        var result = NodeTreeDiffer.Diff(currentRoot: null, previousRoot: null);

        result.AddedNodes.Should().BeEmpty();
        result.DeletedNodes.Should().BeEmpty();
        result.UnchangedNodes.Should().BeEmpty();
    }

    [Fact]
    public void Diff_Should_Find_Added_And_Deleted_And_Unchanged()
    {
        // previous tree
        var prevRoot = new NodeObject
        {
            NodeId = "ns=2;i=1",
            DisplayName = "RootPrev",
        };
        var prevChildObj = new NodeObject { NodeId = "ns=2;i=10", DisplayName = "ChildObj" };
        var prevVarOnlyPrev = new NodeVariable { NodeId = "ns=2;i=20", DisplayName = "VarOnlyPrev" };
        prevRoot.NodeObjects.Add(prevChildObj);
        prevRoot.NodeVariables.Add(prevVarOnlyPrev);

        // current tree
        var currRoot = new NodeObject
        {
            NodeId = "ns=2;i=1",
            DisplayName = "RootCurr",
        };

        var currChildObj = new NodeObject { NodeId = "ns=2;i=10", DisplayName = "ChildObj" }; // unchanged
        var currVarOnlyCurr = new NodeVariable { NodeId = "ns=2;i=30", DisplayName = "VarOnlyCurr" }; // added
        currRoot.NodeObjects.Add(currChildObj);
        currRoot.NodeVariables.Add(currVarOnlyCurr);

        var result = NodeTreeDiffer.Diff(currRoot, prevRoot);

        // unchanged nodes should include node ids 1 and 10
        result.UnchangedNodes.Select(n => n.NodeId).Should().BeEquivalentTo("ns=2;i=1", "ns=2;i=10");

        // deleted should include 20
        result.DeletedNodes.Select(n => n.NodeId).Should().BeEquivalentTo("ns=2;i=20");

        // added should include 30
        result.AddedNodes.Select(n => n.NodeId).Should().BeEquivalentTo("ns=2;i=30");
    }
}
