using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class GroupDescriptionRemoved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Groups");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Groups",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                comment: "Description of the group");

            migrationBuilder.UpdateData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: new Guid("67938441-4f26-429a-a328-828208f6f0e6"),
                column: "Description",
                value: "Tasks related to work and projects");

            migrationBuilder.UpdateData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: new Guid("9ee30a53-b22e-40af-9171-077da6d5d34a"),
                column: "Description",
                value: "Gaming related tasks and goals");

            migrationBuilder.UpdateData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: new Guid("a1a1ab13-d1bc-4a25-8ba7-ca141db73f85"),
                column: "Description",
                value: "Fitness and health goals");

            migrationBuilder.UpdateData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: new Guid("b4c7c90c-2094-4b50-b3a0-3b123a1583f6"),
                column: "Description",
                value: "Personal daily tasks");

            migrationBuilder.UpdateData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: new Guid("eb68a07e-7fc9-40e8-bdd4-ce5cd056fb51"),
                column: "Description",
                value: "University assignments and exams");
        }
    }
}
