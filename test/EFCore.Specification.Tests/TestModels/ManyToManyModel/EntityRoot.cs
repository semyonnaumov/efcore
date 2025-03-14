// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.TestModels.ManyToManyModel;

#nullable disable

public class EntityRoot<T>
{
    public virtual int Id { get; set; }
    public virtual string Name { get; set; }
    public virtual ICollection<EntityThree<T>> ThreeSkipShared { get; set; }
    public virtual ICollection<EntityCompositeKey<T>> CompositeKeySkipShared { get; set; }
    public virtual ICollection<EntityBranch<T>> BranchSkipShared { get; set; }
}
