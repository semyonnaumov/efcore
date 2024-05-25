// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
//using MyPrecompiledApp.Generated;
namespace MyPrecompiledApp;

internal class Program
{
    static void Main(string[] args)
    {
        using var ctx = new MyContext();
        //ctx.Database.EnsureDeleted();
        //ctx.Database.EnsureCreated();
        //var ctx_Entities = ctx.Set<MyEntity>().AsNoTracking();
        var query = ctx.Set<MyEntity>().AsNoTracking().Where(x => x.Id > 5).ToList();



        foreach (var result in query)
        {
            Console.WriteLine("Id: " + result.Id + " Name: " + result.Name);
        }

    }
}




public class MyContext : DbContext
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    //public DbSet<MyEntity> Entities { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MyEntity>().ToTable("Entities");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=Repro;Trusted_Connection=True;MultipleActiveResultSets=true");//.UseModel(MyContextModel.Instance);

    }
}

public class MyEntity
{
    public int Id { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public string Name { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
}
