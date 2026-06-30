using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StayPilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedMarketAreas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ListingSnapshots_PropertyListingId",
                table: "ListingSnapshots");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "MarketAreas",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.InsertData(
                table: "MarketAreas",
                columns: new[] { "Id", "Country", "District", "Municipality", "Notes", "Town", "Zone" },
                values: new object[,]
                {
                    { 1, "Portugal", "Faro", "Albufeira", null, "Albufeira e Olhos de Água", "Centro da Cidade" },
                    { 2, "Portugal", "Faro", "Albufeira", null, "Albufeira e Olhos de Água", "Olhos de Água" },
                    { 3, "Portugal", "Faro", "Albufeira", null, "Albufeira e Olhos de Água", "Montechoro" },
                    { 4, "Portugal", "Faro", "Albufeira", null, "Albufeira e Olhos de Água", "Marina de Albufeira - Cerro da Piedade" },
                    { 5, "Portugal", "Faro", "Albufeira", null, "Albufeira e Olhos de Água", "Balaia" },
                    { 6, "Portugal", "Faro", "Albufeira", null, "Albufeira e Olhos de Água", "Praia da Falésia" },
                    { 7, "Portugal", "Faro", "Albufeira", null, "Albufeira e Olhos de Água", "Clube Albufeira" },
                    { 8, "Portugal", "Faro", "Albufeira", null, "Albufeira e Olhos de Água", "Praia da Oura - Areias de S. João" },
                    { 9, "Portugal", "Faro", "Albufeira", null, "Albufeira e Olhos de Água", "São Rafael" },
                    { 10, "Portugal", "Faro", "Albufeira", null, "Albufeira e Olhos de Água", "Cerro de Águia - Patroves" },
                    { 11, "Portugal", "Faro", "Albufeira", null, "Albufeira e Olhos de Água", "Sesmarias" },
                    { 12, "Portugal", "Faro", "Albufeira", null, "Albufeira e Olhos de Água", "Forte São João" },
                    { 13, "Portugal", "Faro", "Albufeira", null, "Ferreiras", null },
                    { 14, "Portugal", "Faro", "Albufeira", null, "Guia", "Salgados" },
                    { 15, "Portugal", "Faro", "Albufeira", null, "Guia", "Galé" },
                    { 16, "Portugal", "Faro", "Albufeira", null, "Guia", "Vale de Parra" },
                    { 17, "Portugal", "Faro", "Albufeira", null, "Paderne", null },
                    { 18, "Portugal", "Faro", "Alcoutim", null, "Alcoutim e Pereiro", null },
                    { 19, "Portugal", "Faro", "Alcoutim", null, "Giões", null },
                    { 20, "Portugal", "Faro", "Alcoutim", null, "Martim Longo", null },
                    { 21, "Portugal", "Faro", "Alcoutim", null, "Vaqueiros", null },
                    { 22, "Portugal", "Faro", "Aljezur", null, "Aljezur", null },
                    { 23, "Portugal", "Faro", "Aljezur", null, "Bordeira", null },
                    { 24, "Portugal", "Faro", "Aljezur", null, "Odeceixe", null },
                    { 25, "Portugal", "Faro", "Aljezur", null, "Rogil", null },
                    { 26, "Portugal", "Faro", "Castro Marim", null, "Altura", null },
                    { 27, "Portugal", "Faro", "Castro Marim", null, "Azinhal", null },
                    { 28, "Portugal", "Faro", "Castro Marim", null, "Castro Marim", "Centro" },
                    { 29, "Portugal", "Faro", "Castro Marim", null, "Castro Marim", "Praia Verde" },
                    { 30, "Portugal", "Faro", "Castro Marim", null, "Castro Marim", "Quinta do Sobral - São Bartolomeu" },
                    { 31, "Portugal", "Faro", "Castro Marim", null, "Castro Marim", "Monte Francisco" },
                    { 32, "Portugal", "Faro", "Castro Marim", null, "Castro Marim", "Junqueira - Beliche" },
                    { 33, "Portugal", "Faro", "Castro Marim", null, "Castro Marim", "Golf Resort" },
                    { 34, "Portugal", "Faro", "Castro Marim", null, "Odeleite", null },
                    { 35, "Portugal", "Faro", "Faro", null, "Conceição", null },
                    { 36, "Portugal", "Faro", "Faro", null, "Estoi", null },
                    { 37, "Portugal", "Faro", "Faro", null, "Faro", "Centro" },
                    { 38, "Portugal", "Faro", "Faro", null, "Faro", "Arneiro - Braciais - Patacão" },
                    { 39, "Portugal", "Faro", "Faro", null, "Faro", "Areal Gordo - Rio Seco - Ilha da Culatra" },
                    { 40, "Portugal", "Faro", "Faro", null, "Faro", "Horta das Figuras - Lejana - Senhora da Saúde" },
                    { 41, "Portugal", "Faro", "Faro", null, "Faro", "Penha - Vale da Amoreira" },
                    { 42, "Portugal", "Faro", "Faro", null, "Faro", "São Luís" },
                    { 43, "Portugal", "Faro", "Faro", null, "Faro", "Alto de Santo António - Bom João - João de Deus" },
                    { 44, "Portugal", "Faro", "Faro", null, "Faro", "Alto Rodes" },
                    { 45, "Portugal", "Faro", "Faro", null, "Montenegro", null },
                    { 46, "Portugal", "Faro", "Faro", null, "Montenegro", "Quinta do Eucalipto - Ilha de Faro" },
                    { 47, "Portugal", "Faro", "Faro", null, "Santa Bárbara de Nexe", null },
                    { 48, "Portugal", "Faro", "Lagoa", null, "Estombar e Parchal", null },
                    { 49, "Portugal", "Faro", "Lagoa", null, "Ferragudo", null },
                    { 50, "Portugal", "Faro", "Lagoa", null, "Lagoa e Carvoeiro", "Cidade de Lagoa" },
                    { 51, "Portugal", "Faro", "Lagoa", null, "Lagoa e Carvoeiro", "Centro de Carvoeiro" },
                    { 52, "Portugal", "Faro", "Lagoa", null, "Lagoa e Carvoeiro", "Vale Milho - Vale Centeanes - Algar Seco" },
                    { 53, "Portugal", "Faro", "Lagoa", null, "Lagoa e Carvoeiro", "Salicos - Sesmarias - Boavista" },
                    { 54, "Portugal", "Faro", "Lagoa", null, "Lagoa e Carvoeiro", "Caramujeira - Vale d'El Rei - Benagil" },
                    { 55, "Portugal", "Faro", "Lagoa", null, "Lagoa e Carvoeiro", "Mato Serrão - Vale da Lapa - Vale Currais" },
                    { 56, "Portugal", "Faro", "Lagoa", null, "Porches", null },
                    { 57, "Portugal", "Faro", "Lagos", null, "Barão de São João", null },
                    { 58, "Portugal", "Faro", "Lagos", null, "Bensafrim", null },
                    { 59, "Portugal", "Faro", "Lagos", null, "Lagos", null },
                    { 60, "Portugal", "Faro", "Lagos", null, "Lagos", "Lagos Cidade" },
                    { 61, "Portugal", "Faro", "Lagos", null, "Lagos", "Meia Praia" },
                    { 62, "Portugal", "Faro", "Lagos", null, "Lagos", "Quinta da Boavista" },
                    { 63, "Portugal", "Faro", "Lagos", null, "Lagos", "Falfeira - Monte Funchal" },
                    { 64, "Portugal", "Faro", "Lagos", null, "Lagos", "Chinicato - Sargaçal" },
                    { 65, "Portugal", "Faro", "Lagos", null, "Luz", null },
                    { 66, "Portugal", "Faro", "Lagos", null, "Odiaxere", null },
                    { 67, "Portugal", "Faro", "Loulé", null, "Almancil", "Centro" },
                    { 68, "Portugal", "Faro", "Loulé", null, "Almancil", "Vale do Lobo" },
                    { 69, "Portugal", "Faro", "Loulé", null, "Almancil", "Quinta do Lago - Pinheiros Altos" },
                    { 70, "Portugal", "Faro", "Loulé", null, "Almancil", "Vale do Garrão - Varandas do Lago - Quinta das Salinas" },
                    { 71, "Portugal", "Faro", "Loulé", null, "Almancil", "The Village - Fonte Algarve - Quinta Verde" },
                    { 72, "Portugal", "Faro", "Loulé", null, "Almancil", "Vale Formoso - Vale d'Éguas" },
                    { 73, "Portugal", "Faro", "Loulé", null, "Almancil", "São Lourenço - São João da Venda" },
                    { 74, "Portugal", "Faro", "Loulé", null, "Alte", null },
                    { 75, "Portugal", "Faro", "Loulé", null, "Ameixial", null },
                    { 76, "Portugal", "Faro", "Loulé", null, "Benafim", null },
                    { 77, "Portugal", "Faro", "Loulé", null, "Boliqueime", null },
                    { 78, "Portugal", "Faro", "Loulé", null, "Quarteira", "Praia de Quarteira" },
                    { 79, "Portugal", "Faro", "Loulé", null, "Quarteira", "Centro - Quarteira Velha" },
                    { 80, "Portugal", "Faro", "Loulé", null, "Quarteira", "Fonte Santa" },
                    { 81, "Portugal", "Faro", "Loulé", null, "Quarteira", "Aldeia do Golf - Alto do Golf" },
                    { 82, "Portugal", "Faro", "Loulé", null, "Quarteira", "Vilamoura" },
                    { 83, "Portugal", "Faro", "Loulé", null, "Quarteira", "Marina de Vilamoura" },
                    { 84, "Portugal", "Faro", "Loulé", null, "Quarteira", "Pinhal Velho - Terraços do Pinhal - Encosta das Oliveiras" },
                    { 85, "Portugal", "Faro", "Loulé", null, "Quarteira", "Vila Sol - Morgadinho" },
                    { 86, "Portugal", "Faro", "Loulé", null, "Querença", null },
                    { 87, "Portugal", "Faro", "Loulé", null, "Salir", null },
                    { 88, "Portugal", "Faro", "Loulé", null, "São Clemente", "Centro Histórico Este de Loulé" },
                    { 89, "Portugal", "Faro", "Loulé", null, "São Clemente", "Centro Este da Cidade de Loulé" },
                    { 90, "Portugal", "Faro", "Loulé", null, "São Sebastião", "Centro Oeste da Cidade de Loulé" },
                    { 91, "Portugal", "Faro", "Loulé", null, "São Sebastião", "Cerro de Cabeça de Câmara - Estação de Loulé" },
                    { 92, "Portugal", "Faro", "Loulé", null, "Tor", null },
                    { 93, "Portugal", "Faro", "Monchique", null, "Alferce", null },
                    { 94, "Portugal", "Faro", "Monchique", null, "Marmelete", null },
                    { 95, "Portugal", "Faro", "Monchique", null, "Monchique", null },
                    { 96, "Portugal", "Faro", "Olhão", null, "Fuseta", null },
                    { 97, "Portugal", "Faro", "Olhão", null, "Moncarapacho", null },
                    { 98, "Portugal", "Faro", "Olhão", null, "Olhão", "Baixa" },
                    { 99, "Portugal", "Faro", "Olhão", null, "Olhão", "Marina" },
                    { 100, "Portugal", "Faro", "Olhão", null, "Olhão", "Cavalinha - Bombeiros" },
                    { 101, "Portugal", "Faro", "Olhão", null, "Olhão", "Estádio" },
                    { 102, "Portugal", "Faro", "Olhão", null, "Pechão", null },
                    { 103, "Portugal", "Faro", "Olhão", null, "Quelfes", null },
                    { 104, "Portugal", "Faro", "Portimão", null, "Alvor", null },
                    { 105, "Portugal", "Faro", "Portimão", null, "Mexilhoeira Grande", null },
                    { 106, "Portugal", "Faro", "Portimão", null, "Portimão", null },
                    { 107, "Portugal", "Faro", "Portimão", null, "Portimão", "Portimão Cidade" },
                    { 108, "Portugal", "Faro", "Portimão", null, "Portimão", "Praia da Rocha" },
                    { 109, "Portugal", "Faro", "Portimão", null, "Portimão", "Aldeia do Carrasco - Vale da Arrancada" },
                    { 110, "Portugal", "Faro", "Portimão", null, "Portimão", "Amparo - Alto do Quintão" },
                    { 111, "Portugal", "Faro", "Portimão", null, "Portimão", "Bemposta - Quatro Estradas" },
                    { 112, "Portugal", "Faro", "Portimão", null, "Portimão", "Quinta da Malata" },
                    { 113, "Portugal", "Faro", "Portimão", null, "Portimão", "Vale de Lagar - Quinta das Oliveiras - Pedra Mourinha" },
                    { 114, "Portugal", "Faro", "São Brás de Alportel", null, "São Brás de Alportel", "Centro" },
                    { 115, "Portugal", "Faro", "São Brás de Alportel", null, "São Brás de Alportel", "Campina - Mesquita" },
                    { 116, "Portugal", "Faro", "São Brás de Alportel", null, "São Brás de Alportel", "São Romão - Fonte do Touro" },
                    { 117, "Portugal", "Faro", "São Brás de Alportel", null, "São Brás de Alportel", "Funchais - Corotelo" },
                    { 118, "Portugal", "Faro", "São Brás de Alportel", null, "São Brás de Alportel", "Barrabés - Peral" },
                    { 119, "Portugal", "Faro", "Silves", null, "Alcantarilha", null },
                    { 120, "Portugal", "Faro", "Silves", null, "Algoz", null },
                    { 121, "Portugal", "Faro", "Silves", null, "Armação de Pêra", null },
                    { 122, "Portugal", "Faro", "Silves", null, "Pêra", null },
                    { 123, "Portugal", "Faro", "Silves", null, "Silves", "Centro da Cidade" },
                    { 124, "Portugal", "Faro", "Silves", null, "Silves", "Zona Histórica" },
                    { 125, "Portugal", "Faro", "Silves", null, "Silves", "Vila Fria" },
                    { 126, "Portugal", "Faro", "Silves", null, "Silves", "Vale da Vila - Poço Barreto" },
                    { 127, "Portugal", "Faro", "Silves", null, "Silves", "Enxerim - Barrada" },
                    { 128, "Portugal", "Faro", "Silves", null, "Silves", "Estação de Silves - Cerro de São Miguel" },
                    { 129, "Portugal", "Faro", "Silves", null, "Silves", "Serra - Barragem do Arade" },
                    { 130, "Portugal", "Faro", "Silves", null, "São Bartolomeu de Messines", null },
                    { 131, "Portugal", "Faro", "Silves", null, "São Marcos da Serra", null },
                    { 132, "Portugal", "Faro", "Silves", null, "Tunes", null },
                    { 133, "Portugal", "Faro", "Tavira", null, "Cabanas de Tavira", null },
                    { 134, "Portugal", "Faro", "Tavira", null, "Cachopo", null },
                    { 135, "Portugal", "Faro", "Tavira", null, "Conceição", null },
                    { 136, "Portugal", "Faro", "Tavira", null, "Luz de Tavira", null },
                    { 137, "Portugal", "Faro", "Tavira", null, "Santa Catarina - Fonte do Bispo", null },
                    { 138, "Portugal", "Faro", "Tavira", null, "Santa Luzia", null },
                    { 139, "Portugal", "Faro", "Tavira", null, "Santo Estêvão", null },
                    { 140, "Portugal", "Faro", "Tavira", null, "Tavira", null },
                    { 141, "Portugal", "Faro", "Tavira", null, "Tavira", "Centro Histórico" },
                    { 142, "Portugal", "Faro", "Tavira", null, "Tavira", "Porta Nova - Colinas da Boavista" },
                    { 143, "Portugal", "Faro", "Tavira", null, "Tavira", "Pegada - Mato Santo Espírito - Vale Carangueijo" },
                    { 144, "Portugal", "Faro", "Tavira", null, "Tavira", "Quinta da Foz - Escolas" },
                    { 145, "Portugal", "Faro", "Tavira", null, "Tavira", "Colina de Asseca - Quinta de Perogil - São Pedro" },
                    { 146, "Portugal", "Faro", "Tavira", null, "Tavira", "Serra" },
                    { 147, "Portugal", "Faro", "Vila do Bispo", null, "Barão de São Miguel", null },
                    { 148, "Portugal", "Faro", "Vila do Bispo", null, "Budens", null },
                    { 149, "Portugal", "Faro", "Vila do Bispo", null, "Sagres", null },
                    { 150, "Portugal", "Faro", "Vila do Bispo", null, "Vila do Bispo e Raposeira", null },
                    { 151, "Portugal", "Faro", "Vila Real de Santo António", null, "Monte Gordo", null },
                    { 152, "Portugal", "Faro", "Vila Real de Santo António", null, "Vila Nova de Cacela", null },
                    { 153, "Portugal", "Faro", "Vila Real de Santo António", null, "Vila Real de Santo António", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListingSnapshots_PropertyListingId_SnapshotDateUtc",
                table: "ListingSnapshots",
                columns: new[] { "PropertyListingId", "SnapshotDateUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ListingSnapshots_PropertyListingId_SnapshotDateUtc",
                table: "ListingSnapshots");

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 66);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 67);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 68);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 69);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 70);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 71);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 72);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 73);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 74);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 75);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 76);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 77);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 78);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 79);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 80);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 81);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 82);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 83);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 84);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 85);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 86);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 87);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 88);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 89);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 90);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 91);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 92);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 93);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 94);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 95);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 96);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 97);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 98);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 99);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 106);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 107);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 108);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 109);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 110);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 111);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 112);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 113);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 114);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 115);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 116);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 117);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 118);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 119);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 120);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 121);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 122);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 123);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 124);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 125);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 126);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 127);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 128);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 129);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 130);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 131);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 132);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 133);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 134);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 135);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 136);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 137);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 138);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 139);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 140);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 141);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 142);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 143);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 144);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 145);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 146);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 147);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 148);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 149);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 150);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 151);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 152);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 153);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "MarketAreas",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.CreateIndex(
                name: "IX_ListingSnapshots_PropertyListingId",
                table: "ListingSnapshots",
                column: "PropertyListingId");
        }
    }
}
