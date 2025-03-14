// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations.Schema;

namespace Microsoft.EntityFrameworkCore.TestModels.ManyToManyModel;

#nullable disable

public class EntityThree<T>
{
    public virtual int Id { get; set; }
    public virtual string Name { get; set; }

    public virtual int? ReferenceInverseId { get; set; }
    public virtual EntityTwo<T> ReferenceInverse { get; set; }

    public virtual int? CollectionInverseId { get; set; }
    public virtual EntityTwo<T> CollectionInverse { get; set; }

    public virtual ICollection<EntityOne<T>> OneSkipPayloadFull { get; set; }
    public virtual ICollection<JoinOneToThreePayloadFull<T>> JoinOnePayloadFull { get; set; }
    public virtual ICollection<EntityTwo<T>> TwoSkipFull { get; set; }
    public virtual ICollection<JoinTwoToThree<T>> JoinTwoFull { get; set; }
    public virtual ICollection<EntityOne<T>> OneSkipPayloadFullShared { get; set; }
    public virtual ICollection<Dictionary<string, object>> JoinOnePayloadFullShared { get; set; }
    public virtual ICollection<EntityCompositeKey<T>> CompositeKeySkipFull { get; set; }
    public virtual ICollection<JoinThreeToCompositeKeyFull<T>> JoinCompositeKeyFull { get; set; }

    [InverseProperty("ThreeSkipShared")]
    public virtual ICollection<EntityRoot<T>> RootSkipShared { get; set; }
}
