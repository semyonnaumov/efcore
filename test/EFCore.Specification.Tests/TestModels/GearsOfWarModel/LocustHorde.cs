// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.TestModels.GearsOfWarModel;

#nullable disable

public class LocustHorde<T> : Faction<T>
{
    public LocustCommander<T> Commander { get; set; }
    public List<LocustLeader<T>> Leaders { get; set; }

    public string CommanderName { get; set; }
    public bool? Eradicated { get; set; }
}
