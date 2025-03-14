// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.TestModels.ManyToManyModel;

namespace Microsoft.EntityFrameworkCore.Query;

#nullable disable

public abstract class ManyToManyQueryFixtureBase : SharedStoreFixtureBase<ManyToManyContext>, IQueryFixtureBase
{
    protected override string StoreName
        => "ManyToManyQueryTest";

    public Func<DbContext> GetContextCreator()
        => () => CreateContext();

    private ManyToManyData _data;

    public ISetSource GetExpectedData()
    {
        if (_data == null)
        {
            using var context = CreateContext();
            _data = new ManyToManyData(context, false);
            context.ChangeTracker.DetectChanges();
            context.ChangeTracker.Clear();
        }

        return _data;
    }

    public IReadOnlyDictionary<Type, object> EntitySorters { get; } = new Dictionary<Type, Func<object, object>>
    {
        { typeof(EntityOne<int>), e => ((EntityOne<int>)e)?.Id },
        { typeof(EntityTwo<int>), e => ((EntityTwo<int>)e)?.Id },
        { typeof(EntityThree<int>), e => ((EntityThree<int>)e)?.Id },
        { typeof(EntityCompositeKey<int>), e => (((EntityCompositeKey<int>)e)?.Key1, ((EntityCompositeKey<int>)e)?.Key2, ((EntityCompositeKey<int>)e)?.Key3) },
        { typeof(EntityRoot<int>), e => ((EntityRoot<int>)e)?.Id },
        { typeof(EntityBranch<int>), e => ((EntityBranch<int>)e)?.Id },
        { typeof(EntityLeaf<int>), e => ((EntityLeaf<int>)e)?.Id },
        { typeof(EntityBranch2<int>), e => ((EntityBranch2<int>)e)?.Id },
        { typeof(EntityLeaf2<int>), e => ((EntityLeaf2<int>)e)?.Id },
        { typeof(EntityTableSharing1), e => ((EntityTableSharing1)e)?.Id },
        { typeof(EntityTableSharing2), e => ((EntityTableSharing2)e)?.Id },
        { typeof(UnidirectionalEntityOne), e => ((UnidirectionalEntityOne)e)?.Id },
        { typeof(UnidirectionalEntityTwo), e => ((UnidirectionalEntityTwo)e)?.Id },
        { typeof(UnidirectionalEntityThree), e => ((UnidirectionalEntityThree)e)?.Id },
        {
            typeof(UnidirectionalEntityCompositeKey), e => (((UnidirectionalEntityCompositeKey)e)?.Key1,
                ((UnidirectionalEntityCompositeKey)e)?.Key2,
                ((UnidirectionalEntityCompositeKey)e)?.Key3)
        },
        { typeof(UnidirectionalEntityRoot), e => ((UnidirectionalEntityRoot)e)?.Id },
        { typeof(UnidirectionalEntityBranch), e => ((UnidirectionalEntityBranch)e)?.Id },
        { typeof(UnidirectionalEntityLeaf), e => ((UnidirectionalEntityLeaf)e)?.Id },
    }.ToDictionary(e => e.Key, e => (object)e.Value);

