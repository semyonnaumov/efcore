// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestModels.ManyToManyModel;

namespace Microsoft.EntityFrameworkCore.Query;

#nullable disable

public abstract class ManyToManyQueryRelationalTestBase<TFixture>(TFixture fixture) : ManyToManyQueryTestBase<TFixture>(fixture)
    where TFixture : ManyToManyQueryFixtureBase, new()
{
    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual Task Include_skip_navigation_split(bool async)
        => AssertQuery(
            async,
            ss => ss.Set<EntityCompositeKey<int>>().Include(e => e.RootSkipShared).AsSplitQuery(),
            elementAsserter: (e, a) => AssertInclude(e, a, new ExpectedInclude<EntityCompositeKey<int>>(et => et.RootSkipShared)));

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual Task Include_skip_navigation_then_reference_split(bool async)
        => AssertQuery(
            async,
            ss => ss.Set<EntityTwo<int>>().Include(e => e.OneSkip).ThenInclude(e => e.Reference).AsSplitQuery(),
            elementAsserter: (e, a) => AssertInclude(
                e, a,
                new ExpectedInclude<EntityTwo<int>>(et => et.OneSkip),
                new ExpectedInclude<EntityOne<int>>(et => et.Reference, "OneSkip")));

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual Task Include_skip_navigation_then_include_skip_navigation_split(bool async)
        => AssertQuery(
            async,
            ss => ss.Set<EntityCompositeKey<int>>().Include(e => e.LeafSkipFull).ThenInclude(e => e.OneSkip).AsSplitQuery(),
            elementAsserter: (e, a) => AssertInclude(
                e, a,
                new ExpectedInclude<EntityCompositeKey<int>>(et => et.LeafSkipFull),
                new ExpectedInclude<EntityLeaf<int>>(et => et.OneSkip, "LeafSkipFull")));

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual Task Include_skip_navigation_then_include_reference_and_skip_navigation_split(bool async)
        => AssertQuery(
            async,
            ss => ss.Set<EntityThree<int>>().Include(e => e.OneSkipPayloadFull).ThenInclude(e => e.Reference)
                .Include(e => e.OneSkipPayloadFull).ThenInclude(e => e.SelfSkipPayloadRight).AsSplitQuery(),
            elementAsserter: (e, a) => AssertInclude(
                e, a,
                new ExpectedInclude<EntityThree<int>>(et => et.OneSkipPayloadFull),
                new ExpectedInclude<EntityOne<int>>(et => et.Reference, "OneSkipPayloadFull"),
                new ExpectedInclude<EntityOne<int>>(et => et.SelfSkipPayloadRight, "OneSkipPayloadFull")));

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual Task Include_skip_navigation_and_reference_split(bool async)
        => AssertQuery(
            async,
            ss => ss.Set<EntityTwo<int>>().Include(e => e.OneSkipShared).Include(e => e.Reference).AsSplitQuery(),
            elementAsserter: (e, a) => AssertInclude(
                e, a,
                new ExpectedInclude<EntityTwo<int>>(et => et.OneSkipShared),
                new ExpectedInclude<EntityTwo<int>>(et => et.Reference)));

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual Task Filtered_include_skip_navigation_where_split(bool async)
        => AssertQuery(
            async,
            ss => ss.Set<EntityThree<int>>().Include(e => e.OneSkipPayloadFullShared.Where(i => i.Id < 10)).AsSplitQuery(),
            elementAsserter: (e, a) => AssertInclude(
                e, a,
                new ExpectedFilteredInclude<EntityThree<int>, EntityOne<int>>(
                    et => et.OneSkipPayloadFullShared, includeFilter: x => x.Where(i => i.Id < 10))));

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual Task Filtered_include_skip_navigation_order_by_split(bool async)
        => AssertQuery(
            async,
            ss => ss.Set<EntityThree<int>>().Include(e => e.TwoSkipFull.OrderBy(i => i.Id)).AsSplitQuery(),
            elementAsserter: (e, a) => AssertInclude(
                e, a,
                new ExpectedFilteredInclude<EntityThree<int>, EntityTwo<int>>(
                    et => et.TwoSkipFull, includeFilter: x => x.OrderBy(i => i.Id))));

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual Task Filtered_include_skip_navigation_order_by_skip_split(bool async)
        => AssertQuery(
            async,
            ss => ss.Set<EntityTwo<int>>().Include(e => e.SelfSkipSharedRight.OrderBy(i => i.Id).Skip(2)).AsSplitQuery(),
            elementAsserter: (e, a) => AssertInclude(
                e, a,
                new ExpectedFilteredInclude<EntityTwo<int>, EntityTwo<int>>(
                    et => et.SelfSkipSharedRight, includeFilter: x => x.OrderBy(i => i.Id).Skip(2))));

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual Task Filtered_include_skip_navigation_order_by_take_split(bool async)
        => AssertQuery(
            async,
            ss => ss.Set<EntityCompositeKey<int>>().Include(e => e.TwoSkipShared.OrderBy(i => i.Id).Take(2)).AsSplitQuery(),
            elementAsserter: (e, a) => AssertInclude(
                e, a,
                new ExpectedFilteredInclude<EntityCompositeKey<int>, EntityTwo<int>>(
                    et => et.TwoSkipShared, includeFilter: x => x.OrderBy(i => i.Id).Take(2))));

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual Task Filtered_include_skip_navigation_order_by_skip_take_split(bool async)
        => AssertQuery(
            async,
            ss => ss.Set<EntityCompositeKey<int>>().Include(e => e.ThreeSkipFull.OrderBy(i => i.Id).Skip(1).Take(2)).AsSplitQuery(),
            elementAsserter: (e, a) => AssertInclude(
                e, a,
                new ExpectedFilteredInclude<EntityCompositeKey<int>, EntityThree<int>>(
                    et => et.ThreeSkipFull, includeFilter: x => x.OrderBy(i => i.Id).Skip(1).Take(2))));

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual Task Filtered_then_include_skip_navigation_where_split(bool async)
        => AssertQuery(
            async,
            ss => ss.Set<EntityRoot<int>>().Include(e => e.ThreeSkipShared)
                .ThenInclude(e => e.OneSkipPayloadFullShared.Where(i => i.Id < 10))
                .AsSplitQuery(),
            elementAsserter: (e, a) => AssertInclude(
                e, a,
                new ExpectedInclude<EntityRoot<int>>(et => et.ThreeSkipShared),
                new ExpectedFilteredInclude<EntityThree<int>, EntityOne<int>>(
                    et => et.OneSkipPayloadFullShared, "ThreeSkipShared", includeFilter: x => x.Where(i => i.Id < 10))));

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual Task Filtered_then_include_skip_navigation_order_by_skip_take_split(bool async)
        => AssertQuery(
            async,
            ss => ss.Set<EntityRoot<int>>().Include(e => e.CompositeKeySkipShared)
                .ThenInclude(e => e.ThreeSkipFull.OrderBy(i => i.Id).Skip(1).Take(2))
                .AsSplitQuery(),
            elementAsserter: (e, a) => AssertInclude(
                e, a,
                new ExpectedInclude<EntityRoot<int>>(et => et.CompositeKeySkipShared),
                new ExpectedFilteredInclude<EntityCompositeKey<int>, EntityThree<int>>(
                    et => et.ThreeSkipFull, "CompositeKeySkipShared", includeFilter: x => x.OrderBy(i => i.Id).Skip(1).Take(2))));

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual Task Filtered_include_skip_navigation_where_then_include_skip_navigation_split(bool async)
        => AssertQuery(
            async,
            ss => ss.Set<EntityLeaf<int>>().Include(e => e.CompositeKeySkipFull.Where(i => i.Key1 < 5))
                .ThenInclude(e => e.TwoSkipShared).AsSplitQuery(),
            elementAsserter: (e, a) => AssertInclude(
                e, a,
                new ExpectedFilteredInclude<EntityLeaf<int>, EntityCompositeKey<int>>(
                    et => et.CompositeKeySkipFull, includeFilter: x => x.Where(i => i.Key1 < 5)),
                new ExpectedInclude<EntityCompositeKey<int>>(et => et.TwoSkipShared, "CompositeKeySkipFull")));

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual Task Filtered_include_skip_navigation_order_by_skip_take_then_include_skip_navigation_where_split(bool async)
        => AssertQuery(
            async,
            ss => ss.Set<EntityOne<int>>().Include(e => e.TwoSkip.OrderBy(i => i.Id).Skip(1).Take(2))
                .ThenInclude(e => e.ThreeSkipFull.Where(i => i.Id < 10))
                .AsSplitQuery(),
            elementAsserter: (e, a) => AssertInclude(
                e, a,
                new ExpectedFilteredInclude<EntityOne<int>, EntityTwo<int>>(
                    et => et.TwoSkip, includeFilter: x => x.OrderBy(i => i.Id).Skip(1).Take(2)),
                new ExpectedFilteredInclude<EntityTwo<int>, EntityThree<int>>(
                    et => et.ThreeSkipFull, "TwoSkip", includeFilter: x => x.Where(i => i.Id < 10))));

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual Task Filtered_include_skip_navigation_where_then_include_skip_navigation_order_by_skip_take_split(bool async)
        => AssertQuery(
            async,
            ss => ss.Set<EntityOne<int>>().Include(e => e.TwoSkip.Where(i => i.Id < 10))
                .ThenInclude(e => e.ThreeSkipFull.OrderBy(i => i.Id).Skip(1).Take(2))
                .AsSplitQuery(),
            elementAsserter: (e, a) => AssertInclude(
                e, a,
                new ExpectedFilteredInclude<EntityOne<int>, EntityTwo<int>>(
                    et => et.TwoSkip, includeFilter: x => x.Where(i => i.Id < 10)),
                new ExpectedFilteredInclude<EntityTwo<int>, EntityThree<int>>(
                    et => et.ThreeSkipFull, "TwoSkip", includeFilter: x => x.OrderBy(i => i.Id).Skip(1).Take(2))));

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual Task Filter_include_on_skip_navigation_combined_split(bool async)
        => AssertQuery(
            async,
            ss => ss.Set<EntityTwo<int>>().Include(e => e.OneSkip.Where(i => i.Id < 10)).ThenInclude(e => e.Reference)
                .Include(e => e.OneSkip).ThenInclude(e => e.Collection)
                .AsSplitQuery(),
            elementAsserter: (e, a) => AssertInclude(
                e, a,
                new ExpectedFilteredInclude<EntityTwo<int>, EntityOne<int>>(et => et.OneSkip, includeFilter: x => x.Where(i => i.Id < 10)),
                new ExpectedInclude<EntityOne<int>>(et => et.Reference, "OneSkip"),
                new ExpectedInclude<EntityOne<int>>(et => et.Collection, "OneSkip")));

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual Task Filter_include_on_skip_navigation_combined_with_filtered_then_includes_split(bool async)
        => AssertQuery(
            async,
            ss => ss.Set<EntityThree<int>>()
                .Include(e => e.OneSkipPayloadFull.Where(i => i.Id < 10))
                .ThenInclude(e => e.TwoSkip.OrderBy(e => e.Id).Skip(1).Take(2))
                .Include(e => e.OneSkipPayloadFull)
                .ThenInclude(e => e.BranchSkip.Where(e => e.Id < 20))
                .AsSplitQuery(),
            elementAsserter: (e, a) => AssertInclude(
                e, a,
                new ExpectedFilteredInclude<EntityThree<int>, EntityOne<int>>(
                    et => et.OneSkipPayloadFull, includeFilter: x => x.Where(i => i.Id < 10)),
                new ExpectedFilteredInclude<EntityOne<int>, EntityTwo<int>>(
                    et => et.TwoSkip, "OneSkipPayloadFull", includeFilter: x => x.OrderBy(e => e.Id).Skip(1).Take(2)),
                new ExpectedFilteredInclude<EntityOne<int>, EntityBranch<int>>(
                    et => et.BranchSkip, "OneSkipPayloadFull", includeFilter: x => x.Where(e => e.Id < 20))));

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual Task Filtered_include_on_skip_navigation_then_filtered_include_on_navigation_split(bool async)
        => AssertQuery(
            async,
            ss => ss.Set<EntityThree<int>>().Include(e => e.OneSkipPayloadFull.Where(i => i.Id > 15))
                .ThenInclude(e => e.Collection.Where(i => i.Id < 5))
                .AsSplitQuery(),
            elementAsserter: (e, a) => AssertInclude(
                e, a,
                new ExpectedFilteredInclude<EntityThree<int>, EntityOne<int>>(
                    et => et.OneSkipPayloadFull, includeFilter: x => x.Where(i => i.Id > 15)),
                new ExpectedFilteredInclude<EntityOne<int>, EntityTwo<int>>(
                    et => et.Collection, "OneSkipPayloadFull", includeFilter: x => x.Where(i => i.Id < 5))));

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual Task Filtered_include_on_navigation_then_filtered_include_on_skip_navigation_split(bool async)
        => AssertQuery(
            async,
            ss => ss.Set<EntityOne<int>>().Include(e => e.Collection.Where(i => i.Id > 15))
                .ThenInclude(e => e.ThreeSkipFull.Where(i => i.Id < 5))
                .AsSplitQuery(),
            elementAsserter: (e, a) => AssertInclude(
                e, a,
                new ExpectedFilteredInclude<EntityOne<int>, EntityTwo<int>>(et => et.Collection, includeFilter: x => x.Where(i => i.Id > 15)),
                new ExpectedFilteredInclude<EntityTwo<int>, EntityThree<int>>(
                    et => et.ThreeSkipFull, "Collection", includeFilter: x => x.Where(i => i.Id < 5))));

    protected override QueryAsserter CreateQueryAsserter(TFixture fixture)
        => new RelationalQueryAsserter(
            fixture,
            RewriteExpectedQueryExpression,
            RewriteServerQueryExpression,
            ignoreEntryCount: IgnoreEntryCount);
}
