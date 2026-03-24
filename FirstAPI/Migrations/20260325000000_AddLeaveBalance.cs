using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstAPI.Migrations
{
    public partial class AddLeaveBalance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaveBalances",
                columns: table => new
                {
                    LeaveBalanceId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId     = table.Column<int>(nullable: false),
                    Year           = table.Column<int>(nullable: false),
                    CasualTotal    = table.Column<int>(nullable: false, defaultValue: 10),
                    CasualUsed     = table.Column<int>(nullable: false, defaultValue: 0),
                    SickTotal      = table.Column<int>(nullable: false, defaultValue: 10),
                    SickUsed       = table.Column<int>(nullable: false, defaultValue: 0),
                    EarnedTotal    = table.Column<int>(nullable: false, defaultValue: 15),
                    EarnedUsed     = table.Column<int>(nullable: false, defaultValue: 0),
                    MaternityTotal = table.Column<int>(nullable: false, defaultValue: 180),
                    MaternityUsed  = table.Column<int>(nullable: false, defaultValue: 0),
                    PaternityTotal = table.Column<int>(nullable: false, defaultValue: 15),
                    PaternityUsed  = table.Column<int>(nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveBalances", x => x.LeaveBalanceId);
                    table.ForeignKey(
                        name: "FK_LeaveBalances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_EmployeeId_Year",
                table: "LeaveBalances",
                columns: new[] { "EmployeeId", "Year" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "LeaveBalances");
        }
    }
}
