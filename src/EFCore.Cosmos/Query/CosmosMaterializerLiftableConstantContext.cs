// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Cosmos.Query.Internal;

namespace Microsoft.EntityFrameworkCore.Cosmos.Query;

/// <summary>
/// TODO
/// </summary>
public sealed record CosmosMaterializerLiftableConstantContext(
        ShapedQueryCompilingExpressionVisitorDependencies Dependencies,
        IQuerySqlGeneratorFactory QuerySqlGeneratorFactory,
        ISqlExpressionFactory SqlExpressionFactory)
        //CosmosShapedQueryCompilingExpressionVisitorDependencies CosmosDependencies)
    : MaterializerLiftableConstantContext(Dependencies);
