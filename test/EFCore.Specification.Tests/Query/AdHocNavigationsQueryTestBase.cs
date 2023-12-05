// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;

namespace Microsoft.EntityFrameworkCore.Query;

public abstract class AdHocNavigationsQueryTestBase : NonSharedModelTestBase
{
    protected override string StoreName
        => "AdHocNavigationsQueryTests";

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
                            = EF.Property<Context3409.IChild>(c, "SelfReferenceBackNavigation"),
                        ParentBackNavigationB
                            = EF.Property<Context3409.IParent>(
                                EF.Property<Context3409.IChild>(c, "SelfReferenceBackNavigation"),
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
        public DbSet<Parent> Parents { get; set; }
        public DbSet<Child> Children { get; set; }

        public Context3409(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Parent>()
                .HasMany(p => (ICollection<Child>)p.ChildCollection)
                .WithOne(c => (Parent)c.ParentBackNavigation);

            modelBuilder.Entity<Child>()
                .HasMany(c => (ICollection<Child>)c.SelfReferenceCollection)
                .WithOne(c => (Child)c.SelfReferenceBackNavigation);
        }

        public void Seed()
        {
            var parent1 = new Parent();

            var child1 = new Child();
            var child2 = new Child();
            var child3 = new Child();

            parent1.ChildCollection = new List<IChild> { child1 };
            child1.SelfReferenceCollection = new List<IChild> { child2, child3 };

            Parents.AddRange(parent1);
            Children.AddRange(child1, child2, child3);

            SaveChanges();
        }

        public interface IParent
        {
            int Id { get; set; }

            ICollection<IChild> ChildCollection { get; set; }
        }

        public interface IChild
        {
            int Id { get; set; }

            int? ParentBackNavigationId { get; set; }
            IParent ParentBackNavigation { get; set; }

            ICollection<IChild> SelfReferenceCollection { get; set; }
            int? SelfReferenceBackNavigationId { get; set; }
            IChild SelfReferenceBackNavigation { get; set; }
        }

        public class Parent : IParent
        {
            public int Id { get; set; }

            public ICollection<IChild> ChildCollection { get; set; }
        }

        public class Child : IChild
        {
            public int Id { get; set; }

            public int? ParentBackNavigationId { get; set; }
            public IParent ParentBackNavigation { get; set; }

            public ICollection<IChild> SelfReferenceCollection { get; set; }
            public int? SelfReferenceBackNavigationId { get; set; }
            public IChild SelfReferenceBackNavigation { get; set; }
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
            var query = context.Proposals.OfType<Context7312.ProposalLeave>().Include(l => l.LeaveType).ToList();

            Assert.Single(query);
        }
    }

