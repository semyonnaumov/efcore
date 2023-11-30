// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ReSharper disable InconsistentNaming

using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Microsoft.EntityFrameworkCore.Query;

public class SimpleQuerySqlServerTest : SimpleQueryRelationalTestBase
{
    protected override ITestStoreFactory TestStoreFactory
        => SqlServerTestStoreFactory.Instance;

    #region 9214

    [ConditionalFact]
    public async Task Default_schema_applied_when_no_function_schema()
    {
        var contextFactory = await InitializeAsync<Context9214>(seed: c => c.Seed());

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

    protected class Context9214 : DbContext
    {
        public DbSet<Widget9214> Widgets { get; set; }

#pragma warning disable IDE0060 // Remove unused parameter
        public static int AddOne(int num)
            => throw new Exception();

        public static int AddTwo(int num)
            => throw new Exception();

        public static int AddThree(int num)
            => throw new Exception();
#pragma warning restore IDE0060 // Remove unused parameter

        public Context9214(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("foo");

            modelBuilder.Entity<Widget9214>().ToTable("Widgets", "foo");

            modelBuilder.HasDbFunction(typeof(Context9214).GetMethod(nameof(AddOne)));
            modelBuilder.HasDbFunction(typeof(Context9214).GetMethod(nameof(AddTwo))).HasSchema("dbo");
        }

        public void Seed()
        {
            var w1 = new Widget9214 { Val = 1 };
            var w2 = new Widget9214 { Val = 2 };
            var w3 = new Widget9214 { Val = 3 };
            Widgets.AddRange(w1, w2, w3);
            SaveChanges();

            Database.ExecuteSqlRaw(
"""
CREATE FUNCTION foo.AddOne (@num int)
RETURNS int
    AS
BEGIN
    return @num + 1 ;
END
""");


            Database.ExecuteSqlRaw(
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
        var contextFactory = await InitializeAsync<Context9277>(seed: c => c.Seed());

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

    protected class Context9277 : DbContext
    {
        public Context9277(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Blog9277> Blogs { get; set; }

        public void Seed()
        {
            Database.ExecuteSqlRaw(
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

            SaveChanges();
        }

        public class Blog9277
        {
            public int Id { get; set; }
            public int SomeValue { get; set; }
        }
    }

    #endregion

    #region 13118

    [ConditionalFact]
    public virtual async Task DateTime_Contains_with_smalldatetime_generates_correct_literal()
    {
        var contextFactory = await InitializeAsync<Context13118>(seed: c => c.Seed());
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

    private class Context13118 : DbContext
    {
        public virtual DbSet<ReproEntity13118> ReproEntity { get; set; }

        public Context13118(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<ReproEntity13118>(e => e.Property("MyTime").HasColumnType("smalldatetime"));

        public void Seed()
        {
            AddRange(
                new ReproEntity13118 { MyTime = new DateTime(2018, 10, 07) },
                new ReproEntity13118 { MyTime = new DateTime(2018, 10, 08) });

            SaveChanges();
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
        var contextFactory = await InitializeAsync<Context14095>(seed: c => c.Seed());

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
        var contextFactory = await InitializeAsync<Context14095>(seed: c => c.Seed());

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
        var contextFactory = await InitializeAsync<Context14095>(seed: c => c.Seed());

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

        var contextFactory = await InitializeAsync<Context14095>(seed: c => c.Seed());

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

        // TODO: DateTime values in the parameters should reflect their store type, see #32515
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

    protected class Context14095 : DbContext
    {
        public Context14095(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<DatesAndPrunes14095> Dates { get; set; }

        public void Seed()
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
            SaveChanges();
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
) AS [t]
CROSS APPLY [dbo].[GetPersonStatusAsOf]([t].[PersonId], [t].[Timestamp]) AS [g]
ORDER BY [t].[Id]
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

    private class Context24216 : DbContext
    {
        public Context24216(DbContextOptions options)
            : base(options)
        {
        }

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
                    new[] { typeof(long), typeof(DateTime) }));
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

    protected class Context27427 : DbContext
    {
        public Context27427(DbContextOptions options)
            : base(options)
        {
        }

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
        var contextFactory = await InitializeAsync<Context30478>(seed: x => x.Seed());
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
        var contextFactory = await InitializeAsync<Context30478>(seed: x => x.Seed());
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
        var contextFactory = await InitializeAsync<Context30478>(seed: x => x.Seed());
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
        var contextFactory = await InitializeAsync<Context30478>(seed: x => x.Seed());
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

    protected class Context30478 : DbContext
    {
        public Context30478(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Entity30478> Entities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Entity30478>().Property(x => x.Id).ValueGeneratedNever();
            modelBuilder.Entity<Entity30478>().ToTable("Entities", tb => tb.IsTemporal());
            modelBuilder.Entity<Entity30478>().OwnsOne(x => x.Reference, nb =>
            {
                nb.ToJson();
                nb.OwnsOne(x => x.Nested);
            });

            modelBuilder.Entity<Entity30478>().OwnsMany(x => x.Collection, nb =>
            {
                nb.ToJson();
                nb.OwnsOne(x => x.Nested);
            });
        }

        public void Seed()
        {
            var e1 = new Entity30478
            {
                Id = 1,
                Name = "e1",
                Reference = new Json30478
                {
                    Name = "r1",
                    Nested = new JsonNested30478 { Number = 1 }
                },
                Collection = new List<Json30478>
                {
                    new Json30478
                    {
                        Name = "c11",
                        Nested = new JsonNested30478 { Number = 11 }
                    },
                    new Json30478
                    {
                        Name = "c12",
                        Nested = new JsonNested30478 { Number = 12 }
                    },
                    new Json30478
                    {
                        Name = "c13",
                        Nested = new JsonNested30478 { Number = 12 }
                    }
                }
            };

            var e2 = new Entity30478
            {
                Id = 2,
                Name = "e2",
                Reference = new Json30478
                {
                    Name = "r2",
                    Nested = new JsonNested30478 { Number = 2 }
                },
                Collection = new List<Json30478>
                {
                    new Json30478
                    {
                        Name = "c21",
                        Nested = new JsonNested30478 { Number = 21 }
                    },
                    new Json30478
                    {
                        Name = "c22",
                        Nested = new JsonNested30478 { Number = 22 }
                    },
                }
            };

            AddRange(e1, e2);
            SaveChanges();

            RemoveRange(e1, e2);
            SaveChanges();


            Database.ExecuteSqlRaw($"ALTER TABLE [Entities] SET (SYSTEM_VERSIONING = OFF)");
            Database.ExecuteSqlRaw($"ALTER TABLE [Entities] DROP PERIOD FOR SYSTEM_TIME");

            Database.ExecuteSqlRaw($"UPDATE [EntitiesHistory] SET PeriodStart = '2000-01-01T01:00:00.0000000Z'");
            Database.ExecuteSqlRaw($"UPDATE [EntitiesHistory] SET PeriodEnd = '2020-07-01T07:00:00.0000000Z'");

            Database.ExecuteSqlRaw($"ALTER TABLE [Entities] ADD PERIOD FOR SYSTEM_TIME ([PeriodStart], [PeriodEnd])");
            Database.ExecuteSqlRaw($"ALTER TABLE [Entities] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[EntitiesHistory]))");
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

    public override async Task ThenInclude_with_interface_navigations()
    {
        await base.ThenInclude_with_interface_navigations();

        AssertSql(
"""
SELECT [p].[Id], [t].[Id], [t].[ParentBackNavigationId], [t].[SelfReferenceBackNavigationId], [t].[Id0], [t].[ParentBackNavigationId0], [t].[SelfReferenceBackNavigationId0]
FROM [Parents] AS [p]
LEFT JOIN (
    SELECT [c].[Id], [c].[ParentBackNavigationId], [c].[SelfReferenceBackNavigationId], [c0].[Id] AS [Id0], [c0].[ParentBackNavigationId] AS [ParentBackNavigationId0], [c0].[SelfReferenceBackNavigationId] AS [SelfReferenceBackNavigationId0]
    FROM [Children] AS [c]
    LEFT JOIN [Children] AS [c0] ON [c].[Id] = [c0].[SelfReferenceBackNavigationId]
) AS [t] ON [p].[Id] = [t].[ParentBackNavigationId]
ORDER BY [p].[Id], [t].[Id]
""",
                //
                """
SELECT [c0].[Id], [c0].[ParentBackNavigationId], [c0].[SelfReferenceBackNavigationId], [p].[Id]
FROM [Children] AS [c]
LEFT JOIN [Children] AS [c0] ON [c].[SelfReferenceBackNavigationId] = [c0].[Id]
LEFT JOIN [Parents] AS [p] ON [c0].[ParentBackNavigationId] = [p].[Id]
""",
                //
                """
SELECT [c0].[Id], [c0].[ParentBackNavigationId], [c0].[SelfReferenceBackNavigationId], [p].[Id]
FROM [Children] AS [c]
LEFT JOIN [Children] AS [c0] ON [c].[SelfReferenceBackNavigationId] = [c0].[Id]
LEFT JOIN [Parents] AS [p] ON [c0].[ParentBackNavigationId] = [p].[Id]
""",
                //
                """
SELECT [c].[Id], [c].[ParentBackNavigationId], [c].[SelfReferenceBackNavigationId], [c0].[Id], [c0].[ParentBackNavigationId], [c0].[SelfReferenceBackNavigationId], [p].[Id]
FROM [Children] AS [c]
LEFT JOIN [Children] AS [c0] ON [c].[SelfReferenceBackNavigationId] = [c0].[Id]
LEFT JOIN [Parents] AS [p] ON [c0].[ParentBackNavigationId] = [p].[Id]
""");
    }

    public override async Task Reference_include_on_derived_type_with_sibling_works()
    {
        await base.Reference_include_on_derived_type_with_sibling_works();

        AssertSql(
"""
SELECT [p].[Id], [p].[Discriminator], [p].[LeaveStart], [p].[LeaveTypeId], [p0].[Id]
FROM [Proposal] AS [p]
LEFT JOIN [ProposalLeaveType7312] AS [p0] ON [p].[LeaveTypeId] = [p0].[Id]
WHERE [p].[Discriminator] = N'ProposalLeave7312'
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

    public override async Task Include_collection_optional_reference_collection()
    {
        await base.Include_collection_optional_reference_collection();

        AssertSql(
"""
ORDER BY [p].[Id]
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
LEFT JOIN [Configuration9468] AS [c0] ON [c].[ConfigurationId] = [c0].[Id]
""");
    }

    public override async Task Include_with_order_by_on_interface_key()
    {
        await base.Include_with_order_by_on_interface_key();

        AssertSql(
"""
SELECT [p].[Id], [p].[Name], [c].[Id], [c].[Name], [c].[Parent10635Id], [c].[ParentId]
FROM [Parents] AS [p]
LEFT JOIN [Children] AS [c] ON [p].[Id] = [c].[Parent10635Id]
ORDER BY [p].[Id]
""",
                //
                """
SELECT [p].[Id], [c].[Id], [c].[Name], [c].[Parent10635Id], [c].[ParentId]
FROM [Parents] AS [p]
LEFT JOIN [Children] AS [c] ON [p].[Id] = [c].[Parent10635Id]
ORDER BY [p].[Id]
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

    public override async Task Collection_without_setter_materialized_correctly()
    {
        await base.Collection_without_setter_materialized_correctly();

        AssertSql(
"""
SELECT [b].[Id], [p].[Id], [p].[BlogId1], [p].[BlogId2], [p].[BlogId3], [p].[Name], [p0].[Id], [p0].[BlogId1], [p0].[BlogId2], [p0].[BlogId3], [p0].[Name], [p1].[Id], [p1].[BlogId1], [p1].[BlogId2], [p1].[BlogId3], [p1].[Name]
FROM [Blogs] AS [b]
LEFT JOIN [Posts] AS [p] ON [b].[Id] = [p].[BlogId1]
LEFT JOIN [Posts] AS [p0] ON [b].[Id] = [p0].[BlogId2]
LEFT JOIN [Posts] AS [p1] ON [b].[Id] = [p1].[BlogId3]
ORDER BY [b].[Id], [p].[Id], [p0].[Id]
""",
                //
                """
SELECT (
    SELECT TOP(1) (
        SELECT COUNT(*)
        FROM [Comments] AS [c]
        WHERE [p].[Id] = [c].[Post11923Id])
    FROM [Posts] AS [p]
    WHERE [b].[Id] = [p].[BlogId1]
    ORDER BY [p].[Id]) AS [Collection1], (
    SELECT TOP(1) (
        SELECT COUNT(*)
        FROM [Comments] AS [c0]
        WHERE [p0].[Id] = [c0].[Post11923Id])
    FROM [Posts] AS [p0]
    WHERE [b].[Id] = [p0].[BlogId2]
    ORDER BY [p0].[Id]) AS [Collection2], (
    SELECT TOP(1) (
        SELECT COUNT(*)
        FROM [Comments] AS [c1]
        WHERE [p1].[Id] = [c1].[Post11923Id])
    FROM [Posts] AS [p1]
    WHERE [b].[Id] = [p1].[BlogId3]
    ORDER BY [p1].[Id]) AS [Collection3]
FROM [Blogs] AS [b]
""");
    }

    public override async Task Include_collection_with_OfType_base()
    {
        await base.Include_collection_with_OfType_base();

        AssertSql(
"""
SELECT [e].[Id], [e].[Name], [d].[Id], [d].[Device], [d].[EmployeeId]
FROM [Employees] AS [e]
LEFT JOIN [Devices] AS [d] ON [e].[Id] = [d].[EmployeeId]
ORDER BY [e].[Id]
""",
                //
                """
SELECT [e].[Id], [t].[Id], [t].[Device], [t].[EmployeeId]
FROM [Employees] AS [e]
LEFT JOIN (
    SELECT [d].[Id], [d].[Device], [d].[EmployeeId]
    FROM [Devices] AS [d]
    WHERE [d].[Device] <> N'foo' OR [d].[Device] IS NULL
) AS [t] ON [e].[Id] = [t].[EmployeeId]
ORDER BY [e].[Id]
""");
    }

    public override async Task Correlated_collection_correctly_associates_entities_with_byte_array_keys()
    {
        await base.Correlated_collection_correctly_associates_entities_with_byte_array_keys();

        AssertSql(
"""
SELECT [b].[Name], [c].[Id]
FROM [Blogs] AS [b]
LEFT JOIN [Comments] AS [c] ON [b].[Name] = [c].[BlogName]
ORDER BY [b].[Name]
""");
    }

    public override async Task Shadow_property_with_inheritance()
    {
        await base.Shadow_property_with_inheritance();

        AssertSql(
"""
SELECT [c].[Id], [c].[Discriminator], [c].[IsPrimary], [c].[UserName], [c].[Employer6986Id], [c].[ServiceOperator6986Id]
FROM [Contacts] AS [c]
""",
                //
                """
SELECT [c].[Id], [c].[Discriminator], [c].[IsPrimary], [c].[UserName], [c].[ServiceOperator6986Id], [s].[Id]
FROM [Contacts] AS [c]
INNER JOIN [ServiceOperators] AS [s] ON [c].[ServiceOperator6986Id] = [s].[Id]
WHERE [c].[Discriminator] = N'ServiceOperatorContact6986'
""",
                //
                """
SELECT [c].[Id], [c].[Discriminator], [c].[IsPrimary], [c].[UserName], [c].[ServiceOperator6986Id]
FROM [Contacts] AS [c]
WHERE [c].[Discriminator] = N'ServiceOperatorContact6986'
""");
    }


    public override async Task GroupJoin_Anonymous_projection_GroupBy_Aggregate_join_elimination()
    {
        await base.GroupJoin_Anonymous_projection_GroupBy_Aggregate_join_elimination();

        AssertSql(
"""
SELECT [t0].[AnotherEntity11818_Name] AS [Key], COUNT(*) + 5 AS [cnt]
FROM [Table] AS [t]
LEFT JOIN (
    SELECT [t1].[Id], [t1].[Exists], [t1].[AnotherEntity11818_Name]
    FROM [Table] AS [t1]
    WHERE [t1].[Exists] IS NOT NULL
) AS [t0] ON [t].[Id] = CASE
    WHEN [t0].[Exists] IS NOT NULL THEN [t0].[Id]
END
GROUP BY [t0].[AnotherEntity11818_Name]
""",
                //
                """
SELECT [t0].[AnotherEntity11818_Name] AS [MyKey], COUNT(*) + 5 AS [cnt]
FROM [Table] AS [t]
LEFT JOIN (
    SELECT [t1].[Id], [t1].[Exists], [t1].[AnotherEntity11818_Name]
    FROM [Table] AS [t1]
    WHERE [t1].[Exists] IS NOT NULL
) AS [t0] ON [t].[Id] = CASE
    WHEN [t0].[Exists] IS NOT NULL THEN [t0].[Id]
END
LEFT JOIN (
    SELECT [t3].[Id], [t3].[MaumarEntity11818_Exists], [t3].[MaumarEntity11818_Name]
    FROM [Table] AS [t3]
    WHERE [t3].[MaumarEntity11818_Exists] IS NOT NULL
) AS [t2] ON [t].[Id] = CASE
    WHEN [t2].[MaumarEntity11818_Exists] IS NOT NULL THEN [t2].[Id]
END
GROUP BY [t0].[AnotherEntity11818_Name], [t2].[MaumarEntity11818_Name]
""",
                //
                """
SELECT TOP(1) [t0].[AnotherEntity11818_Name] AS [MyKey], [t2].[MaumarEntity11818_Name] AS [cnt]
FROM [Table] AS [t]
LEFT JOIN (
    SELECT [t1].[Id], [t1].[Exists], [t1].[AnotherEntity11818_Name]
    FROM [Table] AS [t1]
    WHERE [t1].[Exists] IS NOT NULL
) AS [t0] ON [t].[Id] = CASE
    WHEN [t0].[Exists] IS NOT NULL THEN [t0].[Id]
END
LEFT JOIN (
    SELECT [t3].[Id], [t3].[MaumarEntity11818_Exists], [t3].[MaumarEntity11818_Name]
    FROM [Table] AS [t3]
    WHERE [t3].[MaumarEntity11818_Exists] IS NOT NULL
) AS [t2] ON [t].[Id] = CASE
    WHEN [t2].[MaumarEntity11818_Exists] IS NOT NULL THEN [t2].[Id]
END
GROUP BY [t0].[AnotherEntity11818_Name], [t2].[MaumarEntity11818_Name]
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
FROM [Food] AS [f]
WHERE [f].[Taste] = CAST(1 AS tinyint)
""");
    }

    public override async Task Null_check_removal_in_ternary_maintain_appropriate_cast(bool async)
    {
        await base.Null_check_removal_in_ternary_maintain_appropriate_cast(async);

        AssertSql(
            """
SELECT CAST([f].[Taste] AS tinyint) AS [Bar]
FROM [Food] AS [f]
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

    public override async Task Count_member_over_IReadOnlyCollection_works(bool async)
    {
        await base.Count_member_over_IReadOnlyCollection_works(async);

        AssertSql(
            """
SELECT (
    SELECT COUNT(*)
    FROM [Books] AS [b]
    WHERE [a].[AuthorId] = [b].[AuthorId]) AS [BooksCount]
FROM [Authors] AS [a]
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
    SELECT [o0].[OrderId] AS [Key], MAX(CASE
        WHEN [o0].[ShippingDate] IS NULL AND [o0].[CancellationDate] IS NULL THEN [o0].[OrderId]
        ELSE [o0].[OrderId] - 10000000
    END) AS [IsPending]
    FROM [OrderItems] AS [o0]
    WHERE [o0].[OrderId] = @__orderId_0
    GROUP BY [o0].[OrderId]
) AS [t] ON [o].[OrderId] = [t].[Key]
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
    SELECT TOP(1) [o2].[Price]
    FROM [OrderItems] AS [o2]
    WHERE [o1].[Id] = [o2].[Order26472Id] AND [o2].[Type] = @__orderItemType_1), 0.0E0) AS [SpecialSum]
FROM (
    SELECT TOP(@__p_0) [o].[Id]
    FROM [Orders] AS [o]
    WHERE EXISTS (
        SELECT 1
        FROM [OrderItems] AS [o0]
        WHERE [o].[Id] = [o0].[Order26472Id])
    ORDER BY [o].[Id]
) AS [t]
INNER JOIN [Orders] AS [o1] ON [t].[Id] = [o1].[Id]
ORDER BY [t].[Id]
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
    FROM [Table] AS [t0]
    WHERE [t0].[Value] = MAX([t].[Id]) * 6 OR ([t0].[Value] IS NULL AND MAX([t].[Id]) IS NULL)) AS [B]
FROM [Table] AS [t]
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
    FROM [Table] AS [t0]
    GROUP BY [t0].[Value]
    ORDER BY (SELECT 1)), 0) AS [C]
FROM [Table] AS [t]
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
    FROM [Child26744] AS [c1]
    WHERE [p].[Id] = [c1].[ParentId] AND [c1].[SomeNullableDateTime] IS NULL
    ORDER BY [c1].[SomeInteger])
FROM [Parents] AS [p]
WHERE EXISTS (
    SELECT 1
    FROM [Child26744] AS [c]
    WHERE [p].[Id] = [c].[ParentId] AND [c].[SomeNullableDateTime] IS NULL) AND (
    SELECT TOP(1) [c0].[SomeOtherNullableDateTime]
    FROM [Child26744] AS [c0]
    WHERE [p].[Id] = [c0].[ParentId] AND [c0].[SomeNullableDateTime] IS NULL
    ORDER BY [c0].[SomeInteger]) IS NOT NULL
""");
    }

    public override async Task SelectMany_where_Select(bool async)
    {
        await base.SelectMany_where_Select(async);

        AssertSql(
            """
SELECT [t0].[SomeNullableDateTime]
FROM [Parents] AS [p]
INNER JOIN (
    SELECT [t].[ParentId], [t].[SomeNullableDateTime], [t].[SomeOtherNullableDateTime]
    FROM (
        SELECT [c].[ParentId], [c].[SomeNullableDateTime], [c].[SomeOtherNullableDateTime], ROW_NUMBER() OVER(PARTITION BY [c].[ParentId] ORDER BY [c].[SomeInteger]) AS [row]
        FROM [Child26744] AS [c]
        WHERE [c].[SomeNullableDateTime] IS NULL
    ) AS [t]
    WHERE [t].[row] <= 1
) AS [t0] ON [p].[Id] = [t0].[ParentId]
WHERE [t0].[SomeOtherNullableDateTime] IS NOT NULL
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
FROM [TableData] AS [t]
INNER JOIN (
    SELECT DISTINCT [i].[Parcel]
    FROM [IndexData] AS [i]
    WHERE [i].[Parcel] = N'some condition'
    GROUP BY [i].[Parcel], [i].[RowId]
    HAVING COUNT(*) = 1
) AS [t0] ON [t].[ParcelNumber] = [t0].[Parcel]
WHERE [t].[TableId] = 123
ORDER BY [t].[ParcelNumber]
""");
    }

    public override async Task Hierarchy_query_with_abstract_type_sibling(bool async)
    {
        await base.Hierarchy_query_with_abstract_type_sibling(async);

        AssertSql(
            """
SELECT [a].[Id], [a].[Discriminator], [a].[Species], [a].[Name], [a].[EdcuationLevel], [a].[FavoriteToy]
FROM [Animals] AS [a]
WHERE [a].[Discriminator] IN (N'Cat', N'Dog') AND [a].[Species] LIKE N'F%'
""");
    }

    public override async Task Hierarchy_query_with_abstract_type_sibling_TPT(bool async)
    {
        await base.Hierarchy_query_with_abstract_type_sibling_TPT(async);

        AssertSql(
            """
SELECT [a].[Id], [a].[Species], [p].[Name], [c].[EdcuationLevel], [d].[FavoriteToy], CASE
    WHEN [d].[Id] IS NOT NULL THEN N'Dog'
    WHEN [c].[Id] IS NOT NULL THEN N'Cat'
END AS [Discriminator]
FROM [Animals] AS [a]
LEFT JOIN [Pets] AS [p] ON [a].[Id] = [p].[Id]
LEFT JOIN [Cats] AS [c] ON [a].[Id] = [c].[Id]
LEFT JOIN [Dogs] AS [d] ON [a].[Id] = [d].[Id]
WHERE ([d].[Id] IS NOT NULL OR [c].[Id] IS NOT NULL) AND [a].[Species] LIKE N'F%'
""");
    }

    public override async Task Hierarchy_query_with_abstract_type_sibling_TPC(bool async)
    {
        await base.Hierarchy_query_with_abstract_type_sibling_TPC(async);

        AssertSql(
            """
SELECT [t].[Id], [t].[Species], [t].[Name], [t].[EdcuationLevel], [t].[FavoriteToy], [t].[Discriminator]
FROM (
    SELECT [c].[Id], [c].[Species], [c].[Name], [c].[EdcuationLevel], NULL AS [FavoriteToy], N'Cat' AS [Discriminator]
    FROM [Cats] AS [c]
    UNION ALL
    SELECT [d].[Id], [d].[Species], [d].[Name], NULL AS [EdcuationLevel], [d].[FavoriteToy], N'Dog' AS [Discriminator]
    FROM [Dogs] AS [d]
) AS [t]
WHERE [t].[Species] LIKE N'F%'
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
}
