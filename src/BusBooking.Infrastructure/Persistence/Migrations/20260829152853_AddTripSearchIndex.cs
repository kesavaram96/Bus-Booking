using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTripSearchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Trips_RouteId_TripDate_Status",
                table: "Trips",
                columns: new[] { "RouteId", "TripDate", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trips_RouteId_TripDate_Status",
                table: "Trips");
        }
    }
}
