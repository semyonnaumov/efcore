// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Cosmos.Query;

/// <summary>
/// TODO
/// </summary>
public interface ICosmosLiftableConstantFactory : ILiftableConstantFactory
{
    /// <summary>
    /// TODO
    /// </summary>
    LiftableConstantExpression CreateLiftableConstant(
        ConstantExpression originalExpression,
        Expression<Func<CosmosMaterializerLiftableConstantContext, object>> resolverExpression,
        string variableName,
        Type type);
}
