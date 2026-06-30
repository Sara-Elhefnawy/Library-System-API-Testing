using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibrarySystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelWithMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Borrows_Books_BookId",
                table: "Borrows");

            migrationBuilder.DropForeignKey(
                name: "FK_Borrows_Members_MemberId",
                table: "Borrows");

            migrationBuilder.AddForeignKey(
                name: "FK_Borrows_Books_BookId",
                table: "Borrows",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Borrows_Members_MemberId",
                table: "Borrows",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Borrows_Books_BookId",
                table: "Borrows");

            migrationBuilder.DropForeignKey(
                name: "FK_Borrows_Members_MemberId",
                table: "Borrows");

            migrationBuilder.AddForeignKey(
                name: "FK_Borrows_Books_BookId",
                table: "Borrows",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Borrows_Members_MemberId",
                table: "Borrows",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
