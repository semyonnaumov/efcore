// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.EntityFrameworkCore;

public abstract class SimpleQueryTestBase : NonSharedModelTestBase
{
    protected override string StoreName
        => "SimpleQueryTests";

    #region 603

    [ConditionalFact]
    public virtual async Task First_FirstOrDefault_ix_async()
    {
        var contextFactory = await InitializeAsync<Context603>();
        using (var context = contextFactory.CreateContext())
        {
            var product = await context.Products.OrderBy(p => p.Id).FirstAsync();
            context.Products.Remove(product);
            await context.SaveChangesAsync();
        }

        using (var context = contextFactory.CreateContext())
        {
            context.Products.Add(new Context603.Product603 { Name = "Product 1" });
            context.SaveChanges();
        }

        using (var context = contextFactory.CreateContext())
        {
            var product = await context.Products.OrderBy(p => p.Id).FirstOrDefaultAsync();
            context.Products.Remove(product);
            await context.SaveChangesAsync();
        }
    }

    protected class Context603 : DbContext
    {
        public Context603(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Product603> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Product603>()
                .HasData(new Product603 { Id = 1, Name = "Product 1" });

        public class Product603
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }
    }

    #endregion

    #region 3409

    [ConditionalFact]
    public virtual async Task ThenInclude_with_interface_navigations()
    {
        var contextFactory = await InitializeAsync<Context3409>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var results = context.Parents
                .Include(p => p.ChildCollection)
                .ThenInclude(c => c.SelfReferenceCollection)
                .ToList();

            Assert.Single(results);
            Assert.Equal(1, results[0].ChildCollection.Count);
            Assert.Equal(2, results[0].ChildCollection.Single().SelfReferenceCollection.Count);
        }

        using (var context = contextFactory.CreateContext())
        {
            var results = context.Children
                .Select(
                    c => new { c.SelfReferenceBackNavigation, c.SelfReferenceBackNavigation.ParentBackNavigation })
                .ToList();

            Assert.Equal(3, results.Count);
            Assert.Equal(2, results.Count(c => c.SelfReferenceBackNavigation != null));
            Assert.Equal(2, results.Count(c => c.ParentBackNavigation != null));
        }

        using (var context = contextFactory.CreateContext())
        {
            var results = context.Children
                .Select(
                    c => new
                    {
                        SelfReferenceBackNavigation
                            = EF.Property<Context3409.IChild3409>(c, "SelfReferenceBackNavigation"),
                        ParentBackNavigationB
                            = EF.Property<Context3409.IParent3409>(
                                EF.Property<Context3409.IChild3409>(c, "SelfReferenceBackNavigation"),
                                "ParentBackNavigation")
                    })
                .ToList();

            Assert.Equal(3, results.Count);
            Assert.Equal(2, results.Count(c => c.SelfReferenceBackNavigation != null));
            Assert.Equal(2, results.Count(c => c.ParentBackNavigationB != null));
        }

