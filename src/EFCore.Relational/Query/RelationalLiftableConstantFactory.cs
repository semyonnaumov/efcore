// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Query;

/// <summary>
/// TODO
/// </summary>
public class RelationalLiftableConstantFactory : LiftableConstantFactory, IRelationalLiftableConstantFactory
{
    /// <summary>
    /// TODO
    /// </summary>
    public virtual LiftableConstantExpression CreateLiftableConstant(
        ConstantExpression originalExpression,
        Expression<Func<RelationalMaterializerLiftableConstantContext, object>> resolverExpression,
        string variableName,
        Type type)
        => new(originalExpression, resolverExpression, variableName, type);
}
