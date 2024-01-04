// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public abstract class SharedTypeQueryTestBase : NonSharedModelTestBase
{
    protected override string StoreName
        => "SharedTypeQueryTests";

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Can_use_shared_type_entity_type_in_query_filter(bool async)
    {
        var contextFactory = await InitializeAsync<Context24601>(
            seed: c => c.Seed());

        using var context = contextFactory.CreateContext();
        var query = context.Set<Context24601.ViewQuery>();
        var result = async
            ? await query.ToListAsync()
            : query.ToList();

        Assert.Empty(result);
    }

    public class Context24601(DbContextOptions options) : DbContext(options)
    {
        public void Seed()
        {
            Set<Dictionary<string, object>>("STET").Add(new Dictionary<string, object> { ["Value"] = "Maumar" });

            SaveChanges();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.SharedTypeEntity<Dictionary<string, object>>(
                "STET",
                b =>
                {
                    b.IndexerProperty<int>("Id");
                    b.IndexerProperty<string>("Value");
                });

            modelBuilder.Entity<ViewQuery>().HasNoKey()
                .HasQueryFilter(e => Set<Dictionary<string, object>>("STET").Select(i => (string)i["Value"]).Contains(e.Value));
        }

        public class ViewQuery
        {
            public string Value { get; set; }
        }
    }
}
