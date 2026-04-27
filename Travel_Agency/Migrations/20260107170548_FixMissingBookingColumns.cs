using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel_Agency.Migrations
{
    public partial class FixMissingBookingColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // columns already exist in DB – no action needed
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // no rollback
        }
    }
}
