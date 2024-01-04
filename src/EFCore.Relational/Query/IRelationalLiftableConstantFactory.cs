// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Query;

/// <summary>
/// TODO
/// </summary>
public interface IRelationalLiftableConstantFactory : ILiftableConstantFactory
{
    /// <summary>
    /// TODO
    /// </summary>
    LiftableConstantExpression CreateLiftableConstant(
        Expression<Func<RelationalMaterializerLiftableConstantContext, object>> resolverExpression,
        string variableName,
        Type type);
}
