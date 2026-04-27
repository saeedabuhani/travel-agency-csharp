using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel_Agency.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingTimeFramesAndWebsiteReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only create the new tables - Notifications and WaitingList columns already exist from AddNotificationSystem migration
            migrationBuilder.CreateTable(
                name: "BookingTimeFrames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TravelPackageId = table.Column<int>(type: "int", nullable: false),
                    LatestBookingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationAllowedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReminderDaysBeforeDeparture = table.Column<int>(type: "int", nullable: true),
                    CancellationPeriodDays = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingTimeFrames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingTimeFrames_TravelPackages_TravelPackageId",
                        column: x => x.TravelPackageId,
                        principalTable: "TravelPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebsiteReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebsiteReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebsiteReviews_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingTimeFrames_TravelPackageId",
                table: "BookingTimeFrames",
                column: "TravelPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_WebsiteReviews_UserId",
                table: "WebsiteReviews",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingTimeFrames");

            migrationBuilder.DropTable(
                name: "WebsiteReviews");
        }
    }
}
