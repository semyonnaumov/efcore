// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ReSharper disable InconsistentNaming

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using NetTopologySuite.Geometries;

namespace Microsoft.EntityFrameworkCore.Query;

#nullable disable

public class AdHocMiscellaneousQuerySqlServerTest : AdHocMiscellaneousQueryRelationalTestBase
{
    protected override ITestStoreFactory TestStoreFactory
        => SqlServerTestStoreFactory.Instance;

    protected override Task Seed2951(Context2951 context)
        => context.Database.ExecuteSqlRawAsync(
            """
CREATE TABLE ZeroKey (Id int);
INSERT ZeroKey VALUES (NULL)
""");

    #region 5456

    [ConditionalFact]
    public virtual async Task Include_group_join_is_per_query_context()
    {
        var contextFactory = await InitializeAsync<Context5456>(
            seed: c => c.SeedAsync(),
            createTestStore: async () => await SqlServerTestStore.CreateInitializedAsync(StoreName, multipleActiveResultSets: true));

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
    public virtual async Task Include_group_join_is_per_query_context_async()
    {
        var contextFactory = await InitializeAsync<Context5456>(
            seed: c => c.SeedAsync(),
            createTestStore: async () => await SqlServerTestStore.CreateInitializedAsync(StoreName, multipleActiveResultSets: true));

        await Parallel.ForAsync(
            0, 10, async (i, ct) =>
            {
                using var ctx = contextFactory.CreateContext();
                var result = await ctx.Posts.Where(x => x.Blog.Id > 1).Include(x => x.Blog).ToListAsync();

                Assert.Equal(198, result.Count);
            });

        await Parallel.ForAsync(
            0, 10, async (i, ct) =>
            {
                using var ctx = contextFactory.CreateContext();
                var result = await ctx.Posts.Where(x => x.Blog.Id > 1).Include(x => x.Blog).Include(x => x.Comments)
                    .ToListAsync();

                Assert.Equal(198, result.Count);
            });

        await Parallel.ForAsync(
            0, 10, async (i, ct) =>
            {
                using var ctx = contextFactory.CreateContext();
                var result = await ctx.Posts.Where(x => x.Blog.Id > 1).Include(x => x.Blog).ThenInclude(b => b.Author)
                    .ToListAsync();

                Assert.Equal(198, result.Count);
            });
    }

    private class Context5456(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Author> Authors { get; set; }

        public Task SeedAsync()
        {
            for (var i = 0; i < 100; i++)
            {
                Add(
                    new Blog { Posts = [new() { Comments = [new(), new()] }, new()], Author = new Author() });
            }

            return SaveChangesAsync();
        }

        public class Blog
        {
            public int Id { get; set; }
            public List<Post> Posts { get; set; }
            public Author Author { get; set; }
        }

        public class Author
        {
            public int Id { get; set; }
            public List<Blog> Blogs { get; set; }
        }

        public class Post
        {
            public int Id { get; set; }
            public Blog Blog { get; set; }
            public List<Comment> Comments { get; set; }
        }

        public class Comment
        {
            public int Id { get; set; }
            public Post Blog { get; set; }
        }
    }

    #endregion

    #region 8864

    [ConditionalFact]
    public virtual async Task Select_nested_projection()
    {
        var contextFactory = await InitializeAsync<Context8864>(seed: c => c.SeedAsync());

        using (var context = contextFactory.CreateContext())
        {
            var customers = context.Customers
                .Select(c => new { Customer = c, CustomerAgain = Context8864.Get(context, c.Id) })
                .ToList();

            Assert.Equal(2, customers.Count);

            foreach (var customer in customers)
            {
                Assert.Same(customer.Customer, customer.CustomerAgain);
            }
        }

        AssertSql(
            """
SELECT [c].[Id], [c].[Name]
FROM [Customers] AS [c]
""",
            //
            """
@__id_0='1'

SELECT TOP(2) [c].[Id], [c].[Name]
FROM [Customers] AS [c]
WHERE [c].[Id] = @__id_0
""",
            //
            """
@__id_0='2'

SELECT TOP(2) [c].[Id], [c].[Name]
FROM [Customers] AS [c]
WHERE [c].[Id] = @__id_0
""");
    }

    private class Context8864(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Customer> Customers { get; set; }

        public Task SeedAsync()
        {
            AddRange(
                new Customer { Name = "Alan" },
                new Customer { Name = "Elon" });

            return SaveChangesAsync();
        }

        public static Customer Get(Context8864 context, int id)
            => context.Customers.Single(c => c.Id == id);

        public class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }

    #endregion

    #region 9214

    [ConditionalFact]
    public async Task Default_schema_applied_when_no_function_schema()
    {
        var contextFactory = await InitializeAsync<Context9214>(seed: c => c.SeedAsync());

        using (var context = contextFactory.CreateContext())
        {
            var result = context.Widgets.Where(w => w.Val == 1).Select(w => Context9214.AddOne(w.Val)).Single();

            Assert.Equal(2, result);

            AssertSql(
                """
SELECT TOP(2) [foo].[AddOne]([w].[Val])
FROM [foo].[Widgets] AS [w]
WHERE [w].[Val] = 1
""");
        }

        using (var context = contextFactory.CreateContext())
        {
            ClearLog();
            var result = context.Widgets.Where(w => w.Val == 1).Select(w => Context9214.AddTwo(w.Val)).Single();

            Assert.Equal(3, result);

            AssertSql(
                """
SELECT TOP(2) [dbo].[AddTwo]([w].[Val])
FROM [foo].[Widgets] AS [w]
WHERE [w].[Val] = 1
""");
        }
    }

    protected class Context9214(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Widget9214> Widgets { get; set; }

#pragma warning disable IDE0060 // Remove unused parameter
        public static int AddOne(int num)
            => throw new Exception();

        public static int AddTwo(int num)
            => throw new Exception();

        public static int AddThree(int num)
            => throw new Exception();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("foo");

            modelBuilder.Entity<Widget9214>().ToTable("Widgets", "foo");

            modelBuilder.HasDbFunction(typeof(Context9214).GetMethod(nameof(AddOne)));
            modelBuilder.HasDbFunction(typeof(Context9214).GetMethod(nameof(AddTwo))).HasSchema("dbo");
        }

        public async Task SeedAsync()
        {
            var w1 = new Widget9214 { Val = 1 };
            var w2 = new Widget9214 { Val = 2 };
            var w3 = new Widget9214 { Val = 3 };
            Widgets.AddRange(w1, w2, w3);
            await SaveChangesAsync();

            await Database.ExecuteSqlRawAsync(
                """
CREATE FUNCTION foo.AddOne (@num int)
RETURNS int
    AS
BEGIN
    return @num + 1 ;
END
""");


            await Database.ExecuteSqlRawAsync(
                """
CREATE FUNCTION dbo.AddTwo (@num int)
RETURNS int
    AS
BEGIN
    return @num + 2 ;
END
""");
        }

        public class Widget9214
        {
            public int Id { get; set; }
            public int Val { get; set; }
        }
    }

    #endregion

    #region 9277

    [ConditionalFact]
    public virtual async Task From_sql_gets_value_of_out_parameter_in_stored_procedure()
    {
        var contextFactory = await InitializeAsync<Context9277>(seed: c => c.SeedAsync());

        using (var context = contextFactory.CreateContext())
        {
            var valueParam = new SqlParameter
            {
                ParameterName = "Value",
                Value = 0,
                Direction = ParameterDirection.Output,
                SqlDbType = SqlDbType.Int
            };

            Assert.Equal(0, valueParam.Value);

            var blogs = context.Blogs.FromSqlRaw(
                    "[dbo].[GetPersonAndVoteCount]  @id, @Value out",
                    new SqlParameter { ParameterName = "id", Value = 1 },
                    valueParam)
                .ToList();

            Assert.Single(blogs);
            Assert.Equal(1, valueParam.Value);
        }
    }

    protected class Context9277(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Blog9277> Blogs { get; set; }

        public async Task SeedAsync()
        {
            await Database.ExecuteSqlRawAsync(
                """
CREATE PROCEDURE [dbo].[GetPersonAndVoteCount]
 (
    @id int,
    @Value int OUTPUT
)
AS
BEGIN
    SELECT @Value = SomeValue
    FROM dbo.Blogs
    WHERE Id = @id;
    SELECT *
    FROM dbo.Blogs
    WHERE Id = @id;
    END
""");

            AddRange(
                new Blog9277 { SomeValue = 1 },
                new Blog9277 { SomeValue = 2 },
                new Blog9277 { SomeValue = 3 }
            );

            await SaveChangesAsync();
        }

        public class Blog9277
        {
            public int Id { get; set; }
            public int SomeValue { get; set; }
        }
    }

    #endregion

    #region 12482

