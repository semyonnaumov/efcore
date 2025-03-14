// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.TestModels.ManyToManyModel;

#nullable disable

public class JoinCompositeKeyToLeaf<T>
{
    public virtual int CompositeId1 { get; set; }
    public virtual string CompositeId2 { get; set; }
    public virtual DateTime CompositeId3 { get; set; }
    public virtual int LeafId { get; set; }

    public virtual EntityCompositeKey<T> Composite { get; set; }
    public virtual EntityLeaf<T> Leaf { get; set; }
}
