using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediCare.Migrations
{
    /// <inheritdoc />
    public partial class AddNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicineLog_Medicines_MedicineId",
                table: "MedicineLog");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicineLog_Patients_PatientId",
                table: "MedicineLog");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicineSchedule_Medicines_MedicineId",
                table: "MedicineSchedule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MedicineSchedule",
                table: "MedicineSchedule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MedicineLog",
                table: "MedicineLog");

            migrationBuilder.RenameTable(
                name: "MedicineSchedule",
                newName: "MedicineSchedules");

            migrationBuilder.RenameTable(
                name: "MedicineLog",
                newName: "MedicineLogs");

            migrationBuilder.RenameIndex(
                name: "IX_MedicineSchedule_MedicineId",
                table: "MedicineSchedules",
                newName: "IX_MedicineSchedules_MedicineId");

            migrationBuilder.RenameIndex(
                name: "IX_MedicineLog_PatientId",
                table: "MedicineLogs",
                newName: "IX_MedicineLogs_PatientId");

            migrationBuilder.RenameIndex(
                name: "IX_MedicineLog_MedicineId",
                table: "MedicineLogs",
                newName: "IX_MedicineLogs_MedicineId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MedicineSchedules",
                table: "MedicineSchedules",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MedicineLogs",
                table: "MedicineLogs",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsRead = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicineLogs_Medicines_MedicineId",
                table: "MedicineLogs",
                column: "MedicineId",
                principalTable: "Medicines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicineLogs_Patients_PatientId",
                table: "MedicineLogs",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicineSchedules_Medicines_MedicineId",
                table: "MedicineSchedules",
                column: "MedicineId",
                principalTable: "Medicines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicineLogs_Medicines_MedicineId",
                table: "MedicineLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicineLogs_Patients_PatientId",
                table: "MedicineLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicineSchedules_Medicines_MedicineId",
                table: "MedicineSchedules");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MedicineSchedules",
                table: "MedicineSchedules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MedicineLogs",
                table: "MedicineLogs");

            migrationBuilder.RenameTable(
                name: "MedicineSchedules",
                newName: "MedicineSchedule");

            migrationBuilder.RenameTable(
                name: "MedicineLogs",
                newName: "MedicineLog");

            migrationBuilder.RenameIndex(
                name: "IX_MedicineSchedules_MedicineId",
                table: "MedicineSchedule",
                newName: "IX_MedicineSchedule_MedicineId");

            migrationBuilder.RenameIndex(
                name: "IX_MedicineLogs_PatientId",
                table: "MedicineLog",
                newName: "IX_MedicineLog_PatientId");

            migrationBuilder.RenameIndex(
                name: "IX_MedicineLogs_MedicineId",
                table: "MedicineLog",
                newName: "IX_MedicineLog_MedicineId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MedicineSchedule",
                table: "MedicineSchedule",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MedicineLog",
                table: "MedicineLog",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicineLog_Medicines_MedicineId",
                table: "MedicineLog",
                column: "MedicineId",
                principalTable: "Medicines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicineLog_Patients_PatientId",
                table: "MedicineLog",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicineSchedule_Medicines_MedicineId",
                table: "MedicineSchedule",
                column: "MedicineId",
                principalTable: "Medicines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
