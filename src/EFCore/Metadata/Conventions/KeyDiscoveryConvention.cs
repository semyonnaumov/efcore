// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Metadata.Conventions;

/// <summary>
///     A convention that finds primary key property for the entity type based on the names, ignoring case:
///     * Id
///     * [entity name]Id
/// </summary>
/// <remarks>
///     <para>
///         If the entity type is owned through a reference navigation property then the corresponding foreign key
///         properties are used.
///     </para>
///     <para>
///         If the entity type is owned through a collection navigation property then a composite primary key
///         is configured using the foreign key properties with an extra property that matches the naming convention above.
///     </para>
///     <para>
///         If the entity type is a many-to-many join entity type then the many-to-many foreign key properties are used.
///     </para>
///     <para>
///         See <see href="https://aka.ms/efcore-docs-conventions">Model building conventions</see> for more information and examples.
///     </para>
/// </remarks>
public class KeyDiscoveryConvention :
    IEntityTypeAddedConvention,
    IPropertyAddedConvention,
    IKeyRemovedConvention,
    IEntityTypeBaseTypeChangedConvention,
    IEntityTypeMemberIgnoredConvention,
    IForeignKeyAddedConvention,
    IForeignKeyRemovedConvention,
    IForeignKeyPropertiesChangedConvention,
    IForeignKeyUniquenessChangedConvention,
    IForeignKeyOwnershipChangedConvention,
    ISkipNavigationForeignKeyChangedConvention
{
    private const string KeySuffix = "Id";

    /// <summary>
    ///     Creates a new instance of <see cref="KeyDiscoveryConvention" />.
    /// </summary>
    /// <param name="dependencies">Parameter object containing dependencies for this convention.</param>
    public KeyDiscoveryConvention(ProviderConventionSetBuilderDependencies dependencies)
    {
        Dependencies = dependencies;
    }

    /// <summary>
    ///     Dependencies for this service.
    /// </summary>
    protected virtual ProviderConventionSetBuilderDependencies Dependencies { get; }

    /// <summary>
    ///     Discovers primary key candidates and configures the primary key if found.
    /// </summary>
    /// <param name="entityTypeBuilder">The entity type builder.</param>
    protected virtual void TryConfigurePrimaryKey(IConventionEntityTypeBuilder entityTypeBuilder)
    {
        var entityType = entityTypeBuilder.Metadata;
        if (!ShouldDiscoverKeyProperties(entityType))
        {
            return;
        }

        var keyProperties = DiscoverKeyProperties(entityType);
        if (keyProperties != null)
        {
            ProcessKeyProperties(keyProperties, entityType);

            if (keyProperties.Count > 0)
            {
                entityTypeBuilder.PrimaryKey(keyProperties);
            }
        }
    }

    /// <summary>
    ///     Determines whether key properties should be discovered for the entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns><see langword="true"/> if key properties should be discovered, otherwise <see langword="false"/>.</returns>
    protected virtual bool ShouldDiscoverKeyProperties(IConventionEntityType entityType) =>
        entityType.BaseType == null
            && (!entityType.IsKeyless || entityType.GetIsKeylessConfigurationSource() == ConfigurationSource.Convention)
            && entityType.Builder.CanSetPrimaryKey((IReadOnlyList<IConventionProperty>?)null);

    /// <summary>
    ///     Returns the properties that should be used for the primary key.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>The properties that should be used for the primary key.</returns>
    protected virtual List<IConventionProperty>? DiscoverKeyProperties(IConventionEntityType entityType)
    {
        List<IConventionProperty>? keyProperties = null;
        var ownership = entityType.FindOwnership();
        if (ownership?.DeclaringEntityType != entityType)
        {
            ownership = null;
        }

        if (ownership?.IsUnique == true)
        {
            keyProperties = ownership.Properties.ToList();
        }

        if (keyProperties == null)
        {
            var candidateProperties = entityType.GetProperties().Where(
                p => !p.IsImplicitlyCreated()
                    || !ConfigurationSource.Convention.Overrides(p.GetConfigurationSource()));
            keyProperties = DiscoverKeyProperties(entityType, candidateProperties).ToList();
            if (keyProperties.Count > 1)
            {
                Dependencies.Logger.MultiplePrimaryKeyCandidates(keyProperties[0], keyProperties[1]);
                return null;
            }
        }

        // if (ownership?.IsUnique == false)
        // {
        //     if (keyProperties.Count == 0
        //         || ownership.Properties.Contains(keyProperties.First()))
        //     {
        //         var primaryKey = entityType.FindPrimaryKey();
        //         var shadowProperty = primaryKey?.Properties.Last();
        //         if (shadowProperty == null
        //             || primaryKey!.Properties.Count == 1
        //             || ownership.Properties.Contains(shadowProperty))
        //         {
        //             shadowProperty = entityType.Builder.CreateUniqueProperty(typeof(int), "Id", required: true)!.Metadata;
        //         }
        //
        //         keyProperties.Clear();
        //         keyProperties.Add(shadowProperty);
        //     }
        //
        //     var extraProperty = keyProperties[0];
        //     keyProperties.RemoveAt(0);
        //     keyProperties.AddRange(ownership.Properties);
        //     keyProperties.Add(extraProperty);
        // }

        if (keyProperties.Count == 0)
        {
            var manyToManyForeignKeys = entityType.GetForeignKeys()
                .Where(fk => fk.GetReferencingSkipNavigations().Any(n => n.IsCollection)).ToList();
            if (manyToManyForeignKeys.Count == 2
                && manyToManyForeignKeys.All(fk => fk.PrincipalEntityType != entityType))
            {
                keyProperties.AddRange(manyToManyForeignKeys.SelectMany(fk => fk.Properties));
            }
        }

        return keyProperties;
    }

    /// <summary>
    ///     Adds or removes properties to be used for the primary key.
    /// </summary>
    /// <param name="keyProperties">The properties that will be used to configure the key.</param>
    /// <param name="entityType">The entity type being configured.</param>
    protected virtual void ProcessKeyProperties(
        IList<IConventionProperty> keyProperties,
        IConventionEntityType entityType)
    {
        var synthesizedProperty = keyProperties.FirstOrDefault(p => p.Name == "__synthesizedId");
        var ownershipForeignKey = entityType.FindOwnership();
        if (ownershipForeignKey?.IsUnique == false)
        {
            // This is an owned collection, so it has a composite key consisting of FK properties pointing to the owner PK,
            // any additional key properties defined by the application, and then the synthesized property.
            // Add these in the correct order--this is somewhat inefficient, but we are limited because we have to manipulate the
            // existing collection.
            var existingKeyProperties = keyProperties.ToList();
            keyProperties.Clear();

            // Add the FK properties to form the first part of the composite key.
            foreach (var conventionProperty in ownershipForeignKey.Properties)
            {
                keyProperties.Add(conventionProperty);
            }

            // Generate the synthesized key property if it doesn't exist.
            if (synthesizedProperty == null)
            {
                var builder = entityType.Builder.CreateUniqueProperty(typeof(int), "__synthesizedId", required: true);
                builder = builder?.ValueGenerated(ValueGenerated.OnAdd) ?? builder;
                synthesizedProperty = builder!.Metadata;
            }

            // Add non-duplicate, non-ownership, non-synthesized properties.
            foreach (var keyProperty in existingKeyProperties)
            {
                if (keyProperty != synthesizedProperty
                    && !keyProperties.Contains(keyProperty))
                {
                    keyProperties.Add(keyProperty);
                }
            }

            // Finally, the synthesized property always goes at the end.
            keyProperties.Add(synthesizedProperty);
        }
        else
        {
            // This was an owned collection, but now is not, so remove the synthesized property.
            if (synthesizedProperty is not null)
            {
                keyProperties.Remove(synthesizedProperty);
            }
        }
    }

    /// <summary>
    ///     Returns the properties that should be used for the primary key.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="candidateProperties">The properties to consider.</param>
    /// <returns>The properties that should be used for the primary key.</returns>
    public static IEnumerable<IConventionProperty> DiscoverKeyProperties(
        IConventionEntityType entityType,
        IEnumerable<IConventionProperty> candidateProperties)
    {
        Check.NotNull(entityType, nameof(entityType));

        // ReSharper disable PossibleMultipleEnumeration
        var keyProperties = candidateProperties.Where(p => string.Equals(p.Name, KeySuffix, StringComparison.OrdinalIgnoreCase));
        if (!keyProperties.Any())
        {
            var entityTypeName = entityType.ShortName();
            keyProperties = candidateProperties.Where(
                p => p.Name.Length == entityTypeName.Length + KeySuffix.Length
                    && p.Name.StartsWith(entityTypeName, StringComparison.OrdinalIgnoreCase)
                    && p.Name.EndsWith(KeySuffix, StringComparison.OrdinalIgnoreCase));
        }
        // ReSharper restore PossibleMultipleEnumeration

        return keyProperties;
    }

    /// <summary>
    ///     Called after an entity type member is ignored.
    /// </summary>
    /// <param name="entityTypeBuilder">The builder for the entity type.</param>
    /// <param name="name">The name of the ignored member.</param>
    /// <param name="context">Additional information associated with convention execution.</param>
    public virtual void ProcessEntityTypeMemberIgnored(
        IConventionEntityTypeBuilder entityTypeBuilder,
        string name,
        IConventionContext<string> context)
    {
        var entityTypeName = entityTypeBuilder.Metadata.ShortName();
        if (string.Equals(name, KeySuffix, StringComparison.OrdinalIgnoreCase)
            || (name.Length == entityTypeName.Length + KeySuffix.Length
                && name.StartsWith(entityTypeName, StringComparison.OrdinalIgnoreCase)
                && name.EndsWith(KeySuffix, StringComparison.OrdinalIgnoreCase)))
        {
            TryConfigurePrimaryKey(entityTypeBuilder);
        }
    }

    /// <inheritdoc />
    public virtual void ProcessEntityTypeAdded(
        IConventionEntityTypeBuilder entityTypeBuilder,
        IConventionContext<IConventionEntityTypeBuilder> context)
        => TryConfigurePrimaryKey(entityTypeBuilder);

    /// <inheritdoc />
    public virtual void ProcessEntityTypeBaseTypeChanged(
        IConventionEntityTypeBuilder entityTypeBuilder,
        IConventionEntityType? newBaseType,
        IConventionEntityType? oldBaseType,
        IConventionContext<IConventionEntityType> context)
    {
        if (entityTypeBuilder.Metadata.BaseType != newBaseType)
        {
            return;
        }

        TryConfigurePrimaryKey(entityTypeBuilder);
    }

    /// <inheritdoc />
    public virtual void ProcessPropertyAdded(
        IConventionPropertyBuilder propertyBuilder,
        IConventionContext<IConventionPropertyBuilder> context)
    {
        if (propertyBuilder.Metadata.DeclaringType is not IConventionEntityType entityType
            || propertyBuilder.Metadata.Name == "__synthesizedId")
        {
            return;
        }

        TryConfigurePrimaryKey(entityType.Builder);
        if (!propertyBuilder.Metadata.IsInModel)
        {
            context.StopProcessing();
        }
    }

    /// <inheritdoc />
    public virtual void ProcessKeyRemoved(
        IConventionEntityTypeBuilder entityTypeBuilder,
        IConventionKey key,
        IConventionContext<IConventionKey> context)
    {
        if (!entityTypeBuilder.Metadata.IsInModel)
        {
            return;
        }

        if (entityTypeBuilder.Metadata.FindPrimaryKey() == null)
        {
            TryConfigurePrimaryKey(entityTypeBuilder);
        }
    }

    /// <inheritdoc />
    public virtual void ProcessForeignKeyAdded(
        IConventionForeignKeyBuilder relationshipBuilder,
        IConventionContext<IConventionForeignKeyBuilder> context)
    {
        if (relationshipBuilder.Metadata.IsOwnership)
        {
            TryConfigurePrimaryKey(relationshipBuilder.Metadata.DeclaringEntityType.Builder);
        }
    }

    /// <inheritdoc />
    public virtual void ProcessForeignKeyPropertiesChanged(
        IConventionForeignKeyBuilder relationshipBuilder,
        IReadOnlyList<IConventionProperty> oldDependentProperties,
        IConventionKey oldPrincipalKey,
        IConventionContext<IReadOnlyList<IConventionProperty>> context)
    {
        var foreignKey = relationshipBuilder.Metadata;
        if ((foreignKey.IsOwnership
            || foreignKey.GetReferencingSkipNavigations().Any(n => n.IsCollection))
            && !foreignKey.Properties.SequenceEqual(oldDependentProperties)
            && relationshipBuilder.Metadata.IsInModel)
        {
            TryConfigurePrimaryKey(foreignKey.DeclaringEntityType.Builder);
        }
    }

    /// <inheritdoc />
    public virtual void ProcessForeignKeyOwnershipChanged(
        IConventionForeignKeyBuilder relationshipBuilder,
        IConventionContext<bool?> context)
        => TryConfigurePrimaryKey(relationshipBuilder.Metadata.DeclaringEntityType.Builder);

    /// <inheritdoc />
    public virtual void ProcessForeignKeyRemoved(
        IConventionEntityTypeBuilder entityTypeBuilder,
        IConventionForeignKey foreignKey,
        IConventionContext<IConventionForeignKey> context)
    {
        if (entityTypeBuilder.Metadata.IsInModel
            && foreignKey.IsOwnership)
        {
            TryConfigurePrimaryKey(entityTypeBuilder);
        }
    }

    /// <inheritdoc />
    public virtual void ProcessForeignKeyUniquenessChanged(
        IConventionForeignKeyBuilder relationshipBuilder,
        IConventionContext<bool?> context)
    {
        if (relationshipBuilder.Metadata.IsOwnership)
        {
            TryConfigurePrimaryKey(relationshipBuilder.Metadata.DeclaringEntityType.Builder);
        }
    }

    /// <inheritdoc />
    public virtual void ProcessSkipNavigationForeignKeyChanged(
        IConventionSkipNavigationBuilder skipNavigationBuilder,
        IConventionForeignKey? foreignKey,
        IConventionForeignKey? oldForeignKey,
        IConventionContext<IConventionForeignKey> context)
    {
        var joinEntityTypeBuilder = skipNavigationBuilder.Metadata.ForeignKey?.DeclaringEntityType.Builder;
        if (joinEntityTypeBuilder != null
            && skipNavigationBuilder.Metadata.IsCollection)
        {
            TryConfigurePrimaryKey(joinEntityTypeBuilder);
        }
    }
}
