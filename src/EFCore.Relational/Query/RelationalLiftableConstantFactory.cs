// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Microsoft.EntityFrameworkCore.Query;

/// <summary>
/// TODO
/// </summary>
public class RelationalLiftableConstantFactory : LiftableConstantFactory, IRelationalLiftableConstantFactory
{
    /// <summary>
    /// TODO
    /// </summary>
    public RelationalLiftableConstantFactory(
#pragma warning disable EF1001 // Internal EF Core API usage.
        LiftableConstantExpressionDependencies dependencies,
#pragma warning restore EF1001 // Internal EF Core API usage.
        RelationalLiftableConstantExpressionDependencies relationalDependencies)
        : base(dependencies)
    {
        RelationalDependencies = relationalDependencies;
    }

    /// <summary>
    /// TODO
    /// </summary>
    public virtual RelationalLiftableConstantExpressionDependencies RelationalDependencies { get; }

    /// <summary>
    /// TODO
    /// </summary>
    public virtual LiftableConstantExpression CreateLiftableConstant(
        ConstantExpression originalExpression,
        Expression<Func<RelationalMaterializerLiftableConstantContext, object>> resolverExpression,
        string variableName,
        Type type)
        => new(originalExpression, resolverExpression, variableName, type);
}
