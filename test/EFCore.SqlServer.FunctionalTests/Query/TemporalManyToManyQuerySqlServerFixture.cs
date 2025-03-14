// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestModels.ManyToManyModel;

namespace Microsoft.EntityFrameworkCore.Query;

#nullable disable

public class TemporalManyToManyQuerySqlServerFixture : ManyToManyQueryFixtureBase, ITestSqlLoggerFactory
{
    protected override string StoreName
        => "TemporalManyToManyQueryTest";

    public DateTime ChangesDate { get; private set; }

    protected override ITestStoreFactory TestStoreFactory
        => SqlServerTestStoreFactory.Instance;

    public TestSqlLoggerFactory TestSqlLoggerFactory
        => (TestSqlLoggerFactory)ListLoggerFactory;

    protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
    {
        modelBuilder.Entity<EntityTableSharing1>().ToTable("TableSharing");
        modelBuilder.Entity<EntityTableSharing2>(
            b =>
            {
                b.HasOne<EntityTableSharing1>().WithOne().HasForeignKey<EntityTableSharing2>(e => e.Id);
                b.ToTable("TableSharing");
            });

        modelBuilder.Entity<EntityOne<int>>().ToTable(tb => tb.IsTemporal());
        modelBuilder.Entity<EntityTwo<int>>().ToTable(tb => tb.IsTemporal());
        modelBuilder.Entity<EntityThree<int>>().ToTable(tb => tb.IsTemporal());
        modelBuilder.Entity<EntityCompositeKey<int>>().ToTable(tb => tb.IsTemporal());
        modelBuilder.Entity<EntityRoot<int>>().ToTable(tb => tb.IsTemporal());
        modelBuilder.Entity<UnidirectionalEntityOne>().ToTable(tb => tb.IsTemporal());
        modelBuilder.Entity<UnidirectionalEntityTwo>().ToTable(tb => tb.IsTemporal());
        modelBuilder.Entity<UnidirectionalEntityThree>().ToTable(tb => tb.IsTemporal());
        modelBuilder.Entity<UnidirectionalEntityCompositeKey>().ToTable(tb => tb.IsTemporal());
        modelBuilder.Entity<UnidirectionalEntityRoot>().ToTable(tb => tb.IsTemporal());

        modelBuilder.Entity<EntityOne<int>>().Property(e => e.Id).ValueGeneratedNever();
        modelBuilder.Entity<EntityTwo<int>>().Property(e => e.Id).ValueGeneratedNever();
        modelBuilder.Entity<EntityThree<int>>().Property(e => e.Id).ValueGeneratedNever();
        modelBuilder.Entity<EntityCompositeKey<int>>().HasKey(
            e => new
            {
                e.Key1,
                e.Key2,
                e.Key3
            });
        modelBuilder.Entity<EntityRoot<int>>().Property(e => e.Id).ValueGeneratedNever();
        modelBuilder.Entity<EntityBranch<int>>().HasBaseType<EntityRoot<int>>();
        modelBuilder.Entity<EntityLeaf<int>>().HasBaseType<EntityBranch<int>>();
        modelBuilder.Entity<EntityLeaf<int>>().HasBaseType<EntityBranch<int>>();
        modelBuilder.Entity<EntityBranch2<int>>().HasBaseType<EntityRoot<int>>();
        modelBuilder.Entity<EntityLeaf2<int>>().HasBaseType<EntityBranch2<int>>();
        modelBuilder.Entity<EntityTableSharing1>().Property(e => e.Id).ValueGeneratedNever();
        modelBuilder.Entity<EntityTableSharing2>().Property(e => e.Id).ValueGeneratedNever();

        modelBuilder.Entity<UnidirectionalEntityOne>().Property(e => e.Id).ValueGeneratedNever();
        modelBuilder.Entity<UnidirectionalEntityTwo>().Property(e => e.Id).ValueGeneratedNever();
        modelBuilder.Entity<UnidirectionalEntityThree>().Property(e => e.Id).ValueGeneratedNever();
        modelBuilder.Entity<UnidirectionalEntityCompositeKey>().HasKey(
            e => new
            {
                e.Key1,
                e.Key2,
                e.Key3
            });
        modelBuilder.Entity<UnidirectionalEntityRoot>().Property(e => e.Id).ValueGeneratedNever();
        modelBuilder.Entity<UnidirectionalEntityBranch>().HasBaseType<UnidirectionalEntityRoot>();
        modelBuilder.Entity<UnidirectionalEntityLeaf>().HasBaseType<UnidirectionalEntityBranch>();

        modelBuilder.Entity<EntityOne<int>>()
            .HasMany(e => e.Collection)
            .WithOne(e => e.CollectionInverse)
            .HasForeignKey(e => e.CollectionInverseId);

        modelBuilder.Entity<EntityOne<int>>()
            .HasOne(e => e.Reference)
            .WithOne(e => e.ReferenceInverse)
            .HasForeignKey<EntityTwo<int>>(e => e.ReferenceInverseId);

        modelBuilder.Entity<EntityOne<int>>()
            .HasMany(e => e.TwoSkipShared)
            .WithMany(e => e.OneSkipShared)
            .UsingEntity(t => t.ToTable(tb => tb.IsTemporal()));

        modelBuilder.Entity<EntityRoot<int>>()
            .HasMany(e => e.BranchSkipShared)
            .WithMany(e => e.RootSkipShared)
            .UsingEntity(t => t.ToTable(tb => tb.IsTemporal()));

        modelBuilder.Entity<EntityBranch2<int>>()
            .HasMany(e => e.SelfSkipSharedLeft)
            .WithMany(e => e.SelfSkipSharedRight);

        modelBuilder.Entity<EntityBranch2<int>>()
            .HasMany(e => e.Leaf2SkipShared)
            .WithMany(e => e.Branch2SkipShared);

        modelBuilder.Entity<EntityTableSharing1>()
            .HasMany(e => e.TableSharing2Shared)
            .WithMany(e => e.TableSharing1Shared);

        // Nav:2 Payload:No Join:Concrete Extra:None
        modelBuilder.Entity<EntityOne<int>>()
            .HasMany(e => e.TwoSkip)
            .WithMany(e => e.OneSkip)
            .UsingEntity<JoinOneToTwo<int>>()
            .ToTable(tb => tb.IsTemporal());

        // Nav:6 Payload:Yes Join:Concrete Extra:None
        modelBuilder.Entity<EntityOne<int>>()
            .HasMany(e => e.ThreeSkipPayloadFull)
            .WithMany(e => e.OneSkipPayloadFull)
            .UsingEntity<JoinOneToThreePayloadFull<int>>(
                r => r.HasOne(x => x.Three).WithMany(e => e.JoinOnePayloadFull),
                l => l.HasOne(x => x.One).WithMany(e => e.JoinThreePayloadFull))
            .ToTable(tb => tb.IsTemporal());

        // Nav:4 Payload:Yes Join:Shared Extra:None
        modelBuilder.Entity<EntityOne<int>>()
            .HasMany(e => e.ThreeSkipPayloadFullShared)
            .WithMany(e => e.OneSkipPayloadFullShared)
            .UsingEntity<Dictionary<string, object>>(
                "JoinOneToThreePayloadFullShared",
                r => r.HasOne<EntityThree<int>>().WithMany(e => e.JoinOnePayloadFullShared).HasForeignKey("ThreeId"),
                l => l.HasOne<EntityOne<int>>().WithMany(e => e.JoinThreePayloadFullShared).HasForeignKey("OneId"))
            .ToTable(tb => tb.IsTemporal())
            .IndexerProperty<string>("Payload");

        // Nav:6 Payload:Yes Join:Concrete Extra:Self-Ref
        modelBuilder.Entity<EntityOne<int>>()
            .HasMany(e => e.SelfSkipPayloadLeft)
            .WithMany(e => e.SelfSkipPayloadRight)
            .UsingEntity<JoinOneSelfPayload<int>>(
                l => l.HasOne(x => x.Left).WithMany(x => x.JoinSelfPayloadLeft),
                r => r.HasOne(x => x.Right).WithMany(x => x.JoinSelfPayloadRight))
            .ToTable(tb => tb.IsTemporal());

        // Nav:2 Payload:No Join:Concrete Extra:Inheritance
        modelBuilder.Entity<EntityOne<int>>()
            .HasMany(e => e.BranchSkip)
            .WithMany(e => e.OneSkip)
            .UsingEntity<JoinOneToBranch>()
            .ToTable(tb => tb.IsTemporal());

        modelBuilder.Entity<EntityTwo<int>>()
            .HasOne(e => e.Reference)
            .WithOne(e => e.ReferenceInverse)
            .HasForeignKey<EntityThree<int>>(e => e.ReferenceInverseId);

        modelBuilder.Entity<EntityTwo<int>>()
            .HasMany(e => e.Collection)
            .WithOne(e => e.CollectionInverse)
            .HasForeignKey(e => e.CollectionInverseId);

        // Nav:6 Payload:No Join:Concrete Extra:None
        modelBuilder.Entity<EntityTwo<int>>()
            .HasMany(e => e.ThreeSkipFull)
            .WithMany(e => e.TwoSkipFull)
            .UsingEntity<JoinTwoToThree<int>>(
                r => r.HasOne(x => x.Three).WithMany(e => e.JoinTwoFull),
                l => l.HasOne(x => x.Two).WithMany(e => e.JoinThreeFull))
            .ToTable(tb => tb.IsTemporal());

        // Nav:2 Payload:No Join:Shared Extra:Self-ref
        modelBuilder.Entity<EntityTwo<int>>()
            .HasMany(e => e.SelfSkipSharedLeft)
            .WithMany(e => e.SelfSkipSharedRight)
            .UsingEntity(t => t.ToTable(tb => tb.IsTemporal()));

        // Nav:2 Payload:No Join:Shared Extra:CompositeKey
        modelBuilder.Entity<EntityTwo<int>>()
            .HasMany(e => e.CompositeKeySkipShared)
            .WithMany(e => e.TwoSkipShared)
            .UsingEntity(t => t.ToTable(tb => tb.IsTemporal()));

        // Nav:6 Payload:No Join:Concrete Extra:CompositeKey
        modelBuilder.Entity<EntityThree<int>>()
            .HasMany(e => e.CompositeKeySkipFull)
            .WithMany(e => e.ThreeSkipFull)
            .UsingEntity<JoinThreeToCompositeKeyFull<int>>(
                l => l.HasOne(x => x.Composite).WithMany(x => x.JoinThreeFull).HasForeignKey(
                    e => new
                    {
                        e.CompositeId1,
                        e.CompositeId2,
                        e.CompositeId3
                    }).IsRequired(),
                r => r.HasOne(x => x.Three).WithMany(x => x.JoinCompositeKeyFull).IsRequired())
            .ToTable(tb => tb.IsTemporal());

        // Nav:2 Payload:No Join:Shared Extra:Inheritance
        modelBuilder.Entity<EntityThree<int>>().HasMany(e => e.RootSkipShared).WithMany(e => e.ThreeSkipShared)
            .UsingEntity(t => t.ToTable(tb => tb.IsTemporal()));

        // Nav:2 Payload:No Join:Shared Extra:Inheritance,CompositeKey
        modelBuilder.Entity<EntityCompositeKey<int>>()
            .HasMany(e => e.RootSkipShared)
            .WithMany(e => e.CompositeKeySkipShared)
            .UsingEntity(t => t.ToTable(tb => tb.IsTemporal()));

        // Nav:6 Payload:No Join:Concrete Extra:Inheritance,CompositeKey
        modelBuilder.Entity<EntityCompositeKey<int>>()
            .HasMany(e => e.LeafSkipFull)
            .WithMany(e => e.CompositeKeySkipFull)
            .UsingEntity<JoinCompositeKeyToLeaf<int>>(
                r => r.HasOne(x => x.Leaf).WithMany(x => x.JoinCompositeKeyFull),
                l => l.HasOne(x => x.Composite).WithMany(x => x.JoinLeafFull).HasForeignKey(
                    e => new
                    {
                        e.CompositeId1,
                        e.CompositeId2,
                        e.CompositeId3
                    }))
            .ToTable(tb => tb.IsTemporal());

        modelBuilder.Entity<UnidirectionalEntityOne>()
            .HasMany(e => e.Collection)
            .WithOne(e => e.CollectionInverse)
            .HasForeignKey(e => e.CollectionInverseId);

        modelBuilder.Entity<UnidirectionalEntityOne>()
            .HasOne(e => e.Reference)
            .WithOne(e => e.ReferenceInverse)
            .HasForeignKey<UnidirectionalEntityTwo>(e => e.ReferenceInverseId);

        modelBuilder.Entity<UnidirectionalEntityOne>()
            .HasMany(e => e.TwoSkipShared)
            .WithMany()
            .UsingEntity(t => t.ToTable(tb => tb.IsTemporal()));

        modelBuilder.Entity<UnidirectionalEntityBranch>()
            .HasMany<UnidirectionalEntityRoot>()
            .WithMany(e => e.BranchSkipShared)
            .UsingEntity(t => t.ToTable(tb => tb.IsTemporal()));

        // Nav:2 Payload:No Join:Concrete Extra:None
        modelBuilder.Entity<UnidirectionalEntityOne>()
            .HasMany(e => e.TwoSkip)
            .WithMany()
            .UsingEntity<UnidirectionalJoinOneToTwo>()
            .ToTable(tb => tb.IsTemporal());

        // Nav:6 Payload:Yes Join:Concrete Extra:None
        modelBuilder.Entity<UnidirectionalEntityOne>()
            .HasMany<UnidirectionalEntityThree>()
            .WithMany()
            .UsingEntity<UnidirectionalJoinOneToThreePayloadFull>(
                r => r.HasOne(x => x.Three).WithMany(e => e.JoinOnePayloadFull),
                l => l.HasOne(x => x.One).WithMany(e => e.JoinThreePayloadFull))
            .ToTable(tb => tb.IsTemporal());

        // Nav:4 Payload:Yes Join:Shared Extra:None
        modelBuilder.Entity<UnidirectionalEntityOne>()
            .HasMany(e => e.ThreeSkipPayloadFullShared)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "UnidirectionalJoinOneToThreePayloadFullShared",
                r => r.HasOne<UnidirectionalEntityThree>().WithMany(e => e.JoinOnePayloadFullShared).HasForeignKey("ThreeId"),
                l => l.HasOne<UnidirectionalEntityOne>().WithMany(e => e.JoinThreePayloadFullShared).HasForeignKey("OneId"))
            .ToTable(tb => tb.IsTemporal())
            .IndexerProperty<string>("Payload");

