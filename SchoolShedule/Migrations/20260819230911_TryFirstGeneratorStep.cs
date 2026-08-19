using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SchoolShedule.Migrations
{
    /// <inheritdoc />
    public partial class TryFirstGeneratorStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TeacherUserId",
                table: "Rooms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleTemplateId",
                table: "ClassGroups",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoomSubject",
                columns: table => new
                {
                    AssignedSubjectsId = table.Column<int>(type: "integer", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomSubject", x => new { x.AssignedSubjectsId, x.RoomId });
                    table.ForeignKey(
                        name: "FK_RoomSubject_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoomSubject_Subjects_AssignedSubjectsId",
                        column: x => x.AssignedSubjectsId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PeriodType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LessonRuleContainers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateId = table.Column<int>(type: "integer", nullable: false),
                    SubjectId = table.Column<int>(type: "integer", nullable: false),
                    ClassGroupId = table.Column<int>(type: "integer", nullable: false),
                    RulesJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonRuleContainers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonRuleContainers_ClassGroups_ClassGroupId",
                        column: x => x.ClassGroupId,
                        principalTable: "ClassGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonRuleContainers_ScheduleTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ScheduleTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonRuleContainers_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_TeacherUserId",
                table: "Rooms",
                column: "TeacherUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassGroups_ScheduleTemplateId",
                table: "ClassGroups",
                column: "ScheduleTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonRuleContainers_ClassGroupId",
                table: "LessonRuleContainers",
                column: "ClassGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonRuleContainers_SubjectId",
                table: "LessonRuleContainers",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonRuleContainers_TemplateId",
                table: "LessonRuleContainers",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomSubject_RoomId",
                table: "RoomSubject",
                column: "RoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassGroups_ScheduleTemplates_ScheduleTemplateId",
                table: "ClassGroups",
                column: "ScheduleTemplateId",
                principalTable: "ScheduleTemplates",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Teachers_TeacherUserId",
                table: "Rooms",
                column: "TeacherUserId",
                principalTable: "Teachers",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassGroups_ScheduleTemplates_ScheduleTemplateId",
                table: "ClassGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Teachers_TeacherUserId",
                table: "Rooms");

            migrationBuilder.DropTable(
                name: "LessonRuleContainers");

            migrationBuilder.DropTable(
                name: "RoomSubject");

            migrationBuilder.DropTable(
                name: "ScheduleTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_TeacherUserId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_ClassGroups_ScheduleTemplateId",
                table: "ClassGroups");

            migrationBuilder.DropColumn(
                name: "TeacherUserId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "ScheduleTemplateId",
                table: "ClassGroups");
        }
    }
}
