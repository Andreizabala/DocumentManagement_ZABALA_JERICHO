using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentManagement.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DocumentsDbContext))]
[Migration("20261110000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Documents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                OriginalFileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                StoredFileName = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                ContentType = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                FileSize = table.Column<long>(type: "bigint", nullable: false),
                UploadedAtUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Documents", item => item.Id))
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_Documents_StoredFileName",
            table: "Documents",
            column: "StoredFileName",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "Documents");
}

