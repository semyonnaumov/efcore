// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.InMemory.Query.Internal;

using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using static Expression;

public partial class InMemoryShapedQueryCompilingExpressionVisitor : ShapedQueryCompilingExpressionVisitor
{
    private readonly Type _contextType;
    private readonly bool _threadSafetyChecksEnabled;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public InMemoryShapedQueryCompilingExpressionVisitor(
        ShapedQueryCompilingExpressionVisitorDependencies dependencies,
        QueryCompilationContext queryCompilationContext)
        : base(dependencies, queryCompilationContext)
    {
        _contextType = queryCompilationContext.ContextType;
        _threadSafetyChecksEnabled = dependencies.CoreSingletonOptions.AreThreadSafetyChecksEnabled;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitExtension(Expression extensionExpression)
    {
        switch (extensionExpression)
        {
            case InMemoryTableExpression inMemoryTableExpression:
                return Call(
                    TableMethodInfo,
                    QueryCompilationContext.QueryContextParameter,
                    Dependencies.LiftableConstantFactory.CreateLiftableConstant(
                        Constant(inMemoryTableExpression.EntityType),
#pragma warning disable EF1001 // Internal EF Core API usage.
                        LiftableConstantExpressionHelpers.BuildMemberAccessLambdaForEntityOrComplexType(inMemoryTableExpression.EntityType),
#pragma warning restore EF1001 // Internal EF Core API usage.
                        inMemoryTableExpression.EntityType.Name + "EntityType",
                        typeof(IEntityType)));
        }

        return base.VisitExtension(extensionExpression);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitShapedQuery(ShapedQueryExpression shapedQueryExpression)
    {
        var inMemoryQueryExpression = (InMemoryQueryExpression)shapedQueryExpression.QueryExpression;
        inMemoryQueryExpression.ApplyProjection();

        var shaperExpression = new ShaperExpressionProcessingExpressionVisitor(
                this, inMemoryQueryExpression, QueryCompilationContext.QueryTrackingBehavior == QueryTrackingBehavior.TrackAll)
            .ProcessShaper(shapedQueryExpression.ShaperExpression);
        var innerEnumerable = Visit(inMemoryQueryExpression.ServerQueryExpression);

        return New(
            typeof(QueryingEnumerable<>).MakeGenericType(shaperExpression.ReturnType).GetConstructors()[0],
            QueryCompilationContext.QueryContextParameter,
            innerEnumerable,
            //Constant(shaperExpression.Compile()),
            shaperExpression,
            Constant(_contextType),
            Constant(
                QueryCompilationContext.QueryTrackingBehavior == QueryTrackingBehavior.NoTrackingWithIdentityResolution),
            Constant(_threadSafetyChecksEnabled));
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
    {
        if (methodCallExpression.Method.IsGenericMethod
            && methodCallExpression.Method.GetGenericMethodDefinition() == EntityFrameworkCore.Infrastructure.ExpressionExtensions.ValueBufferTryReadValueMethod
            && methodCallExpression.Arguments is [Expression valueBuffer, Expression index, ConstantExpression propertyConstant]
            && propertyConstant.Value is IPropertyBase propertyValue)
        {
            var liftedProperty = Dependencies.LiftableConstantFactory.CreateLiftableConstant(
                propertyConstant,
#pragma warning disable EF1001 // Internal EF Core API usage.
                LiftableConstantExpressionHelpers.BuildMemberAccessLambdaForProperty(propertyValue),
#pragma warning restore EF1001 // Internal EF Core API usage.
                propertyValue.Name + "Property",
                propertyConstant.Type);

            return methodCallExpression.Update(methodCallExpression.Object, [valueBuffer, index, liftedProperty]);
        }

        return base.VisitMethodCall(methodCallExpression);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitConstant(ConstantExpression constantExpression)
        => constantExpression switch
        {
            //{ Value: IEntityType entityTypeValue } => Dependencies.LiftableConstantFactory.CreateLiftableConstant(
            //    constantExpression,
            //    LiftableConstantExpressionHelpers.BuildMemberAccessLambdaForEntityOrComplexType(entityTypeValue),
            //    entityTypeValue.Name + "EntityType",
            //    constantExpression.Type),
            //{ Value: IComplexType complexTypeValue } => liftableConstantFactory.CreateLiftableConstant(
            //    constantExpression,
            //    LiftableConstantExpressionHelpers.BuildMemberAccessLambdaForEntityOrComplexType(complexTypeValue),
            //    complexTypeValue.Name + "ComplexType",
            //    constantExpression.Type),
            { Value: IProperty propertyValue } => Dependencies.LiftableConstantFactory.CreateLiftableConstant(
                constantExpression,
#pragma warning disable EF1001 // Internal EF Core API usage.
                LiftableConstantExpressionHelpers.BuildMemberAccessLambdaForProperty(propertyValue),
#pragma warning restore EF1001 // Internal EF Core API usage.
                propertyValue.Name + "Property",
                constantExpression.Type),
            //{ Value: IServiceProperty servicePropertyValue } => liftableConstantFactory.CreateLiftableConstant(
            //    constantExpression,
            //    LiftableConstantExpressionHelpers.BuildMemberAccessLambdaForProperty(servicePropertyValue),
            //    servicePropertyValue.Name + "ServiceProperty",
            //    constantExpression.Type),
            //{ Value: IMaterializationInterceptor materializationInterceptorValue } => liftableConstantFactory.CreateLiftableConstant(
            //    constantExpression,
            //    c => (IMaterializationInterceptor?)new MaterializationInterceptorAggregator().AggregateInterceptors(
            //        c.Dependencies.SingletonInterceptors.OfType<IMaterializationInterceptor>().ToList())!,
            //    "materializationInterceptor",
            //    constantExpression.Type),
            //{ Value: IInstantiationBindingInterceptor instantiationBindingInterceptorValue } => liftableConstantFactory.CreateLiftableConstant(
            //    constantExpression,
            //    c => c.Dependencies.SingletonInterceptors.OfType<IInstantiationBindingInterceptor>().Where(x => x == instantiationBindingInterceptorValue).Single(),
            //    "instantiationBindingInterceptor",
            //    constantExpression.Type),

            _ => base.VisitConstant(constantExpression)
        };

    private static readonly MethodInfo TableMethodInfo
        = typeof(InMemoryShapedQueryCompilingExpressionVisitor).GetTypeInfo().GetDeclaredMethod(nameof(Table))!;

    private static IEnumerable<ValueBuffer> Table(
        QueryContext queryContext,
        IEntityType entityType)
        => ((InMemoryQueryContext)queryContext).GetValueBuffers(entityType);
}