    public IReadOnlyDictionary<Type, object> EntityAsserters { get; } = new Dictionary<Type, Action<object, object>>
    {
        {
            typeof(EntityOne<int>), (e, a) =>
            {
                Assert.Equal(e == null, a == null);

                if (a != null)
                {
                    var ee = (EntityOne<int>)e;
                    var aa = (EntityOne<int>)a;

                    Assert.Equal(ee.Id, aa.Id);
                    Assert.Equal(ee.Name, aa.Name);
                }
            }
        },
        {
            typeof(EntityTwo<int>), (e, a) =>
            {
                Assert.Equal(e == null, a == null);

                if (a != null)
                {
                    var ee = (EntityTwo<int>)e;
                    var aa = (EntityTwo<int>)a;

                    Assert.Equal(ee.Id, aa.Id);
                    Assert.Equal(ee.Name, aa.Name);
                }
            }
        },
        {
            typeof(EntityThree<int>), (e, a) =>
            {
                Assert.Equal(e == null, a == null);

                if (a != null)
                {
                    var ee = (EntityThree<int>)e;
                    var aa = (EntityThree<int>)a;

                    Assert.Equal(ee.Id, aa.Id);
                    Assert.Equal(ee.Name, aa.Name);
                }
            }
        },
        {
            typeof(EntityCompositeKey<int>), (e, a) =>
            {
                Assert.Equal(e == null, a == null);

                if (a != null)
                {
                    var ee = (EntityCompositeKey<int>)e;
                    var aa = (EntityCompositeKey<int>)a;

                    Assert.Equal(ee.Key1, aa.Key1);
                    Assert.Equal(ee.Key2, aa.Key2);
                    Assert.Equal(ee.Key3, aa.Key3);
                    Assert.Equal(ee.Name, aa.Name);
                }
            }
        },
        {
            typeof(EntityRoot<int>), (e, a) =>
            {
                Assert.Equal(e == null, a == null);

                if (a != null)
                {
                    var ee = (EntityRoot<int>)e;
                    var aa = (EntityRoot<int>)a;

                    Assert.Equal(ee.Id, aa.Id);
                    Assert.Equal(ee.Name, aa.Name);
                }
            }
        },
        {
            typeof(EntityBranch<int>), (e, a) =>
            {
                Assert.Equal(e == null, a == null);

                if (a != null)
                {
                    var ee = (EntityBranch<int>)e;
                    var aa = (EntityBranch<int>)a;

                    Assert.Equal(ee.Id, aa.Id);
                    Assert.Equal(ee.Name, aa.Name);
                    Assert.Equal(ee.Number, aa.Number);
                }
            }
        },
        {
            typeof(EntityLeaf<int>), (e, a) =>
            {
                Assert.Equal(e == null, a == null);

                if (a != null)
                {
                    var ee = (EntityLeaf<int>)e;
                    var aa = (EntityLeaf<int>)a;

                    Assert.Equal(ee.Id, aa.Id);
                    Assert.Equal(ee.Name, aa.Name);
                    Assert.Equal(ee.Number, aa.Number);
                    Assert.Equal(ee.IsGreen, aa.IsGreen);
                }
            }
        },
        {
            typeof(EntityBranch2<int>), (e, a) =>
            {
                Assert.Equal(e == null, a == null);

                if (a != null)
                {
                    var ee = (EntityBranch2<int>)e;
                    var aa = (EntityBranch2<int>)a;

                    Assert.Equal(ee.Id, aa.Id);
                    Assert.Equal(ee.Name, aa.Name);
                    Assert.Equal(ee.Slumber, aa.Slumber);
                }
            }
        },
        {
            typeof(EntityLeaf2<int>), (e, a) =>
            {
                Assert.Equal(e == null, a == null);

                if (a != null)
                {
                    var ee = (EntityLeaf2<int>)e;
                    var aa = (EntityLeaf2<int>)a;

                    Assert.Equal(ee.Id, aa.Id);
                    Assert.Equal(ee.Name, aa.Name);
                    Assert.Equal(ee.Slumber, aa.Slumber);
                    Assert.Equal(ee.IsBrown, aa.IsBrown);
                }
            }
        },
        {
            typeof(EntityTableSharing1), (e, a) =>
            {
                Assert.Equal(e == null, a == null);

                if (a != null)
                {
                    var ee = (EntityTableSharing1)e;
                    var aa = (EntityTableSharing1)a;

                    Assert.Equal(ee.Id, aa.Id);
                    Assert.Equal(ee.Name, aa.Name);
                }
            }
        },
        {
            typeof(EntityTableSharing2), (e, a) =>
            {
                Assert.Equal(e == null, a == null);

                if (a != null)
                {
                    var ee = (EntityTableSharing2)e;
                    var aa = (EntityTableSharing2)a;

                    Assert.Equal(ee.Id, aa.Id);
                    Assert.Equal(ee.Cucumber, aa.Cucumber);
                }
            }
        },
        {
            typeof(UnidirectionalEntityOne), (e, a) =>
            {
                Assert.Equal(e == null, a == null);

                if (a != null)
                {
                    var ee = (UnidirectionalEntityOne)e;
                    var aa = (UnidirectionalEntityOne)a;

                    Assert.Equal(ee.Id, aa.Id);
                    Assert.Equal(ee.Name, aa.Name);
                }
            }
        },
        {
            typeof(UnidirectionalEntityTwo), (e, a) =>
            {
                Assert.Equal(e == null, a == null);

                if (a != null)
                {
                    var ee = (UnidirectionalEntityTwo)e;
                    var aa = (UnidirectionalEntityTwo)a;

                    Assert.Equal(ee.Id, aa.Id);
                    Assert.Equal(ee.Name, aa.Name);
                }
            }
        },
        {
            typeof(UnidirectionalEntityThree), (e, a) =>
            {
                Assert.Equal(e == null, a == null);

                if (a != null)
                {
                    var ee = (UnidirectionalEntityThree)e;
                    var aa = (UnidirectionalEntityThree)a;

                    Assert.Equal(ee.Id, aa.Id);
                    Assert.Equal(ee.Name, aa.Name);
                }
            }
        },
        {
            typeof(UnidirectionalEntityCompositeKey), (e, a) =>
            {
                Assert.Equal(e == null, a == null);

                if (a != null)
                {
                    var ee = (UnidirectionalEntityCompositeKey)e;
                    var aa = (UnidirectionalEntityCompositeKey)a;

                    Assert.Equal(ee.Key1, aa.Key1);
                    Assert.Equal(ee.Key2, aa.Key2);
                    Assert.Equal(ee.Key3, aa.Key3);
                    Assert.Equal(ee.Name, aa.Name);
                }
            }
        },
        {
            typeof(UnidirectionalEntityRoot), (e, a) =>
            {
                Assert.Equal(e == null, a == null);

                if (a != null)
                {
                    var ee = (UnidirectionalEntityRoot)e;
                    var aa = (UnidirectionalEntityRoot)a;

                    Assert.Equal(ee.Id, aa.Id);
                    Assert.Equal(ee.Name, aa.Name);
                }
            }
        },
        {
            typeof(UnidirectionalEntityBranch), (e, a) =>
            {
                Assert.Equal(e == null, a == null);

                if (a != null)
                {
                    var ee = (UnidirectionalEntityBranch)e;
                    var aa = (UnidirectionalEntityBranch)a;

                    Assert.Equal(ee.Id, aa.Id);
                    Assert.Equal(ee.Name, aa.Name);
                    Assert.Equal(ee.Number, aa.Number);
                }
            }
        },
        {
            typeof(UnidirectionalEntityLeaf), (e, a) =>
            {
                Assert.Equal(e == null, a == null);

                if (a != null)
                {
                    var ee = (UnidirectionalEntityLeaf)e;
                    var aa = (UnidirectionalEntityLeaf)a;

                    Assert.Equal(ee.Id, aa.Id);
                    Assert.Equal(ee.Name, aa.Name);
                    Assert.Equal(ee.Number, aa.Number);
                    Assert.Equal(ee.IsGreen, aa.IsGreen);
                }
            }
        },
    }.ToDictionary(e => e.Key, e => (object)e.Value);

    protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
    {
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
            .WithMany(e => e.OneSkipShared);

        modelBuilder.Entity<EntityRoot<int>>()
            .HasMany(e => e.BranchSkipShared)
            .WithMany(e => e.RootSkipShared);

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
            .UsingEntity<JoinOneToTwo<int>>();

        // Nav:6 Payload:Yes Join:Concrete Extra:None
        modelBuilder.Entity<EntityOne<int>>()
            .HasMany(e => e.ThreeSkipPayloadFull)
            .WithMany(e => e.OneSkipPayloadFull)
            .UsingEntity<JoinOneToThreePayloadFull<int>>(
                r => r.HasOne(x => x.Three).WithMany(e => e.JoinOnePayloadFull),
                l => l.HasOne(x => x.One).WithMany(e => e.JoinThreePayloadFull));

        // Nav:4 Payload:Yes Join:Shared Extra:None
        modelBuilder.Entity<EntityOne<int>>()
            .HasMany(e => e.ThreeSkipPayloadFullShared)
            .WithMany(e => e.OneSkipPayloadFullShared)
            .UsingEntity<Dictionary<string, object>>(
                "JoinOneToThreePayloadFullShared",
                r => r.HasOne<EntityThree<int>>().WithMany(e => e.JoinOnePayloadFullShared).HasForeignKey("ThreeId"),
                l => l.HasOne<EntityOne<int>>().WithMany(e => e.JoinThreePayloadFullShared).HasForeignKey("OneId"))
            .IndexerProperty<string>("Payload");

        // Nav:6 Payload:Yes Join:Concrete Extra:Self-Ref
        modelBuilder.Entity<EntityOne<int>>()
            .HasMany(e => e.SelfSkipPayloadLeft)
            .WithMany(e => e.SelfSkipPayloadRight)
            .UsingEntity<JoinOneSelfPayload<int>>(
                l => l.HasOne(x => x.Left).WithMany(x => x.JoinSelfPayloadLeft),
                r => r.HasOne(x => x.Right).WithMany(x => x.JoinSelfPayloadRight));

        // Nav:2 Payload:No Join:Concrete Extra:Inheritance
        modelBuilder.Entity<EntityOne<int>>()
            .HasMany(e => e.BranchSkip)
            .WithMany(e => e.OneSkip)
            .UsingEntity<JoinOneToBranch>();

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
                l => l.HasOne(x => x.Two).WithMany(e => e.JoinThreeFull));

        // Nav:2 Payload:No Join:Shared Extra:Self-ref
        modelBuilder.Entity<EntityTwo<int>>()
            .HasMany(e => e.SelfSkipSharedLeft)
            .WithMany(e => e.SelfSkipSharedRight);

