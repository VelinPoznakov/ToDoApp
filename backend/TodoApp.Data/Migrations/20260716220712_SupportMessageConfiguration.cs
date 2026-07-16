using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class SupportMessageConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupportMessages_AspNetUsers_ApplicationUserId",
                table: "SupportMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_SupportMessages_Todos_TodoEntityId",
                table: "SupportMessages");

            migrationBuilder.DropIndex(
                name: "IX_SupportMessages_TodoEntityId",
                table: "SupportMessages");

            migrationBuilder.DropColumn(
                name: "TodoEntityId",
                table: "SupportMessages");

            migrationBuilder.AddForeignKey(
                name: "FK_SupportMessages_AspNetUsers_ApplicationUserId",
                table: "SupportMessages",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupportMessages_AspNetUsers_ApplicationUserId",
                table: "SupportMessages");

            migrationBuilder.AddColumn<Guid>(
                name: "TodoEntityId",
                table: "SupportMessages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessages_TodoEntityId",
                table: "SupportMessages",
                column: "TodoEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupportMessages_AspNetUsers_ApplicationUserId",
                table: "SupportMessages",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupportMessages_Todos_TodoEntityId",
                table: "SupportMessages",
                column: "TodoEntityId",
                principalTable: "Todos",
                principalColumn: "Id");
        }
    }
}
