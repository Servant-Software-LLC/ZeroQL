﻿using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ZeroQL.SourceGenerators.Resolver.Context;

namespace ZeroQL.SourceGenerators.Resolver;

public class GraphQLSourceResolver
{
    public static string Resolve(
        SemanticModel semanticModel,
        string uniqId,
        GraphQLSourceGenerationContext context)
    {
        var graphQLInputTypeSafeName = context.GraphQLMethodInputSymbol.ToSafeGlobalName();
        var typeInfo = context.UploadProperties.ToDictionary(o => o.Type.ToSafeGlobalName());
        var source = $@"// This file generated for ZeroQL.
// <auto-generated/>
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using ZeroQL;
using ZeroQL.Stores;
using ZeroQL.Json;
using ZeroQL.Internal;

namespace {semanticModel.Compilation.Assembly.Name}
{{
    {SourceGeneratorInfo.CodeGenerationAttribute}
    public static class ZeroQLModuleInitializer_{uniqId}
    {{
        [global::System.Runtime.CompilerServices.ModuleInitializer]
        public static void Init()
        {{
            GraphQLQueryStore<{context.QueryTypeName}>.Executor[{SyntaxFactory.Literal(context.Key).Text}] = Execute;
            GraphQLQueryStore<{context.QueryTypeName}>.Query[{SyntaxFactory.Literal(context.Key).Text}] = new QueryInfo 
            {{
                Query = {SyntaxFactory.Literal(context.OperationQuery).Text},
                OperationType = {SyntaxFactory.Literal(context.OperationType).Text},
                Hash = {SyntaxFactory.Literal(context.OperationHash).Text},
            }};
        }}

        public static async Task<GraphQLResult<{context.QueryTypeName}>> Execute(IGraphQLClient qlClient, string queryKey, object variablesObject)
        {{
            var variables = ({context.RequestExecutorInputSymbol.ToGlobalName()})variablesObject;
            var qlResponse = await qlClient.QueryPipeline.ExecuteAsync<{context.QueryTypeName}>(qlClient.HttpClient, queryKey, variablesObject, queryRequest => 
            {{
                {GraphQLUploadResolver.GenerateRequestPreparations(graphQLInputTypeSafeName, typeInfo)}
                return content;
            }});

            if (qlResponse is null)
            {{
                return new GraphQLResult<{context.QueryTypeName}>
                {{
                    Errors = new[]
                    {{
                        new GraphQueryError {{ Message = ""Failed to deserialize response"" }}
                    }}
                }};
            }}

            if (qlResponse.Errors?.Length > 0)
            {{
                return new GraphQLResult<{context.QueryTypeName}>
                {{
                    Query = qlResponse.Query,
                    Errors = qlResponse.Errors,
                    Extensions = qlResponse.Extensions
                }};
            }}

            return new GraphQLResult<{context.QueryTypeName}>
            {{
                Query = qlResponse.Query,
                Data = qlResponse.Data,
                Extensions = qlResponse.Extensions
            }};
        }}

        {GraphQLUploadResolver.GenerateUploadsSelectors(context.UploadProperties, context.UploadType)}
    }}
}}";
        return source;
    }
}