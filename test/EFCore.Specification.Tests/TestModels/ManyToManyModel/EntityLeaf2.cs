// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.TestModels.ManyToManyModel;

#nullable disable

public class EntityLeaf2<T> : EntityBranch2<T>
{
    public virtual bool? IsBrown { get; set; }
    public virtual ICollection<EntityBranch2<T>> Branch2SkipShared { get; set; }
}
