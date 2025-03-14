// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations.Schema;

namespace Microsoft.EntityFrameworkCore.TestModels.ManyToManyModel;

#nullable disable

public class EntityOne<T>
{
    public virtual int Id { get; set; }
    public virtual string Name { get; set; }

    public virtual EntityTwo<T> Reference { get; set; }
    public virtual ICollection<EntityTwo<T>> Collection { get; set; }
    public virtual ICollection<EntityTwo<T>> TwoSkip { get; set; }
    public virtual ICollection<EntityThree<T>> ThreeSkipPayloadFull { get; set; }
    public virtual ICollection<JoinOneToThreePayloadFull<T>> JoinThreePayloadFull { get; set; }

    [InverseProperty("OneSkipShared")]
    public virtual ICollection<EntityTwo<T>> TwoSkipShared { get; set; }

    public virtual ICollection<EntityThree<T>> ThreeSkipPayloadFullShared { get; set; }
    public virtual ICollection<Dictionary<string, object>> JoinThreePayloadFullShared { get; set; }
    public virtual ICollection<EntityOne<T>> SelfSkipPayloadLeft { get; set; }
    public virtual ICollection<JoinOneSelfPayload<T>> JoinSelfPayloadLeft { get; set; }
    public virtual ICollection<EntityOne<T>> SelfSkipPayloadRight { get; set; }
    public virtual ICollection<JoinOneSelfPayload<T>> JoinSelfPayloadRight { get; set; }
    public virtual ICollection<EntityBranch<T>> BranchSkip { get; set; }
}
