// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.TestModels.ManyToManyModel;

#nullable disable

public class EntityCompositeKey<T>
{
    public virtual int Key1 { get; set; }
    public virtual string Key2 { get; set; }
    public virtual DateTime Key3 { get; set; }

    public virtual string Name { get; set; }

    public virtual ICollection<EntityTwo<T>> TwoSkipShared { get; set; }
    public virtual ICollection<EntityThree<T>> ThreeSkipFull { get; set; }
    public virtual ICollection<JoinThreeToCompositeKeyFull<T>> JoinThreeFull { get; set; }
    public virtual ICollection<EntityRoot<T>> RootSkipShared { get; set; }
    public virtual ICollection<EntityLeaf<T>> LeafSkipFull { get; set; }
    public virtual ICollection<JoinCompositeKeyToLeaf<T>> JoinLeafFull { get; set; }
}
