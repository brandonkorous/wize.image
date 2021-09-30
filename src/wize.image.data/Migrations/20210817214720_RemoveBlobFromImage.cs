using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace wize.image.data.Migrations
{
    public partial class RemoveBlobFromImage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Blob",
                table: "Images");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Blob",
                table: "Images",
                type: "varbinary(max)",
                nullable: true);
        }
    }
}
