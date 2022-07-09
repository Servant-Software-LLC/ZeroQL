using System.Reflection;
using Buildalyzer;
using Buildalyzer.Workspaces;
using FluentAssertions;
using LinqQL.Core;
using LinqQL.Tests.Core;
using Microsoft.CodeAnalysis;

namespace LinqQL.Tests.Data;

public static class TestProject
{
    public const string MeQuery = @"static q => q.Me(o => o.FirstName)";
    public const string FullMeQuery = @"Query(static q => q.Me(o => o.FirstName))";

    static TestProject()
    {
        var manager = new AnalyzerManager();
        manager.GetProject(@"../../../../LinqQL.TestApp/LinqQL.TestApp.csproj");
        Workspace = manager.GetWorkspace();

        Project = Workspace.CurrentSolution.Projects.First(o => o.Name == "LinqQL.TestApp");
    }

    public static Project Project { get; }

    public static AdhocWorkspace Workspace { get; }

    public static async Task<IGraphQLQueryProvider> ExecuteRequest(this Assembly assembly)
    {
        var method = (assembly.GetType("LinqQL.TestApp.Program")!
            .GetMethod("Execute", BindingFlags.Static | BindingFlags.Public)!
            .CreateDelegate(typeof(Func<Task<IGraphQLQueryProvider>>)) as Func<Task<IGraphQLQueryProvider>>)!;

        return await method.Invoke();
    }

    public static async Task<IGraphQLQueryProvider> Validate(this Project project, string graphqlQuery)
    {
        var assembly = await project.CompileToRealAssembly();
        var response = await assembly.ExecuteRequest();

        response.Query.Should().Be(graphqlQuery);

        return response;
    }
}