using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ZeroQL;

public static class GraphQLClientRequestExtensions
{
    public static async Task<GraphQLResult<TResult>> Execute<TQuery, TResult>(
        this IGraphQLClient client, GraphQL<TQuery, TResult> request)
    {
        return await client.Execute<GraphQL<TQuery, TResult>, TQuery, TResult>(request, (i, q) => request.Execute(q), request.GetType().Name);
    }
}

[SuppressMessage("ReSharper", "UnusedParameter.Global")]
public static class GraphQLClientLambdaExtensions
{
    public static async Task<GraphQLResult<TResult>> Query<TVariables, TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        string name,
        TVariables variables,
        Func<TVariables, TQuery, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await client.Execute(variables, query!, queryKey);
    }

    public static async Task<GraphQLResult<TResult>> Query<TVariables, TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        TVariables variables,
        Func<TVariables, TQuery, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await client.Execute(variables, query!, queryKey);
    }

    public static async Task<GraphQLResult<TResult>> Query<TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        string name,
        Func<TQuery?, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await client.Execute<Unit, TQuery, TResult>(null, (i, q) => query(q), queryKey);
    }

    public static async Task<GraphQLResult<TResult>> Query<TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        Func<TQuery, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await client.Execute<Unit, TQuery, TResult>(null, (i, q) => query(q), queryKey);
    }

    public static async Task<GraphQLResult<TResult>> Mutation<TVariables, TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        string name,
        TVariables variables,
        Func<TVariables, TMutation, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await client.Execute(variables, query!, queryKey);
    }

    public static async Task<GraphQLResult<TResult>> Mutation<TVariables, TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        TVariables variables,
        Func<TVariables, TMutation, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await client.Execute(variables, query!, queryKey);
    }

    public static async Task<GraphQLResult<TResult>> Mutation<TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        string name,
        Func<TMutation?, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await client.Execute<Unit, TMutation, TResult>(null, (i, q) => query(q), queryKey);
    }

    public static async Task<GraphQLResult<TResult>> Mutation<TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        Func<TMutation, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await client.Execute<Unit, TMutation, TResult>(null, (i, q) => query(q), queryKey);
    }
}