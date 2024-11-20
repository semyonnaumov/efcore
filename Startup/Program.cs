// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Startup;

using Microsoft.EntityFrameworkCore.Benchmarks;
using Microsoft.EntityFrameworkCore.Benchmarks.Query;

/// <summary>
/// Add later.
/// </summary>
internal class Program
{
    private static void Main(string[] args)
        => EFCoreBenchmarkRunner.Run(args, typeof(NavigationsQuerySqlServerTests).Assembly);
}
