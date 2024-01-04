// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public static class ComplexTypeExtensions
{
    private static readonly MethodInfo ModelFindEntiyTypeMethod =
        typeof(IModel).GetRuntimeMethod(nameof(IModel.FindEntityType), [typeof(string)])!;

    private static readonly MethodInfo RuntimeModelFindAdHocEntiyTypeMethod =
        typeof(RuntimeModel).GetRuntimeMethod(nameof(RuntimeModel.FindAdHocEntityType), [typeof(string)])!;

    private static readonly MethodInfo TypeBaseFindComplexPropertyMethod =
        typeof(ITypeBase).GetRuntimeMethod(nameof(ITypeBase.FindComplexProperty), [typeof(string)])!;

    private static readonly MethodInfo TypeBaseFindPropertyMethod =
        typeof(ITypeBase).GetRuntimeMethod(nameof(ITypeBase.FindProperty), [typeof(string)])!;

    private static (IEntityType Root, List<IReadOnlyComplexType> ComplexTypes) FindPathToComplexOrEntityType(ITypeBase complexOrEntityType)
    {
        if (complexOrEntityType is IEntityType inputEntityType)
        {
            return (inputEntityType, []);
        }

        var inputComplexType = (IComplexType)complexOrEntityType;
        var declaringType = inputComplexType.ComplexProperty.DeclaringType;
        if (declaringType is IEntityType declaringEntityType)
        {
            return (declaringEntityType, [inputComplexType]);
        }

        var complexTypes = new List<IReadOnlyComplexType>();
        while (declaringType is IComplexType complex)
        {
            complexTypes.Insert(0, complex);
            declaringType = complex.ComplexProperty.DeclaringType;
        }

        complexTypes.Add(inputComplexType);

        return ((IEntityType)declaringType, complexTypes);
    }

    private static Expression BuildEntityOrComplexTypeAccess(this ITypeBase typeBase, ParameterExpression liftableConstantParameter)
    {
        var (rootEntityType, complexTypes) = FindPathToComplexOrEntityType(typeBase);
        var result = default(Expression);

        // TODO: surely, there is a better way
        if (rootEntityType.Model is RuntimeModel runtimeModel
            && runtimeModel.FindAdHocEntityType(rootEntityType.ClrType) == rootEntityType)
        {
            result = Expression.Call(
                Expression.Convert(
                    Expression.Property(
                        Expression.Property(
                            liftableConstantParameter,
                            nameof(MaterializerLiftableConstantContext.Dependencies)),
                        nameof(ShapedQueryCompilingExpressionVisitorDependencies.Model)),
                    typeof(RuntimeModel)),
                RuntimeModelFindAdHocEntiyTypeMethod,
                Expression.Constant(rootEntityType.Name));
        }
        else
        {
            result = Expression.Call(
                Expression.Property(
                    Expression.Property(
                        liftableConstantParameter,
                        nameof(MaterializerLiftableConstantContext.Dependencies)),
                    nameof(ShapedQueryCompilingExpressionVisitorDependencies.Model)),
                ModelFindEntiyTypeMethod,
                Expression.Constant(rootEntityType.Name));
        }

        foreach (var complexType in complexTypes)
        {
            var complexPropertyName = complexType.ComplexProperty.Name;
            result = Expression.Property(
                Expression.Call(result, TypeBaseFindComplexPropertyMethod, Expression.Constant(complexPropertyName)),
                nameof(IComplexProperty.ComplexType));
        }

        return result;
    }

    /// <summary>
    /// TODO
    /// </summary>
    [EntityFrameworkInternal]
    public static Expression<Func<MaterializerLiftableConstantContext, object>> BuildEntityOrComplexTypeAccessLambda(ITypeBase typeBase)
    {
        var liftableConstantParameter = Expression.Parameter(typeof(MaterializerLiftableConstantContext));
        var body = BuildEntityOrComplexTypeAccess(typeBase, liftableConstantParameter);

        return Expression.Lambda<Func<MaterializerLiftableConstantContext, object>>(body, liftableConstantParameter);
    }

    ///// <summary>
    ///// TODO
    ///// </summary>
    //[EntityFrameworkInternal]
    //public static Expression<Func<MaterializerLiftableConstantContext, object>> BuildComplexTypeAccessLambdaForLiftableConstant(this IReadOnlyComplexType complexType)
    //{
    //    var liftableConstantPrm = Expression.Parameter(typeof(MaterializerLiftableConstantContext));

    //    var (rootEntityType, complexProperties) = FindPathToComplexType(complexType);
    //    var entityTypeName = rootEntityType.Name;
    //    Expression typeBase;

    //    // TODO: can we even have unmapped entity type that contains complex types? or can we delete this?
    //    if (rootEntityType.Model is RuntimeModel runtimeModel
    //        && runtimeModel.FindAdHocEntityType(rootEntityType.ClrType) == rootEntityType)
    //    {
    //        typeBase = Expression.Call(
    //            Expression.Convert(
    //                Expression.Property(
    //                    Expression.Property(
    //                        liftableConstantPrm,
    //                        nameof(MaterializerLiftableConstantContext.Dependencies)),
    //                    nameof(ShapedQueryCompilingExpressionVisitorDependencies.Model)),
    //                typeof(RuntimeModel)),
    //            RuntimeModelFindAdHocEntiyTypeMethod,
    //            Expression.Constant(entityTypeName));
    //    }
    //    else
    //    {
    //        typeBase = Expression.Call(
    //            Expression.Property(
    //                Expression.Property(
    //                    liftableConstantPrm,
    //                    nameof(MaterializerLiftableConstantContext.Dependencies)),
    //                nameof(ShapedQueryCompilingExpressionVisitorDependencies.Model)),
    //            ModelFindEntiyTypeMethod,
    //            Expression.Constant(entityTypeName));
    //    }

    //    foreach (var complexProperty in complexProperties)
    //    {
    //        var complexPropertyName = complexProperty.Name;
    //        typeBase = Expression.Property(
    //            Expression.Call(typeBase, TypeBaseFindComplexPropertyMethod, Expression.Constant(complexPropertyName)),
    //            nameof(IComplexProperty.ComplexType));
    //    }

    //    return Expression.Lambda<Func<MaterializerLiftableConstantContext, object>>(
    //        Expression.Property(
    //            Expression.Call(typeBase, TypeBaseFindComplexPropertyMethod, Expression.Constant(complexType.ComplexProperty.Name)),
    //            nameof(IComplexProperty.ComplexType)),
    //        liftableConstantPrm);
    //}
}
