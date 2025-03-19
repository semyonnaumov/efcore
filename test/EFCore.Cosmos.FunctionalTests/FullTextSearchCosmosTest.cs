// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Cosmos.Extensions;

namespace Microsoft.EntityFrameworkCore;

#pragma warning disable EF9104

public class FullTextSearchCosmosTest : IClassFixture<FullTextSearchCosmosTest.FullTextSearchFixture>
{
    public FullTextSearchCosmosTest(FullTextSearchFixture fixture, ITestOutputHelper testOutputHelper)
    {
        Fixture = fixture;
        _testOutputHelper = testOutputHelper;
        fixture.TestSqlLoggerFactory.Clear();
    }

    protected FullTextSearchFixture Fixture { get; }

    private readonly ITestOutputHelper _testOutputHelper;

    [ConditionalFact]
    public virtual async Task Test1()
    {
        await using var context = CreateContext();

        var query = await context.Set<FtsEntity>()
            .Where(x => EF.Functions.FullTextContains(x.Name, "entity"))
            .ToListAsync();

        AssertSql(
            """
FROM root c
""");
    }

    [ConditionalFact]
    public virtual async Task Test2()
    {
        await using var context = CreateContext();

        var query = await context.Set<FtsEntity>()
            .Where(x => EF.Functions.FullTextContainsAll(x.Name, "Foo", "Bar", "Baz"))
            .ToListAsync();

        AssertSql(
            """
FROM root c
""");
    }

    [ConditionalFact]
    public virtual async Task Test3()
    {
        await using var context = CreateContext();

        var query = await context.Set<FtsEntity>()
            .Where(x => EF.Functions.FullTextContainsAll(x.Name, "Foo"))
            .ToListAsync();

        AssertSql(
            """
FROM root c
""");
    }



    private class FtsEntity
    {
        public int Id { get; set; }

        public string PartitionKey { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string Description { get; set; } = null!;

        public int Number { get; set; }
    }

    protected DbContext CreateContext()
        => Fixture.CreateContext();

    private void AssertSql(params string[] expected)
        => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

    public class FullTextSearchFixture : SharedStoreFixtureBase<PoolableDbContext>
    {
        protected override string StoreName
            => "FullTextSearchTest";

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            modelBuilder.Entity<FtsEntity>(b =>
            {
                b.ToContainer("FtsEntities");
                b.HasPartitionKey(x => x.PartitionKey);
                b.Property(x => x.Name).IsFullText();
                b.HasIndex(x => x.Name).ForFullText();

                b.Property(x => x.Description).IsFullText();
                b.HasIndex(x => x.Description).ForFullText();
            });
        }

        protected override Task SeedAsync(PoolableDbContext context)
        {
            return context.SaveChangesAsync();
        }

        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ListLoggerFactory;

        protected override ITestStoreFactory TestStoreFactory
            => CosmosTestStoreFactory.Instance;
    }
}
#pragma warning restore EF9104
