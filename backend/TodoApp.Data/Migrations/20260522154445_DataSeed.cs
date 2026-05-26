using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TodoApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class DataSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "GroupId",
                table: "Todos",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "Groups",
                columns: new[] { "Id", "CreatedOn", "Description", "Name", "UpdatedOn", "UserId" },
                values: new object[,]
                {
                    { new Guid("67938441-4f26-429a-a328-828208f6f0e6"), new DateTime(2026, 5, 22, 8, 30, 0, 0, DateTimeKind.Unspecified), "Tasks related to work and projects", "Work Tasks", null, new Guid("e095d119-a99e-4fec-d301-08deb59f834f") },
                    { new Guid("9ee30a53-b22e-40af-9171-077da6d5d34a"), new DateTime(2026, 5, 20, 16, 45, 0, 0, DateTimeKind.Unspecified), "Gaming related tasks and goals", "Gaming", null, new Guid("e095d119-a99e-4fec-d301-08deb59f834f") },
                    { new Guid("a1a1ab13-d1bc-4a25-8ba7-ca141db73f85"), new DateTime(2026, 5, 18, 7, 20, 0, 0, DateTimeKind.Unspecified), "Fitness and health goals", "Fitness", null, new Guid("e095d119-a99e-4fec-d301-08deb59f834f") },
                    { new Guid("b4c7c90c-2094-4b50-b3a0-3b123a1583f6"), new DateTime(2026, 5, 19, 9, 0, 0, 0, DateTimeKind.Unspecified), "Personal daily tasks", "Personal", null, new Guid("e095d119-a99e-4fec-d301-08deb59f834f") },
                    { new Guid("eb68a07e-7fc9-40e8-bdd4-ce5cd056fb51"), new DateTime(2026, 5, 21, 11, 15, 0, 0, DateTimeKind.Unspecified), "University assignments and exams", "University", null, new Guid("e095d119-a99e-4fec-d301-08deb59f834f") }
                });

            migrationBuilder.InsertData(
                table: "Todos",
                columns: new[] { "Id", "CreatedOn", "Description", "DueDate", "GroupId", "Name", "Priority", "Status", "UpdatedOn", "UserId" },
                values: new object[,]
                {
                    { new Guid("142279f9-cc2c-4766-8a0e-c801174b4fdf"), new DateTime(2026, 5, 18, 7, 20, 0, 0, DateTimeKind.Unspecified), "Complete cardio and strength training", new DateOnly(2026, 5, 24), new Guid("b4c7c90c-2094-4b50-b3a0-3b123a1583f6"), "Morning workout", 0, 0, null, new Guid("e095d119-a99e-4fec-d301-08deb59f834f") },
                    { new Guid("5259ecb1-ea01-4b61-82ac-5a50d5c3d6b1"), new DateTime(2026, 5, 22, 10, 30, 0, 0, DateTimeKind.Unspecified), "Complete the ASP.NET Core API", new DateOnly(2026, 5, 30), new Guid("a1a1ab13-d1bc-4a25-8ba7-ca141db73f85"), "Finish API", 0, 0, null, new Guid("e095d119-a99e-4fec-d301-08deb59f834f") },
                    { new Guid("9fc3138f-6c8d-47a8-b7b9-192e5df38c7e"), new DateTime(2026, 5, 21, 14, 15, 0, 0, DateTimeKind.Unspecified), "Prepare for Signals and Systems exam", new DateOnly(2026, 6, 5), new Guid("a1a1ab13-d1bc-4a25-8ba7-ca141db73f85"), "Study Signals", 1, 0, null, new Guid("e095d119-a99e-4fec-d301-08deb59f834f") },
                    { new Guid("af8c31f0-a176-427d-916e-8eb9bc1cb1e4"), new DateTime(2026, 5, 20, 18, 45, 0, 0, DateTimeKind.Unspecified), "Play and stream Battlefield 1", new DateOnly(2026, 5, 25), new Guid("a1a1ab13-d1bc-4a25-8ba7-ca141db73f85"), "Play Battlefield 1", 2, 1, null, new Guid("e095d119-a99e-4fec-d301-08deb59f834f") },
                    { new Guid("d35d20fb-d640-4095-90a4-a11a3cd7d608"), new DateTime(2026, 5, 19, 9, 0, 0, 0, DateTimeKind.Unspecified), "Buy food and drinks", new DateOnly(2026, 5, 23), new Guid("a1a1ab13-d1bc-4a25-8ba7-ca141db73f85"), "Buy groceries", 1, 0, null, new Guid("e095d119-a99e-4fec-d301-08deb59f834f") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: new Guid("67938441-4f26-429a-a328-828208f6f0e6"));

            migrationBuilder.DeleteData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: new Guid("9ee30a53-b22e-40af-9171-077da6d5d34a"));

            migrationBuilder.DeleteData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: new Guid("eb68a07e-7fc9-40e8-bdd4-ce5cd056fb51"));

            migrationBuilder.DeleteData(
                table: "Todos",
                keyColumn: "Id",
                keyValue: new Guid("142279f9-cc2c-4766-8a0e-c801174b4fdf"));

            migrationBuilder.DeleteData(
                table: "Todos",
                keyColumn: "Id",
                keyValue: new Guid("5259ecb1-ea01-4b61-82ac-5a50d5c3d6b1"));

            migrationBuilder.DeleteData(
                table: "Todos",
                keyColumn: "Id",
                keyValue: new Guid("9fc3138f-6c8d-47a8-b7b9-192e5df38c7e"));

            migrationBuilder.DeleteData(
                table: "Todos",
                keyColumn: "Id",
                keyValue: new Guid("af8c31f0-a176-427d-916e-8eb9bc1cb1e4"));

            migrationBuilder.DeleteData(
                table: "Todos",
                keyColumn: "Id",
                keyValue: new Guid("d35d20fb-d640-4095-90a4-a11a3cd7d608"));

            migrationBuilder.DeleteData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: new Guid("a1a1ab13-d1bc-4a25-8ba7-ca141db73f85"));

            migrationBuilder.DeleteData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: new Guid("b4c7c90c-2094-4b50-b3a0-3b123a1583f6"));

            migrationBuilder.AlterColumn<Guid>(
                name: "GroupId",
                table: "Todos",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }
    }
}