        // Nav:2 Payload:No Join:Shared Extra:CompositeKey
        modelBuilder.Entity<EntityTwo<int>>()
            .HasMany(e => e.CompositeKeySkipShared)
            .WithMany(e => e.TwoSkipShared);

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
                r => r.HasOne(x => x.Three).WithMany(x => x.JoinCompositeKeyFull).IsRequired());

        // Nav:2 Payload:No Join:Shared Extra:Inheritance
        modelBuilder.Entity<EntityThree<int>>()
            .HasMany(e => e.RootSkipShared)
            .WithMany(e => e.ThreeSkipShared);

        // Nav:2 Payload:No Join:Shared Extra:Inheritance,CompositeKey
        modelBuilder.Entity<EntityCompositeKey<int>>()
            .HasMany(e => e.RootSkipShared)
            .WithMany(e => e.CompositeKeySkipShared);

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
                    }));

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
            .WithMany();

        modelBuilder.Entity<UnidirectionalEntityBranch>()
            .HasMany<UnidirectionalEntityRoot>()
            .WithMany(e => e.BranchSkipShared);

        // Nav:2 Payload:No Join:Concrete Extra:None
        modelBuilder.Entity<UnidirectionalEntityOne>()
            .HasMany(e => e.TwoSkip)
            .WithMany()
            .UsingEntity<UnidirectionalJoinOneToTwo>();

        // Nav:6 Payload:Yes Join:Concrete Extra:None
        modelBuilder.Entity<UnidirectionalEntityOne>()
            .HasMany<UnidirectionalEntityThree>()
            .WithMany()
            .UsingEntity<UnidirectionalJoinOneToThreePayloadFull>(
                r => r.HasOne(x => x.Three).WithMany(e => e.JoinOnePayloadFull),
                l => l.HasOne(x => x.One).WithMany(e => e.JoinThreePayloadFull));

        // Nav:4 Payload:Yes Join:Shared Extra:None
        modelBuilder.Entity<UnidirectionalEntityOne>()
            .HasMany(e => e.ThreeSkipPayloadFullShared)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "UnidirectionalJoinOneToThreePayloadFullShared",
                r => r.HasOne<UnidirectionalEntityThree>().WithMany(e => e.JoinOnePayloadFullShared).HasForeignKey("ThreeId"),
                l => l.HasOne<UnidirectionalEntityOne>().WithMany(e => e.JoinThreePayloadFullShared).HasForeignKey("OneId"))
            .IndexerProperty<string>("Payload");

        // Nav:6 Payload:Yes Join:Concrete Extra:Self-Ref
        modelBuilder.Entity<UnidirectionalEntityOne>()
            .HasMany(e => e.SelfSkipPayloadLeft)
            .WithMany()
            .UsingEntity<UnidirectionalJoinOneSelfPayload>(
                l => l.HasOne(x => x.Left).WithMany(x => x.JoinSelfPayloadLeft),
                r => r.HasOne(x => x.Right).WithMany(x => x.JoinSelfPayloadRight));

        // Nav:2 Payload:No Join:Concrete Extra:Inheritance
        modelBuilder.Entity<UnidirectionalEntityOne>()
            .HasMany(e => e.BranchSkip)
            .WithMany()
            .UsingEntity<UnidirectionalJoinOneToBranch>();

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
                l => l.HasOne(x => x.Two).WithMany(e => e.JoinThreeFull));

        // Nav:2 Payload:No Join:Shared Extra:Self-ref
        modelBuilder.Entity<UnidirectionalEntityTwo>()
            .HasMany<UnidirectionalEntityTwo>()
            .WithMany(e => e.SelfSkipSharedRight);

        // Nav:2 Payload:No Join:Shared Extra:CompositeKey
        modelBuilder.Entity<UnidirectionalEntityTwo>()
            .HasMany<UnidirectionalEntityCompositeKey>()
            .WithMany(e => e.TwoSkipShared);

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
                r => r.HasOne(x => x.Three).WithMany(x => x.JoinCompositeKeyFull).IsRequired());

        // Nav:2 Payload:No Join:Shared Extra:Inheritance
        modelBuilder.Entity<UnidirectionalEntityThree>()
            .HasMany<UnidirectionalEntityRoot>()
            .WithMany(e => e.ThreeSkipShared);

        // Nav:2 Payload:No Join:Shared Extra:Inheritance,CompositeKey
        modelBuilder.Entity<UnidirectionalEntityCompositeKey>()
            .HasMany(e => e.RootSkipShared)
            .WithMany();

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
                    }));

        modelBuilder.SharedTypeEntity<ProxyableSharedType>(
            "PST", b =>
            {
                b.IndexerProperty<int>("Id").ValueGeneratedNever();
                b.IndexerProperty<string>("Payload");
            });
    }

    public virtual bool UseGeneratedKeys
        => false;

    protected override Task SeedAsync(ManyToManyContext context)
    {
        new ManyToManyData(context, UseGeneratedKeys);
        return context.SaveChangesAsync();
    }
}
