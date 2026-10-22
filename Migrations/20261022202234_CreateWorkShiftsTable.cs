
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkTime.Api.Migrations
{
    public partial class CreateWorkShiftsTable : Migration
    {
        // Tạo bảng phân ca làm việc
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkShifts",
                columns: table => new
                {
                    Id = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation(
                            "SqlServer:Identity", "1, 1"),

                    EmployeeId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    WorkDate = table.Column<DateOnly>(
                        type: "date",
                        nullable: false),

                    StartTime = table.Column<TimeOnly>(
                        type: "time",
                        nullable: false),

                    EndTime = table.Column<TimeOnly>(
                        type: "time",
                        nullable: false),

                    CreatedAt = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_WorkShifts",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_WorkShifts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Một nhân viên chỉ có một ca trong một ngày
            migrationBuilder.CreateIndex(
                name: "IX_WorkShifts_EmployeeId_WorkDate",
                table: "WorkShifts",
                columns: new[]
                {
                    "EmployeeId",
                    "WorkDate"
                },
                unique: true);
        }

        // Hoàn tác Migration
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkShifts");
        }
    }
}
