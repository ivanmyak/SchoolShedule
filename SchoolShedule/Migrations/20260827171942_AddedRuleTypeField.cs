using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolShedule.Migrations
{
    /// <inheritdoc />
    public partial class AddedRuleTypeField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RuleType",
                table: "LessonRuleContainers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RuleType",
                table: "LessonRuleContainers");
        }
    }
}
