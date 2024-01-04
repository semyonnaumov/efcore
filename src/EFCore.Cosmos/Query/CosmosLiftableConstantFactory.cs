// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Cosmos.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Microsoft.EntityFrameworkCore.Cosmos.Query;

/// <summary>
/// TODO
/// </summary>
public class CosmosLiftableConstantFactory : LiftableConstantFactory, ICosmosLiftableConstantFactory
{
    /// <summary>
    /// TODO
    /// </summary>
    public CosmosLiftableConstantFactory(
#pragma warning disable EF1001 // Internal EF Core API usage.
        LiftableConstantExpressionDependencies dependencies)
#pragma warning restore EF1001 // Internal EF Core API usage.
//        CosmosLiftableConstantExpressionDependencies cosmosDependencies)
        : base(dependencies)
    {
//        CosmosDependencies = cosmosDependencies;
    }

    ///// <summary>
    ///// TODO
    ///// </summary>
    //public virtual CosmosLiftableConstantExpressionDependencies CosmosDependencies { get; }

    /// <summary>
    /// TODO
    /// </summary>
    public virtual LiftableConstantExpression CreateLiftableConstant(
        ConstantExpression originalExpression,
        Expression<Func<CosmosMaterializerLiftableConstantContext, object>> resolverExpression,
        string variableName,
        Type type)
        => new(originalExpression, resolverExpression, variableName, type);
}
