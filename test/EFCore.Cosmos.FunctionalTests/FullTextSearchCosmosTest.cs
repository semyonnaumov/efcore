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
    public virtual async Task Use_FullTextContains_in_predicate_using_constant_argument()
    {
        await using var context = CreateContext();

        var result = await context.Set<FtsAnimals>()
            .Where(x => EF.Functions.FullTextContains(x.Description, "beaver"))
            .ToListAsync();

        Assert.Equal(3, result.Count);
        Assert.True(result.All(x => x.Description.Contains("beaver")));

        AssertSql(
"""
SELECT VALUE c
FROM root c
WHERE FullTextContains(c["Description"], "beaver")
""");
    }

    [ConditionalFact]
    public virtual async Task Use_FullTextContains_in_predicate_using_parameter_argument()
    {
        await using var context = CreateContext();

        var beaver = "beaver";
        var result = await context.Set<FtsAnimals>()
            .Where(x => EF.Functions.FullTextContains(x.Description, beaver))
            .ToListAsync();

        Assert.Equal(3, result.Count);
        Assert.True(result.All(x => x.Description.Contains("beaver")));

        AssertSql(
"""
@beaver='beaver'

SELECT VALUE c
FROM root c
WHERE FullTextContains(c["Description"], @beaver)
""");
    }

    [ConditionalFact]
    public virtual async Task Use_FullTextContainsAny_in_predicate()
    {
        await using var context = CreateContext();

        var beaver = "beaver";

        var result = await context.Set<FtsAnimals>()
            .Where(x => EF.Functions.FullTextContainsAny(x.Description, beaver, "bat"))
            .ToListAsync();

        Assert.Equal(4, result.Count);
        Assert.True(result.All(x => x.Description.Contains("beaver") || x.Description.Contains("bat")));

        AssertSql(
"""
@beaver='beaver'

SELECT VALUE c
FROM root c
WHERE FullTextContainsAny(c["Description"], @beaver, "bat")
""");
    }

    [ConditionalFact]
    public virtual async Task Use_FullTextContainsAll_in_predicate()
    {
        await using var context = CreateContext();

        var beaver = "beaver";
        var result = await context.Set<FtsAnimals>()
            .Where(x => EF.Functions.FullTextContainsAll(x.Description, beaver, "salmon", "frog"))
            .ToListAsync();

        Assert.Equal(1, result.Count);
        Assert.True(result.All(x => x.Description.Contains("beaver") && x.Description.Contains("salmon") && x.Description.Contains("frog")));

        AssertSql(
"""
@beaver='beaver'

SELECT VALUE c
FROM root c
WHERE FullTextContainsAny(c["Description"], @beaver, "bat")
""");
    }

    [ConditionalFact]
    public virtual async Task Use_FullTextContains_in_projection_using_constant_argument()
    {
        await using var context = CreateContext();

        var result = await context.Set<FtsAnimals>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Description, ContainsBeaver = EF.Functions.FullTextContains(x.Description, "beaver") })
            .ToListAsync();

        Assert.True(result.All(x => x.Description.Contains("beaver") == x.ContainsBeaver));

        AssertSql(
"""
SELECT c["Description"], FullTextContains(c["Description"], "beaver") AS ContainsBeaver
FROM root c
ORDER BY c["Id"]
""");
    }

    [ConditionalFact]
    public virtual async Task Use_FullTextContains_in_projection_using_parameter_argument()
    {
        await using var context = CreateContext();

        var beaver = "beaver";
        var result = await context.Set<FtsAnimals>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Description, ContainsBeaver = EF.Functions.FullTextContains(x.Description, beaver) })
            .ToListAsync();

        Assert.True(result.All(x => x.Description.Contains("beaver") == x.ContainsBeaver));

        AssertSql(
"""
@beaver='beaver'

SELECT c["Description"], FullTextContains(c["Description"], @beaver) AS ContainsBeaver
FROM root c
ORDER BY c["Id"]
""");
    }

    [ConditionalFact]
    public virtual async Task Use_FullTextContains_in_projection_using_complex_expression()
    {
        await using var context = CreateContext();

        var beaver = "beaver";
        var result = await context.Set<FtsAnimals>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.Description, ContainsBeaverOrSometimesDuck = EF.Functions.FullTextContains(x.Description, x.Id < 3 ? beaver : "duck") })
            .ToListAsync();

        Assert.True(result.All(x => (x.Id < 3 ? x.Description.Contains("beaver") : x.Description.Contains("duck")) == x.ContainsBeaverOrSometimesDuck));

        AssertSql(
"""
@beaver='beaver'

SELECT c["Id"], c["Description"], FullTextContains(c["Description"], ((c["Id"] < 3) ? @beaver : "duck")) AS ContainsBeaverOrSometimesDuck
FROM root c
ORDER BY c["Id"]
""");
    }

    [ConditionalFact]
    public virtual async Task Use_FullTextContains_non_property()
    {
        await using var context = CreateContext();

        var result = await context.Set<FtsAnimals>()
            .Where(x => EF.Functions.FullTextContains("habitat is the natural environment in which a particular species thrives", x.PartitionKey))
            .ToListAsync();

        AssertSql(
"""
@beaver='beaver'

SELECT c["Id"], c["Description"], FullTextContains(c["Description"], ((c["Id"] < 3) ? @beaver : "duck")) AS ContainsBeaverOrSometimesDuck
FROM root c
ORDER BY c["Id"]
""");
    }



















    private class FtsAnimals
    {
        public int Id { get; set; }

        public string PartitionKey { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string Description { get; set; } = null!;
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
            modelBuilder.Entity<FtsAnimals>(b =>
            {
                b.ToContainer("FtsAnimals");
                b.HasPartitionKey(x => x.PartitionKey);
                b.Property(x => x.Name).IsFullText();
                b.HasIndex(x => x.Name).ForFullText();

                b.Property(x => x.Description).IsFullText();
                b.HasIndex(x => x.Description).ForFullText();
            });
        }

        protected override Task SeedAsync(PoolableDbContext context)
        {
            var landAnimals = new FtsAnimals
            {
                Id = 1,
                PartitionKey = "habitat",
                Name = "List of several land animals",
                Description = "bison, beaver, moose, fox, wolf, marten, horse, shrew, hare, duck, turtle, frog",
            };

            var waterAnimals = new FtsAnimals
            {
                Id = 2,
                PartitionKey = "habitat",
                Name = "List of several water animals",
                Description = "beaver, otter, duck, dolphin, salmon, turtle, frog",
            };

            var airAnimals = new FtsAnimals
            {
                Id = 3,
                PartitionKey = "habitat",
                Name = "List of several air animals",
                Description = "duck, bat, eagle, butterfly, sparrow",
            };

            var mammals = new FtsAnimals
            {
                Id = 4,
                PartitionKey = "taxonomy",
                Name = "List of several mammals",
                Description = "bison, beaver, moose, fox, wolf, marten, horse, shrew, hare, bat",
            };

            var avians = new FtsAnimals
            {
                Id = 5,
                PartitionKey = "taxonomy",
                Name = "List of several avians",
                Description = "duck, eagle, sparrow",
            };

            context.Set<FtsAnimals>().AddRange(landAnimals, waterAnimals, airAnimals, mammals, avians);
            return context.SaveChangesAsync();
        }

        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ListLoggerFactory;

        protected override ITestStoreFactory TestStoreFactory
            => CosmosTestStoreFactory.Instance;
    }
}
#pragma warning restore EF9104