        // Nav:6 Payload:Yes Join:Concrete Extra:Self-Ref
        modelBuilder.Entity<UnidirectionalEntityOne>()
            .HasMany(e => e.SelfSkipPayloadLeft)
            .WithMany()
            .UsingEntity<UnidirectionalJoinOneSelfPayload>(
                l => l.HasOne(x => x.Left).WithMany(x => x.JoinSelfPayloadLeft),
                r => r.HasOne(x => x.Right).WithMany(x => x.JoinSelfPayloadRight))
            .ToTable(tb => tb.IsTemporal());

        // Nav:2 Payload:No Join:Concrete Extra:Inheritance
        modelBuilder.Entity<UnidirectionalEntityOne>()
            .HasMany(e => e.BranchSkip)
            .WithMany()
            .UsingEntity<UnidirectionalJoinOneToBranch>()
            .ToTable(tb => tb.IsTemporal());

        modelBuilder.Entity<UnidirectionalEntityTwo>()
            .HasOne(e => e.Reference)
            .WithOne(e => e.ReferenceInverse)
            .HasForeignKey<UnidirectionalEntityThree>(e => e.ReferenceInverseId);

        modelBuilder.Entity<UnidirectionalEntityTwo>()
            .HasMany(e => e.Collection)
            .WithOne(e => e.CollectionInverse)
            .HasForeignKey(e => e.CollectionInverseId);

