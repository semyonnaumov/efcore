// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Query;

public abstract class AdHocJsonQueryTestBase : NonSharedModelTestBase
{
    protected override string StoreName
        => "AdHocJsonQueryTest";

    #region 32310

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Contains_on_nested_collection_with_init_only_navigation(bool async)
    {
        var contextFactory = await InitializeAsync<Context32310>(seed: Seed32310);
        await using var context = contextFactory.CreateContext();

        var query = context.Pubs
            .Where(u => u.Visits.DaysVisited.Contains(new DateOnly(2023, 1, 1)));

        var result = async
            ? await query.FirstOrDefaultAsync()!
            : query.FirstOrDefault()!;

        Assert.Equal("FBI", result.Name);
        Assert.Equal(new DateOnly(2023, 1, 1), result.Visits.DaysVisited.Single());
    }

    protected virtual void Seed32310(Context32310 context)
    {
        var user = new Context32310.Pub
        {
            Name = "FBI",
            Visits = new Context32310.Visits
            {
                LocationTag = "tag",
                DaysVisited = [new(2023, 1, 1)]
            }
        };

        context.Add(user);
        context.SaveChanges();
    }

    public class Context32310(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Pub> Pubs => Set<Pub>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Pub>(b => { b.OwnsOne(e => e.Visits).ToJson(); });

        public class Pub
        {
            public int Id { get; set; }
            public required string Name { get; set; }
            public Visits Visits { get; set; } = null!;
        }

        public class Visits
        {
            public string LocationTag { get; set; }
            public required List<DateOnly> DaysVisited { get; init; }
        }
    }

    #endregion

    #region 29219

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Optional_json_properties_materialized_as_null_when_the_element_in_json_is_not_present(bool async)
    {
        var contextFactory = await InitializeAsync<Context29219>(
            seed: Seed29219);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entities.Where(x => x.Id == 3);

            var result = async
                ? await query.SingleAsync()
                : query.Single();

            Assert.Equal(3, result.Id);
            Assert.Null(result.Reference.NullableScalar);
            Assert.Null(result.Collection[0].NullableScalar);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Can_project_nullable_json_property_when_the_element_in_json_is_not_present(bool async)
    {
        var contextFactory = await InitializeAsync<Context29219>(
            seed: Seed29219);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entities.OrderBy(x => x.Id).Select(x => x.Reference.NullableScalar);

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(3, result.Count);
            Assert.Equal(11, result[0]);
            Assert.Null(result[1]);
            Assert.Null(result[2]);
        }
    }

    protected abstract void Seed29219(Context29219 ctx);

    public class Context29219(DbContextOptions options) : DbContext(options)
    {
        public DbSet<MyEntity> Entities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MyEntity>().Property(x => x.Id).ValueGeneratedNever();
            modelBuilder.Entity<MyEntity>().OwnsOne(x => x.Reference).ToJson();
            modelBuilder.Entity<MyEntity>().OwnsMany(x => x.Collection).ToJson();
        }

        public class MyEntity
        {
            public int Id { get; set; }
            public MyJsonEntity Reference { get; set; }
            public List<MyJsonEntity> Collection { get; set; }
        }

