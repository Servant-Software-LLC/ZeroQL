using FluentAssertions;
using ZeroQL.SourceGenerators;
using ZeroQL.SourceGenerators.Analyzers;
using Xunit;
using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

public class QueryTests : IntegrationTest
{

    [Fact]
    public async Task CompilationWorks()
    {
        await TestProject.Project.CompileToRealAssembly();
    }

    [Fact]
    public async Task SimpleQuery()
    {
        var graphqlQuery = @"query { me { firstName } }";
        var project = TestProject.Project;

        var result = (GraphQLResult<string>)await project.Validate(graphqlQuery);

        result.Data.Should().Be("Jon");
    }

    [Fact]
    public async Task SupportForAnonymousType()
    {
        var csharpQuery = "static q => q.Me(o => new { o.FirstName })";
        var graphqlQuery = @"query { me { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task NullableQueryHandled()
    {
        var csharpQuery = "static q => q.User(10, o => o.FirstName)";
        var graphqlQuery = @"query { user(id: 10) { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        var result = await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task SupportForMultipleMembers()
    {
        var csharpQuery = "static q => q.Me(o => new { o.FirstName.Length })";
        var graphqlQuery = @"query { me { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task FailsWhenAccessingVariablesOutLambdaScope()
    {
        var csharpQuery = "static q => q.Me(o => q.Me(o => o.FirstName))";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        var diagnostics = await project.ApplyAnalyzer(new QueryLambdaAnalyzer());

        diagnostics.Should()
            .Contain(o => o.Id == Descriptors.DontUseOutScopeValues.Id);
    }

    [Fact]
    public async Task FailsOnOpenLambda()
    {
        var csharpQuery = "static q => q.Me(o => o)";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        var diagnostics = await project.ApplyAnalyzer(new QueryLambdaAnalyzer());
        diagnostics.Should()
            .Contain(o => o.Id == Descriptors.OpenLambdaIsNotAllowed.Id);
    }

    [Fact]
    public async Task FailsWhenMethodCalled()
    {
        var csharpQuery = "static q => q.Me(o => o.ToString())";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        var diagnostics = await project.ApplyAnalyzer(new QueryLambdaAnalyzer());
        diagnostics.Should()
            .Contain(o => o.Id == Descriptors.OnlyFieldSelectorsAndFragmentsAreAllowed.Id);
    }

    [Fact]
    public async Task FailsWhenLambdaIsNotStatic()
    {
        var csharpQuery = "q => q.Me(o => o.FirstName)";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        var diagnostics = await project.ApplyAnalyzer(new QueryLambdaAnalyzer());

        diagnostics.Should()
            .Contain(o => o.Id == Descriptors.OnlyStaticLambda.Id);
    }

    [Fact]
    public async Task FailsWhenLambdaIsNotStaticWithArgument()
    {
        var csharpQuery = "new { Id = 1}, (i, q) => q.Me(o => o.FirstName)";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        var diagnostics = await project.ApplyAnalyzer(new QueryLambdaAnalyzer());

        diagnostics.Should()
            .Contain(o => o.Id == Descriptors.OnlyStaticLambda.Id);
    }

    [Fact]
    public async Task QueryPreviewGenerated()
    {
        var csharpQuery = "static q => new { Me = q.Me(o => new { o.FirstName }) }";
        var graphqlQuery = @"query { me { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        var diagnostics = await project.ApplyAnalyzer(new QueryLambdaAnalyzer());
        var queryPreview = diagnostics!.First(o => o.Id == Descriptors.GraphQLQueryPreview.Id);

        queryPreview.GetMessage().Should().Be(graphqlQuery);
    }

    [Fact]
    public async Task SupportsAnonymousTypeInQueryRoot()
    {
        var csharpQuery = "static q => new { Me = q.Me(o => new { o.FirstName }) }";
        var graphqlQuery = @"query { me { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task SupportsAnonymousTypeWithMultipleFields()
    {
        var csharpQuery = "static q => q.Me(o => new { o.FirstName, o.LastName })";
        var graphqlQuery = @"query { me { firstName lastName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact(Skip = "Figure out how to support this")]
    public async Task SupportsAnonymousTypeWithMultipleIdenticalFieldsInRootQuery()
    {
        var csharpQuery = "static q => new { Me1 = q.Me(o => new { o.FirstName, o.LastName }), Me2 = q.Me(o => new { o.FirstName, o.LastName }) }";
        var graphqlQuery = @"{ m1: me { firstName lastName } m2: me { firstName lastName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task SupportsAnonymousTypeWithConstantArgumentQuery()
    {
        var csharpQuery = "static q => new { User = q.User(42, o => new { o.FirstName, o.LastName }) }";
        var graphqlQuery = @"query { user(id: 42) { firstName lastName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task SupportForEnums()
    {
        var csharpQuery = "static q => q.Me(o => o.UserKind)";
        var graphqlQuery = @"query { me { userKind } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task SupportForArray()
    {
        var arguments = "new { Filter = new UserFilterInput { UserKind = UserKind.Good } }";
        var csharpQuery = "static (i, q) => q.Users(i.Filter, 0,  10, o => o.FirstName)";
        var graphqlQuery = @"query ($filter: UserFilterInput!) { users(filter: $filter, page: 0, size: 10) { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, $"{arguments}, {csharpQuery}"));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task SupportForArrayWithScalarElements()
    {
        var csharpQuery = "static q => q.UsersIds(UserKind.Good, 0, 10)";
        var graphqlQuery = @"query { usersIds(kind: GOOD, page: 0, size: 10)}";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task SupportsQueryName()
    {
        var csharpQuery = "static q => q.Me(o => o.FirstName)";
        var graphqlQuery = @"query Me{ me { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, @"""Me"", " + csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task SupportsOnlyLiteralQueryName()
    {
        var csharpQuery = "static q => q.Me(o => o.FirstName)";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, @"nameof(Execute), " + csharpQuery));

        var diagnostics = await project.ApplyAnalyzer(new QueryLambdaAnalyzer());

        diagnostics.Should().Contain(o => o.Id == Descriptors.GraphQLQueryNameShouldBeLiteral.Id);
    }

    [Fact]
    public async Task SupportsExtensionsInsideError()
    {
        var csharpQuery = "Mutation(static m => m.DoError)";
        var graphqlQuery = @"mutation { doError}";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FULL_ME_QUERY, csharpQuery));

        var response = await project.Validate(graphqlQuery, false);
        
        var value = response.Errors!.First().Extensions!.First();
        value.Key.Should().Be("message");
        value.Value.ToString().Should().Be("This is an error");
    }
}