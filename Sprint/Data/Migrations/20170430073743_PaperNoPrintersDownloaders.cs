using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Sprint.Migrations
{
    public partial class PaperNoPrintersDownloaders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Paper_AspNetUsers_DoneById",
                table: "Paper");

            migrationBuilder.DropTable(
                name: "Downloads");

            migrationBuilder.DropIndex(
                name: "IX_Paper_DoneById",
                table: "Paper");

            migrationBuilder.DropColumn(
                name: "DoneById",
                table: "Paper");

            migrationBuilder.DropColumn(
                name: "DownloadsNum",
                table: "Paper");

            migrationBuilder.DropColumn(
                name: "Instructor",
                table: "Paper");

            migrationBuilder.RenameColumn(
                name: "Due",
                table: "Paper",
                newName: "UnlockedAt");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Paper",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "Paper",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "Paper",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Locked",
                table: "Paper",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Approved",
                table: "Paper");

            migrationBuilder.DropColumn(
                name: "Locked",
                table: "Paper");

            migrationBuilder.RenameColumn(
                name: "UnlockedAt",
                table: "Paper",
                newName: "Due");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Paper",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "Paper",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddColumn<string>(
                name: "DoneById",
                table: "Paper",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DownloadsNum",
                table: "Paper",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Instructor",
                table: "Paper",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Downloads",
                columns: table => new
                {
                    DownloadsId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    DownloadedAt = table.Column<DateTime>(nullable: false),
                    PaperId = table.Column<int>(nullable: false),
                    UserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Downloads", x => x.DownloadsId);
                    table.ForeignKey(
                        name: "FK_Downloads_Paper_PaperId",
                        column: x => x.PaperId,
                        principalTable: "Paper",
                        principalColumn: "PaperId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Downloads_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Paper_DoneById",
                table: "Paper",
                column: "DoneById");

            migrationBuilder.CreateIndex(
                name: "IX_Downloads_PaperId",
                table: "Downloads",
                column: "PaperId");

            migrationBuilder.CreateIndex(
                name: "IX_Downloads_UserId",
                table: "Downloads",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Paper_AspNetUsers_DoneById",
                table: "Paper",
                column: "DoneById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
