// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace Microsoft.EntityFrameworkCore;

public abstract class OwnedEntityQueryTestBase : NonSharedModelTestBase
{
    protected override string StoreName
        => "OwnedEntityQueryTests";

    #region 9202

    [ConditionalFact]
    public virtual async Task Include_collection_for_entity_with_owned_type_works()
    {
        var contextFactory = await InitializeAsync<Context9202>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Movies.Include(m => m.Cast);
            var result = query.ToList();

            Assert.Single(result);
            Assert.Equal(3, result[0].Cast.Count);
            Assert.NotNull(result[0].Details);
            Assert.True(result[0].Cast.All(a => a.Details != null));
        }

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Movies.Include("Cast");
            var result = query.ToList();

            Assert.Single(result);
            Assert.Equal(3, result[0].Cast.Count);
            Assert.NotNull(result[0].Details);
            Assert.True(result[0].Cast.All(a => a.Details != null));
        }
    }

    protected class Context9202 : DbContext
    {
        public Context9202(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Movie9202> Movies { get; set; }
        public DbSet<Actor9202> Actors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Movie9202>().HasMany(m => m.Cast).WithOne();
            modelBuilder.Entity<Movie9202>().OwnsOne(m => m.Details);
            modelBuilder.Entity<Actor9202>().OwnsOne(m => m.Details);
        }

        public void Seed()
        {
            var av = new Actor9202 { Name = "Alicia Vikander", Details = new Details9202 { Info = "Best actress ever" } };
            var oi = new Actor9202 { Name = "Oscar Isaac", Details = new Details9202 { Info = "Best actor ever made" } };
            var dg = new Actor9202 { Name = "Domhnall Gleeson", Details = new Details9202 { Info = "Second best actor ever" } };
            var em = new Movie9202
            {
                Title = "Ex Machina",
                Cast = new List<Actor9202>
                {
                    av,
                    oi,
                    dg
                },
                Details = new Details9202 { Info = "Best movie ever made" }
            };

            Actors.AddRange(av, oi, dg);
            Movies.Add(em);
            SaveChanges();
        }

        public class Movie9202
        {
            public int Id { get; set; }
            public string Title { get; set; }

            public List<Actor9202> Cast { get; set; }

            public Details9202 Details { get; set; }
        }

        public class Actor9202
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Details9202 Details { get; set; }
        }

        public class Details9202
        {
            public string Info { get; set; }
            public int Rating { get; set; }
        }
    }

    #endregion

    #region 13079

    [ConditionalFact]
    public virtual async Task Multilevel_owned_entities_determine_correct_nullability()
    {
        var contextFactory = await InitializeAsync<Context13079>();
        using var context = contextFactory.CreateContext();
        await context.AddAsync(new Context13079.BaseEntity13079());
        context.SaveChanges();
    }

    protected class Context13079 : DbContext
    {
        public virtual DbSet<BaseEntity13079> BaseEntities { get; set; }

        public Context13079(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<DerivedEntity13079>().OwnsOne(e => e.Data, b => b.OwnsOne(e => e.SubData));

        public class BaseEntity13079
        {
            public int Id { get; set; }
        }

        public class DerivedEntity13079 : BaseEntity13079
        {
            public int Property { get; set; }
            public OwnedData13079 Data { get; set; }
        }

        public class OwnedData13079
        {
            public int Property { get; set; }
            public OwnedSubData13079 SubData { get; set; }
        }

        public class OwnedSubData13079
        {
            public int Property { get; set; }
        }
    }

    #endregion

    #region 13157

    [ConditionalFact]
    public virtual async Task Correlated_subquery_with_owned_navigation_being_compared_to_null_works()
    {
        var contextFactory = await InitializeAsync<Context13157>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var partners = context.Partners
                .Select(
                    x => new
                    {
                        Addresses = x.Addresses.Select(
                            y => new
                            {
                                Turnovers = y.Turnovers == null
                                    ? null
                                    : new { y.Turnovers.AmountIn }
                            }).ToList()
                    }).ToList();

            Assert.Single(partners);
            Assert.Collection(
                partners[0].Addresses,
                t =>
                {
                    Assert.NotNull(t.Turnovers);
                    Assert.Equal(10, t.Turnovers.AmountIn);
                },
                t =>
                {
                    Assert.Null(t.Turnovers);
                });
        }
    }

    protected class Context13157 : DbContext
    {
        public virtual DbSet<Partner13157> Partners { get; set; }

        public Context13157(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Address13157>().OwnsOne(x => x.Turnovers);

        public void Seed()
        {
            AddRange(
                new Partner13157
                {
                    Addresses = new List<Address13157>
                    {
                        new() { Turnovers = new AddressTurnovers13157 { AmountIn = 10 } }, new() { Turnovers = null },
                    }
                }
            );

            SaveChanges();
        }

        public class Partner13157
        {
            public int Id { get; set; }
            public ICollection<Address13157> Addresses { get; set; }
        }

        public class Address13157
        {
            public int Id { get; set; }
            public AddressTurnovers13157 Turnovers { get; set; }
        }

        public class AddressTurnovers13157
        {
            public int AmountIn { get; set; }
        }
    }

    #endregion

    #region 14911

    [ConditionalFact]
    public virtual async Task Owned_entity_multiple_level_in_aggregate()
    {
        var contextFactory = await InitializeAsync<Context14911>(seed: c => c.Seed());
        using var context = contextFactory.CreateContext();
        var aggregate = context.Set<Context14911.Aggregate14911>().OrderByDescending(e => e.Id).FirstOrDefault();
        Assert.Equal(10, aggregate.FirstValueObject.SecondValueObjects[0].FourthValueObject.FifthValueObjects[0].AnyValue);
        Assert.Equal(20, aggregate.FirstValueObject.SecondValueObjects[0].ThirdValueObjects[0].FourthValueObject.FifthValueObjects[0].AnyValue);
    }

    protected class Context14911 : DbContext
    {
        public Context14911(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Aggregate14911>(
                builder =>
                {
                    builder.HasKey(e => e.Id);
                    builder.OwnsOne(
                        e => e.FirstValueObject, dr =>
                        {
                            dr.OwnsMany(
                                d => d.SecondValueObjects, c =>
                                {
                                    c.Property<int>("Id").IsRequired();
                                    c.HasKey("Id");
                                    c.OwnsOne(
                                        b => b.FourthValueObject, b =>
                                        {
                                            b.OwnsMany(
                                                t => t.FifthValueObjects, sp =>
                                                {
                                                    sp.Property<int>("Id").IsRequired();
                                                    sp.HasKey("Id");
                                                    sp.Property(e => e.AnyValue).IsRequired();
                                                    sp.WithOwner().HasForeignKey("SecondValueObjectId");
                                                });
                                        });
                                    c.OwnsMany(
                                        b => b.ThirdValueObjects, b =>
                                        {
                                            b.Property<int>("Id").IsRequired();
                                            b.HasKey("Id");

                                            b.OwnsOne(
                                                d => d.FourthValueObject, dpd =>
                                                {
                                                    dpd.OwnsMany(
                                                        d => d.FifthValueObjects, sp =>
                                                        {
                                                            sp.Property<int>("Id").IsRequired();
                                                            sp.HasKey("Id");
                                                            sp.Property(e => e.AnyValue).IsRequired();
                                                            sp.WithOwner().HasForeignKey("ThirdValueObjectId");
                                                        });
                                                });
                                            b.WithOwner().HasForeignKey("SecondValueObjectId");
                                        });
                                    c.WithOwner().HasForeignKey("AggregateId");
                                });
                        });
                });

        public void Seed()
        {
            var aggregate = new Aggregate14911
            {
                FirstValueObject = new FirstValueObject14911
                {
                    SecondValueObjects = new List<SecondValueObject14911>
                    {
                        new()
                        {
                            FourthValueObject =
                                new FourthValueObject14911
                                {
                                    FifthValueObjects = new List<FifthValueObject14911> { new() { AnyValue = 10 } }
                                },
                            ThirdValueObjects = new List<ThirdValueObject14911>
                            {
                                new()
                                {
                                    FourthValueObject = new FourthValueObject14911
                                    {
                                        FifthValueObjects = new List<FifthValueObject14911> { new() { AnyValue = 20 } }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            Set<Aggregate14911>().Add(aggregate);
            SaveChanges();
        }

        public class Aggregate14911
        {
            public int Id { get; set; }
            public FirstValueObject14911 FirstValueObject { get; set; }
        }

        public class FirstValueObject14911
        {
            public int Value { get; set; }
            public List<SecondValueObject14911> SecondValueObjects { get; set; }
        }

        public class SecondValueObject14911
        {
            public FourthValueObject14911 FourthValueObject { get; set; }
            public List<ThirdValueObject14911> ThirdValueObjects { get; set; }
        }

        public class ThirdValueObject14911
        {
            public FourthValueObject14911 FourthValueObject { get; set; }
        }

        public class FourthValueObject14911
        {
            public int Value { get; set; }
            public List<FifthValueObject14911> FifthValueObjects { get; set; }
        }

        public class FifthValueObject14911
        {
            public int AnyValue { get; set; }
        }
    }

    #endregion

    #region 18582

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Projecting_correlated_collection_property_for_owned_entity(bool async)
    {
        var contextFactory = await InitializeAsync<Context18582>(seed: c => c.Seed());

        using var context = contextFactory.CreateContext();
        var query = context.Warehouses.Select(
            x => new WarehouseModel18582
            {
                WarehouseCode = x.WarehouseCode,
                DestinationCountryCodes = x.DestinationCountries.Select(c => c.CountryCode).ToArray()
            }).AsNoTracking();

        var result = async
            ? await query.ToListAsync()
            : query.ToList();

        var warehouseModel = Assert.Single(result);
        Assert.Equal("W001", warehouseModel.WarehouseCode);
        Assert.True(new[] { "US", "CA" }.SequenceEqual(warehouseModel.DestinationCountryCodes));
    }

    protected class Context18582 : DbContext
    {
        public Context18582(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Warehouse18582> Warehouses { get; set; }

        public void Seed()
        {
            Add(
                new Warehouse18582
                {
                    WarehouseCode = "W001",
                    DestinationCountries =
                    {
                        new WarehouseDestinationCountry18582 { Id = "1", CountryCode = "US" },
                        new WarehouseDestinationCountry18582 { Id = "2", CountryCode = "CA" }
                    }
                });

            SaveChanges();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Warehouse18582>()
                .OwnsMany(x => x.DestinationCountries)
                .WithOwner()
                .HasForeignKey(x => x.WarehouseCode)
                .HasPrincipalKey(x => x.WarehouseCode);
    }

    protected class Warehouse18582
    {
        public int Id { get; set; }
        public string WarehouseCode { get; set; }
        public ICollection<WarehouseDestinationCountry18582> DestinationCountries { get; set; } = new HashSet<WarehouseDestinationCountry18582>();
    }

    protected class WarehouseDestinationCountry18582
    {
        public string Id { get; set; }
        public string WarehouseCode { get; set; }
        public string CountryCode { get; set; }
    }

    protected class WarehouseModel18582
    {
        public string WarehouseCode { get; set; }

        public ICollection<string> DestinationCountryCodes { get; set; }
    }

    #endregion

    #region 19138

    [ConditionalFact]
    public virtual async Task Accessing_scalar_property_in_derived_type_projection_does_not_load_owned_navigations()
    {
        var contextFactory = await InitializeAsync<Context19138>(seed: c => c.Seed());
        using var context = contextFactory.CreateContext();
        var result = context.BaseEntities
            .Select(b => context.OtherEntities.Where(o => o.OtherEntityData == ((Context19138.SubEntity19138)b).Data).FirstOrDefault())
            .ToList();

        Assert.Equal("A", Assert.Single(result).OtherEntityData);
    }

    protected class Context19138 : DbContext
    {
        public DbSet<BaseEntity19138> BaseEntities { get; set; }
        public DbSet<OtherEntity19138> OtherEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BaseEntity19138>();
            modelBuilder.Entity<SubEntity19138>().OwnsOne(se => se.Owned);
            modelBuilder.Entity<OtherEntity19138>();
        }

        public Context19138(DbContextOptions options)
            : base(options)
        {
        }

        public void Seed()
        {
            Add(new OtherEntity19138 { OtherEntityData = "A" });
            Add(new SubEntity19138 { Data = "A" });

            SaveChanges();
        }

        public class BaseEntity19138
        {
            public int Id { get; set; }
        }

        public class SubEntity19138 : BaseEntity19138
        {
            public string Data { get; set; }
            public Owned19138 Owned { get; set; }
        }

        public class Owned19138
        {
            public string OwnedData { get; set; }
            public int Value { get; set; }
        }

        public class OtherEntity19138
        {
            public int Id { get; set; }
            public string OtherEntityData { get; set; }
        }
    }

    #endregion

    #region 20277

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Multiple_single_result_in_projection_containing_owned_types(bool async)
    {
        var contextFactory = await InitializeAsync<Context20277>();
        using var context = contextFactory.CreateContext();
        var query = context.Entities.AsNoTracking().Select(
            e => new
            {
                e.Id,
                FirstChild = e.Children
                    .Where(c => c.Type == 1)
                    .AsQueryable()
                    .Select(_project)
                    .FirstOrDefault(),
                SecondChild = e.Children
                    .Where(c => c.Type == 2)
                    .AsQueryable()
                    .Select(_project)
                    .FirstOrDefault(),
            });

        var result = async
            ? await query.ToListAsync()
            : query.ToList();
    }

    private static readonly Expression<Func<Child20277, object>> _project = x => new
    {
        x.Id,
        x.Owned, // Comment this line for success
        x.Type,
    };

    protected class Context20277 : DbContext
    {
        public Context20277(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Entity20277> Entities
            => Set<Entity20277>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Entity20277>(
                cfg =>
                {
                    cfg.OwnsMany(
                        e => e.Children, inner =>
                        {
                            inner.OwnsOne(e => e.Owned);
                        });
                });
        }
    }

    protected class Entity20277
    {
        public int Id { get; set; }
        public List<Child20277> Children { get; set; }
    }

    protected class Child20277
    {
        public int Id { get; set; }
        public int Type { get; set; }
        public Owned20277 Owned { get; set; }
    }

    protected class Owned20277
    {
        public bool IsDeleted { get; set; }
        public string Value { get; set; }
    }

    #endregion

    #region 21540

    [ConditionalFact]
    public virtual async Task Can_auto_include_navigation_from_model()
    {
        var contextFactory = await InitializeAsync<Context21540>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Parents.AsNoTracking().ToList();
            var result = Assert.Single(query);
            Assert.NotNull(result.OwnedReference);
            Assert.NotNull(result.Reference);
            Assert.NotNull(result.Collection);
            Assert.Equal(2, result.Collection.Count);
            Assert.NotNull(result.SkipOtherSide);
            Assert.Single(result.SkipOtherSide);
        }

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Parents.AsNoTracking().IgnoreAutoIncludes().ToList();
            var result = Assert.Single(query);
            Assert.NotNull(result.OwnedReference);
            Assert.Null(result.Reference);
            Assert.Null(result.Collection);
            Assert.Null(result.SkipOtherSide);
        }
    }

    protected class Context21540 : DbContext
    {
        public DbSet<Parent21540> Parents { get; set; }

        public Context21540(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Parent21540>().HasMany(e => e.SkipOtherSide).WithMany(e => e.SkipParent)
                .UsingEntity<JoinEntity21540>(
                    e => e.HasOne(i => i.OtherSide).WithMany().HasForeignKey(e => e.OtherSideId),
                    e => e.HasOne(i => i.Parent).WithMany().HasForeignKey(e => e.ParentId))
                .HasKey(e => new { e.ParentId, e.OtherSideId });
            modelBuilder.Entity<Parent21540>().OwnsOne(e => e.OwnedReference);

            modelBuilder.Entity<Parent21540>().Navigation(e => e.Reference).AutoInclude();
            modelBuilder.Entity<Parent21540>().Navigation(e => e.Collection).AutoInclude();
            modelBuilder.Entity<Parent21540>().Navigation(e => e.SkipOtherSide).AutoInclude();
        }

        public void Seed()
        {
            var joinEntity = new JoinEntity21540
            {
                OtherSide = new OtherSide21540(),
                Parent = new Parent21540
                {
                    Reference = new Reference21540(),
                    OwnedReference = new Owned21540(),
                    Collection = new List<Collection21540>
                    {
                        new(), new(),
                    }
                }
            };

            AddRange(joinEntity);

            SaveChanges();
        }

        public class Parent21540
        {
            public int Id { get; set; }
            public Reference21540 Reference { get; set; }
            public Owned21540 OwnedReference { get; set; }
            public List<Collection21540> Collection { get; set; }
            public List<OtherSide21540> SkipOtherSide { get; set; }
        }

        public class JoinEntity21540
        {
            public int ParentId { get; set; }
            public Parent21540 Parent { get; set; }
            public int OtherSideId { get; set; }
            public OtherSide21540 OtherSide { get; set; }
        }

        public class OtherSide21540
        {
            public int Id { get; set; }
            public List<Parent21540> SkipParent { get; set; }
        }

        public class Reference21540
        {
            public int Id { get; set; }
            public int ParentId { get; set; }
            public Parent21540 Parent { get; set; }
        }

        public class Owned21540
        {
            public int Id { get; set; }
        }

        public class Collection21540
        {
            public int Id { get; set; }
            public int ParentId { get; set; }
            public Parent21540 Parent { get; set; }
        }
    }

    #endregion

    #region 21807

    [ConditionalFact]
    public virtual async Task Nested_owned_required_dependents_are_materialized()
    {
        var contextFactory = await InitializeAsync<Context21807>(seed: c => c.Seed());
        using var context = contextFactory.CreateContext();
        var query = context.Set<Context21807.Entity21807>().ToList();
        var result = Assert.Single(query);
        Assert.NotNull(result.Contact);
        Assert.NotNull(result.Contact.Address);
        Assert.Equal(12345, result.Contact.Address.Zip);
    }

    protected class Context21807 : DbContext
    {
        public Context21807(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Entity21807>(
                builder =>
                {
                    builder.HasKey(x => x.Id);

                    builder.OwnsOne(
                        x => x.Contact, contact =>
                        {
                            contact.OwnsOne(c => c.Address);
                        });

                    builder.Navigation(x => x.Contact).IsRequired();
                });

        public void Seed()
        {
            Add(new Entity21807 { Id = "1", Contact = new Contact21807 { Address = new Address21807 { Zip = 12345 } } });

            SaveChanges();
        }

        public class Entity21807
        {
            public string Id { get; set; }
            public Contact21807 Contact { get; set; }
        }

        public class Contact21807
        {
            public string Name { get; set; }
            public Address21807 Address { get; set; }
        }

        public class Address21807
        {
            public string Street { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public int Zip { get; set; }
        }
    }

    #endregion

    #region 22090

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task OwnsMany_correlated_projection(bool async)
    {
        var contextFactory = await InitializeAsync<Context22089>();
        using var context = contextFactory.CreateContext();
        var results = await context.Contacts.Select(
                contact => new ContactDto22089 { Id = contact.Id, Names = contact.Names.Select(name => new NameDto22089()).ToArray() })
            .ToListAsync();
    }

    protected class Contact22089
    {
        public Guid Id { get; set; }
        public IReadOnlyList<Name22809> Names { get; protected set; } = new List<Name22809>();
    }

    protected class ContactDto22089
    {
        public Guid Id { get; set; }
        public IReadOnlyList<NameDto22089> Names { get; set; }
    }

    protected class Name22809
    {
        public Guid Id { get; set; }
        public Guid ContactId { get; set; }
    }

    protected class NameDto22089
    {
        public Guid Id { get; set; }
        public Guid ContactId { get; set; }
    }

    protected class Context22089 : DbContext
    {
        public Context22089(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Contact22089> Contacts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Contact22089>().HasKey(c => c.Id);
            modelBuilder.Entity<Contact22089>().OwnsMany(c => c.Names, names => names.WithOwner().HasForeignKey(n => n.ContactId));
        }
    }

    #endregion

    #region 24133

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Projecting_owned_collection_and_aggregate(bool async)
    {
        var contextFactory = await InitializeAsync<Context24133>();
        using var context = contextFactory.CreateContext();
        var query = context.Set<Blog24133>()
            .Select(
                b => new BlogDto24133
                {
                    Id = b.Id,
                    TotalComments = b.Posts.Sum(p => p.CommentsCount),
                    Posts = b.Posts.Select(p => new PostDto24133 { Title = p.Title, CommentsCount = p.CommentsCount })
                });

        var result = async
            ? await query.ToListAsync()
            : query.ToList();
    }

    protected class Context24133 : DbContext
    {
        public Context24133(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Blog24133>(
                blog =>
                {
                    blog.OwnsMany(
                        b => b.Posts, p =>
                        {
                            p.WithOwner().HasForeignKey("BlogId");
                            p.Property("BlogId").HasMaxLength(40);
                        });
                });
    }

    protected class Blog24133
    {
        public int Id { get; private set; }

        private List<Post24133> _posts = new();

        public static Blog24133 Create(IEnumerable<Post24133> posts)
            => new() { _posts = posts.ToList() };

        public IReadOnlyCollection<Post24133> Posts
            => new ReadOnlyCollection<Post24133>(_posts);
    }

    protected class Post24133
    {
        public string Title { get; set; }
        public int CommentsCount { get; set; }
    }

    protected class BlogDto24133
    {
        public int Id { get; set; }
        public int TotalComments { get; set; }
        public IEnumerable<PostDto24133> Posts { get; set; }
    }

    protected class PostDto24133
    {
        public string Title { get; set; }
        public int CommentsCount { get; set; }
    }

    #endregion

    protected virtual async Task Owned_references_on_same_level_expanded_at_different_times_around_take_helper(
        MyContext26592Base context,
        bool async)
    {
        var query = context.Companies.Where(e => e.CustomerData != null).OrderBy(e => e.Id).Take(10);
        var result = async
            ? await query.ToListAsync()
            : query.ToList();

        var company = Assert.Single(result);
        Assert.Equal("Acme Inc.", company.Name);
        Assert.Equal("Regular", company.CustomerData.AdditionalCustomerData);
        Assert.Equal("Free shipping", company.SupplierData.AdditionalSupplierData);
    }

    protected virtual async Task Owned_references_on_same_level_nested_expanded_at_different_times_around_take_helper(
        MyContext26592Base context,
        bool async)
    {
        var query = context.Owners.Where(e => e.OwnedEntity.CustomerData != null).OrderBy(e => e.Id).Take(10);
        var result = async
            ? await query.ToListAsync()
            : query.ToList();

        var owner = Assert.Single(result);
        Assert.Equal("Owner1", owner.Name);
        Assert.Equal("Intermediate1", owner.OwnedEntity.Name);
        Assert.Equal("IM Regular", owner.OwnedEntity.CustomerData.AdditionalCustomerData);
        Assert.Equal("IM Free shipping", owner.OwnedEntity.SupplierData.AdditionalSupplierData);
    }

    protected abstract class MyContext26592Base : DbContext
    {
        protected MyContext26592Base(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Owner> Owners { get; set; }

        public void Seed()
        {
            Add(
                new Company
                {
                    Name = "Acme Inc.",
                    CustomerData = new CustomerData { AdditionalCustomerData = "Regular" },
                    SupplierData = new SupplierData { AdditionalSupplierData = "Free shipping" }
                });

            Add(
                new Owner
                {
                    Name = "Owner1",
                    OwnedEntity = new IntermediateOwnedEntity
                    {
                        Name = "Intermediate1",
                        CustomerData = new CustomerData { AdditionalCustomerData = "IM Regular" },
                        SupplierData = new SupplierData { AdditionalSupplierData = "IM Free shipping" }
                    }
                });

            SaveChanges();
        }
    }

    protected class Company
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public CustomerData CustomerData { get; set; }
        public SupplierData SupplierData { get; set; }
    }

    [Owned]
    protected class CustomerData
    {
        public int Id { get; set; }
        public string AdditionalCustomerData { get; set; }
    }

    [Owned]
    protected class SupplierData
    {
        public int Id { get; set; }
        public string AdditionalSupplierData { get; set; }
    }

    protected class Owner
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public IntermediateOwnedEntity OwnedEntity { get; set; }
    }

    [Owned]
    protected class IntermediateOwnedEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public CustomerData CustomerData { get; set; }
        public SupplierData SupplierData { get; set; }
    }
}
