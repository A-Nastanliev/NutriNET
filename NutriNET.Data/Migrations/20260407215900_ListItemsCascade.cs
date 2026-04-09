using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriNET.Data.Migrations
{
    /// <inheritdoc />
    public partial class ListItemsCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecipeListItems_Recipes_RecipeId",
                table: "RecipeListItems");

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeListItems_Recipes_RecipeId",
                table: "RecipeListItems",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecipeListItems_Recipes_RecipeId",
                table: "RecipeListItems");

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeListItems_Recipes_RecipeId",
                table: "RecipeListItems",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
