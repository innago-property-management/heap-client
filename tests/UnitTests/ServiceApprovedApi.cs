namespace UnitTests;

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using System.Xml.XPath;

using PublicApiGenerator;

using VerifyTests.DiffPlex;

using Xunit.OpenCategories;

[Category("PublicApiTests")]
public class ServiceApproval
{
    private const string SourceFolder = "src";
    private const string ProjectFolder = "Service";
    private const string ProjectName = $"{ServiceApproval.ProjectFolder}.csproj";

    static ServiceApproval()
    {
        try
        {
            VerifyDiffPlex.Initialize(OutputType.Minimal);
        }
        catch
        {
            // do nothing
        }
    }

    [Theory]
    [ClassData(typeof(TargetFrameworksTheoryData))]
    public Task ApproveApi(string framework)
    {
        var dllName = $"{GetProjectElement("/Project/PropertyGroup/AssemblyName")?.Value ?? ServiceApproval.ProjectFolder}.dll";
        
        string configuration = typeof(ServiceApproval).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;

        string assemblyFile = CombinedPaths(ServiceApproval.SourceFolder,
            ServiceApproval.ProjectFolder,
            "bin",
            configuration,
            framework,
            dllName);

        Assembly assembly = Assembly.LoadFile(assemblyFile);
        string publicApi = assembly.GeneratePublicApi(options: null);

        return Verifier
            .Verify(publicApi)
            .ScrubLinesContaining("FrameworkDisplayName")
            .UseDirectory(Path.Combine("ApprovedApi", ServiceApproval.ProjectFolder))
            .UseFileName(framework)
            .DisableDiff();
    }

    private class TargetFrameworksTheoryData : TheoryData<string>
    {
        public TargetFrameworksTheoryData()
        {
            XElement? targetFrameworks = GetProjectElement("/Project/PropertyGroup/TargetFramework") ??
                                         GetProjectElement("/Project/PropertyGroup/TargetFrameworks");

            this.AddRange(targetFrameworks!.Value.Split(';'));
        }
    }

    private static XElement? GetProjectElement(string expression)
    {
        string csproj = CombinedPaths(ServiceApproval.SourceFolder, ServiceApproval.ProjectFolder, ServiceApproval.ProjectName);
        XDocument project = XDocument.Load(csproj);
        return project.XPathSelectElement(expression);
    }

    private static string GetSolutionDirectory([CallerFilePath] string path = "") =>
        Path.Combine(Path.GetDirectoryName(path)!, "..", "..");

    private static string CombinedPaths(params string[] paths) =>
        Path.GetFullPath(Path.Combine(paths.Prepend(GetSolutionDirectory()).ToArray()));
}