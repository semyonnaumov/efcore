// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Metadata.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public static class PropertyExtensions
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static bool ForAdd(this ValueGenerated valueGenerated)
        => (valueGenerated & ValueGenerated.OnAdd) != 0;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static bool ForUpdate(this ValueGenerated valueGenerated)
        => (valueGenerated & ValueGenerated.OnUpdate) != 0;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static IReadOnlyProperty? FindFirstDifferentPrincipal(this IReadOnlyProperty property)
    {
        var principal = property.FindFirstPrincipal();

        return principal != property ? principal : null;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static IProperty? FindGenerationProperty(this IProperty property)
    {
        var traversalList = new List<IProperty> { property };

        var index = 0;
        while (index < traversalList.Count)
        {
            var currentProperty = traversalList[index];

            if (currentProperty.RequiresValueGenerator())
            {
                return currentProperty;
            }

            foreach (var foreignKey in currentProperty.GetContainingForeignKeys())
            {
                for (var propertyIndex = 0; propertyIndex < foreignKey.Properties.Count; propertyIndex++)
                {
                    if (currentProperty == foreignKey.Properties[propertyIndex])
                    {
                        var nextProperty = foreignKey.PrincipalKey.Properties[propertyIndex];
                        if (!traversalList.Contains(nextProperty))
                        {
                            traversalList.Add(nextProperty);
                        }
                    }
                }
            }

            index++;
        }

        return null;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static bool RequiresValueGenerator(this IReadOnlyProperty property)
        => (property.ValueGenerated.ForAdd()
                && property.IsKey()
                && (!property.IsForeignKey()
                    || property.IsForeignKeyToSelf()
                    || (property.GetContainingForeignKeys().All(fk => fk.Properties.Any(p => p != property && p.IsNullable)))))
            || property.GetValueGeneratorFactory() != null;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static bool IsForeignKeyToSelf(this IReadOnlyProperty property)
    {
        Check.DebugAssert(property.IsKey(), "Only call this method for properties known to be part of a key.");

        foreach (var foreignKey in property.GetContainingForeignKeys())
        {
            var propertyIndex = foreignKey.Properties.IndexOf(property);
            if (propertyIndex == foreignKey.PrincipalKey.Properties.IndexOf(property))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static bool IsKey(this Property property)
        => property.Keys != null;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static bool MayBeStoreGenerated(this IProperty property)
    {
        if (property.ValueGenerated != ValueGenerated.Never
            || property.IsForeignKey())
        {
            return true;
        }

        if (property.IsKey())
        {
            var generationProperty = property.FindGenerationProperty();
            return (generationProperty != null)
                && (generationProperty.ValueGenerated != ValueGenerated.Never);
        }

        return false;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static bool RequiresOriginalValue(this IReadOnlyProperty property)
        => property.DeclaringType.GetChangeTrackingStrategy() != ChangeTrackingStrategy.ChangingAndChangedNotifications
            || property.IsConcurrencyToken
            || property.IsKey()
            || property.IsForeignKey()
            || property.IsUniqueIndex();

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static bool RequiresOriginalValue(this IReadOnlyComplexProperty property)
        => property.ComplexType.ContainingEntityType.GetChangeTrackingStrategy() != ChangeTrackingStrategy.ChangingAndChangedNotifications;

    ///// <summary>
    /////     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    /////     the same compatibility standards as public APIs. It may be changed or removed without notice in
    /////     any release. You should only use it directly in your code with extreme caution and knowing that
    /////     doing so can result in application failures when updating to a new Entity Framework Core release.
    ///// </summary>
    //public static (IEntityType Root, List<IComplexProperty> ComplexProperties) FindPathForPropertyOnComplexType(this IPropertyBase property)
    //{
    //    var declaringType = property.DeclaringType;
    //    if (declaringType is IEntityType declaringEntityType)
    //    {
    //        return (declaringEntityType, []);
    //    }

    //    var complexProperties = new List<IComplexProperty>();
    //    while (declaringType is IComplexType complexType)
    //    {
    //        complexProperties.Add(complexType.ComplexProperty);
    //        declaringType = complexType.ComplexProperty.DeclaringType;
    //    }

    //    complexProperties.Reverse();
    //    return ((IEntityType)declaringType, complexProperties);

    //    //var complexProperties = new List<IComplexProperty>();
    //    //while (declaringType is IComplexType declaringComplexType)
    //    //{
    //    //    complexProperties.Add(declaringComplexType)

    //    //}
    //}

    //private static readonly MethodInfo ModelFindEntiyTypeMethod =
    //    typeof(IModel).GetRuntimeMethod(nameof(IModel.FindEntityType), [typeof(string)])!;

    //private static readonly MethodInfo RuntimeModelFindAdHocEntiyTypeMethod =
    //    typeof(RuntimeModel).GetRuntimeMethod(nameof(RuntimeModel.FindAdHocEntityType), [typeof(string)])!;

    //private static readonly MethodInfo TypeBaseFindComplexPropertyMethod =
    //    typeof(ITypeBase).GetRuntimeMethod(nameof(ITypeBase.FindComplexProperty), [typeof(string)])!;

    //private static readonly MethodInfo TypeBaseFindPropertyMethod =
    //    typeof(ITypeBase).GetRuntimeMethod(nameof(ITypeBase.FindProperty), [typeof(string)])!;

    ///// <summary>
    ///// TODO
    ///// </summary>
    //[EntityFrameworkInternal]
    //public static Expression<Func<MaterializerLiftableConstantContext, object>> BuildPropertyAccessLambdaForLiftableConstant(this IPropertyBase property)
    //{
    //    var liftableConstantPrm = Expression.Parameter(typeof(MaterializerLiftableConstantContext));
    //    var liftableConstantBody = property.BuildPropertyAccessForLiftableConstant(liftableConstantPrm);

    //    return Expression.Lambda<Func<MaterializerLiftableConstantContext, object>>(liftableConstantBody, liftableConstantPrm);
    //}

    ///// <summary>
    ///// TODO
    ///// </summary>
    //[EntityFrameworkInternal]
    //public static Expression BuildPropertyAccessForLiftableConstant(this IPropertyBase property, ParameterExpression materializerLiftableConstantContextParameter)
    //{
    //    var (rootEntityType, complexProperties) = property.FindPathForPropertyOnComplexType();
    //    var entityTypeName = rootEntityType.Name;

    //    Expression typeBase;

    //    // TODO: surely, there is a better way?
    //    if (rootEntityType.Model is RuntimeModel runtimeModel
    //        && runtimeModel.FindAdHocEntityType(rootEntityType.ClrType) == rootEntityType)
    //    {
    //        typeBase = (Expression)Expression.Call(
    //            Expression.Convert(
    //                Expression.Property(
    //                    Expression.Property(
    //                        materializerLiftableConstantContextParameter,
    //                        nameof(MaterializerLiftableConstantContext.Dependencies)),
    //                    nameof(ShapedQueryCompilingExpressionVisitorDependencies.Model)),
    //                typeof(RuntimeModel)),
    //            RuntimeModelFindAdHocEntiyTypeMethod,
    //            Expression.Constant(entityTypeName));
    //    }
    //    else
    //    {
    //        typeBase = (Expression)Expression.Call(
    //            Expression.Property(
    //                Expression.Property(
    //                    materializerLiftableConstantContextParameter,
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

    //    var propertyName = property.Name;

    //    return Expression.Call(typeBase, TypeBaseFindPropertyMethod, Expression.Constant(propertyName));
    //}
}
