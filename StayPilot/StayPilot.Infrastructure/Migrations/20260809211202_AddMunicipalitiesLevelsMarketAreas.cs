using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StayPilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMunicipalitiesLevelsMarketAreas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MarketAreas",
                columns: new[] { "Id", "Country", "District", "Municipality", "Notes", "Town", "Zone" },
                values: new object[,]
                {
                    { 4366, "Portugal", "Aveiro", "Albergaria-a-Velha", null, "Albergaria-a-Velha", null },
                    { 4367, "Portugal", "Aveiro", "Anadia", null, "Anadia", null },
                    { 4368, "Portugal", "Aveiro", "Arouca", null, "Arouca", null },
                    { 4369, "Portugal", "Aveiro", "Aveiro", null, "Aveiro", null },
                    { 4370, "Portugal", "Aveiro", "Castelo de Paiva", null, "Castelo de Paiva", null },
                    { 4371, "Portugal", "Aveiro", "Estarreja", null, "Estarreja", null },
                    { 4372, "Portugal", "Aveiro", "Oliveira de Azeméis", null, "Oliveira de Azeméis", null },
                    { 4373, "Portugal", "Aveiro", "Santa Maria da Feira", null, "Santa Maria da Feira", null },
                    { 4374, "Portugal", "Aveiro", "Vale de Cambra", null, "Vale de Cambra", null },
                    { 4375, "Portugal", "Aveiro", "Ílhavo", null, "Ílhavo", null },
                    { 4376, "Portugal", "Beja", "Beja", null, "Beja", null },
                    { 4377, "Portugal", "Beja", "Castro Verde", null, "Castro Verde", null },
                    { 4378, "Portugal", "Beja", "Moura", null, "Moura", null },
                    { 4379, "Portugal", "Beja", "Odemira", null, "Odemira", null },
                    { 4380, "Portugal", "Braga", "Amares", null, "Amares", null },
                    { 4381, "Portugal", "Braga", "Braga", null, "Braga", null },
                    { 4382, "Portugal", "Braga", "Celorico de Basto", null, "Celorico de Basto", null },
                    { 4383, "Portugal", "Braga", "Guimarães", null, "Guimarães", null },
                    { 4384, "Portugal", "Braga", "Póvoa de Lanhoso", null, "Póvoa de Lanhoso", null },
                    { 4385, "Portugal", "Braga", "Terras de Bouro", null, "Terras de Bouro", null },
                    { 4386, "Portugal", "Braga", "Vila Nova de Famalicão", null, "Vila Nova de Famalicão", null },
                    { 4387, "Portugal", "Braga", "Vizela", null, "Vizela", null },
                    { 4388, "Portugal", "Bragança", "Bragança", null, "Bragança", null },
                    { 4389, "Portugal", "Bragança", "Freixo Espada à Cinta", null, "Freixo Espada à Cinta", null },
                    { 4390, "Portugal", "Castelo Branco", "Covilhã", null, "Covilhã", null },
                    { 4391, "Portugal", "Castelo Branco", "Fundão", null, "Fundão", null },
                    { 4392, "Portugal", "Castelo Branco", "Idanha-a-Nova", null, "Idanha-a-Nova", null },
                    { 4393, "Portugal", "Castelo Branco", "Proença-a-Nova", null, "Proença-a-Nova", null },
                    { 4394, "Portugal", "Coimbra", "Coimbra", null, "Coimbra", null },
                    { 4395, "Portugal", "Coimbra", "Condeixa-a-Nova", null, "Condeixa-a-Nova", null },
                    { 4396, "Portugal", "Coimbra", "Figueira da Foz", null, "Figueira da Foz", null },
                    { 4397, "Portugal", "Coimbra", "Montemor-o-Velho", null, "Montemor-o-Velho", null },
                    { 4398, "Portugal", "Coimbra", "Oliveira do Hospital", null, "Oliveira do Hospital", null },
                    { 4399, "Portugal", "Coimbra", "Penela", null, "Penela", null },
                    { 4400, "Portugal", "Coimbra", "Vila Nova de Poiares", null, "Vila Nova de Poiares", null },
                    { 4401, "Portugal", "Faro", "Albufeira", null, "Albufeira", null },
                    { 4402, "Portugal", "Faro", "Alcoutim", null, "Alcoutim", null },
                    { 4403, "Portugal", "Faro", "Lagoa (Algarve)", null, "Lagoa (Algarve)", null },
                    { 4404, "Portugal", "Faro", "Loulé", null, "Loulé", null },
                    { 4405, "Portugal", "Faro", "Vila do Bispo", null, "Vila do Bispo", null },
                    { 4406, "Portugal", "Guarda", "Celorico da Beira", null, "Celorico da Beira", null },
                    { 4407, "Portugal", "Guarda", "Manteigas", null, "Manteigas", null },
                    { 4408, "Portugal", "Guarda", "Trancoso", null, "Trancoso", null },
                    { 4409, "Portugal", "Leiria", "Alcobaça", null, "Alcobaça", null },
                    { 4410, "Portugal", "Leiria", "Bombarral", null, "Bombarral", null },
                    { 4411, "Portugal", "Leiria", "Caldas da Rainha", null, "Caldas da Rainha", null },
                    { 4412, "Portugal", "Leiria", "Figueiró dos Vinhos", null, "Figueiró dos Vinhos", null },
                    { 4413, "Portugal", "Leiria", "Leiria", null, "Leiria", null },
                    { 4414, "Portugal", "Leiria", "Óbidos", null, "Óbidos", null },
                    { 4415, "Portugal", "Lisboa", "Amadora", null, "Amadora", null },
                    { 4416, "Portugal", "Lisboa", "Cadaval", null, "Cadaval", null },
                    { 4417, "Portugal", "Lisboa", "Cascais", null, "Cascais", null },
                    { 4418, "Portugal", "Lisboa", "Lisboa", null, "Lisboa", null },
                    { 4419, "Portugal", "Lisboa", "Oeiras", null, "Oeiras", null },
                    { 4420, "Portugal", "Lisboa", "Torres Vedras", null, "Torres Vedras", null },
                    { 4421, "Portugal", "Portalegre", "Arronches", null, "Arronches", null },
                    { 4422, "Portugal", "Portalegre", "Campo Maior", null, "Campo Maior", null },
                    { 4423, "Portugal", "Portalegre", "Castelo de Vide", null, "Castelo de Vide", null },
                    { 4424, "Portugal", "Portalegre", "Crato", null, "Crato", null },
                    { 4425, "Portugal", "Portalegre", "Elvas", null, "Elvas", null },
                    { 4426, "Portugal", "Portalegre", "Marvão", null, "Marvão", null },
                    { 4427, "Portugal", "Portalegre", "Nisa", null, "Nisa", null },
                    { 4428, "Portugal", "Portalegre", "Portalegre", null, "Portalegre", null },
                    { 4429, "Portugal", "Porto", "Amarante", null, "Amarante", null },
                    { 4430, "Portugal", "Porto", "Baião", null, "Baião", null },
                    { 4431, "Portugal", "Porto", "Gondomar", null, "Gondomar", null },
                    { 4432, "Portugal", "Porto", "Lousada", null, "Lousada", null },
                    { 4433, "Portugal", "Porto", "Maia", null, "Maia", null },
                    { 4434, "Portugal", "Porto", "Marco de Canaveses", null, "Marco de Canaveses", null },
                    { 4435, "Portugal", "Porto", "Porto", null, "Porto", null },
                    { 4436, "Portugal", "Porto", "Santo Tirso", null, "Santo Tirso", null },
                    { 4437, "Portugal", "Porto", "Trofa", null, "Trofa", null },
                    { 4438, "Portugal", "Porto", "Vila Nova de Gaia", null, "Vila Nova de Gaia", null },
                    { 4439, "Portugal", "Santarém", "Abrantes", null, "Abrantes", null },
                    { 4440, "Portugal", "Santarém", "Cartaxo", null, "Cartaxo", null },
                    { 4441, "Portugal", "Santarém", "Entroncamento", null, "Entroncamento", null },
                    { 4442, "Portugal", "Santarém", "Ourém", null, "Ourém", null },
                    { 4443, "Portugal", "Santarém", "Santarém", null, "Santarém", null },
                    { 4444, "Portugal", "Santarém", "Tomar", null, "Tomar", null },
                    { 4445, "Portugal", "Santarém", "Torres Novas", null, "Torres Novas", null },
                    { 4446, "Portugal", "Setúbal", "Alcácer do Sal", null, "Alcácer do Sal", null },
                    { 4447, "Portugal", "Setúbal", "Almada", null, "Almada", null },
                    { 4448, "Portugal", "Setúbal", "Barreiro", null, "Barreiro", null },
                    { 4449, "Portugal", "Setúbal", "Grândola", null, "Grândola", null },
                    { 4450, "Portugal", "Setúbal", "Montijo", null, "Montijo", null },
                    { 4451, "Portugal", "Setúbal", "Santiago do Cacém", null, "Santiago do Cacém", null },
                    { 4452, "Portugal", "Setúbal", "Sesimbra", null, "Sesimbra", null },
                    { 4453, "Portugal", "Viana do Castelo", "Arcos de Valdevez", null, "Arcos de Valdevez", null },
                    { 4454, "Portugal", "Viana do Castelo", "Caminha", null, "Caminha", null },
                    { 4455, "Portugal", "Viana do Castelo", "Melgaço", null, "Melgaço", null },
                    { 4456, "Portugal", "Viana do Castelo", "Ponte de Lima", null, "Ponte de Lima", null },
                    { 4457, "Portugal", "Viana do Castelo", "Valença", null, "Valença", null },
                    { 4458, "Portugal", "Viana do Castelo", "Viana do Castelo", null, "Viana do Castelo", null },
                    { 4459, "Portugal", "Vila Real", "Chaves", null, "Chaves", null },
                    { 4460, "Portugal", "Vila Real", "Montalegre", null, "Montalegre", null },
                    { 4461, "Portugal", "Vila Real", "Ribeira de Pena", null, "Ribeira de Pena", null },
                    { 4462, "Portugal", "Vila Real", "Santa Marta de Penaguião", null, "Santa Marta de Penaguião", null },
                    { 4463, "Portugal", "Viseu", "Armamar", null, "Armamar", null },
                    { 4464, "Portugal", "Viseu", "Carregal do Sal", null, "Carregal do Sal", null },
                    { 4465, "Portugal", "Viseu", "Penalva do Castelo", null, "Penalva do Castelo", null },
                    { 4466, "Portugal", "Viseu", "São Pedro do Sul", null, "São Pedro do Sul", null },
                    { 4467, "Portugal", "Viseu", "Tondela", null, "Tondela", null },
                    { 4468, "Portugal", "Évora", "Alandroal", null, "Alandroal", null },
                    { 4469, "Portugal", "Évora", "Borba", null, "Borba", null },
                    { 4470, "Portugal", "Évora", "Estremoz", null, "Estremoz", null },
                    { 4471, "Portugal", "Évora", "Montemor-o-Novo", null, "Montemor-o-Novo", null },
                    { 4472, "Portugal", "Évora", "Vila Viçosa", null, "Vila Viçosa", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4366);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4367);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4368);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4369);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4370);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4371);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4372);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4373);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4374);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4375);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4376);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4377);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4378);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4379);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4380);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4381);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4382);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4383);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4384);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4385);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4386);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4387);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4388);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4389);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4390);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4391);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4392);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4393);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4394);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4395);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4396);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4397);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4398);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4399);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4400);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4401);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4402);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4403);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4404);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4405);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4406);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4407);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4408);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4409);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4410);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4411);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4412);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4413);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4414);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4415);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4416);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4417);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4418);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4419);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4420);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4421);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4422);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4423);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4424);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4425);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4426);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4427);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4428);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4429);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4430);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4431);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4432);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4433);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4434);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4435);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4436);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4437);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4438);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4439);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4440);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4441);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4442);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4443);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4444);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4445);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4446);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4447);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4448);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4449);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4450);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4451);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4452);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4453);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4454);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4455);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4456);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4457);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4458);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4459);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4460);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4461);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4462);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4463);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4464);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4465);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4466);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4467);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4468);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4469);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4470);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4471);

            migrationBuilder.DeleteData(
                table: "MarketAreas",
                keyColumn: "Id",
                keyValue: 4472);
        }
    }
}
