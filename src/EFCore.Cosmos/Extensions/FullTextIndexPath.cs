// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Cosmos;

[Experimental(EFDiagnostics.CosmosFullTextSearchExperimental)]
internal sealed class FullTextIndexPath
{
    public string? Path { get; set; }
}
