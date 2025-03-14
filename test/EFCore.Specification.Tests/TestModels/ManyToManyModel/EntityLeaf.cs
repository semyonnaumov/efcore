// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.TestModels.ManyToManyModel;

#nullable disable

public class EntityLeaf<T> : EntityBranch<T>
{
    public virtual bool? IsGreen { get; set; }

    public virtual ICollection<EntityCompositeKey<T>> CompositeKeySkipFull { get; set; }
    public virtual ICollection<JoinCompositeKeyToLeaf<T>> JoinCompositeKeyFull { get; set; }
}
