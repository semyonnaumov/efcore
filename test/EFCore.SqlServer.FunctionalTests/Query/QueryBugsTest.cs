// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetTopologySuite.Geometries;
using Newtonsoft.Json.Linq;

// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable AccessToDisposedClosure
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable UnusedMember.Local
namespace Microsoft.EntityFrameworkCore.Query;

public class QueryBugsTest : NonSharedModelTestBase
{
    public QueryBugsTest(ITestOutputHelper testOutputHelper)
    {
    }

    #region Issue5456

    [ConditionalFact]
    public virtual async Task Repro5456_include_group_join_is_per_query_context()
    {
        var contextFactory = await InitializeAsync<MyContext5456>(seed: c => c.Seed());

        Parallel.For(
            0, 10, i =>
            {
                using var ctx = contextFactory.CreateContext();
                var result = ctx.Posts.Where(x => x.Blog.Id > 1).Include(x => x.Blog).ToList();

                Assert.Equal(198, result.Count);
            });

        Parallel.For(
            0, 10, i =>
            {
                using var ctx = contextFactory.CreateContext();
                var result = ctx.Posts.Where(x => x.Blog.Id > 1).Include(x => x.Blog).Include(x => x.Comments).ToList();

                Assert.Equal(198, result.Count);
            });

        Parallel.For(
            0, 10, i =>
            {
                using var ctx = contextFactory.CreateContext();
                var result = ctx.Posts.Where(x => x.Blog.Id > 1).Include(x => x.Blog).ThenInclude(b => b.Author).ToList();

                Assert.Equal(198, result.Count);
            });
    }

    [ConditionalFact]
    public virtual async Task Repro5456_include_group_join_is_per_query_context_async()
    {
        var contextFactory = await InitializeAsync<MyContext5456>(seed: c => c.Seed());

        await Task.WhenAll(
            Enumerable.Range(0, 10)
                .Select(
                    async i =>
                    {
                        using var ctx = contextFactory.CreateContext();
                        var result = await ctx.Posts.Where(x => x.Blog.Id > 1).Include(x => x.Blog).ToListAsync();

                        Assert.Equal(198, result.Count);
                    }));

        await Task.WhenAll(
            Enumerable.Range(0, 10)
                .Select(
                    async i =>
                    {
                        using var ctx = contextFactory.CreateContext();
                        var result = await ctx.Posts.Where(x => x.Blog.Id > 1).Include(x => x.Blog).Include(x => x.Comments)
                            .ToListAsync();

                        Assert.Equal(198, result.Count);
                    }));

        await Task.WhenAll(
            Enumerable.Range(0, 10)
                .Select(
                    async i =>
                    {
                        using var ctx = contextFactory.CreateContext();
                        var result = await ctx.Posts.Where(x => x.Blog.Id > 1).Include(x => x.Blog).ThenInclude(b => b.Author)
                            .ToListAsync();

                        Assert.Equal(198, result.Count);
                    }));
    }

    protected class MyContext5456 : DbContext
    {
        public MyContext5456(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Blog5456> Blogs { get; set; }
        public DbSet<Post5456> Posts { get; set; }
        public DbSet<Comment5456> Comments { get; set; }
        public DbSet<Author5456> Authors { get; set; }

        public void Seed()
        {
            for (var i = 0; i < 100; i++)
            {
                Add(
                    new Blog5456
                    {
                        Posts = new List<Post5456> { new() { Comments = new List<Comment5456> { new(), new() } }, new() },
                        Author = new Author5456()
                    });
            }

            SaveChanges();
        }

        public class Blog5456
        {
            public int Id { get; set; }
            public List<Post5456> Posts { get; set; }
            public Author5456 Author { get; set; }
        }

        public class Author5456
        {
            public int Id { get; set; }
            public List<Blog5456> Blogs { get; set; }
        }

        public class Post5456
        {
            public int Id { get; set; }
            public Blog5456 Blog { get; set; }
            public List<Comment5456> Comments { get; set; }
        }

        public class Comment5456
        {
            public int Id { get; set; }
            public Post5456 Blog { get; set; }
        }
    }

    #endregion

    #region Issue8538

    [ConditionalFact]
    public virtual async Task Enum_has_flag_applies_explicit_cast_for_constant()
    {
        var contextFactory = await InitializeAsync<MyContext8538>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entity.Where(e => e.Permission.HasFlag(MyContext8538.Permission.READ_WRITE)).ToList();

            Assert.Single(query);

            AssertSql(
                """
SELECT [e].[Id], [e].[Permission], [e].[PermissionByte], [e].[PermissionShort]
FROM [Entity] AS [e]
WHERE [e].[Permission] & CAST(17179869184 AS bigint) = CAST(17179869184 AS bigint)
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            var query = context.Entity.Where(e => e.PermissionShort.HasFlag(MyContext8538.PermissionShort.READ_WRITE)).ToList();

            Assert.Single(query);

            AssertSql(
                """
SELECT [e].[Id], [e].[Permission], [e].[PermissionByte], [e].[PermissionShort]
FROM [Entity] AS [e]
WHERE [e].[PermissionShort] & CAST(4 AS smallint) = CAST(4 AS smallint)
""");
        }
    }

    [ConditionalFact]
    public virtual async Task Enum_has_flag_does_not_apply_explicit_cast_for_non_constant()
    {
        var contextFactory = await InitializeAsync<MyContext8538>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entity.Where(e => e.Permission.HasFlag(e.Permission)).ToList();

            Assert.Equal(3, query.Count);

            AssertSql(
                """
SELECT [e].[Id], [e].[Permission], [e].[PermissionByte], [e].[PermissionShort]
FROM [Entity] AS [e]
WHERE [e].[Permission] & [e].[Permission] = [e].[Permission]
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            var query = context.Entity.Where(e => e.PermissionByte.HasFlag(e.PermissionByte)).ToList();

            Assert.Equal(3, query.Count);

            AssertSql(
                """
SELECT [e].[Id], [e].[Permission], [e].[PermissionByte], [e].[PermissionShort]
FROM [Entity] AS [e]
WHERE [e].[PermissionByte] & [e].[PermissionByte] = [e].[PermissionByte]
""");
        }
    }

    private class MyContext8538 : DbContext
    {
        public MyContext8538(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Entity8538> Entity { get; set; }

        public void Seed()
        {
            AddRange(
                new Entity8538
                {
                    Permission = Permission.NONE,
                    PermissionByte = PermissionByte.NONE,
                    PermissionShort = PermissionShort.NONE
                },
                new Entity8538
                {
                    Permission = Permission.READ_ONLY,
                    PermissionByte = PermissionByte.READ_ONLY,
                    PermissionShort = PermissionShort.READ_ONLY
                },
                new Entity8538
                {
                    Permission = Permission.READ_WRITE,
                    PermissionByte = PermissionByte.READ_WRITE,
                    PermissionShort = PermissionShort.READ_WRITE
                }
            );

            SaveChanges();
        }

        public class Entity8538
        {
            public int Id { get; set; }
            public Permission Permission { get; set; }
            public PermissionByte PermissionByte { get; set; }
            public PermissionShort PermissionShort { get; set; }
        }

        [Flags]
        public enum PermissionByte : byte
        {
            NONE = 1,
            READ_ONLY = 2,
            READ_WRITE = 4
        }

        [Flags]
        public enum PermissionShort : short
        {
            NONE = 1,
            READ_ONLY = 2,
            READ_WRITE = 4
        }

        [Flags]
        public enum Permission : long
        {
            NONE = 0x01,
            READ_ONLY = 0x02,
            READ_WRITE = 0x400000000 // 36 bits
        }
    }

    #endregion

    #region Issue11885

    [ConditionalFact]
    public virtual async Task Average_with_cast()
    {
        var contextFactory = await InitializeAsync<MyContext11885>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var prices = context.Prices.ToList();

            ClearLog();

            Assert.Equal(prices.Average(e => e.Price), context.Prices.Average(e => e.Price));
            Assert.Equal(prices.Average(e => e.IntColumn), context.Prices.Average(e => e.IntColumn));
            Assert.Equal(prices.Average(e => e.NullableIntColumn), context.Prices.Average(e => e.NullableIntColumn));
            Assert.Equal(prices.Average(e => e.LongColumn), context.Prices.Average(e => e.LongColumn));
            Assert.Equal(prices.Average(e => e.NullableLongColumn), context.Prices.Average(e => e.NullableLongColumn));
            Assert.Equal(prices.Average(e => e.FloatColumn), context.Prices.Average(e => e.FloatColumn));
            Assert.Equal(prices.Average(e => e.NullableFloatColumn), context.Prices.Average(e => e.NullableFloatColumn));
            Assert.Equal(prices.Average(e => e.DoubleColumn), context.Prices.Average(e => e.DoubleColumn));
            Assert.Equal(prices.Average(e => e.NullableDoubleColumn), context.Prices.Average(e => e.NullableDoubleColumn));
            Assert.Equal(prices.Average(e => e.DecimalColumn), context.Prices.Average(e => e.DecimalColumn));
            Assert.Equal(prices.Average(e => e.NullableDecimalColumn), context.Prices.Average(e => e.NullableDecimalColumn));

            AssertSql(
                """
SELECT AVG([p].[Price])
FROM [Prices] AS [p]
""",
                //
                """
SELECT AVG(CAST([p].[IntColumn] AS float))
FROM [Prices] AS [p]
""",
                //
                """
SELECT AVG(CAST([p].[NullableIntColumn] AS float))
FROM [Prices] AS [p]
""",
                //
                """
SELECT AVG(CAST([p].[LongColumn] AS float))
FROM [Prices] AS [p]
""",
                //
                """
SELECT AVG(CAST([p].[NullableLongColumn] AS float))
FROM [Prices] AS [p]
""",
                //
                """
SELECT CAST(AVG([p].[FloatColumn]) AS real)
FROM [Prices] AS [p]
""",
                //
                """
SELECT CAST(AVG([p].[NullableFloatColumn]) AS real)
FROM [Prices] AS [p]
""",
                //
                """
SELECT AVG([p].[DoubleColumn])
FROM [Prices] AS [p]
""",
                //
                """
SELECT AVG([p].[NullableDoubleColumn])
FROM [Prices] AS [p]
""",
                //
                """
SELECT AVG([p].[DecimalColumn])
FROM [Prices] AS [p]
""",
                //
                """
SELECT AVG([p].[NullableDecimalColumn])
FROM [Prices] AS [p]
""");
        }
    }

    protected class MyContext11885 : DbContext
    {
        public DbSet<Price11885> Prices { get; set; }

        public MyContext11885(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Price11885>(
                b =>
                {
                    b.Property(e => e.Price).HasColumnType("DECIMAL(18, 8)");
                    b.Property(e => e.DecimalColumn).HasColumnType("DECIMAL(18, 2)");
                    b.Property(e => e.NullableDecimalColumn).HasColumnType("DECIMAL(18, 2)");
                });

        public void Seed()
        {
            AddRange(
                new Price11885
                {
                    IntColumn = 1,
                    NullableIntColumn = 1,
                    LongColumn = 1000,
                    NullableLongColumn = 1000,
                    FloatColumn = 0.1F,
                    NullableFloatColumn = 0.1F,
                    DoubleColumn = 0.000001,
                    NullableDoubleColumn = 0.000001,
                    DecimalColumn = 1.0m,
                    NullableDecimalColumn = 1.0m,
                    Price = 0.00112000m
                },
                new Price11885
                {
                    IntColumn = 2,
                    NullableIntColumn = 2,
                    LongColumn = 2000,
                    NullableLongColumn = 2000,
                    FloatColumn = 0.2F,
                    NullableFloatColumn = 0.2F,
                    DoubleColumn = 0.000002,
                    NullableDoubleColumn = 0.000002,
                    DecimalColumn = 2.0m,
                    NullableDecimalColumn = 2.0m,
                    Price = 0.00232111m
                },
                new Price11885
                {
                    IntColumn = 3,
                    LongColumn = 3000,
                    FloatColumn = 0.3F,
                    DoubleColumn = 0.000003,
                    DecimalColumn = 3.0m,
                    Price = 0.00345223m
                }
            );

            SaveChanges();
        }

        public class Price11885
        {
            public int Id { get; set; }
            public int IntColumn { get; set; }
            public int? NullableIntColumn { get; set; }
            public long LongColumn { get; set; }
            public long? NullableLongColumn { get; set; }
            public float FloatColumn { get; set; }
            public float? NullableFloatColumn { get; set; }
            public double DoubleColumn { get; set; }
            public double? NullableDoubleColumn { get; set; }
            public decimal DecimalColumn { get; set; }
            public decimal? NullableDecimalColumn { get; set; }
            public decimal Price { get; set; }
        }
    }

    #endregion

    #region Issue11944

