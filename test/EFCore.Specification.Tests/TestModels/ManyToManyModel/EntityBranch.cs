// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.TestModels.ManyToManyModel;

#nullable disable

public class EntityBranch<T> : EntityRoot<T>
{
    public virtual long Number { get; set; }
    public virtual ICollection<EntityOne<T>> OneSkip { get; set; }
    public virtual ICollection<EntityRoot<T>> RootSkipShared { get; set; }
}
