namespace Atc.Opc.Ua.Tests;

public static class TestResourceProvider
{
    public static string GetTestData(
        string resourceName)
    {
        var assembly = typeof(TestResourceProvider).Assembly;
        var resourcePath = $"Atc.Opc.Ua.Tests.XUnitTestData.{resourceName}.json";

        using var stream = assembly.GetManifestResourceStream(resourcePath) ?? throw new InvalidOperationException($"Resource '{resourcePath}' not found.");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}