    private class Context7312 : DbContext
    {
        public Context7312(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Proposal> Proposals { get; set; }
        public DbSet<ProposalCustom> ProposalCustoms { get; set; }
        public DbSet<ProposalLeave> ProposalLeaves { get; set; }

        public void Seed()
        {
            AddRange(
                new Proposal(),
                new ProposalCustom { Name = "CustomProposal" },
                new ProposalLeave { LeaveStart = DateTime.Now, LeaveType = new ProposalLeaveType() }
            );
            SaveChanges();
        }

        public class Proposal
        {
            public int Id { get; set; }
        }

        public class ProposalCustom : Proposal
        {
            public string Name { get; set; }
        }

        public class ProposalLeave : Proposal
        {
            public DateTime LeaveStart { get; set; }
            public virtual ProposalLeaveType LeaveType { get; set; }
        }

        public class ProposalLeaveType
        {
            public int Id { get; set; }
            public ICollection<ProposalLeave> ProposalLeaves { get; set; }
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

    private class Context9038 : DbContext
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

    #region 10447

    [ConditionalFact]
    public virtual async Task Nested_include_queries_do_not_populate_navigation_twice()
    {
        var contextFactory = await InitializeAsync<Context10447>(seed: c => c.Seed());
        using var context = contextFactory.CreateContext();
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

    protected class Context10447 : DbContext
    {
        public DbSet<Blog> Blogs { get; set; }

        public Context10447(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        public void Seed()
        {
            AddRange(
                new Blog
                {
                    Posts = new List<Post>
                    {
                        new(),
                        new(),
                        new()
                    }
                },
                new Blog { Posts = new List<Post> { new(), new() } },
                new Blog { Posts = new List<Post> { new() } });

            SaveChanges();
        }

        public class Blog
        {
            public int Id { get; set; }
            public List<Post> Posts { get; set; }
        }

        public class Post
        {
            public int Id { get; set; }

            public Blog Blog { get; set; }
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

    #region 11944

    [ConditionalFact]
    public virtual async Task Include_collection_works_when_defined_on_intermediate_type()
    {
        var contextFactory = await InitializeAsync<Context11944>(seed: c => c.Seed());

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Schools.Include(s => ((Context11944.ElementarySchool)s).Students);
            var result = query.ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(2, result.OfType<Context11944.ElementarySchool>().Single().Students.Count);
        }

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Schools.Select(s => ((Context11944.ElementarySchool)s).Students.Where(ss => true).ToList());
            var result = query.ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Count() == 2);
        }
    }

    protected class Context11944 : DbContext
    {
        public DbSet<Student> Students { get; set; }
        public DbSet<School> Schools { get; set; }
        public DbSet<ElementarySchool> ElementarySchools { get; set; }

        public Context11944(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<ElementarySchool>().HasMany(s => s.Students).WithOne(s => s.School);

        public void Seed()
        {
            var student1 = new Student();
            var student2 = new Student();
            var school = new School();
            var elementarySchool = new ElementarySchool { Students = new List<Student> { student1, student2 } };

            Students.AddRange(student1, student2);
            Schools.AddRange(school);
            ElementarySchools.Add(elementarySchool);

            SaveChanges();
        }

        public class Student
        {
            public int Id { get; set; }
            public ElementarySchool School { get; set; }
        }

        public class School
        {
            public int Id { get; set; }
        }

        public abstract class PrimarySchool : School
        {
            public List<Student> Students { get; set; }
        }

        public class ElementarySchool : PrimarySchool
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
                .OfType<Context12582.IEmployee>()
                .ToList();

            Assert.Single(query);

            var employee = (Context12582.Employee)query[0];
            Assert.Equal(2, employee.Devices.Count);
        }

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Employees
                .Select(e => e.Devices.Where(d => d.Device != "foo").Cast<Context12582.IEmployeeDevice>())
                .ToList();

            Assert.Single(query);
            var result = query[0];
            Assert.Equal(2, result.Count());
        }
    }

    private class Context12582 : DbContext
    {
        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeDevice> Devices { get; set; }

        public Context12582(DbContextOptions options)
            : base(options)
        {
        }

        public void Seed()
        {
            var d1 = new EmployeeDevice { Device = "d1" };
            var d2 = new EmployeeDevice { Device = "d2" };
            var e = new Employee { Devices = new List<EmployeeDevice> { d1, d2 }, Name = "e" };

            Devices.AddRange(d1, d2);
            Employees.Add(e);
            SaveChanges();
        }

        public interface IEmployee
        {
            string Name { get; set; }
        }

        public class Employee : IEmployee
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public ICollection<EmployeeDevice> Devices { get; set; }
        }

        public interface IEmployeeDevice
        {
            string Device { get; set; }
        }

        public class EmployeeDevice : IEmployeeDevice
        {
            public int Id { get; set; }
            public int EmployeeId { get; set; }
            public string Device { get; set; }
            public Employee Employee { get; set; }
        }
    }

    #endregion

    #region 20609

    [ConditionalFact]
    public virtual async Task Can_ignore_invalid_include_path_error()
    {
        var contextFactory = await InitializeAsync<Context20609>(
            onConfiguring: o => o.ConfigureWarnings(x => x.Ignore(CoreEventId.InvalidIncludePathError)));

        using var context = contextFactory.CreateContext();
        var result = context.Set<Context20609.ClassA>().Include("SubB").ToList();
    }

    protected class Context20609 : DbContext
    {
        public Context20609(DbContextOptions options)
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

    #region 22568

    [ConditionalFact]
    public virtual async Task Cycles_in_auto_include()
    {
        var contextFactory = await InitializeAsync<Context22568>(seed: c => c.Seed());
        using (var context = contextFactory.CreateContext())
        {
            var principals = context.Set<Context22568.PrincipalOneToOne>().ToList();
            Assert.Single(principals);
            Assert.NotNull(principals[0].Dependent);
            Assert.NotNull(principals[0].Dependent.Principal);

            var dependents = context.Set<Context22568.DependentOneToOne>().ToList();
            Assert.Single(dependents);
            Assert.NotNull(dependents[0].Principal);
            Assert.NotNull(dependents[0].Principal.Dependent);
        }

        using (var context = contextFactory.CreateContext())
        {
            var principals = context.Set<Context22568.PrincipalOneToMany>().ToList();
            Assert.Single(principals);
            Assert.NotNull(principals[0].Dependents);
            Assert.True(principals[0].Dependents.All(e => e.Principal != null));

            var dependents = context.Set<Context22568.DependentOneToMany>().ToList();
            Assert.Equal(2, dependents.Count);
            Assert.True(dependents.All(e => e.Principal != null));
            Assert.True(dependents.All(e => e.Principal.Dependents != null));
            Assert.True(dependents.All(e => e.Principal.Dependents.All(i => i.Principal != null)));
        }

        using (var context = contextFactory.CreateContext())
        {
            Assert.Equal(
                CoreStrings.AutoIncludeNavigationCycle("'PrincipalManyToMany.Dependents', 'DependentManyToMany.Principals'"),
                Assert.Throws<InvalidOperationException>(() => context.Set<Context22568.PrincipalManyToMany>().ToList()).Message);

            Assert.Equal(
                CoreStrings.AutoIncludeNavigationCycle("'DependentManyToMany.Principals', 'PrincipalManyToMany.Dependents'"),
                Assert.Throws<InvalidOperationException>(() => context.Set<Context22568.DependentManyToMany>().ToList()).Message);

            context.Set<Context22568.PrincipalManyToMany>().IgnoreAutoIncludes().ToList();
            context.Set<Context22568.DependentManyToMany>().IgnoreAutoIncludes().ToList();
        }

        using (var context = contextFactory.CreateContext())
        {
            Assert.Equal(
                CoreStrings.AutoIncludeNavigationCycle("'CycleA.Bs', 'CycleB.C', 'CycleC.As'"),
                Assert.Throws<InvalidOperationException>(() => context.Set<Context22568.CycleA>().ToList()).Message);

            Assert.Equal(
                CoreStrings.AutoIncludeNavigationCycle("'CycleB.C', 'CycleC.As', 'CycleA.Bs'"),
                Assert.Throws<InvalidOperationException>(() => context.Set<Context22568.CycleB>().ToList()).Message);

            Assert.Equal(
                CoreStrings.AutoIncludeNavigationCycle("'CycleC.As', 'CycleA.Bs', 'CycleB.C'"),
                Assert.Throws<InvalidOperationException>(() => context.Set<Context22568.CycleC>().ToList()).Message);

            context.Set<Context22568.CycleA>().IgnoreAutoIncludes().ToList();
            context.Set<Context22568.CycleB>().IgnoreAutoIncludes().ToList();
            context.Set<Context22568.CycleC>().IgnoreAutoIncludes().ToList();
        }
    }

    protected class Context22568 : DbContext
    {
        public Context22568(DbContextOptions options)
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

    #region 23674

    [ConditionalFact]
    public virtual async Task Walking_back_include_tree_is_not_allowed_1()
    {
        var contextFactory = await InitializeAsync<Context23674>();

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<Context23674.Principal>()
                .Include(p => p.ManyDependents)
                .ThenInclude(m => m.Principal.SingleDependent);

            Assert.Equal(
                CoreStrings.WarningAsErrorTemplate(
                    CoreEventId.NavigationBaseIncludeIgnored.ToString(),
                    CoreResources.LogNavigationBaseIncludeIgnored(new TestLogger<TestLoggingDefinitions>())
                        .GenerateMessage("ManyDependent.Principal"),
                    "CoreEventId.NavigationBaseIncludeIgnored"),
                Assert.Throws<InvalidOperationException>(
                    () => query.ToList()).Message);
        }
    }

    [ConditionalFact]
    public virtual async Task Walking_back_include_tree_is_not_allowed_2()
    {
        var contextFactory = await InitializeAsync<Context23674>();

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<Context23674.Principal>().Include(p => p.SingleDependent.Principal.ManyDependents);

            Assert.Equal(
                CoreStrings.WarningAsErrorTemplate(
                    CoreEventId.NavigationBaseIncludeIgnored.ToString(),
                    CoreResources.LogNavigationBaseIncludeIgnored(new TestLogger<TestLoggingDefinitions>())
                        .GenerateMessage("SingleDependent.Principal"),
                    "CoreEventId.NavigationBaseIncludeIgnored"),
                Assert.Throws<InvalidOperationException>(
                    () => query.ToList()).Message);
        }
    }

    [ConditionalFact]
    public virtual async Task Walking_back_include_tree_is_not_allowed_3()
    {
        var contextFactory = await InitializeAsync<Context23674>();

        using (var context = contextFactory.CreateContext())
        {
            // This does not warn because after round-tripping from one-to-many from dependent side, the number of dependents could be larger.
            var query = context.Set<Context23674.ManyDependent>()
                .Include(p => p.Principal.ManyDependents)
                .ThenInclude(m => m.SingleDependent)
                .ToList();
        }
    }

    [ConditionalFact]
    public virtual async Task Walking_back_include_tree_is_not_allowed_4()
    {
        var contextFactory = await InitializeAsync<Context23674>();

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Set<Context23674.SingleDependent>().Include(p => p.ManyDependent.SingleDependent.Principal);

            Assert.Equal(
                CoreStrings.WarningAsErrorTemplate(
                    CoreEventId.NavigationBaseIncludeIgnored.ToString(),
                    CoreResources.LogNavigationBaseIncludeIgnored(new TestLogger<TestLoggingDefinitions>())
                        .GenerateMessage("ManyDependent.SingleDependent"),
                    "CoreEventId.NavigationBaseIncludeIgnored"),
                Assert.Throws<InvalidOperationException>(
                    () => query.ToList()).Message);
        }
    }

