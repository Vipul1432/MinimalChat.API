using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinmalChat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGIFUrlInMessageTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GIFUrls",
                table: "Messages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GIFUrls",
                table: "Messages");
        }
    }
}
