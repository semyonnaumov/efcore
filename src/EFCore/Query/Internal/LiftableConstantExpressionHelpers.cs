// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class LiftableConstantExpressionHelpers
{
    private static readonly MethodInfo ModelFindEntiyTypeMethod =
        typeof(IModel).GetRuntimeMethod(nameof(IModel.FindEntityType), [typeof(string)])!;

    private static readonly MethodInfo RuntimeModelFindAdHocEntiyTypeMethod =
        typeof(RuntimeModel).GetRuntimeMethod(nameof(RuntimeModel.FindAdHocEntityType), [typeof(string)])!;

    private static readonly MethodInfo TypeBaseFindComplexPropertyMethod =
        typeof(ITypeBase).GetRuntimeMethod(nameof(ITypeBase.FindComplexProperty), [typeof(string)])!;

    private static readonly MethodInfo TypeBaseFindPropertyMethod =
        typeof(ITypeBase).GetRuntimeMethod(nameof(ITypeBase.FindProperty), [typeof(string)])!;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static (IEntityType Root, List<IComplexProperty> ComplexProperties) FindPathForPropertyOnComplexType(this IPropertyBase property)
    {
        var declaringType = property.DeclaringType;
        if (declaringType is IEntityType declaringEntityType)
        {
            return (declaringEntityType, []);
        }

        var complexProperties = new List<IComplexProperty>();
        while (declaringType is IComplexType complexType)
        {
            complexProperties.Add(complexType.ComplexProperty);
            declaringType = complexType.ComplexProperty.DeclaringType;
        }

        complexProperties.Reverse();

        return ((IEntityType)declaringType, complexProperties);
    }


    /// <summary>
    /// TODO
    /// </summary>
    [EntityFrameworkInternal]
    public static Expression<Func<MaterializerLiftableConstantContext, object>> BuildPropertyAccessLambdaForLiftableConstant(IPropertyBase property)
    {
        var liftableConstantPrm = Expression.Parameter(typeof(MaterializerLiftableConstantContext));
        var liftableConstantBody = BuildPropertyAccessForLiftableConstant(property, liftableConstantPrm);

        return Expression.Lambda<Func<MaterializerLiftableConstantContext, object>>(liftableConstantBody, liftableConstantPrm);
    }

    /// <summary>
    /// TODO
    /// </summary>
    [EntityFrameworkInternal]
    public static Expression BuildPropertyAccessForLiftableConstant(IPropertyBase property, ParameterExpression materializerLiftableConstantContextParameter)
    {
        var (rootEntityType, complexProperties) = FindPathForPropertyOnComplexType(property);
        var entityTypeName = rootEntityType.Name;

        Expression typeBase;

        // TODO: surely, there is a better way?
        if (rootEntityType.Model is RuntimeModel runtimeModel
            && runtimeModel.FindAdHocEntityType(rootEntityType.ClrType) == rootEntityType)
        {
            typeBase = Expression.Call(
                Expression.Convert(
                    Expression.Property(
                        Expression.Property(
                            materializerLiftableConstantContextParameter,
                            nameof(MaterializerLiftableConstantContext.Dependencies)),
                        nameof(ShapedQueryCompilingExpressionVisitorDependencies.Model)),
                    typeof(RuntimeModel)),
                RuntimeModelFindAdHocEntiyTypeMethod,
                Expression.Constant(entityTypeName));
        }
        else
        {
            typeBase = Expression.Call(
                Expression.Property(
                    Expression.Property(
                        materializerLiftableConstantContextParameter,
                        nameof(MaterializerLiftableConstantContext.Dependencies)),
                    nameof(ShapedQueryCompilingExpressionVisitorDependencies.Model)),
                ModelFindEntiyTypeMethod,
                Expression.Constant(entityTypeName));
        }


        foreach (var complexProperty in complexProperties)
        {
            var complexPropertyName = complexProperty.Name;
            typeBase = Expression.Property(
                Expression.Call(typeBase, TypeBaseFindComplexPropertyMethod, Expression.Constant(complexPropertyName)),
                nameof(IComplexProperty.ComplexType));
        }

        var propertyName = property.Name;

        return Expression.Call(typeBase, TypeBaseFindPropertyMethod, Expression.Constant(propertyName));
    }
}
