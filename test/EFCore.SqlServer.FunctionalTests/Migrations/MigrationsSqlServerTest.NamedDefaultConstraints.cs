// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal;

namespace Microsoft.EntityFrameworkCore.Migrations;

public partial class MigrationsSqlServerTest : MigrationsTestBase<MigrationsSqlServerTest.MigrationsSqlServerFixture>
{
    [ConditionalFact]
    public virtual async Task Named_default_constraints_add_column_with_explicit_name()
    {
        await Test(
            builder => builder.Entity("Entity").Property<string>("Id"),
            builder => { },
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7, defaultConstraintName: "MyConstraint"),
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Equal(7, column.DefaultValue);
                //Assert.Equal("MyConstraint", column[SqlServerAnnotationNames.DefaultConstraintName]);
            });

        AssertSql(
"""
ALTER TABLE [Entity] ADD [Number] int NOT NULL CONSTRAINT MyConstraint DEFAULT 7;
""");
    }

    [ConditionalFact]
    public virtual async Task Named_default_constraints_drop_column_with_explicit_name()
    {
        await Test(
            builder => builder.Entity("Entity").Property<string>("Id"),
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7, defaultConstraintName: "MyConstraint"),
            builder => { },
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns);
            });

        AssertSql(
"""
ALTER TABLE [Entity] DROP CONSTRAINT [MyConstraint];
ALTER TABLE [Entity] DROP COLUMN [Number];
""");
    }

    [ConditionalFact]
    public virtual async Task Named_default_constraints_create_table_with_column_with_explicit_name()
    {
        await Test(
            builder => { },
            builder =>
            {
                builder.Entity("Entity").Property<string>("Id");
                builder.Entity("Entity").Property<int>("Number")
                    .HasDefaultValue(7, defaultConstraintName: "MyConstraint");
            },
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Equal(7, column.DefaultValue);
                //Assert.Equal("MyConstraint", column[SqlServerAnnotationNames.DefaultConstraintName]);
            });

        AssertSql(
"""
CREATE TABLE [Entity] (
    [Id] nvarchar(450) NOT NULL,
    [Number] int NOT NULL CONSTRAINT MyConstraint DEFAULT 7,
    CONSTRAINT [PK_Entity] PRIMARY KEY ([Id])
);
""");
    }

    [ConditionalFact]
    public virtual async Task Named_default_constraints_drop_table_with_column_with_explicit_name()
    {
        await Test(
            builder =>
            {
                builder.Entity("Entity").Property<string>("Id");
                builder.Entity("Entity").Property<int>("Number")
                    .HasDefaultValue(7, defaultConstraintName: "MyConstraint");
            },
            builder => { },
            model =>
            {
                Assert.Empty(model.Tables);
            });

        AssertSql(
"""
DROP TABLE [Entity];
""");
    }

    [ConditionalFact]
    public virtual async Task Named_default_constraints_rename_constraint()
    {
        await Test(
            builder => builder.Entity("Entity").Property<string>("Id"),
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7, defaultConstraintName: "MyConstraint"),
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7, defaultConstraintName: "RenamedConstraint"),
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Equal(7, column.DefaultValue);
                //Assert.Equal("RenamedConstraint", column[SqlServerAnnotationNames.DefaultConstraintName]);
            });

        AssertSql(
"""
ALTER TABLE [Entity] DROP CONSTRAINT [MyConstraint];
ALTER TABLE [Entity] ALTER COLUMN [Number] int NOT NULL;
ALTER TABLE [Entity] ADD CONSTRAINT RenamedConstraint DEFAULT 7 FOR [Number];
""");
    }

    [ConditionalFact]
    public virtual async Task Named_default_constraints_add_explicit_constraint_name()
    {
        await Test(
            builder => builder.Entity("Entity").Property<string>("Id"),
            builder => builder.Entity("Entity").Property<int>("Number").HasDefaultValue(7),
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7, defaultConstraintName: "MyConstraint"),
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Equal(7, column.DefaultValue);
                //Assert.Equal("MyConstraint", column[SqlServerAnnotationNames.DefaultConstraintName]);
            });

        AssertSql(
"""
DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Entity]') AND [c].[name] = N'Number');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Entity] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [Entity] ALTER COLUMN [Number] int NOT NULL;
ALTER TABLE [Entity] ADD CONSTRAINT MyConstraint DEFAULT 7 FOR [Number];
""");
    }

    [ConditionalFact]
    public virtual async Task Named_default_constraints_remove_explicit_constraint_name()
    {
        await Test(
            builder => builder.Entity("Entity").Property<string>("Id"),
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7, defaultConstraintName: "MyConstraint"),
            builder => builder.Entity("Entity").Property<int>("Number").HasDefaultValue(7),
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Equal(7, column.DefaultValue);
            });

        AssertSql(
"""
ALTER TABLE [Entity] DROP CONSTRAINT [MyConstraint];
ALTER TABLE [Entity] ALTER COLUMN [Number] int NOT NULL;
ALTER TABLE [Entity] ADD DEFAULT 7 FOR [Number];
""");
    }

    [ConditionalFact]
    public virtual async Task Named_default_constraints_change_explicit_constraint_name_to_empty_string()
    {
        await Test(
            builder => builder.Entity("Entity").Property<string>("Id"),
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7, defaultConstraintName: "MyConstraint"),
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7, defaultConstraintName: ""),
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Equal(7, column.DefaultValue);
            });

        AssertSql(
"""
ALTER TABLE [Entity] DROP CONSTRAINT [MyConstraint];
ALTER TABLE [Entity] ALTER COLUMN [Number] int NOT NULL;
ALTER TABLE [Entity] ADD DEFAULT 7 FOR [Number];
""");
    }

    [ConditionalFact]
    public virtual async Task Named_default_constraints_change_explicit_constraint_name_from_empty_string()
    {
        await Test(
            builder => builder.Entity("Entity").Property<string>("Id"),
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7, defaultConstraintName: ""),
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7, defaultConstraintName: "MyConstraint"),
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Equal(7, column.DefaultValue);
                //Assert.Equal("MyConstraint", column[SqlServerAnnotationNames.DefaultConstraintName]);
            });

        AssertSql(
"""
DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Entity]') AND [c].[name] = N'Number');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Entity] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [Entity] ALTER COLUMN [Number] int NOT NULL;
ALTER TABLE [Entity] ADD CONSTRAINT MyConstraint DEFAULT 7 FOR [Number];
""");
    }



























    [ConditionalFact]
    public virtual async Task Named_default_constraints_with_opt_in_add_column_with_implicit_constraint_name()
    {
        await Test(
            builder =>
            {
                builder.UseNamedDefaultConstraints();
                builder.Entity("Entity").Property<string>("Id");
            },
            builder => { },
            builder => builder.Entity("Entity").Property<int>("Number").HasDefaultValue(7),
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Equal(7, column.DefaultValue);
                //Assert.Equal("MyConstraint", column[SqlServerAnnotationNames.DefaultConstraintName]);
            });

        AssertSql(
"""
ALTER TABLE [Entity] ADD [Number] int NOT NULL CONSTRAINT DF_Entity_Number DEFAULT 7;
""");
    }

    [ConditionalFact]
    public virtual async Task Named_default_constraints_with_opt_in_drop_column_with_implicit_constraint__name()
    {
        await Test(
            builder =>
            {
                builder.UseNamedDefaultConstraints();
                builder.Entity("Entity").Property<string>("Id");
            },
            builder => builder.Entity("Entity").Property<int>("Number").HasDefaultValue(7),
            builder => { },
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns);
            });

        AssertSql(
"""
ALTER TABLE [Entity] DROP CONSTRAINT [DF_Entity_Number];
ALTER TABLE [Entity] DROP COLUMN [Number];
""");
    }

    [ConditionalFact]
    public virtual async Task Named_default_constraints_with_opt_in_create_table_with_column_with_implicit_constraint_name()
    {
        await Test(
            builder => builder.UseNamedDefaultConstraints(),
            builder => { },
            builder =>
            {
                builder.Entity("Entity").Property<string>("Id");
                builder.Entity("Entity").Property<int>("Number").HasDefaultValue(7);
            },
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Equal(7, column.DefaultValue);
                //Assert.Equal("DF_Entity_Number", column[SqlServerAnnotationNames.DefaultConstraintName]);
            });

        AssertSql(
"""
CREATE TABLE [Entity] (
    [Id] nvarchar(450) NOT NULL,
    [Number] int NOT NULL CONSTRAINT DF_Entity_Number DEFAULT 7,
    CONSTRAINT [PK_Entity] PRIMARY KEY ([Id])
);
""");
    }

    [ConditionalFact]
    public virtual async Task Named_default_constraints_with_opt_in_drop_table_with_column_with_implicit_constraint_name()
    {
        await Test(
            builder => builder.UseNamedDefaultConstraints(),
            builder =>
            {
                builder.Entity("Entity").Property<string>("Id");
                builder.Entity("Entity").Property<int>("Number").HasDefaultValue(7);
            },
            builder => { },
            model =>
            {
                Assert.Empty(model.Tables);
            });

        AssertSql(
"""
DROP TABLE [Entity];
""");
    }

    [ConditionalFact]
    public virtual async Task Named_default_constraints_with_opt_in_rename_column_with_implicit_constraint_name()
    {
        await Test(
            builder =>
            {
                builder.UseNamedDefaultConstraints();
                builder.Entity("Entity").Property<string>("Id");
            },
            builder => builder.Entity("Entity").Property<int>("Number").HasColumnName("Number").HasDefaultValue(7),
            builder => builder.Entity("Entity").Property<int>("Number").HasColumnName("ModifiedNumber").HasDefaultValue(7),
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "ModifiedNumber");
                Assert.Equal(7, column.DefaultValue);
                //Assert.Equal("DF_Entity_ModifiedNumber", column[SqlServerAnnotationNames.DefaultConstraintName]);
            });

        AssertSql(
"""
EXEC sp_rename N'[Entity].[Number]', N'ModifiedNumber', 'COLUMN';
""",
                //
                """
ALTER TABLE [Entity] DROP CONSTRAINT [DF_Entity_Number];
ALTER TABLE [Entity] ALTER COLUMN [ModifiedNumber] int NOT NULL;
ALTER TABLE [Entity] ADD CONSTRAINT DF_Entity_ModifiedNumber DEFAULT 7 FOR [ModifiedNumber];
""");
    }

    [ConditionalFact]
    public virtual async Task Named_default_constraints_with_opt_in_rename_table_with_column_with_implicit_constraint_name()
    {
        await Test(
            builder =>
            {
                builder.UseNamedDefaultConstraints();
                builder.Entity("Entity").Property<string>("Id");
            },
            builder => builder.Entity("Entity").ToTable("Entities").Property<int>("Number").HasDefaultValue(7),
            builder => builder.Entity("Entity").ToTable("RenamedEntities").Property<int>("Number").HasDefaultValue(7),
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Equal(7, column.DefaultValue);
                //Assert.Equal("DF_RenamedEntities_Number", column[SqlServerAnnotationNames.DefaultConstraintName]);
            });

        AssertSql(
"""
ALTER TABLE [Entities] DROP CONSTRAINT [PK_Entities];
""",
                //
                """
EXEC sp_rename N'[Entities]', N'RenamedEntities', 'OBJECT';
""",
                //
                """
ALTER TABLE [RenamedEntities] DROP CONSTRAINT [DF_Entities_Number];
ALTER TABLE [RenamedEntities] ALTER COLUMN [Number] int NOT NULL;
ALTER TABLE [RenamedEntities] ADD CONSTRAINT DF_RenamedEntities_Number DEFAULT 7 FOR [Number];
""",
                //
                """
ALTER TABLE [RenamedEntities] ADD CONSTRAINT [PK_RenamedEntities] PRIMARY KEY ([Id]);
""");
    }



























    [ConditionalFact]
    public virtual async Task Named_default_constraints_add_opt_in_with_column_with_implicit_constraint_name()
    {
        await Test(
            builder =>
            {
                builder.Entity("Entity").Property<string>("Id");
                builder.Entity("Entity").Property<int>("Number").HasDefaultValue(7);
            },
            builder => { },
            builder => builder.UseNamedDefaultConstraints(),
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Equal(7, column.DefaultValue);
                //Assert.Equal("DF_Entity_Number", column[SqlServerAnnotationNames.DefaultConstraintName]);
            });

        AssertSql(
"""
DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Entity]') AND [c].[name] = N'Number');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Entity] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [Entity] ALTER COLUMN [Number] int NOT NULL;
ALTER TABLE [Entity] ADD CONSTRAINT DF_Entity_Number DEFAULT 7 FOR [Number];
""");
    }

    [ConditionalFact]
    public virtual async Task Named_default_constraints_remove_opt_in_with_column_with_implicit_constraint_name()
    {
        await Test(
            builder =>
            {
                builder.Entity("Entity").Property<string>("Id");
                builder.Entity("Entity").Property<int>("Number").HasDefaultValue(7);
            },
            builder => builder.UseNamedDefaultConstraints(),
            builder => { },
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Equal(7, column.DefaultValue);
            });

        AssertSql(
"""
ALTER TABLE [Entity] DROP CONSTRAINT [DF_Entity_Number];
ALTER TABLE [Entity] ALTER COLUMN [Number] int NOT NULL;
ALTER TABLE [Entity] ADD DEFAULT 7 FOR [Number];
""");
    }








































    [ConditionalFact]
    public virtual async Task Named_default_constraints_add_opt_in_with_column_with_explicit_constraint_name()
    {
        await Test(
            builder =>
            {
                builder.Entity("Entity").Property<string>("Id");
                builder.Entity("Entity").Property<int>("Number")
                    .HasDefaultValue(7, defaultConstraintName: "MyConstraint");
            },
            builder => { },
            builder => builder.UseNamedDefaultConstraints(),
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Equal(7, column.DefaultValue);
                //Assert.Equal("MyConstraint", column[SqlServerAnnotationNames.DefaultConstraintName]);
            });

        // opt-in doesn't make a difference when constraint name is explicitly defined
        AssertSql();
    }

    [ConditionalFact]
    public virtual async Task Named_default_constraints_remove_opt_in_with_column_with_explicit_constraint_name()
    {
        await Test(
            builder =>
            {
                builder.Entity("Entity").Property<string>("Id");
                builder.Entity("Entity").Property<int>("Number")
                    .HasDefaultValue(7, defaultConstraintName: "MyConstraint");
            },
            builder => builder.UseNamedDefaultConstraints(),
            builder => { },
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Equal(7, column.DefaultValue);
                //Assert.Equal("MyConstraint", column[SqlServerAnnotationNames.DefaultConstraintName]);
            });

        // opt-in doesn't make a difference when constraint name is explicitly defined
        AssertSql();
    }
























    [ConditionalFact]
    public virtual async Task Add_column_with_defaultValue_with_explicit_default_constraint_name()
    {
        await Test(
            builder => builder.Entity("Entity").Property<string>("Id"),
            builder => { },
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7, defaultConstraintName: "MyConstraint"),
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Equal(7, column.DefaultValue);
            });

        AssertSql(
"""
ALTER TABLE [Entity] ADD [Number] int NOT NULL CONSTRAINT MyConstraint DEFAULT 7;
""");
    }

    [ConditionalFact]
    public virtual async Task Modify_default_constraint_name()
    {
        await Test(
            builder => builder.Entity("Entity").Property<string>("Id"),
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7, defaultConstraintName: "MyConstraint"),
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7, defaultConstraintName: "MyModifiedConstraint"),

            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Equal(7, column.DefaultValue);
            });

        AssertSql(
"""
ALTER TABLE [Entity] DROP CONSTRAINT [MyConstraint];
ALTER TABLE [Entity] ALTER COLUMN [Number] int NOT NULL;
ALTER TABLE [Entity] ADD CONSTRAINT MyModifiedConstraint DEFAULT 7 FOR [Number];
""");
    }

    [ConditionalFact]
    public virtual async Task Add_explicit_default_constraint_name()
    {
        await Test(
            builder => builder.Entity("Entity").Property<string>("Id"),
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7),
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7, defaultConstraintName: "MyConstraint"),
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Equal(7, column.DefaultValue);
            });

        AssertSql(
"""
ALTER TABLE [Entity] DROP CONSTRAINT [DF_Entity_Number];
ALTER TABLE [Entity] ALTER COLUMN [Number] int NOT NULL;
ALTER TABLE [Entity] ADD CONSTRAINT MyConstraint DEFAULT 7 FOR [Number];
""");
    }

    [ConditionalFact]
    public virtual async Task Remove_explicit_default_constraint_name()
    {
        await Test(
            builder => builder.Entity("Entity").Property<string>("Id"),
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7, defaultConstraintName: "MyConstraint"),
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7),
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Equal(7, column.DefaultValue);
            });

        AssertSql(
"""
ALTER TABLE [Entity] DROP CONSTRAINT [MyConstraint];
ALTER TABLE [Entity] ALTER COLUMN [Number] int NOT NULL;
ALTER TABLE [Entity] ADD CONSTRAINT DF_Entity_Number DEFAULT 7 FOR [Number];
""");
    }

    [ConditionalFact]
    public virtual async Task Remove_default_value_with_explicit_default_constraint_name()
    {
        await Test(
            builder => builder.Entity("Entity").Property<string>("Id"),
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7, defaultConstraintName: "MyConstraint"),
            builder => builder.Entity("Entity").Property<int>("Number"),
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Null(column.DefaultValue);
            });

        AssertSql(
"""
ALTER TABLE [Entity] DROP CONSTRAINT [MyConstraint];
ALTER TABLE [Entity] ALTER COLUMN [Number] int NOT NULL;
""");
    }

    [ConditionalFact]
    public virtual async Task Remove_default_value_with_imlicit_default_constraint_name()
    {
        await Test(
            builder => builder.Entity("Entity").Property<string>("Id"),
            builder => builder.Entity("Entity").Property<int>("Number").HasDefaultValue(7),
            builder => builder.Entity("Entity").Property<int>("Number"),
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Null(column.DefaultValue);
            });

        AssertSql(
"""
ALTER TABLE [Entity] DROP CONSTRAINT [DF_Entity_Number];
ALTER TABLE [Entity] ALTER COLUMN [Number] int NOT NULL;
""");
    }

    [ConditionalFact]
    public virtual async Task Drop_column_with_default_value_with_explicit_default_constraint_name()
    {
        await Test(
            builder => builder.Entity("Entity").Property<string>("Id"),
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7, defaultConstraintName: "MyConstraint"),
            builder => { },
            model =>
            {
                var table = Assert.Single(model.Tables);
            });

        AssertSql(
"""
ALTER TABLE [Entity] DROP CONSTRAINT [MyConstraint];
ALTER TABLE [Entity] DROP COLUMN [Number];
""");
    }

    [ConditionalFact]
    public virtual async Task Rename_column_with_default_value_with_explicit_default_constraint_name()
    {
        await Test(
            builder => builder.Entity("Entity").Property<string>("Id"),
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7, defaultConstraintName: "MyConstraint"),
            builder => builder.Entity("Entity").Property<int>("RenamedNumber")
                .HasDefaultValue(7, defaultConstraintName: "MyConstraint"),
            model =>
            {
                var table = Assert.Single(model.Tables);
            });

        AssertSql(
"""
EXEC sp_rename N'[Entity].[Number]', N'RenamedNumber', 'COLUMN';
""");
    }

    [ConditionalFact]
    public virtual async Task Modify_column_with_default_value_with_explicit_default_constraint_name()
    {
        await Test(
            builder => builder.Entity("Entity").Property<string>("Id"),
            builder => builder.Entity("Entity").Property<int>("Number")
                .HasDefaultValue(7, defaultConstraintName: "MyConstraint"),
            builder => builder.Entity("Entity").Property<int?>("Number")
                .HasDefaultValue(7, defaultConstraintName: "MyConstraint").IsRequired(false),
            model =>
            {
                var table = Assert.Single(model.Tables);
            });

        AssertSql(
"""
ALTER TABLE [Entity] DROP CONSTRAINT [MyConstraint];
ALTER TABLE [Entity] ALTER COLUMN [Number] int NULL;
ALTER TABLE [Entity] ADD CONSTRAINT MyConstraint DEFAULT 7 FOR [Number];
""");
    }








    [ConditionalFact]
    public virtual async Task Opt_in_default_constraint_name()
    {
        await Test(
            builder => builder.Entity("Entity").Property<string>("Id"),
            builder => builder.Entity("Entity").Property<int>("Number").HasDefaultValue(7),
            builder =>
            {
                builder.UseNamedDefaultConstraints();
                builder.Entity("Entity").Property<int>("Number").HasDefaultValue(7);
            },
            model =>
            {
                var table = Assert.Single(model.Tables);
            });

        AssertSql(
"""
DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Entity]') AND [c].[name] = N'Number');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Entity] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [Entity] ALTER COLUMN [Number] int NULL;
ALTER TABLE [Entity] ADD CONSTRAINT DF_Entity_Number DEFAULT 7 FOR [Number];
""");
    }

    [ConditionalFact]
    public virtual async Task Opt_out_default_constraint_name()
    {
        await Test(
            builder => builder.Entity("Entity").Property<string>("Id"),
            builder =>
            {
                builder.UseNamedDefaultConstraints();
                builder.Entity("Entity").Property<int>("Number").HasDefaultValue(7);
            },
            builder => builder.Entity("Entity").Property<int>("Number").HasDefaultValue(7),
            model =>
            {
                var table = Assert.Single(model.Tables);
            });

        AssertSql(
"""
ALTER TABLE [Entity] DROP CONSTRAINT [DF_Entity_Number];
ALTER TABLE [Entity] ALTER COLUMN [Number] int NOT NULL;
ALTER TABLE [Entity] ADD DEFAULT 7 FOR [Number];
""");
    }

    [ConditionalFact]
    public virtual async Task Rename_column_with_implicitly_named_default_constraint()
    {
        await Test(
            builder => builder.Entity("Entity").Property<string>("Id"),
            builder =>
            {
                builder.UseNamedDefaultConstraints();
                builder.Entity("Entity").Property<int>("Number").HasDefaultValue(7).HasColumnName("Number");
            },
            builder =>
            {
                builder.UseNamedDefaultConstraints();
                builder.Entity("Entity").Property<int>("Number").HasDefaultValue(7).HasColumnName("ModifiedNumber");
            },
            model =>
            {
                var table = Assert.Single(model.Tables);
            });

        AssertSql(
"""
EXEC sp_rename N'[Entity].[Number]', N'ModifiedNumber', 'COLUMN';
""",
                //
                """
ALTER TABLE [Entity] DROP CONSTRAINT [DF_Entity_Number];
ALTER TABLE [Entity] ALTER COLUMN [ModifiedNumber] int NOT NULL;
ALTER TABLE [Entity] ADD CONSTRAINT DF_Entity_ModifiedNumber DEFAULT 7 FOR [ModifiedNumber];
""");
    }











    [ConditionalFact]
    public virtual async Task Create_table_with_column_having_default_value_with_default_constraint_name()
    {
        await Test(
            builder => { },
            builder => { },
            builder =>
            {
                builder.Entity("Entity").Property<string>("Id");
                builder.Entity("Entity").Property<int>("Number")
                    .HasDefaultValue(7, defaultConstraintName: "MyConstraint");
            },
            model =>
            {
                var table = Assert.Single(model.Tables);
                var column = Assert.Single(table.Columns, c => c.Name == "Number");
                Assert.Equal(7, column.DefaultValue);
            });

        AssertSql(
"""
CREATE TABLE [Entity] (
    [Id] nvarchar(450) NOT NULL,
    [Number] int NOT NULL CONSTRAINT MyConstraint DEFAULT 7,
    CONSTRAINT [PK_Entity] PRIMARY KEY ([Id])
);
""");
    }
}