        // Nav:6 Payload:No Join:Concrete Extra:None
        modelBuilder.Entity<UnidirectionalEntityTwo>()
            .HasMany<UnidirectionalEntityThree>()
            .WithMany(e => e.TwoSkipFull)
            .UsingEntity<UnidirectionalJoinTwoToThree>(
                r => r.HasOne(x => x.Three).WithMany(e => e.JoinTwoFull),
                l => l.HasOne(x => x.Two).WithMany(e => e.JoinThreeFull))
            .ToTable(tb => tb.IsTemporal());

        // Nav:2 Payload:No Join:Shared Extra:Self-ref
        modelBuilder.Entity<UnidirectionalEntityTwo>()
            .HasMany<UnidirectionalEntityTwo>()
            .WithMany(e => e.SelfSkipSharedRight)
            .UsingEntity(t => t.ToTable(tb => tb.IsTemporal()));

        // Nav:2 Payload:No Join:Shared Extra:CompositeKey
        modelBuilder.Entity<UnidirectionalEntityTwo>()
            .HasMany<UnidirectionalEntityCompositeKey>()
            .WithMany(e => e.TwoSkipShared)
            .UsingEntity(t => t.ToTable(tb => tb.IsTemporal()));

        // Nav:6 Payload:No Join:Concrete Extra:CompositeKey
        modelBuilder.Entity<UnidirectionalEntityThree>()
            .HasMany<UnidirectionalEntityCompositeKey>()
            .WithMany(e => e.ThreeSkipFull)
            .UsingEntity<UnidirectionalJoinThreeToCompositeKeyFull>(
                l => l.HasOne(x => x.Composite).WithMany(x => x.JoinThreeFull).HasForeignKey(
                    e => new
                    {
                        e.CompositeId1,
                        e.CompositeId2,
                        e.CompositeId3
                    }).IsRequired(),
                r => r.HasOne(x => x.Three).WithMany(x => x.JoinCompositeKeyFull).IsRequired())
            .ToTable(tb => tb.IsTemporal());

