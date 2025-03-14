// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.TestModels.GearsOfWarModel;

#nullable disable

public class LocustHighCommand<T>
{
    public int Id { get; set; }

    public string Name { get; set; }
    public bool IsOperational { get; set; }

    public List<LocustCommander<T>> Commanders { get; set; }
}
