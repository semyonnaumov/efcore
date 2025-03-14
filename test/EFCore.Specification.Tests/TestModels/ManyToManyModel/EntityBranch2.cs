// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.TestModels.ManyToManyModel;

#nullable disable

public abstract class EntityBranch2<T> : EntityRoot<T>
{
    public virtual long Slumber { get; set; }
    public virtual ICollection<EntityLeaf2<T>> Leaf2SkipShared { get; set; }

    public virtual ICollection<EntityBranch2<T>> SelfSkipSharedLeft { get; set; }
    public virtual ICollection<EntityBranch2<T>> SelfSkipSharedRight { get; set; }
}
