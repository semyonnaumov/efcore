// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.EntityFrameworkCore.Query.Internal;
using Newtonsoft.Json.Linq;
using static System.Linq.Expressions.Expression;

namespace Microsoft.EntityFrameworkCore.Cosmos.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public partial class CosmosShapedQueryCompilingExpressionVisitor : ShapedQueryCompilingExpressionVisitor
{
    private readonly ISqlExpressionFactory _sqlExpressionFactory;
    private readonly IQuerySqlGeneratorFactory _querySqlGeneratorFactory;
    private readonly ICosmosLiftableConstantFactory _cosmosLiftableConstantFactory;
    private readonly Type _contextType;
    private readonly bool _threadSafetyChecksEnabled;
    private readonly string _partitionKeyFromExtension;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public CosmosShapedQueryCompilingExpressionVisitor(
        ShapedQueryCompilingExpressionVisitorDependencies dependencies,
//        CosmosShapedQueryCompilingExpressionVisitorDependencies cosmosDependencies,
        CosmosQueryCompilationContext cosmosQueryCompilationContext,
        ISqlExpressionFactory sqlExpressionFactory,
        IQuerySqlGeneratorFactory querySqlGeneratorFactory,
        ICosmosLiftableConstantFactory cosmosLiftableConstantFactory)
        : base(dependencies, cosmosQueryCompilationContext)
    {
        //CosmosDependencies = cosmosDependencies;

        _sqlExpressionFactory = sqlExpressionFactory;
        _querySqlGeneratorFactory = querySqlGeneratorFactory;
        _cosmosLiftableConstantFactory = cosmosLiftableConstantFactory;
        _contextType = cosmosQueryCompilationContext.ContextType;
        _threadSafetyChecksEnabled = dependencies.CoreSingletonOptions.AreThreadSafetyChecksEnabled;
        _partitionKeyFromExtension = cosmosQueryCompilationContext.PartitionKeyFromExtension;
    }

    ///// <summary>
    /////     Cosmos provider-specific dependencies for this service.
    ///// </summary>
    //protected virtual CosmosShapedQueryCompilingExpressionVisitorDependencies CosmosDependencies { get; }

    private static readonly ConstructorInfo _readItemExpressionCtor
        = typeof(ReadItemExpression).GetConstructor([typeof(IEntityType), typeof(IDictionary<IProperty, string>)]);

    private static readonly ConstructorInfo _readItemDictionaryCtor
        = typeof(Dictionary<IProperty, string>).GetConstructor([]);

    private static readonly MethodInfo _readItemDictionaryAddMethod
        = typeof(Dictionary<IProperty, string>).GetMethod("Add", [typeof(IProperty), typeof(string)]);


