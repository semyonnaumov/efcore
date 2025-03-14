// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.TestModels.ManyToManyModel;

#nullable disable

public class JoinTwoToThree<T>
{
    public virtual int TwoId { get; set; }
    public virtual int ThreeId { get; set; }
    public virtual EntityTwo<T> Two { get; set; }
    public virtual EntityThree<T> Three { get; set; }
}