    [ConditionalFact]
    public virtual async Task Batch_insert_with_sqlvariant_different_types()
    {
        var contextFactory = await InitializeAsync<Context12482>();

        using (var context = contextFactory.CreateContext())
        {
            context.AddRange(
                new Context12482.BaseEntity { Value = 10.0999 },
                new Context12482.BaseEntity { Value = -12345 },
                new Context12482.BaseEntity { Value = "String Value" },
                new Context12482.BaseEntity { Value = new DateTime(2020, 1, 1) });

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

    private class Context12482(DbContextOptions options) : DbContext(options)
    {
        public virtual DbSet<BaseEntity> BaseEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<BaseEntity>();

        public class BaseEntity
        {
            public int Id { get; set; }

            [Column(TypeName = "sql_variant")]
            public object Value { get; set; }
        }
    }

    #endregion

    #region 12518

    [ConditionalFact]
    public virtual async Task Projecting_entity_with_value_converter_and_include_works()
    {
        var contextFactory = await InitializeAsync<Context12518>(seed: c => c.SeedAsync());
        using var context = contextFactory.CreateContext();
        var result = context.Parents.Include(p => p.Child).OrderBy(e => e.Id).FirstOrDefault();

        AssertSql(
            """
SELECT TOP(1) [p].[Id], [p].[ChildId], [c].[Id], [c].[ParentId], [c].[ULongRowVersion]
FROM [Parents] AS [p]
LEFT JOIN [Children] AS [c] ON [p].[ChildId] = [c].[Id]
ORDER BY [p].[Id]
""");
    }

    [ConditionalFact]
    public virtual async Task Projecting_column_with_value_converter_of_ulong_byte_array()
    {
        var contextFactory = await InitializeAsync<Context12518>(seed: c => c.SeedAsync());
        using var context = contextFactory.CreateContext();
        var result = context.Parents.OrderBy(e => e.Id).Select(p => (ulong?)p.Child.ULongRowVersion).FirstOrDefault();

        AssertSql(
            """
SELECT TOP(1) [c].[ULongRowVersion]
FROM [Parents] AS [p]
LEFT JOIN [Children] AS [c] ON [p].[ChildId] = [c].[Id]
ORDER BY [p].[Id]
""");
    }

    protected class Context12518(DbContextOptions options) : DbContext(options)
    {
        public virtual DbSet<Parent12518> Parents { get; set; }
        public virtual DbSet<Child12518> Children { get; set; }

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

        public Task SeedAsync()
        {
            Parents.Add(new Parent12518());
            return SaveChangesAsync();
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

    #region 13118

    [ConditionalFact]
    public virtual async Task DateTime_Contains_with_smalldatetime_generates_correct_literal()
    {
        var contextFactory = await InitializeAsync<Context13118>(seed: c => c.SeedAsync());
        using var context = contextFactory.CreateContext();
        var testDateList = new List<DateTime> { new(2018, 10, 07) };
        var findRecordsWithDateInList = context.ReproEntity
            .Where(a => testDateList.Contains(a.MyTime))
            .ToList();

        Assert.Single(findRecordsWithDateInList);

        AssertSql(
            """
@__testDateList_0='["2018-10-07T00:00:00"]' (Size = 4000)

SELECT [r].[Id], [r].[MyTime]
FROM [ReproEntity] AS [r]
WHERE [r].[MyTime] IN (
    SELECT [t].[value]
    FROM OPENJSON(@__testDateList_0) WITH ([value] smalldatetime '$') AS [t]
)
""");
    }

    private class Context13118(DbContextOptions options) : DbContext(options)
    {
        public virtual DbSet<ReproEntity13118> ReproEntity { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<ReproEntity13118>(e => e.Property("MyTime").HasColumnType("smalldatetime"));

        public Task SeedAsync()
        {
            AddRange(
                new ReproEntity13118 { MyTime = new DateTime(2018, 10, 07) },
                new ReproEntity13118 { MyTime = new DateTime(2018, 10, 08) });

            return SaveChangesAsync();
        }
    }

    private class ReproEntity13118
    {
        public Guid Id { get; set; }
        public DateTime MyTime { get; set; }
    }

    #endregion

    #region 14095

    [ConditionalTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Where_equals_DateTime_Now(bool async)
    {
        var contextFactory = await InitializeAsync<Context14095>(seed: c => c.SeedAsync());

        using var context = contextFactory.CreateContext();
        var query = context.Dates.Where(
            d => d.DateTime2_2 == DateTime.Now
                || d.DateTime2_7 == DateTime.Now
                || d.DateTime == DateTime.Now
                || d.SmallDateTime == DateTime.Now);

        var results = async
            ? await query.ToListAsync()
            : query.ToList();

        Assert.Empty(results);

        AssertSql(
            """
SELECT [d].[Id], [d].[DateTime], [d].[DateTime2], [d].[DateTime2_0], [d].[DateTime2_1], [d].[DateTime2_2], [d].[DateTime2_3], [d].[DateTime2_4], [d].[DateTime2_5], [d].[DateTime2_6], [d].[DateTime2_7], [d].[SmallDateTime]
FROM [Dates] AS [d]
WHERE [d].[DateTime2_2] = GETDATE() OR [d].[DateTime2_7] = GETDATE() OR [d].[DateTime] = GETDATE() OR [d].[SmallDateTime] = GETDATE()
""");
    }

    [ConditionalTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Where_not_equals_DateTime_Now(bool async)
    {
        var contextFactory = await InitializeAsync<Context14095>(seed: c => c.SeedAsync());

        using var context = contextFactory.CreateContext();
        var query = context.Dates.Where(
            d => d.DateTime2_2 != DateTime.Now
                && d.DateTime2_7 != DateTime.Now
                && d.DateTime != DateTime.Now
                && d.SmallDateTime != DateTime.Now);

        var results = async
            ? await query.ToListAsync()
            : query.ToList();

        Assert.Single(results);

        AssertSql(
            """
SELECT [d].[Id], [d].[DateTime], [d].[DateTime2], [d].[DateTime2_0], [d].[DateTime2_1], [d].[DateTime2_2], [d].[DateTime2_3], [d].[DateTime2_4], [d].[DateTime2_5], [d].[DateTime2_6], [d].[DateTime2_7], [d].[SmallDateTime]
FROM [Dates] AS [d]
WHERE [d].[DateTime2_2] <> GETDATE() AND [d].[DateTime2_7] <> GETDATE() AND [d].[DateTime] <> GETDATE() AND [d].[SmallDateTime] <> GETDATE()
""");
    }

    [ConditionalTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Where_equals_new_DateTime(bool async)
    {
        var contextFactory = await InitializeAsync<Context14095>(seed: c => c.SeedAsync());

        using var context = contextFactory.CreateContext();
        var query = context.Dates.Where(
            d => d.SmallDateTime == new DateTime(1970, 9, 3, 12, 0, 0)
                && d.DateTime == new DateTime(1971, 9, 3, 12, 0, 10, 220)
                && d.DateTime2 == new DateTime(1972, 9, 3, 12, 0, 10, 333)
                && d.DateTime2_0 == new DateTime(1973, 9, 3, 12, 0, 10)
                && d.DateTime2_1 == new DateTime(1974, 9, 3, 12, 0, 10, 500)
                && d.DateTime2_2 == new DateTime(1975, 9, 3, 12, 0, 10, 660)
                && d.DateTime2_3 == new DateTime(1976, 9, 3, 12, 0, 10, 777)
                && d.DateTime2_4 == new DateTime(1977, 9, 3, 12, 0, 10, 888)
                && d.DateTime2_5 == new DateTime(1978, 9, 3, 12, 0, 10, 999)
                && d.DateTime2_6 == new DateTime(1979, 9, 3, 12, 0, 10, 111)
                && d.DateTime2_7 == new DateTime(1980, 9, 3, 12, 0, 10, 222));

        var results = async
            ? await query.ToListAsync()
            : query.ToList();

        Assert.Single(results);

        AssertSql(
            """
SELECT [d].[Id], [d].[DateTime], [d].[DateTime2], [d].[DateTime2_0], [d].[DateTime2_1], [d].[DateTime2_2], [d].[DateTime2_3], [d].[DateTime2_4], [d].[DateTime2_5], [d].[DateTime2_6], [d].[DateTime2_7], [d].[SmallDateTime]
FROM [Dates] AS [d]
WHERE [d].[SmallDateTime] = '1970-09-03T12:00:00' AND [d].[DateTime] = '1971-09-03T12:00:10.220' AND [d].[DateTime2] = '1972-09-03T12:00:10.3330000' AND [d].[DateTime2_0] = '1973-09-03T12:00:10' AND [d].[DateTime2_1] = '1974-09-03T12:00:10.5' AND [d].[DateTime2_2] = '1975-09-03T12:00:10.66' AND [d].[DateTime2_3] = '1976-09-03T12:00:10.777' AND [d].[DateTime2_4] = '1977-09-03T12:00:10.8880' AND [d].[DateTime2_5] = '1978-09-03T12:00:10.99900' AND [d].[DateTime2_6] = '1979-09-03T12:00:10.111000' AND [d].[DateTime2_7] = '1980-09-03T12:00:10.2220000'
""");
    }

    [ConditionalTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Where_contains_DateTime_literals(bool async)
    {
        var dateTimes = new[]
        {
            new DateTime(1970, 9, 3, 12, 0, 0),
            new DateTime(1971, 9, 3, 12, 0, 10, 220),
            new DateTime(1972, 9, 3, 12, 0, 10, 333),
            new DateTime(1973, 9, 3, 12, 0, 10),
            new DateTime(1974, 9, 3, 12, 0, 10, 500),
            new DateTime(1975, 9, 3, 12, 0, 10, 660),
            new DateTime(1976, 9, 3, 12, 0, 10, 777),
            new DateTime(1977, 9, 3, 12, 0, 10, 888),
            new DateTime(1978, 9, 3, 12, 0, 10, 999),
            new DateTime(1979, 9, 3, 12, 0, 10, 111),
            new DateTime(1980, 9, 3, 12, 0, 10, 222)
        };

        var contextFactory = await InitializeAsync<Context14095>(seed: c => c.SeedAsync());

        using var context = contextFactory.CreateContext();
        var query = context.Dates.Where(
            d => dateTimes.Contains(d.SmallDateTime)
                && dateTimes.Contains(d.DateTime)
                && dateTimes.Contains(d.DateTime2)
                && dateTimes.Contains(d.DateTime2_0)
                && dateTimes.Contains(d.DateTime2_1)
                && dateTimes.Contains(d.DateTime2_2)
                && dateTimes.Contains(d.DateTime2_3)
                && dateTimes.Contains(d.DateTime2_4)
                && dateTimes.Contains(d.DateTime2_5)
                && dateTimes.Contains(d.DateTime2_6)
                && dateTimes.Contains(d.DateTime2_7));

        var results = async
            ? await query.ToListAsync()
            : query.ToList();

        Assert.Single(results);

        AssertSql(
            """
@__dateTimes_0='["1970-09-03T12:00:00","1971-09-03T12:00:10.22","1972-09-03T12:00:10.333","1973-09-03T12:00:10","1974-09-03T12:00:10.5","1975-09-03T12:00:10.66","1976-09-03T12:00:10.777","1977-09-03T12:00:10.888","1978-09-03T12:00:10.999","1979-09-03T12:00:10.111","1980-09-03T12:00:10.222"]' (Size = 4000)
@__dateTimes_0_1='["1970-09-03T12:00:00","1971-09-03T12:00:10.22","1972-09-03T12:00:10.333","1973-09-03T12:00:10","1974-09-03T12:00:10.5","1975-09-03T12:00:10.66","1976-09-03T12:00:10.777","1977-09-03T12:00:10.888","1978-09-03T12:00:10.999","1979-09-03T12:00:10.111","1980-09-03T12:00:10.222"]' (Size = 4000)
@__dateTimes_0_2='["1970-09-03T12:00:00","1971-09-03T12:00:10.22","1972-09-03T12:00:10.333","1973-09-03T12:00:10","1974-09-03T12:00:10.5","1975-09-03T12:00:10.66","1976-09-03T12:00:10.777","1977-09-03T12:00:10.888","1978-09-03T12:00:10.999","1979-09-03T12:00:10.111","1980-09-03T12:00:10.222"]' (Size = 4000)
@__dateTimes_0_3='["1970-09-03T12:00:00","1971-09-03T12:00:10.22","1972-09-03T12:00:10.333","1973-09-03T12:00:10","1974-09-03T12:00:10.5","1975-09-03T12:00:10.66","1976-09-03T12:00:10.777","1977-09-03T12:00:10.888","1978-09-03T12:00:10.999","1979-09-03T12:00:10.111","1980-09-03T12:00:10.222"]' (Size = 4000)
@__dateTimes_0_4='["1970-09-03T12:00:00","1971-09-03T12:00:10.22","1972-09-03T12:00:10.333","1973-09-03T12:00:10","1974-09-03T12:00:10.5","1975-09-03T12:00:10.66","1976-09-03T12:00:10.777","1977-09-03T12:00:10.888","1978-09-03T12:00:10.999","1979-09-03T12:00:10.111","1980-09-03T12:00:10.222"]' (Size = 4000)
@__dateTimes_0_5='["1970-09-03T12:00:00","1971-09-03T12:00:10.22","1972-09-03T12:00:10.333","1973-09-03T12:00:10","1974-09-03T12:00:10.5","1975-09-03T12:00:10.66","1976-09-03T12:00:10.777","1977-09-03T12:00:10.888","1978-09-03T12:00:10.999","1979-09-03T12:00:10.111","1980-09-03T12:00:10.222"]' (Size = 4000)
@__dateTimes_0_6='["1970-09-03T12:00:00","1971-09-03T12:00:10.22","1972-09-03T12:00:10.333","1973-09-03T12:00:10","1974-09-03T12:00:10.5","1975-09-03T12:00:10.66","1976-09-03T12:00:10.777","1977-09-03T12:00:10.888","1978-09-03T12:00:10.999","1979-09-03T12:00:10.111","1980-09-03T12:00:10.222"]' (Size = 4000)
@__dateTimes_0_7='["1970-09-03T12:00:00","1971-09-03T12:00:10.22","1972-09-03T12:00:10.333","1973-09-03T12:00:10","1974-09-03T12:00:10.5","1975-09-03T12:00:10.66","1976-09-03T12:00:10.777","1977-09-03T12:00:10.888","1978-09-03T12:00:10.999","1979-09-03T12:00:10.111","1980-09-03T12:00:10.222"]' (Size = 4000)
@__dateTimes_0_8='["1970-09-03T12:00:00","1971-09-03T12:00:10.22","1972-09-03T12:00:10.333","1973-09-03T12:00:10","1974-09-03T12:00:10.5","1975-09-03T12:00:10.66","1976-09-03T12:00:10.777","1977-09-03T12:00:10.888","1978-09-03T12:00:10.999","1979-09-03T12:00:10.111","1980-09-03T12:00:10.222"]' (Size = 4000)
@__dateTimes_0_9='["1970-09-03T12:00:00","1971-09-03T12:00:10.22","1972-09-03T12:00:10.333","1973-09-03T12:00:10","1974-09-03T12:00:10.5","1975-09-03T12:00:10.66","1976-09-03T12:00:10.777","1977-09-03T12:00:10.888","1978-09-03T12:00:10.999","1979-09-03T12:00:10.111","1980-09-03T12:00:10.222"]' (Size = 4000)
@__dateTimes_0_10='["1970-09-03T12:00:00","1971-09-03T12:00:10.22","1972-09-03T12:00:10.333","1973-09-03T12:00:10","1974-09-03T12:00:10.5","1975-09-03T12:00:10.66","1976-09-03T12:00:10.777","1977-09-03T12:00:10.888","1978-09-03T12:00:10.999","1979-09-03T12:00:10.111","1980-09-03T12:00:10.222"]' (Size = 4000)

SELECT [d].[Id], [d].[DateTime], [d].[DateTime2], [d].[DateTime2_0], [d].[DateTime2_1], [d].[DateTime2_2], [d].[DateTime2_3], [d].[DateTime2_4], [d].[DateTime2_5], [d].[DateTime2_6], [d].[DateTime2_7], [d].[SmallDateTime]
FROM [Dates] AS [d]
WHERE [d].[SmallDateTime] IN (
    SELECT [d0].[value]
    FROM OPENJSON(@__dateTimes_0) WITH ([value] smalldatetime '$') AS [d0]
) AND [d].[DateTime] IN (
    SELECT [d1].[value]
    FROM OPENJSON(@__dateTimes_0_1) WITH ([value] datetime '$') AS [d1]
) AND [d].[DateTime2] IN (
    SELECT [d2].[value]
    FROM OPENJSON(@__dateTimes_0_2) WITH ([value] datetime2 '$') AS [d2]
) AND [d].[DateTime2_0] IN (
    SELECT [d3].[value]
    FROM OPENJSON(@__dateTimes_0_3) WITH ([value] datetime2(0) '$') AS [d3]
) AND [d].[DateTime2_1] IN (
    SELECT [d4].[value]
    FROM OPENJSON(@__dateTimes_0_4) WITH ([value] datetime2(1) '$') AS [d4]
) AND [d].[DateTime2_2] IN (
    SELECT [d5].[value]
    FROM OPENJSON(@__dateTimes_0_5) WITH ([value] datetime2(2) '$') AS [d5]
) AND [d].[DateTime2_3] IN (
    SELECT [d6].[value]
    FROM OPENJSON(@__dateTimes_0_6) WITH ([value] datetime2(3) '$') AS [d6]
) AND [d].[DateTime2_4] IN (
    SELECT [d7].[value]
    FROM OPENJSON(@__dateTimes_0_7) WITH ([value] datetime2(4) '$') AS [d7]
) AND [d].[DateTime2_5] IN (
    SELECT [d8].[value]
    FROM OPENJSON(@__dateTimes_0_8) WITH ([value] datetime2(5) '$') AS [d8]
) AND [d].[DateTime2_6] IN (
    SELECT [d9].[value]
    FROM OPENJSON(@__dateTimes_0_9) WITH ([value] datetime2(6) '$') AS [d9]
) AND [d].[DateTime2_7] IN (
    SELECT [d10].[value]
    FROM OPENJSON(@__dateTimes_0_10) WITH ([value] datetime2(7) '$') AS [d10]
)
""");
    }

    protected class Context14095(DbContextOptions options) : DbContext(options)
    {
        public DbSet<DatesAndPrunes14095> Dates { get; set; }

        public Task SeedAsync()
        {
            Add(
                new DatesAndPrunes14095
                {
                    SmallDateTime = new DateTime(1970, 9, 3, 12, 0, 0),
                    DateTime = new DateTime(1971, 9, 3, 12, 0, 10, 220),
                    DateTime2 = new DateTime(1972, 9, 3, 12, 0, 10, 333),
                    DateTime2_0 = new DateTime(1973, 9, 3, 12, 0, 10),
                    DateTime2_1 = new DateTime(1974, 9, 3, 12, 0, 10, 500),
                    DateTime2_2 = new DateTime(1975, 9, 3, 12, 0, 10, 660),
                    DateTime2_3 = new DateTime(1976, 9, 3, 12, 0, 10, 777),
                    DateTime2_4 = new DateTime(1977, 9, 3, 12, 0, 10, 888),
                    DateTime2_5 = new DateTime(1978, 9, 3, 12, 0, 10, 999),
                    DateTime2_6 = new DateTime(1979, 9, 3, 12, 0, 10, 111),
                    DateTime2_7 = new DateTime(1980, 9, 3, 12, 0, 10, 222)
                });
            return SaveChangesAsync();
        }

        public class DatesAndPrunes14095
        {
            public int Id { get; set; }

            [Column(TypeName = "smalldatetime")]
            public DateTime SmallDateTime { get; set; }

            [Column(TypeName = "datetime")]
            public DateTime DateTime { get; set; }

            [Column(TypeName = "datetime2")]
            public DateTime DateTime2 { get; set; }

            [Column(TypeName = "datetime2(0)")]
            public DateTime DateTime2_0 { get; set; }

            [Column(TypeName = "datetime2(1)")]
            public DateTime DateTime2_1 { get; set; }

            [Column(TypeName = "datetime2(2)")]
            public DateTime DateTime2_2 { get; set; }

            [Column(TypeName = "datetime2(3)")]
            public DateTime DateTime2_3 { get; set; }

            [Column(TypeName = "datetime2(4)")]
            public DateTime DateTime2_4 { get; set; }

            [Column(TypeName = "datetime2(5)")]
            public DateTime DateTime2_5 { get; set; }

            [Column(TypeName = "datetime2(6)")]
            public DateTime DateTime2_6 { get; set; }

            [Column(TypeName = "datetime2(7)")]
            public DateTime DateTime2_7 { get; set; }
        }
    }

    #endregion

    #region 15518

    [ConditionalTheory]
    [InlineData(false)]
    [InlineData(true)]
    public virtual async Task Nested_queries_does_not_cause_concurrency_exception_sync(bool tracking)
    {
        var contextFactory = await InitializeAsync<Context15518>(seed: c => c.SeedAsync());

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

        AssertSql(
            """
SELECT [r].[Id], [r].[Name]
FROM [Repos] AS [r]
WHERE [r].[Id] > 0
ORDER BY [r].[Id]
""",
            //
            """
SELECT [r].[Id], [r].[Name]
FROM [Repos] AS [r]
WHERE [r].[Id] > 0
ORDER BY [r].[Id]
""",
            //
            """
SELECT [r].[Id], [r].[Name]
FROM [Repos] AS [r]
WHERE [r].[Id] > 0
ORDER BY [r].[Id]
""",
            //
            """
SELECT [r].[Id], [r].[Name]
FROM [Repos] AS [r]
WHERE [r].[Id] > 0
ORDER BY [r].[Id]
""",
            //
            """
SELECT [r].[Id], [r].[Name]
FROM [Repos] AS [r]
WHERE [r].[Id] > 0
ORDER BY [r].[Id]
""",
            //
            """
SELECT [r].[Id], [r].[Name]
FROM [Repos] AS [r]
WHERE [r].[Id] > 0
ORDER BY [r].[Id]
""");
    }

    private class Context15518(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Repo> Repos { get; set; }

        public Task SeedAsync()
        {
            AddRange(
                new Repo { Name = "London" },
                new Repo { Name = "New York" });

            return SaveChangesAsync();
        }

        public class Repo
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }

    #endregion

    #region 19206

    [ConditionalFact]
    public virtual async Task From_sql_expression_compares_correctly()
    {
        var contextFactory = await InitializeAsync<Context19206>(seed: c => c.SeedAsync());

        using (var context = contextFactory.CreateContext())
        {
            var query = from t1 in context.Tests.FromSqlInterpolated(
                            $"Select * from Tests Where Type = {Context19206.TestType19206.Unit}")
                        from t2 in context.Tests.FromSqlInterpolated(
                            $"Select * from Tests Where Type = {Context19206.TestType19206.Integration}")
                        select new { t1, t2 };

            var result = query.ToList();

            var item = Assert.Single(result);
            Assert.Equal(Context19206.TestType19206.Unit, item.t1.Type);
            Assert.Equal(Context19206.TestType19206.Integration, item.t2.Type);

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

    private class Context19206(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Test> Tests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        public Task SeedAsync()
        {
            Add(new Test { Type = TestType19206.Unit });
            Add(new Test { Type = TestType19206.Integration });
            return SaveChangesAsync();
        }

        public class Test
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

    #region 21666

    [ConditionalFact]
    public virtual async Task Thread_safety_in_relational_command_cache()
    {
        var contextFactory = await InitializeAsync<Context21666>(
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

    private class Context21666(DbContextOptions options) : DbContext(options)
    {
        public DbSet<List> Lists { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        public class List
        {
            public int Id { get; set; }
            public bool IsDeleted { get; set; }
        }
    }

    #endregion

    #region 23282

    [ConditionalFact]
    [SqlServerCondition(SqlServerCondition.SupportsSqlClr)]
    public virtual async Task Can_query_point_with_buffered_data_reader()
    {
        var contextFactory = await InitializeAsync<Context23282>(
            seed: c => c.SeedAsync(),
            onConfiguring: o => new SqlServerDbContextOptionsBuilder(o).UseNetTopologySuite(),
            addServices: c => c.AddEntityFrameworkSqlServerNetTopologySuite());

        using var context = contextFactory.CreateContext();
        var testUser = context.Locations.FirstOrDefault(x => x.Name == "My Location");

        Assert.NotNull(testUser);

        AssertSql(
            """
SELECT TOP(1) [l].[Id], [l].[Name], [l].[Address_County], [l].[Address_Line1], [l].[Address_Line2], [l].[Address_Point], [l].[Address_Postcode], [l].[Address_Town], [l].[Address_Value]
FROM [Locations] AS [l]
WHERE [l].[Name] = N'My Location'
""");
    }

    private class Context23282(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Location> Locations { get; set; }

        public Task SeedAsync()
        {
            Locations.Add(
                new Location
                {
                    Name = "My Location",
                    Address = new Address
                    {
                        Line1 = "1 Fake Street",
                        Town = "Fake Town",
                        County = "Fakeshire",
                        Postcode = "PO57 0DE",
                        Point = new Point(115.7930, 37.2431) { SRID = 4326 }
                    }
                });
            return SaveChangesAsync();
        }

        [Owned]
        public class Address
        {
            public string Line1 { get; set; }
            public string Line2 { get; set; }
            public string Town { get; set; }
            public string County { get; set; }
            public string Postcode { get; set; }
            public int Value { get; set; }

            public Point Point { get; set; }
        }

        public class Location
        {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public Guid Id { get; set; }

            public string Name { get; set; }
            public Address Address { get; set; }
        }
    }

    #endregion

    #region 24216

    [ConditionalFact]
    public virtual async Task Subquery_take_SelectMany_with_TVF()
    {
        var contextFactory = await InitializeAsync<Context24216>();
        using var context = contextFactory.CreateContext();

        context.Database.ExecuteSqlRaw(
            """
create function [dbo].[GetPersonStatusAsOf] (@personId bigint, @timestamp datetime2)
returns @personStatus table
(
    Id bigint not null,
    PersonId bigint not null,
    GenderId bigint not null,
    StatusMessage nvarchar(max)
)
as
begin
    insert into @personStatus
    select [m].[Id], [m].[PersonId], [m].[PersonId], null
    from [Message] as [m]
    where [m].[PersonId] = @personId and [m].[TimeStamp] = @timestamp
    return
end
""");

        ClearLog();

        var q = from m in context.Message
                orderby m.Id
                select m;

        var q2 =
            from m in q.Take(10)
            from asof in context.GetPersonStatusAsOf(m.PersonId, m.Timestamp)
            select new { Gender = (from g in context.Gender where g.Id == asof.GenderId select g.Description).Single() };

        q2.ToList();

        AssertSql(
            """
@__p_0='10'

SELECT (
    SELECT TOP(1) [g0].[Description]
    FROM [Gender] AS [g0]
    WHERE [g0].[Id] = [g].[GenderId]) AS [Gender]
FROM (
    SELECT TOP(@__p_0) [m].[Id], [m].[PersonId], [m].[Timestamp]
    FROM [Message] AS [m]
    ORDER BY [m].[Id]
) AS [m0]
CROSS APPLY [dbo].[GetPersonStatusAsOf]([m0].[PersonId], [m0].[Timestamp]) AS [g]
ORDER BY [m0].[Id]
""");
    }

    private class Gender24216
    {
        public long Id { get; set; }

        public string Description { get; set; }
    }

    private class Message24216
    {
        public long Id { get; set; }

        public long PersonId { get; set; }

        public DateTime Timestamp { get; set; }
    }

    private class PersonStatus24216
    {
        public long Id { get; set; }

        public long PersonId { get; set; }

        public long GenderId { get; set; }

        public string StatusMessage { get; set; }
    }

    private class Context24216(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Gender24216> Gender { get; set; }

        public DbSet<Message24216> Message { get; set; }

        public IQueryable<PersonStatus24216> GetPersonStatusAsOf(long personId, DateTime asOf)
            => FromExpression(() => GetPersonStatusAsOf(personId, asOf));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDbFunction(
                typeof(Context24216).GetMethod(
                    nameof(GetPersonStatusAsOf),
                    [typeof(long), typeof(DateTime)]));
        }
    }

    #endregion

    #region 27427

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Muliple_occurrences_of_FromSql_in_group_by_aggregate(bool async)
    {
        var contextFactory = await InitializeAsync<Context27427>();
        using var context = contextFactory.CreateContext();
        var query = context.DemoEntities
            .FromSqlRaw("SELECT * FROM DemoEntities WHERE Id = {0}", new SqlParameter { Value = 1 })
            .Select(e => e.Id);

        var query2 = context.DemoEntities
            .Where(e => query.Contains(e.Id))
            .GroupBy(e => e.Id)
            .Select(g => new { g.Key, Aggregate = g.Count() });

        if (async)
        {
            await query2.ToListAsync();
        }
        else
        {
            query2.ToList();
        }

        AssertSql(
            """
p0='1'

SELECT [d].[Id] AS [Key], COUNT(*) AS [Aggregate]
FROM [DemoEntities] AS [d]
WHERE [d].[Id] IN (
    SELECT [m].[Id]
    FROM (
        SELECT * FROM DemoEntities WHERE Id = @p0
    ) AS [m]
)
GROUP BY [d].[Id]
""");
    }

    protected class Context27427(DbContextOptions options) : DbContext(options)
    {
        public DbSet<DemoEntity> DemoEntities { get; set; }
    }

    protected class DemoEntity
    {
        public int Id { get; set; }
    }

    #endregion

    #region 30478

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task TemporalAsOf_with_json_basic_query(bool async)
    {
        var contextFactory = await InitializeAsync<Context30478>(seed: x => x.SeedAsync());
        using var context = contextFactory.CreateContext();
        var query = context.Entities.TemporalAsOf(new DateTime(2010, 1, 1));

        var result = async
            ? await query.ToListAsync()
            : query.ToList();

        Assert.Equal(2, result.Count);
        Assert.True(result.All(x => x.Reference != null));
        Assert.True(result.All(x => x.Collection.Count > 0));

        AssertSql(
            """
SELECT [e].[Id], [e].[Name], [e].[PeriodEnd], [e].[PeriodStart], [e].[Collection], [e].[Reference]
FROM [Entities] FOR SYSTEM_TIME AS OF '2010-01-01T00:00:00.0000000' AS [e]
""");
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task TemporalAll_with_json_basic_query(bool async)
    {
        var contextFactory = await InitializeAsync<Context30478>(seed: x => x.SeedAsync());
        using var context = contextFactory.CreateContext();
        var query = context.Entities.TemporalAll();

        var result = async
            ? await query.ToListAsync()
            : query.ToList();

        Assert.Equal(2, result.Count);
        Assert.True(result.All(x => x.Reference != null));
        Assert.True(result.All(x => x.Collection.Count > 0));

        AssertSql(
            """
SELECT [e].[Id], [e].[Name], [e].[PeriodEnd], [e].[PeriodStart], [e].[Collection], [e].[Reference]
FROM [Entities] FOR SYSTEM_TIME ALL AS [e]
""");
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task TemporalAsOf_project_json_entity_reference(bool async)
    {
        var contextFactory = await InitializeAsync<Context30478>(seed: x => x.SeedAsync());
        using var context = contextFactory.CreateContext();
        var query = context.Entities.TemporalAsOf(new DateTime(2010, 1, 1)).Select(x => x.Reference);

        var result = async
            ? await query.ToListAsync()
            : query.ToList();

        Assert.Equal(2, result.Count);
        Assert.True(result.All(x => x != null));

        AssertSql(
            """
SELECT [e].[Reference], [e].[Id]
FROM [Entities] FOR SYSTEM_TIME AS OF '2010-01-01T00:00:00.0000000' AS [e]
""");
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task TemporalAsOf_project_json_entity_collection(bool async)
    {
        var contextFactory = await InitializeAsync<Context30478>(seed: x => x.SeedAsync());
        using var context = contextFactory.CreateContext();
        var query = context.Entities.TemporalAsOf(new DateTime(2010, 1, 1)).Select(x => x.Collection);

        var result = async
            ? await query.ToListAsync()
            : query.ToList();

        Assert.Equal(2, result.Count);
        Assert.True(result.All(x => x.Count > 0));

        AssertSql(
            """
SELECT [e].[Collection], [e].[Id]
FROM [Entities] FOR SYSTEM_TIME AS OF '2010-01-01T00:00:00.0000000' AS [e]
""");
    }

    protected class Context30478(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Entity30478> Entities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Entity30478>().Property(x => x.Id).ValueGeneratedNever();
            modelBuilder.Entity<Entity30478>().ToTable("Entities", tb => tb.IsTemporal());
            modelBuilder.Entity<Entity30478>().OwnsOne(
                x => x.Reference, nb =>
                {
                    nb.ToJson();
                    nb.OwnsOne(x => x.Nested);
                });

            modelBuilder.Entity<Entity30478>().OwnsMany(
                x => x.Collection, nb =>
                {
                    nb.ToJson();
                    nb.OwnsOne(x => x.Nested);
                });
        }

        public async Task SeedAsync()
        {
            var e1 = new Entity30478
            {
                Id = 1,
                Name = "e1",
                Reference = new Json30478 { Name = "r1", Nested = new JsonNested30478 { Number = 1 } },
                Collection =
                [
                    new Json30478 { Name = "c11", Nested = new JsonNested30478 { Number = 11 } },

                    new Json30478 { Name = "c12", Nested = new JsonNested30478 { Number = 12 } },

                    new Json30478 { Name = "c13", Nested = new JsonNested30478 { Number = 12 } }
                ]
            };

            var e2 = new Entity30478
            {
                Id = 2,
                Name = "e2",
                Reference = new Json30478 { Name = "r2", Nested = new JsonNested30478 { Number = 2 } },
                Collection =
                [
                    new Json30478 { Name = "c21", Nested = new JsonNested30478 { Number = 21 } },

                    new Json30478 { Name = "c22", Nested = new JsonNested30478 { Number = 22 } }

                ]
            };

            AddRange(e1, e2);
            await SaveChangesAsync();

            RemoveRange(e1, e2);
            await SaveChangesAsync();


            await Database.ExecuteSqlRawAsync("ALTER TABLE [Entities] SET (SYSTEM_VERSIONING = OFF)");
            await Database.ExecuteSqlRawAsync("ALTER TABLE [Entities] DROP PERIOD FOR SYSTEM_TIME");

            await Database.ExecuteSqlRawAsync("UPDATE [EntitiesHistory] SET PeriodStart = '2000-01-01T01:00:00.0000000Z'");
            await Database.ExecuteSqlRawAsync("UPDATE [EntitiesHistory] SET PeriodEnd = '2020-07-01T07:00:00.0000000Z'");

            await Database.ExecuteSqlRawAsync("ALTER TABLE [Entities] ADD PERIOD FOR SYSTEM_TIME ([PeriodStart], [PeriodEnd])");
            await Database.ExecuteSqlRawAsync(
                "ALTER TABLE [Entities] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[EntitiesHistory]))");
        }
    }

    protected class Entity30478
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Json30478 Reference { get; set; }
        public List<Json30478> Collection { get; set; }
    }

    protected class Json30478
    {
        public string Name { get; set; }
        public JsonNested30478 Nested { get; set; }
    }

    protected class JsonNested30478
    {
        public int Number { get; set; }
    }

    #endregion

    public override async Task First_FirstOrDefault_ix_async()
    {
        await base.First_FirstOrDefault_ix_async();

        AssertSql(
            """
SELECT TOP(1) [p].[Id], [p].[Name]
FROM [Products] AS [p]
ORDER BY [p].[Id]
""",
            //
            """
@p0='1'

SET IMPLICIT_TRANSACTIONS OFF;
SET NOCOUNT ON;
DELETE FROM [Products]
OUTPUT 1
WHERE [Id] = @p0;
""",
            //
            """
@p0='Product 1' (Size = 4000)

SET IMPLICIT_TRANSACTIONS OFF;
SET NOCOUNT ON;
INSERT INTO [Products] ([Name])
OUTPUT INSERTED.[Id]
VALUES (@p0);
""",
            //
            """
SELECT TOP(1) [p].[Id], [p].[Name]
FROM [Products] AS [p]
ORDER BY [p].[Id]
""",
            //
            """
@p0='2'

SET IMPLICIT_TRANSACTIONS OFF;
SET NOCOUNT ON;
DELETE FROM [Products]
OUTPUT 1
WHERE [Id] = @p0;
""");
    }

    public override async Task Discriminator_type_is_handled_correctly()
    {
        await base.Discriminator_type_is_handled_correctly();

        AssertSql(
            """
SELECT [p].[Id], [p].[Discriminator], [p].[Name]
FROM [Products] AS [p]
WHERE [p].[Discriminator] = 1
""",
            //
            """
SELECT [p].[Id], [p].[Discriminator], [p].[Name]
FROM [Products] AS [p]
WHERE [p].[Discriminator] = 1
""");
    }

    public override async Task New_instances_in_projection_are_not_shared_across_results()
    {
        await base.New_instances_in_projection_are_not_shared_across_results();

        AssertSql(
            """
SELECT [p].[Id], [p].[BlogId], [p].[Title]
FROM [Posts] AS [p]
""");
    }

    public override async Task Enum_has_flag_applies_explicit_cast_for_constant()
    {
        await base.Enum_has_flag_applies_explicit_cast_for_constant();

        AssertSql(
            """
SELECT [e].[Id], [e].[Permission], [e].[PermissionByte], [e].[PermissionShort]
FROM [Entities] AS [e]
WHERE [e].[Permission] & CAST(17179869184 AS bigint) = CAST(17179869184 AS bigint)
""",
            //
            """
SELECT [e].[Id], [e].[Permission], [e].[PermissionByte], [e].[PermissionShort]
FROM [Entities] AS [e]
WHERE [e].[PermissionShort] & CAST(4 AS smallint) = CAST(4 AS smallint)
""");
    }

    public override async Task Enum_has_flag_does_not_apply_explicit_cast_for_non_constant()
    {
        await base.Enum_has_flag_does_not_apply_explicit_cast_for_non_constant();

        AssertSql(
            """
SELECT [e].[Id], [e].[Permission], [e].[PermissionByte], [e].[PermissionShort]
FROM [Entities] AS [e]
WHERE [e].[Permission] & [e].[Permission] = [e].[Permission]
""",
            //
            """
SELECT [e].[Id], [e].[Permission], [e].[PermissionByte], [e].[PermissionShort]
FROM [Entities] AS [e]
WHERE [e].[PermissionByte] & [e].[PermissionByte] = [e].[PermissionByte]
""");
    }

    public override async Task Variable_from_closure_is_parametrized()
    {
        await base.Variable_from_closure_is_parametrized();

        AssertSql(
            """
@__id_0='1'

SELECT [e].[Id], [e].[Name]
FROM [Entities] AS [e]
WHERE [e].[Id] = @__id_0
""",
            //
            """
@__id_0='2'

SELECT [e].[Id], [e].[Name]
FROM [Entities] AS [e]
WHERE [e].[Id] = @__id_0
""",
            //
            """
@__id_0='1'

SELECT [e].[Id], [e].[Name]
FROM [Entities] AS [e]
WHERE [e].[Id] = @__id_0
""",
            //
            """
@__id_0='2'

SELECT [e].[Id], [e].[Name]
FROM [Entities] AS [e]
WHERE [e].[Id] = @__id_0
""",
            //
            """
@__id_0='1'

SELECT [e].[Id], [e].[Name]
FROM [Entities] AS [e]
WHERE [e].[Id] IN (
    SELECT [e0].[Id]
    FROM [Entities] AS [e0]
    WHERE [e0].[Id] = @__id_0
)
""",
            //
            """
@__id_0='2'

SELECT [e].[Id], [e].[Name]
FROM [Entities] AS [e]
WHERE [e].[Id] IN (
    SELECT [e0].[Id]
    FROM [Entities] AS [e0]
    WHERE [e0].[Id] = @__id_0
)
""");
    }

    public override async Task Relational_command_cache_creates_new_entry_when_parameter_nullability_changes()
    {
        await base.Relational_command_cache_creates_new_entry_when_parameter_nullability_changes();

        AssertSql(
            """
@__name_0='A' (Size = 4000)

SELECT [e].[Id], [e].[Name]
FROM [Entities] AS [e]
WHERE [e].[Name] = @__name_0
""",
            //
            """
SELECT [e].[Id], [e].[Name]
FROM [Entities] AS [e]
WHERE [e].[Name] IS NULL
""");
    }

    public override async Task Query_cache_entries_are_evicted_as_necessary()
    {
        await base.Query_cache_entries_are_evicted_as_necessary();

        AssertSql();
    }

    public override async Task Explicitly_compiled_query_does_not_add_cache_entry()
    {
        await base.Explicitly_compiled_query_does_not_add_cache_entry();

        AssertSql(
            """
SELECT TOP(2) [e].[Id], [e].[Name]
FROM [Entities] AS [e]
WHERE [e].[Id] = 1
""");
    }

    public override async Task Conditional_expression_with_conditions_does_not_collapse_if_nullable_bool()
    {
        await base.Conditional_expression_with_conditions_does_not_collapse_if_nullable_bool();

        AssertSql(
            """
SELECT CASE
    WHEN [c0].[Id] IS NOT NULL THEN CASE
        WHEN [c0].[Processed] = CAST(0 AS bit) THEN CAST(1 AS bit)
        ELSE CAST(0 AS bit)
    END
    ELSE NULL
END AS [Processing]
FROM [Carts] AS [c]
LEFT JOIN [Configuration] AS [c0] ON [c].[ConfigurationId] = [c0].[Id]
""");
    }

    public override async Task QueryBuffer_requirement_is_computed_when_querying_base_type_while_derived_type_has_shadow_prop()
    {
        await base.QueryBuffer_requirement_is_computed_when_querying_base_type_while_derived_type_has_shadow_prop();

        AssertSql(
            """
SELECT [b].[Id], [b].[IsTwo], [b].[MoreStuffId]
FROM [Bases] AS [b]
""");
    }

    public override async Task Average_with_cast()
    {
        await base.Average_with_cast();

        AssertSql(
            """
SELECT [p].[Id], [p].[DecimalColumn], [p].[DoubleColumn], [p].[FloatColumn], [p].[IntColumn], [p].[LongColumn], [p].[NullableDecimalColumn], [p].[NullableDoubleColumn], [p].[NullableFloatColumn], [p].[NullableIntColumn], [p].[NullableLongColumn], [p].[Price]
FROM [Prices] AS [p]
""",
            //
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

    public override async Task Parameterless_ctor_on_inner_DTO_gets_called_for_every_row()
    {
        await base.Parameterless_ctor_on_inner_DTO_gets_called_for_every_row();

        AssertSql(
            """
SELECT [e].[Id], [e].[Name]
FROM [Entities] AS [e]
""");
    }

    public override async Task Union_and_insert_works_correctly_together()
    {
        await base.Union_and_insert_works_correctly_together();

        AssertSql(
            """
@__id1_0='1'
@__id2_1='2'

SELECT [t].[Id]
FROM [Tables1] AS [t]
WHERE [t].[Id] = @__id1_0
UNION
SELECT [t0].[Id]
FROM [Tables2] AS [t0]
WHERE [t0].[Id] = @__id2_1
""",
            //
            """
SET NOCOUNT ON;
INSERT INTO [Tables1]
OUTPUT INSERTED.[Id]
DEFAULT VALUES;
INSERT INTO [Tables1]
OUTPUT INSERTED.[Id]
DEFAULT VALUES;
INSERT INTO [Tables2]
OUTPUT INSERTED.[Id]
DEFAULT VALUES;
INSERT INTO [Tables2]
OUTPUT INSERTED.[Id]
DEFAULT VALUES;
""");
    }

    public override async Task Repeated_parameters_in_generated_query_sql()
    {
        await base.Repeated_parameters_in_generated_query_sql();

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

    public override async Task Operators_combine_nullability_of_entity_shapers()
    {
        await base.Operators_combine_nullability_of_entity_shapers();

        AssertSql(
            """
SELECT [a].[Id], [a].[a], [a].[a1], [a].[forkey], [b].[Id] AS [Id0], [b].[b], [b].[b1], [b].[forkey] AS [forkey0]
FROM [As] AS [a]
LEFT JOIN [Bs] AS [b] ON [a].[forkey] = [b].[forkey]
UNION ALL
SELECT [a0].[Id], [a0].[a], [a0].[a1], [a0].[forkey], [b0].[Id] AS [Id0], [b0].[b], [b0].[b1], [b0].[forkey] AS [forkey0]
FROM [Bs] AS [b0]
LEFT JOIN [As] AS [a0] ON [b0].[forkey] = [a0].[forkey]
WHERE [a0].[Id] IS NULL
""",
            //
            """
SELECT [a].[Id], [a].[a], [a].[a1], [a].[forkey], [b].[Id] AS [Id0], [b].[b], [b].[b1], [b].[forkey] AS [forkey0]
FROM [As] AS [a]
LEFT JOIN [Bs] AS [b] ON [a].[forkey] = [b].[forkey]
UNION
SELECT [a0].[Id], [a0].[a], [a0].[a1], [a0].[forkey], [b0].[Id] AS [Id0], [b0].[b], [b0].[b1], [b0].[forkey] AS [forkey0]
FROM [Bs] AS [b0]
LEFT JOIN [As] AS [a0] ON [b0].[forkey] = [a0].[forkey]
WHERE [a0].[Id] IS NULL
""",
            //
            """
SELECT [a].[Id], [a].[a], [a].[a1], [a].[forkey], [b].[Id] AS [Id0], [b].[b], [b].[b1], [b].[forkey] AS [forkey0]
FROM [As] AS [a]
LEFT JOIN [Bs] AS [b] ON [a].[forkey] = [b].[forkey]
EXCEPT
SELECT [a0].[Id], [a0].[a], [a0].[a1], [a0].[forkey], [b0].[Id] AS [Id0], [b0].[b], [b0].[b1], [b0].[forkey] AS [forkey0]
FROM [Bs] AS [b0]
LEFT JOIN [As] AS [a0] ON [b0].[forkey] = [a0].[forkey]
""",
            //
            """
SELECT [a].[Id], [a].[a], [a].[a1], [a].[forkey], [b].[Id] AS [Id0], [b].[b], [b].[b1], [b].[forkey] AS [forkey0]
FROM [As] AS [a]
LEFT JOIN [Bs] AS [b] ON [a].[forkey] = [b].[forkey]
INTERSECT
SELECT [a0].[Id], [a0].[a], [a0].[a1], [a0].[forkey], [b0].[Id] AS [Id0], [b0].[b], [b0].[b1], [b0].[forkey] AS [forkey0]
FROM [Bs] AS [b0]
LEFT JOIN [As] AS [a0] ON [b0].[forkey] = [a0].[forkey]
""");
    }

    public override async Task Shadow_property_with_inheritance()
    {
        await base.Shadow_property_with_inheritance();

        AssertSql(
            """
SELECT [c].[Id], [c].[Discriminator], [c].[IsPrimary], [c].[UserName], [c].[EmployerId], [c].[ServiceOperatorId]
FROM [Contacts] AS [c]
""",
            //
            """
SELECT [c].[Id], [c].[Discriminator], [c].[IsPrimary], [c].[UserName], [c].[ServiceOperatorId], [s].[Id]
FROM [Contacts] AS [c]
INNER JOIN [ServiceOperators] AS [s] ON [c].[ServiceOperatorId] = [s].[Id]
WHERE [c].[Discriminator] = N'ServiceOperatorContact'
""",
            //
            """
SELECT [c].[Id], [c].[Discriminator], [c].[IsPrimary], [c].[UserName], [c].[ServiceOperatorId]
FROM [Contacts] AS [c]
WHERE [c].[Discriminator] = N'ServiceOperatorContact'
""");
    }

    public override async Task Inlined_dbcontext_is_not_leaking()
    {
        await base.Inlined_dbcontext_is_not_leaking();

        AssertSql(
            """
SELECT [b].[Id]
FROM [Blogs] AS [b]
""");
    }

    public override async Task GroupJoin_Anonymous_projection_GroupBy_Aggregate_join_elimination()
    {
        await base.GroupJoin_Anonymous_projection_GroupBy_Aggregate_join_elimination();

        AssertSql(
            """
SELECT [t1].[AnotherEntity11818_Name] AS [Key], COUNT(*) + 5 AS [cnt]
FROM [Table] AS [t]
LEFT JOIN (
    SELECT [t0].[Id], [t0].[Exists], [t0].[AnotherEntity11818_Name]
    FROM [Table] AS [t0]
    WHERE [t0].[Exists] IS NOT NULL
) AS [t1] ON [t].[Id] = CASE
    WHEN [t1].[Exists] IS NOT NULL THEN [t1].[Id]
END
GROUP BY [t1].[AnotherEntity11818_Name]
""",
            //
            """
SELECT [t1].[AnotherEntity11818_Name] AS [MyKey], COUNT(*) + 5 AS [cnt]
FROM [Table] AS [t]
LEFT JOIN (
    SELECT [t0].[Id], [t0].[Exists], [t0].[AnotherEntity11818_Name]
    FROM [Table] AS [t0]
    WHERE [t0].[Exists] IS NOT NULL
) AS [t1] ON [t].[Id] = CASE
    WHEN [t1].[Exists] IS NOT NULL THEN [t1].[Id]
END
LEFT JOIN (
    SELECT [t2].[Id], [t2].[MaumarEntity11818_Exists], [t2].[MaumarEntity11818_Name]
    FROM [Table] AS [t2]
    WHERE [t2].[MaumarEntity11818_Exists] IS NOT NULL
) AS [t3] ON [t].[Id] = CASE
    WHEN [t3].[MaumarEntity11818_Exists] IS NOT NULL THEN [t3].[Id]
END
GROUP BY [t1].[AnotherEntity11818_Name], [t3].[MaumarEntity11818_Name]
""",
            //
            """
SELECT TOP(1) [t1].[AnotherEntity11818_Name] AS [MyKey], [t3].[MaumarEntity11818_Name] AS [cnt]
FROM [Table] AS [t]
LEFT JOIN (
    SELECT [t0].[Id], [t0].[Exists], [t0].[AnotherEntity11818_Name]
    FROM [Table] AS [t0]
    WHERE [t0].[Exists] IS NOT NULL
) AS [t1] ON [t].[Id] = CASE
    WHEN [t1].[Exists] IS NOT NULL THEN [t1].[Id]
END
LEFT JOIN (
    SELECT [t2].[Id], [t2].[MaumarEntity11818_Exists], [t2].[MaumarEntity11818_Name]
    FROM [Table] AS [t2]
    WHERE [t2].[MaumarEntity11818_Exists] IS NOT NULL
) AS [t3] ON [t].[Id] = CASE
    WHEN [t3].[MaumarEntity11818_Exists] IS NOT NULL THEN [t3].[Id]
END
GROUP BY [t1].[AnotherEntity11818_Name], [t3].[MaumarEntity11818_Name]
""");
    }

    public override async Task Left_join_with_missing_key_values_on_both_sides(bool async)
    {
        await base.Left_join_with_missing_key_values_on_both_sides(async);

        AssertSql(
            """
SELECT [c].[CustomerID], [c].[CustomerName], CASE
    WHEN [p].[PostcodeID] IS NULL THEN ''
    ELSE [p].[TownName]
END AS [TownName], CASE
    WHEN [p].[PostcodeID] IS NULL THEN ''
    ELSE [p].[PostcodeValue]
END AS [PostcodeValue]
FROM [Customers] AS [c]
LEFT JOIN [Postcodes] AS [p] ON [c].[PostcodeID] = [p].[PostcodeID]
""");
    }

    public override async Task Comparing_enum_casted_to_byte_with_int_parameter(bool async)
    {
        await base.Comparing_enum_casted_to_byte_with_int_parameter(async);

        AssertSql(
            """
@__bitterTaste_0='1'

SELECT [i].[IceCreamId], [i].[Name], [i].[Taste]
FROM [IceCreams] AS [i]
WHERE [i].[Taste] = @__bitterTaste_0
""");
    }

    public override async Task Comparing_enum_casted_to_byte_with_int_constant(bool async)
    {
        await base.Comparing_enum_casted_to_byte_with_int_constant(async);

        AssertSql(
            """
SELECT [i].[IceCreamId], [i].[Name], [i].[Taste]
FROM [IceCreams] AS [i]
WHERE [i].[Taste] = 1
""");
    }

    public override async Task Comparing_byte_column_to_enum_in_vb_creating_double_cast(bool async)
    {
        await base.Comparing_byte_column_to_enum_in_vb_creating_double_cast(async);

        AssertSql(
            """
SELECT [f].[Id], [f].[Taste]
FROM [Foods] AS [f]
WHERE [f].[Taste] = CAST(1 AS tinyint)
""");
    }

    public override async Task Null_check_removal_in_ternary_maintain_appropriate_cast(bool async)
    {
        await base.Null_check_removal_in_ternary_maintain_appropriate_cast(async);

        AssertSql(
            """
SELECT CAST([f].[Taste] AS tinyint) AS [Bar]
FROM [Foods] AS [f]
""");
    }

    public override async Task SaveChangesAsync_accepts_changes_with_ConfigureAwait_true()
    {
        await base.SaveChangesAsync_accepts_changes_with_ConfigureAwait_true();

        AssertSql(
            """
SET IMPLICIT_TRANSACTIONS OFF;
SET NOCOUNT ON;
INSERT INTO [ObservableThings]
OUTPUT INSERTED.[Id]
DEFAULT VALUES;
""");
    }

    public override async Task Bool_discriminator_column_works(bool async)
    {
        await base.Bool_discriminator_column_works(async);

        AssertSql(
            """
SELECT [a].[Id], [a].[BlogId], [b].[Id], [b].[IsPhotoBlog], [b].[Title], [b].[NumberOfPhotos]
FROM [Authors] AS [a]
LEFT JOIN [Blog] AS [b] ON [a].[BlogId] = [b].[Id]
""");
    }

    public override async Task Multiple_different_entity_type_from_different_namespaces(bool async)
    {
        await base.Multiple_different_entity_type_from_different_namespaces(async);

        AssertSql(
            """
SELECT cast(null as int) AS MyValue
""");
    }

    public override async Task Unwrap_convert_node_over_projection_when_translating_contains_over_subquery(bool async)
    {
        await base.Unwrap_convert_node_over_projection_when_translating_contains_over_subquery(async);

        AssertSql(
            """
@__currentUserId_0='1'

SELECT CASE
    WHEN [u].[Id] IN (
        SELECT [u0].[Id]
        FROM [Memberships] AS [m]
        INNER JOIN [Users] AS [u0] ON [m].[UserId] = [u0].[Id]
        WHERE [m].[GroupId] IN (
            SELECT [m0].[GroupId]
            FROM [Memberships] AS [m0]
            WHERE [m0].[UserId] = @__currentUserId_0
        )
    ) THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END AS [HasAccess]
FROM [Users] AS [u]
""");
    }

    public override async Task Unwrap_convert_node_over_projection_when_translating_contains_over_subquery_2(bool async)
    {
        await base.Unwrap_convert_node_over_projection_when_translating_contains_over_subquery_2(async);

        AssertSql(
            """
@__currentUserId_0='1'

SELECT CASE
    WHEN [u].[Id] IN (
        SELECT [u0].[Id]
        FROM [Memberships] AS [m]
        INNER JOIN [Groups] AS [g] ON [m].[GroupId] = [g].[Id]
        INNER JOIN [Users] AS [u0] ON [m].[UserId] = [u0].[Id]
        WHERE [g].[Id] IN (
            SELECT [g0].[Id]
            FROM [Memberships] AS [m0]
            INNER JOIN [Groups] AS [g0] ON [m0].[GroupId] = [g0].[Id]
            WHERE [m0].[UserId] = @__currentUserId_0
        )
    ) THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END AS [HasAccess]
FROM [Users] AS [u]
""");
    }

    public override async Task Unwrap_convert_node_over_projection_when_translating_contains_over_subquery_3(bool async)
    {
        await base.Unwrap_convert_node_over_projection_when_translating_contains_over_subquery_3(async);

        AssertSql(
            """
@__currentUserId_0='1'

SELECT CASE
    WHEN EXISTS (
        SELECT 1
        FROM [Memberships] AS [m]
        INNER JOIN [Users] AS [u0] ON [m].[UserId] = [u0].[Id]
        WHERE [m].[GroupId] IN (
            SELECT [m0].[GroupId]
            FROM [Memberships] AS [m0]
            WHERE [m0].[UserId] = @__currentUserId_0
        ) AND [u0].[Id] = [u].[Id]) THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END AS [HasAccess]
FROM [Users] AS [u]
""");
    }

    public override async Task GroupBy_aggregate_on_right_side_of_join(bool async)
    {
        await base.GroupBy_aggregate_on_right_side_of_join(async);

        AssertSql(
            """
@__orderId_0='123456'

SELECT [o].[Id], [o].[CancellationDate], [o].[OrderId], [o].[ShippingDate]
FROM [OrderItems] AS [o]
INNER JOIN (
    SELECT [o0].[OrderId] AS [Key]
    FROM [OrderItems] AS [o0]
    WHERE [o0].[OrderId] = @__orderId_0
    GROUP BY [o0].[OrderId]
) AS [o1] ON [o].[OrderId] = [o1].[Key]
WHERE [o].[OrderId] = @__orderId_0
ORDER BY [o].[OrderId]
""");
    }

    public override async Task Enum_with_value_converter_matching_take_value(bool async)
    {
        await base.Enum_with_value_converter_matching_take_value(async);

        AssertSql(
            """
@__orderItemType_1='MyType1' (Nullable = false) (Size = 4000)
@__p_0='1'

SELECT [o1].[Id], COALESCE((
    SELECT TOP(1) [o3].[Price]
    FROM [OrderItems] AS [o3]
    WHERE [o1].[Id] = [o3].[OrderId] AND [o3].[Type] = @__orderItemType_1), 0.0E0) AS [SpecialSum]
FROM (
    SELECT TOP(@__p_0) [o].[Id]
    FROM [Orders] AS [o]
    WHERE EXISTS (
        SELECT 1
        FROM [OrderItems] AS [o0]
        WHERE [o].[Id] = [o0].[OrderId])
    ORDER BY [o].[Id]
) AS [o2]
INNER JOIN [Orders] AS [o1] ON [o2].[Id] = [o1].[Id]
ORDER BY [o2].[Id]
""");
    }

    public override async Task GroupBy_Aggregate_over_navigations_repeated(bool async)
    {
        await base.GroupBy_Aggregate_over_navigations_repeated(async);

        AssertSql(
            """
SELECT (
    SELECT MIN([o].[HourlyRate])
    FROM [TimeSheets] AS [t0]
    LEFT JOIN [Order] AS [o] ON [t0].[OrderId] = [o].[Id]
    WHERE [t0].[OrderId] IS NOT NULL AND [t].[OrderId] = [t0].[OrderId]) AS [HourlyRate], (
    SELECT MIN([c].[Id])
    FROM [TimeSheets] AS [t1]
    INNER JOIN [Project] AS [p] ON [t1].[ProjectId] = [p].[Id]
    INNER JOIN [Customers] AS [c] ON [p].[CustomerId] = [c].[Id]
    WHERE [t1].[OrderId] IS NOT NULL AND [t].[OrderId] = [t1].[OrderId]) AS [CustomerId], (
    SELECT MIN([c0].[Name])
    FROM [TimeSheets] AS [t2]
    INNER JOIN [Project] AS [p0] ON [t2].[ProjectId] = [p0].[Id]
    INNER JOIN [Customers] AS [c0] ON [p0].[CustomerId] = [c0].[Id]
    WHERE [t2].[OrderId] IS NOT NULL AND [t].[OrderId] = [t2].[OrderId]) AS [CustomerName]
FROM [TimeSheets] AS [t]
WHERE [t].[OrderId] IS NOT NULL
GROUP BY [t].[OrderId]
""");
    }

    public override async Task Aggregate_over_subquery_in_group_by_projection(bool async)
    {
        await base.Aggregate_over_subquery_in_group_by_projection(async);

        AssertSql(
            """
SELECT [o].[CustomerId], (
    SELECT MIN([o0].[HourlyRate])
    FROM [Order] AS [o0]
    WHERE [o0].[CustomerId] = [o].[CustomerId]) AS [CustomerMinHourlyRate], MIN([o].[HourlyRate]) AS [HourlyRate], COUNT(*) AS [Count]
FROM [Order] AS [o]
WHERE [o].[Number] <> N'A1' OR [o].[Number] IS NULL
GROUP BY [o].[CustomerId], [o].[Number]
""");
    }

    public override async Task Aggregate_over_subquery_in_group_by_projection_2(bool async)
    {
        await base.Aggregate_over_subquery_in_group_by_projection_2(async);

        AssertSql(
            """
SELECT [t].[Value] AS [A], (
    SELECT MAX([t0].[Id])
    FROM [Tables] AS [t0]
    WHERE [t0].[Value] = MAX([t].[Id]) * 6 OR ([t0].[Value] IS NULL AND MAX([t].[Id]) IS NULL)) AS [B]
FROM [Tables] AS [t]
GROUP BY [t].[Value]
""");
    }

    public override async Task Group_by_aggregate_in_subquery_projection_after_group_by(bool async)
    {
        await base.Group_by_aggregate_in_subquery_projection_after_group_by(async);

        AssertSql(
            """
SELECT [t].[Value] AS [A], COALESCE(SUM([t].[Id]), 0) AS [B], COALESCE((
    SELECT TOP(1) COALESCE(SUM([t].[Id]), 0) + COALESCE(SUM([t0].[Id]), 0)
    FROM [Tables] AS [t0]
    GROUP BY [t0].[Value]
    ORDER BY (SELECT 1)), 0) AS [C]
FROM [Tables] AS [t]
GROUP BY [t].[Value]
""");
    }

    public override async Task Subquery_first_member_compared_to_null(bool async)
    {
        await base.Subquery_first_member_compared_to_null(async);

        AssertSql(
            """
SELECT (
    SELECT TOP(1) [c1].[SomeOtherNullableDateTime]
    FROM [Child] AS [c1]
    WHERE [p].[Id] = [c1].[ParentId] AND [c1].[SomeNullableDateTime] IS NULL
    ORDER BY [c1].[SomeInteger])
FROM [Parents] AS [p]
WHERE EXISTS (
    SELECT 1
    FROM [Child] AS [c]
    WHERE [p].[Id] = [c].[ParentId] AND [c].[SomeNullableDateTime] IS NULL) AND (
    SELECT TOP(1) [c0].[SomeOtherNullableDateTime]
    FROM [Child] AS [c0]
    WHERE [p].[Id] = [c0].[ParentId] AND [c0].[SomeNullableDateTime] IS NULL
    ORDER BY [c0].[SomeInteger]) IS NOT NULL
""");
    }

    public override async Task SelectMany_where_Select(bool async)
    {
        await base.SelectMany_where_Select(async);

        AssertSql(
            """
SELECT [c1].[SomeNullableDateTime]
FROM [Parents] AS [p]
INNER JOIN (
    SELECT [c0].[ParentId], [c0].[SomeNullableDateTime], [c0].[SomeOtherNullableDateTime]
    FROM (
        SELECT [c].[ParentId], [c].[SomeNullableDateTime], [c].[SomeOtherNullableDateTime], ROW_NUMBER() OVER(PARTITION BY [c].[ParentId] ORDER BY [c].[SomeInteger]) AS [row]
        FROM [Child] AS [c]
        WHERE [c].[SomeNullableDateTime] IS NULL
    ) AS [c0]
    WHERE [c0].[row] <= 1
) AS [c1] ON [p].[Id] = [c1].[ParentId]
WHERE [c1].[SomeOtherNullableDateTime] IS NOT NULL
""");
    }

    public override async Task Flattened_GroupJoin_on_interface_generic(bool async)
    {
        await base.Flattened_GroupJoin_on_interface_generic(async);

        AssertSql(
            """
SELECT [c].[Id], [c].[ParentId], [c].[SomeInteger], [c].[SomeNullableDateTime], [c].[SomeOtherNullableDateTime]
FROM [Parents] AS [p]
LEFT JOIN [Child] AS [c] ON [p].[Id] = [c].[Id]
""");
    }

    public override async Task StoreType_for_UDF_used(bool async)
    {
        await base.StoreType_for_UDF_used(async);

        AssertSql(
            """
@__date_0='2012-12-12T00:00:00.0000000' (DbType = DateTime)

SELECT [m].[Id], [m].[SomeDate]
FROM [MyEntities] AS [m]
WHERE [m].[SomeDate] = @__date_0
""",
            //
            """
@__date_0='2012-12-12T00:00:00.0000000' (DbType = DateTime)

SELECT [m].[Id], [m].[SomeDate]
FROM [MyEntities] AS [m]
WHERE [dbo].[ModifyDate]([m].[SomeDate]) = @__date_0
""");
    }

    public override async Task Pushdown_does_not_add_grouping_key_to_projection_when_distinct_is_applied(bool async)
    {
        await base.Pushdown_does_not_add_grouping_key_to_projection_when_distinct_is_applied(async);

        AssertSql(
            """
@__p_0='123456'

SELECT TOP(@__p_0) [t].[JSON]
FROM [TableDatas] AS [t]
INNER JOIN (
    SELECT DISTINCT [i].[Parcel]
    FROM [IndexDatas] AS [i]
    WHERE [i].[Parcel] = N'some condition'
    GROUP BY [i].[Parcel], [i].[RowId]
    HAVING COUNT(*) = 1
) AS [i0] ON [t].[ParcelNumber] = [i0].[Parcel]
WHERE [t].[TableId] = 123
ORDER BY [t].[ParcelNumber]
""");
    }

    public override async Task Filter_on_nested_DTO_with_interface_gets_simplified_correctly(bool async)
    {
        await base.Filter_on_nested_DTO_with_interface_gets_simplified_correctly(async);

        AssertSql(
            """
SELECT [c].[Id], [c].[CompanyId], CASE
    WHEN [c0].[Id] IS NOT NULL THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END, [c0].[Id], [c0].[CompanyName], [c0].[CountryId], [c1].[Id], [c1].[CountryName]
FROM [Customers] AS [c]
LEFT JOIN [Companies] AS [c0] ON [c].[CompanyId] = [c0].[Id]
LEFT JOIN [Countries] AS [c1] ON [c0].[CountryId] = [c1].[Id]
WHERE CASE
    WHEN [c0].[Id] IS NOT NULL THEN [c1].[CountryName]
    ELSE NULL
END = N'COUNTRY'
""");
    }






    [ConditionalFact]
    public void PerfRepro()
    {
        var bencher = new EFCoreBencher();
        bencher.Setup();

        var sw = new Stopwatch();
        sw.Start();

        for (int i = 0; i < 500; i++)
        {
            bencher.LoadSalesOrderHeaders();
        }

        throw new InvalidOperationException(sw.Elapsed.ToString());
    }

    public class EFCoreBencher
    {
        private static PooledDbContextFactory<AdventureWorksContext> _factory;

        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AdventureWorksContext>()
                //.UseSqlServer(@"Data Source=(localdb)\MSSqlLocalDb;Integrated Security=SSPI;Initial Catalog=AdventureWorks;")
                .UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=AdventureWorks2019;Trusted_Connection=True;MultipleActiveResultSets=true")
                .EnableThreadSafetyChecks(false)
                .Options;

            _factory = new PooledDbContextFactory<AdventureWorksContext>(options);
            using var ctx = _factory.CreateDbContext();
        }

        public List<SalesOrderHeader> LoadSalesOrderHeaders()
        {
            using var ctx = _factory.CreateDbContext();

            return ctx.SalesOrderHeaders.AsNoTracking()
                .Where(p => p.SalesOrderId > 50000 && p.SalesOrderId <= 50300)
                .Include(p => p.Customer)
                .Include(p => p.SalesOrderDetails)
                .ToList();
        }

        public async Task<List<SalesOrderHeader>> LoadSalesOrderHeadersAsync()
        {
            await using var ctx = _factory.CreateDbContext();
            return await ctx.SalesOrderHeaders.AsNoTracking()
                .Where(p => p.SalesOrderId > 50000 && p.SalesOrderId <= 50300)
                .Include(p => p.Customer)
                .Include(p => p.SalesOrderDetails)
                .ToListAsync();
        }
    }

    public partial class AdventureWorksContext : DbContext
    {
        public AdventureWorksContext(DbContextOptions<AdventureWorksContext> options)
            : base(options)
        {
        }

        public virtual DbSet<CountryRegionCurrency> CountryRegionCurrencies { get; set; }

        public virtual DbSet<CreditCard> CreditCards { get; set; }

        public virtual DbSet<Currency> Currencies { get; set; }

        public virtual DbSet<CurrencyRate> CurrencyRates { get; set; }

        public virtual DbSet<Customer> Customers { get; set; }

        public virtual DbSet<PersonCreditCard> PersonCreditCards { get; set; }

        public virtual DbSet<SalesOrderDetail> SalesOrderDetails { get; set; }

        public virtual DbSet<SalesOrderHeader> SalesOrderHeaders { get; set; }

        public virtual DbSet<SalesOrderHeaderSalesReason> SalesOrderHeaderSalesReasons { get; set; }

        public virtual DbSet<SalesPerson> SalesPeople { get; set; }

        public virtual DbSet<SalesPersonQuotaHistory> SalesPersonQuotaHistories { get; set; }

        public virtual DbSet<SalesReason> SalesReasons { get; set; }

        public virtual DbSet<SalesTaxRate> SalesTaxRates { get; set; }

        public virtual DbSet<SalesTerritory> SalesTerritories { get; set; }

        public virtual DbSet<SalesTerritoryHistory> SalesTerritoryHistories { get; set; }

        public virtual DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }

        public virtual DbSet<SpecialOffer> SpecialOffers { get; set; }

        public virtual DbSet<SpecialOfferProduct> SpecialOfferProducts { get; set; }

        public virtual DbSet<Store> Stores { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CountryRegionCurrency>(entity =>
            {
                entity.HasKey(e => new { e.CountryRegionCode, e.CurrencyCode }).HasName("PK_CountryRegionCurrency_CountryRegionCode_CurrencyCode");

                entity.ToTable("CountryRegionCurrency", "Sales", tb => tb.HasComment("Cross-reference table mapping ISO currency codes to a country or region."));

                entity.HasIndex(e => e.CurrencyCode, "IX_CountryRegionCurrency_CurrencyCode");

                entity.Property(e => e.CountryRegionCode)
                    .HasMaxLength(3)
                    .HasComment("ISO code for countries and regions. Foreign key to CountryRegion.CountryRegionCode.");
                entity.Property(e => e.CurrencyCode)
                    .HasMaxLength(3)
                    .IsFixedLength()
                    .HasComment("ISO standard currency code. Foreign key to Currency.CurrencyCode.");
                entity.Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Date and time the record was last updated.")
                    .HasColumnType("datetime");

                entity.HasOne(d => d.CurrencyCodeNavigation).WithMany(p => p.CountryRegionCurrencies)
                    .HasForeignKey(d => d.CurrencyCode)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<CreditCard>(entity =>
            {
                entity.HasKey(e => e.CreditCardId).HasName("PK_CreditCard_CreditCardID");

                entity.ToTable("CreditCard", "Sales", tb => tb.HasComment("Customer credit card information."));

                entity.HasIndex(e => e.CardNumber, "AK_CreditCard_CardNumber").IsUnique();

                entity.Property(e => e.CreditCardId)
                    .HasComment("Primary key for CreditCard records.")
                    .HasColumnName("CreditCardID");
                entity.Property(e => e.CardNumber)
                    .IsRequired()
                    .HasMaxLength(25)
                    .HasComment("Credit card number.");
                entity.Property(e => e.CardType)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasComment("Credit card name.");
                entity.Property(e => e.ExpMonth).HasComment("Credit card expiration month.");
                entity.Property(e => e.ExpYear).HasComment("Credit card expiration year.");
                entity.Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Date and time the record was last updated.")
                    .HasColumnType("datetime");
            });

            modelBuilder.Entity<Currency>(entity =>
            {
                entity.HasKey(e => e.CurrencyCode).HasName("PK_Currency_CurrencyCode");

                entity.ToTable("Currency", "Sales", tb => tb.HasComment("Lookup table containing standard ISO currencies."));

                entity.HasIndex(e => e.Name, "AK_Currency_Name").IsUnique();

                entity.Property(e => e.CurrencyCode)
                    .HasMaxLength(3)
                    .IsFixedLength()
                    .HasComment("The ISO code for the Currency.");
                entity.Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Date and time the record was last updated.")
                    .HasColumnType("datetime");
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasComment("Currency name.");
            });

            modelBuilder.Entity<CurrencyRate>(entity =>
            {
                entity.HasKey(e => e.CurrencyRateId).HasName("PK_CurrencyRate_CurrencyRateID");

                entity.ToTable("CurrencyRate", "Sales", tb => tb.HasComment("Currency exchange rates."));

                entity.HasIndex(e => new { e.CurrencyRateDate, e.FromCurrencyCode, e.ToCurrencyCode }, "AK_CurrencyRate_CurrencyRateDate_FromCurrencyCode_ToCurrencyCode").IsUnique();

                entity.Property(e => e.CurrencyRateId)
                    .HasComment("Primary key for CurrencyRate records.")
                    .HasColumnName("CurrencyRateID");
                entity.Property(e => e.AverageRate)
                    .HasComment("Average exchange rate for the day.")
                    .HasColumnType("money");
                entity.Property(e => e.CurrencyRateDate)
                    .HasComment("Date and time the exchange rate was obtained.")
                    .HasColumnType("datetime");
                entity.Property(e => e.EndOfDayRate)
                    .HasComment("Final exchange rate for the day.")
                    .HasColumnType("money");
                entity.Property(e => e.FromCurrencyCode)
                    .IsRequired()
                    .HasMaxLength(3)
                    .IsFixedLength()
                    .HasComment("Exchange rate was converted from this currency code.");
                entity.Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Date and time the record was last updated.")
                    .HasColumnType("datetime");
                entity.Property(e => e.ToCurrencyCode)
                    .IsRequired()
                    .HasMaxLength(3)
                    .IsFixedLength()
                    .HasComment("Exchange rate was converted to this currency code.");

                entity.HasOne(d => d.FromCurrencyCodeNavigation).WithMany(p => p.CurrencyRateFromCurrencyCodeNavigations)
                    .HasForeignKey(d => d.FromCurrencyCode)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.ToCurrencyCodeNavigation).WithMany(p => p.CurrencyRateToCurrencyCodeNavigations)
                    .HasForeignKey(d => d.ToCurrencyCode)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.CustomerId).HasName("PK_Customer_CustomerID");

                entity.ToTable("Customer", "Sales", tb => tb.HasComment("Current customer information. Also see the Person and Store tables."));

                entity.HasIndex(e => e.AccountNumber, "AK_Customer_AccountNumber").IsUnique();

                entity.HasIndex(e => e.Rowguid, "AK_Customer_rowguid").IsUnique();

                entity.HasIndex(e => e.TerritoryId, "IX_Customer_TerritoryID");

                entity.Property(e => e.CustomerId)
                    .HasComment("Primary key.")
                    .HasColumnName("CustomerID");
                entity.Property(e => e.AccountNumber)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasComputedColumnSql("(isnull('AW'+[dbo].[ufnLeadingZeros]([CustomerID]),''))", false)
                    .HasComment("Unique number identifying the customer assigned by the accounting system.");
                entity.Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Date and time the record was last updated.")
                    .HasColumnType("datetime");
                entity.Property(e => e.PersonId)
                    .HasComment("Foreign key to Person.BusinessEntityID")
                    .HasColumnName("PersonID");
                entity.Property(e => e.Rowguid)
                    .HasDefaultValueSql("(newid())")
                    .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                    .HasColumnName("rowguid");
                entity.Property(e => e.StoreId)
                    .HasComment("Foreign key to Store.BusinessEntityID")
                    .HasColumnName("StoreID");
                entity.Property(e => e.TerritoryId)
                    .HasComment("ID of the territory in which the customer is located. Foreign key to SalesTerritory.SalesTerritoryID.")
                    .HasColumnName("TerritoryID");

                entity.HasOne(d => d.Store).WithMany(p => p.Customers).HasForeignKey(d => d.StoreId);

                entity.HasOne(d => d.Territory).WithMany(p => p.Customers).HasForeignKey(d => d.TerritoryId);
            });

            modelBuilder.Entity<PersonCreditCard>(entity =>
            {
                entity.HasKey(e => new { e.BusinessEntityId, e.CreditCardId }).HasName("PK_PersonCreditCard_BusinessEntityID_CreditCardID");

                entity.ToTable("PersonCreditCard", "Sales", tb => tb.HasComment("Cross-reference table mapping people to their credit card information in the CreditCard table. "));

                entity.Property(e => e.BusinessEntityId)
                    .HasComment("Business entity identification number. Foreign key to Person.BusinessEntityID.")
                    .HasColumnName("BusinessEntityID");
                entity.Property(e => e.CreditCardId)
                    .HasComment("Credit card identification number. Foreign key to CreditCard.CreditCardID.")
                    .HasColumnName("CreditCardID");
                entity.Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Date and time the record was last updated.")
                    .HasColumnType("datetime");

                entity.HasOne(d => d.CreditCard).WithMany(p => p.PersonCreditCards)
                    .HasForeignKey(d => d.CreditCardId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<SalesOrderDetail>(entity =>
            {
                entity.HasKey(e => new { e.SalesOrderId, e.SalesOrderDetailId }).HasName("PK_SalesOrderDetail_SalesOrderID_SalesOrderDetailID");

                entity.ToTable("SalesOrderDetail", "Sales", tb => tb.HasComment("Individual products associated with a specific sales order. See SalesOrderHeader."));

                entity.HasIndex(e => e.Rowguid, "AK_SalesOrderDetail_rowguid").IsUnique();

                entity.HasIndex(e => e.ProductId, "IX_SalesOrderDetail_ProductID");

                entity.Property(e => e.SalesOrderId)
                    .HasComment("Primary key. Foreign key to SalesOrderHeader.SalesOrderID.")
                    .HasColumnName("SalesOrderID");
                entity.Property(e => e.SalesOrderDetailId)
                    .ValueGeneratedOnAdd()
                    .HasComment("Primary key. One incremental unique number per product sold.")
                    .HasColumnName("SalesOrderDetailID");
                entity.Property(e => e.CarrierTrackingNumber)
                    .HasMaxLength(25)
                    .HasComment("Shipment tracking number supplied by the shipper.");
                entity.Property(e => e.LineTotal)
                    .HasComputedColumnSql("(isnull(([UnitPrice]*((1.0)-[UnitPriceDiscount]))*[OrderQty],(0.0)))", false)
                    .HasComment("Per product subtotal. Computed as UnitPrice * (1 - UnitPriceDiscount) * OrderQty.")
                    .HasColumnType("numeric(38, 6)");
                entity.Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Date and time the record was last updated.")
                    .HasColumnType("datetime");
                entity.Property(e => e.OrderQty).HasComment("Quantity ordered per product.");
                entity.Property(e => e.ProductId)
                    .HasComment("Product sold to customer. Foreign key to Product.ProductID.")
                    .HasColumnName("ProductID");
                entity.Property(e => e.Rowguid)
                    .HasDefaultValueSql("(newid())")
                    .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                    .HasColumnName("rowguid");
                entity.Property(e => e.SpecialOfferId)
                    .HasComment("Promotional code. Foreign key to SpecialOffer.SpecialOfferID.")
                    .HasColumnName("SpecialOfferID");
                entity.Property(e => e.UnitPrice)
                    .HasComment("Selling price of a single product.")
                    .HasColumnType("money");
                entity.Property(e => e.UnitPriceDiscount)
                    .HasComment("Discount amount.")
                    .HasColumnType("money");

                entity.HasOne(d => d.SalesOrder).WithMany(p => p.SalesOrderDetails).HasForeignKey(d => d.SalesOrderId);

                entity.HasOne(d => d.SpecialOfferProduct).WithMany(p => p.SalesOrderDetails)
                    .HasForeignKey(d => new { d.SpecialOfferId, d.ProductId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_SalesOrderDetail_SpecialOfferProduct_SpecialOfferIDProductID");
            });

            modelBuilder.Entity<SalesOrderHeader>(entity =>
            {
                entity.HasKey(e => e.SalesOrderId).HasName("PK_SalesOrderHeader_SalesOrderID");

                entity.ToTable("SalesOrderHeader", "Sales", tb => tb.HasComment("General sales order information."));

                entity.HasIndex(e => e.SalesOrderNumber, "AK_SalesOrderHeader_SalesOrderNumber").IsUnique();

                entity.HasIndex(e => e.Rowguid, "AK_SalesOrderHeader_rowguid").IsUnique();

                entity.HasIndex(e => e.CustomerId, "IX_SalesOrderHeader_CustomerID");

                entity.HasIndex(e => e.SalesPersonId, "IX_SalesOrderHeader_SalesPersonID");

                entity.Property(e => e.SalesOrderId)
                    .HasComment("Primary key.")
                    .HasColumnName("SalesOrderID");
                entity.Property(e => e.AccountNumber)
                    .HasMaxLength(15)
                    .HasComment("Financial accounting number reference.");
                entity.Property(e => e.BillToAddressId)
                    .HasComment("Customer billing address. Foreign key to Address.AddressID.")
                    .HasColumnName("BillToAddressID");
                entity.Property(e => e.Comment)
                    .HasMaxLength(128)
                    .HasComment("Sales representative comments.");
                entity.Property(e => e.CreditCardApprovalCode)
                    .HasMaxLength(15)
                    .IsUnicode(false)
                    .HasComment("Approval code provided by the credit card company.");
                entity.Property(e => e.CreditCardId)
                    .HasComment("Credit card identification number. Foreign key to CreditCard.CreditCardID.")
                    .HasColumnName("CreditCardID");
                entity.Property(e => e.CurrencyRateId)
                    .HasComment("Currency exchange rate used. Foreign key to CurrencyRate.CurrencyRateID.")
                    .HasColumnName("CurrencyRateID");
                entity.Property(e => e.CustomerId)
                    .HasComment("Customer identification number. Foreign key to Customer.BusinessEntityID.")
                    .HasColumnName("CustomerID");
                entity.Property(e => e.DueDate)
                    .HasComment("Date the order is due to the customer.")
                    .HasColumnType("datetime");
                entity.Property(e => e.Freight)
                    .HasComment("Shipping cost.")
                    .HasColumnType("money");
                entity.Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Date and time the record was last updated.")
                    .HasColumnType("datetime");
                entity.Property(e => e.OnlineOrderFlag)
                    .HasDefaultValue(true)
                    .HasComment("0 = Order placed by sales person. 1 = Order placed online by customer.");
                entity.Property(e => e.OrderDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Dates the sales order was created.")
                    .HasColumnType("datetime");
                entity.Property(e => e.PurchaseOrderNumber)
                    .HasMaxLength(25)
                    .HasComment("Customer purchase order number reference. ");
                entity.Property(e => e.RevisionNumber).HasComment("Incremental number to track changes to the sales order over time.");
                entity.Property(e => e.Rowguid)
                    .HasDefaultValueSql("(newid())")
                    .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                    .HasColumnName("rowguid");
                entity.Property(e => e.SalesOrderNumber)
                    .IsRequired()
                    .HasMaxLength(25)
                    .HasComputedColumnSql("(isnull(N'SO'+CONVERT([nvarchar](23),[SalesOrderID]),N'*** ERROR ***'))", false)
                    .HasComment("Unique sales order identification number.");
                entity.Property(e => e.SalesPersonId)
                    .HasComment("Sales person who created the sales order. Foreign key to SalesPerson.BusinessEntityID.")
                    .HasColumnName("SalesPersonID");
                entity.Property(e => e.ShipDate)
                    .HasComment("Date the order was shipped to the customer.")
                    .HasColumnType("datetime");
                entity.Property(e => e.ShipMethodId)
                    .HasComment("Shipping method. Foreign key to ShipMethod.ShipMethodID.")
                    .HasColumnName("ShipMethodID");
                entity.Property(e => e.ShipToAddressId)
                    .HasComment("Customer shipping address. Foreign key to Address.AddressID.")
                    .HasColumnName("ShipToAddressID");
                entity.Property(e => e.Status)
                    .HasDefaultValue((byte)1)
                    .HasComment("Order current status. 1 = In process; 2 = Approved; 3 = Backordered; 4 = Rejected; 5 = Shipped; 6 = Cancelled");
                entity.Property(e => e.SubTotal)
                    .HasComment("Sales subtotal. Computed as SUM(SalesOrderDetail.LineTotal)for the appropriate SalesOrderID.")
                    .HasColumnType("money");
                entity.Property(e => e.TaxAmt)
                    .HasComment("Tax amount.")
                    .HasColumnType("money");
                entity.Property(e => e.TerritoryId)
                    .HasComment("Territory in which the sale was made. Foreign key to SalesTerritory.SalesTerritoryID.")
                    .HasColumnName("TerritoryID");
                entity.Property(e => e.TotalDue)
                    .HasComputedColumnSql("(isnull(([SubTotal]+[TaxAmt])+[Freight],(0)))", false)
                    .HasComment("Total due from customer. Computed as Subtotal + TaxAmt + Freight.")
                    .HasColumnType("money");

                entity.HasOne(d => d.CreditCard).WithMany(p => p.SalesOrderHeaders).HasForeignKey(d => d.CreditCardId);

                entity.HasOne(d => d.CurrencyRate).WithMany(p => p.SalesOrderHeaders).HasForeignKey(d => d.CurrencyRateId);

                entity.HasOne(d => d.Customer).WithMany(p => p.SalesOrderHeaders)
                    .HasForeignKey(d => d.CustomerId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.SalesPerson).WithMany(p => p.SalesOrderHeaders).HasForeignKey(d => d.SalesPersonId);

                entity.HasOne(d => d.Territory).WithMany(p => p.SalesOrderHeaders).HasForeignKey(d => d.TerritoryId);
            });

            modelBuilder.Entity<SalesOrderHeaderSalesReason>(entity =>
            {
                entity.HasKey(e => new { e.SalesOrderId, e.SalesReasonId }).HasName("PK_SalesOrderHeaderSalesReason_SalesOrderID_SalesReasonID");

                entity.ToTable("SalesOrderHeaderSalesReason", "Sales", tb => tb.HasComment("Cross-reference table mapping sales orders to sales reason codes."));

                entity.Property(e => e.SalesOrderId)
                    .HasComment("Primary key. Foreign key to SalesOrderHeader.SalesOrderID.")
                    .HasColumnName("SalesOrderID");
                entity.Property(e => e.SalesReasonId)
                    .HasComment("Primary key. Foreign key to SalesReason.SalesReasonID.")
                    .HasColumnName("SalesReasonID");
                entity.Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Date and time the record was last updated.")
                    .HasColumnType("datetime");

                entity.HasOne(d => d.SalesOrder).WithMany(p => p.SalesOrderHeaderSalesReasons).HasForeignKey(d => d.SalesOrderId);

                entity.HasOne(d => d.SalesReason).WithMany(p => p.SalesOrderHeaderSalesReasons)
                    .HasForeignKey(d => d.SalesReasonId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<SalesPerson>(entity =>
            {
                entity.HasKey(e => e.BusinessEntityId).HasName("PK_SalesPerson_BusinessEntityID");

                entity.ToTable("SalesPerson", "Sales", tb => tb.HasComment("Sales representative current information."));

                entity.HasIndex(e => e.Rowguid, "AK_SalesPerson_rowguid").IsUnique();

                entity.Property(e => e.BusinessEntityId)
                    .ValueGeneratedNever()
                    .HasComment("Primary key for SalesPerson records. Foreign key to Employee.BusinessEntityID")
                    .HasColumnName("BusinessEntityID");
                entity.Property(e => e.Bonus)
                    .HasComment("Bonus due if quota is met.")
                    .HasColumnType("money");
                entity.Property(e => e.CommissionPct)
                    .HasComment("Commision percent received per sale.")
                    .HasColumnType("smallmoney");
                entity.Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Date and time the record was last updated.")
                    .HasColumnType("datetime");
                entity.Property(e => e.Rowguid)
                    .HasDefaultValueSql("(newid())")
                    .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                    .HasColumnName("rowguid");
                entity.Property(e => e.SalesLastYear)
                    .HasComment("Sales total of previous year.")
                    .HasColumnType("money");
                entity.Property(e => e.SalesQuota)
                    .HasComment("Projected yearly sales.")
                    .HasColumnType("money");
                entity.Property(e => e.SalesYtd)
                    .HasComment("Sales total year to date.")
                    .HasColumnType("money")
                    .HasColumnName("SalesYTD");
                entity.Property(e => e.TerritoryId)
                    .HasComment("Territory currently assigned to. Foreign key to SalesTerritory.SalesTerritoryID.")
                    .HasColumnName("TerritoryID");

                entity.HasOne(d => d.Territory).WithMany(p => p.SalesPeople).HasForeignKey(d => d.TerritoryId);
            });

            modelBuilder.Entity<SalesPersonQuotaHistory>(entity =>
            {
                entity.HasKey(e => new { e.BusinessEntityId, e.QuotaDate }).HasName("PK_SalesPersonQuotaHistory_BusinessEntityID_QuotaDate");

                entity.ToTable("SalesPersonQuotaHistory", "Sales", tb => tb.HasComment("Sales performance tracking."));

                entity.HasIndex(e => e.Rowguid, "AK_SalesPersonQuotaHistory_rowguid").IsUnique();

                entity.Property(e => e.BusinessEntityId)
                    .HasComment("Sales person identification number. Foreign key to SalesPerson.BusinessEntityID.")
                    .HasColumnName("BusinessEntityID");
                entity.Property(e => e.QuotaDate)
                    .HasComment("Sales quota date.")
                    .HasColumnType("datetime");
                entity.Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Date and time the record was last updated.")
                    .HasColumnType("datetime");
                entity.Property(e => e.Rowguid)
                    .HasDefaultValueSql("(newid())")
                    .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                    .HasColumnName("rowguid");
                entity.Property(e => e.SalesQuota)
                    .HasComment("Sales quota amount.")
                    .HasColumnType("money");

                entity.HasOne(d => d.BusinessEntity).WithMany(p => p.SalesPersonQuotaHistories)
                    .HasForeignKey(d => d.BusinessEntityId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<SalesReason>(entity =>
            {
                entity.HasKey(e => e.SalesReasonId).HasName("PK_SalesReason_SalesReasonID");

                entity.ToTable("SalesReason", "Sales", tb => tb.HasComment("Lookup table of customer purchase reasons."));

                entity.Property(e => e.SalesReasonId)
                    .HasComment("Primary key for SalesReason records.")
                    .HasColumnName("SalesReasonID");
                entity.Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Date and time the record was last updated.")
                    .HasColumnType("datetime");
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasComment("Sales reason description.");
                entity.Property(e => e.ReasonType)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasComment("Category the sales reason belongs to.");
            });

            modelBuilder.Entity<SalesTaxRate>(entity =>
            {
                entity.HasKey(e => e.SalesTaxRateId).HasName("PK_SalesTaxRate_SalesTaxRateID");

                entity.ToTable("SalesTaxRate", "Sales", tb => tb.HasComment("Tax rate lookup table."));

                entity.HasIndex(e => new { e.StateProvinceId, e.TaxType }, "AK_SalesTaxRate_StateProvinceID_TaxType").IsUnique();

                entity.HasIndex(e => e.Rowguid, "AK_SalesTaxRate_rowguid").IsUnique();

                entity.Property(e => e.SalesTaxRateId)
                    .HasComment("Primary key for SalesTaxRate records.")
                    .HasColumnName("SalesTaxRateID");
                entity.Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Date and time the record was last updated.")
                    .HasColumnType("datetime");
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasComment("Tax rate description.");
                entity.Property(e => e.Rowguid)
                    .HasDefaultValueSql("(newid())")
                    .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                    .HasColumnName("rowguid");
                entity.Property(e => e.StateProvinceId)
                    .HasComment("State, province, or country/region the sales tax applies to.")
                    .HasColumnName("StateProvinceID");
                entity.Property(e => e.TaxRate)
                    .HasComment("Tax rate amount.")
                    .HasColumnType("smallmoney");
                entity.Property(e => e.TaxType).HasComment("1 = Tax applied to retail transactions, 2 = Tax applied to wholesale transactions, 3 = Tax applied to all sales (retail and wholesale) transactions.");
            });

            modelBuilder.Entity<SalesTerritory>(entity =>
            {
                entity.HasKey(e => e.TerritoryId).HasName("PK_SalesTerritory_TerritoryID");

                entity.ToTable("SalesTerritory", "Sales", tb => tb.HasComment("Sales territory lookup table."));

                entity.HasIndex(e => e.Name, "AK_SalesTerritory_Name").IsUnique();

                entity.HasIndex(e => e.Rowguid, "AK_SalesTerritory_rowguid").IsUnique();

                entity.Property(e => e.TerritoryId)
                    .HasComment("Primary key for SalesTerritory records.")
                    .HasColumnName("TerritoryID");
                entity.Property(e => e.CostLastYear)
                    .HasComment("Business costs in the territory the previous year.")
                    .HasColumnType("money");
                entity.Property(e => e.CostYtd)
                    .HasComment("Business costs in the territory year to date.")
                    .HasColumnType("money")
                    .HasColumnName("CostYTD");
                entity.Property(e => e.CountryRegionCode)
                    .IsRequired()
                    .HasMaxLength(3)
                    .HasComment("ISO standard country or region code. Foreign key to CountryRegion.CountryRegionCode. ");
                entity.Property(e => e.Group)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasComment("Geographic area to which the sales territory belong.");
                entity.Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Date and time the record was last updated.")
                    .HasColumnType("datetime");
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasComment("Sales territory description");
                entity.Property(e => e.Rowguid)
                    .HasDefaultValueSql("(newid())")
                    .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                    .HasColumnName("rowguid");
                entity.Property(e => e.SalesLastYear)
                    .HasComment("Sales in the territory the previous year.")
                    .HasColumnType("money");
                entity.Property(e => e.SalesYtd)
                    .HasComment("Sales in the territory year to date.")
                    .HasColumnType("money")
                    .HasColumnName("SalesYTD");
            });

            modelBuilder.Entity<SalesTerritoryHistory>(entity =>
            {
                entity.HasKey(e => new { e.BusinessEntityId, e.StartDate, e.TerritoryId }).HasName("PK_SalesTerritoryHistory_BusinessEntityID_StartDate_TerritoryID");

                entity.ToTable("SalesTerritoryHistory", "Sales", tb => tb.HasComment("Sales representative transfers to other sales territories."));

                entity.HasIndex(e => e.Rowguid, "AK_SalesTerritoryHistory_rowguid").IsUnique();

                entity.Property(e => e.BusinessEntityId)
                    .HasComment("Primary key. The sales rep.  Foreign key to SalesPerson.BusinessEntityID.")
                    .HasColumnName("BusinessEntityID");
                entity.Property(e => e.StartDate)
                    .HasComment("Primary key. Date the sales representive started work in the territory.")
                    .HasColumnType("datetime");
                entity.Property(e => e.TerritoryId)
                    .HasComment("Primary key. Territory identification number. Foreign key to SalesTerritory.SalesTerritoryID.")
                    .HasColumnName("TerritoryID");
                entity.Property(e => e.EndDate)
                    .HasComment("Date the sales representative left work in the territory.")
                    .HasColumnType("datetime");
                entity.Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Date and time the record was last updated.")
                    .HasColumnType("datetime");
                entity.Property(e => e.Rowguid)
                    .HasDefaultValueSql("(newid())")
                    .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                    .HasColumnName("rowguid");

                entity.HasOne(d => d.BusinessEntity).WithMany(p => p.SalesTerritoryHistories)
                    .HasForeignKey(d => d.BusinessEntityId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Territory).WithMany(p => p.SalesTerritoryHistories)
                    .HasForeignKey(d => d.TerritoryId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<ShoppingCartItem>(entity =>
            {
                entity.HasKey(e => e.ShoppingCartItemId).HasName("PK_ShoppingCartItem_ShoppingCartItemID");

                entity.ToTable("ShoppingCartItem", "Sales", tb => tb.HasComment("Contains online customer orders until the order is submitted or cancelled."));

                entity.HasIndex(e => new { e.ShoppingCartId, e.ProductId }, "IX_ShoppingCartItem_ShoppingCartID_ProductID");

                entity.Property(e => e.ShoppingCartItemId)
                    .HasComment("Primary key for ShoppingCartItem records.")
                    .HasColumnName("ShoppingCartItemID");
                entity.Property(e => e.DateCreated)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Date the time the record was created.")
                    .HasColumnType("datetime");
                entity.Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Date and time the record was last updated.")
                    .HasColumnType("datetime");
                entity.Property(e => e.ProductId)
                    .HasComment("Product ordered. Foreign key to Product.ProductID.")
                    .HasColumnName("ProductID");
                entity.Property(e => e.Quantity)
                    .HasDefaultValue(1)
                    .HasComment("Product quantity ordered.");
                entity.Property(e => e.ShoppingCartId)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasComment("Shopping cart identification number.")
                    .HasColumnName("ShoppingCartID");
            });

            modelBuilder.Entity<SpecialOffer>(entity =>
            {
                entity.HasKey(e => e.SpecialOfferId).HasName("PK_SpecialOffer_SpecialOfferID");

                entity.ToTable("SpecialOffer", "Sales", tb => tb.HasComment("Sale discounts lookup table."));

                entity.HasIndex(e => e.Rowguid, "AK_SpecialOffer_rowguid").IsUnique();

                entity.Property(e => e.SpecialOfferId)
                    .HasComment("Primary key for SpecialOffer records.")
                    .HasColumnName("SpecialOfferID");
                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasComment("Group the discount applies to such as Reseller or Customer.");
                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("Discount description.");
                entity.Property(e => e.DiscountPct)
                    .HasComment("Discount precentage.")
                    .HasColumnType("smallmoney");
                entity.Property(e => e.EndDate)
                    .HasComment("Discount end date.")
                    .HasColumnType("datetime");
                entity.Property(e => e.MaxQty).HasComment("Maximum discount percent allowed.");
                entity.Property(e => e.MinQty).HasComment("Minimum discount percent allowed.");
                entity.Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Date and time the record was last updated.")
                    .HasColumnType("datetime");
                entity.Property(e => e.Rowguid)
                    .HasDefaultValueSql("(newid())")
                    .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                    .HasColumnName("rowguid");
                entity.Property(e => e.StartDate)
                    .HasComment("Discount start date.")
                    .HasColumnType("datetime");
                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasComment("Discount type category.");
            });

            modelBuilder.Entity<SpecialOfferProduct>(entity =>
            {
                entity.HasKey(e => new { e.SpecialOfferId, e.ProductId }).HasName("PK_SpecialOfferProduct_SpecialOfferID_ProductID");

                entity.ToTable("SpecialOfferProduct", "Sales", tb => tb.HasComment("Cross-reference table mapping products to special offer discounts."));

                entity.HasIndex(e => e.Rowguid, "AK_SpecialOfferProduct_rowguid").IsUnique();

                entity.HasIndex(e => e.ProductId, "IX_SpecialOfferProduct_ProductID");

                entity.Property(e => e.SpecialOfferId)
                    .HasComment("Primary key for SpecialOfferProduct records.")
                    .HasColumnName("SpecialOfferID");
                entity.Property(e => e.ProductId)
                    .HasComment("Product identification number. Foreign key to Product.ProductID.")
                    .HasColumnName("ProductID");
                entity.Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Date and time the record was last updated.")
                    .HasColumnType("datetime");
                entity.Property(e => e.Rowguid)
                    .HasDefaultValueSql("(newid())")
                    .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                    .HasColumnName("rowguid");

                entity.HasOne(d => d.SpecialOffer).WithMany(p => p.SpecialOfferProducts)
                    .HasForeignKey(d => d.SpecialOfferId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Store>(entity =>
            {
                entity.HasKey(e => e.BusinessEntityId).HasName("PK_Store_BusinessEntityID");

                entity.ToTable("Store", "Sales", tb => tb.HasComment("Customers (resellers) of Adventure Works products."));

                entity.HasIndex(e => e.Rowguid, "AK_Store_rowguid").IsUnique();

                entity.HasIndex(e => e.SalesPersonId, "IX_Store_SalesPersonID");

                entity.HasIndex(e => e.Demographics, "PXML_Store_Demographics");

                entity.Property(e => e.BusinessEntityId)
                    .ValueGeneratedNever()
                    .HasComment("Primary key. Foreign key to Customer.BusinessEntityID.")
                    .HasColumnName("BusinessEntityID");
                entity.Property(e => e.Demographics)
                    .HasComment("Demographic informationg about the store such as the number of employees, annual sales and store type.")
                    .HasColumnType("xml");
                entity.Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasComment("Date and time the record was last updated.")
                    .HasColumnType("datetime");
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasComment("Name of the store.");
                entity.Property(e => e.Rowguid)
                    .HasDefaultValueSql("(newid())")
                    .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                    .HasColumnName("rowguid");
                entity.Property(e => e.SalesPersonId)
                    .HasComment("ID of the sales person assigned to the customer. Foreign key to SalesPerson.BusinessEntityID.")
                    .HasColumnName("SalesPersonID");

                entity.HasOne(d => d.SalesPerson).WithMany(p => p.Stores).HasForeignKey(d => d.SalesPersonId);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }

    public partial class CountryRegionCurrency
    {
        /// <summary>
        /// ISO code for countries and regions. Foreign key to CountryRegion.CountryRegionCode.
        /// </summary>
        public string CountryRegionCode { get; set; }

        /// <summary>
        /// ISO standard currency code. Foreign key to Currency.CurrencyCode.
        /// </summary>
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        public virtual Currency CurrencyCodeNavigation { get; set; }
    }

    public partial class CreditCard
    {
        /// <summary>
        /// Primary key for CreditCard records.
        /// </summary>
        public int CreditCardId { get; set; }

        /// <summary>
        /// Credit card name.
        /// </summary>
        public string CardType { get; set; }

        /// <summary>
        /// Credit card number.
        /// </summary>
        public string CardNumber { get; set; }

        /// <summary>
        /// Credit card expiration month.
        /// </summary>
        public byte ExpMonth { get; set; }

        /// <summary>
        /// Credit card expiration year.
        /// </summary>
        public short ExpYear { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        public virtual ICollection<PersonCreditCard> PersonCreditCards { get; set; } = new List<PersonCreditCard>();

        public virtual ICollection<SalesOrderHeader> SalesOrderHeaders { get; set; } = new List<SalesOrderHeader>();
    }

    public partial class Currency
    {
        /// <summary>
        /// The ISO code for the Currency.
        /// </summary>
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Currency name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        public virtual ICollection<CountryRegionCurrency> CountryRegionCurrencies { get; set; } = new List<CountryRegionCurrency>();

        public virtual ICollection<CurrencyRate> CurrencyRateFromCurrencyCodeNavigations { get; set; } = new List<CurrencyRate>();

        public virtual ICollection<CurrencyRate> CurrencyRateToCurrencyCodeNavigations { get; set; } = new List<CurrencyRate>();
    }

    public partial class CurrencyRate
    {
        /// <summary>
        /// Primary key for CurrencyRate records.
        /// </summary>
        public int CurrencyRateId { get; set; }

        /// <summary>
        /// Date and time the exchange rate was obtained.
        /// </summary>
        public DateTime CurrencyRateDate { get; set; }

        /// <summary>
        /// Exchange rate was converted from this currency code.
        /// </summary>
        public string FromCurrencyCode { get; set; }

        /// <summary>
        /// Exchange rate was converted to this currency code.
        /// </summary>
        public string ToCurrencyCode { get; set; }

        /// <summary>
        /// Average exchange rate for the day.
        /// </summary>
        public decimal AverageRate { get; set; }

        /// <summary>
        /// Final exchange rate for the day.
        /// </summary>
        public decimal EndOfDayRate { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        public virtual Currency FromCurrencyCodeNavigation { get; set; }

        public virtual ICollection<SalesOrderHeader> SalesOrderHeaders { get; set; } = new List<SalesOrderHeader>();

        public virtual Currency ToCurrencyCodeNavigation { get; set; }
    }

    public new partial class Customer
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Foreign key to Person.BusinessEntityID
        /// </summary>
        public int? PersonId { get; set; }

        /// <summary>
        /// Foreign key to Store.BusinessEntityID
        /// </summary>
        public int? StoreId { get; set; }

        /// <summary>
        /// ID of the territory in which the customer is located. Foreign key to SalesTerritory.SalesTerritoryID.
        /// </summary>
        public int? TerritoryId { get; set; }

        /// <summary>
        /// Unique number identifying the customer assigned by the accounting system.
        /// </summary>
        public string AccountNumber { get; set; }

        /// <summary>
        /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
        /// </summary>
        public Guid Rowguid { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        public virtual ICollection<SalesOrderHeader> SalesOrderHeaders { get; set; } = new List<SalesOrderHeader>();

        public virtual Store Store { get; set; }

        public virtual SalesTerritory Territory { get; set; }
    }

    public partial class PersonCreditCard
    {
        /// <summary>
        /// Business entity identification number. Foreign key to Person.BusinessEntityID.
        /// </summary>
        public int BusinessEntityId { get; set; }

        /// <summary>
        /// Credit card identification number. Foreign key to CreditCard.CreditCardID.
        /// </summary>
        public int CreditCardId { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        public virtual CreditCard CreditCard { get; set; }
    }

    public partial class SalesOrderDetail
    {
        /// <summary>
        /// Primary key. Foreign key to SalesOrderHeader.SalesOrderID.
        /// </summary>
        public int SalesOrderId { get; set; }

        /// <summary>
        /// Primary key. One incremental unique number per product sold.
        /// </summary>
        public int SalesOrderDetailId { get; set; }

        /// <summary>
        /// Shipment tracking number supplied by the shipper.
        /// </summary>
        public string CarrierTrackingNumber { get; set; }

        /// <summary>
        /// Quantity ordered per product.
        /// </summary>
        public short OrderQty { get; set; }

        /// <summary>
        /// Product sold to customer. Foreign key to Product.ProductID.
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Promotional code. Foreign key to SpecialOffer.SpecialOfferID.
        /// </summary>
        public int SpecialOfferId { get; set; }

        /// <summary>
        /// Selling price of a single product.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Discount amount.
        /// </summary>
        public decimal UnitPriceDiscount { get; set; }

        /// <summary>
        /// Per product subtotal. Computed as UnitPrice * (1 - UnitPriceDiscount) * OrderQty.
        /// </summary>
        public decimal LineTotal { get; set; }

        /// <summary>
        /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
        /// </summary>
        public Guid Rowguid { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        public virtual SalesOrderHeader SalesOrder { get; set; }

        public virtual SpecialOfferProduct SpecialOfferProduct { get; set; }
    }

    public partial class SalesOrderHeader
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public int SalesOrderId { get; set; }

        /// <summary>
        /// Incremental number to track changes to the sales order over time.
        /// </summary>
        public byte RevisionNumber { get; set; }

        /// <summary>
        /// Dates the sales order was created.
        /// </summary>
        public DateTime OrderDate { get; set; }

        /// <summary>
        /// Date the order is due to the customer.
        /// </summary>
        public DateTime DueDate { get; set; }

        /// <summary>
        /// Date the order was shipped to the customer.
        /// </summary>
        public DateTime? ShipDate { get; set; }

        /// <summary>
        /// Order current status. 1 = In process; 2 = Approved; 3 = Backordered; 4 = Rejected; 5 = Shipped; 6 = Cancelled
        /// </summary>
        public byte Status { get; set; }

        /// <summary>
        /// 0 = Order placed by sales person. 1 = Order placed online by customer.
        /// </summary>
        public bool OnlineOrderFlag { get; set; }

        /// <summary>
        /// Unique sales order identification number.
        /// </summary>
        public string SalesOrderNumber { get; set; }

        /// <summary>
        /// Customer purchase order number reference. 
        /// </summary>
        public string PurchaseOrderNumber { get; set; }

        /// <summary>
        /// Financial accounting number reference.
        /// </summary>
        public string AccountNumber { get; set; }

        /// <summary>
        /// Customer identification number. Foreign key to Customer.BusinessEntityID.
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Sales person who created the sales order. Foreign key to SalesPerson.BusinessEntityID.
        /// </summary>
        public int? SalesPersonId { get; set; }

        /// <summary>
        /// Territory in which the sale was made. Foreign key to SalesTerritory.SalesTerritoryID.
        /// </summary>
        public int? TerritoryId { get; set; }

        /// <summary>
        /// Customer billing address. Foreign key to Address.AddressID.
        /// </summary>
        public int BillToAddressId { get; set; }

        /// <summary>
        /// Customer shipping address. Foreign key to Address.AddressID.
        /// </summary>
        public int ShipToAddressId { get; set; }

        /// <summary>
        /// Shipping method. Foreign key to ShipMethod.ShipMethodID.
        /// </summary>
        public int ShipMethodId { get; set; }

        /// <summary>
        /// Credit card identification number. Foreign key to CreditCard.CreditCardID.
        /// </summary>
        public int? CreditCardId { get; set; }

        /// <summary>
        /// Approval code provided by the credit card company.
        /// </summary>
        public string CreditCardApprovalCode { get; set; }

        /// <summary>
        /// Currency exchange rate used. Foreign key to CurrencyRate.CurrencyRateID.
        /// </summary>
        public int? CurrencyRateId { get; set; }

        /// <summary>
        /// Sales subtotal. Computed as SUM(SalesOrderDetail.LineTotal)for the appropriate SalesOrderID.
        /// </summary>
        public decimal SubTotal { get; set; }

        /// <summary>
        /// Tax amount.
        /// </summary>
        public decimal TaxAmt { get; set; }

        /// <summary>
        /// Shipping cost.
        /// </summary>
        public decimal Freight { get; set; }

        /// <summary>
        /// Total due from customer. Computed as Subtotal + TaxAmt + Freight.
        /// </summary>
        public decimal TotalDue { get; set; }

        /// <summary>
        /// Sales representative comments.
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
        /// </summary>
        public Guid Rowguid { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        public virtual CreditCard CreditCard { get; set; }

        public virtual CurrencyRate CurrencyRate { get; set; }

        public virtual Customer Customer { get; set; }

        public virtual ICollection<SalesOrderDetail> SalesOrderDetails { get; set; } = new List<SalesOrderDetail>();

        public virtual ICollection<SalesOrderHeaderSalesReason> SalesOrderHeaderSalesReasons { get; set; } = new List<SalesOrderHeaderSalesReason>();

        public virtual SalesPerson SalesPerson { get; set; }

        public virtual SalesTerritory Territory { get; set; }
    }

    public partial class SalesOrderHeaderSalesReason
    {
        /// <summary>
        /// Primary key. Foreign key to SalesOrderHeader.SalesOrderID.
        /// </summary>
        public int SalesOrderId { get; set; }

        /// <summary>
        /// Primary key. Foreign key to SalesReason.SalesReasonID.
        /// </summary>
        public int SalesReasonId { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        public virtual SalesOrderHeader SalesOrder { get; set; }

        public virtual SalesReason SalesReason { get; set; }
    }

    public partial class SalesPerson
    {
        /// <summary>
        /// Primary key for SalesPerson records. Foreign key to Employee.BusinessEntityID
        /// </summary>
        public int BusinessEntityId { get; set; }

        /// <summary>
        /// Territory currently assigned to. Foreign key to SalesTerritory.SalesTerritoryID.
        /// </summary>
        public int? TerritoryId { get; set; }

        /// <summary>
        /// Projected yearly sales.
        /// </summary>
        public decimal? SalesQuota { get; set; }

        /// <summary>
        /// Bonus due if quota is met.
        /// </summary>
        public decimal Bonus { get; set; }

        /// <summary>
        /// Commision percent received per sale.
        /// </summary>
        public decimal CommissionPct { get; set; }

        /// <summary>
        /// Sales total year to date.
        /// </summary>
        public decimal SalesYtd { get; set; }

        /// <summary>
        /// Sales total of previous year.
        /// </summary>
        public decimal SalesLastYear { get; set; }

        /// <summary>
        /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
        /// </summary>
        public Guid Rowguid { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        public virtual ICollection<SalesOrderHeader> SalesOrderHeaders { get; set; } = new List<SalesOrderHeader>();

        public virtual ICollection<SalesPersonQuotaHistory> SalesPersonQuotaHistories { get; set; } = new List<SalesPersonQuotaHistory>();

        public virtual ICollection<SalesTerritoryHistory> SalesTerritoryHistories { get; set; } = new List<SalesTerritoryHistory>();

        public virtual ICollection<Store> Stores { get; set; } = new List<Store>();

        public virtual SalesTerritory Territory { get; set; }
    }

    public partial class SalesPersonQuotaHistory
    {
        /// <summary>
        /// Sales person identification number. Foreign key to SalesPerson.BusinessEntityID.
        /// </summary>
        public int BusinessEntityId { get; set; }

        /// <summary>
        /// Sales quota date.
        /// </summary>
        public DateTime QuotaDate { get; set; }

        /// <summary>
        /// Sales quota amount.
        /// </summary>
        public decimal SalesQuota { get; set; }

        /// <summary>
        /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
        /// </summary>
        public Guid Rowguid { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        public virtual SalesPerson BusinessEntity { get; set; }
    }

    public partial class SalesReason
    {
        /// <summary>
        /// Primary key for SalesReason records.
        /// </summary>
        public int SalesReasonId { get; set; }

        /// <summary>
        /// Sales reason description.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Category the sales reason belongs to.
        /// </summary>
        public string ReasonType { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        public virtual ICollection<SalesOrderHeaderSalesReason> SalesOrderHeaderSalesReasons { get; set; } = new List<SalesOrderHeaderSalesReason>();
    }

    public partial class SalesTaxRate
    {
        /// <summary>
        /// Primary key for SalesTaxRate records.
        /// </summary>
        public int SalesTaxRateId { get; set; }

        /// <summary>
        /// State, province, or country/region the sales tax applies to.
        /// </summary>
        public int StateProvinceId { get; set; }

        /// <summary>
        /// 1 = Tax applied to retail transactions, 2 = Tax applied to wholesale transactions, 3 = Tax applied to all sales (retail and wholesale) transactions.
        /// </summary>
        public byte TaxType { get; set; }

        /// <summary>
        /// Tax rate amount.
        /// </summary>
        public decimal TaxRate { get; set; }

        /// <summary>
        /// Tax rate description.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
        /// </summary>
        public Guid Rowguid { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }
    }

    public partial class SalesTerritory
    {
        /// <summary>
        /// Primary key for SalesTerritory records.
        /// </summary>
        public int TerritoryId { get; set; }

        /// <summary>
        /// Sales territory description
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// ISO standard country or region code. Foreign key to CountryRegion.CountryRegionCode. 
        /// </summary>
        public string CountryRegionCode { get; set; }

        /// <summary>
        /// Geographic area to which the sales territory belong.
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Sales in the territory year to date.
        /// </summary>
        public decimal SalesYtd { get; set; }

        /// <summary>
        /// Sales in the territory the previous year.
        /// </summary>
        public decimal SalesLastYear { get; set; }

        /// <summary>
        /// Business costs in the territory year to date.
        /// </summary>
        public decimal CostYtd { get; set; }

        /// <summary>
        /// Business costs in the territory the previous year.
        /// </summary>
        public decimal CostLastYear { get; set; }

        /// <summary>
        /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
        /// </summary>
        public Guid Rowguid { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();

        public virtual ICollection<SalesOrderHeader> SalesOrderHeaders { get; set; } = new List<SalesOrderHeader>();

        public virtual ICollection<SalesPerson> SalesPeople { get; set; } = new List<SalesPerson>();

        public virtual ICollection<SalesTerritoryHistory> SalesTerritoryHistories { get; set; } = new List<SalesTerritoryHistory>();
    }

    public partial class SalesTerritoryHistory
    {
        /// <summary>
        /// Primary key. The sales rep.  Foreign key to SalesPerson.BusinessEntityID.
        /// </summary>
        public int BusinessEntityId { get; set; }

        /// <summary>
        /// Primary key. Territory identification number. Foreign key to SalesTerritory.SalesTerritoryID.
        /// </summary>
        public int TerritoryId { get; set; }

        /// <summary>
        /// Primary key. Date the sales representive started work in the territory.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Date the sales representative left work in the territory.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
        /// </summary>
        public Guid Rowguid { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        public virtual SalesPerson BusinessEntity { get; set; }

        public virtual SalesTerritory Territory { get; set; }
    }

    public partial class ShoppingCartItem
    {
        /// <summary>
        /// Primary key for ShoppingCartItem records.
        /// </summary>
        public int ShoppingCartItemId { get; set; }

        /// <summary>
        /// Shopping cart identification number.
        /// </summary>
        public string ShoppingCartId { get; set; }

        /// <summary>
        /// Product quantity ordered.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Product ordered. Foreign key to Product.ProductID.
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Date the time the record was created.
        /// </summary>
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }
    }

    public partial class SpecialOffer
    {
        /// <summary>
        /// Primary key for SpecialOffer records.
        /// </summary>
        public int SpecialOfferId { get; set; }

        /// <summary>
        /// Discount description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Discount precentage.
        /// </summary>
        public decimal DiscountPct { get; set; }

        /// <summary>
        /// Discount type category.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Group the discount applies to such as Reseller or Customer.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Discount start date.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Discount end date.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Minimum discount percent allowed.
        /// </summary>
        public int MinQty { get; set; }

        /// <summary>
        /// Maximum discount percent allowed.
        /// </summary>
        public int? MaxQty { get; set; }

        /// <summary>
        /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
        /// </summary>
        public Guid Rowguid { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        public virtual ICollection<SpecialOfferProduct> SpecialOfferProducts { get; set; } = new List<SpecialOfferProduct>();
    }

    public partial class SpecialOfferProduct
    {
        /// <summary>
        /// Primary key for SpecialOfferProduct records.
        /// </summary>
        public int SpecialOfferId { get; set; }

        /// <summary>
        /// Product identification number. Foreign key to Product.ProductID.
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
        /// </summary>
        public Guid Rowguid { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        public virtual ICollection<SalesOrderDetail> SalesOrderDetails { get; set; } = new List<SalesOrderDetail>();

        public virtual SpecialOffer SpecialOffer { get; set; }
    }

    public partial class Store
    {
        /// <summary>
        /// Primary key. Foreign key to Customer.BusinessEntityID.
        /// </summary>
        public int BusinessEntityId { get; set; }

        /// <summary>
        /// Name of the store.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// ID of the sales person assigned to the customer. Foreign key to SalesPerson.BusinessEntityID.
        /// </summary>
        public int? SalesPersonId { get; set; }

        /// <summary>
        /// Demographic informationg about the store such as the number of employees, annual sales and store type.
        /// </summary>
        public string Demographics { get; set; }

        /// <summary>
        /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
        /// </summary>
        public Guid Rowguid { get; set; }

        /// <summary>
        /// Date and time the record was last updated.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();

        public virtual SalesPerson SalesPerson { get; set; }
    }













}
