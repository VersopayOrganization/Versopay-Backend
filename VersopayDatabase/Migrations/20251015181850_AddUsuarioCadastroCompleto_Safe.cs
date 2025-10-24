using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VersopayDatabase.Migrations
{
    public partial class AddUsuarioCadastroCompleto_Safe : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cria a coluna somente se ela não existir; adiciona DEFAULT para viabilizar NOT NULL
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[Usuarios]', N'CadastroCompleto') IS NULL
BEGIN
    ALTER TABLE [dbo].[Usuarios]
      ADD [CadastroCompleto] BIT NOT NULL
          CONSTRAINT [DF_Usuarios_CadastroCompleto] DEFAULT(0);
END;

-- Remove o DEFAULT para ficar alinhado ao modelo (opcional)
DECLARE @df sysname, @sql nvarchar(max);
SELECT @df = dc.name
  FROM sys.default_constraints dc
  JOIN sys.columns c ON c.default_object_id = dc.object_id
 WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Usuarios]')
   AND c.name = N'CadastroCompleto';
IF @df IS NOT NULL
BEGIN
    SET @sql = N'ALTER TABLE [dbo].[Usuarios] DROP CONSTRAINT ' + QUOTENAME(@df) + N';';
    EXEC(@sql);
END;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove a coluna apenas se existir
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[Usuarios]', N'CadastroCompleto') IS NOT NULL
BEGIN
    -- Garante dropar default constraint se houver
    DECLARE @df sysname, @sql nvarchar(max);
    SELECT @df = dc.name
      FROM sys.default_constraints dc
      JOIN sys.columns c ON c.default_object_id = dc.object_id
     WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Usuarios]')
       AND c.name = N'CadastroCompleto';
    IF @df IS NOT NULL
    BEGIN
        SET @sql = N'ALTER TABLE [dbo].[Usuarios] DROP CONSTRAINT ' + QUOTENAME(@df) + N';';
        EXEC(@sql);
    END;

    ALTER TABLE [dbo].[Usuarios] DROP COLUMN [CadastroCompleto];
END;
");
        }
    }
}
