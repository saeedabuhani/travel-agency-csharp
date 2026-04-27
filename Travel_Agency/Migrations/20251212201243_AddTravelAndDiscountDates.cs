using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel_Agency.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelAndDiscountDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DiscountStart",
                table: "TravelPackages",
                newName: "DiscountStartDate");

            migrationBuilder.RenameColumn(
                name: "DiscountEnd",
                table: "TravelPackages",
                newName: "DiscountEndDate");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "TravelPackages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "TravelPackages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "TravelPackages");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "TravelPackages");

            migrationBuilder.RenameColumn(
                name: "DiscountStartDate",
                table: "TravelPackages",
                newName: "DiscountStart");

            migrationBuilder.RenameColumn(
                name: "DiscountEndDate",
                table: "TravelPackages",
                newName: "DiscountEnd");
        }
    }
}
