// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

/// <summary>
/// A class containing application context switches for Entity Framework Core.
/// </summary>
public static class AppContextSwitches
{
    /// <summary>
    /// Value representing whether query compilation should be disabled.
    /// </summary>
    public static bool DisableQueryCompilation { get; } = InitializeDisableQueryCompilation();

    private static bool InitializeDisableQueryCompilation() => false;
}