    [ConditionalFact]
    public virtual async Task Include_collection_works_when_defined_on_intermediate_type()
    {
        var contextFactory = await InitializeAsync<MyContext11944>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Schools.Include(s => ((MyContext11944.ElementarySchool11944)s).Students);
            var result = query.ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(2, result.OfType<MyContext11944.ElementarySchool11944>().Single().Students.Count);
        }

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Schools.Select(s => ((MyContext11944.ElementarySchool11944)s).Students.Where(ss => true).ToList());
            var result = query.ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Count() == 2);
        }
    }

    protected class MyContext11944 : DbContext
    {
        public DbSet<Student11944> Students { get; set; }
        public DbSet<School11944> Schools { get; set; }
        public DbSet<ElementarySchool11944> ElementarySchools { get; set; }

        public MyContext11944(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<ElementarySchool11944>().HasMany(s => s.Students).WithOne(s => s.School);

        public void Seed()
        {
            var student1 = new Student11944();
            var student2 = new Student11944();
            var school = new School11944();
            var elementarySchool = new ElementarySchool11944 { Students = new List<Student11944> { student1, student2 } };

            Students.AddRange(student1, student2);
            Schools.AddRange(school);
            ElementarySchools.Add(elementarySchool);

            SaveChanges();
        }

        public class Student11944
        {
            public int Id { get; set; }
            public ElementarySchool11944 School { get; set; }
        }

        public class School11944
        {
            public int Id { get; set; }
        }

        public abstract class PrimarySchool11944 : School11944
        {
            public List<Student11944> Students { get; set; }
        }

        public class ElementarySchool11944 : PrimarySchool11944
        {
        }
    }

    #endregion

    #region Issue12732

    [ConditionalFact]
    public virtual async Task Nested_contains_with_enum()
    {
        var contextFactory = await InitializeAsync<MyContext12732>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var key = Guid.Parse("5f221fb9-66f4-442a-92c9-d97ed5989cc7");
            var keys = new List<Guid> { Guid.Parse("0a47bcb7-a1cb-4345-8944-c58f82d6aac7"), key };
            var todoTypes = new List<MyContext12732.TodoType> { MyContext12732.TodoType.foo0 };

            // Note that in this query, the outer Contains really has no type mapping, neither for its source (collection parameter), nor
            // for its item (the conditional expression returns key, which is also a parameter). The default type mapping must be applied.
            var query = context.Todos
                .Where(x => keys.Contains(todoTypes.Contains(x.Type) ? key : key))
                .ToList();

            Assert.Single(query);

            AssertSql(
                """
@__todoTypes_1='[0]' (Size = 4000)
@__key_2='5f221fb9-66f4-442a-92c9-d97ed5989cc7'
@__keys_0='["0a47bcb7-a1cb-4345-8944-c58f82d6aac7","5f221fb9-66f4-442a-92c9-d97ed5989cc7"]' (Size = 4000)

SELECT [t].[Id], [t].[Type]
FROM [Todos] AS [t]
WHERE CASE
    WHEN [t].[Type] IN (
        SELECT [t0].[value]
        FROM OPENJSON(@__todoTypes_1) WITH ([value] int '$') AS [t0]
    ) THEN @__key_2
    ELSE @__key_2
END IN (
    SELECT [k].[value]
    FROM OPENJSON(@__keys_0) WITH ([value] uniqueidentifier '$') AS [k]
)
""");
        }
    }

    protected class MyContext12732 : DbContext
    {
        public DbSet<Todo> Todos { get; set; }

        public MyContext12732(DbContextOptions options)
            : base(options)
        {
        }

        public void Seed()
        {
            Add(new Todo { Type = TodoType.foo0 });
            SaveChanges();
        }

        public class Todo
        {
            public Guid Id { get; set; }
            public TodoType Type { get; set; }
        }

        public enum TodoType
        {
            foo0 = 0
        }
    }

    #endregion

    #region Issue13587

    [ConditionalFact]
    public virtual async Task Type_casting_inside_sum()
    {
        var contextFactory = await InitializeAsync<MyContext13587>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var result = context.InventoryPools.Sum(p => (decimal)p.Quantity);

            AssertSql(
                """
SELECT COALESCE(SUM(CAST([i].[Quantity] AS decimal(18,2))), 0.0)
FROM [InventoryPools] AS [i]
""");
        }
    }

    protected class MyContext13587 : DbContext
    {
        public virtual DbSet<InventoryPool13587> InventoryPools { get; set; }

        public MyContext13587(DbContextOptions options)
            : base(options)
        {
        }

        public void Seed()
        {
            InventoryPools.Add(new InventoryPool13587 { Quantity = 2 });

            SaveChanges();
        }

        public class InventoryPool13587
        {
            public int Id { get; set; }
            public double Quantity { get; set; }
        }
    }

    #endregion

    #region Issue12518

    [ConditionalFact]
    public virtual async Task Projecting_entity_with_value_converter_and_include_works()
    {
        var contextFactory = await InitializeAsync<MyContext12518>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var result = context.Parents.Include(p => p.Child).OrderBy(e => e.Id).FirstOrDefault();

            AssertSql(
                """
SELECT TOP(1) [p].[Id], [p].[ChildId], [c].[Id], [c].[ParentId], [c].[ULongRowVersion]
FROM [Parents] AS [p]
LEFT JOIN [Children] AS [c] ON [p].[ChildId] = [c].[Id]
ORDER BY [p].[Id]
""");
        }
    }

    [ConditionalFact]
    public virtual async Task Projecting_column_with_value_converter_of_ulong_byte_array()
    {
        var contextFactory = await InitializeAsync<MyContext12518>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var result = context.Parents.OrderBy(e => e.Id).Select(p => (ulong?)p.Child.ULongRowVersion).FirstOrDefault();

            AssertSql(
                """
SELECT TOP(1) [c].[ULongRowVersion]
FROM [Parents] AS [p]
LEFT JOIN [Children] AS [c] ON [p].[ChildId] = [c].[Id]
ORDER BY [p].[Id]
""");
        }
    }

    protected class MyContext12518 : DbContext
    {
        public virtual DbSet<Parent12518> Parents { get; set; }
        public virtual DbSet<Child12518> Children { get; set; }

        public MyContext12518(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var child = modelBuilder.Entity<Child12518>();
            child.HasOne(_ => _.Parent)
                .WithOne(_ => _.Child)
                .HasForeignKey<Parent12518>(_ => _.ChildId);
            child.Property(x => x.ULongRowVersion)
                .HasConversion(new NumberToBytesConverter<ulong>())
                .IsRowVersion()
                .IsRequired()
                .HasColumnType("RowVersion");

            modelBuilder.Entity<Parent12518>();
        }

        public void Seed()
        {
            Parents.Add(new Parent12518());
            SaveChanges();
        }

        public class Parent12518
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public Guid? ChildId { get; set; }
            public Child12518 Child { get; set; }
        }

        public class Child12518
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public ulong ULongRowVersion { get; set; }
            public Guid ParentId { get; set; }
            public Parent12518 Parent { get; set; }
        }
    }

    #endregion

    #region Issue12549

    [ConditionalFact]
    public virtual async Task Union_and_insert_12549()
    {
        var contextFactory = await InitializeAsync<MyContext12549>();

        using (var context = contextFactory.CreateContext())
        {
            var id1 = 1;
            var id2 = 2;

            var ids1 = context.Set<MyContext12549.Table1_12549>()
                .Where(x => x.Id == id1)
                .Select(x => x.Id);

            var ids2 = context.Set<MyContext12549.Table2_12549>()
                .Where(x => x.Id == id2)
                .Select(x => x.Id);

            var results = ids1.Union(ids2).ToList();

            context.AddRange(
                new MyContext12549.Table1_12549(),
                new MyContext12549.Table2_12549(),
                new MyContext12549.Table1_12549(),
                new MyContext12549.Table2_12549());
            context.SaveChanges();
        }
    }

    private class MyContext12549 : DbContext
    {
        public DbSet<Table1_12549> Table1 { get; set; }
        public DbSet<Table2_12549> Table2 { get; set; }

        public MyContext12549(DbContextOptions options)
            : base(options)
        {
        }

        public class Table1_12549
        {
            public int Id { get; set; }
        }

        public class Table2_12549
        {
            public int Id { get; set; }
        }
    }

    #endregion

    #region Issue16233

    [ConditionalFact]
    public virtual async Task Derived_reference_is_skipped_when_base_type()
    {
        var contextFactory = await InitializeAsync<MyContext16233>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var result = context.Bases.Include(p => ((MyContext16233.DerivedType16233)p).Reference).OrderBy(b => b.Id).ToList();

            Assert.Equal(3, result.Count);
            Assert.NotNull(Assert.IsType<MyContext16233.DerivedType16233>(result[1]).Reference);
            Assert.Null(Assert.IsType<MyContext16233.DerivedType16233>(result[2]).Reference);
            Assert.True(context.Entry(Assert.IsType<MyContext16233.DerivedType16233>(result[2])).Reference("Reference").IsLoaded);

            AssertSql(
                """
SELECT [b].[Id], [b].[Discriminator], [r].[Id], [r].[DerivedTypeId]
FROM [Bases] AS [b]
LEFT JOIN [Reference16233] AS [r] ON [b].[Id] = [r].[DerivedTypeId]
ORDER BY [b].[Id]
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            var result = context.Bases.AsNoTracking().Include(p => ((MyContext16233.DerivedType16233)p).Reference).OrderBy(b => b.Id)
                .ToList();

            Assert.Equal(3, result.Count);
            Assert.NotNull(Assert.IsType<MyContext16233.DerivedType16233>(result[1]).Reference);
            Assert.NotNull(Assert.IsType<MyContext16233.DerivedType16233>(result[1]).Reference.DerivedType);
            Assert.Null(Assert.IsType<MyContext16233.DerivedType16233>(result[2]).Reference);

            AssertSql(
                """
SELECT [b].[Id], [b].[Discriminator], [r].[Id], [r].[DerivedTypeId]
FROM [Bases] AS [b]
LEFT JOIN [Reference16233] AS [r] ON [b].[Id] = [r].[DerivedTypeId]
ORDER BY [b].[Id]
""");
        }
    }

    private class MyContext16233 : DbContext
    {
        public virtual DbSet<BaseType16233> Bases { get; set; }
        public virtual DbSet<DerivedType16233> Derived { get; set; }

        public MyContext16233(DbContextOptions options)
            : base(options)
        {
        }

        public void Seed()
        {
            AddRange(
                new BaseType16233(),
                new DerivedType16233 { Reference = new Reference16233() },
                new DerivedType16233());

            SaveChanges();
        }

        public class BaseType16233
        {
            public int Id { get; set; }
        }

        public class DerivedType16233 : BaseType16233
        {
            public Reference16233 Reference { get; set; }
        }

        public class Reference16233
        {
            public int Id { get; set; }
            public int DerivedTypeId { get; set; }
            public DerivedType16233 DerivedType { get; set; }
        }
    }

    #endregion

    #region Issue15684

    [ConditionalFact]
    public virtual async Task Projection_failing_with_EnumToStringConverter()
    {
        var contextFactory = await InitializeAsync<MyContext15684>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var query = from p in context.Products
                        join c in context.Categories on p.CategoryId equals c.Id into grouping
                        from c in grouping.DefaultIfEmpty()
                        select new MyContext15684.ProductDto15684
                        {
                            Id = p.Id,
                            Name = p.Name,
                            CategoryName = c == null ? "Other" : c.Name,
                            CategoryStatus = c == null ? MyContext15684.CategoryStatus15684.Active : c.Status
                        };
            var result = query.ToList();
            Assert.Equal(2, result.Count);

            AssertSql(
                """
SELECT [p].[Id], [p].[Name], CASE
    WHEN [c].[Id] IS NULL THEN N'Other'
    ELSE [c].[Name]
END AS [CategoryName], CASE
    WHEN [c].[Id] IS NULL THEN N'Active'
    ELSE [c].[Status]
END AS [CategoryStatus]
FROM [Products] AS [p]
LEFT JOIN [Categories] AS [c] ON [p].[CategoryId] = [c].[Id]
""");
        }
    }

    protected class MyContext15684 : DbContext
    {
        public DbSet<Category15684> Categories { get; set; }
        public DbSet<Product15684> Products { get; set; }

        public MyContext15684(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder
                .Entity<Category15684>()
                .Property(e => e.Status)
                .HasConversion(new EnumToStringConverter<CategoryStatus15684>());

        public void Seed()
        {
            Products.Add(
                new Product15684 { Name = "Apple", Category = new Category15684 { Name = "Fruit", Status = CategoryStatus15684.Active } });

            Products.Add(new Product15684 { Name = "Bike" });

            SaveChanges();
        }

        public class Product15684
        {
            [Key]
            public int Id { get; set; }

            [Required]
            public string Name { get; set; }

            public int? CategoryId { get; set; }

            public Category15684 Category { get; set; }
        }

        public class Category15684
        {
            [Key]
            public int Id { get; set; }

            [Required]
            public string Name { get; set; }

            public CategoryStatus15684 Status { get; set; }
        }

        public class ProductDto15684
        {
            public string CategoryName { get; set; }
            public CategoryStatus15684 CategoryStatus { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public enum CategoryStatus15684
        {
            Active = 0,
            Removed = 1
        }
    }

    #endregion

    #region Issue15204

    private MemberInfo GetMemberInfo(Type type, string name)
        => type.GetProperty(name);

    [ConditionalFact]
    public virtual async Task Null_check_removal_applied_recursively()
    {
        var contextFactory = await InitializeAsync<MyContext15204>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var userParam = Expression.Parameter(typeof(MyContext15204.TBuilding15204), "s");
            var builderProperty = Expression.MakeMemberAccess(
                userParam, GetMemberInfo(typeof(MyContext15204.TBuilding15204), "Builder"));
            var cityProperty = Expression.MakeMemberAccess(
                builderProperty, GetMemberInfo(typeof(MyContext15204.TBuilder15204), "City"));
            var nameProperty = Expression.MakeMemberAccess(cityProperty, GetMemberInfo(typeof(MyContext15204.TCity15204), "Name"));

            //{s => (IIF((IIF((s.Builder == null), null, s.Builder.City) == null), null, s.Builder.City.Name) == "Leeds")}
            var selection = Expression.Lambda<Func<MyContext15204.TBuilding15204, bool>>(
                Expression.Equal(
                    Expression.Condition(
                        Expression.Equal(
                            Expression.Condition(
                                Expression.Equal(
                                    builderProperty,
                                    Expression.Constant(null, typeof(MyContext15204.TBuilder15204))),
                                Expression.Constant(null, typeof(MyContext15204.TCity15204)),
                                cityProperty),
                            Expression.Constant(null, typeof(MyContext15204.TCity15204))),
                        Expression.Constant(null, typeof(string)),
                        nameProperty),
                    Expression.Constant("Leeds", typeof(string))),
                userParam);

            var query = context.BuildingSet
                .Where(selection)
                .Include(a => a.Builder).ThenInclude(a => a.City)
                .Include(a => a.Mandator).ToList();

            Assert.True(query.Count == 1);
            Assert.True(query.First().Builder.City.Name == "Leeds");
            Assert.True(query.First().LongName == "Two L2");

            AssertSql(
                """
SELECT [b].[Id], [b].[BuilderId], [b].[Identity], [b].[LongName], [b].[MandatorId], [b0].[Id], [b0].[CityId], [b0].[Name], [c].[Id], [c].[Name], [m].[Id], [m].[Identity], [m].[Name]
FROM [BuildingSet] AS [b]
INNER JOIN [Builder] AS [b0] ON [b].[BuilderId] = [b0].[Id]
INNER JOIN [City] AS [c] ON [b0].[CityId] = [c].[Id]
INNER JOIN [MandatorSet] AS [m] ON [b].[MandatorId] = [m].[Id]
WHERE [c].[Name] = N'Leeds'
""");
        }
    }

    protected class MyContext15204 : DbContext
    {
        public DbSet<TMandator15204> MandatorSet { get; set; }
        public DbSet<TBuilding15204> BuildingSet { get; set; }
        public DbSet<TBuilder15204> Builder { get; set; }
        public DbSet<TCity15204> City { get; set; }

        public MyContext15204(DbContextOptions options)
            : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            ChangeTracker.AutoDetectChangesEnabled = false;
        }

        public void Seed()
        {
            var london = new TCity15204 { Name = "London" };
            var sam = new TBuilder15204 { Name = "Sam", City = london };

            MandatorSet.Add(
                new TMandator15204
                {
                    Identity = Guid.NewGuid(),
                    Name = "One",
                    Buildings = new List<TBuilding15204>
                    {
                        new()
                        {
                            Identity = Guid.NewGuid(),
                            LongName = "One L1",
                            Builder = sam
                        },
                        new()
                        {
                            Identity = Guid.NewGuid(),
                            LongName = "One L2",
                            Builder = sam
                        }
                    }
                });
            MandatorSet.Add(
                new TMandator15204
                {
                    Identity = Guid.NewGuid(),
                    Name = "Two",
                    Buildings = new List<TBuilding15204>
                    {
                        new()
                        {
                            Identity = Guid.NewGuid(),
                            LongName = "Two L1",
                            Builder = new TBuilder15204 { Name = "John", City = london }
                        },
                        new()
                        {
                            Identity = Guid.NewGuid(),
                            LongName = "Two L2",
                            Builder = new TBuilder15204 { Name = "Mark", City = new TCity15204 { Name = "Leeds" } }
                        }
                    }
                });

            SaveChanges();
        }

        public class TBuilding15204
        {
            public int Id { get; set; }
            public Guid Identity { get; set; }
            public string LongName { get; set; }
            public int BuilderId { get; set; }
            public TBuilder15204 Builder { get; set; }
            public TMandator15204 Mandator { get; set; }
            public int MandatorId { get; set; }
        }

        public class TBuilder15204
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int CityId { get; set; }
            public TCity15204 City { get; set; }
        }

        public class TCity15204
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class TMandator15204
        {
            public int Id { get; set; }
            public Guid Identity { get; set; }
            public string Name { get; set; }
            public virtual ICollection<TBuilding15204> Buildings { get; set; }
        }
    }

    #endregion

    #region Issue15518

    [ConditionalTheory]
    [InlineData(false)]
    [InlineData(true)]
    public virtual async Task Nested_queries_does_not_cause_concurrency_exception_sync(bool tracking)
    {
        var contextFactory = await InitializeAsync<MyContext15518>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Repos.OrderBy(r => r.Id).Where(r => r.Id > 0);
            query = tracking ? query.AsTracking() : query.AsNoTracking();

            foreach (var a in query)
            {
                foreach (var b in query)
                {
                }
            }
        }

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Repos.OrderBy(r => r.Id).Where(r => r.Id > 0);
            query = tracking ? query.AsTracking() : query.AsNoTracking();

            await foreach (var a in query.AsAsyncEnumerable())
            {
                await foreach (var b in query.AsAsyncEnumerable())
                {
                }
            }
        }
    }

    protected class MyContext15518 : DbContext
    {
        public DbSet<Repo15518> Repos { get; set; }

        public MyContext15518(DbContextOptions options)
            : base(options)
        {
        }

        public void Seed()
        {
            AddRange(
                new Repo15518 { Name = "London" },
                new Repo15518 { Name = "New York" });

            SaveChanges();
        }

        public class Repo15518
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }

    #endregion

    #region Issue8864

    [ConditionalFact]
    public virtual async Task Select_nested_projection()
    {
        var contextFactory = await InitializeAsync<MyContext8864>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var customers = context.Customers
                .Select(c => new { Customer = c, CustomerAgain = MyContext8864.Get(context, c.Id) })
                .ToList();

            Assert.Equal(2, customers.Count);

            foreach (var customer in customers)
            {
                Assert.Same(customer.Customer, customer.CustomerAgain);
            }
        }
    }

    protected class MyContext8864 : DbContext
    {
        public DbSet<Customer8864> Customers { get; set; }

        public MyContext8864(DbContextOptions options)
            : base(options)
        {
        }

        public void Seed()
        {
            AddRange(
                new Customer8864 { Name = "Alan" },
                new Customer8864 { Name = "Elon" });

            SaveChanges();
        }

        public static Customer8864 Get(MyContext8864 context, int id)
            => context.Customers.Single(c => c.Id == id);

        public class Customer8864
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }

    #endregion

    #region Issue7983

    [ConditionalFact]
    public virtual async Task New_instances_in_projection_are_not_shared_across_results()
    {
        var contextFactory = await InitializeAsync<MyContext7983>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var list = context.Posts.Select(p => new MyContext7983.PostDTO7983().From(p)).ToList();

            Assert.Equal(3, list.Count);
            Assert.Equal(new[] { "First", "Second", "Third" }, list.Select(dto => dto.Title));

            AssertSql(
                """
SELECT [p].[Id], [p].[BlogId], [p].[Title]
FROM [Posts] AS [p]
""");
        }
    }

    protected class MyContext7983 : DbContext
    {
        public DbSet<Blog7983> Blogs { get; set; }
        public DbSet<Post7983> Posts { get; set; }

        public MyContext7983(DbContextOptions options)
            : base(options)
        {
        }

        public void Seed()
        {
            Add(
                new Blog7983
                {
                    Posts = new List<Post7983>
                    {
                        new() { Title = "First" },
                        new() { Title = "Second" },
                        new() { Title = "Third" }
                    }
                });

            SaveChanges();
        }

        public class Blog7983
        {
            public int Id { get; set; }
            public string Title { get; set; }

            public ICollection<Post7983> Posts { get; set; }
        }

        public class Post7983
        {
            public int Id { get; set; }
            public string Title { get; set; }

            public int? BlogId { get; set; }
            public Blog7983 Blog { get; set; }
        }

        public class PostDTO7983
        {
            public string Title { get; set; }

            public PostDTO7983 From(Post7983 post)
            {
                Title = post.Title;
                return this;
            }
        }
    }

    #endregion

    #region Issue17276_17099_16759

    [ConditionalFact]
    public virtual async Task Expression_tree_constructed_via_interface_works_17276()
    {
        var contextFactory = await InitializeAsync<MyContext17276>();

        using (var context = contextFactory.CreateContext())
        {
            var query = MyContext17276.List17276(context.RemovableEntities);

            AssertSql(
                """
SELECT [r].[Id], [r].[IsRemoved], [r].[Removed], [r].[RemovedByUser], [r].[OwnedEntity_Exists], [r].[OwnedEntity_OwnedValue]
FROM [RemovableEntities] AS [r]
WHERE [r].[IsRemoved] = CAST(0 AS bit)
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            var query = context.Parents
                .Where(p => EF.Property<bool>(EF.Property<MyContext17276.IRemovable17276>(p, "RemovableEntity"), "IsRemoved"))
                .ToList();

            AssertSql(
                """
SELECT [p].[Id], [p].[RemovableEntityId]
FROM [Parents] AS [p]
LEFT JOIN [RemovableEntities] AS [r] ON [p].[RemovableEntityId] = [r].[Id]
WHERE [r].[IsRemoved] = CAST(1 AS bit)
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            var query = context.RemovableEntities
                .Where(p => EF.Property<string>(EF.Property<MyContext17276.IOwned>(p, "OwnedEntity"), "OwnedValue") == "Abc")
                .ToList();

            AssertSql(
                """
SELECT [r].[Id], [r].[IsRemoved], [r].[Removed], [r].[RemovedByUser], [r].[OwnedEntity_Exists], [r].[OwnedEntity_OwnedValue]
FROM [RemovableEntities] AS [r]
WHERE [r].[OwnedEntity_OwnedValue] = N'Abc'
""");
        }

        // #16759
        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            var specification = new MyContext17276.Specification17276<MyContext17276.Parent17276>(1);
            var entities = context.Set<MyContext17276.Parent17276>().Where(specification.Criteria).ToList();

            AssertSql(
                """
@__id_0='1'

SELECT [p].[Id], [p].[RemovableEntityId]
FROM [Parents] AS [p]
WHERE [p].[Id] = @__id_0
""");
        }
    }

    protected class MyContext17276 : DbContext
    {
        public DbSet<RemovableEntity17276> RemovableEntities { get; set; }
        public DbSet<Parent17276> Parents { get; set; }

        public MyContext17276(DbContextOptions options)
            : base(options)
        {
        }

        public static List<T> List17276<T>(IQueryable<T> query)
            where T : IRemovable17276
            => query.Where(x => !x.IsRemoved).ToList();

        public interface IRemovable17276
        {
            bool IsRemoved { get; set; }

            string RemovedByUser { get; set; }

            DateTime? Removed { get; set; }
        }

        public class RemovableEntity17276 : IRemovable17276
        {
            public int Id { get; set; }
            public bool IsRemoved { get; set; }
            public string RemovedByUser { get; set; }
            public DateTime? Removed { get; set; }
            public OwnedEntity OwnedEntity { get; set; }
        }

        public class Parent17276 : IHasId17276<int>
        {
            public int Id { get; set; }
            public RemovableEntity17276 RemovableEntity { get; set; }
        }

        [Owned]
        public class OwnedEntity : IOwned
        {
            public string OwnedValue { get; set; }
            public int Exists { get; set; }
        }

        public interface IHasId17276<out T>
        {
            T Id { get; }
        }

        public interface IOwned
        {
            string OwnedValue { get; }
            int Exists { get; }
        }

        public class Specification17276<T>
            where T : IHasId17276<int>
        {
            public Expression<Func<T, bool>> Criteria { get; }

            public Specification17276(int id)
            {
                Criteria = t => t.Id == id;
            }
        }
    }

    #endregion

    #region Issue6864

    [ConditionalFact]
    public virtual async Task Implicit_cast_6864()
    {
        var contextFactory = await InitializeAsync<MyContext6864>();

        using (var context = contextFactory.CreateContext())
        {
            // Verify no client eval
            var result = context.Foos.Where(f => f.String == new MyContext6864.Bar6864(1337)).ToList();

            Assert.Empty(result);

            AssertSql(
                """
SELECT [f].[Id], [f].[String]
FROM [Foos] AS [f]
WHERE [f].[String] = N'1337'
""");
        }

        //Access_property_of_closure
        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            // Verify no client eval
            var bar = new MyContext6864.Bar6864(1337);
            var result = context.Foos.Where(f => f.String == bar.Value).ToList();

            Assert.Empty(result);

            AssertSql(
                """
@__bar_Value_0='1337' (Size = 4000)

SELECT [f].[Id], [f].[String]
FROM [Foos] AS [f]
WHERE [f].[String] = @__bar_Value_0
""");
        }

        //Implicitly_cast_closure
        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            // Verify no client eval
            var bar = new MyContext6864.Bar6864(1337);
            var result = context.Foos.Where(f => f.String == bar.ToString()).ToList();

            Assert.Empty(result);

            AssertSql(
                """
@__ToString_0='1337' (Size = 4000)

SELECT [f].[Id], [f].[String]
FROM [Foos] AS [f]
WHERE [f].[String] = @__ToString_0
""");
        }

        //Implicitly_cast_closure
        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            // Verify no client eval
            var bar = new MyContext6864.Bar6864(1337);
            var result = context.Foos.Where(f => f.String == bar).ToList();

            Assert.Empty(result);

            AssertSql(
                """
@__p_0='1337' (Size = 4000)

SELECT [f].[Id], [f].[String]
FROM [Foos] AS [f]
WHERE [f].[String] = @__p_0
""");
        }

        // Implicitly_cast_return_value
        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            // Verify no client eval
            var result = context.Foos.Where(f => f.String == new MyContext6864.Bar6864(1337).Clone()).ToList();

            Assert.Empty(result);

            AssertSql(
                """
SELECT [f].[Id], [f].[String]
FROM [Foos] AS [f]
WHERE [f].[String] = N'1337'
""");
        }
    }

    private class MyContext6864 : DbContext
    {
        public DbSet<FooEntity6864> Foos { get; set; }

        public MyContext6864(DbContextOptions options)
            : base(options)
        {
        }

        public class FooEntity6864
        {
            public int Id { get; set; }
            public string String { get; set; }
        }

        public class Bar6864
        {
            private readonly int _value;

            public Bar6864(int value)
            {
                _value = value;
            }

            public string Value
                => _value.ToString();

            public override string ToString()
                => Value;

            public static implicit operator string(Bar6864 bar)
                => bar.Value;

            public Bar6864 Clone()
                => new(_value);
        }
    }

    #endregion

    #region Issue9582

    [ConditionalFact]
    public virtual async Task Setting_IsUnicode_generates_unicode_literal_in_SQL()
    {
        var contextFactory = await InitializeAsync<MyContext9582>();

        using (var context = contextFactory.CreateContext())
        {
            // Verify SQL
            var query = context.Set<MyContext9582.TipoServicio9582>().Where(xx => xx.Nombre.Contains("lla")).ToList();

            AssertSql(
                """
SELECT [t].[Id], [t].[Nombre]
FROM [TipoServicio9582] AS [t]
WHERE [t].[Nombre] LIKE '%lla%'
""");
        }
    }

    protected class MyContext9582 : DbContext
    {
        public MyContext9582(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TipoServicio9582>(
                builder =>
                {
                    builder.HasKey(ts => ts.Id);

                    builder.Property(ts => ts.Id).IsRequired();
                    builder.Property(ts => ts.Nombre).IsRequired().HasMaxLength(20);
                });

            foreach (var property in modelBuilder.Model.GetEntityTypes()
                         .SelectMany(e => e.GetProperties().Where(p => p.ClrType == typeof(string))))
            {
                property.SetIsUnicode(false);
            }
        }

        public class TipoServicio9582
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
        }
    }

    #endregion

    #region Issue7222

    [ConditionalFact]
    public virtual async Task Inlined_dbcontext_is_not_leaking()
    {
        var contextFactory = await InitializeAsync<MyContext7222>();

        using (var context = contextFactory.CreateContext())
        {
            var entities = context.Blogs.Select(b => context.ClientMethod(b)).ToList();

            AssertSql(
                """
SELECT [b].[Id]
FROM [Blogs] AS [b]
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            Assert.Throws<InvalidOperationException>(() => context.RunQuery());
        }
    }

    protected class MyContext7222 : DbContext
    {
        public DbSet<Blog7222> Blogs { get; set; }

        public MyContext7222(DbContextOptions options)
            : base(options)
        {
        }

        public void RunQuery()
            => Blogs.Select(b => ClientMethod(b)).ToList();

        public int ClientMethod(Blog7222 blog)
            => blog.Id;

        public class Blog7222
        {
            public int Id { get; set; }
        }
    }

    #endregion

    #region Issue11023

    [ConditionalFact]
    public virtual async Task Async_correlated_projection_with_first()
    {
        var contextFactory = await InitializeAsync<MyContext11023>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var query = await context.Entities
                .Select(e => new { ThingIds = e.Values.First().Things.Select(t => t.Subthing.ThingId).ToList() })
                .ToListAsync();

            var result = Assert.Single(query);
            Assert.Equal(new[] { 1, 2 }, result.ThingIds);

            AssertSql(
                """
SELECT [e].[Id], [t0].[ThingId], [t0].[Id], [t0].[Id0]
FROM [Entities] AS [e]
OUTER APPLY (
    SELECT [s].[ThingId], [t].[Id], [s].[Id] AS [Id0]
    FROM [Things] AS [t]
    LEFT JOIN [Subthings] AS [s] ON [t].[Id] = [s].[ThingId]
    WHERE (
        SELECT TOP(1) [v].[Id]
        FROM [Values] AS [v]
        WHERE [e].[Id] = [v].[Entity11023Id]) IS NOT NULL AND ((
        SELECT TOP(1) [v0].[Id]
        FROM [Values] AS [v0]
        WHERE [e].[Id] = [v0].[Entity11023Id]) = [t].[Value11023Id] OR ((
        SELECT TOP(1) [v0].[Id]
        FROM [Values] AS [v0]
        WHERE [e].[Id] = [v0].[Entity11023Id]) IS NULL AND [t].[Value11023Id] IS NULL))
) AS [t0]
ORDER BY [e].[Id], [t0].[Id]
""");
        }
    }

    protected class MyContext11023 : DbContext
    {
        public DbSet<Entity11023> Entities { get; set; }
        public DbSet<Value11023> Values { get; set; }
        public DbSet<Thing11023> Things { get; set; }
        public DbSet<Subthing11023> Subthings { get; set; }

        public MyContext11023(DbContextOptions options)
            : base(options)
        {
        }

        public void Seed()
        {
            Add(
                new Entity11023
                {
                    Values = new List<Value11023>
                    {
                        new()
                        {
                            Things = new List<Thing11023>
                            {
                                new() { Subthing = new Subthing11023() }, new() { Subthing = new Subthing11023() }
                            }
                        }
                    }
                });

            SaveChanges();
        }

        public class Entity11023
        {
            public int Id { get; set; }
            public ICollection<Value11023> Values { get; set; }
        }

        public class Value11023
        {
            public int Id { get; set; }
            public ICollection<Thing11023> Things { get; set; }
        }

        public class Thing11023
        {
            public int Id { get; set; }
            public Subthing11023 Subthing { get; set; }
        }

        public class Subthing11023
        {
            public int Id { get; set; }
            public int ThingId { get; set; }
            public Thing11023 Thing { get; set; }
        }
    }

    #endregion

    #region Issue7973

    [ConditionalFact]
    public virtual async Task SelectMany_with_collection_selector_having_subquery()
    {
        var contextFactory = await InitializeAsync<MyContext7973>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var users = (from user in context.Users
                         from organisation in context.Organisations.Where(o => o.OrganisationUsers.Any()).DefaultIfEmpty()
                         select new { UserId = user.Id, OrgId = organisation.Id }).ToList();

            Assert.Equal(2, users.Count);

            AssertSql(
                """
SELECT [u].[Id] AS [UserId], [t0].[Id] AS [OrgId]
FROM [Users] AS [u]
CROSS JOIN (
    SELECT [t].[Id]
    FROM (
        SELECT NULL AS [empty]
    ) AS [e]
    LEFT JOIN (
        SELECT [o].[Id]
        FROM [Organisations] AS [o]
        WHERE EXISTS (
            SELECT 1
            FROM [OrganisationUser7973] AS [o0]
            WHERE [o].[Id] = [o0].[OrganisationId])
    ) AS [t] ON 1 = 1
) AS [t0]
""");
        }
    }

    protected class MyContext7973 : DbContext
    {
        public DbSet<User7973> Users { get; set; }
        public DbSet<Organisation7973> Organisations { get; set; }

        public MyContext7973(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrganisationUser7973>().HasKey(ou => new { ou.OrganisationId, ou.UserId });
            modelBuilder.Entity<OrganisationUser7973>().HasOne(ou => ou.Organisation).WithMany(o => o.OrganisationUsers)
                .HasForeignKey(ou => ou.OrganisationId);
            modelBuilder.Entity<OrganisationUser7973>().HasOne(ou => ou.User).WithMany(u => u.OrganisationUsers)
                .HasForeignKey(ou => ou.UserId);
        }

        public void Seed()
        {
            AddRange(
                new OrganisationUser7973 { Organisation = new Organisation7973(), User = new User7973() },
                new Organisation7973(),
                new User7973());

            SaveChanges();
        }

        public class User7973
        {
            public int Id { get; set; }
            public List<OrganisationUser7973> OrganisationUsers { get; set; }
        }

        public class Organisation7973
        {
            public int Id { get; set; }
            public List<OrganisationUser7973> OrganisationUsers { get; set; }
        }

        public class OrganisationUser7973
        {
            public int OrganisationId { get; set; }
            public Organisation7973 Organisation { get; set; }

            public int UserId { get; set; }
            public User7973 User { get; set; }
        }
    }

    #endregion

    #region Issue10447

    [ConditionalFact]
    public virtual async Task Nested_include_queries_do_not_populate_navigation_twice()
    {
        var contextFactory = await InitializeAsync<MyContext10447>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Blogs.Include(b => b.Posts);

            foreach (var blog in query)
            {
                query.ToList();
            }

            Assert.Collection(
                query,
                b => Assert.Equal(3, b.Posts.Count),
                b => Assert.Equal(2, b.Posts.Count),
                b => Assert.Single(b.Posts));
        }
    }

    protected class MyContext10447 : DbContext
    {
        public DbSet<Blog10447> Blogs { get; set; }

        public MyContext10447(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        public void Seed()
        {
            AddRange(
                new Blog10447
                {
                    Posts = new List<Post10447>
                    {
                        new(),
                        new(),
                        new()
                    }
                },
                new Blog10447 { Posts = new List<Post10447> { new(), new() } },
                new Blog10447 { Posts = new List<Post10447> { new() } });

            SaveChanges();
        }

        public class Blog10447
        {
            public int Id { get; set; }
            public List<Post10447> Posts { get; set; }
        }

        public class Post10447
        {
            public int Id { get; set; }

            public Blog10447 Blog { get; set; }
        }
    }

    #endregion

    #region Issue12456

    [ConditionalFact]
    public virtual async Task Let_multiple_references_with_reference_to_outer()
    {
        var contextFactory = await InitializeAsync<MyContext12456>();

        using (var context = contextFactory.CreateContext())
        {
            var users = (from a in context.Activities
                         let cs = context.CompetitionSeasons
                             .First(s => s.StartDate <= a.DateTime && a.DateTime < s.EndDate)
                         select new { cs.Id, Points = a.ActivityType.Points.Where(p => p.CompetitionSeason == cs) }).ToList();

            AssertSql(
                """
SELECT (
    SELECT TOP(1) [c].[Id]
    FROM [CompetitionSeasons] AS [c]
    WHERE [c].[StartDate] <= [a].[DateTime] AND [a].[DateTime] < [c].[EndDate]), [a].[Id], [a0].[Id], [t].[Id], [t].[ActivityTypeId], [t].[CompetitionSeasonId], [t].[Points], [t].[Id0]
FROM [Activities] AS [a]
INNER JOIN [ActivityType12456] AS [a0] ON [a].[ActivityTypeId] = [a0].[Id]
OUTER APPLY (
    SELECT [a1].[Id], [a1].[ActivityTypeId], [a1].[CompetitionSeasonId], [a1].[Points], [c0].[Id] AS [Id0]
    FROM [ActivityTypePoints12456] AS [a1]
    INNER JOIN [CompetitionSeasons] AS [c0] ON [a1].[CompetitionSeasonId] = [c0].[Id]
    WHERE [a0].[Id] = [a1].[ActivityTypeId] AND [c0].[Id] = (
        SELECT TOP(1) [c1].[Id]
        FROM [CompetitionSeasons] AS [c1]
        WHERE [c1].[StartDate] <= [a].[DateTime] AND [a].[DateTime] < [c1].[EndDate])
) AS [t]
ORDER BY [a].[Id], [a0].[Id], [t].[Id]
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            var users = context.Activities
                .Select(
                    a => new
                    {
                        Activity = a,
                        CompetitionSeason = context.CompetitionSeasons
                            .First(s => s.StartDate <= a.DateTime && a.DateTime < s.EndDate)
                    })
                .Select(
                    a => new
                    {
                        a.Activity,
                        CompetitionSeasonId = a.CompetitionSeason.Id,
                        Points = a.Activity.Points
                            ?? a.Activity.ActivityType.Points
                                .Where(p => p.CompetitionSeason == a.CompetitionSeason)
                                .Select(p => p.Points).SingleOrDefault()
                    }).ToList();

            AssertSql(
                """
SELECT [a].[Id], [a].[ActivityTypeId], [a].[DateTime], [a].[Points], (
    SELECT TOP(1) [c].[Id]
    FROM [CompetitionSeasons] AS [c]
    WHERE [c].[StartDate] <= [a].[DateTime] AND [a].[DateTime] < [c].[EndDate]) AS [CompetitionSeasonId], COALESCE([a].[Points], (
    SELECT TOP(1) [a1].[Points]
    FROM [ActivityTypePoints12456] AS [a1]
    INNER JOIN [CompetitionSeasons] AS [c0] ON [a1].[CompetitionSeasonId] = [c0].[Id]
    WHERE [a0].[Id] = [a1].[ActivityTypeId] AND [c0].[Id] = (
        SELECT TOP(1) [c1].[Id]
        FROM [CompetitionSeasons] AS [c1]
        WHERE [c1].[StartDate] <= [a].[DateTime] AND [a].[DateTime] < [c1].[EndDate])), 0) AS [Points]
FROM [Activities] AS [a]
INNER JOIN [ActivityType12456] AS [a0] ON [a].[ActivityTypeId] = [a0].[Id]
""");
        }
    }

    private class MyContext12456 : DbContext
    {
        public DbSet<Activity12456> Activities { get; set; }
        public DbSet<CompetitionSeason12456> CompetitionSeasons { get; set; }

        public MyContext12456(DbContextOptions options)
            : base(options)
        {
        }

        public class CompetitionSeason12456
        {
            public int Id { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public List<ActivityTypePoints12456> ActivityTypePoints { get; set; }
        }

        public class Point12456
        {
            public int Id { get; set; }
            public CompetitionSeason12456 CompetitionSeason { get; set; }
            public int? Points { get; set; }
        }

        public class ActivityType12456
        {
            public int Id { get; set; }
            public List<ActivityTypePoints12456> Points { get; set; }
        }

        public class ActivityTypePoints12456
        {
            public int Id { get; set; }
            public int ActivityTypeId { get; set; }
            public int CompetitionSeasonId { get; set; }
            public int Points { get; set; }

            public ActivityType12456 ActivityType { get; set; }
            public CompetitionSeason12456 CompetitionSeason { get; set; }
        }

        public class Activity12456
        {
            public int Id { get; set; }
            public int ActivityTypeId { get; set; }
            public DateTime DateTime { get; set; }
            public int? Points { get; set; }
            public ActivityType12456 ActivityType { get; set; }
        }
    }

    #endregion

    #region Issue15137

    [ConditionalFact]
    public virtual async Task Max_in_multi_level_nested_subquery()
    {
        var contextFactory = await InitializeAsync<MyContext15137>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var container = await context.Trades
                .Select(
                    x => new
                    {
                        x.Id,
                        Assets = x.Assets.AsQueryable()
                            .Select(
                                y => new
                                {
                                    y.Id,
                                    Contract = new
                                    {
                                        y.Contract.Id,
                                        Season = new
                                        {
                                            y.Contract.Season.Id,
                                            IsPastTradeDeadline =
                                                (y.Contract.Season.Games.Max(z => (int?)z.GameNumber) ?? 0) > 10
                                        }
                                    }
                                })
                            .ToList()
                    })
                .SingleAsync();

            AssertSql(
                """
SELECT [t0].[Id], [t1].[Id], [t1].[Id0], [t1].[Id1], [t1].[IsPastTradeDeadline]
FROM (
    SELECT TOP(2) [t].[Id]
    FROM [Trades] AS [t]
) AS [t0]
LEFT JOIN (
    SELECT [d].[Id], [d0].[Id] AS [Id0], [d1].[Id] AS [Id1], CASE
        WHEN COALESCE((
            SELECT MAX([d2].[GameNumber])
            FROM [DbGame] AS [d2]
            WHERE [d1].[Id] IS NOT NULL AND [d1].[Id] = [d2].[SeasonId]), 0) > 10 THEN CAST(1 AS bit)
        ELSE CAST(0 AS bit)
    END AS [IsPastTradeDeadline], [d].[DbTradeId]
    FROM [DbTradeAsset] AS [d]
    INNER JOIN [DbContract] AS [d0] ON [d].[ContractId] = [d0].[Id]
    LEFT JOIN [DbSeason] AS [d1] ON [d0].[SeasonId] = [d1].[Id]
) AS [t1] ON [t0].[Id] = [t1].[DbTradeId]
ORDER BY [t0].[Id], [t1].[Id], [t1].[Id0]
""");
        }
    }

    protected class MyContext15137 : DbContext
    {
        public DbSet<DbTrade> Trades { get; set; }

        public MyContext15137(DbContextOptions options)
            : base(options)
        {
        }

        public void Seed()
        {
            var dbTrade = new DbTrade
            {
                Assets = new List<DbTradeAsset>
                {
                    new()
                    {
                        Contract = new DbContract
                        {
                            Season = new DbSeason { Games = new List<DbGame> { new() { GameNumber = 1 } } }
                        }
                    }
                }
            };

            Trades.Add(dbTrade);
            SaveChanges();
        }

        public class DbTrade
        {
            public int Id { get; set; }
            public List<DbTradeAsset> Assets { get; set; }
        }

        public class DbTradeAsset
        {
            public int Id { get; set; }
            public int ContractId { get; set; }

            public DbContract Contract { get; set; }
        }

        public class DbContract
        {
            public int Id { get; set; }

            public DbSeason Season { get; set; }
        }

        public class DbSeason
        {
            public int Id { get; set; }

            public List<DbGame> Games { get; set; }
        }

        public class DbGame
        {
            public int Id { get; set; }
            public int GameNumber { get; set; }

            public DbSeason Season { get; set; }
        }
    }

    #endregion

    #region Issue17794

    [ConditionalFact]
    public async Task Double_convert_interface_created_expression_tree()
    {
        var contextFactory = await InitializeAsync<IssueContext17794>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var expression =
                IssueContext17794.HasAction17794<IssueContext17794.Offer17794>(IssueContext17794.OfferActions17794.Accepted);
            var query = context.Offers.Where(expression).Count();

            Assert.Equal(1, query);

            AssertSql(
                """
@__action_0='1'

SELECT COUNT(*)
FROM [Offers] AS [o]
WHERE EXISTS (
    SELECT 1
    FROM [OfferActions] AS [o0]
    WHERE [o].[Id] = [o0].[OfferId] AND [o0].[Action] = @__action_0)
""");
        }
    }

    protected class IssueContext17794 : DbContext
    {
        public DbSet<Offer17794> Offers { get; set; }
        public DbSet<OfferAction17794> OfferActions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        public IssueContext17794(DbContextOptions options)
            : base(options)
        {
        }

        public void Seed()
        {
            Add(
                new Offer17794 { Actions = new List<OfferAction17794> { new() { Action = OfferActions17794.Accepted } } });

            SaveChanges();
        }

        public static Expression<Func<T, bool>> HasAction17794<T>(OfferActions17794 action)
            where T : IOffer17794
        {
            Expression<Func<OfferAction17794, bool>> predicate = oa => oa.Action == action;

            return v => v.Actions.AsQueryable().Any(predicate);
        }

        public interface IOffer17794
        {
            ICollection<OfferAction17794> Actions { get; set; }
        }

        public class Offer17794 : IOffer17794
        {
            public int Id { get; set; }

            public ICollection<OfferAction17794> Actions { get; set; }
        }

        public enum OfferActions17794
        {
            Accepted = 1,
            Declined = 2
        }

        public class OfferAction17794
        {
            public int Id { get; set; }

            [Required]
            public Offer17794 Offer { get; set; }

            public int OfferId { get; set; }

            [Required]
            public OfferActions17794 Action { get; set; }
        }
    }

    #endregion

    #region Issue18087

    [ConditionalFact]
    public async Task Casts_are_removed_from_expression_tree_when_redundant()
    {
        var contextFactory = await InitializeAsync<IssueContext18087>(seed: c => c.Seed());

        // implemented_interface
        using (var context = contextFactory.CreateContext())
        {
            var queryBase = (IQueryable)context.MockEntities;
            var id = 1;
            var query = queryBase.Cast<IssueContext18087.IDomainEntity>().FirstOrDefault(x => x.Id == id);

            Assert.Equal(1, query.Id);

            AssertSql(
                """
@__id_0='1'

SELECT TOP(1) [m].[Id], [m].[Name], [m].[NavigationEntityId]
FROM [MockEntities] AS [m]
WHERE [m].[Id] = @__id_0
""");
        }

        // object
        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            var queryBase = (IQueryable)context.MockEntities;
            var query = queryBase.Cast<object>().Count();

            Assert.Equal(3, query);

            AssertSql(
                """
SELECT COUNT(*)
FROM [MockEntities] AS [m]
""");
        }

        // non_implemented_interface
        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            var queryBase = (IQueryable)context.MockEntities;
            var id = 1;

            var message = Assert.Throws<InvalidOperationException>(
                () => queryBase.Cast<IssueContext18087.IDummyEntity>().FirstOrDefault(x => x.Id == id)).Message;

            Assert.Equal(
                CoreStrings.TranslationFailed(
                    @"DbSet<MockEntity>()    .Cast<IDummyEntity>()    .Where(e => e.Id == __id_0)"),
                message.Replace("\r", "").Replace("\n", ""));
        }
    }

    protected class IssueContext18087 : DbContext
    {
        public IssueContext18087(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<MockEntity> MockEntities { get; set; }

        public void Seed()
        {
            AddRange(
                new MockEntity { Name = "Entity1", NavigationEntity = null },
                new MockEntity { Name = "Entity2", NavigationEntity = null },
                new MockEntity { Name = "NewEntity", NavigationEntity = null });

            SaveChanges();
        }

        public interface IDomainEntity
        {
            int Id { get; set; }
        }

        public interface IDummyEntity
        {
            int Id { get; set; }
        }

        public class MockEntity : IDomainEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public MockEntity NavigationEntity { get; set; }
        }
    }

    #endregion

    #region Issue20097

    [ConditionalFact]
    public async Task Interface_casting_though_generic_method()
    {
        var contextFactory = await InitializeAsync<IssueContext20097>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var originalQuery = context.Entities.Select(a => new IssueContext20097.MyModel20097 { Id = a.Id });
            var query = IssueContext20097.AddFilter(originalQuery, 1).ToList();

            Assert.Single(query);

            AssertSql(
                """
@__id_0='1'

SELECT [e].[Id]
FROM [Entities] AS [e]
WHERE [e].[Id] = @__id_0
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            var originalQuery = context.Entities.Select(a => new IssueContext20097.MyModel20097 { Id = a.Id });
            var query = originalQuery.Where<IssueContext20097.IHaveId20097>(a => a.Id == 1).ToList();

            Assert.Single(query);

            AssertSql(
                """
SELECT [e].[Id]
FROM [Entities] AS [e]
WHERE [e].[Id] = CAST(1 AS bigint)
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            var originalQuery = context.Entities.Select(a => new IssueContext20097.MyModel20097 { Id = a.Id });
            var query = originalQuery.Where(a => ((IssueContext20097.IHaveId20097)a).Id == 1).ToList();

            Assert.Single(query);

            AssertSql(
                """
SELECT [e].[Id]
FROM [Entities] AS [e]
WHERE [e].[Id] = CAST(1 AS bigint)
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            var originalQuery = context.Entities.Select(a => new IssueContext20097.MyModel20097 { Id = a.Id });
            var query = originalQuery.Where(a => (a as IssueContext20097.IHaveId20097).Id == 1).ToList();

            Assert.Single(query);

            AssertSql(
                """
SELECT [e].[Id]
FROM [Entities] AS [e]
WHERE [e].[Id] = CAST(1 AS bigint)
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            var originalQuery = context.Entities.Select(a => new IssueContext20097.MyModel20097 { Id = a.Id });
            var query = originalQuery.Where(a => ((IssueContext20097.IHaveId20097)a).Id == 1).ToList();
            Assert.Single(query);

            AssertSql(
                """
SELECT [e].[Id]
FROM [Entities] AS [e]
WHERE [e].[Id] = CAST(1 AS bigint)
""");
        }
    }

    protected class IssueContext20097 : DbContext
    {
        public IssueContext20097(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Entity20097> Entities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        public static IQueryable<T> AddFilter<T>(IQueryable<T> query, long id)
            where T : IHaveId20097
            => query.Where(a => a.Id == id);

        public void Seed()
        {
            Add(new Entity20097());

            SaveChanges();
        }

        public class Entity20097
        {
            public long Id { get; set; }
        }

        public interface IHaveId20097
        {
            long Id { get; }
        }

        public class MyModel20097 : IHaveId20097
        {
            public long Id { get; set; }
        }
    }

    #endregion

    #region Issue20609

    [ConditionalFact]
    public virtual async Task Can_ignore_invalid_include_path_error()
    {
        var contextFactory = await InitializeAsync<IssueContext20609>(
            onConfiguring: o => o.ConfigureWarnings(x => x.Ignore(CoreEventId.InvalidIncludePathError)));

        using var context = contextFactory.CreateContext();
        var result = context.Set<IssueContext20609.ClassA>().Include("SubB").ToList();
    }

    protected class IssueContext20609 : DbContext
    {
        public IssueContext20609(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<BaseClass> BaseClasses { get; set; }
        public DbSet<SubA> SubAs { get; set; }
        public DbSet<SubB> SubBs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClassA>().HasBaseType<BaseClass>().HasOne(x => x.SubA).WithMany();
            modelBuilder.Entity<ClassB>().HasBaseType<BaseClass>().HasOne(x => x.SubB).WithMany();
        }

        public class BaseClass
        {
            public string Id { get; set; }
        }

        public class ClassA : BaseClass
        {
            public SubA SubA { get; set; }
        }

        public class ClassB : BaseClass
        {
            public SubB SubB { get; set; }
        }

        public class SubA
        {
            public int Id { get; set; }
        }

        public class SubB
        {
            public int Id { get; set; }
        }
    }

    #endregion

    #region Issue21355

    [ConditionalFact]
    public virtual async Task Can_configure_SingleQuery_at_context_level()
    {
        var contextFactory = await InitializeAsync<IssueContext21355>(
            seed: c => c.Seed(),
            onConfiguring: o => new SqlServerDbContextOptionsBuilder(o).UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery));

        using (var context = contextFactory.CreateContext())
        {
            var result = context.Parents.Include(p => p.Children1).ToList();

            AssertSql(
                """
SELECT [p].[Id], [c].[Id], [c].[ParentId]
FROM [Parents] AS [p]
LEFT JOIN [Child21355] AS [c] ON [p].[Id] = [c].[ParentId]
ORDER BY [p].[Id]
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            var result = context.Parents.Include(p => p.Children1).AsSplitQuery().ToList();

            AssertSql(
                """
SELECT [p].[Id]
FROM [Parents] AS [p]
ORDER BY [p].[Id]
""",
                """
SELECT [c].[Id], [c].[ParentId], [p].[Id]
FROM [Parents] AS [p]
INNER JOIN [Child21355] AS [c] ON [p].[Id] = [c].[ParentId]
ORDER BY [p].[Id]
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            context.Parents.Include(p => p.Children1).Include(p => p.Children2).ToList();

            AssertSql(
                """
SELECT [p].[Id], [c].[Id], [c].[ParentId], [a].[Id], [a].[ParentId]
FROM [Parents] AS [p]
LEFT JOIN [Child21355] AS [c] ON [p].[Id] = [c].[ParentId]
LEFT JOIN [AnotherChild21355] AS [a] ON [p].[Id] = [a].[ParentId]
ORDER BY [p].[Id], [c].[Id]
""");
        }
    }

    [ConditionalFact]
    public virtual async Task Can_configure_SplitQuery_at_context_level()
    {
        var contextFactory = await InitializeAsync<IssueContext21355>(
            seed: c => c.Seed(),
            onConfiguring: o => new SqlServerDbContextOptionsBuilder(o).UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

        using (var context = contextFactory.CreateContext())
        {
            var result = context.Parents.Include(p => p.Children1).ToList();

            AssertSql(
                """
SELECT [p].[Id]
FROM [Parents] AS [p]
ORDER BY [p].[Id]
""",
                //
                """
SELECT [c].[Id], [c].[ParentId], [p].[Id]
FROM [Parents] AS [p]
INNER JOIN [Child21355] AS [c] ON [p].[Id] = [c].[ParentId]
ORDER BY [p].[Id]
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            var result = context.Parents.Include(p => p.Children1).AsSingleQuery().ToList();

            AssertSql(
                """
SELECT [p].[Id], [c].[Id], [c].[ParentId]
FROM [Parents] AS [p]
LEFT JOIN [Child21355] AS [c] ON [p].[Id] = [c].[ParentId]
ORDER BY [p].[Id]
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            context.Parents.Include(p => p.Children1).Include(p => p.Children2).ToList();

            AssertSql(
                """
SELECT [p].[Id]
FROM [Parents] AS [p]
ORDER BY [p].[Id]
""",
                """
SELECT [c].[Id], [c].[ParentId], [p].[Id]
FROM [Parents] AS [p]
INNER JOIN [Child21355] AS [c] ON [p].[Id] = [c].[ParentId]
ORDER BY [p].[Id]
""",
                """
SELECT [a].[Id], [a].[ParentId], [p].[Id]
FROM [Parents] AS [p]
INNER JOIN [AnotherChild21355] AS [a] ON [p].[Id] = [a].[ParentId]
ORDER BY [p].[Id]
""");
        }
    }

    [ConditionalFact]
    public virtual async Task Unconfigured_query_splitting_behavior_throws_a_warning()
    {
        var contextFactory = await InitializeAsync<IssueContext21355>(
            seed: c => c.Seed(), onConfiguring: o => ClearQuerySplittingBehavior(o));

        using (var context = contextFactory.CreateContext())
        {
            context.Parents.Include(p => p.Children1).Include(p => p.Children2).AsSplitQuery().ToList();

            AssertSql(
                """
SELECT [p].[Id]
FROM [Parents] AS [p]
ORDER BY [p].[Id]
""",
                //
                """
SELECT [c].[Id], [c].[ParentId], [p].[Id]
FROM [Parents] AS [p]
INNER JOIN [Child21355] AS [c] ON [p].[Id] = [c].[ParentId]
ORDER BY [p].[Id]
""",
                //
                """
SELECT [a].[Id], [a].[ParentId], [p].[Id]
FROM [Parents] AS [p]
INNER JOIN [AnotherChild21355] AS [a] ON [p].[Id] = [a].[ParentId]
ORDER BY [p].[Id]
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            Assert.Contains(
                RelationalResources.LogMultipleCollectionIncludeWarning(new TestLogger<TestRelationalLoggingDefinitions>())
                    .GenerateMessage(),
                Assert.Throws<InvalidOperationException>(
                    () => context.Parents.Include(p => p.Children1).Include(p => p.Children2).ToList()).Message);
        }
    }

    [ConditionalFact]
    public virtual async Task Using_AsSingleQuery_without_context_configuration_does_not_throw_warning()
    {
        var contextFactory = await InitializeAsync<IssueContext21355>(seed: c => c.Seed());

        using var context = contextFactory.CreateContext();

        context.Parents.Include(p => p.Children1).Include(p => p.Children2).AsSingleQuery().ToList();

        AssertSql(
            """
SELECT [p].[Id], [c].[Id], [c].[ParentId], [a].[Id], [a].[ParentId]
FROM [Parents] AS [p]
LEFT JOIN [Child21355] AS [c] ON [p].[Id] = [c].[ParentId]
LEFT JOIN [AnotherChild21355] AS [a] ON [p].[Id] = [a].[ParentId]
ORDER BY [p].[Id], [c].[Id]
""");
    }

    [ConditionalFact]
    public virtual async Task SplitQuery_disposes_inner_data_readers()
    {
        var contextFactory = await InitializeAsync<IssueContext21355>(seed: c => c.Seed());

        ((RelationalTestStore)contextFactory.TestStore).CloseConnection();

        using (var context = contextFactory.CreateContext())
        {
            context.Parents.Include(p => p.Children1).Include(p => p.Children2).AsSplitQuery().ToList();

            Assert.Equal(ConnectionState.Closed, context.Database.GetDbConnection().State);
        }

        using (var context = contextFactory.CreateContext())
        {
            await context.Parents.Include(p => p.Children1).Include(p => p.Children2).AsSplitQuery().ToListAsync();

            Assert.Equal(ConnectionState.Closed, context.Database.GetDbConnection().State);
        }

        using (var context = contextFactory.CreateContext())
        {
            context.Parents.Include(p => p.Children1).Include(p => p.Children2).OrderBy(e => e.Id).AsSplitQuery().Single();

            Assert.Equal(ConnectionState.Closed, context.Database.GetDbConnection().State);
        }

        using (var context = contextFactory.CreateContext())
        {
            await context.Parents.Include(p => p.Children1).Include(p => p.Children2).OrderBy(e => e.Id).AsSplitQuery().SingleAsync();

            Assert.Equal(ConnectionState.Closed, context.Database.GetDbConnection().State);
        }
    }

    [ConditionalFact]
    public virtual async Task Using_AsSplitQuery_without_multiple_active_result_sets_works()
    {
        var contextFactory = await InitializeAsync<IssueContext21355>(
            seed: c => c.Seed(),
            createTestStore: () => SqlServerTestStore.CreateInitialized(StoreName, multipleActiveResultSets: false));

        using var context = contextFactory.CreateContext();

        context.Parents.Include(p => p.Children1).Include(p => p.Children2).AsSplitQuery().ToList();
    }

    protected class IssueContext21355 : DbContext
    {
        public IssueContext21355(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Parent21355> Parents { get; set; }

        public void Seed()
        {
            Add(new Parent21355 { Id = "Parent1", Children1 = new List<Child21355> { new(), new() } });
            SaveChanges();
        }

        public class Parent21355
        {
            public string Id { get; set; }
            public List<Child21355> Children1 { get; set; }
            public List<AnotherChild21355> Children2 { get; set; }
        }

        public class Child21355
        {
            public int Id { get; set; }
            public string ParentId { get; set; }
            public Parent21355 Parent { get; set; }
        }

        public class AnotherChild21355
        {
            public int Id { get; set; }
            public string ParentId { get; set; }
            public Parent21355 Parent { get; set; }
        }
    }

    #endregion

    #region Issue18346

    [ConditionalFact]
    public virtual async Task Can_query_hierarchy_with_non_nullable_property_on_derived()
    {
        var contextFactory = await InitializeAsync<MyContext18346>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Businesses.ToList();
            Assert.Equal(3, query.Count);

            AssertSql(
                """
SELECT [b].[Id], [b].[Name], [b].[Type], [b].[IsOnline]
FROM [Businesses] AS [b]
""");
        }
    }

    protected class MyContext18346 : DbContext
    {
        public DbSet<Business18346> Businesses { get; set; }

        public MyContext18346(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Business18346>()
                .HasDiscriminator(x => x.Type)
                .HasValue<Shop18346>(BusinessType18346.Shop)
                .HasValue<Brand18346>(BusinessType18346.Brand);

        public void Seed()
        {
            var shop1 = new Shop18346 { IsOnline = true, Name = "Amzn" };
            var shop2 = new Shop18346 { IsOnline = false, Name = "Mom and Pop's Shoppe" };
            var brand = new Brand18346 { Name = "Tsla" };
            Businesses.AddRange(shop1, shop2, brand);
            SaveChanges();
        }

        public abstract class Business18346
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public BusinessType18346 Type { get; set; }
        }

        public class Shop18346 : Business18346
        {
            public bool IsOnline { get; set; }
        }

        public class Brand18346 : Business18346
        {
        }

        public enum BusinessType18346
        {
            Shop,
            Brand,
        }
    }

    #endregion

    #region Issue21666

    [ConditionalFact]
    public virtual async Task Thread_safety_in_relational_command_cache()
    {
        var contextFactory = await InitializeAsync<MyContext21666>(
            onConfiguring: options => ((IDbContextOptionsBuilderInfrastructure)options).AddOrUpdateExtension(
                options.Options.FindExtension<SqlServerOptionsExtension>()
                    .WithConnection(null)
                    .WithConnectionString(SqlServerTestStore.CreateConnectionString(StoreName))));

        var ids = new[] { 1, 2, 3 };

        Parallel.For(
            0, 100,
            i =>
            {
                using var context = contextFactory.CreateContext();
                var query = context.Lists.Where(l => !l.IsDeleted && ids.Contains(l.Id)).ToList();
            });
    }

    protected class MyContext21666 : DbContext
    {
        public DbSet<List21666> Lists { get; set; }

        public MyContext21666(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        public class List21666
        {
            public int Id { get; set; }
            public bool IsDeleted { get; set; }
        }
    }

    #endregion

    #region Issue21768

    [ConditionalFact]
    public virtual async Task Using_explicit_interface_implementation_as_navigation_works()
    {
        var contextFactory = await InitializeAsync<MyContext21768>();

        using (var context = contextFactory.CreateContext())
        {
            Expression<Func<MyContext21768.IBook21768, MyContext21768.BookViewModel21768>> projection =
                b => new MyContext21768.BookViewModel21768
                {
                    FirstPage = b.FrontCover.Illustrations.FirstOrDefault(
                            i => i.State >= MyContext21768.IllustrationState21768.Approved)
                        != null
                            ? new MyContext21768.PageViewModel21768
                            {
                                Uri = b.FrontCover.Illustrations
                                    .FirstOrDefault(i => i.State >= MyContext21768.IllustrationState21768.Approved).Uri
                            }
                            : null,
                };

            var result = context.Books.Where(b => b.Id == 1).Select(projection).SingleOrDefault();

            AssertSql(
                """
SELECT TOP(2) CASE
    WHEN EXISTS (
        SELECT 1
        FROM [CoverIllustrations] AS [c]
        WHERE [b0].[Id] = [c].[CoverId] AND [c].[State] >= 2) THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END, (
    SELECT TOP(1) [c0].[Uri]
    FROM [CoverIllustrations] AS [c0]
    WHERE [b0].[Id] = [c0].[CoverId] AND [c0].[State] >= 2)
FROM [Books] AS [b]
INNER JOIN [BookCovers] AS [b0] ON [b].[FrontCoverId] = [b0].[Id]
WHERE [b].[Id] = 1
""");
        }
    }

    protected class MyContext21768 : DbContext
    {
        public DbSet<Book21768> Books { get; set; }
        public DbSet<BookCover21768> BookCovers { get; set; }
        public DbSet<CoverIllustration21768> CoverIllustrations { get; set; }

        public MyContext21768(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                fk.DeleteBehavior = DeleteBehavior.NoAction;
            }
        }

        public class BookViewModel21768
        {
            public PageViewModel21768 FirstPage { get; set; }
        }

        public class PageViewModel21768
        {
            public string Uri { get; set; }
        }

        public interface IBook21768
        {
            public int Id { get; set; }

            public IBookCover21768 FrontCover { get; }
            public int FrontCoverId { get; set; }

            public IBookCover21768 BackCover { get; }
            public int BackCoverId { get; set; }
        }

        public interface IBookCover21768
        {
            public int Id { get; set; }
            public IEnumerable<ICoverIllustration21768> Illustrations { get; }
        }

        public interface ICoverIllustration21768
        {
            public int Id { get; set; }
            public IBookCover21768 Cover { get; }
            public int CoverId { get; set; }
            public string Uri { get; set; }
            public IllustrationState21768 State { get; set; }
        }

        public class Book21768 : IBook21768
        {
            public int Id { get; set; }

            public BookCover21768 FrontCover { get; set; }
            public int FrontCoverId { get; set; }

            public BookCover21768 BackCover { get; set; }
            public int BackCoverId { get; set; }

            IBookCover21768 IBook21768.FrontCover
                => FrontCover;

            IBookCover21768 IBook21768.BackCover
                => BackCover;
        }

        public class BookCover21768 : IBookCover21768
        {
            public int Id { get; set; }
            public ICollection<CoverIllustration21768> Illustrations { get; set; }

            IEnumerable<ICoverIllustration21768> IBookCover21768.Illustrations
                => Illustrations;
        }

        public class CoverIllustration21768 : ICoverIllustration21768
        {
            public int Id { get; set; }
            public BookCover21768 Cover { get; set; }
            public int CoverId { get; set; }
            public string Uri { get; set; }
            public IllustrationState21768 State { get; set; }

            IBookCover21768 ICoverIllustration21768.Cover
                => Cover;
        }

        public enum IllustrationState21768
        {
            New,
            PendingApproval,
            Approved,
            Printed
        }
    }

    #endregion

    #region Issue19206

    [ConditionalFact]
    public virtual async Task From_sql_expression_compares_correctly()
    {
        var contextFactory = await InitializeAsync<MyContext19206>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var query = from t1 in context.Tests.FromSqlInterpolated(
                            $"Select * from Tests Where Type = {MyContext19206.TestType19206.Unit}")
                        from t2 in context.Tests.FromSqlInterpolated(
                            $"Select * from Tests Where Type = {MyContext19206.TestType19206.Integration}")
                        select new { t1, t2 };

            var result = query.ToList();

            var item = Assert.Single(result);
            Assert.Equal(MyContext19206.TestType19206.Unit, item.t1.Type);
            Assert.Equal(MyContext19206.TestType19206.Integration, item.t2.Type);

            AssertSql(
                """
p0='0'
p1='1'

SELECT [m].[Id], [m].[Type], [m0].[Id], [m0].[Type]
FROM (
    Select * from Tests Where Type = @p0
) AS [m]
CROSS JOIN (
    Select * from Tests Where Type = @p1
) AS [m0]
""");
        }
    }

    protected class MyContext19206 : DbContext
    {
        public DbSet<Test19206> Tests { get; set; }

        public MyContext19206(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        public void Seed()
        {
            Add(new Test19206 { Type = TestType19206.Unit });
            Add(new Test19206 { Type = TestType19206.Integration });
            SaveChanges();
        }

        public class Test19206
        {
            public int Id { get; set; }
            public TestType19206 Type { get; set; }
        }

        public enum TestType19206
        {
            Unit,
            Integration,
        }
    }

    #endregion

    #region Issue21803

    [ConditionalTheory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public virtual async Task Select_enumerable_navigation_backed_by_collection(bool async, bool split)
    {
        var contextFactory = await InitializeAsync<MyContext21803>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<MyContext21803.AppEntity21803>().Select(appEntity => appEntity.OtherEntities);

            if (split)
            {
                query = query.AsSplitQuery();
            }

            if (async)
            {
                await query.ToListAsync();
            }
            else
            {
                query.ToList();
            }

            if (split)
            {
                AssertSql(
                    """
SELECT [e].[Id]
FROM [Entities] AS [e]
ORDER BY [e].[Id]
""",
                    //
                    """
SELECT [o].[Id], [o].[AppEntityId], [e].[Id]
FROM [Entities] AS [e]
INNER JOIN [OtherEntity21803] AS [o] ON [e].[Id] = [o].[AppEntityId]
ORDER BY [e].[Id]
""");
            }
            else
            {
                AssertSql(
                    """
SELECT [e].[Id], [o].[Id], [o].[AppEntityId]
FROM [Entities] AS [e]
LEFT JOIN [OtherEntity21803] AS [o] ON [e].[Id] = [o].[AppEntityId]
ORDER BY [e].[Id]
""");
            }
        }
    }

    protected class MyContext21803 : DbContext
    {
        public DbSet<AppEntity21803> Entities { get; set; }

        public MyContext21803(DbContextOptions options)
            : base(options)
        {
        }

        public void Seed()
        {
            var appEntity = new AppEntity21803();
            AddRange(
                new OtherEntity21803 { AppEntity = appEntity },
                new OtherEntity21803 { AppEntity = appEntity },
                new OtherEntity21803 { AppEntity = appEntity },
                new OtherEntity21803 { AppEntity = appEntity });

            SaveChanges();
        }

        public class AppEntity21803
        {
            private readonly List<OtherEntity21803> _otherEntities = new();

            public int Id { get; private set; }

            public IEnumerable<OtherEntity21803> OtherEntities
                => _otherEntities;
        }

        public class OtherEntity21803
        {
            public int Id { get; private set; }
            public AppEntity21803 AppEntity { get; set; }
        }
    }

    #endregion

    #region Issue15215

    [ConditionalFact]
    public virtual async Task Repeated_parameters_in_generated_query_sql()
    {
        var contextFactory = await InitializeAsync<MyContext15215>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var k = 1;
            var a = context.Autos.Where(e => e.Id == k).First();
            var b = context.Autos.Where(e => e.Id == k + 1).First();

            var equalQuery = (from d in context.EqualAutos
                              where (d.Auto == a && d.AnotherAuto == b)
                                  || (d.Auto == b && d.AnotherAuto == a)
                              select d).ToList();

            Assert.Single(equalQuery);

            AssertSql(
                """
@__k_0='1'

SELECT TOP(1) [a].[Id], [a].[Name]
FROM [Autos] AS [a]
WHERE [a].[Id] = @__k_0
""",
                //
                """
@__p_0='2'

SELECT TOP(1) [a].[Id], [a].[Name]
FROM [Autos] AS [a]
WHERE [a].[Id] = @__p_0
""",
                //
                """
@__entity_equality_a_0_Id='1' (Nullable = true)
@__entity_equality_b_1_Id='2' (Nullable = true)

SELECT [e].[Id], [e].[AnotherAutoId], [e].[AutoId]
FROM [EqualAutos] AS [e]
LEFT JOIN [Autos] AS [a] ON [e].[AutoId] = [a].[Id]
LEFT JOIN [Autos] AS [a0] ON [e].[AnotherAutoId] = [a0].[Id]
WHERE ([a].[Id] = @__entity_equality_a_0_Id AND [a0].[Id] = @__entity_equality_b_1_Id) OR ([a].[Id] = @__entity_equality_b_1_Id AND [a0].[Id] = @__entity_equality_a_0_Id)
""");
        }
    }

    protected class MyContext15215 : DbContext
    {
        public MyContext15215(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Auto15215> Autos { get; set; }
        public DbSet<EqualAuto15215> EqualAutos { get; set; }

        public void Seed()
        {
            for (var i = 0; i < 10; i++)
            {
                Add(new Auto15215 { Name = "Auto " + i });
            }

            SaveChanges();

            AddRange(
                new EqualAuto15215 { Auto = Autos.Find(1), AnotherAuto = Autos.Find(2) },
                new EqualAuto15215 { Auto = Autos.Find(5), AnotherAuto = Autos.Find(4) });

            SaveChanges();
        }

        public class Auto15215
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class EqualAuto15215
        {
            public int Id { get; set; }
            public Auto15215 Auto { get; set; }
            public Auto15215 AnotherAuto { get; set; }
        }
    }

    #endregion

    #region Issue22568

    [ConditionalFact]
    public virtual async Task Cycles_in_auto_include()
    {
        var contextFactory = await InitializeAsync<MyContext22568>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var principals = context.Set<MyContext22568.PrincipalOneToOne>().ToList();
            Assert.Single(principals);
            Assert.NotNull(principals[0].Dependent);
            Assert.NotNull(principals[0].Dependent.Principal);

            var dependents = context.Set<MyContext22568.DependentOneToOne>().ToList();
            Assert.Single(dependents);
            Assert.NotNull(dependents[0].Principal);
            Assert.NotNull(dependents[0].Principal.Dependent);

            AssertSql(
                """
SELECT [p].[Id], [d].[Id], [d].[PrincipalId]
FROM [PrincipalOneToOne] AS [p]
LEFT JOIN [DependentOneToOne] AS [d] ON [p].[Id] = [d].[PrincipalId]
""",
                //
                """
SELECT [d].[Id], [d].[PrincipalId], [p].[Id]
FROM [DependentOneToOne] AS [d]
INNER JOIN [PrincipalOneToOne] AS [p] ON [d].[PrincipalId] = [p].[Id]
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            var principals = context.Set<MyContext22568.PrincipalOneToMany>().ToList();
            Assert.Single(principals);
            Assert.NotNull(principals[0].Dependents);
            Assert.True(principals[0].Dependents.All(e => e.Principal != null));

            var dependents = context.Set<MyContext22568.DependentOneToMany>().ToList();
            Assert.Equal(2, dependents.Count);
            Assert.True(dependents.All(e => e.Principal != null));
            Assert.True(dependents.All(e => e.Principal.Dependents != null));
            Assert.True(dependents.All(e => e.Principal.Dependents.All(i => i.Principal != null)));

            AssertSql(
                """
SELECT [p].[Id], [d].[Id], [d].[PrincipalId]
FROM [PrincipalOneToMany] AS [p]
LEFT JOIN [DependentOneToMany] AS [d] ON [p].[Id] = [d].[PrincipalId]
ORDER BY [p].[Id]
""",
                //
                """
SELECT [d].[Id], [d].[PrincipalId], [p].[Id], [d0].[Id], [d0].[PrincipalId]
FROM [DependentOneToMany] AS [d]
INNER JOIN [PrincipalOneToMany] AS [p] ON [d].[PrincipalId] = [p].[Id]
LEFT JOIN [DependentOneToMany] AS [d0] ON [p].[Id] = [d0].[PrincipalId]
ORDER BY [d].[Id], [p].[Id]
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            Assert.Equal(
                CoreStrings.AutoIncludeNavigationCycle("'PrincipalManyToMany.Dependents', 'DependentManyToMany.Principals'"),
                Assert.Throws<InvalidOperationException>(() => context.Set<MyContext22568.PrincipalManyToMany>().ToList()).Message);

            Assert.Equal(
                CoreStrings.AutoIncludeNavigationCycle("'DependentManyToMany.Principals', 'PrincipalManyToMany.Dependents'"),
                Assert.Throws<InvalidOperationException>(() => context.Set<MyContext22568.DependentManyToMany>().ToList()).Message);

            context.Set<MyContext22568.PrincipalManyToMany>().IgnoreAutoIncludes().ToList();
            context.Set<MyContext22568.DependentManyToMany>().IgnoreAutoIncludes().ToList();

            AssertSql(
                """
SELECT [p].[Id]
FROM [PrincipalManyToMany] AS [p]
""",
                //
                """
SELECT [d].[Id]
FROM [DependentManyToMany] AS [d]
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            Assert.Equal(
                CoreStrings.AutoIncludeNavigationCycle("'CycleA.Bs', 'CycleB.C', 'CycleC.As'"),
                Assert.Throws<InvalidOperationException>(() => context.Set<MyContext22568.CycleA>().ToList()).Message);

            Assert.Equal(
                CoreStrings.AutoIncludeNavigationCycle("'CycleB.C', 'CycleC.As', 'CycleA.Bs'"),
                Assert.Throws<InvalidOperationException>(() => context.Set<MyContext22568.CycleB>().ToList()).Message);

            Assert.Equal(
                CoreStrings.AutoIncludeNavigationCycle("'CycleC.As', 'CycleA.Bs', 'CycleB.C'"),
                Assert.Throws<InvalidOperationException>(() => context.Set<MyContext22568.CycleC>().ToList()).Message);

            context.Set<MyContext22568.CycleA>().IgnoreAutoIncludes().ToList();
            context.Set<MyContext22568.CycleB>().IgnoreAutoIncludes().ToList();
            context.Set<MyContext22568.CycleC>().IgnoreAutoIncludes().ToList();

            AssertSql(
                """
SELECT [c].[Id], [c].[CycleCId]
FROM [CycleA] AS [c]
""",
                //
                """
SELECT [c].[Id], [c].[CId], [c].[CycleAId]
FROM [CycleB] AS [c]
""",
                //
                """
SELECT [c].[Id], [c].[BId]
FROM [CycleC] AS [c]
""");
        }
    }

    protected class MyContext22568 : DbContext
    {
        public MyContext22568(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PrincipalOneToOne>().Navigation(e => e.Dependent).AutoInclude();
            modelBuilder.Entity<DependentOneToOne>().Navigation(e => e.Principal).AutoInclude();
            modelBuilder.Entity<PrincipalOneToMany>().Navigation(e => e.Dependents).AutoInclude();
            modelBuilder.Entity<DependentOneToMany>().Navigation(e => e.Principal).AutoInclude();
            modelBuilder.Entity<PrincipalManyToMany>().Navigation(e => e.Dependents).AutoInclude();
            modelBuilder.Entity<DependentManyToMany>().Navigation(e => e.Principals).AutoInclude();

            modelBuilder.Entity<CycleA>().Navigation(e => e.Bs).AutoInclude();
            modelBuilder.Entity<CycleB>().Navigation(e => e.C).AutoInclude();
            modelBuilder.Entity<CycleC>().Navigation(e => e.As).AutoInclude();
        }

        public void Seed()
        {
            Add(new PrincipalOneToOne { Dependent = new DependentOneToOne() });
            Add(
                new PrincipalOneToMany
                {
                    Dependents = new List<DependentOneToMany>
                    {
                        new(), new(),
                    }
                });

            SaveChanges();
        }

        public class PrincipalOneToOne
        {
            public int Id { get; set; }
            public DependentOneToOne Dependent { get; set; }
        }

        public class DependentOneToOne
        {
            public int Id { get; set; }

            [ForeignKey("Principal")]
            public int PrincipalId { get; set; }

            public PrincipalOneToOne Principal { get; set; }
        }

        public class PrincipalOneToMany
        {
            public int Id { get; set; }
            public List<DependentOneToMany> Dependents { get; set; }
        }

        public class DependentOneToMany
        {
            public int Id { get; set; }

            [ForeignKey("Principal")]
            public int PrincipalId { get; set; }

            public PrincipalOneToMany Principal { get; set; }
        }

        public class PrincipalManyToMany
        {
            public int Id { get; set; }
            public List<DependentManyToMany> Dependents { get; set; }
        }

        public class DependentManyToMany
        {
            public int Id { get; set; }
            public List<PrincipalManyToMany> Principals { get; set; }
        }

        public class CycleA
        {
            public int Id { get; set; }
            public List<CycleB> Bs { get; set; }
        }

        public class CycleB
        {
            public int Id { get; set; }
            public CycleC C { get; set; }
        }

        public class CycleC
        {
            public int Id { get; set; }

            [ForeignKey("B")]
            public int BId { get; set; }

            private CycleB B { get; set; }
            public List<CycleA> As { get; set; }
        }
    }

    #endregion

    #region Issue12274

    [ConditionalFact]
    public virtual async Task Parameterless_ctor_on_inner_DTO_gets_called_for_every_row()
    {
        var contextFactory = await InitializeAsync<MyContext12274>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var results = context.Entities.Select(
                x =>
                    new MyContext12274.OuterDTO12274
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Inner = new MyContext12274.InnerDTO12274()
                    }).ToList();
            Assert.Equal(4, results.Count);
            Assert.False(ReferenceEquals(results[0].Inner, results[1].Inner));
            Assert.False(ReferenceEquals(results[1].Inner, results[2].Inner));
            Assert.False(ReferenceEquals(results[2].Inner, results[3].Inner));
        }
    }

    protected class MyContext12274 : DbContext
    {
        public DbSet<MyEntity12274> Entities { get; set; }

        public MyContext12274(DbContextOptions options)
            : base(options)
        {
        }

        public void Seed()
        {
            var e1 = new MyEntity12274 { Name = "1" };
            var e2 = new MyEntity12274 { Name = "2" };
            var e3 = new MyEntity12274 { Name = "3" };
            var e4 = new MyEntity12274 { Name = "4" };

            Entities.AddRange(e1, e2, e3, e4);
            SaveChanges();
        }

        public class MyEntity12274
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class OuterDTO12274
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public InnerDTO12274 Inner { get; set; }
        }

        public class InnerDTO12274
        {
        }
    }

    #endregion

    #region Issue11835

    [ConditionalFact]
    public virtual async Task Projecting_correlated_collection_along_with_non_mapped_property()
    {
        var contextFactory = await InitializeAsync<MyContext11835>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var result = context.Blogs.Select(
                e => new
                {
                    e.Id,
                    e.Title,
                    FirstPostName = e.Posts.Where(i => i.Name.Contains("2")).ToList()
                }).ToList();

            AssertSql(
                """
SELECT [b].[Id], [t].[Id], [t].[BlogId], [t].[Name]
FROM [Blogs] AS [b]
LEFT JOIN (
    SELECT [p].[Id], [p].[BlogId], [p].[Name]
    FROM [Posts] AS [p]
    WHERE [p].[Name] LIKE N'%2%'
) AS [t] ON [b].[Id] = [t].[BlogId]
ORDER BY [b].[Id]
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            var result = context.Blogs.Select(
                e => new
                {
                    e.Id,
                    e.Title,
                    FirstPostName = e.Posts.OrderBy(i => i.Id).FirstOrDefault().Name
                }).ToList();

            AssertSql(
                """
SELECT [b].[Id], (
    SELECT TOP(1) [p].[Name]
    FROM [Posts] AS [p]
    WHERE [b].[Id] = [p].[BlogId]
    ORDER BY [p].[Id])
FROM [Blogs] AS [b]
""");
        }
    }

    protected class MyContext11835 : DbContext
    {
        public DbSet<Blog11835> Blogs { get; set; }
        public DbSet<Post11835> Posts { get; set; }

        public MyContext11835(DbContextOptions options)
            : base(options)
        {
        }

        public void Seed()
        {
            var b1 = new Blog11835 { Title = "B1" };
            var b2 = new Blog11835 { Title = "B2" };
            var p11 = new Post11835 { Name = "P11", Blog = b1 };
            var p12 = new Post11835 { Name = "P12", Blog = b1 };
            var p13 = new Post11835 { Name = "P13", Blog = b1 };
            var p21 = new Post11835 { Name = "P21", Blog = b2 };
            var p22 = new Post11835 { Name = "P22", Blog = b2 };

            Blogs.AddRange(b1, b2);
            Posts.AddRange(p11, p12, p13, p21, p22);
            SaveChanges();
        }

        public class Blog11835
        {
            public int Id { get; set; }

            [NotMapped]
            public string Title { get; set; }

            public List<Post11835> Posts { get; set; }
        }

        public class Post11835
        {
            public int Id { get; set; }
            public int BlogId { get; set; }
            public Blog11835 Blog { get; set; }
            public string Name { get; set; }
        }
    }

    #endregion

    #region Issue23282

    [ConditionalFact]
    [SqlServerCondition(SqlServerCondition.SupportsSqlClr)]
    public virtual async Task Can_query_point_with_buffered_data_reader()
    {
        var contextFactory = await InitializeAsync<MyContext23282>(
            seed: c => c.Seed(),
            onConfiguring: o => new SqlServerDbContextOptionsBuilder(o).UseNetTopologySuite(),
            addServices: c => c.AddEntityFrameworkSqlServerNetTopologySuite());

        using (var context = contextFactory.CreateContext())
        {
            var testUser = context.Locations.FirstOrDefault(x => x.Name == "My Location");

            Assert.NotNull(testUser);

            AssertSql(
                """
SELECT TOP(1) [l].[Id], [l].[Name], [l].[Address_County], [l].[Address_Line1], [l].[Address_Line2], [l].[Address_Point], [l].[Address_Postcode], [l].[Address_Town], [l].[Address_Value]
FROM [Locations] AS [l]
WHERE [l].[Name] = N'My Location'
""");
        }
    }

    protected class MyContext23282 : DbContext
    {
        public DbSet<Location23282> Locations { get; set; }

        public MyContext23282(DbContextOptions options)
            : base(options)
        {
        }

        public void Seed()
        {
            Locations.Add(
                new Location23282
                {
                    Name = "My Location",
                    Address = new Address23282
                    {
                        Line1 = "1 Fake Street",
                        Town = "Fake Town",
                        County = "Fakeshire",
                        Postcode = "PO57 0DE",
                        Point = new Point(115.7930, 37.2431) { SRID = 4326 }
                    }
                });
            SaveChanges();
        }

        [Owned]
        public class Address23282
        {
            public string Line1 { get; set; }
            public string Line2 { get; set; }
            public string Town { get; set; }
            public string County { get; set; }
            public string Postcode { get; set; }
            public int Value { get; set; }

            public Point Point { get; set; }
        }

        public class Location23282
        {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public Guid Id { get; set; }

            public string Name { get; set; }
            public Address23282 Address { get; set; }
        }
    }

    #endregion

    #region Issue19253

    [ConditionalFact]
    public virtual async Task Operators_combine_nullability_of_entity_shapers()
    {
        var contextFactory = await InitializeAsync<MyContext19253>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            Expression<Func<MyContext19253.A19253, string>> leftKeySelector = x => x.forkey;
            Expression<Func<MyContext19253.B19253, string>> rightKeySelector = y => y.forkey;

            var query = context.A.GroupJoin(
                    context.B,
                    leftKeySelector,
                    rightKeySelector,
                    (left, rightg) => new { left, rightg })
                .SelectMany(
                    r => r.rightg.DefaultIfEmpty(),
                    (x, y) => new MyContext19253.JoinResult19253<MyContext19253.A19253, MyContext19253.B19253> { Left = x.left, Right = y })
                .Concat(
                    context.B.GroupJoin(
                            context.A,
                            rightKeySelector,
                            leftKeySelector,
                            (right, leftg) => new { leftg, right })
                        .SelectMany(
                            l => l.leftg.DefaultIfEmpty(),
                            (x, y) => new MyContext19253.JoinResult19253<MyContext19253.A19253, MyContext19253.B19253>
                            {
                                Left = y, Right = x.right
                            })
                        .Where(z => z.Left.Equals(null)))
                .ToList();

            Assert.Equal(3, query.Count);

            AssertSql(
                """
SELECT [a].[Id], [a].[a], [a].[a1], [a].[forkey], [b].[Id] AS [Id0], [b].[b], [b].[b1], [b].[forkey] AS [forkey0]
FROM [A] AS [a]
LEFT JOIN [B] AS [b] ON [a].[forkey] = [b].[forkey]
UNION ALL
SELECT [a0].[Id], [a0].[a], [a0].[a1], [a0].[forkey], [b0].[Id] AS [Id0], [b0].[b], [b0].[b1], [b0].[forkey] AS [forkey0]
FROM [B] AS [b0]
LEFT JOIN [A] AS [a0] ON [b0].[forkey] = [a0].[forkey]
WHERE [a0].[Id] IS NULL
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            Expression<Func<MyContext19253.A19253, string>> leftKeySelector = x => x.forkey;
            Expression<Func<MyContext19253.B19253, string>> rightKeySelector = y => y.forkey;

            var query = context.A.GroupJoin(
                    context.B,
                    leftKeySelector,
                    rightKeySelector,
                    (left, rightg) => new { left, rightg })
                .SelectMany(
                    r => r.rightg.DefaultIfEmpty(),
                    (x, y) => new MyContext19253.JoinResult19253<MyContext19253.A19253, MyContext19253.B19253> { Left = x.left, Right = y })
                .Union(
                    context.B.GroupJoin(
                            context.A,
                            rightKeySelector,
                            leftKeySelector,
                            (right, leftg) => new { leftg, right })
                        .SelectMany(
                            l => l.leftg.DefaultIfEmpty(),
                            (x, y) => new MyContext19253.JoinResult19253<MyContext19253.A19253, MyContext19253.B19253>
                            {
                                Left = y, Right = x.right
                            })
                        .Where(z => z.Left.Equals(null)))
                .ToList();

            Assert.Equal(3, query.Count);

            AssertSql(
                """
SELECT [a].[Id], [a].[a], [a].[a1], [a].[forkey], [b].[Id] AS [Id0], [b].[b], [b].[b1], [b].[forkey] AS [forkey0]
FROM [A] AS [a]
LEFT JOIN [B] AS [b] ON [a].[forkey] = [b].[forkey]
UNION
SELECT [a0].[Id], [a0].[a], [a0].[a1], [a0].[forkey], [b0].[Id] AS [Id0], [b0].[b], [b0].[b1], [b0].[forkey] AS [forkey0]
FROM [B] AS [b0]
LEFT JOIN [A] AS [a0] ON [b0].[forkey] = [a0].[forkey]
WHERE [a0].[Id] IS NULL
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            Expression<Func<MyContext19253.A19253, string>> leftKeySelector = x => x.forkey;
            Expression<Func<MyContext19253.B19253, string>> rightKeySelector = y => y.forkey;

            var query = context.A.GroupJoin(
                    context.B,
                    leftKeySelector,
                    rightKeySelector,
                    (left, rightg) => new { left, rightg })
                .SelectMany(
                    r => r.rightg.DefaultIfEmpty(),
                    (x, y) => new MyContext19253.JoinResult19253<MyContext19253.A19253, MyContext19253.B19253> { Left = x.left, Right = y })
                .Except(
                    context.B.GroupJoin(
                            context.A,
                            rightKeySelector,
                            leftKeySelector,
                            (right, leftg) => new { leftg, right })
                        .SelectMany(
                            l => l.leftg.DefaultIfEmpty(),
                            (x, y) => new MyContext19253.JoinResult19253<MyContext19253.A19253, MyContext19253.B19253>
                            {
                                Left = y, Right = x.right
                            }))
                .ToList();

            Assert.Single(query);

            AssertSql(
                """
SELECT [a].[Id], [a].[a], [a].[a1], [a].[forkey], [b].[Id] AS [Id0], [b].[b], [b].[b1], [b].[forkey] AS [forkey0]
FROM [A] AS [a]
LEFT JOIN [B] AS [b] ON [a].[forkey] = [b].[forkey]
EXCEPT
SELECT [a0].[Id], [a0].[a], [a0].[a1], [a0].[forkey], [b0].[Id] AS [Id0], [b0].[b], [b0].[b1], [b0].[forkey] AS [forkey0]
FROM [B] AS [b0]
LEFT JOIN [A] AS [a0] ON [b0].[forkey] = [a0].[forkey]
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            Expression<Func<MyContext19253.A19253, string>> leftKeySelector = x => x.forkey;
            Expression<Func<MyContext19253.B19253, string>> rightKeySelector = y => y.forkey;

            var query = context.A.GroupJoin(
                    context.B,
                    leftKeySelector,
                    rightKeySelector,
                    (left, rightg) => new { left, rightg })
                .SelectMany(
                    r => r.rightg.DefaultIfEmpty(),
                    (x, y) => new MyContext19253.JoinResult19253<MyContext19253.A19253, MyContext19253.B19253> { Left = x.left, Right = y })
                .Intersect(
                    context.B.GroupJoin(
                            context.A,
                            rightKeySelector,
                            leftKeySelector,
                            (right, leftg) => new { leftg, right })
                        .SelectMany(
                            l => l.leftg.DefaultIfEmpty(),
                            (x, y) => new MyContext19253.JoinResult19253<MyContext19253.A19253, MyContext19253.B19253>
                            {
                                Left = y, Right = x.right
                            }))
                .ToList();

            Assert.Single(query);

            AssertSql(
                """
SELECT [a].[Id], [a].[a], [a].[a1], [a].[forkey], [b].[Id] AS [Id0], [b].[b], [b].[b1], [b].[forkey] AS [forkey0]
FROM [A] AS [a]
LEFT JOIN [B] AS [b] ON [a].[forkey] = [b].[forkey]
INTERSECT
SELECT [a0].[Id], [a0].[a], [a0].[a1], [a0].[forkey], [b0].[Id] AS [Id0], [b0].[b], [b0].[b1], [b0].[forkey] AS [forkey0]
FROM [B] AS [b0]
LEFT JOIN [A] AS [a0] ON [b0].[forkey] = [a0].[forkey]
""");
        }
    }

    public class MyContext19253 : DbContext
    {
        public DbSet<A19253> A { get; set; }
        public DbSet<B19253> B { get; set; }

        public MyContext19253(DbContextOptions options)
            : base(options)
        {
        }

        public void Seed()
        {
            var tmp_a = new[]
            {
                new()
                {
                    a = "a0",
                    a1 = "a1",
                    forkey = "a"
                },
                new A19253
                {
                    a = "a2",
                    a1 = "a1",
                    forkey = "d"
                },
            };
            var tmp_b = new[]
            {
                new()
                {
                    b = "b0",
                    b1 = "b1",
                    forkey = "a"
                },
                new B19253
                {
                    b = "b2",
                    b1 = "b1",
                    forkey = "c"
                },
            };
            A.AddRange(tmp_a);
            B.AddRange(tmp_b);
            SaveChanges();
        }

        public class JoinResult19253<TLeft, TRight>
        {
            public TLeft Left { get; set; }

            public TRight Right { get; set; }
        }

        public class A19253
        {
            public int Id { get; set; }
            public string a { get; set; }
            public string a1 { get; set; }
            public string forkey { get; set; }
        }

        public class B19253
        {
            public int Id { get; set; }
            public string b { get; set; }
            public string b1 { get; set; }
            public string forkey { get; set; }
        }
    }

    #endregion

    #region Issue23410

    // TODO: Remove when JSON is first class. See issue#4021

    [ConditionalFact]
    public virtual async Task Method_call_translators_are_invoked_for_indexer_if_not_indexer_property()
    {
        var contextFactory = await InitializeAsync<MyContext23410>(
            seed: c => c.Seed(),
            addServices: c => c.TryAddEnumerable(
                new ServiceDescriptor(
                    typeof(IMethodCallTranslatorPlugin), typeof(MyContext23410.JsonMethodCallTranslatorPlugin),
                    ServiceLifetime.Scoped)));

        using (var context = contextFactory.CreateContext())
        {
            var testUser = context.Blogs.FirstOrDefault(x => x.JObject["Author"].Value<string>() == "Maumar");

            Assert.NotNull(testUser);

            AssertSql(
                """
SELECT TOP(1) [b].[Id], [b].[JObject], [b].[Name]
FROM [Blogs] AS [b]
WHERE JSON_VALUE([b].[JObject], '$.Author') = N'Maumar'
""");
        }
    }

    protected class MyContext23410 : DbContext
    {
        public DbSet<Blog23410> Blogs { get; set; }

        public MyContext23410(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Blog23410>().Property(e => e.JObject).HasConversion(
                e => e.ToString(),
                e => JObject.Parse(e));

        public void Seed()
        {
            Blogs.Add(new Blog23410 { Name = "My Location", JObject = JObject.Parse(@"{ ""Author"": ""Maumar"" }") });
            SaveChanges();
        }

        public class Blog23410
        {
            public int Id { get; set; }

            public string Name { get; set; }
            public JObject JObject { get; set; }
        }

        public class JsonMethodCallTranslatorPlugin : IMethodCallTranslatorPlugin
        {
            public JsonMethodCallTranslatorPlugin(ISqlExpressionFactory sqlExpressionFactory)
            {
                Translators = new IMethodCallTranslator[]
                {
                    new JsonIndexerMethodTranslator(sqlExpressionFactory), new JsonValueMethodTranslator(sqlExpressionFactory)
                };
            }

            public IEnumerable<IMethodCallTranslator> Translators { get; }
        }

        private class JsonValueMethodTranslator : IMethodCallTranslator
        {
            private readonly ISqlExpressionFactory _sqlExpressionFactory;

            public JsonValueMethodTranslator(ISqlExpressionFactory sqlExpressionFactory)
            {
                _sqlExpressionFactory = sqlExpressionFactory;
            }

            public SqlExpression Translate(
                SqlExpression instance,
                MethodInfo method,
                IReadOnlyList<SqlExpression> arguments,
                IDiagnosticsLogger<DbLoggerCategory.Query> logger)
            {
                if (method.IsGenericMethod
                    && method.DeclaringType == typeof(Newtonsoft.Json.Linq.Extensions)
                    && method.Name == "Value"
                    && arguments.Count == 1
                    && arguments[0] is SqlFunctionExpression sqlFunctionExpression)
                {
                    return _sqlExpressionFactory.Function(
                        sqlFunctionExpression.Name,
                        sqlFunctionExpression.Arguments,
                        sqlFunctionExpression.IsNullable,
                        sqlFunctionExpression.ArgumentsPropagateNullability,
                        method.ReturnType);
                }

                return null;
            }
        }

        private class JsonIndexerMethodTranslator : IMethodCallTranslator
        {
            private readonly MethodInfo _indexerMethod = typeof(JObject).GetRuntimeMethod("get_Item", new[] { typeof(string) });

            private readonly ISqlExpressionFactory _sqlExpressionFactory;

            public JsonIndexerMethodTranslator(ISqlExpressionFactory sqlExpressionFactory)
            {
                _sqlExpressionFactory = sqlExpressionFactory;
            }

            public SqlExpression Translate(
                SqlExpression instance,
                MethodInfo method,
                IReadOnlyList<SqlExpression> arguments,
                IDiagnosticsLogger<DbLoggerCategory.Query> logger)
            {
                if (Equals(_indexerMethod, method))
                {
                    return _sqlExpressionFactory.Function(
                        "JSON_VALUE",
                        new[] { instance, _sqlExpressionFactory.Fragment($"'$.{((SqlConstantExpression)arguments[0]).Value}'") },
                        nullable: true,
                        argumentsPropagateNullability: new[] { true, false },
                        _indexerMethod.ReturnType);
                }

                return null;
            }
        }
    }

    #endregion

    #region Issue22841

    [ConditionalFact]
    public async Task SaveChangesAsync_accepts_changes_with_ConfigureAwait_true_22841()
    {
        var contextFactory = await InitializeAsync<MyContext22841>();

        using var context = contextFactory.CreateContext();
        var observableThing = new ObservableThing22841();

        using var trackingSynchronizationContext = new SingleThreadSynchronizationContext();
        var origSynchronizationContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(trackingSynchronizationContext);

        // Do a dispatch once to make sure we're in the new synchronization context. This is necessary in case the below happens
        // to complete synchronously, which shouldn't happen in principle - but just to be safe.
        await Task.Delay(1).ConfigureAwait(true);

        bool? isMySyncContext = null;
        Action callback = () => isMySyncContext =
            SynchronizationContext.Current == trackingSynchronizationContext
            && Thread.CurrentThread == trackingSynchronizationContext.Thread;
        observableThing.Event += callback;

        try
        {
            await context.AddAsync(observableThing);
            await context.SaveChangesAsync();
        }
        finally
        {
            observableThing.Event -= callback;
            SynchronizationContext.SetSynchronizationContext(origSynchronizationContext);
        }

        Assert.True(isMySyncContext);
    }

    protected class MyContext22841 : DbContext
    {
        public MyContext22841(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder
                .Entity<ObservableThing22841>()
                .Property(o => o.Id)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

        public DbSet<ObservableThing22841> ObservableThings { get; set; }
    }

    public class ObservableThing22841
    {
        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                Event?.Invoke();
            }
        }

        private int _id;

        public event Action Event;
    }

    #endregion Issue22841

    #region Issue12482

    [ConditionalFact]
    public virtual async Task Batch_insert_with_sqlvariant_different_types_12482()
    {
        var contextFactory = await InitializeAsync<MyContext12482>();

        using (var context = contextFactory.CreateContext())
        {
            context.AddRange(
                new MyContext12482.BaseEntity12482 { Value = 10.0999 },
                new MyContext12482.BaseEntity12482 { Value = -12345 },
                new MyContext12482.BaseEntity12482 { Value = "String Value" },
                new MyContext12482.BaseEntity12482 { Value = new DateTime(2020, 1, 1) });

            context.SaveChanges();

            AssertSql(
                """
@p0='10.0999' (Nullable = true) (DbType = Object)
@p1='-12345' (Nullable = true) (DbType = Object)
@p2='String Value' (Size = 12) (DbType = Object)
@p3='2020-01-01T00:00:00.0000000' (Nullable = true) (DbType = Object)

SET IMPLICIT_TRANSACTIONS OFF;
SET NOCOUNT ON;
MERGE [BaseEntities] USING (
VALUES (@p0, 0),
(@p1, 1),
(@p2, 2),
(@p3, 3)) AS i ([Value], _Position) ON 1=0
WHEN NOT MATCHED THEN
INSERT ([Value])
VALUES (i.[Value])
OUTPUT INSERTED.[Id], i._Position;
""");
        }
    }

    protected class MyContext12482 : DbContext
    {
        public virtual DbSet<BaseEntity12482> BaseEntities { get; set; }

        public MyContext12482(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<BaseEntity12482>();

        public class BaseEntity12482
        {
            public int Id { get; set; }

            [Column(TypeName = "sql_variant")]
            public object Value { get; set; }
        }
    }

    #endregion

    #region Issue23674

    [ConditionalFact]
    public virtual async Task Walking_back_include_tree_is_not_allowed_1()
    {
        var contextFactory = await InitializeAsync<MyContext23674>();

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<Principal23674>()
                .Include(p => p.ManyDependents)
                .ThenInclude(m => m.Principal.SingleDependent);

            Assert.Equal(
                CoreStrings.WarningAsErrorTemplate(
                    CoreEventId.NavigationBaseIncludeIgnored.ToString(),
                    CoreResources.LogNavigationBaseIncludeIgnored(new TestLogger<TestLoggingDefinitions>())
                        .GenerateMessage("ManyDependent23674.Principal"),
                    "CoreEventId.NavigationBaseIncludeIgnored"),
                Assert.Throws<InvalidOperationException>(
                    () => query.ToList()).Message);
        }
    }

    [ConditionalFact]
    public virtual async Task Walking_back_include_tree_is_not_allowed_2()
    {
        var contextFactory = await InitializeAsync<MyContext23674>();

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<Principal23674>().Include(p => p.SingleDependent.Principal.ManyDependents);

            Assert.Equal(
                CoreStrings.WarningAsErrorTemplate(
                    CoreEventId.NavigationBaseIncludeIgnored.ToString(),
                    CoreResources.LogNavigationBaseIncludeIgnored(new TestLogger<TestLoggingDefinitions>())
                        .GenerateMessage("SingleDependent23674.Principal"),
                    "CoreEventId.NavigationBaseIncludeIgnored"),
                Assert.Throws<InvalidOperationException>(
                    () => query.ToList()).Message);
        }
    }

    [ConditionalFact]
    public virtual async Task Walking_back_include_tree_is_not_allowed_3()
    {
        var contextFactory = await InitializeAsync<MyContext23674>();

        using (var context = contextFactory.CreateContext())
        {
            // This does not warn because after round-tripping from one-to-many from dependent side, the number of dependents could be larger.
            var query = context.Set<ManyDependent23674>()
                .Include(p => p.Principal.ManyDependents)
                .ThenInclude(m => m.SingleDependent)
                .ToList();
        }
    }

    [ConditionalFact]
    public virtual async Task Walking_back_include_tree_is_not_allowed_4()
    {
        var contextFactory = await InitializeAsync<MyContext23674>();

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<SingleDependent23674>().Include(p => p.ManyDependent.SingleDependent.Principal);

            Assert.Equal(
                CoreStrings.WarningAsErrorTemplate(
                    CoreEventId.NavigationBaseIncludeIgnored.ToString(),
                    CoreResources.LogNavigationBaseIncludeIgnored(new TestLogger<TestLoggingDefinitions>())
                        .GenerateMessage("ManyDependent23674.SingleDependent"),
                    "CoreEventId.NavigationBaseIncludeIgnored"),
                Assert.Throws<InvalidOperationException>(
                    () => query.ToList()).Message);
        }
    }

    private class Principal23674
    {
        public int Id { get; set; }
        public List<ManyDependent23674> ManyDependents { get; set; }
        public SingleDependent23674 SingleDependent { get; set; }
    }

    private class ManyDependent23674
    {
        public int Id { get; set; }
        public Principal23674 Principal { get; set; }
        public SingleDependent23674 SingleDependent { get; set; }
    }

    private class SingleDependent23674
    {
        public int Id { get; set; }
        public Principal23674 Principal { get; set; }
        public int PrincipalId { get; set; }
        public int ManyDependentId { get; set; }
        public ManyDependent23674 ManyDependent { get; set; }
    }

    private class MyContext23674 : DbContext
    {
        public MyContext23674(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Principal23674>();
    }

    #endregion

    #region Issue23676

    [ConditionalFact]
    public virtual async Task Projection_with_multiple_includes_and_subquery_with_set_operation()
    {
        var contextFactory = await InitializeAsync<MyContext23676>();

        using var context = contextFactory.CreateContext();
        var id = 1;
        var person = await context.Persons
            .Include(p => p.Images)
            .Include(p => p.Actor)
            .ThenInclude(a => a.Movies)
            .ThenInclude(p => p.Movie)
            .Include(p => p.Director)
            .ThenInclude(a => a.Movies)
            .ThenInclude(p => p.Movie)
            .Select(
                x => new
                {
                    x.Id,
                    x.Name,
                    x.Surname,
                    x.Birthday,
                    x.Hometown,
                    x.Bio,
                    x.AvatarUrl,
                    Images = x.Images
                        .Select(
                            i => new
                            {
                                i.Id,
                                i.ImageUrl,
                                i.Height,
                                i.Width
                            }).ToList(),
                    KnownByFilms = x.Actor.Movies
                        .Select(m => m.Movie)
                        .Union(
                            x.Director.Movies
                                .Select(m => m.Movie))
                        .Select(
                            m => new
                            {
                                m.Id,
                                m.Name,
                                m.PosterUrl,
                                m.Rating
                            }).ToList()
                })
            .FirstOrDefaultAsync(x => x.Id == id);

        // Verify the valid generated SQL
        AssertSql(
            """
@__id_0='1'

SELECT [t].[Id], [t].[Name], [t].[Surname], [t].[Birthday], [t].[Hometown], [t].[Bio], [t].[AvatarUrl], [t].[Id0], [t].[Id1], [p0].[Id], [p0].[ImageUrl], [p0].[Height], [p0].[Width], [t0].[Id], [t0].[Name], [t0].[PosterUrl], [t0].[Rating]
FROM (
    SELECT TOP(1) [p].[Id], [p].[Name], [p].[Surname], [p].[Birthday], [p].[Hometown], [p].[Bio], [p].[AvatarUrl], [a].[Id] AS [Id0], [d].[Id] AS [Id1]
    FROM [Persons] AS [p]
    LEFT JOIN [ActorEntity] AS [a] ON [p].[Id] = [a].[PersonId]
    LEFT JOIN [DirectorEntity] AS [d] ON [p].[Id] = [d].[PersonId]
    WHERE [p].[Id] = @__id_0
) AS [t]
LEFT JOIN [PersonImageEntity] AS [p0] ON [t].[Id] = [p0].[PersonId]
OUTER APPLY (
    SELECT [m0].[Id], [m0].[Budget], [m0].[Description], [m0].[DurationInMins], [m0].[Name], [m0].[PosterUrl], [m0].[Rating], [m0].[ReleaseDate], [m0].[Revenue]
    FROM [MovieActorEntity] AS [m]
    INNER JOIN [MovieEntity] AS [m0] ON [m].[MovieId] = [m0].[Id]
    WHERE [t].[Id0] IS NOT NULL AND [t].[Id0] = [m].[ActorId]
    UNION
    SELECT [m2].[Id], [m2].[Budget], [m2].[Description], [m2].[DurationInMins], [m2].[Name], [m2].[PosterUrl], [m2].[Rating], [m2].[ReleaseDate], [m2].[Revenue]
    FROM [MovieDirectorEntity] AS [m1]
    INNER JOIN [MovieEntity] AS [m2] ON [m1].[MovieId] = [m2].[Id]
    WHERE [t].[Id1] IS NOT NULL AND [t].[Id1] = [m1].[DirectorId]
) AS [t0]
ORDER BY [t].[Id], [t].[Id0], [t].[Id1], [p0].[Id]
""");
    }

    private class PersonEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public DateTime Birthday { get; set; }
        public string Hometown { get; set; }
        public string Bio { get; set; }
        public string AvatarUrl { get; set; }

        public ActorEntity Actor { get; set; }
        public DirectorEntity Director { get; set; }
        public IList<PersonImageEntity> Images { get; } = new List<PersonImageEntity>();
    }

    private class PersonImageEntity
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public PersonEntity Person { get; set; }
    }

    private class ActorEntity
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public PersonEntity Person { get; set; }

        public IList<MovieActorEntity> Movies { get; } = new List<MovieActorEntity>();
    }

    private class MovieActorEntity
    {
        public int Id { get; set; }
        public int ActorId { get; set; }
        public ActorEntity Actor { get; set; }

        public int MovieId { get; set; }
        public MovieEntity Movie { get; set; }

        public string RoleInFilm { get; set; }

        public int Order { get; set; }
    }

    private class DirectorEntity
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public PersonEntity Person { get; set; }

        public IList<MovieDirectorEntity> Movies { get; } = new List<MovieDirectorEntity>();
    }

    private class MovieDirectorEntity
    {
        public int Id { get; set; }
        public int DirectorId { get; set; }
        public DirectorEntity Director { get; set; }

        public int MovieId { get; set; }
        public MovieEntity Movie { get; set; }
    }

    private class MovieEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Rating { get; set; }
        public string Description { get; set; }
        public DateTime ReleaseDate { get; set; }
        public int DurationInMins { get; set; }
        public int Budget { get; set; }
        public int Revenue { get; set; }
        public string PosterUrl { get; set; }

        public IList<MovieDirectorEntity> Directors { get; set; } = new List<MovieDirectorEntity>();
        public IList<MovieActorEntity> Actors { get; set; } = new List<MovieActorEntity>();
    }

    private class MyContext23676 : DbContext
    {
        public MyContext23676(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<PersonEntity> Persons { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }

    #endregion

    #region Issue19947

    [ConditionalFact]
    public virtual async Task Multiple_select_many_in_projection()
    {
        var contextFactory = await InitializeAsync<MyContext19947>();

        using var context = contextFactory.CreateContext();
        var query = context.Users.Select(
            captain => new
            {
                CaptainRateDtos = captain.Cars
                    .SelectMany(car0 => car0.Taxis)
                    .OrderByDescending(taxi => taxi.DateArrived).Take(12)
                    .Select(
                        taxi => new
                        {
                            Rate = taxi.UserRate.Value,
                            UserRateText = taxi.UserTextRate,
                            UserId = taxi.UserEUser.Id,
                        }).ToList(),
                ReportCount = captain.Cars
                    .SelectMany(car1 => car1.Taxis).Count(taxi0 => taxi0.ReportText != ""),
            }).SingleOrDefault();

        // Verify the valid generated SQL
        AssertSql(
            """
SELECT [t].[Id], [t1].[Rate], [t1].[UserRateText], [t1].[UserId], [t1].[Id], [t1].[Id0], [t].[c]
FROM (
    SELECT TOP(2) (
        SELECT COUNT(*)
        FROM [Cars] AS [c]
        INNER JOIN [Taxis] AS [t0] ON [c].[Id] = [t0].[CarId]
        WHERE [u].[Id] = [c].[EUserId] AND ([t0].[ReportText] <> N'' OR [t0].[ReportText] IS NULL)) AS [c], [u].[Id]
    FROM [Users] AS [u]
) AS [t]
OUTER APPLY (
    SELECT [t2].[UserRate] AS [Rate], [t2].[UserTextRate] AS [UserRateText], [u0].[Id] AS [UserId], [t2].[Id], [t2].[Id0], [t2].[DateArrived]
    FROM (
        SELECT TOP(12) [c0].[Id], [t3].[Id] AS [Id0], [t3].[DateArrived], [t3].[UserEUserId], [t3].[UserRate], [t3].[UserTextRate]
        FROM [Cars] AS [c0]
        INNER JOIN [Taxis] AS [t3] ON [c0].[Id] = [t3].[CarId]
        WHERE [t].[Id] = [c0].[EUserId]
        ORDER BY [t3].[DateArrived] DESC
    ) AS [t2]
    LEFT JOIN [Users] AS [u0] ON [t2].[UserEUserId] = [u0].[Id]
) AS [t1]
ORDER BY [t].[Id], [t1].[DateArrived] DESC, [t1].[Id], [t1].[Id0]
""");
    }

    [ConditionalFact]
    public virtual async Task Single_select_many_in_projection_with_take()
    {
        var contextFactory = await InitializeAsync<MyContext19947>();

        using var context = contextFactory.CreateContext();
        var query = context.Users.Select(
            captain => new
            {
                CaptainRateDtos = captain.Cars
                    .SelectMany(car0 => car0.Taxis)
                    .OrderByDescending(taxi => taxi.DateArrived).Take(12)
                    .Select(
                        taxi => new
                        {
                            Rate = taxi.UserRate.Value,
                            UserRateText = taxi.UserTextRate,
                            UserId = taxi.UserEUser.Id,
                        }).ToList()
            }).SingleOrDefault();

        // Verify the valid generated SQL
        AssertSql(
            """
SELECT [t].[Id], [t1].[Rate], [t1].[UserRateText], [t1].[UserId], [t1].[Id], [t1].[Id0]
FROM (
    SELECT TOP(2) [u].[Id]
    FROM [Users] AS [u]
) AS [t]
OUTER APPLY (
    SELECT [t0].[UserRate] AS [Rate], [t0].[UserTextRate] AS [UserRateText], [u0].[Id] AS [UserId], [t0].[Id], [t0].[Id0], [t0].[DateArrived]
    FROM (
        SELECT TOP(12) [c].[Id], [t2].[Id] AS [Id0], [t2].[DateArrived], [t2].[UserEUserId], [t2].[UserRate], [t2].[UserTextRate]
        FROM [Cars] AS [c]
        INNER JOIN [Taxis] AS [t2] ON [c].[Id] = [t2].[CarId]
        WHERE [t].[Id] = [c].[EUserId]
        ORDER BY [t2].[DateArrived] DESC
    ) AS [t0]
    LEFT JOIN [Users] AS [u0] ON [t0].[UserEUserId] = [u0].[Id]
) AS [t1]
ORDER BY [t].[Id], [t1].[DateArrived] DESC, [t1].[Id], [t1].[Id0]
""");
    }

    private class EUser
    {
        public int Id { get; set; }

        public ICollection<Car> Cars { get; set; }
    }

    private class Taxi
    {
        public int Id { get; set; }
        public DateTime? DateArrived { get; set; }
        public int? UserRate { get; set; }
        public string UserTextRate { get; set; }
        public string ReportText { get; set; }
        public EUser UserEUser { get; set; }
    }

    private class Car
    {
        public int Id { get; set; }
        public ICollection<Taxi> Taxis { get; set; }
    }

    private class MyContext19947 : DbContext
    {
        public MyContext19947(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<EUser> Users { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<Taxi> Taxis { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }

    #endregion

    #region Issue20813

    [ConditionalFact]
    public virtual async Task SelectMany_and_collection_in_projection_in_FirstOrDefault()
    {
        var contextFactory = await InitializeAsync<MyContext20813>();

        using var context = contextFactory.CreateContext();
        var referenceId = "a";
        var customerId = new Guid("1115c816-6c4c-4016-94df-d8b60a22ffa1");
        var query = context.Orders
            .Where(o => o.ExternalReferenceId == referenceId && o.CustomerId == customerId)
            .Select(
                o => new
                {
                    IdentityDocuments = o.IdentityDocuments.Select(
                        id => new
                        {
                            Images = o.IdentityDocuments
                                .SelectMany(id => id.Images)
                                .Select(i => new { i.Image }),
                        })
                }).SingleOrDefault();

        // Verify the valid generated SQL
        AssertSql(
            """
@__referenceId_0='a' (Size = 4000)
@__customerId_1='1115c816-6c4c-4016-94df-d8b60a22ffa1'

SELECT [t].[Id], [t0].[Id], [t0].[Image], [t0].[Id0], [t0].[Id00]
FROM (
    SELECT TOP(2) [o].[Id]
    FROM [Orders] AS [o]
    WHERE [o].[ExternalReferenceId] = @__referenceId_0 AND [o].[CustomerId] = @__customerId_1
) AS [t]
OUTER APPLY (
    SELECT [i].[Id], [t1].[Image], [t1].[Id] AS [Id0], [t1].[Id0] AS [Id00]
    FROM [IdentityDocument] AS [i]
    OUTER APPLY (
        SELECT [i1].[Image], [i0].[Id], [i1].[Id] AS [Id0]
        FROM [IdentityDocument] AS [i0]
        INNER JOIN [IdentityDocumentImage] AS [i1] ON [i0].[Id] = [i1].[IdentityDocumentId]
        WHERE [t].[Id] = [i0].[OrderId]
    ) AS [t1]
    WHERE [t].[Id] = [i].[OrderId]
) AS [t0]
ORDER BY [t].[Id], [t0].[Id], [t0].[Id0]
""");
    }

    private class Order
    {
        private ICollection<IdentityDocument> _identityDocuments;

        public Guid Id { get; set; }

        public Guid CustomerId { get; set; }

        public string ExternalReferenceId { get; set; }

        public ICollection<IdentityDocument> IdentityDocuments
        {
            get => _identityDocuments = _identityDocuments ?? new Collection<IdentityDocument>();
            set => _identityDocuments = value;
        }
    }

    private class IdentityDocument
    {
        private ICollection<IdentityDocumentImage> _images;

        public Guid Id { get; set; }

        [ForeignKey(nameof(Order))]
        public Guid OrderId { get; set; }

        public Order Order { get; set; }

        public ICollection<IdentityDocumentImage> Images
        {
            get => _images = _images ?? new Collection<IdentityDocumentImage>();
            set => _images = value;
        }
    }

    private class IdentityDocumentImage
    {
        public Guid Id { get; set; }

        [ForeignKey(nameof(IdentityDocument))]
        public Guid IdentityDocumentId { get; set; }

        public byte[] Image { get; set; }

        public IdentityDocument IdentityDocument { get; set; }
    }

    private class MyContext20813 : DbContext
    {
        public MyContext20813(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; }
    }

    #endregion

    #region Issue25400

    [ConditionalTheory]
    [InlineData(true)]
    [InlineData(false)]
    public virtual async Task NoTracking_split_query_creates_only_required_instances(bool async)
    {
        var contextFactory = await InitializeAsync<MyContext25400>(
            seed: c => c.Seed(),
            onConfiguring: o => new SqlServerDbContextOptionsBuilder(o).UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

        using (var context = contextFactory.CreateContext())
        {
            Test25400.ConstructorCallCount = 0;

            var query = context.Set<Test25400>().AsNoTracking().OrderBy(e => e.Id);
            var test = async
                ? await query.FirstOrDefaultAsync()
                : query.FirstOrDefault();

            Assert.Equal(1, Test25400.ConstructorCallCount);

            AssertSql(
                """
SELECT TOP(1) [t].[Id], [t].[Value]
FROM [Tests] AS [t]
ORDER BY [t].[Id]
""");
        }
    }

    protected class MyContext25400 : DbContext
    {
        public DbSet<Test25400> Tests { get; set; }

        public MyContext25400(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Test25400>().HasKey(e => e.Id);

        public void Seed()
        {
            Tests.Add(new Test25400(15));

            SaveChanges();
        }
    }

    protected class Test25400
    {
        public static int ConstructorCallCount;

        public Test25400()
        {
            ++ConstructorCallCount;
        }

        public Test25400(int value)
        {
            Value = value;
        }

        public int Id { get; set; }
        public int Value { get; set; }
    }

    #endregion

    #region Issue25225

    [ConditionalFact]
    public virtual async Task Can_query_with_nav_collection_in_projection_with_split_query_in_parallel_async()
    {
        var contextFactory = await CreateContext25225Async();
        var task1 = QueryAsync(MyContext25225.Parent1Id, MyContext25225.Collection1Id);
        var task2 = QueryAsync(MyContext25225.Parent2Id, MyContext25225.Collection2Id);
        await Task.WhenAll(task1, task2);

        async Task QueryAsync(Guid parentId, Guid collectionId)
        {
            using (var context = contextFactory.CreateContext())
            {
                ClearLog();
                for (var i = 0; i < 100; i++)
                {
                    var parent = await SelectParent25225(context, parentId).SingleAsync();
                    AssertParent25225(parentId, collectionId, parent);
                }
            }
        }
    }

    [ConditionalFact]
    public virtual async Task Can_query_with_nav_collection_in_projection_with_split_query_in_parallel_sync()
    {
        var contextFactory = await CreateContext25225Async();
        var task1 = Task.Run(() => Query(MyContext25225.Parent1Id, MyContext25225.Collection1Id));
        var task2 = Task.Run(() => Query(MyContext25225.Parent2Id, MyContext25225.Collection2Id));
        await Task.WhenAll(task1, task2);

        void Query(Guid parentId, Guid collectionId)
        {
            using (var context = contextFactory.CreateContext())
            {
                ClearLog();
                for (var i = 0; i < 10; i++)
                {
                    var parent = SelectParent25225(context, parentId).Single();
                    AssertParent25225(parentId, collectionId, parent);
                }
            }
        }
    }

    private Task<ContextFactory<MyContext25225>> CreateContext25225Async()
        => InitializeAsync<MyContext25225>(
            seed: c => c.Seed(),
            onConfiguring: o => new SqlServerDbContextOptionsBuilder(o).UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
        );

    private static IQueryable<ParentViewModel25225> SelectParent25225(MyContext25225 context, Guid parentId)
        => context
            .Parents
            .Where(x => x.Id == parentId)
            .Select(
                p => new ParentViewModel25225
                {
                    Id = p.Id,
                    Collection = p
                        .Collection
                        .Select(
                            c => new CollectionViewModel25225
                            {
                                Id = c.Id, ParentId = c.ParentId,
                            })
                        .ToArray()
                });

    private static void AssertParent25225(Guid expectedParentId, Guid expectedCollectionId, ParentViewModel25225 actualParent)
    {
        Assert.Equal(expectedParentId, actualParent.Id);
        Assert.Collection(
            actualParent.Collection,
            c => Assert.Equal(expectedCollectionId, c.Id)
        );
    }

    protected class MyContext25225 : DbContext
    {
        public static readonly Guid Parent1Id = new("d6457b52-690a-419e-8982-a1a8551b4572");
        public static readonly Guid Parent2Id = new("e79c82f4-3ae7-4c65-85db-04e08cba6fa7");
        public static readonly Guid Collection1Id = new("7ce625fb-863d-41b3-b42e-e4e4367f7548");
        public static readonly Guid Collection2Id = new("d347bbd5-003a-441f-a148-df8ab8ac4a29");
        public DbSet<Parent25225> Parents { get; set; }

        public MyContext25225(DbContextOptions options)
            : base(options)
        {
        }

        public void Seed()
        {
            var parent1 = new Parent25225 { Id = Parent1Id, Collection = new List<Collection25225> { new() { Id = Collection1Id, } } };

            var parent2 = new Parent25225 { Id = Parent2Id, Collection = new List<Collection25225> { new() { Id = Collection2Id, } } };

            AddRange(parent1, parent2);

            SaveChanges();
        }

        public class Parent25225
        {
            public Guid Id { get; set; }
            public ICollection<Collection25225> Collection { get; set; }
        }

        public class Collection25225
        {
            public Guid Id { get; set; }
            public Guid ParentId { get; set; }
            public Parent25225 Parent { get; set; }
        }
    }

    public class ParentViewModel25225
    {
        public Guid Id { get; set; }
        public ICollection<CollectionViewModel25225> Collection { get; set; }
    }

    public class CollectionViewModel25225
    {
        public Guid Id { get; set; }
        public Guid ParentId { get; set; }
    }

    #endregion

    #region Issue26742

    [ConditionalTheory]
    [InlineData(null, "")]
    //[InlineData(0, " (Scale = 0)")] //https://github.com/dotnet/SqlClient/issues/1380 cause this test to fail, not EF
    [InlineData(1, " (Scale = 1)")]
    [InlineData(2, " (Scale = 2)")]
    [InlineData(3, " (Scale = 3)")]
    [InlineData(4, " (Scale = 4)")]
    [InlineData(5, " (Scale = 5)")]
    [InlineData(6, " (Scale = 6)")]
    [InlineData(7, " (Scale = 7)")]
    public virtual async Task Query_generates_correct_datetime2_parameter_definition(int? fractionalSeconds, string postfix)
    {
        var contextFactory = await InitializeAsync<MyContext_26742>(
            onModelCreating: modelBuilder =>
            {
                if (fractionalSeconds.HasValue)
                {
                    modelBuilder.Entity<MyContext_26742.Entity>().Property(p => p.DateTime).HasPrecision(fractionalSeconds.Value);
                }
            });

        var parameter = new DateTime(2021, 11, 12, 13, 14, 15).AddTicks(1234567);

        using (var context = contextFactory.CreateContext())
        {
            _ = context.Entities.Where(x => x.DateTime == parameter).Select(e => e.DateTime).FirstOrDefault();

            AssertSql(
                $"""
@__parameter_0='2021-11-12T13:14:15.1234567'{postfix}

SELECT TOP(1) [e].[DateTime]
FROM [Entities] AS [e]
WHERE [e].[DateTime] = @__parameter_0
""");
        }
    }

    [ConditionalTheory]
    [InlineData(null, "")]
    //[InlineData(0, " (Scale = 0)")] //https://github.com/dotnet/SqlClient/issues/1380 cause this test to fail, not EF
    [InlineData(1, " (Scale = 1)")]
    [InlineData(2, " (Scale = 2)")]
    [InlineData(3, " (Scale = 3)")]
    [InlineData(4, " (Scale = 4)")]
    [InlineData(5, " (Scale = 5)")]
    [InlineData(6, " (Scale = 6)")]
    [InlineData(7, " (Scale = 7)")]
    public virtual async Task Query_generates_correct_datetimeoffset_parameter_definition(int? fractionalSeconds, string postfix)
    {
        var contextFactory = await InitializeAsync<MyContext_26742>(
            onModelCreating: modelBuilder =>
            {
                if (fractionalSeconds.HasValue)
                {
                    modelBuilder.Entity<MyContext_26742.Entity>().Property(p => p.DateTimeOffset).HasPrecision(fractionalSeconds.Value);
                }
            });

        var parameter = new DateTimeOffset(new DateTime(2021, 11, 12, 13, 14, 15).AddTicks(1234567), TimeSpan.FromHours(10));

        using (var context = contextFactory.CreateContext())
        {
            _ = context.Entities.Where(x => x.DateTimeOffset == parameter).Select(e => e.DateTimeOffset).FirstOrDefault();

            AssertSql(
                $"""
@__parameter_0='2021-11-12T13:14:15.1234567+10:00'{postfix}

SELECT TOP(1) [e].[DateTimeOffset]
FROM [Entities] AS [e]
WHERE [e].[DateTimeOffset] = @__parameter_0
""");
        }
    }

    [ConditionalTheory]
    [InlineData(null, "")]
    //[InlineData(0, " (Scale = 0)")] //https://github.com/dotnet/SqlClient/issues/1380 cause this test to fail, not EF
    [InlineData(1, " (Scale = 1)")]
    [InlineData(2, " (Scale = 2)")]
    [InlineData(3, " (Scale = 3)")]
    [InlineData(4, " (Scale = 4)")]
    [InlineData(5, " (Scale = 5)")]
    [InlineData(6, " (Scale = 6)")]
    [InlineData(7, " (Scale = 7)")]
    public virtual async Task Query_generates_correct_timespan_parameter_definition(int? fractionalSeconds, string postfix)
    {
        var contextFactory = await InitializeAsync<MyContext_26742>(
            onModelCreating: modelBuilder =>
            {
                if (fractionalSeconds.HasValue)
                {
                    modelBuilder.Entity<MyContext_26742.Entity>().Property(p => p.TimeSpan).HasPrecision(fractionalSeconds.Value);
                }
            });

        var parameter = TimeSpan.Parse("12:34:56.7890123", CultureInfo.InvariantCulture);

        using (var context = contextFactory.CreateContext())
        {
            _ = context.Entities.Where(x => x.TimeSpan == parameter).Select(e => e.TimeSpan).FirstOrDefault();

            AssertSql(
                $"""
@__parameter_0='12:34:56.7890123'{postfix}

SELECT TOP(1) [e].[TimeSpan]
FROM [Entities] AS [e]
WHERE [e].[TimeSpan] = @__parameter_0
""");
        }
    }

    protected class MyContext_26742 : DbContext
    {
        public DbSet<Entity> Entities { get; set; }

        public MyContext_26742(DbContextOptions options)
            : base(options)
        {
        }

        public class Entity
        {
            public int Id { get; set; }
            public TimeSpan TimeSpan { get; set; }
            public DateTime DateTime { get; set; }
            public DateTimeOffset DateTimeOffset { get; set; }
        }
    }

    #endregion

    protected override string StoreName
        => "QueryBugsTest";

    protected TestSqlLoggerFactory TestSqlLoggerFactory
        => (TestSqlLoggerFactory)ListLoggerFactory;

    protected override ITestStoreFactory TestStoreFactory
        => SqlServerTestStoreFactory.Instance;

    protected override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
        => base.AddOptions(builder).ConfigureWarnings(
            w =>
            {
                w.Log(SqlServerEventId.ByteIdentityColumnWarning);
                w.Log(SqlServerEventId.DecimalTypeKeyWarning);
            });

    protected override TestStore CreateTestStore()
        => SqlServerTestStore.CreateInitialized(StoreName, multipleActiveResultSets: true);

    private static readonly FieldInfo _querySplittingBehaviorFieldInfo =
        typeof(RelationalOptionsExtension).GetField("_querySplittingBehavior", BindingFlags.NonPublic | BindingFlags.Instance);

    protected DbContextOptionsBuilder ClearQuerySplittingBehavior(DbContextOptionsBuilder optionsBuilder)
    {
        var extension = optionsBuilder.Options.FindExtension<SqlServerOptionsExtension>();
        if (extension == null)
        {
            extension = new SqlServerOptionsExtension();
        }
        else
        {
            _querySplittingBehaviorFieldInfo.SetValue(extension, null);
        }

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

        return optionsBuilder;
    }

    protected void ClearLog()
        => TestSqlLoggerFactory.Clear();

    protected void AssertSql(params string[] expected)
        => TestSqlLoggerFactory.AssertBaseline(expected);
}
