using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicPulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SlaWarningEmailSent",
                table: "Complaints",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SlaWarningEmailSent",
                table: "Complaints");
        }
    }
}
