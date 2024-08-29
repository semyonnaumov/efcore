// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Cosmos.Metadata.Internal;

namespace Microsoft.EntityFrameworkCore.Metadata.Conventions;

/// <summary>
///     A convention that finds primary key property for the entity type based on the names
///     and adds the partition key to it if present.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-conventions">Model building conventions</see>, and
///     <see href="https://aka.ms/efcore-docs-cosmos">Accessing Azure Cosmos DB with EF Core</see> for more information and examples.
/// </remarks>
public class CosmosKeyDiscoveryConvention :
    KeyDiscoveryConvention,
    IEntityTypeAnnotationChangedConvention
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    [EntityFrameworkInternal]
    public const string SynthesizedOrdinalPropertyName = "__synthesizedOrdinal";

    /// <summary>
    ///     Creates a new instance of <see cref="CosmosKeyDiscoveryConvention" />.
    /// </summary>
    /// <param name="dependencies">Parameter object containing dependencies for this convention.</param>
    public CosmosKeyDiscoveryConvention(ProviderConventionSetBuilderDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <summary>
    ///     Called after an annotation is changed on an entity type.
    /// </summary>
    /// <param name="entityTypeBuilder">The builder for the entity type.</param>
    /// <param name="name">The annotation name.</param>
    /// <param name="annotation">The new annotation.</param>
    /// <param name="oldAnnotation">The old annotation.</param>
    /// <param name="context">Additional information associated with convention execution.</param>
    public virtual void ProcessEntityTypeAnnotationChanged(
        IConventionEntityTypeBuilder entityTypeBuilder,
        string name,
        IConventionAnnotation? annotation,
        IConventionAnnotation? oldAnnotation,
        IConventionContext<IConventionAnnotation> context)
    {
        if (name == CosmosAnnotationNames.PartitionKeyNames)
        {
            TryConfigurePrimaryKey(entityTypeBuilder);
        }
    }

    /// <inheritdoc />
    protected override List<IConventionProperty>? DiscoverKeyProperties(IConventionEntityType entityType)
    {
        var ownership = entityType.FindOwnership();
        if (ownership?.DeclaringEntityType != entityType)
        {
            ownership = null;
        }

        // Don't discover key properties for owned collection types called `Id` without attempting to persist key values.
        if (ownership?.IsUnique == false)
        {
            return [];
        }

        return base.DiscoverKeyProperties(entityType);
    }

    /// <inheritdoc />
    public override void ProcessPropertyAdded(
        IConventionPropertyBuilder propertyBuilder,
        IConventionContext<IConventionPropertyBuilder> context)
    {
        if (propertyBuilder.Metadata.Name != SynthesizedOrdinalPropertyName)
        {
            base.ProcessPropertyAdded(propertyBuilder, context);
        }
    }

    /// <inheritdoc />
    protected override void ProcessKeyProperties(
        IList<IConventionProperty> keyProperties,
        IConventionEntityType entityType)
    {
        foreach (var propertyName in entityType.GetPartitionKeyPropertyNames())
        {
            var partitionKeyProperty = entityType.FindProperty(propertyName);
            if (partitionKeyProperty != null
                && !keyProperties.Contains(partitionKeyProperty))
            {
                keyProperties.Add(partitionKeyProperty);
            }
        }

        var synthesizedProperty = keyProperties.FirstOrDefault(p => p.Name == SynthesizedOrdinalPropertyName);
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
                var builder = entityType.Builder.CreateUniqueProperty(typeof(int), SynthesizedOrdinalPropertyName, required: true);
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
            // Not an owned collection or not mapped to JSON.
            if (synthesizedProperty is not null)
            {
                // This was an owned collection, but now is not, so remove the synthesized property.
                keyProperties.Remove(synthesizedProperty);
            }

            base.ProcessKeyProperties(keyProperties, entityType);
        }
    }
}