        using (var context = contextFactory.CreateContext())
        {
            var results = context.Children
                .Include(c => c.SelfReferenceBackNavigation)
                .ThenInclude(c => c.ParentBackNavigation)
                .ToList();

            Assert.Equal(3, results.Count);
            Assert.Equal(2, results.Count(c => c.SelfReferenceBackNavigation != null));
            Assert.Equal(1, results.Count(c => c.ParentBackNavigation != null));
        }
    }

    private class Context3409 : DbContext
    {
        public DbSet<Parent3409> Parents { get; set; }
        public DbSet<Child3409> Children { get; set; }

        public Context3409(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Parent3409>()
                .HasMany(p => (ICollection<Child3409>)p.ChildCollection)
                .WithOne(c => (Parent3409)c.ParentBackNavigation);

            modelBuilder.Entity<Child3409>()
                .HasMany(c => (ICollection<Child3409>)c.SelfReferenceCollection)
                .WithOne(c => (Child3409)c.SelfReferenceBackNavigation);
        }

        public void Seed()
        {
            var parent1 = new Parent3409();

            var child1 = new Child3409();
            var child2 = new Child3409();
            var child3 = new Child3409();

            parent1.ChildCollection = new List<IChild3409> { child1 };
            child1.SelfReferenceCollection = new List<IChild3409> { child2, child3 };

            Parents.AddRange(parent1);
            Children.AddRange(child1, child2, child3);

            SaveChanges();
        }

        public interface IParent3409
        {
            int Id { get; set; }

            ICollection<IChild3409> ChildCollection { get; set; }
        }

        public interface IChild3409
        {
            int Id { get; set; }

            int? ParentBackNavigationId { get; set; }
            IParent3409 ParentBackNavigation { get; set; }

            ICollection<IChild3409> SelfReferenceCollection { get; set; }
            int? SelfReferenceBackNavigationId { get; set; }
            IChild3409 SelfReferenceBackNavigation { get; set; }
        }

        public class Parent3409 : IParent3409
        {
            public int Id { get; set; }

            public ICollection<IChild3409> ChildCollection { get; set; }
        }

        public class Child3409 : IChild3409
        {
            public int Id { get; set; }

            public int? ParentBackNavigationId { get; set; }
            public IParent3409 ParentBackNavigation { get; set; }

            public ICollection<IChild3409> SelfReferenceCollection { get; set; }
            public int? SelfReferenceBackNavigationId { get; set; }
            public IChild3409 SelfReferenceBackNavigation { get; set; }
        }
    }

    #endregion

    #region 3758

    [ConditionalFact]
    public async Task Customer_collections_materialize_properly()
    {
        var contextFactory = await InitializeAsync<Context3758>(seed: c => c.Seed());

        using var ctx = contextFactory.CreateContext();

        var query1 = ctx.Customers.Select(c => c.Orders1);
        var result1 = query1.ToList();

        Assert.Equal(2, result1.Count);
        Assert.IsType<HashSet<Context3758.Order3758>>(result1[0]);
        Assert.Equal(2, result1[0].Count);
        Assert.Equal(2, result1[1].Count);

        var query2 = ctx.Customers.Select(c => c.Orders2);
        var result2 = query2.ToList();

        Assert.Equal(2, result2.Count);
        Assert.IsType<Context3758.MyGenericCollection3758<Context3758.Order3758>>(result2[0]);
        Assert.Equal(2, result2[0].Count);
        Assert.Equal(2, result2[1].Count);

        var query3 = ctx.Customers.Select(c => c.Orders3);
        var result3 = query3.ToList();

        Assert.Equal(2, result3.Count);
        Assert.IsType<Context3758.MyNonGenericCollection3758>(result3[0]);
        Assert.Equal(2, result3[0].Count);
        Assert.Equal(2, result3[1].Count);

        var query4 = ctx.Customers.Select(c => c.Orders4);

        Assert.Equal(
            CoreStrings.NavigationCannotCreateType(
                "Orders4", typeof(Context3758.Customer3758).Name,
                typeof(Context3758.MyInvalidCollection3758<Context3758.Order3758>).ShortDisplayName()),
            Assert.Throws<InvalidOperationException>(() => query4.ToList()).Message);
    }

    protected class Context3758 : DbContext
    {
        public Context3758(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Customer3758> Customers { get; set; }
        public DbSet<Order3758> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer3758>(
                b =>
                {
                    //b.ToTable("Customer3758");

                    b.HasMany(e => e.Orders1).WithOne().HasForeignKey("CustomerId1");
                    b.HasMany(e => e.Orders2).WithOne().HasForeignKey("CustomerId2");
                    b.HasMany(e => e.Orders3).WithOne().HasForeignKey("CustomerId3");
                    b.HasMany(e => e.Orders4).WithOne().HasForeignKey("CustomerId4");
                });

            //modelBuilder.Entity<Order3758>().ToTable("Order3758");
        }

        public void Seed()
        {
            var o111 = new Order3758 { Name = "O111" };
            var o112 = new Order3758 { Name = "O112" };
            var o121 = new Order3758 { Name = "O121" };
            var o122 = new Order3758 { Name = "O122" };
            var o131 = new Order3758 { Name = "O131" };
            var o132 = new Order3758 { Name = "O132" };
            var o141 = new Order3758 { Name = "O141" };

            var o211 = new Order3758 { Name = "O211" };
            var o212 = new Order3758 { Name = "O212" };
            var o221 = new Order3758 { Name = "O221" };
            var o222 = new Order3758 { Name = "O222" };
            var o231 = new Order3758 { Name = "O231" };
            var o232 = new Order3758 { Name = "O232" };
            var o241 = new Order3758 { Name = "O241" };

            var c1 = new Customer3758
            {
                Name = "C1",
                Orders1 = new List<Order3758> { o111, o112 },
                Orders2 = new MyGenericCollection3758<Order3758>(),
                Orders3 = new MyNonGenericCollection3758(),
                Orders4 = new MyInvalidCollection3758<Order3758>(42)
            };

            c1.Orders2.AddRange(new[] { o121, o122 });
            c1.Orders3.AddRange(new[] { o131, o132 });
            c1.Orders4.Add(o141);

            var c2 = new Customer3758
            {
                Name = "C2",
                Orders1 = new List<Order3758> { o211, o212 },
                Orders2 = new MyGenericCollection3758<Order3758>(),
                Orders3 = new MyNonGenericCollection3758(),
                Orders4 = new MyInvalidCollection3758<Order3758>(42)
            };

            c2.Orders2.AddRange(new[] { o221, o222 });
            c2.Orders3.AddRange(new[] { o231, o232 });
            c2.Orders4.Add(o241);

            Customers.AddRange(c1, c2);
            Orders.AddRange(
                o111, o112, o121, o122,
                o131, o132, o141, o211,
                o212, o221, o222, o231,
                o232, o241);

            SaveChanges();
        }

        public class Customer3758
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public ICollection<Order3758> Orders1 { get; set; }
            public MyGenericCollection3758<Order3758> Orders2 { get; set; }
            public MyNonGenericCollection3758 Orders3 { get; set; }
            public MyInvalidCollection3758<Order3758> Orders4 { get; set; }
        }

        public class Order3758
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class MyGenericCollection3758<TElement> : List<TElement>
        {
        }

        public class MyNonGenericCollection3758 : List<Order3758>
        {
        }

        public class MyInvalidCollection3758<TElement> : List<TElement>
        {
            public MyInvalidCollection3758(int argument)
            {
                var _ = argument;
            }
        }
    }

    #endregion

    #region 6901

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Left_join_with_missing_key_values_on_both_sides(bool async)
    {
        var contextFactory = await InitializeAsync<Context6901>();
        using var context = contextFactory.CreateContext();

        var customers
            = from customer in context.Customers
              join postcode in context.Postcodes
                  on customer.PostcodeID equals postcode.PostcodeID into custPCTmp
              from custPC in custPCTmp.DefaultIfEmpty()
              select new
              {
                  customer.CustomerID,
                  customer.CustomerName,
                  TownName = custPC == null ? string.Empty : custPC.TownName,
                  PostcodeValue = custPC == null ? string.Empty : custPC.PostcodeValue
              };

        var results = customers.ToList();

        Assert.Equal(5, results.Count);
        Assert.True(results[3].CustomerName != results[4].CustomerName);
    }

    public class Context6901 : DbContext
    {
        public Context6901(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer6901>(
                c =>
                {
                    c.HasKey(x => x.CustomerID);
                    c.Property(c => c.CustomerID).ValueGeneratedNever();
                    c.Property(c => c.CustomerName).HasMaxLength(120).IsUnicode(false);
                    c.HasData(
                        new Customer6901
                        {
                            CustomerID = 1,
                            CustomerName = "Sam Tippet",
                            PostcodeID = 5
                        },
                        new Customer6901
                        {
                            CustomerID = 2,
                            CustomerName = "William Greig",
                            PostcodeID = 2
                        },
                        new Customer6901
                        {
                            CustomerID = 3,
                            CustomerName = "Steve Jones",
                            PostcodeID = 3
                        },
                        new Customer6901 { CustomerID = 4, CustomerName = "Jim Warren" },
                        new Customer6901
                        {
                            CustomerID = 5,
                            CustomerName = "Andrew Smith",
                            PostcodeID = 5
                        });
                });

            modelBuilder.Entity<Postcode6901>(
                p =>
                {
                    p.HasKey(x => x.PostcodeID);
                    p.Property(c => c.PostcodeID).ValueGeneratedNever();
                    p.Property(c => c.PostcodeValue).HasMaxLength(100).IsUnicode(false);
                    p.Property(c => c.TownName).HasMaxLength(255).IsUnicode(false);
                    p.HasData(
                        new Postcode6901
                        {
                            PostcodeID = 2,
                            PostcodeValue = "1000",
                            TownName = "Town 1"
                        },
                        new Postcode6901
                        {
                            PostcodeID = 3,
                            PostcodeValue = "2000",
                            TownName = "Town 2"
                        },
                        new Postcode6901
                        {
                            PostcodeID = 4,
                            PostcodeValue = "3000",
                            TownName = "Town 3"
                        },
                        new Postcode6901
                        {
                            PostcodeID = 5,
                            PostcodeValue = "4000",
                            TownName = "Town 4"
                        });
                });
        }

        public DbSet<Customer6901> Customers { get; set; }
        public DbSet<Postcode6901> Postcodes { get; set; }
    }

    public class Customer6901
    {
        public int CustomerID { get; set; }
        public string CustomerName { get; set; }
        public int? PostcodeID { get; set; }
    }

    public class Postcode6901
    {
        public int PostcodeID { get; set; }
        public string PostcodeValue { get; set; }
        public string TownName { get; set; }
    }

    #endregion

    #region 6986

    [ConditionalFact]
    public virtual async Task Shadow_property_with_inheritance()
    {
        var contextFactory = await InitializeAsync<Context6986>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            // can_query_base_type_when_derived_types_contain_shadow_properties
            var query = context.Contacts.ToList();

            Assert.Equal(4, query.Count);
            Assert.Equal(2, query.OfType<Context6986.EmployerContact6986>().Count());
            Assert.Single(query.OfType<Context6986.ServiceOperatorContact6986>());
        }

        using (var context = contextFactory.CreateContext())
        {
            // can_include_dependent_to_principal_navigation_of_derived_type_with_shadow_fk
            var query = context.Contacts.OfType<Context6986.ServiceOperatorContact6986>().Include(e => e.ServiceOperator6986)
                .ToList();

            Assert.Single(query);
            Assert.NotNull(query[0].ServiceOperator6986);
        }

        using (var context = contextFactory.CreateContext())
        {
            // can_project_shadow_property_using_ef_property
            var query = context.Contacts.OfType<Context6986.ServiceOperatorContact6986>().Select(
                c => new { c, Prop = EF.Property<int>(c, "ServiceOperator6986Id") }).ToList();

            Assert.Single(query);
            Assert.Equal(1, query[0].Prop);
        }
    }

    private class Context6986 : DbContext
    {
        public Context6986(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Contact6986> Contacts { get; set; }
        public DbSet<EmployerContact6986> EmployerContacts { get; set; }
        public DbSet<Employer6986> Employers { get; set; }
        public DbSet<ServiceOperatorContact6986> ServiceOperatorContacts { get; set; }
        public DbSet<ServiceOperator6986> ServiceOperators { get; set; }

        public void Seed()
        {
            ServiceOperators.Add(new ServiceOperator6986());
            Employers.AddRange(
                new Employer6986 { Name = "UWE" },
                new Employer6986 { Name = "Hewlett Packard" });

            SaveChanges();

            Contacts.AddRange(
                new ServiceOperatorContact6986
                {
                    UserName = "service.operator@esoterix.co.uk",
                    ServiceOperator6986 = ServiceOperators.OrderBy(o => o.Id).First()
                },
                new EmployerContact6986
                {
                    UserName = "uwe@esoterix.co.uk",
                    Employer6986 = Employers.OrderBy(e => e.Id).First(e => e.Name == "UWE")
                },
                new EmployerContact6986
                {
                    UserName = "hp@esoterix.co.uk",
                    Employer6986 = Employers.OrderBy(e => e.Id).First(e => e.Name == "Hewlett Packard")
                },
                new Contact6986 { UserName = "noroles@esoterix.co.uk" });
            SaveChanges();
        }

        public class EmployerContact6986 : Contact6986
        {
            [Required]
            public Employer6986 Employer6986 { get; set; }
        }

        public class ServiceOperatorContact6986 : Contact6986
        {
            [Required]
            public ServiceOperator6986 ServiceOperator6986 { get; set; }
        }

        public class Contact6986
        {
            public int Id { get; set; }
            public string UserName { get; set; }
            public bool IsPrimary { get; set; }
        }

        public class Employer6986
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public List<EmployerContact6986> Contacts { get; set; }
        }

        public class ServiceOperator6986
        {
            public int Id { get; set; }
            public List<ServiceOperatorContact6986> Contacts { get; set; }
        }
    }

    #endregion

    #region 7312

    [ConditionalFact]
    public virtual async Task Reference_include_on_derived_type_with_sibling_works()
    {
        var contextFactory = await InitializeAsync<Context7312>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Proposal.OfType<Context7312.ProposalLeave7312>().Include(l => l.LeaveType).ToList();

            Assert.Single(query);
        }
    }

    private class Context7312 : DbContext
    {
        public Context7312(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Proposal7312> Proposal { get; set; }
        public DbSet<ProposalCustom7312> ProposalCustoms { get; set; }
        public DbSet<ProposalLeave7312> ProposalLeaves { get; set; }

        public void Seed()
        {
            AddRange(
                new Proposal7312(),
                new ProposalCustom7312 { Name = "CustomProposal" },
                new ProposalLeave7312 { LeaveStart = DateTime.Now, LeaveType = new ProposalLeaveType7312() }
            );
            SaveChanges();
        }

        public class Proposal7312
        {
            public int Id { get; set; }
        }

        public class ProposalCustom7312 : Proposal7312
        {
            public string Name { get; set; }
        }

        public class ProposalLeave7312 : Proposal7312
        {
            public DateTime LeaveStart { get; set; }
            public virtual ProposalLeaveType7312 LeaveType { get; set; }
        }

        public class ProposalLeaveType7312
        {
            public int Id { get; set; }
            public ICollection<ProposalLeave7312> ProposalLeaves { get; set; }
        }
    }

    #endregion

    #region 7359

    [ConditionalFact]
    public virtual async Task Discriminator_type_is_handled_correctly()
    {
        var contextFactory = await InitializeAsync<Context7359>(seed: c => c.Seed());

        using (var ctx = contextFactory.CreateContext())
        {
            var query = ctx.Products.OfType<Context7359.SpecialProduct7359>().ToList();

            Assert.Single(query);
        }

        using (var ctx = contextFactory.CreateContext())
        {
            var query = ctx.Products.Where(p => p is Context7359.SpecialProduct7359).ToList();

            Assert.Single(query);
        }
    }

    protected class Context7359 : DbContext
    {
        public Context7359(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Product7359> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SpecialProduct7359>();
            modelBuilder.Entity<Product7359>()
                .HasDiscriminator<int?>("Discriminator")
                .HasValue(0)
                .HasValue<SpecialProduct7359>(1);
        }

        public void Seed()
        {
            Add(new Product7359 { Name = "Product1" });
            Add(new SpecialProduct7359 { Name = "SpecialProduct" });
            SaveChanges();
        }

        public class Product7359
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        public class SpecialProduct7359 : Product7359
        {
        }
    }

    #endregion

    #region 8909

    [ConditionalFact]
    public virtual async Task Variable_from_closure_is_parametrized()
    {
        var contextFactory = await InitializeAsync<Context8909>();
        using (var context = contextFactory.CreateContext())
        {
            context.Cache.Compact(1);

            var id = 1;
            context.Entities.Where(c => c.Id == id).ToList();
            Assert.Equal(2, context.Cache.Count);

            id = 2;
            context.Entities.Where(c => c.Id == id).ToList();
            Assert.Equal(2, context.Cache.Count);
        }

        using (var context = contextFactory.CreateContext())
        {
            context.Cache.Compact(1);

            var id = 0;
            // ReSharper disable once AccessToModifiedClosure
            Expression<Func<Context8909.Entity8909, bool>> whereExpression = c => c.Id == id;

            id = 1;
            context.Entities.Where(whereExpression).ToList();
            Assert.Equal(2, context.Cache.Count);

            id = 2;
            context.Entities.Where(whereExpression).ToList();
            Assert.Equal(2, context.Cache.Count);
        }

        using (var context = contextFactory.CreateContext())
        {
            context.Cache.Compact(1);

            var id = 0;
            // ReSharper disable once AccessToModifiedClosure
            Expression<Func<Context8909.Entity8909, bool>> whereExpression = c => c.Id == id;
            Expression<Func<Context8909.Entity8909, bool>> containsExpression =
                c => context.Entities.Where(whereExpression).Select(e => e.Id).Contains(c.Id);

            id = 1;
            context.Entities.Where(containsExpression).ToList();
            Assert.Equal(2, context.Cache.Count);

            id = 2;
            context.Entities.Where(containsExpression).ToList();
            Assert.Equal(2, context.Cache.Count);
        }
    }

    [ConditionalFact]
    public virtual async Task Relational_command_cache_creates_new_entry_when_parameter_nullability_changes()
    {
        var contextFactory = await InitializeAsync<Context8909>();
        using var context = contextFactory.CreateContext();
        context.Cache.Compact(1);

        var name = "A";

        context.Entities.Where(e => e.Name == name).ToList();
        Assert.Equal(2, context.Cache.Count);

        name = null;
        context.Entities.Where(e => e.Name == name).ToList();
        Assert.Equal(3, context.Cache.Count);
    }

    [ConditionalFact]
    public virtual async Task Query_cache_entries_are_evicted_as_necessary()
    {
        var contextFactory = await InitializeAsync<Context8909>();
        using var context = contextFactory.CreateContext();
        context.Cache.Compact(1);
        Assert.Equal(0, context.Cache.Count);

        var entityParam = Expression.Parameter(typeof(Context8909.Entity8909), "e");
        var idPropertyInfo = context.Model.FindEntityType((typeof(Context8909.Entity8909)))
            .FindProperty(nameof(Context8909.Entity8909.Id))
            .PropertyInfo;
        for (var i = 0; i < 1100; i++)
        {
            var conditionBody = Expression.Equal(
                Expression.MakeMemberAccess(entityParam, idPropertyInfo),
                Expression.Constant(i));
            var whereExpression = Expression.Lambda<Func<Context8909.Entity8909, bool>>(conditionBody, entityParam);
            context.Entities.Where(whereExpression).GetEnumerator();
        }

        Assert.True(context.Cache.Count <= 1024);
    }

    [ConditionalFact]
    public virtual async Task Explicitly_compiled_query_does_not_add_cache_entry()
    {
        var parameter = Expression.Parameter(typeof(Context8909.Entity8909));
        var predicate = Expression.Lambda<Func<Context8909.Entity8909, bool>>(
            Expression.MakeBinary(
                ExpressionType.Equal,
                Expression.PropertyOrField(parameter, "Id"),
                Expression.Constant(1)),
            parameter);
        var query = EF.CompileQuery((Context8909 context) => context.Set<Context8909.Entity8909>().SingleOrDefault(predicate));

        var contextFactory = await InitializeAsync<Context8909>();

        using (var context = contextFactory.CreateContext())
        {
            context.Cache.Compact(1);
            Assert.Equal(0, context.Cache.Count);

            query(context);

            // 1 entry for RelationalCommandCache
            Assert.Equal(1, context.Cache.Count);
        }
    }

    protected class Context8909 : DbContext
    {
        public Context8909(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Entity8909> Entities { get; set; }

        public MemoryCache Cache
        {
            get
            {
                var compiledQueryCache = this.GetService<ICompiledQueryCache>();

                return (MemoryCache)typeof(CompiledQueryCache)
                    .GetField("_memoryCache", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(compiledQueryCache);
            }
        }

        public class Entity8909
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }

    #endregion

    #region 9038

    [ConditionalFact]
    public virtual async Task Include_collection_optional_reference_collection()
    {
        var contextFactory = await InitializeAsync<Context9038>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var result = await context.People.OfType<Context9038.PersonTeacher9038>()
                .Include(m => m.Students)
                .ThenInclude(m => m.Family)
                .ThenInclude(m => m.Members)
                .ToListAsync();

            Assert.Equal(2, result.Count);
            Assert.True(result.All(r => r.Students.Count > 0));
        }

        using (var context = contextFactory.CreateContext())
        {
            var result = await context.Set<Context9038.PersonTeacher9038>()
                .Include(m => m.Family.Members)
                .Include(m => m.Students)
                .ToListAsync();

            Assert.Equal(2, result.Count);
            Assert.True(result.All(r => r.Students.Count > 0));
            Assert.Null(result.Single(t => t.Name == "Ms. Frizzle").Family);
            Assert.NotNull(result.Single(t => t.Name == "Mr. Garrison").Family);
        }
    }

    protected class Context9038 : DbContext
    {
        public Context9038(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Person9038> People { get; set; }

        public DbSet<PersonFamily9038> Families { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PersonTeacher9038>().HasBaseType<Person9038>();
            modelBuilder.Entity<PersonKid9038>().HasBaseType<Person9038>();
            modelBuilder.Entity<PersonFamily9038>();

            modelBuilder.Entity<PersonKid9038>(
                entity =>
                {
                    entity.Property("Discriminator").HasMaxLength(63);
                    entity.HasIndex("Discriminator");
                    entity.HasOne(m => m.Teacher)
                        .WithMany(m => m.Students)
                        .HasForeignKey(m => m.TeacherId)
                        .HasPrincipalKey(m => m.Id)
                        .OnDelete(DeleteBehavior.Restrict);
                });
        }

        public void Seed()
        {
            var famalies = new List<PersonFamily9038> { new() { LastName = "Garrison" }, new() { LastName = "Cartman" } };
            var teachers = new List<PersonTeacher9038>
            {
                new() { Name = "Ms. Frizzle" }, new() { Name = "Mr. Garrison", Family = famalies[0] }
            };
            var students = new List<PersonKid9038>
            {
                new()
                {
                    Name = "Arnold",
                    Grade = 2,
                    Teacher = teachers[0]
                },
                new()
                {
                    Name = "Eric",
                    Grade = 4,
                    Teacher = teachers[1],
                    Family = famalies[1]
                }
            };

            People.AddRange(teachers);
            People.AddRange(students);
            SaveChanges();
        }

        public abstract class Person9038
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public int? TeacherId { get; set; }

            public PersonFamily9038 Family { get; set; }
        }

        public class PersonKid9038 : Person9038
        {
            public int Grade { get; set; }

            public PersonTeacher9038 Teacher { get; set; }
        }

        public class PersonTeacher9038 : Person9038
        {
            public ICollection<PersonKid9038> Students { get; set; }
        }

        public class PersonFamily9038
        {
            public int Id { get; set; }

            public string LastName { get; set; }

            public ICollection<Person9038> Members { get; set; }
        }
    }

    #endregion

    #region 9468

    [ConditionalFact]
    public virtual async Task Conditional_expression_with_conditions_does_not_collapse_if_nullable_bool()
    {
        var contextFactory = await InitializeAsync<Context9468>(seed: c => c.Seed());
        using var context = contextFactory.CreateContext();
        var query = context.Carts.Select(
            t => new { Processing = t.Configuration != null ? !t.Configuration.Processed : (bool?)null }).ToList();

        Assert.Single(query.Where(t => t.Processing == null));
        Assert.Single(query.Where(t => t.Processing == true));
        Assert.Single(query.Where(t => t.Processing == false));
    }

    protected class Context9468 : DbContext
    {
        public Context9468(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Cart9468> Carts { get; set; }

        public void Seed()
        {
            AddRange(
                new Cart9468(),
                new Cart9468 { Configuration = new Configuration9468 { Processed = true } },
                new Cart9468 { Configuration = new Configuration9468() }
            );

            SaveChanges();
        }

        public class Cart9468
        {
            public int Id { get; set; }
            public int? ConfigurationId { get; set; }
            public Configuration9468 Configuration { get; set; }
        }

        public class Configuration9468
        {
            public int Id { get; set; }
            public bool Processed { get; set; }
        }
    }

    #endregion

    #region 10635

    [ConditionalFact]
    public virtual async Task Include_with_order_by_on_interface_key()
    {
        var contextFactory = await InitializeAsync<Context10635>(seed: c => c.Seed());
        using (var context = contextFactory.CreateContext())
        {
            var query = context.Parents.Include(p => p.Children).OrderBy(p => p.Id).ToList();
        }

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Parents.OrderBy(p => p.Id).Select(p => p.Children.ToList()).ToList();
        }
    }

    private class Context10635 : DbContext
    {
        public Context10635(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Parent10635> Parents { get; set; }
        public DbSet<Child10635> Children { get; set; }

        public void Seed()
        {
            var c11 = new Child10635 { Name = "Child111" };
            var c12 = new Child10635 { Name = "Child112" };
            var c13 = new Child10635 { Name = "Child113" };
            var c21 = new Child10635 { Name = "Child121" };

            var p1 = new Parent10635 { Name = "Parent1", Children = new[] { c11, c12, c13 } };
            var p2 = new Parent10635 { Name = "Parent2", Children = new[] { c21 } };
            Parents.AddRange(p1, p2);
            Children.AddRange(c11, c12, c13, c21);
            SaveChanges();
        }

        public interface IEntity10635
        {
            int Id { get; set; }
        }

        public class Parent10635 : IEntity10635
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public virtual ICollection<Child10635> Children { get; set; }
        }

        public class Child10635 : IEntity10635
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int ParentId { get; set; }
        }
    }

    #endregion

    #region 11104

    [ConditionalFact]
    public virtual async Task QueryBuffer_requirement_is_computed_when_querying_base_type_while_derived_type_has_shadow_prop()
    {
        var contextFactory = await InitializeAsync<Context11104>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Bases.ToList();

            var derived1 = Assert.Single(query);
            Assert.Equal(typeof(Context11104.Derived11104_1), derived1.GetType());
        }
    }

    protected class Context11104 : DbContext
    {
        public DbSet<Base11104> Bases { get; set; }

        public Context11104(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Base11104>()
                .HasDiscriminator(x => x.IsTwo)
                .HasValue<Derived11104_1>(false)
                .HasValue<Derived11104_2>(true);

        public void Seed()
        {
            AddRange(
                new Derived11104_1 { IsTwo = false }
            );

            SaveChanges();
        }

        public abstract class Base11104
        {
            public int Id { get; set; }
            public bool IsTwo { get; set; }
        }

        public class Derived11104_1 : Base11104
        {
            public Stuff11104 MoreStuff { get; set; }
        }

        public class Derived11104_2 : Base11104
        {
        }

        public class Stuff11104
        {
            public int Id { get; set; }
        }
    }

    #endregion

    #region 11923

    [ConditionalFact]
    public virtual async Task Collection_without_setter_materialized_correctly()
    {
        var contextFactory = await InitializeAsync<Context11923>(seed: c => c.Seed());
        using var context = contextFactory.CreateContext();
        var query1 = context.Blogs
            .Select(
                b => new
                {
                    Collection1 = b.Posts1,
                    Collection2 = b.Posts2,
                    Collection3 = b.Posts3
                }).ToList();

        var query2 = context.Blogs
            .Select(
                b => new
                {
                    Collection1 = b.Posts1.OrderBy(p => p.Id).First().Comments.Count,
                    Collection2 = b.Posts2.OrderBy(p => p.Id).First().Comments.Count,
                    Collection3 = b.Posts3.OrderBy(p => p.Id).First().Comments.Count
                }).ToList();

        Assert.Throws<InvalidOperationException>(
            () => context.Blogs
                .Select(
                    b => new
                    {
                        Collection1 = b.Posts1.OrderBy(p => p.Id),
                        Collection2 = b.Posts2.OrderBy(p => p.Id),
                        Collection3 = b.Posts3.OrderBy(p => p.Id)
                    }).ToList());
    }

    protected class Context11923 : DbContext
    {
        public DbSet<Blog11923> Blogs { get; set; }
        public DbSet<Post11923> Posts { get; set; }
        public DbSet<Comment11923> Comments { get; set; }

        public Context11923(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Blog11923>(
                b =>
                {
                    b.HasMany(e => e.Posts1).WithOne().HasForeignKey("BlogId1");
                    b.HasMany(e => e.Posts2).WithOne().HasForeignKey("BlogId2");
                    b.HasMany(e => e.Posts3).WithOne().HasForeignKey("BlogId3");
                });

            modelBuilder.Entity<Post11923>();
        }

        public void Seed()
        {
            var p111 = new Post11923 { Name = "P111" };
            var p112 = new Post11923 { Name = "P112" };
            var p121 = new Post11923 { Name = "P121" };
            var p122 = new Post11923 { Name = "P122" };
            var p123 = new Post11923 { Name = "P123" };
            var p131 = new Post11923 { Name = "P131" };

            var p211 = new Post11923 { Name = "P211" };
            var p212 = new Post11923 { Name = "P212" };
            var p221 = new Post11923 { Name = "P221" };
            var p222 = new Post11923 { Name = "P222" };
            var p223 = new Post11923 { Name = "P223" };
            var p231 = new Post11923 { Name = "P231" };

            var b1 = new Blog11923 { Name = "B1" };
            var b2 = new Blog11923 { Name = "B2" };

            b1.Posts1.AddRange(new[] { p111, p112 });
            b1.Posts2.AddRange(new[] { p121, p122, p123 });
            b1.Posts3.Add(p131);

            b2.Posts1.AddRange(new[] { p211, p212 });
            b2.Posts2.AddRange(new[] { p221, p222, p223 });
            b2.Posts3.Add(p231);

            Blogs.AddRange(b1, b2);
            Posts.AddRange(p111, p112, p121, p122, p123, p131, p211, p212, p221, p222, p223, p231);
            SaveChanges();
        }

        public class Blog11923
        {
            public Blog11923()
            {
                Posts1 = new List<Post11923>();
                Posts2 = new CustomCollection11923();
                Posts3 = new HashSet<Post11923>();
            }

            public Blog11923(List<Post11923> posts1, CustomCollection11923 posts2, HashSet<Post11923> posts3)
            {
                Posts1 = posts1;
                Posts2 = posts2;
                Posts3 = posts3;
            }

            public int Id { get; set; }
            public string Name { get; set; }

            public List<Post11923> Posts1 { get; }
            public CustomCollection11923 Posts2 { get; }
            public HashSet<Post11923> Posts3 { get; }
        }

        public class Post11923
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public List<Comment11923> Comments { get; set; }
        }

        public class Comment11923
        {
            public int Id { get; set; }
        }

        public class CustomCollection11923 : List<Post11923>
        {
        }
    }

    #endregion

    #region 12582

    [ConditionalFact]
    public virtual async Task Include_collection_with_OfType_base()
    {
        var contextFactory = await InitializeAsync<Context12582>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Employees
                .Include(i => i.Devices)
                .OfType<Context12582.IEmployee12582>()
                .ToList();

            Assert.Single(query);

            var employee = (Context12582.Employee12582)query[0];
            Assert.Equal(2, employee.Devices.Count);
        }

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Employees
                .Select(e => e.Devices.Where(d => d.Device != "foo").Cast<Context12582.IEmployeeDevice12582>())
                .ToList();

            Assert.Single(query);
            var result = query[0];
            Assert.Equal(2, result.Count());
        }
    }

    private class Context12582 : DbContext
    {
        public DbSet<Employee12582> Employees { get; set; }
        public DbSet<EmployeeDevice12582> Devices { get; set; }

        public Context12582(DbContextOptions options)
            : base(options)
        {
        }

        public void Seed()
        {
            var d1 = new EmployeeDevice12582 { Device = "d1" };
            var d2 = new EmployeeDevice12582 { Device = "d2" };
            var e = new Employee12582 { Devices = new List<EmployeeDevice12582> { d1, d2 }, Name = "e" };

            Devices.AddRange(d1, d2);
            Employees.Add(e);
            SaveChanges();
        }

        public interface IEmployee12582
        {
            string Name { get; set; }
        }

        public class Employee12582 : IEmployee12582
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public ICollection<EmployeeDevice12582> Devices { get; set; }
        }

        public interface IEmployeeDevice12582
        {
            string Device { get; set; }
        }

        public class EmployeeDevice12582 : IEmployeeDevice12582
        {
            public int Id { get; set; }
            public int EmployeeId { get; set; }
            public string Device { get; set; }
            public Employee12582 Employee { get; set; }
        }
    }

    #endregion

    #region 12748

    [ConditionalFact]
    public virtual async Task Correlated_collection_correctly_associates_entities_with_byte_array_keys()
    {
        var contextFactory = await InitializeAsync<Context12748>(seed: c => c.Seed());
        using var context = contextFactory.CreateContext();
        var query = from blog in context.Blogs
                    select new
                    {
                        blog.Name,
                        Comments = blog.Comments.Select(
                            u => new { u.Id }).ToArray()
                    };
        var result = query.ToList();
        Assert.Single(result[0].Comments);
    }

    protected class Context12748 : DbContext
    {
        public DbSet<Blog12748> Blogs { get; set; }
        public DbSet<Comment12748> Comments { get; set; }

        public Context12748(DbContextOptions options)
            : base(options)
        {
        }

        public void Seed()
        {
            Blogs.Add(new Blog12748 { Name = Encoding.UTF8.GetBytes("Awesome Blog") });
            Comments.Add(new Comment12748 { BlogName = Encoding.UTF8.GetBytes("Awesome Blog") });
            SaveChanges();
        }

        public class Blog12748
        {
            [Key]
            public byte[] Name { get; set; }

            public List<Comment12748> Comments { get; set; }
        }

        public class Comment12748
        {
            public int Id { get; set; }
            public byte[] BlogName { get; set; }
            public Blog12748 Blog { get; set; }
        }
    }

    #endregion

    #region 21770

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Comparing_enum_casted_to_byte_with_int_parameter(bool async)
    {
        var contextFactory = await InitializeAsync<Context21770>();
        using var context = contextFactory.CreateContext();
        var bitterTaste = Taste.Bitter;
        var query = context.IceCreams.Where(i => i.Taste == (byte)bitterTaste);

        var bitterIceCreams = async
            ? await query.ToListAsync()
            : query.ToList();

        Assert.Single(bitterIceCreams);
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Comparing_enum_casted_to_byte_with_int_constant(bool async)
    {
        var contextFactory = await InitializeAsync<Context21770>();
        using var context = contextFactory.CreateContext();
        var query = context.IceCreams.Where(i => i.Taste == (byte)Taste.Bitter);

        var bitterIceCreams = async
            ? await query.ToListAsync()
            : query.ToList();

        Assert.Single(bitterIceCreams);
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Comparing_byte_column_to_enum_in_vb_creating_double_cast(bool async)
    {
        var contextFactory = await InitializeAsync<Context21770>();
        using var context = contextFactory.CreateContext();
        Expression<Func<Food, byte?>> memberAccess = i => i.Taste;
        var predicate = Expression.Lambda<Func<Food, bool>>(
            Expression.Equal(
                Expression.Convert(memberAccess.Body, typeof(int?)),
                Expression.Convert(
                    Expression.Convert(Expression.Constant(Taste.Bitter, typeof(Taste)), typeof(int)),
                    typeof(int?))),
            memberAccess.Parameters);
        var query = context.Food.Where(predicate);

        var bitterFood = async
            ? await query.ToListAsync()
            : query.ToList();
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Null_check_removal_in_ternary_maintain_appropriate_cast(bool async)
    {
        var contextFactory = await InitializeAsync<Context21770>();
        using var context = contextFactory.CreateContext();

        var query = from f in context.Food
                    select new { Bar = f.Taste != null ? (Taste)f.Taste : (Taste?)null };

        var bitterFood = async
            ? await query.ToListAsync()
            : query.ToList();
    }

    protected class Context21770 : DbContext
    {
        public Context21770(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<IceCream> IceCreams { get; set; }
        public DbSet<Food> Food { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IceCream>(
                entity =>
                {
                    entity.HasData(
                        new IceCream
                        {
                            IceCreamId = 1,
                            Name = "Vanilla",
                            Taste = (byte)Taste.Sweet
                        },
                        new IceCream
                        {
                            IceCreamId = 2,
                            Name = "Chocolate",
                            Taste = (byte)Taste.Sweet
                        },
                        new IceCream
                        {
                            IceCreamId = 3,
                            Name = "Match",
                            Taste = (byte)Taste.Bitter
                        });
                });

            modelBuilder.Entity<Food>(
                entity =>
                {
                    entity.HasData(new Food { Id = 1, Taste = null });
                });
        }
    }

    protected enum Taste : byte
    {
        Sweet = 0,
        Bitter = 1,
    }

    protected class IceCream
    {
        public int IceCreamId { get; set; }
        public string Name { get; set; }
        public int Taste { get; set; }
    }

    protected class Food
    {
        public int Id { get; set; }
        public byte? Taste { get; set; }
    }

    #endregion

    #region 24657

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Bool_discriminator_column_works(bool async)
    {
        var contextFactory = await InitializeAsync<Context24657>(seed: c => c.Seed());
        using var context = contextFactory.CreateContext();

        var query = context.Authors.Include(e => e.Blog);

        var authors = async
            ? await query.ToListAsync()
            : query.ToList();

        Assert.Equal(2, authors.Count);
    }

    protected class Context24657 : DbContext
    {
        public Context24657(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Author> Authors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Blog>()
                .HasDiscriminator<bool>(nameof(Blog.IsPhotoBlog))
                .HasValue<DevBlog>(false)
                .HasValue<PhotoBlog>(true);

        public void Seed()
        {
            Add(new Author { Blog = new DevBlog { Title = "Dev Blog", } });
            Add(new Author { Blog = new PhotoBlog { Title = "Photo Blog", } });

            SaveChanges();
        }
    }

    protected class Author
    {
        public int Id { get; set; }
        public Blog Blog { get; set; }
    }

    protected abstract class Blog
    {
        public int Id { get; set; }
        public bool IsPhotoBlog { get; set; }
        public string Title { get; set; }
    }

    protected class DevBlog : Blog
    {
        public DevBlog()
        {
            IsPhotoBlog = false;
        }
    }

    protected class PhotoBlog : Blog
    {
        public PhotoBlog()
        {
            IsPhotoBlog = true;
        }

        public int NumberOfPhotos { get; set; }
    }

    #endregion

    #region 26433

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Count_member_over_IReadOnlyCollection_works(bool async)
    {
        var contextFactory = await InitializeAsync<Context26433>(seed: c => c.Seed());
        using var context = contextFactory.CreateContext();

        var query = context.Authors
            .Select(a => new { BooksCount = a.Books.Count });

        var authors = async
            ? await query.ToListAsync()
            : query.ToList();

        Assert.Equal(3, Assert.Single(authors).BooksCount);
    }

    protected class Context26433 : DbContext
    {
        public Context26433(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Book26433> Books { get; set; }
        public DbSet<Author26433> Authors { get; set; }

        public void Seed()
        {
            base.Add(
                new Author26433
                {
                    FirstName = "William",
                    LastName = "Shakespeare",
                    Books = new List<Book26433>
                    {
                        new() { Title = "Hamlet" },
                        new() { Title = "Othello" },
                        new() { Title = "MacBeth" }
                    }
                });

            SaveChanges();
        }
    }

    protected class Author26433
    {
        [Key]
        public int AuthorId { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public IReadOnlyCollection<Book26433> Books { get; set; }
    }

    protected class Book26433
    {
        [Key]
        public int BookId { get; set; }

        public string Title { get; set; }
        public int AuthorId { get; set; }
        public Author26433 Author { get; set; }
    }

    #endregion

    #region 26593

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Unwrap_convert_node_over_projection_when_translating_contains_over_subquery(bool async)
    {
        var contextFactory = await InitializeAsync<Context26593>(seed: c => c.Seed());
        using var context = contextFactory.CreateContext();

        var currentUserId = 1;

        var currentUserGroupIds = context.Memberships
            .Where(m => m.UserId == currentUserId)
            .Select(m => m.GroupId);

        var hasMembership = context.Memberships
            .Where(m => currentUserGroupIds.Contains(m.GroupId))
            .Select(m => m.User);

        var query = context.Users
            .Select(u => new { HasAccess = hasMembership.Contains(u) });

        var users = async
            ? await query.ToListAsync()
            : query.ToList();
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Unwrap_convert_node_over_projection_when_translating_contains_over_subquery_2(bool async)
    {
        var contextFactory = await InitializeAsync<Context26593>(seed: c => c.Seed());
        using var context = contextFactory.CreateContext();

        var currentUserId = 1;

        var currentUserGroupIds = context.Memberships
            .Where(m => m.UserId == currentUserId)
            .Select(m => m.Group);

        var hasMembership = context.Memberships
            .Where(m => currentUserGroupIds.Contains(m.Group))
            .Select(m => m.User);

        var query = context.Users
            .Select(u => new { HasAccess = hasMembership.Contains(u) });

        var users = async
            ? await query.ToListAsync()
            : query.ToList();
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Unwrap_convert_node_over_projection_when_translating_contains_over_subquery_3(bool async)
    {
        var contextFactory = await InitializeAsync<Context26593>(seed: c => c.Seed());
        using var context = contextFactory.CreateContext();

        var currentUserId = 1;

        var currentUserGroupIds = context.Memberships
            .Where(m => m.UserId == currentUserId)
            .Select(m => m.GroupId);

        var hasMembership = context.Memberships
            .Where(m => currentUserGroupIds.Contains(m.GroupId))
            .Select(m => m.User);

        var query = context.Users
            .Select(u => new { HasAccess = hasMembership.Any(e => e == u) });

        var users = async
            ? await query.ToListAsync()
            : query.ToList();
    }

    protected class Context26593 : DbContext
    {
        public Context26593(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Membership> Memberships { get; set; }

        public void Seed()
        {
            var user = new User();
            var group = new Group();
            var membership = new Membership { Group = group, User = user };
            AddRange(user, group, membership);

            SaveChanges();
        }
    }

    protected class User
    {
        public int Id { get; set; }

        public ICollection<Membership> Memberships { get; set; }
    }

    protected class Group
    {
        public int Id { get; set; }

        public ICollection<Membership> Memberships { get; set; }
    }

    protected class Membership
    {
        public int Id { get; set; }
        public User User { get; set; }
        public int UserId { get; set; }
        public Group Group { get; set; }
        public int GroupId { get; set; }
    }

    #endregion

    #region 26587

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task GroupBy_aggregate_on_right_side_of_join(bool async)
    {
        var contextFactory = await InitializeAsync<Context26587>();
        using var context = contextFactory.CreateContext();

        var orderId = 123456;

        var orderItems = context.OrderItems.Where(o => o.OrderId == orderId);
        var items = orderItems
            .GroupBy(
                o => o.OrderId,
                (o, g) => new
                {
                    Key = o, IsPending = g.Max(y => y.ShippingDate == null && y.CancellationDate == null ? o : (o - 10000000))
                })
            .OrderBy(e => e.Key);

        var query = orderItems
            .Join(items, x => x.OrderId, x => x.Key, (x, y) => x)
            .OrderBy(x => x.OrderId);

        var users = async
            ? await query.ToListAsync()
            : query.ToList();
    }

    protected class Context26587 : DbContext
    {
        public Context26587(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<OrderItem> OrderItems { get; set; }
    }

    protected class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public DateTime? ShippingDate { get; set; }
        public DateTime? CancellationDate { get; set; }
    }

    #endregion

    #region 26472

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Enum_with_value_converter_matching_take_value(bool async)
    {
        var contextFactory = await InitializeAsync<Context26472>();
        using var context = contextFactory.CreateContext();
        var orderItemType = OrderItemType.MyType1;
        var query = context.Orders.Where(x => x.Items.Any()).OrderBy(e => e.Id).Take(1)
            .Select(e => e.Id)
            .Join(context.Orders, o => o, i => i.Id, (o, i) => i)
            .Select(
                entity => new
                {
                    entity.Id,
                    SpecialSum = entity.Items.Where(x => x.Type == orderItemType)
                        .Select(x => x.Price)
                        .FirstOrDefault()
                });

        var result = async
            ? await query.ToListAsync()
            : query.ToList();
    }

    protected class Context26472 : DbContext
    {
        public Context26472(DbContextOptions options)
            : base(options)
        {
        }

        public virtual DbSet<Order26472> Orders { get; set; }
        public virtual DbSet<OrderItem26472> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<OrderItem26472>().Property(x => x.Type).HasConversion<string>();
    }

    protected class Order26472
    {
        public int Id { get; set; }

        public virtual ICollection<OrderItem26472> Items { get; set; }
    }

    protected class OrderItem26472
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public OrderItemType Type { get; set; }
        public double Price { get; set; }
    }

    protected enum OrderItemType
    {
        Undefined = 0,
        MyType1 = 1,
        MyType2 = 2
    }

    #endregion

    #region 27083

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task GroupBy_Aggregate_over_navigations_repeated(bool async)
    {
        var contextFactory = await InitializeAsync<Context27083>(seed: c => c.Seed());
        using var context = contextFactory.CreateContext();

        var query = context
            .Set<TimeSheet>()
            .Where(x => x.OrderId != null)
            .GroupBy(x => x.OrderId)
            .Select(
                x => new
                {
                    HourlyRate = x.Min(f => f.Order.HourlyRate),
                    CustomerId = x.Min(f => f.Project.Customer.Id),
                    CustomerName = x.Min(f => f.Project.Customer.Name),
                });

        var timeSheets = async
            ? await query.ToListAsync()
            : query.ToList();

        Assert.Equal(2, timeSheets.Count);
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Aggregate_over_subquery_in_group_by_projection(bool async)
    {
        var contextFactory = await InitializeAsync<Context27083>(seed: c => c.Seed());
        using var context = contextFactory.CreateContext();

        Expression<Func<Order, bool>> someFilterFromOutside = x => x.Number != "A1";

        var query = context
            .Set<Order>()
            .Where(someFilterFromOutside)
            .GroupBy(x => new { x.CustomerId, x.Number })
            .Select(
                x => new
                {
                    x.Key.CustomerId,
                    CustomerMinHourlyRate = context.Set<Order>().Where(n => n.CustomerId == x.Key.CustomerId).Min(h => h.HourlyRate),
                    HourlyRate = x.Min(f => f.HourlyRate),
                    Count = x.Count()
                });

        var orders = async
            ? await query.ToListAsync()
            : query.ToList();

        Assert.Collection(
            orders.OrderBy(x => x.CustomerId),
            t =>
            {
                Assert.Equal(1, t.CustomerId);
                Assert.Equal(10, t.CustomerMinHourlyRate);
                Assert.Equal(11, t.HourlyRate);
                Assert.Equal(1, t.Count);
            },
            t =>
            {
                Assert.Equal(2, t.CustomerId);
                Assert.Equal(20, t.CustomerMinHourlyRate);
                Assert.Equal(20, t.HourlyRate);
                Assert.Equal(1, t.Count);
            });
    }

    protected class Context27083 : DbContext
    {
        public Context27083(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<TimeSheet> TimeSheets { get; set; }
        public DbSet<Customer> Customers { get; set; }

        public void Seed()
        {
            var customerA = new Customer { Name = "Customer A" };
            var customerB = new Customer { Name = "Customer B" };

            var projectA = new Project { Customer = customerA };
            var projectB = new Project { Customer = customerB };

            var orderA1 = new Order
            {
                Number = "A1",
                Customer = customerA,
                HourlyRate = 10
            };
            var orderA2 = new Order
            {
                Number = "A2",
                Customer = customerA,
                HourlyRate = 11
            };
            var orderB1 = new Order
            {
                Number = "B1",
                Customer = customerB,
                HourlyRate = 20
            };

            var timeSheetA = new TimeSheet { Order = orderA1, Project = projectA };
            var timeSheetB = new TimeSheet { Order = orderB1, Project = projectB };

            AddRange(customerA, customerB);
            AddRange(projectA, projectB);
            AddRange(orderA1, orderA2, orderB1);
            AddRange(timeSheetA, timeSheetB);
            SaveChanges();
        }
    }

    protected class Customer
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public List<Project> Projects { get; set; }
        public List<Order> Orders { get; set; }
    }

    protected class Order
    {
        public int Id { get; set; }
        public string Number { get; set; }

        public int CustomerId { get; set; }
        public Customer Customer { get; set; }

        public int HourlyRate { get; set; }
    }

    protected class Project
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }

        public Customer Customer { get; set; }
    }

    protected class TimeSheet
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; }

        public int? OrderId { get; set; }
        public Order Order { get; set; }
    }

    #endregion

    #region 27094

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Aggregate_over_subquery_in_group_by_projection_2(bool async)
    {
        var contextFactory = await InitializeAsync<Context27094>();
        using var context = contextFactory.CreateContext();

        var query = from t in context.Table
                    group t.Id by t.Value
                    into tg
                    select new
                    {
                        A = tg.Key, B = context.Table.Where(t => t.Value == tg.Max() * 6).Max(t => (int?)t.Id),
                    };

        var orders = async
            ? await query.ToListAsync()
            : query.ToList();
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Group_by_aggregate_in_subquery_projection_after_group_by(bool async)
    {
        var contextFactory = await InitializeAsync<Context27094>();
        using var context = contextFactory.CreateContext();

        var query = from t in context.Table
                    group t.Id by t.Value
                    into tg
                    select new
                    {
                        A = tg.Key,
                        B = tg.Sum(),
                        C = (from t in context.Table
                             group t.Id by t.Value
                             into tg2
                             select tg.Sum() + tg2.Sum()
                            ).OrderBy(e => 1).FirstOrDefault()
                    };

        var orders = async
            ? await query.ToListAsync()
            : query.ToList();
    }

    protected class Context27094 : DbContext
    {
        public Context27094(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Table> Table { get; set; }
    }

    protected class Table
    {
        public int Id { get; set; }
        public int? Value { get; set; }
    }

    #endregion

    #region 26744

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Subquery_first_member_compared_to_null(bool async)
    {
        var contextFactory = await InitializeAsync<Context26744>(seed: c => c.Seed());
        using var context = contextFactory.CreateContext();

        var query = context.Parents
            .Where(
                p => p.Children.Any(c => c.SomeNullableDateTime == null)
                    && p.Children.Where(c => c.SomeNullableDateTime == null)
                        .OrderBy(c => c.SomeInteger)
                        .First().SomeOtherNullableDateTime
                    != null)
            .Select(
                p => p.Children.Where(c => c.SomeNullableDateTime == null)
                    .OrderBy(c => c.SomeInteger)
                    .First().SomeOtherNullableDateTime);

        var result = async
            ? await query.ToListAsync()
            : query.ToList();

        Assert.Single(result);
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task SelectMany_where_Select(bool async)
    {
        var contextFactory = await InitializeAsync<Context26744>(seed: c => c.Seed());
        using var context = contextFactory.CreateContext();

        var query = context.Parents
            .SelectMany(
                p => p.Children
                    .Where(c => c.SomeNullableDateTime == null)
                    .OrderBy(c => c.SomeInteger)
                    .Take(1))
            .Where(c => c.SomeOtherNullableDateTime != null)
            .Select(c => c.SomeNullableDateTime);

        var result = async
            ? await query.ToListAsync()
            : query.ToList();

        Assert.Single(result);
    }

    protected class Context26744 : DbContext
    {
        public Context26744(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Parent26744> Parents { get; set; }

        public void Seed()
        {
            Add(
                new Parent26744
                {
                    Children = new List<Child26744>
                    {
                        new() { SomeInteger = 1, SomeOtherNullableDateTime = new DateTime(2000, 11, 18) }
                    }
                });

            Add(new Parent26744 { Children = new List<Child26744> { new() { SomeInteger = 1, } } });

            SaveChanges();
        }
    }

    protected class Parent26744
    {
        public int Id { get; set; }
        public List<Child26744> Children { get; set; }
    }

    protected class Child26744
    {
        public int Id { get; set; }
        public int SomeInteger { get; set; }
        public DateTime? SomeNullableDateTime { get; set; }
        public DateTime? SomeOtherNullableDateTime { get; set; }
        public Parent26744 Parent { get; set; }
    }

    #endregion

    #region 27343

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Flattened_GroupJoin_on_interface_generic(bool async)
    {
        var contextFactory = await InitializeAsync<Context27343>(seed: c => c.Seed());
        using var context = contextFactory.CreateContext();

        var entitySet = context.Parents.AsQueryable<IDocumentType27343>();

        var query = from p in entitySet
                    join c in context.Set<Child27343>()
                        on p.Id equals c.Id into grouping
                    from c in grouping.DefaultIfEmpty()
                    select c;

        var result = async
            ? await query.ToListAsync()
            : query.ToList();

        Assert.Empty(result);
    }

    protected class Context27343 : DbContext
    {
        public Context27343(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Parent27343> Parents { get; set; }

        public void Seed()
            => SaveChanges();
    }

    protected interface IDocumentType27343
    {
        public int Id { get; }
    }

    protected class Parent27343 : IDocumentType27343
    {
        public int Id { get; set; }
        public List<Child27343> Children { get; set; }
    }

    protected class Child27343
    {
        public int Id { get; set; }
        public int SomeInteger { get; set; }
        public DateTime? SomeNullableDateTime { get; set; }
        public DateTime? SomeOtherNullableDateTime { get; set; }
        public Parent27343 Parent { get; set; }
    }

    #endregion

    #region 28196

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual Task Hierarchy_query_with_abstract_type_sibling(bool async)
        => Hierarchy_query_with_abstract_type_sibling_helper(async, null);

    public virtual async Task Hierarchy_query_with_abstract_type_sibling_helper(bool async, Action<ModelBuilder> onModelCreating)
    {
        var contextFactory = await InitializeAsync<Context28196>(onModelCreating: onModelCreating, seed: c => c.Seed());
        using var context = contextFactory.CreateContext();

        var query = context.Animals.OfType<Pet>().Where(a => a.Species.StartsWith("F"));

        var result = async
            ? await query.ToListAsync()
            : query.ToList();
    }

    protected class Context28196 : DbContext
    {
        public Context28196(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Animal> Animals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Animal>().Property(e => e.Id).ValueGeneratedNever();
            modelBuilder.Entity<Pet>();
            modelBuilder.Entity<Cat>();
            modelBuilder.Entity<Dog>();
            modelBuilder.Entity<FarmAnimal>();
        }

        public void Seed()
        {
            AddRange(
                new Cat
                {
                    Id = 1,
                    Name = "Alice",
                    Species = "Felis catus",
                    EdcuationLevel = "MBA"
                },
                new Cat
                {
                    Id = 2,
                    Name = "Mac",
                    Species = "Felis catus",
                    EdcuationLevel = "BA"
                },
                new Dog
                {
                    Id = 3,
                    Name = "Toast",
                    Species = "Canis familiaris",
                    FavoriteToy = "Mr. Squirrel"
                },
                new FarmAnimal
                {
                    Id = 4,
                    Value = 100.0,
                    Species = "Ovis aries"
                });

            SaveChanges();
        }
    }

    #endregion

    #region 28039

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Pushdown_does_not_add_grouping_key_to_projection_when_distinct_is_applied(bool async)
    {
        var contextFactory = await InitializeAsync<Context28039>();
        using var db = contextFactory.CreateContext();

        var queryResults = (from i in db.IndexData.Where(a => a.Parcel == "some condition")
                                .Select(a => new SearchResult { ParcelNumber = a.Parcel, RowId = a.RowId })
                            group i by new { i.ParcelNumber, i.RowId }
                            into grp
                            where grp.Count() == 1
                            select grp.Key.ParcelNumber).Distinct();

        var jsonLookup = (from dcv in db.TableData.Where(a => a.TableId == 123)
                          join wos in queryResults
                              on dcv.ParcelNumber equals wos
                          orderby dcv.ParcelNumber
                          select dcv.JSON).Take(123456);

        var result = async
            ? await jsonLookup.ToListAsync()
            : jsonLookup.ToList();
    }

    protected class Context28039 : DbContext
    {
        public Context28039(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<IndexData> IndexData { get; set; }
        public DbSet<TableData> TableData { get; set; }
    }

    public class TableData : EntityBase
    {
        public int TableId { get; set; }
        public string ParcelNumber { get; set; }
        public short RowId { get; set; }
        public string JSON { get; set; }
    }

    public abstract class EntityBase
    {
        [Key]
        public int ID { get; set; }
    }

    public class IndexData : EntityBase
    {
        public string Parcel { get; set; }
        public int RowId { get; set; }
    }

    internal class SearchResult
    {
        public string ParcelNumber { get; set; }
        public int RowId { get; set; }
        public string DistinctValue { get; set; }
    }

    protected abstract class Animal
    {
        public int Id { get; set; }
        public string Species { get; set; }
    }

    protected class FarmAnimal : Animal
    {
        public double Value { get; set; }
    }

    protected abstract class Pet : Animal
    {
        public string Name { get; set; }
    }

    protected class Cat : Pet
    {
        public string EdcuationLevel { get; set; }
    }

    protected class Dog : Pet
    {
        public string FavoriteToy { get; set; }
    }

    #endregion

    #region 31961

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Filter_on_nested_DTO_with_interface_gets_simplified_correctly(bool async)
    {
        var contextFactory = await InitializeAsync<Context31961>();
        using var context = contextFactory.CreateContext();

        var query = await context.Customers
            .Select(m => new CustomerDto31961()
            {
                Id = m.Id,
                CompanyId = m.CompanyId,
                Company = m.Company != null ? new CompanyDto31961()
                {
                    Id = m.Company.Id,
                    CompanyName = m.Company.CompanyName,
                    CountryId = m.Company.CountryId,
                    Country = new CountryDto31961()
                    {
                        Id = m.Company.Country.Id,
                        CountryName = m.Company.Country.CountryName,
                    },
                } : null,
            })
        .Where(m => m.Company.Country.CountryName == "COUNTRY")
        .ToListAsync();
    }

    protected class Context31961 : DbContext
    {
        public Context31961(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Customer31961> Customers { get; set; }

        public DbSet<Company31961> Companies { get; set; }

        public DbSet<Country31961> Countries { get; set; }
    }

    public class Customer31961
    {
        public string Id { get; set; } = string.Empty;

        public string CompanyId { get; set; }

        public Company31961 Company { get; set; }
    }

    public class Country31961
    {
        public string Id { get; set; } = string.Empty;

        public string CountryName { get; set; } = string.Empty;
    }

    public class Company31961
    {
        public string Id { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string CountryId { get; set; }

        public Country31961 Country { get; set; }
    }

    public interface ICustomerDto31961
    {
        string Id { get; set; }

        string CompanyId { get; set; }

        ICompanyDto31961 Company { get; set; }
    }

    public interface ICountryDto31961
    {
        string Id { get; set; }

        string CountryName { get; set; }
    }

    public interface ICompanyDto31961
    {
        string Id { get; set; }

        string CompanyName { get; set; }

        string CountryId { get; set; }

        ICountryDto31961 Country { get; set; }
    }

    public class CustomerDto31961 : ICustomerDto31961
    {
        public string Id { get; set; } = string.Empty;

        public string CompanyId { get; set; }

        public ICompanyDto31961 Company { get; set; }
    }

    public class CountryDto31961 : ICountryDto31961
    {
        public string Id { get; set; } = string.Empty;

        public string CountryName { get; set; } = string.Empty;
    }

    public class CompanyDto31961 : ICompanyDto31961
    {
        public string Id { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string CountryId { get; set; }

        public ICountryDto31961 Country { get; set; }
    }

    #endregion
}
