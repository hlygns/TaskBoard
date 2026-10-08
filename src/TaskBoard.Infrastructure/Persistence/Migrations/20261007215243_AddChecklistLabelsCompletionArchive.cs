using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskBoard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChecklistLabelsCompletionArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "archived_at",
                table: "cards",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "completed_at",
                table: "cards",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "checklist_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    is_done = table.Column<bool>(type: "boolean", nullable: false),
                    position = table.Column<double>(type: "double precision", nullable: false),
                    card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_checklist_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_checklist_items_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "labels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_labels", x => x.id);
                    table.ForeignKey(
                        name: "fk_labels_boards_board_id",
                        column: x => x.board_id,
                        principalTable: "boards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "card_labels",
                columns: table => new
                {
                    card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_card_labels", x => new { x.card_id, x.label_id });
                    table.ForeignKey(
                        name: "fk_card_labels_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_card_labels_labels_label_id",
                        column: x => x.label_id,
                        principalTable: "labels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_card_labels_label_id",
                table: "card_labels",
                column: "label_id");

            migrationBuilder.CreateIndex(
                name: "ix_checklist_items_card_id_position",
                table: "checklist_items",
                columns: new[] { "card_id", "position" });

            migrationBuilder.CreateIndex(
                name: "ix_labels_board_id_name",
                table: "labels",
                columns: new[] { "board_id", "name" },
                unique: true);

            // Veri migration'ı (elle eklendi): Eskiden "tamamlandı" bilgisi sütun adından tahmin ediliyordu.
            // Yeni kuralla uyumlu olsun diye bu adlardaki sütunlarda duran kartları tamamlandı işaretliyoruz.
            migrationBuilder.Sql("""
                UPDATE cards
                SET completed_at = now()
                WHERE column_id IN (
                    SELECT id FROM board_columns
                    WHERE lower(name) IN ('bitti', 'done', 'tamamlandı')
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "card_labels");

            migrationBuilder.DropTable(
                name: "checklist_items");

            migrationBuilder.DropTable(
                name: "labels");

            migrationBuilder.DropColumn(
                name: "archived_at",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "cards");
        }
    }
}
