using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class SupportMessageMoreFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicationUserId",
                table: "SupportMessages",
                type: "uniqueidentifier",
                nullable: false,
                comment: "Support message user fk",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "Support message description text");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "SupportMessages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                comment: "Support message created on date");

            migrationBuilder.AddColumn<DateTime>(
                name: "HandledOn",
                table: "SupportMessages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                comment: "Support message handled on date");

            migrationBuilder.AddColumn<bool>(
                name: "IsHandled",
                table: "SupportMessages",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "Support message completed on date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "SupportMessages");

            migrationBuilder.DropColumn(
                name: "HandledOn",
                table: "SupportMessages");

            migrationBuilder.DropColumn(
                name: "IsHandled",
                table: "SupportMessages");

            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicationUserId",
                table: "SupportMessages",
                type: "uniqueidentifier",
                nullable: false,
                comment: "Support message description text",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "Support message user fk");
        }
    }
}