        public class MyJsonEntity
        {
            public int NonNullableScalar { get; set; }
            public int? NullableScalar { get; set; }
        }
    }

    #endregion

    #region 30028

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Accessing_missing_navigation_works(bool async)
    {
        var contextFactory = await InitializeAsync<Context30028>(seed: Seed30028);
        using (var context = contextFactory.CreateContext())
        {
            var result = context.Entities.OrderBy(x => x.Id).ToList();
            Assert.Equal(4, result.Count);
            Assert.NotNull(result[0].Json.Collection);
            Assert.NotNull(result[0].Json.OptionalReference);
            Assert.NotNull(result[0].Json.RequiredReference);

            Assert.Null(result[1].Json.Collection);
            Assert.NotNull(result[1].Json.OptionalReference);
            Assert.NotNull(result[1].Json.RequiredReference);

            Assert.NotNull(result[2].Json.Collection);
            Assert.Null(result[2].Json.OptionalReference);
            Assert.NotNull(result[2].Json.RequiredReference);

            Assert.NotNull(result[3].Json.Collection);
            Assert.NotNull(result[3].Json.OptionalReference);
            Assert.Null(result[3].Json.RequiredReference);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Missing_navigation_works_with_deduplication(bool async)
    {
        var contextFactory = await InitializeAsync<Context30028>(seed: Seed30028);
        using (var context = contextFactory.CreateContext())
        {
            var result = context.Entities.OrderBy(x => x.Id).Select(
                x => new
                {
                    x,
                    x.Json,
                    x.Json.OptionalReference,
                    x.Json.RequiredReference,
                    NestedOptional = x.Json.OptionalReference.Nested,
                    NestedRequired = x.Json.RequiredReference.Nested,
                    x.Json.Collection,
                }).AsNoTracking().ToList();

            Assert.Equal(4, result.Count);
            Assert.NotNull(result[0].OptionalReference);
            Assert.NotNull(result[0].RequiredReference);
            Assert.NotNull(result[0].NestedOptional);
            Assert.NotNull(result[0].NestedRequired);
            Assert.NotNull(result[0].Collection);

            Assert.NotNull(result[1].OptionalReference);
            Assert.NotNull(result[1].RequiredReference);
            Assert.NotNull(result[1].NestedOptional);
            Assert.NotNull(result[1].NestedRequired);
            Assert.Null(result[1].Collection);

            Assert.Null(result[2].OptionalReference);
            Assert.NotNull(result[2].RequiredReference);
            Assert.Null(result[2].NestedOptional);
            Assert.NotNull(result[2].NestedRequired);
            Assert.NotNull(result[2].Collection);

            Assert.NotNull(result[3].OptionalReference);
            Assert.Null(result[3].RequiredReference);
            Assert.NotNull(result[3].NestedOptional);
            Assert.Null(result[3].NestedRequired);
            Assert.NotNull(result[3].Collection);
        }
    }

    protected abstract void Seed30028(Context30028 ctx);

    public class Context30028(DbContextOptions options) : DbContext(options)
    {
        public DbSet<MyEntity> Entities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<MyEntity>(
                b =>
                {
                    b.Property(x => x.Id).ValueGeneratedNever();
                    b.OwnsOne(
                        x => x.Json, nb =>
                        {
                            nb.ToJson();
                            nb.OwnsMany(x => x.Collection, nnb => nnb.OwnsOne(x => x.Nested));
                            nb.OwnsOne(x => x.OptionalReference, nnb => nnb.OwnsOne(x => x.Nested));
                            nb.OwnsOne(x => x.RequiredReference, nnb => nnb.OwnsOne(x => x.Nested));
                            nb.Navigation(x => x.RequiredReference).IsRequired();
                        });
                });

        public class MyEntity
        {
            public int Id { get; set; }
            public MyJsonRootEntity Json { get; set; }
        }

        public class MyJsonRootEntity
        {
            public string RootName { get; set; }
            public MyJsonBranchEntity RequiredReference { get; set; }
            public MyJsonBranchEntity OptionalReference { get; set; }
            public List<MyJsonBranchEntity> Collection { get; set; }
        }

        public class MyJsonBranchEntity
        {
            public string BranchName { get; set; }
            public MyJsonLeafEntity Nested { get; set; }
        }

        public class MyJsonLeafEntity
        {
            public string LeafName { get; set; }
        }
    }

    #endregion

    #region 32939

    [ConditionalFact]
    public virtual async Task Project_json_with_no_properties()
    {
        var contextFactory = await InitializeAsync<Context32939>(seed: Seed30028);
        using var context = contextFactory.CreateContext();
        context.Entities.ToList();
    }

    protected void Seed30028(Context32939 ctx)
    {
        var entity = new Context32939.Entity
        {
            Empty = new Context32939.JsonEmpty(),
            FieldOnly = new Context32939.JsonFieldOnly()
        };

        ctx.Entities.Add(entity);
        ctx.SaveChanges();
    }

    public class Context32939(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Entity> Entities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Entity>().Property(x => x.Id).ValueGeneratedNever();
            modelBuilder.Entity<Entity>().OwnsOne(x => x.Empty, b => b.ToJson());
            modelBuilder.Entity<Entity>().OwnsOne(x => x.FieldOnly, b => b.ToJson());
        }

        public class Entity
        {
            public int Id { get; set; }
            public JsonEmpty Empty { get; set; }
            public JsonFieldOnly FieldOnly { get; set; }

        }

        public class JsonEmpty
        {
        }

        public class JsonFieldOnly
        {
            public int Field;
        }
    }

    #endregion

    #region 33046

    protected abstract void Seed33046(Context33046 ctx);

    [ConditionalFact]
    public virtual async Task Query_with_nested_json_collection_mapped_to_private_field_via_IReadOnlyList()
    {
        var contextFactory = await InitializeAsync<Context33046>(seed: Seed33046);
        using var context = contextFactory.CreateContext();
        var query = context.Reviews.ToList();
        Assert.Equal(1, query.Count);
    }

    public class Context33046(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Review>().Property(x => x.Id).ValueGeneratedNever();
            modelBuilder.Entity<Review>().OwnsMany(x => x.Rounds, ownedBuilder =>
            {
                ownedBuilder.ToJson();
                ownedBuilder.OwnsMany(r => r.SubRounds);
            });
        }

        public class Review
        {
            public int Id { get; set; }

#pragma warning disable IDE0044 // Add readonly modifier
            private List<ReviewRound> _rounds = [];
#pragma warning restore IDE0044 // Add readonly modifier
            public IReadOnlyList<ReviewRound> Rounds => _rounds.AsReadOnly();
        }

        public class ReviewRound
        {
            public int RoundNumber { get; set; }

#pragma warning disable IDE0044 // Add readonly modifier
            private List<SubRound> _subRounds = [];
#pragma warning restore IDE0044 // Add readonly modifier
            public IReadOnlyList<SubRound> SubRounds => _subRounds.AsReadOnly();
        }

        public class SubRound
        {
            public int SubRoundNumber { get; set; }
        }
    }

    #endregion

    #region ArrayOfPrimitives

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Project_json_array_of_primitives_on_reference(bool async)
    {
        var contextFactory = await InitializeAsync<ContextArrayOfPrimitives>(
            seed: SeedArrayOfPrimitives);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entities.OrderBy(x => x.Id).Select(x => new { x.Reference.IntArray, x.Reference.ListOfString });

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(3, result[0].IntArray.Length);
            Assert.Equal(3, result[0].ListOfString.Count);
            Assert.Equal(3, result[1].IntArray.Length);
            Assert.Equal(3, result[1].ListOfString.Count);
        }
    }

    [ConditionalTheory(Skip = "Issue #32611")]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Project_json_array_of_primitives_on_collection(bool async)
    {
        var contextFactory = await InitializeAsync<ContextArrayOfPrimitives>(
            seed: SeedArrayOfPrimitives);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entities.OrderBy(x => x.Id).Select(x => new { x.Collection[0].IntArray, x.Collection[1].ListOfString });

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(3, result[0].IntArray.Length);
            Assert.Equal(2, result[0].ListOfString.Count);
            Assert.Equal(3, result[1].IntArray.Length);
            Assert.Equal(2, result[1].ListOfString.Count);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Project_element_of_json_array_of_primitives(bool async)
    {
        var contextFactory = await InitializeAsync<ContextArrayOfPrimitives>(
            seed: SeedArrayOfPrimitives);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entities.OrderBy(x => x.Id).Select(
                x => new { ArrayElement = x.Reference.IntArray[0], ListElement = x.Reference.ListOfString[1] });

            var result = async
                ? await query.ToListAsync()
                : query.ToList();
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Predicate_based_on_element_of_json_array_of_primitives1(bool async)
    {
        var contextFactory = await InitializeAsync<ContextArrayOfPrimitives>(
            seed: SeedArrayOfPrimitives);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entities.Where(x => x.Reference.IntArray[0] == 1);

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal(1, result[0].Reference.IntArray[0]);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Predicate_based_on_element_of_json_array_of_primitives2(bool async)
    {
        var contextFactory = await InitializeAsync<ContextArrayOfPrimitives>(
            seed: SeedArrayOfPrimitives);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entities.Where(x => x.Reference.ListOfString[1] == "Bar");

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal("Bar", result[0].Reference.ListOfString[1]);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Predicate_based_on_element_of_json_array_of_primitives3(bool async)
    {
        var contextFactory = await InitializeAsync<ContextArrayOfPrimitives>(
            seed: SeedArrayOfPrimitives);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entities.Where(
                    x => x.Reference.IntArray.AsQueryable().ElementAt(0) == 1
                        || x.Reference.ListOfString.AsQueryable().ElementAt(1) == "Bar")
                .OrderBy(e => e.Id);

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal(1, result[0].Reference.IntArray[0]);
            Assert.Equal("Bar", result[0].Reference.ListOfString[1]);
        }
    }

    protected abstract void SeedArrayOfPrimitives(ContextArrayOfPrimitives ctx);

    public class ContextArrayOfPrimitives(DbContextOptions options) : DbContext(options)
    {
        public DbSet<MyEntity> Entities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MyEntity>().Property(x => x.Id).ValueGeneratedNever();
            modelBuilder.Entity<MyEntity>().OwnsOne(
                x => x.Reference, b => b.ToJson());

            modelBuilder.Entity<MyEntity>().OwnsMany(
                x => x.Collection, b => b.ToJson());
        }

        public class MyEntity
        {
            public int Id { get; set; }
            public MyJsonEntity Reference { get; set; }
            public List<MyJsonEntity> Collection { get; set; }
        }

        public class MyJsonEntity
        {
            public int[] IntArray { get; set; }
            public List<string> ListOfString { get; set; }
        }
    }

    #endregion

    #region JunkInJson

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Junk_in_json_basic_tracking(bool async)
    {
        var contextFactory = await InitializeAsync<ContextJunkInJson>(
            seed: SeedJunkInJson);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entities;

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal(2, result[0].Collection.Count);
            Assert.Equal(2, result[0].CollectionWithCtor.Count);
            Assert.Equal(2, result[0].Reference.NestedCollection.Count);
            Assert.NotNull(result[0].Reference.NestedReference);
            Assert.Equal(2, result[0].ReferenceWithCtor.NestedCollection.Count);
            Assert.NotNull(result[0].ReferenceWithCtor.NestedReference);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Junk_in_json_basic_no_tracking(bool async)
    {
        var contextFactory = await InitializeAsync<ContextJunkInJson>(
            seed: SeedJunkInJson);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entities.AsNoTracking();

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal(2, result[0].Collection.Count);
            Assert.Equal(2, result[0].CollectionWithCtor.Count);
            Assert.Equal(2, result[0].Reference.NestedCollection.Count);
            Assert.NotNull(result[0].Reference.NestedReference);
            Assert.Equal(2, result[0].ReferenceWithCtor.NestedCollection.Count);
            Assert.NotNull(result[0].ReferenceWithCtor.NestedReference);
        }
    }

    protected abstract void SeedJunkInJson(ContextJunkInJson ctx);

    public class ContextJunkInJson(DbContextOptions options) : DbContext(options)
    {
        public DbSet<MyEntity> Entities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MyEntity>().Property(x => x.Id).ValueGeneratedNever();
            modelBuilder.Entity<MyEntity>().OwnsOne(
                x => x.Reference, b =>
                {
                    b.ToJson();
                    b.OwnsOne(x => x.NestedReference);
                    b.OwnsMany(x => x.NestedCollection);
                });
            modelBuilder.Entity<MyEntity>().OwnsOne(
                x => x.ReferenceWithCtor, b =>
                {
                    b.ToJson();
                    b.OwnsOne(x => x.NestedReference);
                    b.OwnsMany(x => x.NestedCollection);
                });
            modelBuilder.Entity<MyEntity>().OwnsMany(
                x => x.Collection, b =>
                {
                    b.ToJson();
                    b.OwnsOne(x => x.NestedReference);
                    b.OwnsMany(x => x.NestedCollection);
                });
            modelBuilder.Entity<MyEntity>().OwnsMany(
                x => x.CollectionWithCtor, b =>
                {
                    b.ToJson();
                    b.OwnsOne(x => x.NestedReference);
                    b.OwnsMany(x => x.NestedCollection);
                });
        }

        public class MyEntity
        {
            public int Id { get; set; }
            public MyJsonEntity Reference { get; set; }
            public MyJsonEntityWithCtor ReferenceWithCtor { get; set; }
            public List<MyJsonEntity> Collection { get; set; }
            public List<MyJsonEntityWithCtor> CollectionWithCtor { get; set; }
        }

        public class MyJsonEntity
        {
            public string Name { get; set; }
            public double Number { get; set; }

            public MyJsonEntityNested NestedReference { get; set; }
            public List<MyJsonEntityNested> NestedCollection { get; set; }
        }

        public class MyJsonEntityNested
        {
            public DateTime DoB { get; set; }
        }

        public class MyJsonEntityWithCtor(bool myBool, string name)
        {
            public bool MyBool { get; set; } = myBool;
            public string Name { get; set; } = name;

            public MyJsonEntityWithCtorNested NestedReference { get; set; }
            public List<MyJsonEntityWithCtorNested> NestedCollection { get; set; }
        }

        public class MyJsonEntityWithCtorNested(DateTime doB)
        {
            public DateTime DoB { get; set; } = doB;
        }
    }

    #endregion

    #region TrickyBuffering

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Tricky_buffering_basic(bool async)
    {
        var contextFactory = await InitializeAsync<ContextTrickyBuffering>(
            seed: SeedTrickyBuffering);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entities;

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal("r1", result[0].Reference.Name);
            Assert.Equal(7, result[0].Reference.Number);
            Assert.Equal(new DateTime(2000, 1, 1), result[0].Reference.NestedReference.DoB);
            Assert.Equal(2, result[0].Reference.NestedCollection.Count);
        }
    }

    protected abstract void SeedTrickyBuffering(ContextTrickyBuffering ctx);

    public class ContextTrickyBuffering(DbContextOptions options) : DbContext(options)
    {
        public DbSet<MyEntity> Entities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MyEntity>().Property(x => x.Id).ValueGeneratedNever();
            modelBuilder.Entity<MyEntity>().OwnsOne(
                x => x.Reference, b =>
                {
                    b.ToJson();
                    b.OwnsOne(x => x.NestedReference);
                    b.OwnsMany(x => x.NestedCollection);
                });
        }

        public class MyEntity
        {
            public int Id { get; set; }
            public MyJsonEntity Reference { get; set; }
        }

        public class MyJsonEntity
        {
            public string Name { get; set; }
            public int Number { get; set; }
            public MyJsonEntityNested NestedReference { get; set; }
            public List<MyJsonEntityNested> NestedCollection { get; set; }
        }

        public class MyJsonEntityNested
        {
            public DateTime DoB { get; set; }
        }
    }

    #endregion

    #region ShadowProperties

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Shadow_properties_basic_tracking(bool async)
    {
        var contextFactory = await InitializeAsync<ContextShadowProperties>(
            seed: SeedShadowProperties);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entities;

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal(2, result[0].Collection.Count);
            Assert.Equal(2, result[0].CollectionWithCtor.Count);
            Assert.NotNull(result[0].Reference);
            Assert.NotNull(result[0].ReferenceWithCtor);

            var referenceEntry = context.ChangeTracker.Entries().Single(x => x.Entity == result[0].Reference);
            Assert.Equal("Foo", referenceEntry.Property("ShadowString").CurrentValue);

            var referenceCtorEntry = context.ChangeTracker.Entries().Single(x => x.Entity == result[0].ReferenceWithCtor);
            Assert.Equal(143, referenceCtorEntry.Property("Shadow_Int").CurrentValue);

            var collectionEntry1 = context.ChangeTracker.Entries().Single(x => x.Entity == result[0].Collection[0]);
            var collectionEntry2 = context.ChangeTracker.Entries().Single(x => x.Entity == result[0].Collection[1]);
            Assert.Equal(5.5, collectionEntry1.Property("ShadowDouble").CurrentValue);
            Assert.Equal(20.5, collectionEntry2.Property("ShadowDouble").CurrentValue);

            var collectionCtorEntry1 = context.ChangeTracker.Entries().Single(x => x.Entity == result[0].CollectionWithCtor[0]);
            var collectionCtorEntry2 = context.ChangeTracker.Entries().Single(x => x.Entity == result[0].CollectionWithCtor[1]);
            Assert.Equal((byte)6, collectionCtorEntry1.Property("ShadowNullableByte").CurrentValue);
            Assert.Null(collectionCtorEntry2.Property("ShadowNullableByte").CurrentValue);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Shadow_properties_basic_no_tracking(bool async)
    {
        var contextFactory = await InitializeAsync<ContextShadowProperties>(
            seed: SeedShadowProperties);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entities.AsNoTracking();

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal(2, result[0].Collection.Count);
            Assert.Equal(2, result[0].CollectionWithCtor.Count);
            Assert.NotNull(result[0].Reference);
            Assert.NotNull(result[0].ReferenceWithCtor);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Project_shadow_properties_from_json_entity(bool async)
    {
        var contextFactory = await InitializeAsync<ContextShadowProperties>(
            seed: SeedShadowProperties);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entities.Select(
                x => new
                {
                    ShadowString = EF.Property<string>(x.Reference, "ShadowString"),
                    ShadowInt = EF.Property<int>(x.ReferenceWithCtor, "Shadow_Int"),
                });

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal("Foo", result[0].ShadowString);
            Assert.Equal(143, result[0].ShadowInt);
        }
    }

    protected abstract void SeedShadowProperties(ContextShadowProperties ctx);

    public class ContextShadowProperties(DbContextOptions options) : DbContext(options)
    {
        public DbSet<MyEntity> Entities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MyEntity>().Property(x => x.Id).ValueGeneratedNever();
            modelBuilder.Entity<MyEntity>().OwnsOne(
                x => x.Reference, b =>
                {
                    b.ToJson();
                    b.Property<string>("ShadowString");
                });
            modelBuilder.Entity<MyEntity>().OwnsOne(
                x => x.ReferenceWithCtor, b =>
                {
                    b.ToJson();
                    b.Property<int>("Shadow_Int").HasJsonPropertyName("ShadowInt");
                });
            modelBuilder.Entity<MyEntity>().OwnsMany(
                x => x.Collection, b =>
                {
                    b.ToJson();
                    b.Property<double>("ShadowDouble");
                });
            modelBuilder.Entity<MyEntity>().OwnsMany(
                x => x.CollectionWithCtor, b =>
                {
                    b.ToJson();
                    b.Property<byte?>("ShadowNullableByte");
                });
        }

        public class MyEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public MyJsonEntity Reference { get; set; }
            public List<MyJsonEntity> Collection { get; set; }
            public MyJsonEntityWithCtor ReferenceWithCtor { get; set; }
            public List<MyJsonEntityWithCtor> CollectionWithCtor { get; set; }
        }

        public class MyJsonEntity
        {
            public string Name { get; set; }
        }

        public class MyJsonEntityWithCtor(string name)
        {
            public string Name { get; set; } = name;
        }
    }

    #endregion

    #region LazyLoadingProxies

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Project_proxies_entity_with_json(bool async)
    {
        var contextFactory = await InitializeAsync<ContextLazyLoadingProxies>(
            seed: SeedLazyLoadingProxies,
            onConfiguring: OnConfiguringLazyLoadingProxies,
            addServices: AddServicesLazyLoadingProxies);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entities;

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(2, result.Count);
        }
    }

    protected void OnConfiguringLazyLoadingProxies(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseLazyLoadingProxies();

    protected IServiceCollection AddServicesLazyLoadingProxies(IServiceCollection addServices)
        => addServices.AddEntityFrameworkProxies();

    private void SeedLazyLoadingProxies(ContextLazyLoadingProxies ctx)
    {
        var r1 = new ContextLazyLoadingProxies.MyJsonEntityWithCtor("r1", 1);
        var c11 = new ContextLazyLoadingProxies.MyJsonEntity { Name = "c11", Number = 11 };
        var c12 = new ContextLazyLoadingProxies.MyJsonEntity { Name = "c12", Number = 12 };
        var c13 = new ContextLazyLoadingProxies.MyJsonEntity { Name = "c13", Number = 13 };

        var r2 = new ContextLazyLoadingProxies.MyJsonEntityWithCtor("r2", 2);
        var c21 = new ContextLazyLoadingProxies.MyJsonEntity { Name = "c21", Number = 21 };
        var c22 = new ContextLazyLoadingProxies.MyJsonEntity { Name = "c22", Number = 22 };

        var e1 = new ContextLazyLoadingProxies.MyEntity
        {
            Id = 1,
            Name = "e1",
            Reference = r1,
            Collection =
            [
                c11,
                c12,
                c13
            ]
        };

        var e2 = new ContextLazyLoadingProxies.MyEntity
        {
            Id = 2,
            Name = "e2",
            Reference = r2,
            Collection = [c21, c22]
        };

        ctx.Entities.AddRange(e1, e2);
        ctx.SaveChanges();
    }

    public class ContextLazyLoadingProxies(DbContextOptions options) : DbContext(options)
    {
        public DbSet<MyEntity> Entities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MyEntity>().Property(x => x.Id).ValueGeneratedNever();
            modelBuilder.Entity<MyEntity>().OwnsOne(x => x.Reference, b => b.ToJson());
            modelBuilder.Entity<MyEntity>().OwnsMany(x => x.Collection, b => b.ToJson());
        }

        public class MyEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public virtual MyJsonEntityWithCtor Reference { get; set; }
            public virtual List<MyJsonEntity> Collection { get; set; }
        }

        public class MyJsonEntityWithCtor(string name, int number)
        {
            public string Name { get; set; } = name;
            public int Number { get; set; } = number;
        }

        public class MyJsonEntity
        {
            public string Name { get; set; }
            public int Number { get; set; }
        }
    }

    #endregion

    #region NotICollection

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Not_ICollection_basic_projection(bool async)
    {
        var contextFactory = await InitializeAsync<ContextNotICollection>(
            seed: SeedNotICollection);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entities;

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(2, result.Count);
        }
    }

    protected abstract void SeedNotICollection(ContextNotICollection ctx);

    public class ContextNotICollection(DbContextOptions options) : DbContext(options)
    {
        public DbSet<MyEntity> Entities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MyEntity>().Property(x => x.Id).ValueGeneratedNever();
            modelBuilder.Entity<MyEntity>().OwnsOne(cr => cr.Json, nb =>
            {
                nb.ToJson();
                nb.OwnsMany(x => x.Collection);
            });
        }

        public class MyEntity
        {
            public int Id { get; set; }

            public MyJsonEntity Json { get; set; }
        }

        public class MyJsonEntity
        {
            private readonly List<MyJsonNested> _collection = [];

            public IEnumerable<MyJsonNested> Collection => _collection.AsReadOnly();
        }

        public class MyJsonNested
        {
            public string Foo { get; set; }
            public int Bar { get; set; }
        }
    }

    #endregion

    protected TestSqlLoggerFactory TestSqlLoggerFactory
        => (TestSqlLoggerFactory)ListLoggerFactory;
}
