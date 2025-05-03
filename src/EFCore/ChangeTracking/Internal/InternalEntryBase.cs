// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

public abstract partial class InternalEntryBase : IInternalEntry
{
    private OriginalValues _originalValues;
    private SidecarValues _temporaryValues;
    private SidecarValues _storeGeneratedValues;
    private StateData _stateData;
    private readonly ISnapshot _shadowValues;
    private readonly ComplexCollections _complexCollections;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public InternalEntryBase(IRuntimeTypeBase structuralType)
    {
        StructuralType = structuralType;
        _shadowValues = structuralType.EmptyShadowValuesFactory();
        PropertyStateData = new StateData(structuralType.PropertyCount, structuralType.NavigationCount);
        _complexCollections = new ComplexCollections(this);

        foreach (var property in structuralType.GetFlattenedProperties())
        {
            if (property.IsShadowProperty())
            {
                PropertyStateData.FlagProperty(property.GetIndex(), PropertyFlag.Unknown, true);
            }
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public InternalEntryBase(IRuntimeTypeBase structuralType, in ISnapshot shadowValues)
    {
        StructuralType = structuralType;
        _shadowValues = shadowValues;
        PropertyStateData = new StateData(structuralType.PropertyCount, structuralType.NavigationCount);
        _complexCollections = new ComplexCollections(this);
    }
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public InternalEntryBase(
        IRuntimeTypeBase structuralType,
        IDictionary<string, object?> shadowValues)
    {
        StructuralType = structuralType;
        _shadowValues = structuralType.ShadowValuesFactory(shadowValues);
        PropertyStateData = new StateData(structuralType.PropertyCount, structuralType.NavigationCount);
        _complexCollections = new ComplexCollections(this);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public IRuntimeTypeBase StructuralType { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public DbContext Context
        => StateManager.Context;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public abstract IStateManager StateManager { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual InternalEntryBase ContainingEntry => this;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual ReadOnlySpan<int> Ordinals => Span<int>.Empty;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual InternalEntityEntry EntityEntry => (InternalEntityEntry)this;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public EntityState EntityState
    {
        get => PropertyStateData.EntityState;

        protected set => PropertyStateData.EntityState = value;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected ref StateData PropertyStateData => ref _stateData;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual IEnumerable<InternalComplexEntry> GetFlattenedComplexEntries()
        => _complexCollections.SelectMany(c => c);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual void SetEntityState(
        EntityState entityState,
        bool acceptChanges = false,
        bool modifyProperties = true,
        EntityState? forceStateWhenUnknownKey = null,
        EntityState? fallbackState = null)
    {
        PrepareForAdd(entityState);

        SetEntityState(PropertyStateData.EntityState, entityState, acceptChanges, modifyProperties);

        foreach (var complexEntry in GetFlattenedComplexEntries())
        {
            complexEntry.SetEntityState(entityState, acceptChanges, modifyProperties);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual Task SetEntityStateAsync(
        EntityState entityState,
        bool acceptChanges = false,
        bool modifyProperties = true,
        EntityState? forceStateWhenUnknownKey = null,
        EntityState? fallbackState = null,
        CancellationToken cancellationToken = default)
    {
        PrepareForAdd(entityState);

        SetEntityState(PropertyStateData.EntityState, entityState, acceptChanges, modifyProperties);

        foreach (var complexEntry in GetFlattenedComplexEntries())
        {
            complexEntry.SetEntityState(entityState, acceptChanges, modifyProperties);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual bool PrepareForAdd(EntityState newState)
    {
        if (newState != EntityState.Added
            || EntityState == EntityState.Added)
        {
            return false;
        }

        if (EntityState == EntityState.Modified)
        {
            PropertyStateData.FlagAllProperties(
                StructuralType.PropertyCount, PropertyFlag.Modified,
                flagged: false);
        }

        // Temporarily change the internal state to unknown so that key generation, including setting key values
        // can happen without constraints on changing read-only values kicking in
        PropertyStateData.EntityState = EntityState.Detached;

        return true;
    }


    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual void SetEntityState(EntityState oldState, EntityState newState, bool acceptChanges, bool modifyProperties)
    {
        var structuralType = StructuralType;

        // Prevent temp values from becoming permanent values
        if (oldState == EntityState.Added
            && newState != EntityState.Added
            && newState != EntityState.Detached)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var property in structuralType.GetFlattenedProperties())
            {
                if (property.IsKey() && HasTemporaryValue(property))
                {
                    throw new InvalidOperationException(
                        CoreStrings.TempValuePersists(
                            property.Name,
                            structuralType.DisplayName(), newState));
                }
            }
        }

        // The entity state can be Modified even if some properties are not modified so always
        // set all properties to modified if the entity state is explicitly set to Modified.
        if (newState == EntityState.Modified
            && modifyProperties)
        {
            PropertyStateData.FlagAllProperties(StructuralType.PropertyCount, PropertyFlag.Modified, flagged: true);

            // Hot path; do not use LINQ
            foreach (IRuntimeProperty property in structuralType.GetFlattenedProperties())
            {
                if (property.GetAfterSaveBehavior() != PropertySaveBehavior.Save)
                {
                    PropertyStateData.FlagProperty(property.GetIndex(), PropertyFlag.Modified, isFlagged: false);
                }
            }
        }

        if (oldState == newState)
        {
            return;
        }

        if (newState == EntityState.Unchanged)
        {
            PropertyStateData.FlagAllProperties(
                StructuralType.PropertyCount, PropertyFlag.Modified,
                flagged: false);
        }

        if (PropertyStateData.EntityState != oldState)
        {
            PropertyStateData.EntityState = oldState;
        }

        OnStateChanging(newState);

        if (newState == EntityState.Unchanged
            && EntityState == EntityState.Modified)
        {
            if (acceptChanges)
            {
                _originalValues.AcceptChanges(this);
            }
            else
            {
                _originalValues.RejectChanges(this);
            }
        }

        SetServiceProperties(oldState, newState);

        PropertyStateData.EntityState = newState;

        if (newState is EntityState.Deleted or EntityState.Detached
            && HasConceptualNull)
        {
            PropertyStateData.FlagAllProperties(StructuralType.PropertyCount, PropertyFlag.Null, flagged: false);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected abstract void SetServiceProperties(EntityState oldState, EntityState newState);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual void OnStateChanging(EntityState newState)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual void OnStateChanged(EntityState oldState)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public bool IsModified(IProperty property)
    {
        var propertyIndex = property.GetIndex();

        return PropertyStateData.EntityState == EntityState.Modified
            && PropertyStateData.IsPropertyFlagged(propertyIndex, PropertyFlag.Modified)
            && !PropertyStateData.IsPropertyFlagged(propertyIndex, PropertyFlag.Unknown);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public bool IsUnknown(IProperty property)
        => PropertyStateData.IsPropertyFlagged(property.GetIndex(), PropertyFlag.Unknown);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public void SetPropertyModified(
        IProperty property,
        bool changeState = true,
        bool isModified = true,
        bool isConceptualNull = false,
        bool acceptChanges = false)
    {
        var propertyIndex = property.GetIndex();
        PropertyStateData.FlagProperty(propertyIndex, PropertyFlag.Unknown, false);

        var currentState = PropertyStateData.EntityState;

        if (currentState is EntityState.Added or EntityState.Detached
            || !changeState)
        {
            var index = property.GetOriginalValueIndex();
            if (index != -1 && !IsConceptualNull(property))
            {
                SetOriginalValue(property, this[property], index);
            }

            if (currentState == EntityState.Added)
            {
                if (FlaggedAsTemporary(propertyIndex)
                    && !FlaggedAsStoreGenerated(propertyIndex)
                    && !HasSentinelValue(property))
                {
                    PropertyStateData.FlagProperty(propertyIndex, PropertyFlag.IsTemporary, false);
                }

                return;
            }
        }

        if (changeState
            && !isConceptualNull
            && isModified
            && !StateManager.SavingChanges
            && property.IsKey()
            && property.GetAfterSaveBehavior() == PropertySaveBehavior.Throw)
        {
            throw new InvalidOperationException(CoreStrings.KeyReadOnly(property.Name, StructuralType.DisplayName()));
        }

        if (currentState == EntityState.Deleted)
        {
            return;
        }

        if (changeState)
        {
            if (!isModified
                && currentState != EntityState.Detached
                && property.GetOriginalValueIndex() != -1)
            {
                if (acceptChanges)
                {
                    SetOriginalValue(property, GetCurrentValue(property));
                }

                SetProperty(property, GetOriginalValue(property), isMaterialization: false, setModified: false);
            }

            PropertyStateData.FlagProperty(propertyIndex, PropertyFlag.Modified, isModified);
        }

        if (isModified
            && currentState is EntityState.Unchanged or EntityState.Detached)
        {
            if (changeState)
            {
                OnStateChanging(EntityState.Modified);

                SetServiceProperties(currentState, EntityState.Modified);

                PropertyStateData.EntityState = EntityState.Modified;

                OnStateChanged(currentState);
            }
        }
        else if (currentState == EntityState.Modified
                 && changeState
                 && !isModified
                 && !PropertyStateData.AnyPropertiesFlagged(PropertyFlag.Modified))
        {
            OnStateChanging(EntityState.Unchanged);
            PropertyStateData.EntityState = EntityState.Unchanged;
            OnStateChanged(currentState);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public void OnComplexPropertyModified(IComplexProperty property, bool isModified = true)
    {
        var currentState = PropertyStateData.EntityState;
        if (currentState == EntityState.Deleted)
        {
            return;
        }

        if (isModified
            && currentState is EntityState.Unchanged or EntityState.Detached)
        {
            PropertyStateData.EntityState = EntityState.Modified;
        }
        else if (currentState == EntityState.Modified
                 && !isModified
                 && !PropertyStateData.AnyPropertiesFlagged(PropertyFlag.Modified)
                 && GetFlattenedComplexEntries().All(e => e.EntityState == EntityState.Unchanged))
        {
            PropertyStateData.EntityState = EntityState.Unchanged;
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public bool HasConceptualNull
        => PropertyStateData.AnyPropertiesFlagged(PropertyFlag.Null);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public bool IsConceptualNull(IProperty property)
        => PropertyStateData.IsPropertyFlagged(property.GetIndex(), PropertyFlag.Null);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public bool HasTemporaryValue(IProperty property)
        => GetValueType(property) == CurrentValueType.Temporary;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected CurrentValueType GetValueType(IProperty property)
        => PropertyStateData.IsPropertyFlagged(property.GetIndex(), PropertyFlag.IsStoreGenerated)
            ? CurrentValueType.StoreGenerated
            : PropertyStateData.IsPropertyFlagged(property.GetIndex(), PropertyFlag.IsTemporary)
                ? CurrentValueType.Temporary
                : CurrentValueType.Normal;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public void SetTemporaryValue(IProperty property, object? value, bool setModified = true)
    {
        if (property.GetStoreGeneratedIndex() == -1)
        {
            throw new InvalidOperationException(
                CoreStrings.TempValue(property.Name, StructuralType.DisplayName()));
        }

        SetProperty(property, value, isMaterialization: false, setModified, isCascadeDelete: false, CurrentValueType.Temporary);
        PropertyStateData.FlagProperty(property.GetIndex(), PropertyFlag.IsTemporary, true);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public void MarkAsTemporary(IProperty property, bool temporary)
        => PropertyStateData.FlagProperty(property.GetIndex(), PropertyFlag.IsTemporary, temporary);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static readonly MethodInfo FlaggedAsTemporaryMethod
        = typeof(IInternalEntry).GetMethod(nameof(IInternalEntry.FlaggedAsTemporary))!;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static readonly MethodInfo FlaggedAsStoreGeneratedMethod
        = typeof(IInternalEntry).GetMethod(nameof(IInternalEntry.FlaggedAsStoreGenerated))!;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public void SetStoreGeneratedValue(IProperty property, object? value, bool setModified = true)
    {
        if (property.GetStoreGeneratedIndex() == -1)
        {
            throw new InvalidOperationException(
                CoreStrings.StoreGenValue(property.Name, StructuralType.DisplayName()));
        }

        SetProperty(
            property,
            value,
            isMaterialization: false,
            setModified,
            isCascadeDelete: false,
            CurrentValueType.StoreGenerated);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public void MarkUnknown(IProperty property)
        => PropertyStateData.FlagProperty(property.GetIndex(), PropertyFlag.Unknown, true);

    internal static MethodInfo MakeReadShadowValueMethod(Type type)
        => typeof(IInternalEntry).GetMethod(nameof(IInternalEntry.ReadShadowValue))!
            .MakeGenericMethod(type);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public T ReadShadowValue<T>(int shadowIndex)
        => _shadowValues.GetValue<T>(shadowIndex);

    private static readonly MethodInfo ReadOriginalValueMethod
        = typeof(IInternalEntry).GetMethod(nameof(IInternalEntry.ReadOriginalValue))!;

    [UnconditionalSuppressMessage(
        "ReflectionAnalysis", "IL2060",
        Justification = "MakeGenericMethod wrapper, see https://github.com/dotnet/linker/issues/2482")]
    internal static MethodInfo MakeReadOriginalValueMethod(Type type)
        => ReadOriginalValueMethod.MakeGenericMethod(type);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public T ReadOriginalValue<T>(IProperty property, int originalValueIndex)
        => _originalValues.GetValue<T>(this, property, originalValueIndex);

    [UnconditionalSuppressMessage(
        "ReflectionAnalysis", "IL2060",
        Justification = "MakeGenericMethod wrapper, see https://github.com/dotnet/linker/issues/2482")]
    internal static MethodInfo MakeReadStoreGeneratedValueMethod(Type type)
        => ReadStoreGeneratedValueMethod.MakeGenericMethod(type);

    private static readonly MethodInfo ReadStoreGeneratedValueMethod
        = typeof(IInternalEntry).GetMethod(nameof(IInternalEntry.ReadStoreGeneratedValue))!;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public T ReadStoreGeneratedValue<T>(int storeGeneratedIndex)
        => _storeGeneratedValues.GetValue<T>(storeGeneratedIndex);

    private static readonly MethodInfo ReadTemporaryValueMethod
        = typeof(IInternalEntry).GetMethod(nameof(IInternalEntry.ReadTemporaryValue))!;

    [UnconditionalSuppressMessage(
        "ReflectionAnalysis", "IL2060",
        Justification = "MakeGenericMethod wrapper, see https://github.com/dotnet/linker/issues/2482")]
    internal static MethodInfo MakeReadTemporaryValueMethod(Type type)
        => ReadTemporaryValueMethod.MakeGenericMethod(type);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public T ReadTemporaryValue<T>(int storeGeneratedIndex)
        => _temporaryValues.GetValue<T>(storeGeneratedIndex);

    private static readonly MethodInfo GetCurrentValueMethod
        = typeof(IInternalEntry).GetMethods().Single(m => m.IsGenericMethod && m.Name == nameof(IInternalEntry.GetCurrentValue));

    [UnconditionalSuppressMessage(
        "ReflectionAnalysis", "IL2060",
        Justification = "MakeGenericMethod wrapper, see https://github.com/dotnet/linker/issues/2482")]
    internal static MethodInfo MakeGetCurrentValueMethod(Type type)
        => GetCurrentValueMethod.MakeGenericMethod(type);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public TProperty GetCurrentValue<TProperty>(IPropertyBase propertyBase)
        => ((Func<IInternalEntry, TProperty>)propertyBase.GetPropertyAccessors().CurrentValueGetter)(this);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public TProperty GetOriginalValue<TProperty>(IProperty property)
        => ((Func<IInternalEntry, TProperty>)property.GetPropertyAccessors().OriginalValueGetter!)(this);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public object? ReadPropertyValue(IPropertyBase propertyBase)
        => propertyBase.IsShadowProperty()
            ? _shadowValues[propertyBase.GetShadowIndex()]
            : propertyBase.GetPropertyAccessors().GetClrValueUsingContainingEntity(EntityEntry.Entity, Ordinals);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    private void WritePropertyValue(
        IPropertyBase propertyBase,
        object? value,
        bool forMaterialization)
    {
        if (propertyBase.IsShadowProperty())
        {
            _shadowValues[propertyBase.GetShadowIndex()] = value;
        }
        else
        {
            propertyBase.GetPropertyAccessors().SetClrValueUsingContainingEntity(EntityEntry.Entity, Ordinals, value, forMaterialization);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public object? GetCurrentValue(IPropertyBase propertyBase)
        => propertyBase is not IProperty property || !IsConceptualNull(property)
            ? this[propertyBase]
            : null;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public object? GetPreStoreGeneratedCurrentValue(IPropertyBase propertyBase)
        => propertyBase is not IProperty property || !IsConceptualNull(property)
            ? ReadPropertyValue(propertyBase)
            : null;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public object? GetOriginalValue(IPropertyBase propertyBase)
        => _originalValues.GetValue(this, (IProperty)propertyBase);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public bool CanHaveOriginalValue(IPropertyBase propertyBase)
        => propertyBase.GetOriginalValueIndex() >= 0;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public void SetOriginalValue(
        IPropertyBase propertyBase,
        object? value,
        int index = -1)
    {
        EnsureOriginalValues();

        var property = (IProperty)propertyBase;

        _originalValues.SetValue(property, value, index);

        // If setting the original value results in the current value being different from the
        // original value, then mark the property as modified.
        if ((EntityState == EntityState.Unchanged
                || (EntityState == EntityState.Modified && !IsModified(property)))
            && !PropertyStateData.IsPropertyFlagged(property.GetIndex(), PropertyFlag.Unknown))
        {
            ((StateManager as StateManager)?.ChangeDetector as ChangeDetector)?.DetectValueChange(this, property);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public void EnsureOriginalValues()
    {
        if (_originalValues.IsEmpty)
        {
            _originalValues = new OriginalValues(this);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public void EnsureTemporaryValues()
    {
        if (_temporaryValues.IsEmpty)
        {
            _temporaryValues = new SidecarValues(StructuralType.TemporaryValuesFactory(this));
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public void EnsureStoreGeneratedValues()
    {
        if (_storeGeneratedValues.IsEmpty)
        {
            _storeGeneratedValues = new SidecarValues(StructuralType.StoreGeneratedValuesFactory());
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public bool HasOriginalValuesSnapshot
        => !_originalValues.IsEmpty;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public IInternalEntry GetComplexCollectionEntry(IComplexProperty property, int ordinal)
        => _complexCollections.GetEntry(this, property, ordinal);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public object? this[IPropertyBase propertyBase]
    {
        get
        {
            var storeGeneratedIndex = propertyBase.GetStoreGeneratedIndex();
            if (storeGeneratedIndex != -1)
            {
                var property = (IProperty)propertyBase;
                var propertyIndex = property.GetIndex();

                if (FlaggedAsStoreGenerated(propertyIndex))
                {
                    return _storeGeneratedValues.GetValue(storeGeneratedIndex);
                }

                if (FlaggedAsTemporary(propertyIndex)
                    && HasSentinelValue(property))
                {
                    return _temporaryValues.GetValue(storeGeneratedIndex);
                }
            }

            return ReadPropertyValue(propertyBase);
        }

        set => SetProperty(propertyBase, value, isMaterialization: false);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public bool FlaggedAsStoreGenerated(int propertyIndex)
        => !_storeGeneratedValues.IsEmpty
            && PropertyStateData.IsPropertyFlagged(propertyIndex, PropertyFlag.IsStoreGenerated);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public bool FlaggedAsTemporary(int propertyIndex)
        => !_temporaryValues.IsEmpty
            && PropertyStateData.IsPropertyFlagged(propertyIndex, PropertyFlag.IsTemporary);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public void SetProperty(
        IPropertyBase propertyBase,
        object? value,
        bool isMaterialization,
        bool setModified = true,
        bool isCascadeDelete = false)
        => SetProperty(propertyBase, value, isMaterialization, setModified, isCascadeDelete, CurrentValueType.Normal);

    private void SetProperty(
        IPropertyBase propertyBase,
        object? value,
        bool isMaterialization,
        bool setModified,
        bool isCascadeDelete,
        CurrentValueType valueType)
    {
        var currentValue = ReadPropertyValue(propertyBase);

        var asProperty = propertyBase as IProperty;
        int propertyIndex;
        CurrentValueType currentValueType;
        int storeGeneratedIndex;
        bool valuesEqual;

        if (asProperty != null)
        {
            propertyIndex = asProperty.GetIndex();
            valuesEqual = AreEqual(currentValue, value, asProperty);
            currentValueType = GetValueType(asProperty);
            storeGeneratedIndex = asProperty.GetStoreGeneratedIndex();
        }
        else
        {
            propertyIndex = -1;
            valuesEqual = ReferenceEquals(currentValue, value);
            currentValueType = CurrentValueType.Normal;
            storeGeneratedIndex = -1;
        }

        if (!valuesEqual
            || (propertyIndex != -1
                && (PropertyStateData.IsPropertyFlagged(propertyIndex, PropertyFlag.Unknown)
                    || PropertyStateData.IsPropertyFlagged(propertyIndex, PropertyFlag.Null)
                    || valueType != currentValueType)))
        {
            var writeValue = true;

            if (asProperty != null
                && valueType == CurrentValueType.Normal
                && (!asProperty.ClrType.IsNullableType()
                    || asProperty.GetContainingForeignKeys().Any(
                        fk => fk is { IsRequired: true, DeleteBehavior: DeleteBehavior.Cascade or DeleteBehavior.ClientCascade }
                            && fk.DeclaringEntityType.IsAssignableFrom(ComplexType))))
            {
                if (value == null)
                {
                    HandleNullForeignKey(asProperty, setModified, isCascadeDelete);
                    writeValue = false;
                }
                else
                {
                    PropertyStateData.FlagProperty(propertyIndex, PropertyFlag.Null, isFlagged: false);
                }
            }

            if (writeValue)
            {
                StateManager.InternalEntityEntryNotifier.PropertyChanging(this, propertyBase);

                if (storeGeneratedIndex == -1)
                {
                    WritePropertyValue(propertyBase, value, isMaterialization);
                }
                else
                {
                    switch (valueType)
                    {
                        case CurrentValueType.Normal:
                            WritePropertyValue(propertyBase, value, isMaterialization);
                            PropertyStateData.FlagProperty(propertyIndex, PropertyFlag.IsTemporary, isFlagged: false);
                            PropertyStateData.FlagProperty(propertyIndex, PropertyFlag.IsStoreGenerated, isFlagged: false);
                            break;
                        case CurrentValueType.StoreGenerated:
                            EnsureStoreGeneratedValues();
                            _storeGeneratedValues.SetValue(asProperty!, value, storeGeneratedIndex);
                            PropertyStateData.FlagProperty(propertyIndex, PropertyFlag.IsStoreGenerated, isFlagged: true);
                            break;
                        case CurrentValueType.Temporary:
                            EnsureTemporaryValues();
                            _temporaryValues.SetValue(asProperty!, value, storeGeneratedIndex);
                            PropertyStateData.FlagProperty(propertyIndex, PropertyFlag.IsTemporary, isFlagged: true);
                            PropertyStateData.FlagProperty(propertyIndex, PropertyFlag.IsStoreGenerated, isFlagged: false);
                            if (!HasSentinelValue(asProperty!))
                            {
                                WritePropertyValue(propertyBase, value, isMaterialization);
                            }

                            break;
                        default:
                            Check.DebugFail($"Bad value type {valueType}");
                            break;
                    }
                }

                if (propertyIndex != -1)
                {
                    if (PropertyStateData.IsPropertyFlagged(propertyIndex, PropertyFlag.Unknown))
                    {
                        if (!_originalValues.IsEmpty)
                        {
                            SetOriginalValue(propertyBase, value);
                        }

                        PropertyStateData.FlagProperty(propertyIndex, PropertyFlag.Unknown, isFlagged: false);
                    }
                }

                OnPropertyChanged(propertyBase, value, setModified);
            }
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual void OnPropertyChanged(IPropertyBase propertyBase, object? value, bool setModified)
    {
        StateManager.InternalEntityEntryNotifier.PropertyChanged(this, propertyBase, setModified);
    }


    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public void HandleNullForeignKey(
        IProperty property,
        bool setModified = false,
        bool isCascadeDelete = false)
    {
        if (EntityState != EntityState.Deleted
            && EntityState != EntityState.Detached)
        {
            PropertyStateData.FlagProperty(property.GetIndex(), PropertyFlag.Null, isFlagged: true);

            if (setModified)
            {
                SetPropertyModified(
                    property, changeState: true, isModified: true,
                    isConceptualNull: true);
            }

            if (!isCascadeDelete
                && StateManager.DeleteOrphansTiming == CascadeTiming.Immediate)
            {
                HandleConceptualNulls(
                    StateManager.SensitiveLoggingEnabled,
                    force: false,
                    isCascadeDelete: false);
            }
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual void HandleConceptualNulls(bool sensitiveLoggingEnabled, bool force, bool isCascadeDelete)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public void AcceptChanges()
    {
        if (!_storeGeneratedValues.IsEmpty)
        {
            foreach (var property in EntityType.GetFlattenedProperties())
            {
                var storeGeneratedIndex = property.GetStoreGeneratedIndex();
                if (storeGeneratedIndex != -1
                    && PropertyStateData.IsPropertyFlagged(property.GetIndex(), PropertyFlag.IsStoreGenerated)
                    && _storeGeneratedValues.TryGetValue(storeGeneratedIndex, out var value))
                {
                    this[property] = value;
                }
            }

            _storeGeneratedValues = new SidecarValues();
            _temporaryValues = new SidecarValues();
        }

        PropertyStateData.FlagAllProperties(EntityType.PropertyCount, PropertyFlag.IsStoreGenerated, false);
        PropertyStateData.FlagAllProperties(EntityType.PropertyCount, PropertyFlag.IsTemporary, false);
        PropertyStateData.FlagAllProperties(EntityType.PropertyCount, PropertyFlag.Unknown, false);

        var currentState = EntityState;
        switch (currentState)
        {
            case EntityState.Unchanged:
            case EntityState.Detached:
                return;
            case EntityState.Added:
            case EntityState.Modified:
                _originalValues.AcceptChanges(this);
                SharedIdentityEntry?.AcceptChanges();

                SetEntityState(EntityState.Unchanged, true);
                break;
            case EntityState.Deleted:
                SetEntityState(EntityState.Detached);
                break;
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public IInternalEntry PrepareToSave()
    {
        var structuralType = StructuralType;

        if (EntityState == EntityState.Added)
        {
            foreach (var property in structuralType.GetFlattenedProperties())
            {
                if (property.GetBeforeSaveBehavior() == PropertySaveBehavior.Throw
                    && !HasTemporaryValue(property)
                    && HasExplicitValue(property))
                {
                    throw new InvalidOperationException(
                        CoreStrings.PropertyReadOnlyBeforeSave(
                            property.Name,
                            structuralType.DisplayName()));
                }

                if (property.IsKey()
                    && property.IsForeignKey()
                    && PropertyStateData.IsPropertyFlagged(property.GetIndex(), PropertyFlag.Unknown)
                    && !IsStoreGenerated(property))
                {
                    if (property.GetContainingForeignKeys().Any(fk => fk.IsOwnership))
                    {
                        throw new InvalidOperationException(CoreStrings.SaveOwnedWithoutOwner(structuralType.DisplayName()));
                    }

                    throw new InvalidOperationException(CoreStrings.UnknownKeyValue(structuralType.DisplayName(), property.Name));
                }

                CheckForNullCollection(property);
            }

            CheckForNullComplexProperties();
        }
        else if (EntityState == EntityState.Modified)
        {
            foreach (var property in structuralType.GetFlattenedProperties())
            {
                if (property.GetAfterSaveBehavior() == PropertySaveBehavior.Throw
                    && IsModified(property))
                {
                    throw new InvalidOperationException(
                        CoreStrings.PropertyReadOnlyAfterSave(
                            property.Name,
                            StructuralType.DisplayName()));
                }

                CheckForNullCollection(property);
                CheckForUnknownKey(property);
            }

            CheckForNullComplexProperties();
        }
        else if (EntityState == EntityState.Deleted)
        {
            foreach (var property in structuralType.GetFlattenedProperties())
            {
                CheckForUnknownKey(property);
            }
        }

        DiscardStoreGeneratedValues();

        return this;

        void CheckForUnknownKey(IProperty property)
        {
            if (property.IsKey()
                && PropertyStateData.IsPropertyFlagged(property.GetIndex(), PropertyFlag.Unknown))
            {
                throw new InvalidOperationException(CoreStrings.UnknownShadowKeyValue(structuralType.DisplayName(), property.Name));
            }
        }

        void CheckForNullCollection(IProperty property)
        {
            if (property.GetElementType() != null
                && !property.IsNullable
                && GetCurrentValue(property) == null)
            {
                throw new InvalidOperationException(
                    CoreStrings.NullRequiredPrimitiveCollection(structuralType.DisplayName(), property.Name));
            }
        }

        void CheckForNullComplexProperties()
        {
            foreach (var complexProperty in structuralType.GetFlattenedComplexProperties())
            {
                if (!complexProperty.IsNullable
                    && !complexProperty.IsCollection
                    && this[complexProperty] == null
                    && complexProperty.ComplexType.GetProperties().Any(p => !p.IsNullable))
                {
                    throw new InvalidOperationException(
                        CoreStrings.NullRequiredComplexProperty(
                            complexProperty.DeclaringType.ClrType.ShortDisplayName(), complexProperty.Name));
                }
            }
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public void DiscardStoreGeneratedValues()
    {
        if (!_storeGeneratedValues.IsEmpty)
        {
            _storeGeneratedValues = new SidecarValues();
            PropertyStateData.FlagAllProperties(StructuralType.PropertyCount, PropertyFlag.IsStoreGenerated, false);
        }

        foreach (var complexEntry in _complexCollections)
        {
            complexEntry.DiscardStoreGeneratedValues();
        }
    }

    private static bool AreEqual(object? value, object? otherValue, IProperty property)
        => property.GetValueComparer().Equals(value, otherValue);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public bool IsStoreGenerated(IProperty property)
        => (property.ValueGenerated.ForAdd()
                && EntityState == EntityState.Added
                && ((property.GetBeforeSaveBehavior() == PropertySaveBehavior.Ignore
                        && GetValueType(property) != CurrentValueType.StoreGenerated)
                    || HasTemporaryValue(property)
                    || !HasExplicitValue(property)))
            || (property.ValueGenerated.ForUpdate()
                && (EntityState is EntityState.Modified or EntityState.Deleted)
                && (property.GetAfterSaveBehavior() == PropertySaveBehavior.Ignore
                    || !IsModified(property)));

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasExplicitValue(IProperty property)
        => !HasSentinelValue(property)
            || PropertyStateData.IsPropertyFlagged(property.GetIndex(), PropertyFlag.IsStoreGenerated)
            || PropertyStateData.IsPropertyFlagged(property.GetIndex(), PropertyFlag.IsTemporary);

    private bool HasSentinelValue(IProperty property)
        => property.IsShadowProperty()
            ? AreEqual(_shadowValues[property.GetShadowIndex()], property.Sentinel, property)
            : property.GetGetter().HasSentinel(ComplexObject!);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public bool HasStoreGeneratedValue(IProperty property)
        => GetValueType(property) == CurrentValueType.StoreGenerated;


    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected enum CurrentValueType
    {
        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        Normal,

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        StoreGenerated,

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        Temporary
    }
}
