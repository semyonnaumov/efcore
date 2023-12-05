// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Query;

public class AdHocNavigationsQuerySqlServerTest : AdHocNavigationsQueryRelationalTestBase
{
    protected override ITestStoreFactory TestStoreFactory
        => SqlServerTestStoreFactory.Instance;

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
FROM [Proposals] AS [p]
LEFT JOIN [ProposalLeaveType] AS [p0] ON [p].[LeaveTypeId] = [p0].[Id]
WHERE [p].[Discriminator] = N'ProposalLeave'
""");
    }

    public override async Task Include_collection_optional_reference_collection()
    {
        await base.Include_collection_optional_reference_collection();

        AssertSql(
"""
SELECT [p].[Id], [p].[Discriminator], [p].[FamilyId], [p].[Name], [p].[TeacherId], [t].[Id], [t].[Discriminator], [t].[FamilyId], [t].[Name], [t].[TeacherId], [t].[Grade], [t].[Id0], [t].[LastName], [t].[Id1], [t].[Discriminator0], [t].[FamilyId0], [t].[Name0], [t].[TeacherId0], [t].[Grade0]
FROM [People] AS [p]
LEFT JOIN (
    SELECT [p0].[Id], [p0].[Discriminator], [p0].[FamilyId], [p0].[Name], [p0].[TeacherId], [p0].[Grade], [f].[Id] AS [Id0], [f].[LastName], [p1].[Id] AS [Id1], [p1].[Discriminator] AS [Discriminator0], [p1].[FamilyId] AS [FamilyId0], [p1].[Name] AS [Name0], [p1].[TeacherId] AS [TeacherId0], [p1].[Grade] AS [Grade0]
    FROM [People] AS [p0]
    LEFT JOIN [Families] AS [f] ON [p0].[FamilyId] = [f].[Id]
    LEFT JOIN [People] AS [p1] ON [f].[Id] = [p1].[FamilyId]
    WHERE [p0].[Discriminator] = N'PersonKid9038'
) AS [t] ON [p].[Id] = [t].[TeacherId]
WHERE [p].[Discriminator] = N'PersonTeacher9038'
ORDER BY [p].[Id], [t].[Id], [t].[Id0]
""",
                //
                """
SELECT [p].[Id], [p].[Discriminator], [p].[FamilyId], [p].[Name], [p].[TeacherId], [f].[Id], [f].[LastName], [p0].[Id], [p0].[Discriminator], [p0].[FamilyId], [p0].[Name], [p0].[TeacherId], [p0].[Grade], [t].[Id], [t].[Discriminator], [t].[FamilyId], [t].[Name], [t].[TeacherId], [t].[Grade]
FROM [People] AS [p]
LEFT JOIN [Families] AS [f] ON [p].[FamilyId] = [f].[Id]
LEFT JOIN [People] AS [p0] ON [f].[Id] = [p0].[FamilyId]
LEFT JOIN (
    SELECT [p1].[Id], [p1].[Discriminator], [p1].[FamilyId], [p1].[Name], [p1].[TeacherId], [p1].[Grade]
    FROM [People] AS [p1]
    WHERE [p1].[Discriminator] = N'PersonKid9038'
) AS [t] ON [p].[Id] = [t].[TeacherId]
WHERE [p].[Discriminator] = N'PersonTeacher9038'
ORDER BY [p].[Id], [f].[Id], [p0].[Id]
""");
    }

    public override async Task Nested_include_queries_do_not_populate_navigation_twice()
    {
        await base.Nested_include_queries_do_not_populate_navigation_twice();

        AssertSql(
"""
SELECT [b].[Id], [p].[Id], [p].[BlogId]
FROM [Blogs] AS [b]
LEFT JOIN [Post10447] AS [p] ON [b].[Id] = [p].[BlogId]
ORDER BY [b].[Id]
""",
                //
                """
SELECT [b].[Id], [p].[Id], [p].[BlogId]
FROM [Blogs] AS [b]
LEFT JOIN [Post10447] AS [p] ON [b].[Id] = [p].[BlogId]
ORDER BY [b].[Id]
""",
                //
                """
SELECT [b].[Id], [p].[Id], [p].[BlogId]
FROM [Blogs] AS [b]
LEFT JOIN [Post10447] AS [p] ON [b].[Id] = [p].[BlogId]
ORDER BY [b].[Id]
""",
                //
                """
SELECT [b].[Id], [p].[Id], [p].[BlogId]
FROM [Blogs] AS [b]
LEFT JOIN [Post10447] AS [p] ON [b].[Id] = [p].[BlogId]
ORDER BY [b].[Id]
""",
                //
                """
SELECT [b].[Id], [p].[Id], [p].[BlogId]
FROM [Blogs] AS [b]
LEFT JOIN [Post10447] AS [p] ON [b].[Id] = [p].[BlogId]
ORDER BY [b].[Id]
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

    public override async Task Include_collection_works_when_defined_on_intermediate_type()
    {
        await base.Include_collection_works_when_defined_on_intermediate_type();

        AssertSql(
"""
SELECT [s].[Id], [s].[Discriminator], [s0].[Id], [s0].[SchoolId]
FROM [Schools] AS [s]
LEFT JOIN [Students] AS [s0] ON [s].[Id] = [s0].[SchoolId]
ORDER BY [s].[Id]
""",
                //
                """
SELECT [s].[Id], [s0].[Id], [s0].[SchoolId]
FROM [Schools] AS [s]
LEFT JOIN [Students] AS [s0] ON [s].[Id] = [s0].[SchoolId]
ORDER BY [s].[Id]
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

    public override async Task Can_ignore_invalid_include_path_error()
    {
        await base.Can_ignore_invalid_include_path_error();

        AssertSql(
"""
SELECT [b].[Id], [b].[Discriminator], [b].[SubAId]
FROM [BaseClasses] AS [b]
WHERE [b].[Discriminator] = N'ClassA'
""");
    }

    public override async Task Cycles_in_auto_include()
    {
        await base.Cycles_in_auto_include();

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
""",
                //
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
""",
                //
                """
SELECT [p].[Id]
FROM [PrincipalManyToMany] AS [p]
""",
                //
                """
SELECT [d].[Id]
FROM [DependentManyToMany] AS [d]
""",
                //
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

    public override async Task Projection_with_multiple_includes_and_subquery_with_set_operation()
    {
        await base.Projection_with_multiple_includes_and_subquery_with_set_operation();

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
}
