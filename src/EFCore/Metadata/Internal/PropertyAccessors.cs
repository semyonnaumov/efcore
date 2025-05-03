// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

namespace Microsoft.EntityFrameworkCore.Metadata.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
// Sealed for perf
public sealed class PropertyAccessors<TEntity, TStructural, TValue> : IPropertyAccessors
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public PropertyAccessors(
        IPropertyBase propertyBase,
        Delegate currentValueGetter,
        Delegate preStoreGeneratedCurrentValueGetter,
        Delegate? originalValueGetter,
        Delegate relationshipSnapshotGetter,
        Delegate getClrValueUsingContainingEntityDelegate,
        Delegate hasSentinelUsingContainingEntityDelegate,
        Delegate setClrValueUsingContainingEntityDelegate,
        Delegate setClrValueForMaterializationUsingContainingEntityDelegate,
        Delegate getClrValueDelegate,
        Delegate hasSentinelDelegate,
        Delegate setClrValueDelegate,
        Delegate setClrValueDelegateForMaterialization)
    {
        Property = propertyBase;
        CurrentValueGetter = currentValueGetter;
        PreStoreGeneratedCurrentValueGetter = preStoreGeneratedCurrentValueGetter;
        OriginalValueGetter = originalValueGetter;
        RelationshipSnapshotGetter = relationshipSnapshotGetter;
        GetClrValueUsingContainingEntityDelegate = getClrValueUsingContainingEntityDelegate;
        HasSentinelUsingContainingEntityDelegate = hasSentinelUsingContainingEntityDelegate;
        SetClrValueUsingContainingEntityDelegate = setClrValueUsingContainingEntityDelegate;
        SetClrValueForMaterializationUsingContainingEntityDelegate = setClrValueForMaterializationUsingContainingEntityDelegate;
        GetClrValueDelegate = getClrValueDelegate;
        HasSentinelDelegate = hasSentinelDelegate;
        SetClrValueDelegate = setClrValueDelegate;
        SetClrValueForMaterializationDelegate = setClrValueDelegateForMaterialization;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public IPropertyBase Property { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public Delegate CurrentValueGetter { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public Delegate PreStoreGeneratedCurrentValueGetter { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public Delegate? OriginalValueGetter { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public Delegate RelationshipSnapshotGetter { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public Delegate GetClrValueUsingContainingEntityDelegate { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public Delegate HasSentinelUsingContainingEntityDelegate { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public Delegate SetClrValueUsingContainingEntityDelegate { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public Delegate SetClrValueForMaterializationUsingContainingEntityDelegate { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public Delegate GetClrValueDelegate { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public Delegate HasSentinelDelegate { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public Delegate SetClrValueDelegate { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public Delegate SetClrValueForMaterializationDelegate { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public TProperty GetCurrentValue<TProperty>(IInternalEntry entry)
        => ((Func<IInternalEntry, TProperty>)CurrentValueGetter)(entry);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public TProperty GetPreStoreGeneratedCurrentValue<TProperty>(IInternalEntry entry)
        => ((Func<IInternalEntry, TProperty>)PreStoreGeneratedCurrentValueGetter)(entry);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public TProperty GetOriginalValue<TProperty>(IInternalEntry entry)
        => OriginalValueGetter == null
            ? throw new InvalidOperationException($"Original values are not stored for the property '{entry.StructuralType.DisplayName()}'.'{Property.Name}'")
            : ((Func<IInternalEntry, TProperty>)OriginalValueGetter)(entry);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public TProperty GetRelationshipSnapshot<TProperty>(IInternalEntry entry)
        => ((Func<IInternalEntry, TProperty>)RelationshipSnapshotGetter)(entry);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public TValue GetClrValueUsingContainingEntity<TEntity, TValue>(TEntity entity, ReadOnlySpan<int> indices)
        => ((Func<TEntity, ReadOnlySpan<int>, TValue>)GetClrValueUsingContainingEntityDelegate)(entity, indices);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public bool HasSentinelUsingContainingEntity<TEntity>(TEntity entity, ReadOnlySpan<int> indices)
        => ((Func<TEntity, ReadOnlySpan<int>, bool>)HasSentinelUsingContainingEntityDelegate)(entity, indices);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public void SetClrValueUsingContainingEntity<TEntity, TValue>(TEntity entity, ReadOnlySpan<int> indices, TValue value, bool forMaterialization)
        => (forMaterialization
        ? ((Action<TEntity, ReadOnlySpan<int>, TValue>)SetClrValueForMaterializationUsingContainingEntityDelegate)
        : ((Action<TEntity, ReadOnlySpan<int>, TValue>)SetClrValueUsingContainingEntityDelegate))(entity, indices, value);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public TValue GetClrValue<TStructural, TValue>(TStructural structuralObject)
        => ((Func<TStructural, TValue>)GetClrValueDelegate)(structuralObject);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public bool HasSentinel<TStructural>(TStructural structuralObject)
        => ((Func<TStructural, bool>)HasSentinelDelegate)(structuralObject);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public void SetClrValue<TStructural, TValue>(TStructural instance, TValue value, bool forMaterialization)
        => (forMaterialization
        ? ((Action<TStructural, TValue>)SetClrValueForMaterializationDelegate)
        : ((Action<TStructural, TValue>)SetClrValueDelegate))(instance, value);
}
