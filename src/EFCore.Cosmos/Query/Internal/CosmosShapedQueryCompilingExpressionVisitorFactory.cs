// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace Microsoft.EntityFrameworkCore.Cosmos.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class CosmosShapedQueryCompilingExpressionVisitorFactory : IShapedQueryCompilingExpressionVisitorFactory
{
    private readonly ISqlExpressionFactory _sqlExpressionFactory;
    private readonly IQuerySqlGeneratorFactory _querySqlGeneratorFactory;
    private readonly ICosmosLiftableConstantFactory _cosmosLiftableConstantFactory;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public CosmosShapedQueryCompilingExpressionVisitorFactory(
        ShapedQueryCompilingExpressionVisitorDependencies dependencies,
        //CosmosShapedQueryCompilingExpressionVisitorDependencies cosmosDependencies,
        ISqlExpressionFactory sqlExpressionFactory,
        IQuerySqlGeneratorFactory querySqlGeneratorFactory,
        ICosmosLiftableConstantFactory cosmosLiftableConstantFactory)
    {
        Dependencies = dependencies;
        //CosmosDependencies = cosmosDependencies;
        _sqlExpressionFactory = sqlExpressionFactory;
        _querySqlGeneratorFactory = querySqlGeneratorFactory;
        _cosmosLiftableConstantFactory = cosmosLiftableConstantFactory;
    }

    /// <summary>
    ///     Dependencies for this service.
    /// </summary>
    protected virtual ShapedQueryCompilingExpressionVisitorDependencies Dependencies { get; }

    ///// <summary>
    /////     Cosmos provider-specific dependencies for this service.
    ///// </summary>
    //protected virtual CosmosShapedQueryCompilingExpressionVisitorDependencies CosmosDependencies { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual ShapedQueryCompilingExpressionVisitor Create(QueryCompilationContext queryCompilationContext)
        => new CosmosShapedQueryCompilingExpressionVisitor(
            Dependencies,
            (CosmosQueryCompilationContext)queryCompilationContext,
            _sqlExpressionFactory,
            _querySqlGeneratorFactory,
            _cosmosLiftableConstantFactory);
}
