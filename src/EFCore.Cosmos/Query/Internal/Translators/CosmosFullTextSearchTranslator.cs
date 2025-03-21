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

        var paramsArgument = (SqlConstantExpression)arguments[2];
        foreach (var paramsValue in (IEnumerable)paramsArgument.Value!)
        {
            resultAguments.Add(sqlExpressionFactory.Constant(paramsValue, typeMapping));
        }

        var wrong = arguments[2].Print();

        return sqlExpressionFactory.Function(
            method.Name,
            resultAguments,
            typeof(bool),
            typeMappingSource.FindMapping(typeof(bool)));




        //var vectorMapping = arguments[1].TypeMapping as CosmosVectorTypeMapping
        //    ?? arguments[2].TypeMapping as CosmosVectorTypeMapping
        //    ?? throw new InvalidOperationException(CosmosStrings.VectorSearchRequiresVector);

        //Check.DebugAssert(arguments.Count is 3 or 4 or 5, "Did you add a parameter?");

        //SqlConstantExpression bruteForce;
        //if (arguments.Count >= 4)
        //{
        //    if (arguments[3] is not SqlConstantExpression { Value: bool })
        //    {
        //        throw new InvalidOperationException(
        //            CoreStrings.ArgumentNotConstant("useBruteForce", nameof(CosmosDbFunctionsExtensions.VectorDistance)));
        //    }

        //    bruteForce = (SqlConstantExpression)arguments[3];
        //}
        //else
        //{
        //    bruteForce = (SqlConstantExpression)sqlExpressionFactory.Constant(false);
        //}

        //var vectorType = vectorMapping.VectorType;
        //if (arguments.Count == 5)
        //{
        //    if (arguments[4] is not SqlConstantExpression { Value: DistanceFunction distanceFunction })
        //    {
        //        throw new InvalidOperationException(
        //            CoreStrings.ArgumentNotConstant("distanceFunction", nameof(CosmosDbFunctionsExtensions.VectorDistance)));
        //    }

        //    vectorType = vectorType with { DistanceFunction = distanceFunction };
        //}

        //var dataType = CosmosVectorType.CreateDefaultVectorDataType(vectorMapping.ClrType);

        //return sqlExpressionFactory.Function(
        //    "VectorDistance",
        //    [
        //        sqlExpressionFactory.ApplyTypeMapping(arguments[1], vectorMapping),
        //        sqlExpressionFactory.ApplyTypeMapping(arguments[2], vectorMapping),
        //        bruteForce,
        //        new FragmentExpression(
        //            $"{{'distanceFunction':'{vectorType.DistanceFunction.ToString().ToLower()}', 'dataType':'{dataType.ToString().ToLower()}'}}")
        //    ],
        //    typeof(double),
        //    typeMappingSource.FindMapping(typeof(double))!);
    }
}