    private class Context23674 : DbContext
    {
        public Context23674(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Principal>();


        public class Principal
        {
            public int Id { get; set; }
            public List<ManyDependent> ManyDependents { get; set; }
            public SingleDependent SingleDependent { get; set; }
        }

        public class ManyDependent
        {
            public int Id { get; set; }
            public Principal Principal { get; set; }
            public SingleDependent SingleDependent { get; set; }
        }

        public class SingleDependent
        {
            public int Id { get; set; }
            public Principal Principal { get; set; }
            public int PrincipalId { get; set; }
            public int ManyDependentId { get; set; }
            public ManyDependent ManyDependent { get; set; }
        }
    }

    #endregion

    #region 23676

    [ConditionalFact]
    public virtual async Task Projection_with_multiple_includes_and_subquery_with_set_operation()
    {
        var contextFactory = await InitializeAsync<Context23676>();

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
    }

    private class Context23676 : DbContext
    {
        public Context23676(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<PersonEntity> Persons { get; set; }

        public class PersonEntity
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

        public class PersonImageEntity
        {
            public int Id { get; set; }
            public string ImageUrl { get; set; }
            public int Height { get; set; }
            public int Width { get; set; }
            public PersonEntity Person { get; set; }
        }

        public class ActorEntity
        {
            public int Id { get; set; }
            public int PersonId { get; set; }
            public PersonEntity Person { get; set; }

            public IList<MovieActorEntity> Movies { get; } = new List<MovieActorEntity>();
        }

        public class MovieActorEntity
        {
            public int Id { get; set; }
            public int ActorId { get; set; }
            public ActorEntity Actor { get; set; }

            public int MovieId { get; set; }
            public MovieEntity Movie { get; set; }

            public string RoleInFilm { get; set; }

            public int Order { get; set; }
        }

        public class DirectorEntity
        {
            public int Id { get; set; }
            public int PersonId { get; set; }
            public PersonEntity Person { get; set; }

            public IList<MovieDirectorEntity> Movies { get; } = new List<MovieDirectorEntity>();
        }

        public class MovieDirectorEntity
        {
            public int Id { get; set; }
            public int DirectorId { get; set; }
            public DirectorEntity Director { get; set; }

            public int MovieId { get; set; }
            public MovieEntity Movie { get; set; }
        }

        public class MovieEntity
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
    }

    #endregion
}
