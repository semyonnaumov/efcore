// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Query;

#nullable disable

public abstract class SharedTypeQueryRelationalTestBase : SharedTypeQueryTestBase
{
    protected TestSqlLoggerFactory TestSqlLoggerFactory
        => (TestSqlLoggerFactory)ListLoggerFactory;

    protected void ClearLog()
        => TestSqlLoggerFactory.Clear();

    protected void AssertSql(params string[] expected)
        => TestSqlLoggerFactory.AssertBaseline(expected);

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Can_use_shared_type_entity_type_in_query_filter_with_from_sql(bool async)
    {
        var contextFactory = await InitializeAsync<ContextRelational24601>(
            seed: c => c.SeedAsync());

        using var context = contextFactory.CreateContext();
        var query = context.Set<Context24601.ViewQuery>();
        var result = async
            ? await query.ToListAsync()
            : query.ToList();

        Assert.Empty(result);
    }

    [ConditionalFact]
    public virtual async Task Ad_hoc_query_for_shared_type_entity_type_works()
    {
        var contextFactory = await InitializeAsync<ContextRelational24601>(
            seed: c => c.SeedAsync());

        using var context = contextFactory.CreateContext();

        var result = context.Database.SqlQueryRaw<Context24601.ViewQuery>(
            ((RelationalTestStore)TestStore).NormalizeDelimitersInRawString(@"SELECT * FROM [ViewQuery]"));

        Assert.Empty(await result.ToListAsync());
    }

    [ConditionalFact]
    public virtual async Task Ad_hoc_query_for_default_shared_type_entity_type_throws()
    {
        var contextFactory = await InitializeAsync<ContextRelational24601>(
            seed: c => c.SeedAsync());

        using var context = contextFactory.CreateContext();

        Assert.Equal(
            CoreStrings.ClashingSharedType("Dictionary<string, object>"),
            Assert.Throws<InvalidOperationException>(
                () => context.Database.SqlQueryRaw<Dictionary<string, object>>(@"SELECT * FROM X")).Message);
    }

    protected class ContextRelational24601(DbContextOptions options) : Context24601(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ViewQuery>()
                .HasQueryFilter(
                    e => Set<Dictionary<string, object>>("STET")
                        .FromSqlRaw("Select * from STET").Select(i => (string)i["Value"]).Contains(e.Value));
        }
    }
}