        // Nav:2 Payload:No Join:Shared Extra:Inheritance
        modelBuilder.Entity<UnidirectionalEntityThree>()
            .HasMany<UnidirectionalEntityRoot>()
            .WithMany(e => e.ThreeSkipShared)
            .UsingEntity(t => t.ToTable(tb => tb.IsTemporal()));

        // Nav:2 Payload:No Join:Shared Extra:Inheritance,CompositeKey
        modelBuilder.Entity<UnidirectionalEntityCompositeKey>()
            .HasMany(e => e.RootSkipShared)
            .WithMany()
            .UsingEntity(t => t.ToTable(tb => tb.IsTemporal()));

        // Nav:6 Payload:No Join:Concrete Extra:Inheritance,CompositeKey
        modelBuilder.Entity<UnidirectionalEntityCompositeKey>()
            .HasMany<UnidirectionalEntityLeaf>()
            .WithMany(e => e.CompositeKeySkipFull)
            .UsingEntity<UnidirectionalJoinCompositeKeyToLeaf>(
                r => r.HasOne(x => x.Leaf).WithMany(x => x.JoinCompositeKeyFull),
                l => l.HasOne(x => x.Composite).WithMany(x => x.JoinLeafFull).HasForeignKey(
                    e => new
                    {
                        e.CompositeId1,
                        e.CompositeId2,
                        e.CompositeId3
                    }))
            .ToTable(tb => tb.IsTemporal());