    // TODO: add proper quoting infra on expressions themselves?
    private Expression QuoteReadItemExpression(ReadItemExpression readItemExpression)
    {
        var entityTypeExpression = _cosmosLiftableConstantFactory.CreateLiftableConstant(
            Constant(readItemExpression.EntityType),
#pragma warning disable EF1001 // Internal EF Core API usage.
            LiftableConstantExpressionHelpers.BuildMemberAccessLambdaForEntityOrComplexType(readItemExpression.EntityType),
#pragma warning restore EF1001 // Internal EF Core API usage.
            readItemExpression.EntityType.Name + "EntityType",
            typeof(IEntityType));

        var newDictionary = New(_readItemDictionaryCtor);
        var propertyParameterDictionaryInitializers = new List<ElementInit>();
        foreach (var element in readItemExpression.PropertyParameters)
        {
            propertyParameterDictionaryInitializers.Add(
                ElementInit(
                    _readItemDictionaryAddMethod,
                    _cosmosLiftableConstantFactory.CreateLiftableConstant(
                        Constant(element.Key),
#pragma warning disable EF1001 // Internal EF Core API usage.
                        LiftableConstantExpressionHelpers.BuildMemberAccessLambdaForProperty(element.Key),
#pragma warning restore EF1001 // Internal EF Core API usage.
                        element.Key.Name + "Property",
                        typeof(IProperty)),
                    Constant(element.Value)));
        }

        IProperty prop1 = null;
        IProperty prop2 = null;


        Expression<Func<int, Dictionary<IProperty, string>>> wrapper = x => new Dictionary<IProperty, string>
        {
            { prop1, "Foo" },
            { prop2, "Bar" }
        };

        var body = wrapper.Body;

 
        return New(
            _readItemExpressionCtor,
            entityTypeExpression,
            ListInit(
                New(_readItemDictionaryCtor),
                propertyParameterDictionaryInitializers));
    } 

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitShapedQuery(ShapedQueryExpression shapedQueryExpression)
    {
        var jObjectParameter = Parameter(typeof(JObject), "jObject");

        var shaperBody = shapedQueryExpression.ShaperExpression;
        shaperBody = new JObjectInjectingExpressionVisitor().Visit(shaperBody);
        shaperBody = InjectEntityMaterializers(shaperBody);

        switch (shapedQueryExpression.QueryExpression)
        {
            case SelectExpression selectExpression:
                shaperBody = new CosmosProjectionBindingRemovingExpressionVisitor(
                        this, selectExpression, jObjectParameter,
                        QueryCompilationContext.QueryTrackingBehavior == QueryTrackingBehavior.TrackAll)
                    .Visit(shaperBody);

                var shaperLambda = Lambda(
                    shaperBody,
                    QueryCompilationContext.QueryContextParameter,
                    jObjectParameter);

                return New(
                    typeof(QueryingEnumerable<>).MakeGenericType(shaperLambda.ReturnType).GetConstructors()[0],
                    Convert(
                        QueryCompilationContext.QueryContextParameter,
                        typeof(CosmosQueryContext)),
                    //Constant(_sqlExpressionFactory),
                    _cosmosLiftableConstantFactory.CreateLiftableConstant(
                        Constant(_sqlExpressionFactory),
                        c => c.SqlExpressionFactory,
                        "sqlExpressionFactory",
                        typeof(ISqlExpressionFactory)),
                    //Constant(_querySqlGeneratorFactory),
                    _cosmosLiftableConstantFactory.CreateLiftableConstant(
                        Constant(_querySqlGeneratorFactory),
                        c => c.QuerySqlGeneratorFactory,
                        "sqlGeneratorFactory",
                        typeof(IQuerySqlGeneratorFactory)),
                    Constant(selectExpression),
                    shaperLambda,
                    Constant(_contextType),
                    Constant(_partitionKeyFromExtension, typeof(string)),
                    Constant(
                        QueryCompilationContext.QueryTrackingBehavior == QueryTrackingBehavior.NoTrackingWithIdentityResolution),
                    Constant(_threadSafetyChecksEnabled));

            case ReadItemExpression readItemExpression:
                shaperBody = new CosmosProjectionBindingRemovingReadItemExpressionVisitor(
                        this, readItemExpression, jObjectParameter,
                        QueryCompilationContext.QueryTrackingBehavior == QueryTrackingBehavior.TrackAll)
                    .Visit(shaperBody);

                var shaperReadItemLambda = Lambda(
                    shaperBody,
                    QueryCompilationContext.QueryContextParameter,
                    jObjectParameter);

                return New(
                    typeof(ReadItemQueryingEnumerable<>).MakeGenericType(shaperReadItemLambda.ReturnType).GetConstructors()[0],
                    Convert(
                        QueryCompilationContext.QueryContextParameter,
                        typeof(CosmosQueryContext)),
                    //Constant(readItemExpression),
                    QuoteReadItemExpression(readItemExpression),
                    shaperReadItemLambda,
                    Constant(_contextType),
                    Constant(
                        QueryCompilationContext.QueryTrackingBehavior == QueryTrackingBehavior.NoTrackingWithIdentityResolution),
                    Constant(_threadSafetyChecksEnabled));

            default:
                throw new NotSupportedException(CoreStrings.UnhandledExpressionNode(shapedQueryExpression.QueryExpression));
        }
    }
}
