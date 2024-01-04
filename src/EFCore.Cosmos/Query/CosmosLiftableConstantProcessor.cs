// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Cosmos.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Microsoft.EntityFrameworkCore.Cosmos.Query;

#pragma warning disable EF1001 // LiftableConstantProcessor is internal

/// <summary>
/// TODO
/// </summary>
public class CosmosLiftableConstantProcessor : LiftableConstantProcessor
{
    private readonly CosmosMaterializerLiftableConstantContext _cosmosMaterializerLiftableConstantContext;

    /// <summary>
    /// TODO
    /// </summary>
    public CosmosLiftableConstantProcessor(
        ShapedQueryCompilingExpressionVisitorDependencies dependencies,
        IQuerySqlGeneratorFactory querySqlGeneratorFactory,
        ISqlExpressionFactory sqlExpressionFactory)
        //CosmosShapedQueryCompilingExpressionVisitorDependencies cosmosDependencies)
        : base(dependencies)
        => _cosmosMaterializerLiftableConstantContext = new(dependencies, querySqlGeneratorFactory, sqlExpressionFactory);

    /// <inheritdoc/>
    protected override ConstantExpression InlineConstant(LiftableConstantExpression liftableConstant)
    {
        if (liftableConstant.ResolverExpression is Expression<Func<CosmosMaterializerLiftableConstantContext, object>>
            resolverExpression)
        {
            var resolver = resolverExpression.Compile(preferInterpretation: true);
            var value = resolver(_cosmosMaterializerLiftableConstantContext);
            return Expression.Constant(value, liftableConstant.Type);
        }

        return base.InlineConstant(liftableConstant);
    }
}
