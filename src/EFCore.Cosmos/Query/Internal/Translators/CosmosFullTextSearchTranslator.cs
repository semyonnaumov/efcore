// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using Microsoft.EntityFrameworkCore.Cosmos.Extensions;
using Microsoft.EntityFrameworkCore.Cosmos.Internal;
using Microsoft.EntityFrameworkCore.Cosmos.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Cosmos.Storage.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore.Cosmos.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class CosmosFullTextSearchTranslator(ISqlExpressionFactory sqlExpressionFactory, ITypeMappingSource typeMappingSource)
    : IMethodCallTranslator
{
    private static readonly List<string> SupportedFullTextMethods = new()
    {
        nameof(CosmosDbFunctionsExtensions.FullTextContains),
        nameof(CosmosDbFunctionsExtensions.FullTextContainsAll),
        nameof(CosmosDbFunctionsExtensions.FullTextContainsAny),
    };

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (method.DeclaringType != typeof(CosmosDbFunctionsExtensions)
            || !SupportedFullTextMethods.Contains(method.Name))
        {
            return null;
        }

        var typeMapping = arguments[1].TypeMapping;
        if (method.Name == nameof(CosmosDbFunctionsExtensions.FullTextContains))
        {
            return sqlExpressionFactory.Function(
                method.Name,
                [
                    arguments[1],
                sqlExpressionFactory.ApplyTypeMapping(arguments[2], typeMapping)
                ],
                typeof(bool),
                typeMappingSource.FindMapping(typeof(bool)));
        }

        var resultAguments = new List<SqlExpression>
        {
            arguments[1]
        };

        var paramsArgument = (ArrayConstantExpression)arguments[2];
        foreach (var item in paramsArgument.Items)
        {
            resultAguments.Add(item);
        }

        return sqlExpressionFactory.Function(
            method.Name,
            resultAguments,
            typeof(bool),
            typeMappingSource.FindMapping(typeof(bool)));
    }
}
