// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal;

namespace Microsoft.EntityFrameworkCore.SqlServer.Metadata.Conventions;

/// <summary>
/// TODO
/// </summary>
public class SqlServerDefaultValueConvention : IPropertyAnnotationChangedConvention, IModelFinalizingConvention
{
    /// <summary>
    ///     Creates a new instance of <see cref="SqlServerDefaultValueConvention" />.
    /// </summary>
    /// <param name="dependencies">Parameter object containing dependencies for this convention.</param>
    /// <param name="relationalDependencies">Parameter object containing relational dependencies for this convention.</param>
    public SqlServerDefaultValueConvention(
        ProviderConventionSetBuilderDependencies dependencies,
        RelationalConventionSetBuilderDependencies relationalDependencies)
    {
        Dependencies = dependencies;
        RelationalDependencies = relationalDependencies;
    }

    /// <summary>
    ///     Dependencies for this service.
    /// </summary>
    protected virtual ProviderConventionSetBuilderDependencies Dependencies { get; }

    /// <summary>
    ///     Relational provider-specific dependencies for this service.
    /// </summary>
    protected virtual RelationalConventionSetBuilderDependencies RelationalDependencies { get; }

    /// <inheritdoc />
    public void ProcessPropertyAnnotationChanged(
        IConventionPropertyBuilder propertyBuilder,
        string name,
        IConventionAnnotation? annotation,
        IConventionAnnotation? oldAnnotation,
        IConventionContext<IConventionAnnotation> context)
    {
        if (name == RelationalAnnotationNames.DefaultValue)
        {
            //if (oldAnnotation == null && annotation != null)
            //{
            //    var constraintName = "DF_" + PropertyPath(propertyBuilder.Metadata);

            //    propertyBuilder.Metadata.SetDefaultConstraintName(constraintName);
            //}
        }
    }

    //private static string PropertyPath(IConventionProperty property)
    //{
    //    var soi = StoreObjectIdentifier.Create(property.DeclaringType, StoreObjectType.Table);

    //    var declaringType = property.DeclaringType;
    //    string name = string.Empty;
    //    if (declaringType is IConventionEntityType entityType)
    //    {
    //        // TODO: can soi ever be null here?
    //        name = soi?.Name ?? entityType.DisplayName();
    //    }
    //    else if (declaringType is IConventionComplexType complexType)
    //    {
    //        // TODO
    //    }

    //    var result = name + "_" + property.Name;

    //    // TODO: hack - remove all trash from the name instead
    //    result = result.Replace("<", "").Replace(">", "").Replace(".", "").Replace("?", "").Replace("+", "");

    //    return result.Length > 100 ? result.Substring(0, 100) : result;
    //}

    /// <inheritdoc />
    public void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        var explicitDefaultConstraintNames = new List<string>();

        var useNamedDefaultConstraints = modelBuilder.Metadata.AreNamedDefaultConstraintsUsed() == true;

        if (useNamedDefaultConstraints)
        {
            // store all explicit names first - we don't want to change those in case they conflict with implicit names
            // we only need to do this if implicit default constraint names are used
            foreach (var entity in modelBuilder.Metadata.GetEntityTypes())
            {
                foreach (var property in entity.GetDeclaredProperties())
                {
                    // TODO: includes complex?
                    if (property.FindAnnotation(SqlServerAnnotationNames.DefaultConstraintName) is IConventionAnnotation annotation
                        && annotation.Value is string explicitDefaultConstraintName
                        && annotation.GetConfigurationSource() == ConfigurationSource.Explicit)
                    {
                        // TODO: what about data annotation?
                        explicitDefaultConstraintNames.Add(explicitDefaultConstraintName);
                    }
                }
            }
        }

        var existingDefaultConstraintNames = new List<string>(explicitDefaultConstraintNames);

        var suffixCounter = 1;
        foreach (var entity in modelBuilder.Metadata.GetEntityTypes())
        {
            // TODO: includes complex?
            foreach (var property in entity.GetDeclaredProperties())
            {
                if (property.FindAnnotation(RelationalAnnotationNames.DefaultValue) is IConventionAnnotation defaultValueAnnotation
                    || property.FindAnnotation(RelationalAnnotationNames.DefaultValueSql) is IConventionAnnotation defaultValueSqlAnnotation)
                {
                    var defaultConstraintNameAnnotation = property.FindAnnotation(SqlServerAnnotationNames.DefaultConstraintName);
                    if (defaultConstraintNameAnnotation != null && defaultConstraintNameAnnotation.GetConfigurationSource() != ConfigurationSource.Convention)
                    {
                        // explicit constraint name - nothing to do here
                        continue;
                    }

                    if (useNamedDefaultConstraints)
                    {
                        var defaultConstraintName = $"DF_{property.DeclaringType.GetTableName() ?? ""}_{property.GetColumnName()}";

                        if (!existingDefaultConstraintNames.Contains(defaultConstraintName))
                        {
                            existingDefaultConstraintNames.Add(defaultConstraintName);
                            property.SetDefaultConstraintName(defaultConstraintName);
                        }
                        else
                        {
                            // conflict - increase the counter and try again
                            // for now sharing counter for all constraints, will do proper thing later
                            // maybe reuse what we have for alias uniquefincation?
                            while (existingDefaultConstraintNames.Contains(defaultConstraintName + suffixCounter))
                            {
                                suffixCounter++;
                            }

                            existingDefaultConstraintNames.Add(defaultConstraintName + suffixCounter);
                            property.SetDefaultConstraintName(defaultConstraintName + suffixCounter);
                        }
                    }
                    else
                    {
                        // "" means use to unnamed constraints
                        property.SetDefaultConstraintName("");
                    }
                }

                //// TODO: includes complex?
                //if (property.FindAnnotation(SqlServerAnnotationNames.DefaultConstraintName) is IConventionAnnotation annotation
                //    && annotation.Value is string conventionDefaultConstraintName
                //    && annotation.GetConfigurationSource() == ConfigurationSource.Convention)
                //{
                //    if (!existingDefaultConstraintNames.Contains(conventionDefaultConstraintName))
                //    {
                //        existingDefaultConstraintNames.Add(conventionDefaultConstraintName);
                //    }
                //    else
                //    {
                //        // conflict - increase the counter and try again
                //        // for now sharing counter for all constraints, will do proper thing later
                //        // maybe reuse what we have for alias uniquefincation?
                //        while(existingDefaultConstraintNames.Contains(conventionDefaultConstraintName + suffixCounter))
                //        {
                //            suffixCounter++;
                //        }

                //        existingDefaultConstraintNames.Add(conventionDefaultConstraintName + suffixCounter);
                //        property.SetDefaultConstraintName(conventionDefaultConstraintName + suffixCounter);
                //    }
                //}
            }
        }
    }
}