        modelBuilder.SharedTypeEntity<ProxyableSharedType>(
            "PST", b =>
            {
                b.IndexerProperty<int>("Id").ValueGeneratedNever();
                b.IndexerProperty<string>("Payload");
            });
    }

    protected override async Task SeedAsync(ManyToManyContext context)
    {
        await base.SeedAsync(context);

        ChangesDate = new DateTime(2010, 1, 1);

        context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is EntityThree<int>).Select(e => e.Entity));
        context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is EntityTwo<int>).Select(e => e.Entity));
        context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is EntityOne<int>).Select(e => e.Entity));
        context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is EntityCompositeKey<int>).Select(e => e.Entity));
        context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is EntityRoot<int>).Select(e => e.Entity));
        context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is UnidirectionalEntityThree).Select(e => e.Entity));
        context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is UnidirectionalEntityTwo).Select(e => e.Entity));
        context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is UnidirectionalEntityOne).Select(e => e.Entity));
        context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is UnidirectionalEntityCompositeKey).Select(e => e.Entity));
        context.RemoveRange(context.ChangeTracker.Entries().Where(e => e.Entity is UnidirectionalEntityRoot).Select(e => e.Entity));
        await context.SaveChangesAsync();

        var tableNames = new List<string>
        {
            "EntityCompositeKeys",
            "EntityOneEntityTwo",
            "EntityOnes",
            "EntityTwos",
            "EntityThrees",
            "EntityRoots",
            "EntityRootEntityThree",
            "JoinCompositeKeyToLeaf",
            "EntityCompositeKeyEntityRoot",
            "JoinOneSelfPayload",
            "JoinOneToBranch",
            "JoinOneToThreePayloadFull",
            "JoinOneToThreePayloadFullShared",
            "JoinOneToTwo",
            "JoinThreeToCompositeKeyFull",
            "EntityTwoEntityTwo",
            "EntityCompositeKeyEntityTwo",
            "JoinTwoToThree",
            "UnidirectionalEntityCompositeKeys",
            "UnidirectionalEntityOneUnidirectionalEntityTwo",
            "UnidirectionalEntityOnes",
            "UnidirectionalEntityTwos",
            "UnidirectionalEntityThrees",
            "UnidirectionalEntityRoots",
            "UnidirectionalEntityRootUnidirectionalEntityThree",
            "UnidirectionalJoinCompositeKeyToLeaf",
            "UnidirectionalEntityCompositeKeyUnidirectionalEntityRoot",
            "UnidirectionalJoinOneSelfPayload",
            "UnidirectionalJoinOneToBranch",
            "UnidirectionalJoinOneToThreePayloadFull",
            "UnidirectionalJoinOneToThreePayloadFullShared",
            "UnidirectionalJoinOneToTwo",
            "UnidirectionalJoinThreeToCompositeKeyFull",
            "UnidirectionalEntityTwoUnidirectionalEntityTwo",
            "UnidirectionalEntityCompositeKeyUnidirectionalEntityTwo",
            "UnidirectionalJoinTwoToThree",
        };

        foreach (var tableName in tableNames)
        {
            await context.Database.ExecuteSqlRawAsync($"ALTER TABLE [{tableName}] SET (SYSTEM_VERSIONING = OFF)");
            await context.Database.ExecuteSqlRawAsync($"ALTER TABLE [{tableName}] DROP PERIOD FOR SYSTEM_TIME");

            await context.Database.ExecuteSqlRawAsync($"UPDATE [{tableName + "History"}] SET PeriodStart = '2000-01-01T01:00:00.0000000Z'");
            await context.Database.ExecuteSqlRawAsync($"UPDATE [{tableName + "History"}] SET PeriodEnd = '2020-07-01T07:00:00.0000000Z'");

            await context.Database.ExecuteSqlRawAsync($"ALTER TABLE [{tableName}] ADD PERIOD FOR SYSTEM_TIME ([PeriodStart], [PeriodEnd])");
            await context.Database.ExecuteSqlRawAsync(
                $"ALTER TABLE [{tableName}] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[{tableName + "History"}]))");
        }
    }
}
