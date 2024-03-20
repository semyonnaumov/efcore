// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Query;

/// <summary>
/// TODO
/// </summary>
public class LiftableConstantFactory : ILiftableConstantFactory
{
    /// <summary>
    /// TODO
    /// </summary>
    public virtual LiftableConstantExpression CreateLiftableConstant(
        ConstantExpression originalExpression,
        Expression<Func<MaterializerLiftableConstantContext, object>> resolverExpression,
        string variableName,
        Type type)
        => new(originalExpression, resolverExpression, variableName, type);
}
