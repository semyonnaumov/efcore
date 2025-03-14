// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations.Schema;

namespace Microsoft.EntityFrameworkCore.TestModels.ManyToManyModel;

#nullable disable

public class EntityTwo<T>
{
    public virtual int Id { get; set; }
    public virtual string Name { get; set; }

    public virtual int? ReferenceInverseId { get; set; }
    public virtual EntityOne<T> ReferenceInverse { get; set; }

    public virtual int? CollectionInverseId { get; set; }
    public virtual EntityOne<T> CollectionInverse { get; set; }

    public virtual EntityThree<T> Reference { get; set; }
    public virtual ICollection<EntityThree<T>> Collection { get; set; }
    public virtual ICollection<EntityOne<T>> OneSkip { get; set; }
    public virtual ICollection<EntityThree<T>> ThreeSkipFull { get; set; }
    public virtual ICollection<JoinTwoToThree<T>> JoinThreeFull { get; set; }
    public virtual ICollection<EntityTwo<T>> SelfSkipSharedLeft { get; set; }
    public virtual ICollection<EntityTwo<T>> SelfSkipSharedRight { get; set; }

    [InverseProperty("TwoSkipShared")]
    public virtual ICollection<EntityOne<T>> OneSkipShared { get; set; }

    public virtual ICollection<EntityCompositeKey<T>> CompositeKeySkipShared { get; set; }

    public virtual int? ExtraId { get; set; }
    public virtual JoinOneToTwoExtra<T> Extra { get; set; }
}
