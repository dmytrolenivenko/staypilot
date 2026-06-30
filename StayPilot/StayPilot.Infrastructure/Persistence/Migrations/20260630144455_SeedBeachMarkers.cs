using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StayPilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedBeachMarkers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "BeachMarkers",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.InsertData(
                table: "BeachMarkers",
                columns: new[] { "Id", "Latitude", "Longitude", "Name", "OsmId", "Region" },
                values: new object[,]
                {
                    { 1, 37.119263m, -8.637639m, "Meia Praia", 432987903L, null },
                    { 2, 37.120719m, -8.626936m, "Meia Praia", 1069286046L, null },
                    { 3, 37.109609m, -8.655367m, "Meia Praia", 1245391449L, null },
                    { 4, 37.176695m, -7.447375m, "Monte Gordo", 590527191L, null },
                    { 5, 37.088848m, -8.668601m, "Pinheiro Beach", 93278498L, null },
                    { 6, 37.176213m, -7.466071m, "Praia Adão e Eva", 8211403118L, null },
                    { 7, 37.102812m, -8.507711m, "Praia Afurada Naturista", 1252978463L, null },
                    { 8, 37.056394m, -8.850439m, "Praia Andorinha", 1244215650L, null },
                    { 9, 37.126854m, -8.925067m, "Praia Cordama Naturista", 1239998563L, null },
                    { 10, 37.161706m, -8.486540m, "Praia Fluvial", 474645895L, null },
                    { 11, 37.116416m, -8.520260m, "Praia Grande (Ferragudo)", 101015524L, null },
                    { 12, 37.093923m, -8.340672m, "Praia Grande de Pêra", 252621789L, null },
                    { 13, 37.061813m, -8.834376m, "Praia Grodo Mexilhão", 1328895446L, null },
                    { 14, 37.043951m, -8.885063m, "Praia João Vaz", 810715094L, null },
                    { 15, 37.116217m, -8.567881m, "Praia João de Arens", 1245934887L, null },
                    { 16, 37.115636m, -8.567341m, "Praia João de Arens", 1246032644L, null },
                    { 17, 37.076409m, -8.309226m, "Praia Manuel Lourenço", 1250926293L, null },
                    { 18, 37.088785m, -8.201259m, "Praia Maria Luísa", 129581900L, null },
                    { 19, 37.095920m, -8.388262m, "Praia Nova", 119246518L, null },
                    { 20, 37.116219m, -8.574277m, "Praia RF", 1245711162L, null },
                    { 21, 37.061441m, -8.836760m, "Praia Santa", 393042815L, null },
                    { 22, 37.115079m, -7.623417m, "Praia Tavira-Ria", 79087228L, null },
                    { 23, 37.077116m, -8.310642m, "Praia Tomás Franco", 93448953L, null },
                    { 24, 37.101802m, -8.370281m, "Praia Vale do Olival", 1497785950L, null },
                    { 25, 37.173954m, -7.478928m, "Praia Verde", 79503288L, null },
                    { 26, 37.172407m, -7.485948m, "Praia Verdelago", 588367516L, null },
                    { 27, 37.103400m, -8.509231m, "Praia da Afurada", 1252978464L, null },
                    { 28, 37.169481m, -7.498076m, "Praia da Alagoa", 79503985L, null },
                    { 29, 37.091049m, -8.399888m, "Praia da Albandeira", 1249710119L, null },
                    { 30, 37.090888m, -8.400358m, "Praia da Albandeira", 1249710120L, null },
                    { 31, 37.350858m, -8.844728m, "Praia da Amoreira", 1242019126L, null },
                    { 32, 37.482407m, -8.794534m, "Praia da Amália", 922225754L, null },
                    { 33, 37.121504m, -8.522726m, "Praia da Angrinha", 101836680L, null },
                    { 34, 37.018157m, -7.792200m, "Praia da Armona-Mar", 79531240L, null },
                    { 35, 37.023450m, -7.804726m, "Praia da Armona-Ria", 222576671L, null },
                    { 36, 37.292058m, -8.865464m, "Praia da Arrifana", 1241777732L, null },
                    { 37, 37.075513m, -8.305551m, "Praia da Balbina", 1236137162L, null },
                    { 38, 37.081766m, -8.262189m, "Praia da Baleeira", 129580025L, null },
                    { 39, 37.011318m, -8.930381m, "Praia da Baleeira", 176818984L, null },
                    { 40, 37.394556m, -8.818939m, "Praia da Barradinha", 964252291L, null },
                    { 41, 37.118858m, -8.930705m, "Praia da Barriga", 1239998570L, null },
                    { 42, 37.118974m, -8.929653m, "Praia da Barriga", 1239998572L, null },
                    { 43, 37.097853m, -8.667959m, "Praia da Batata", 24461578L, null },
                    { 44, 37.066364m, -8.808916m, "Praia da Boca do Rio", 1244670407L, null },
                    { 45, 37.199504m, -8.900906m, "Praia da Bordeira", 1241619236L, null },
                    { 46, 37.096044m, -8.667240m, "Praia da Caldeira", 1253692196L, null },
                    { 47, 37.362283m, -8.839475m, "Praia da Carreagem", 1242776850L, null },
                    { 48, 37.073684m, -8.294141m, "Praia da Coelha", 1251789728L, null },
                    { 49, 37.109300m, -8.937528m, "Praia da Cordoama", 1327926836L, null },
                    { 50, 37.087520m, -8.421607m, "Praia da Corredoura", 1247825356L, null },
                    { 51, 37.098578m, -8.380612m, "Praia da Cova Redonda", 107913221L, null },
                    { 52, 36.994880m, -7.824390m, "Praia da Culatra", 159418246L, null },
                    { 53, 37.003242m, -7.802258m, "Praia da Culatra", 6757020738L, null },
                    { 54, 37.090889m, -8.401201m, "Praia da Estaquinha", 1249669603L, null },
                    { 55, 37.408054m, -8.811674m, "Praia da Esteveira", 957426818L, null },
                    { 56, 37.075193m, -8.132325m, "Praia da Falésia", 129581895L, null },
                    { 57, 37.080440m, -8.149166m, "Praia da Falésia", 675161585L, null },
                    { 58, 37.083064m, -8.158103m, "Praia da Falésia Alfamar", 129584057L, null },
                    { 59, 37.086681m, -8.169256m, "Praia da Falésia Açoteias", 677024792L, null },
                    { 60, 37.060727m, -8.840333m, "Praia da Figueira", 156815841L, null },
                    { 61, 37.058393m, -8.845473m, "Praia da Foia do Carro", 1244215657L, null },
                    { 62, 37.073568m, -8.296799m, "Praia da Fraternidade", 309582065L, null },
                    { 63, 37.046321m, -7.739255m, "Praia da Fuseta-Mar", 79522255L, null },
                    { 64, 37.049962m, -7.744352m, "Praia da Fuseta-Ria", 78427662L, null },
                    { 65, 37.081841m, -8.318332m, "Praia da Galé", 1250910960L, null },
                    { 66, 37.080016m, -8.315348m, "Praia da Galé (leste)", 1250910959L, null },
                    { 67, 36.962919m, -7.879166m, "Praia da Ilha Deserta", 82505950L, null },
                    { 68, 36.972267m, -7.923369m, "Praia da Ilha da Barreta", 471184443L, null },
                    { 69, 37.003842m, -7.990809m, "Praia da Ilha de Faro", 12567393L, null },
                    { 70, 37.109911m, -7.620998m, "Praia da Ilha de Tavira", 78432559L, null },
                    { 71, 37.046621m, -8.879296m, "Praia da Ingrina", 156814646L, null },
                    { 72, 37.057651m, -8.081173m, "Praia da Lagoa", 655688261L, null },
                    { 73, 37.166069m, -7.510443m, "Praia da Lota", 79504039L, null },
                    { 74, 37.086769m, -8.724609m, "Praia da Luz", 156815842L, null },
                    { 75, 37.089945m, -8.407134m, "Praia da Malhada do Baraço", 160455813L, null },
                    { 76, 37.161878m, -7.521573m, "Praia da Manta Rota", 79504187L, null },
                    { 77, 37.005047m, -8.938728m, "Praia da Mareta", 71340752L, null },
                    { 78, 37.089777m, -8.412608m, "Praia da Marinha", 120164558L, null },
                    { 79, 37.089642m, -8.414159m, "Praia da Marinha", 1249063477L, null },
                    { 80, 37.073835m, -8.295783m, "Praia da Maré das Porcas", 1251789729L, null },
                    { 81, 37.089199m, -8.415089m, "Praia da Mesquita", 160455810L, null },
                    { 82, 37.093053m, -8.393998m, "Praia da Morena", 1249669595L, null },
                    { 83, 37.154764m, -8.908356m, "Praia da Muração", 1240127219L, null },
                    { 84, 37.085311m, -8.223371m, "Praia da Oura", 1329493935L, null },
                    { 85, 37.274950m, -8.863148m, "Praia da Pedra da Agulha", 1491203599L, null },
                    { 86, 37.085055m, -8.218348m, "Praia da Pedra dos Bicos", 155804981L, null },
                    { 87, 37.073297m, -8.287443m, "Praia da Ponta Grande", 1251789696L, null },
                    { 88, 37.073565m, -8.285315m, "Praia da Ponta Pequena", 1106872193L, null },
                    { 89, 37.068772m, -8.964573m, "Praia da Ponta Ruiva", 1243482695L, null },
                    { 90, 37.097147m, -8.384048m, "Praia da Ponta da Adega", 470523247L, null },
                    { 91, 37.118278m, -8.578246m, "Praia da Prainha", 1326145039L, null },
                    { 92, 37.024676m, -8.024685m, "Praia da Quinta do Lago", 84374228L, null },
                    { 93, 37.115647m, -8.536209m, "Praia da Rocha", 98687797L, null },
                    { 94, 37.064971m, -8.821625m, "Praia da Salema", 1244670402L, null },
                    { 95, 37.398341m, -8.816673m, "Praia da Samouqueira", 1256654176L, null },
                    { 96, 37.097102m, -8.385648m, "Praia da Senhora da Rocha", 1249999772L, null },
                    { 97, 37.098588m, -7.639625m, "Praia da Terra Estreita na Ilha de Tavira", 78432429L, null },
                    { 98, 37.075098m, -8.278401m, "Praia da Viga", 1252144328L, null },
                    { 99, 37.192942m, -8.913909m, "Praia da Zimbreirinha", 1328233392L, null },
                    { 100, 37.439180m, -8.800545m, "Praia das Adegas", 1242990213L, null },
                    { 101, 37.097444m, -8.383522m, "Praia das Escaleiras", 1250076184L, null },
                    { 102, 37.092007m, -8.395595m, "Praia das Fontaínhas", 160455815L, null },
                    { 103, 37.055223m, -8.854430m, "Praia das Furnas", 156814659L, null },
                    { 104, 37.168321m, -7.503516m, "Praia das Primas", 12070720137L, null },
                    { 105, 37.073274m, -8.289793m, "Praia das Salamitras", 1251789705L, null },
                    { 106, 37.065572m, -8.795579m, "Praia de Almádena", 1244670399L, null },
                    { 107, 37.100188m, -8.359572m, "Praia de Armação de Pêra", 259211193L, null },
                    { 108, 37.087351m, -8.425864m, "Praia de Benagil", 465776072L, null },
                    { 109, 37.132303m, -7.591430m, "Praia de Cabanas", 79509901L, null },
                    { 110, 37.064806m, -8.792952m, "Praia de Cabanas Velhas (Naturista)", 1056515224L, null },
                    { 111, 37.151574m, -7.547415m, "Praia de Cacela Velha", 7212953L, null },
                    { 112, 37.334504m, -8.860224m, "Praia de Coelha", 38502852L, null },
                    { 113, 37.092082m, -8.668811m, "Praia de Dona Ana", 29437665L, null },
                    { 114, 37.054563m, -8.075932m, "Praia de Loulé Velho", 261606273L, null },
                    { 115, 37.342187m, -8.853054m, "Praia de Monte Clérigo", 55705498L, null },
                    { 116, 37.176682m, -7.447351m, "Praia de Monte Gordo", 587923984L, null },
                    { 117, 37.442145m, -8.797848m, "Praia de Odeceixe-Mar", 1242990216L, null },
                    { 118, 37.472315m, -7.476970m, "Praia de Pego Fundo", 78417257L, null },
                    { 119, 37.065284m, -8.099553m, "Praia de Quarteira", 142594780L, null },
                    { 120, 37.087687m, -8.213484m, "Praia de Santa Eulália", 62955217L, null },
                    { 121, 37.171962m, -7.417776m, "Praia de Santo António", 5448095L, null },
                    { 122, 37.074783m, -8.280342m, "Praia de São Rafael", 1252085905L, null },
                    { 123, 37.091385m, -8.455771m, "Praia de Vale Centianes", 1247020899L, null },
                    { 124, 37.237411m, -8.875412m, "Praia de Vale Figueira", 828824087L, null },
                    { 125, 37.247352m, -8.869399m, "Praia de Vale Figueira", 1241777810L, null },
                    { 126, 37.048690m, -8.065718m, "Praia de Vale do Lobo", 655688263L, null },
                    { 127, 37.385319m, -8.824572m, "Praia de Vale dos Homens", 1242844058L, null },
                    { 128, 37.071106m, -8.116533m, "Praia de Vilamoura", 85956082L, null },
                    { 129, 37.119778m, -8.562213m, "Praia do Alemão", 607027167L, null },
                    { 130, 37.059271m, -8.084370m, "Praia do Almargem", 655688259L, null },
                    { 131, 37.121559m, -8.589980m, "Praia do Alvor Nascente", 606996959L, null },
                    { 132, 37.122701m, -8.597534m, "Praia do Alvor Poente", 606996960L, null },
                    { 133, 37.164340m, -8.903388m, "Praia do Amado", 1239998556L, null },
                    { 134, 37.169320m, -8.903121m, "Praia do Amado", 1328449735L, null },
                    { 135, 37.032438m, -8.038003m, "Praia do Ancão", 655688262L, null },
                    { 136, 37.042099m, -8.895173m, "Praia do Barranco", 156814654L, null },
                    { 137, 37.095394m, -8.391569m, "Praia do Barranco", 1249999787L, null },
                    { 138, 37.089869m, -8.180487m, "Praia do Barranco das Belharucas", 129584051L, null },
                    { 139, 37.119214m, -8.564496m, "Praia do Barranco das Canas", 218009625L, null },
                    { 140, 37.089894m, -8.405567m, "Praia do Barranquinho", 1249300471L, null },
                    { 141, 37.073892m, -7.686754m, "Praia do Barril", 78432336L, null },
                    { 142, 37.085207m, -7.663338m, "Praia do Barril", 1501060262L, null },
                    { 143, 37.025934m, -8.965080m, "Praia do Beliche", 94838670L, null },
                    { 144, 37.118387m, -8.566070m, "Praia do Boião", 1246032617L, null },
                    { 145, 37.089433m, -8.411204m, "Praia do Buraco", 232240426L, null },
                    { 146, 37.071302m, -8.775575m, "Praia do Burgau", 38502661L, null },
                    { 147, 37.175494m, -7.468997m, "Praia do Cabeço", 79503102L, null },
                    { 148, 37.087326m, -8.668430m, "Praia do Camilo", 71500098L, null },
                    { 149, 37.087981m, -8.668616m, "Praia do Camilo", 71500099L, null },
                    { 150, 37.266840m, -8.860857m, "Praia do Canal", 208839203L, null },
                    { 151, 37.270565m, -8.860261m, "Praia do Canal", 1255794138L, null },
                    { 152, 37.083841m, -8.679296m, "Praia do Canavial", 288904496L, null },
                    { 153, 37.500879m, -8.792577m, "Praia do Carvalhal", 157818268L, null },
                    { 154, 37.086638m, -8.431714m, "Praia do Carvalho", 110468045L, null },
                    { 155, 37.096037m, -8.472388m, "Praia do Carvoeiro", 129212171L, null },
                    { 156, 37.100071m, -8.947016m, "Praia do Castelejo", 1244531551L, null },
                    { 157, 37.073176m, -8.298900m, "Praia do Castelo", 48433466L, null },
                    { 158, 37.078995m, -8.313542m, "Praia do Chiringuito", 1250594381L, null },
                    { 159, 37.087450m, -8.418131m, "Praia do Cão Raivoso", 1248930430L, null },
                    { 160, 37.074228m, -8.302827m, "Praia do Evaristo", 51789928L, null },
                    { 161, 36.977569m, -7.857225m, "Praia do Farol", 19450196L, null },
                    { 162, 36.981104m, -7.861517m, "Praia do Farol (ria)", 1421663412L, null },
                    { 163, 37.061309m, -8.088447m, "Praia do Forte Novo", 655494861L, null },
                    { 164, 37.098841m, -8.668077m, "Praia do Forte da Bandeira", 6802536L, null },
                    { 165, 37.120012m, -7.611980m, "Praia do Forte da Barra", 79511624L, null },
                    { 166, 37.041329m, -8.053124m, "Praia do Garrão", 655688264L, null },
                    { 167, 37.061961m, -7.709914m, "Praia do Homem Nú", 7523213L, null },
                    { 168, 37.085770m, -8.245228m, "Praia do Inatel", 1357090689L, null },
                    { 169, 37.141966m, -7.571321m, "Praia do Lacém", 490879098L, null },
                    { 170, 37.018518m, -8.925617m, "Praia do Martinhal", 1254208003L, null },
                    { 171, 37.102211m, -8.506231m, "Praia do Mato", 1246703809L, null },
                    { 172, 37.324294m, -8.869489m, "Praia do Medo da Fonte Santa", 1242614517L, null },
                    { 173, 37.137011m, -8.920089m, "Praia do Mirouço", 208834927L, null },
                    { 174, 37.146929m, -8.915884m, "Praia do Mirouço", 1240127231L, null },
                    { 175, 37.110338m, -8.519859m, "Praia do Molhe", 7093714L, null },
                    { 176, 37.073473m, -8.286632m, "Praia do Ninho de Andorinha", 1251789697L, null },
                    { 177, 37.096949m, -8.475614m, "Praia do Paraíso", 303665313L, null },
                    { 178, 37.089741m, -8.410205m, "Praia do Pau", 160455814L, null },
                    { 179, 37.086098m, -8.253657m, "Praia do Peneco", 129575837L, null },
                    { 180, 37.252319m, -8.866244m, "Praia do Penedo", 1255794144L, null },
                    { 181, 37.094389m, -8.668038m, "Praia do Pinhão", 93278485L, null },
                    { 182, 37.108079m, -8.518860m, "Praia do Pintadinho", 117570615L, null },
                    { 183, 37.093443m, -8.392541m, "Praia do Pontal", 1249669592L, null },
                    { 184, 37.174365m, -8.907809m, "Praia do Portinho do Forno", 487969876L, null },
                    { 185, 37.084820m, -8.689938m, "Praia do Porto de Mós", 886719362L, null },
                    { 186, 37.118590m, -7.625204m, "Praia do Rato", 517429596L, null },
                    { 187, 37.079659m, -8.264317m, "Praia do Risco", 1252443721L, null },
                    { 188, 37.077844m, -8.311647m, "Praia do Sao Lourenco", 1250910953L, null },
                    { 189, 37.116502m, -8.571263m, "Praia do Submarino", 1245711159L, null },
                    { 190, 37.116397m, -8.571722m, "Praia do Submarino", 1245711160L, null },
                    { 191, 37.116586m, -8.570437m, "Praia do Submarino", 1245934920L, null },
                    { 192, 37.045636m, -8.979094m, "Praia do Telheiro", 1243320967L, null },
                    { 193, 37.005714m, -8.947897m, "Praia do Tonel", 1255056880L, null },
                    { 194, 37.105867m, -8.515883m, "Praia do Torrado", 204181234L, null },
                    { 195, 37.093147m, -8.459854m, "Praia do Vale Covo", 481006224L, null },
                    { 196, 37.093313m, -8.460129m, "Praia do Vale Covo", 1247020903L, null },
                    { 197, 37.087810m, -8.440759m, "Praia do Vale Espinhaço", 470544217L, null },
                    { 198, 37.119863m, -8.559292m, "Praia do Vau", 607027170L, null },
                    { 199, 37.046402m, -8.870493m, "Praia do Zavial", 156814657L, null },
                    { 200, 37.043959m, -8.866545m, "Praia do Zavial Naturista", 1243839258L, null },
                    { 201, 37.084011m, -8.237966m, "Praia dos Alemães", 129577161L, null },
                    { 202, 37.075921m, -8.277528m, "Praia dos Arrifes", 1252144318L, null },
                    { 203, 37.083406m, -8.231314m, "Praia dos Aveiros", 129581901L, null },
                    { 204, 37.100828m, -8.373887m, "Praia dos Beijinhos", 108165451L, null },
                    { 205, 37.078270m, -8.312479m, "Praia dos Bés", 1250910956L, null },
                    { 206, 37.104988m, -8.514001m, "Praia dos Caneiros", 1246703830L, null },
                    { 207, 37.119051m, -8.555377m, "Praia dos Careanos", 607027161L, null },
                    { 208, 37.098696m, -8.481946m, "Praia dos Castelos", 621125242L, null },
                    { 209, 37.036260m, -7.796241m, "Praia dos Cavacos", 508415850L, null },
                    { 210, 37.096498m, -8.667490m, "Praia dos Estudantes", 93278502L, null },
                    { 211, 37.492439m, -8.794897m, "Praia dos Machados", 208117757L, null },
                    { 212, 37.089709m, -8.190699m, "Praia dos Olhos de Água", 1254991134L, null },
                    { 213, 37.074524m, -8.282057m, "Praia dos Paradinha", 1252085894L, null },
                    { 214, 37.086563m, -8.250091m, "Praia dos Pescadores", 129575835L, null },
                    { 215, 37.084340m, -8.667266m, "Praia dos Pinheiros", 1245077034L, null },
                    { 216, 37.073850m, -8.283768m, "Praia dos Piratas", 1252085915L, null },
                    { 217, 37.022221m, -8.920294m, "Praia dos Rebolinhos", 1254208004L, null },
                    { 218, 37.068774m, -8.782215m, "Praia dos Rebolos", 453722169L, null },
                    { 219, 37.086338m, -8.325025m, "Praia dos Salgados", 51986551L, null },
                    { 220, 37.006532m, -7.941509m, "Praia dos Tesos", 11384145L, null },
                    { 221, 37.078372m, -8.142019m, "Praia dos Tomates", 1324074429L, null },
                    { 222, 37.099803m, -8.377072m, "Praia dos Tremoços", 119246525L, null },
                    { 223, 37.117654m, -8.547906m, "Praia dos Três Castelos", 1246173354L, null },
                    { 224, 37.119713m, -8.581893m, "Praia dos Três Irmãos", 1245711193L, null },
                    { 225, 37.084918m, -8.730815m, "Prainha", 549029920L, null },
                    { 226, 37.197106m, -8.498926m, "Quinta Amoroso", 10224558485L, null },
                    { 227, 37.319324m, -8.876724m, null, 38502688L, null },
                    { 228, 37.092326m, -7.649813m, null, 78432449L, null },
                    { 229, 37.032097m, -7.763479m, null, 79527276L, null },
                    { 230, 36.970832m, -7.875069m, null, 82505947L, null },
                    { 231, 37.052261m, -7.741904m, null, 90144518L, null },
                    { 232, 37.075680m, -8.306681m, null, 92000516L, null },
                    { 233, 37.094959m, -8.667712m, null, 93278499L, null },
                    { 234, 37.008333m, -8.948965m, null, 94838668L, null },
                    { 235, 37.133315m, -8.595440m, null, 105689399L, null },
                    { 236, 37.156158m, -7.545889m, null, 132676483L, null },
                    { 237, 37.125535m, -8.609114m, null, 143915392L, null },
                    { 238, 37.104999m, -8.941940m, null, 157867259L, null },
                    { 239, 37.055196m, -7.722937m, null, 172993677L, null },
                    { 240, 37.185821m, -7.346082m, null, 192229778L, null },
                    { 241, 37.001969m, -7.814670m, null, 222572205L, null },
                    { 242, 37.021790m, -7.806153m, null, 222576674L, null },
                    { 243, 37.128418m, -8.523629m, null, 233235193L, null },
                    { 244, 37.082952m, -8.667795m, null, 288904494L, null },
                    { 245, 37.083633m, -8.667184m, null, 288904495L, null },
                    { 246, 37.085573m, -8.228386m, null, 308241715L, null },
                    { 247, 37.471649m, -7.468302m, null, 364956367L, null },
                    { 248, 37.099216m, -8.377830m, null, 437587998L, null },
                    { 249, 37.035187m, -8.024354m, null, 443102845L, null },
                    { 250, 37.035168m, -8.025976m, null, 443102849L, null },
                    { 251, 37.117368m, -7.617496m, null, 490880503L, null },
                    { 252, 37.118929m, -7.616337m, null, 490882066L, null },
                    { 253, 37.086565m, -8.216681m, null, 564141456L, null },
                    { 254, 37.088333m, -8.207676m, null, 564274793L, null },
                    { 255, 37.121797m, -8.609001m, null, 604051046L, null },
                    { 256, 37.113968m, -8.934413m, null, 810707335L, null },
                    { 257, 37.086824m, -8.676214m, null, 986473094L, null },
                    { 258, 37.292205m, -8.467887m, null, 989678720L, null },
                    { 259, 37.209234m, -8.894058m, null, 1241717170L, null },
                    { 260, 37.217422m, -8.887937m, null, 1241717181L, null },
                    { 261, 37.213668m, -8.890750m, null, 1241777800L, null },
                    { 262, 37.242035m, -8.872296m, null, 1241777808L, null },
                    { 263, 37.242805m, -8.872015m, null, 1241777809L, null },
                    { 264, 37.027507m, -8.981808m, null, 1243513171L, null },
                    { 265, 37.057260m, -8.848322m, null, 1244215651L, null },
                    { 266, 37.055562m, -8.852719m, null, 1244215652L, null },
                    { 267, 37.059629m, -8.842395m, null, 1244215656L, null },
                    { 268, 37.098203m, -8.947404m, null, 1244531550L, null },
                    { 269, 37.117078m, -8.575344m, null, 1245711165L, null },
                    { 270, 37.117322m, -8.575883m, null, 1245711168L, null },
                    { 271, 37.115041m, -8.528531m, null, 1246423276L, null },
                    { 272, 37.100624m, -8.374960m, null, 1250076164L, null },
                    { 273, 37.101448m, -8.371729m, null, 1250076174L, null },
                    { 274, 37.098062m, -8.382532m, null, 1250076188L, null },
                    { 275, 37.101399m, -8.372160m, null, 1250548776L, null },
                    { 276, 37.075610m, -8.307255m, null, 1250926280L, null },
                    { 277, 37.073671m, -8.284536m, null, 1252085924L, null },
                    { 278, 37.073731m, -8.284654m, null, 1252085925L, null },
                    { 279, 37.079226m, -8.266801m, null, 1252443719L, null },
                    { 280, 37.073749m, -8.283770m, null, 1252495883L, null },
                    { 281, 37.096106m, -8.667517m, null, 1253692197L, null },
                    { 282, 37.336950m, -8.858353m, null, 1256162807L, null },
                    { 283, 37.090052m, -8.187091m, null, 1322900164L, null },
                    { 284, 37.049898m, -8.980785m, null, 1322931016L, null },
                    { 285, 37.118003m, -8.577588m, null, 1326145037L, null },
                    { 286, 37.117964m, -8.576956m, null, 1326145038L, null },
                    { 287, 37.171007m, -8.903739m, null, 1328449733L, null },
                    { 288, 37.171100m, -8.904012m, null, 1328449734L, null },
                    { 289, 37.002627m, -7.986199m, null, 1351185084L, null },
                    { 290, 37.049158m, -7.733876m, null, 5117235785L, null },
                    { 291, 37.010991m, -7.931671m, null, 8795678512L, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 66);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 67);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 68);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 69);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 70);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 71);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 72);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 73);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 74);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 75);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 76);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 77);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 78);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 79);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 80);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 81);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 82);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 83);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 84);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 85);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 86);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 87);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 88);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 89);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 90);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 91);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 92);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 93);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 94);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 95);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 96);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 97);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 98);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 99);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 106);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 107);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 108);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 109);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 110);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 111);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 112);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 113);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 114);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 115);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 116);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 117);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 118);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 119);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 120);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 121);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 122);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 123);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 124);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 125);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 126);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 127);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 128);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 129);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 130);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 131);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 132);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 133);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 134);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 135);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 136);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 137);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 138);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 139);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 140);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 141);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 142);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 143);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 144);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 145);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 146);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 147);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 148);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 149);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 150);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 151);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 152);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 153);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 154);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 155);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 156);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 157);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 158);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 159);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 160);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 161);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 162);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 163);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 164);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 165);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 166);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 167);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 168);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 169);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 170);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 171);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 172);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 173);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 174);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 175);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 176);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 177);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 178);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 179);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 180);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 181);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 182);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 183);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 184);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 185);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 186);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 187);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 188);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 189);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 190);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 191);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 192);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 193);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 194);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 195);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 196);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 197);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 198);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 199);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 200);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 201);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 202);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 203);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 204);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 205);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 206);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 207);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 208);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 209);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 210);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 211);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 212);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 213);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 214);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 215);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 216);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 217);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 218);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 219);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 220);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 221);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 222);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 223);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 224);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 225);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 226);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 227);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 228);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 229);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 230);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 231);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 232);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 233);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 234);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 235);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 236);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 237);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 238);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 239);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 240);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 241);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 242);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 243);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 244);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 245);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 246);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 247);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 248);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 249);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 250);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 251);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 252);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 253);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 254);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 255);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 256);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 257);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 258);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 259);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 260);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 261);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 262);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 263);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 264);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 265);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 266);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 267);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 268);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 269);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 270);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 271);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 272);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 273);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 274);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 275);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 276);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 277);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 278);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 279);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 280);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 281);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 282);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 283);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 284);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 285);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 286);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 287);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 288);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 289);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 290);

            migrationBuilder.DeleteData(
                table: "BeachMarkers",
                keyColumn: "Id",
                keyValue: 291);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "BeachMarkers",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");
        }
    }
}
