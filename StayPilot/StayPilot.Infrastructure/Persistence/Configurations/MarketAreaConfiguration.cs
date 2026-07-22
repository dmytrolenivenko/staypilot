using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayPilot.Domain.Entities;


namespace StayPilot.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Maps the MarketArea entity to its table and loads the market area seed data.
    /// The seed rows are written into the table when the migration runs.
    /// </summary>
    public class MarketAreaConfiguration : IEntityTypeConfiguration<MarketArea>
    {
        /// <summary>
        /// Sets column rules and adds the fixed list of Algarve market areas.
        /// </summary>
        public void Configure(EntityTypeBuilder<MarketArea> builder)
        {
            // The database fills the create date on insert (UTC now).
            builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

            // Compare these text columns without caring about case or accents.
            // "CI" = case insensitive, "AI" = accent insensitive. So "Faro" matches "faró".
            builder.Property(x => x.District).UseCollation("Latin1_General_CI_AI");
            builder.Property(x => x.Municipality).UseCollation("Latin1_General_CI_AI");
            builder.Property(x => x.Town).UseCollation("Latin1_General_CI_AI");
            builder.Property(x => x.Zone).UseCollation("Latin1_General_CI_AI");

            // Seed data: all the Algarve market areas (district / municipality / town / zone) taken from Idealista.
            builder.HasData(
                // Albufeira
                new MarketArea { Id = 1, Country = "Portugal", District = "Faro", Municipality = "Albufeira", Town = "Albufeira e Olhos de Água", Zone = "Centro da Cidade" },
                new MarketArea { Id = 2, Country = "Portugal", District = "Faro", Municipality = "Albufeira", Town = "Albufeira e Olhos de Água", Zone = "Olhos de Água" },
                new MarketArea { Id = 3, Country = "Portugal", District = "Faro", Municipality = "Albufeira", Town = "Albufeira e Olhos de Água", Zone = "Montechoro" },
                new MarketArea { Id = 4, Country = "Portugal", District = "Faro", Municipality = "Albufeira", Town = "Albufeira e Olhos de Água", Zone = "Marina de Albufeira - Cerro da Piedade" },
                new MarketArea { Id = 5, Country = "Portugal", District = "Faro", Municipality = "Albufeira", Town = "Albufeira e Olhos de Água", Zone = "Balaia" },
                new MarketArea { Id = 6, Country = "Portugal", District = "Faro", Municipality = "Albufeira", Town = "Albufeira e Olhos de Água", Zone = "Praia da Falésia" },
                new MarketArea { Id = 7, Country = "Portugal", District = "Faro", Municipality = "Albufeira", Town = "Albufeira e Olhos de Água", Zone = "Clube Albufeira" },
                new MarketArea { Id = 8, Country = "Portugal", District = "Faro", Municipality = "Albufeira", Town = "Albufeira e Olhos de Água", Zone = "Praia da Oura - Areias de S. João" },
                new MarketArea { Id = 9, Country = "Portugal", District = "Faro", Municipality = "Albufeira", Town = "Albufeira e Olhos de Água", Zone = "São Rafael" },
                new MarketArea { Id = 10, Country = "Portugal", District = "Faro", Municipality = "Albufeira", Town = "Albufeira e Olhos de Água", Zone = "Cerro de Águia - Patroves" },
                new MarketArea { Id = 11, Country = "Portugal", District = "Faro", Municipality = "Albufeira", Town = "Albufeira e Olhos de Água", Zone = "Sesmarias" },
                new MarketArea { Id = 12, Country = "Portugal", District = "Faro", Municipality = "Albufeira", Town = "Albufeira e Olhos de Água", Zone = "Forte São João" },
                new MarketArea { Id = 13, Country = "Portugal", District = "Faro", Municipality = "Albufeira", Town = "Ferreiras", Zone = null },
                new MarketArea { Id = 14, Country = "Portugal", District = "Faro", Municipality = "Albufeira", Town = "Guia", Zone = "Salgados" },
                new MarketArea { Id = 15, Country = "Portugal", District = "Faro", Municipality = "Albufeira", Town = "Guia", Zone = "Galé" },
                new MarketArea { Id = 16, Country = "Portugal", District = "Faro", Municipality = "Albufeira", Town = "Guia", Zone = "Vale de Parra" },
                new MarketArea { Id = 17, Country = "Portugal", District = "Faro", Municipality = "Albufeira", Town = "Paderne", Zone = null },
                // Alcoutim
                new MarketArea { Id = 18, Country = "Portugal", District = "Faro", Municipality = "Alcoutim", Town = "Alcoutim e Pereiro", Zone = null },
                new MarketArea { Id = 19, Country = "Portugal", District = "Faro", Municipality = "Alcoutim", Town = "Giões", Zone = null },
                new MarketArea { Id = 20, Country = "Portugal", District = "Faro", Municipality = "Alcoutim", Town = "Martim Longo", Zone = null },
                new MarketArea { Id = 21, Country = "Portugal", District = "Faro", Municipality = "Alcoutim", Town = "Vaqueiros", Zone = null },
                // Aljezur
                new MarketArea { Id = 22, Country = "Portugal", District = "Faro", Municipality = "Aljezur", Town = "Aljezur", Zone = null },
                new MarketArea { Id = 23, Country = "Portugal", District = "Faro", Municipality = "Aljezur", Town = "Bordeira", Zone = null },
                new MarketArea { Id = 24, Country = "Portugal", District = "Faro", Municipality = "Aljezur", Town = "Odeceixe", Zone = null },
                new MarketArea { Id = 25, Country = "Portugal", District = "Faro", Municipality = "Aljezur", Town = "Rogil", Zone = null },
                // Castro Marim
                new MarketArea { Id = 26, Country = "Portugal", District = "Faro", Municipality = "Castro Marim", Town = "Altura", Zone = null },
                new MarketArea { Id = 27, Country = "Portugal", District = "Faro", Municipality = "Castro Marim", Town = "Azinhal", Zone = null },
                new MarketArea { Id = 28, Country = "Portugal", District = "Faro", Municipality = "Castro Marim", Town = "Castro Marim", Zone = "Centro" },
                new MarketArea { Id = 29, Country = "Portugal", District = "Faro", Municipality = "Castro Marim", Town = "Castro Marim", Zone = "Praia Verde" },
                new MarketArea { Id = 30, Country = "Portugal", District = "Faro", Municipality = "Castro Marim", Town = "Castro Marim", Zone = "Quinta do Sobral - São Bartolomeu" },
                new MarketArea { Id = 31, Country = "Portugal", District = "Faro", Municipality = "Castro Marim", Town = "Castro Marim", Zone = "Monte Francisco" },
                new MarketArea { Id = 32, Country = "Portugal", District = "Faro", Municipality = "Castro Marim", Town = "Castro Marim", Zone = "Junqueira - Beliche" },
                new MarketArea { Id = 33, Country = "Portugal", District = "Faro", Municipality = "Castro Marim", Town = "Castro Marim", Zone = "Golf Resort" },
                new MarketArea { Id = 34, Country = "Portugal", District = "Faro", Municipality = "Castro Marim", Town = "Odeleite", Zone = null },
                // Faro
                new MarketArea { Id = 35, Country = "Portugal", District = "Faro", Municipality = "Faro", Town = "Conceição", Zone = null },
                new MarketArea { Id = 36, Country = "Portugal", District = "Faro", Municipality = "Faro", Town = "Estoi", Zone = null },
                new MarketArea { Id = 37, Country = "Portugal", District = "Faro", Municipality = "Faro", Town = "Faro", Zone = "Centro" },
                new MarketArea { Id = 38, Country = "Portugal", District = "Faro", Municipality = "Faro", Town = "Faro", Zone = "Arneiro - Braciais - Patacão" },
                new MarketArea { Id = 39, Country = "Portugal", District = "Faro", Municipality = "Faro", Town = "Faro", Zone = "Areal Gordo - Rio Seco - Ilha da Culatra" },
                new MarketArea { Id = 40, Country = "Portugal", District = "Faro", Municipality = "Faro", Town = "Faro", Zone = "Horta das Figuras - Lejana - Senhora da Saúde" },
                new MarketArea { Id = 41, Country = "Portugal", District = "Faro", Municipality = "Faro", Town = "Faro", Zone = "Penha - Vale da Amoreira" },
                new MarketArea { Id = 42, Country = "Portugal", District = "Faro", Municipality = "Faro", Town = "Faro", Zone = "São Luís" },
                new MarketArea { Id = 43, Country = "Portugal", District = "Faro", Municipality = "Faro", Town = "Faro", Zone = "Alto de Santo António - Bom João - João de Deus" },
                new MarketArea { Id = 44, Country = "Portugal", District = "Faro", Municipality = "Faro", Town = "Faro", Zone = "Alto Rodes" },
                new MarketArea { Id = 45, Country = "Portugal", District = "Faro", Municipality = "Faro", Town = "Montenegro", Zone = null },
                new MarketArea { Id = 46, Country = "Portugal", District = "Faro", Municipality = "Faro", Town = "Montenegro", Zone = "Quinta do Eucalipto - Ilha de Faro" },
                new MarketArea { Id = 47, Country = "Portugal", District = "Faro", Municipality = "Faro", Town = "Santa Bárbara de Nexe", Zone = null },
                // Lagoa
                new MarketArea { Id = 48, Country = "Portugal", District = "Faro", Municipality = "Lagoa", Town = "Estombar e Parchal", Zone = null },
                new MarketArea { Id = 49, Country = "Portugal", District = "Faro", Municipality = "Lagoa", Town = "Ferragudo", Zone = null },
                new MarketArea { Id = 50, Country = "Portugal", District = "Faro", Municipality = "Lagoa", Town = "Lagoa e Carvoeiro", Zone = "Cidade de Lagoa" },
                new MarketArea { Id = 51, Country = "Portugal", District = "Faro", Municipality = "Lagoa", Town = "Lagoa e Carvoeiro", Zone = "Centro de Carvoeiro" },
                new MarketArea { Id = 52, Country = "Portugal", District = "Faro", Municipality = "Lagoa", Town = "Lagoa e Carvoeiro", Zone = "Vale Milho - Vale Centeanes - Algar Seco" },
                new MarketArea { Id = 53, Country = "Portugal", District = "Faro", Municipality = "Lagoa", Town = "Lagoa e Carvoeiro", Zone = "Salicos - Sesmarias - Boavista" },
                new MarketArea { Id = 54, Country = "Portugal", District = "Faro", Municipality = "Lagoa", Town = "Lagoa e Carvoeiro", Zone = "Caramujeira - Vale d'El Rei - Benagil" },
                new MarketArea { Id = 55, Country = "Portugal", District = "Faro", Municipality = "Lagoa", Town = "Lagoa e Carvoeiro", Zone = "Mato Serrão - Vale da Lapa - Vale Currais" },
                new MarketArea { Id = 56, Country = "Portugal", District = "Faro", Municipality = "Lagoa", Town = "Porches", Zone = null },
                // Lagos
                new MarketArea { Id = 57, Country = "Portugal", District = "Faro", Municipality = "Lagos", Town = "Barão de São João", Zone = null },
                new MarketArea { Id = 58, Country = "Portugal", District = "Faro", Municipality = "Lagos", Town = "Bensafrim", Zone = null },
                new MarketArea { Id = 59, Country = "Portugal", District = "Faro", Municipality = "Lagos", Town = "Lagos", Zone = null },
                new MarketArea { Id = 60, Country = "Portugal", District = "Faro", Municipality = "Lagos", Town = "Lagos", Zone = "Lagos Cidade" },
                new MarketArea { Id = 61, Country = "Portugal", District = "Faro", Municipality = "Lagos", Town = "Lagos", Zone = "Meia Praia" },
                new MarketArea { Id = 62, Country = "Portugal", District = "Faro", Municipality = "Lagos", Town = "Lagos", Zone = "Quinta da Boavista" },
                new MarketArea { Id = 63, Country = "Portugal", District = "Faro", Municipality = "Lagos", Town = "Lagos", Zone = "Falfeira - Monte Funchal" },
                new MarketArea { Id = 64, Country = "Portugal", District = "Faro", Municipality = "Lagos", Town = "Lagos", Zone = "Chinicato - Sargaçal" },
                new MarketArea { Id = 65, Country = "Portugal", District = "Faro", Municipality = "Lagos", Town = "Luz", Zone = null },
                new MarketArea { Id = 66, Country = "Portugal", District = "Faro", Municipality = "Lagos", Town = "Odiaxere", Zone = null },
                // Loulé
                new MarketArea { Id = 67, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Almancil", Zone = "Centro" },
                new MarketArea { Id = 68, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Almancil", Zone = "Vale do Lobo" },
                new MarketArea { Id = 69, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Almancil", Zone = "Quinta do Lago - Pinheiros Altos" },
                new MarketArea { Id = 70, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Almancil", Zone = "Vale do Garrão - Varandas do Lago - Quinta das Salinas" },
                new MarketArea { Id = 71, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Almancil", Zone = "The Village - Fonte Algarve - Quinta Verde" },
                new MarketArea { Id = 72, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Almancil", Zone = "Vale Formoso - Vale d'Éguas" },
                new MarketArea { Id = 73, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Almancil", Zone = "São Lourenço - São João da Venda" },
                new MarketArea { Id = 74, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Alte", Zone = null },
                new MarketArea { Id = 75, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Ameixial", Zone = null },
                new MarketArea { Id = 76, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Benafim", Zone = null },
                new MarketArea { Id = 77, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Boliqueime", Zone = null },
                new MarketArea { Id = 78, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Quarteira", Zone = "Praia de Quarteira" },
                new MarketArea { Id = 79, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Quarteira", Zone = "Centro - Quarteira Velha" },
                new MarketArea { Id = 80, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Quarteira", Zone = "Fonte Santa" },
                new MarketArea { Id = 81, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Quarteira", Zone = "Aldeia do Golf - Alto do Golf" },
                new MarketArea { Id = 82, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Quarteira", Zone = "Vilamoura" },
                new MarketArea { Id = 83, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Quarteira", Zone = "Marina de Vilamoura" },
                new MarketArea { Id = 84, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Quarteira", Zone = "Pinhal Velho - Terraços do Pinhal - Encosta das Oliveiras" },
                new MarketArea { Id = 85, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Quarteira", Zone = "Vila Sol - Morgadinho" },
                new MarketArea { Id = 86, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Querença", Zone = null },
                new MarketArea { Id = 87, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Salir", Zone = null },
                new MarketArea { Id = 88, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "São Clemente", Zone = "Centro Histórico Este de Loulé" },
                new MarketArea { Id = 89, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "São Clemente", Zone = "Centro Este da Cidade de Loulé" },
                new MarketArea { Id = 90, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "São Sebastião", Zone = "Centro Oeste da Cidade de Loulé" },
                new MarketArea { Id = 91, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "São Sebastião", Zone = "Cerro de Cabeça de Câmara - Estação de Loulé" },
                new MarketArea { Id = 92, Country = "Portugal", District = "Faro", Municipality = "Loulé", Town = "Tor", Zone = null },
                // Monchique
                new MarketArea { Id = 93, Country = "Portugal", District = "Faro", Municipality = "Monchique", Town = "Alferce", Zone = null },
                new MarketArea { Id = 94, Country = "Portugal", District = "Faro", Municipality = "Monchique", Town = "Marmelete", Zone = null },
                new MarketArea { Id = 95, Country = "Portugal", District = "Faro", Municipality = "Monchique", Town = "Monchique", Zone = null },
                // Olhão
                new MarketArea { Id = 96, Country = "Portugal", District = "Faro", Municipality = "Olhão", Town = "Fuseta", Zone = null },
                new MarketArea { Id = 97, Country = "Portugal", District = "Faro", Municipality = "Olhão", Town = "Moncarapacho", Zone = null },
                new MarketArea { Id = 98, Country = "Portugal", District = "Faro", Municipality = "Olhão", Town = "Olhão", Zone = "Baixa" },
                new MarketArea { Id = 99, Country = "Portugal", District = "Faro", Municipality = "Olhão", Town = "Olhão", Zone = "Marina" },
                new MarketArea { Id = 100, Country = "Portugal", District = "Faro", Municipality = "Olhão", Town = "Olhão", Zone = "Cavalinha - Bombeiros" },
                new MarketArea { Id = 101, Country = "Portugal", District = "Faro", Municipality = "Olhão", Town = "Olhão", Zone = "Estádio" },
                new MarketArea { Id = 102, Country = "Portugal", District = "Faro", Municipality = "Olhão", Town = "Pechão", Zone = null },
                new MarketArea { Id = 103, Country = "Portugal", District = "Faro", Municipality = "Olhão", Town = "Quelfes", Zone = null },
                // Portimão
                new MarketArea { Id = 104, Country = "Portugal", District = "Faro", Municipality = "Portimão", Town = "Alvor", Zone = null },
                new MarketArea { Id = 105, Country = "Portugal", District = "Faro", Municipality = "Portimão", Town = "Mexilhoeira Grande", Zone = null },
                new MarketArea { Id = 106, Country = "Portugal", District = "Faro", Municipality = "Portimão", Town = "Portimão", Zone = null },
                new MarketArea { Id = 107, Country = "Portugal", District = "Faro", Municipality = "Portimão", Town = "Portimão", Zone = "Portimão Cidade" },
                new MarketArea { Id = 108, Country = "Portugal", District = "Faro", Municipality = "Portimão", Town = "Portimão", Zone = "Praia da Rocha" },
                new MarketArea { Id = 109, Country = "Portugal", District = "Faro", Municipality = "Portimão", Town = "Portimão", Zone = "Aldeia do Carrasco - Vale da Arrancada" },
                new MarketArea { Id = 110, Country = "Portugal", District = "Faro", Municipality = "Portimão", Town = "Portimão", Zone = "Amparo - Alto do Quintão" },
                new MarketArea { Id = 111, Country = "Portugal", District = "Faro", Municipality = "Portimão", Town = "Portimão", Zone = "Bemposta - Quatro Estradas" },
                new MarketArea { Id = 112, Country = "Portugal", District = "Faro", Municipality = "Portimão", Town = "Portimão", Zone = "Quinta da Malata" },
                new MarketArea { Id = 113, Country = "Portugal", District = "Faro", Municipality = "Portimão", Town = "Portimão", Zone = "Vale de Lagar - Quinta das Oliveiras - Pedra Mourinha" },
                // São Brás de Alportel
                new MarketArea { Id = 114, Country = "Portugal", District = "Faro", Municipality = "São Brás de Alportel", Town = "São Brás de Alportel", Zone = "Centro" },
                new MarketArea { Id = 115, Country = "Portugal", District = "Faro", Municipality = "São Brás de Alportel", Town = "São Brás de Alportel", Zone = "Campina - Mesquita" },
                new MarketArea { Id = 116, Country = "Portugal", District = "Faro", Municipality = "São Brás de Alportel", Town = "São Brás de Alportel", Zone = "São Romão - Fonte do Touro" },
                new MarketArea { Id = 117, Country = "Portugal", District = "Faro", Municipality = "São Brás de Alportel", Town = "São Brás de Alportel", Zone = "Funchais - Corotelo" },
                new MarketArea { Id = 118, Country = "Portugal", District = "Faro", Municipality = "São Brás de Alportel", Town = "São Brás de Alportel", Zone = "Barrabés - Peral" },
                // Silves
                new MarketArea { Id = 119, Country = "Portugal", District = "Faro", Municipality = "Silves", Town = "Alcantarilha", Zone = null },
                new MarketArea { Id = 120, Country = "Portugal", District = "Faro", Municipality = "Silves", Town = "Algoz", Zone = null },
                new MarketArea { Id = 121, Country = "Portugal", District = "Faro", Municipality = "Silves", Town = "Armação de Pêra", Zone = null },
                new MarketArea { Id = 122, Country = "Portugal", District = "Faro", Municipality = "Silves", Town = "Pêra", Zone = null },
                new MarketArea { Id = 123, Country = "Portugal", District = "Faro", Municipality = "Silves", Town = "Silves", Zone = "Centro da Cidade" },
                new MarketArea { Id = 124, Country = "Portugal", District = "Faro", Municipality = "Silves", Town = "Silves", Zone = "Zona Histórica" },
                new MarketArea { Id = 125, Country = "Portugal", District = "Faro", Municipality = "Silves", Town = "Silves", Zone = "Vila Fria" },
                new MarketArea { Id = 126, Country = "Portugal", District = "Faro", Municipality = "Silves", Town = "Silves", Zone = "Vale da Vila - Poço Barreto" },
                new MarketArea { Id = 127, Country = "Portugal", District = "Faro", Municipality = "Silves", Town = "Silves", Zone = "Enxerim - Barrada" },
                new MarketArea { Id = 128, Country = "Portugal", District = "Faro", Municipality = "Silves", Town = "Silves", Zone = "Estação de Silves - Cerro de São Miguel" },
                new MarketArea { Id = 129, Country = "Portugal", District = "Faro", Municipality = "Silves", Town = "Silves", Zone = "Serra - Barragem do Arade" },
                new MarketArea { Id = 130, Country = "Portugal", District = "Faro", Municipality = "Silves", Town = "São Bartolomeu de Messines", Zone = null },
                new MarketArea { Id = 131, Country = "Portugal", District = "Faro", Municipality = "Silves", Town = "São Marcos da Serra", Zone = null },
                new MarketArea { Id = 132, Country = "Portugal", District = "Faro", Municipality = "Silves", Town = "Tunes", Zone = null },
                // Tavira
                new MarketArea { Id = 133, Country = "Portugal", District = "Faro", Municipality = "Tavira", Town = "Cabanas de Tavira", Zone = null },
                new MarketArea { Id = 134, Country = "Portugal", District = "Faro", Municipality = "Tavira", Town = "Cachopo", Zone = null },
                new MarketArea { Id = 135, Country = "Portugal", District = "Faro", Municipality = "Tavira", Town = "Conceição", Zone = null },
                new MarketArea { Id = 136, Country = "Portugal", District = "Faro", Municipality = "Tavira", Town = "Luz de Tavira", Zone = null },
                new MarketArea { Id = 137, Country = "Portugal", District = "Faro", Municipality = "Tavira", Town = "Santa Catarina - Fonte do Bispo", Zone = null },
                new MarketArea { Id = 138, Country = "Portugal", District = "Faro", Municipality = "Tavira", Town = "Santa Luzia", Zone = null },
                new MarketArea { Id = 139, Country = "Portugal", District = "Faro", Municipality = "Tavira", Town = "Santo Estêvão", Zone = null },
                new MarketArea { Id = 140, Country = "Portugal", District = "Faro", Municipality = "Tavira", Town = "Tavira", Zone = null },
                new MarketArea { Id = 141, Country = "Portugal", District = "Faro", Municipality = "Tavira", Town = "Tavira", Zone = "Centro Histórico" },
                new MarketArea { Id = 142, Country = "Portugal", District = "Faro", Municipality = "Tavira", Town = "Tavira", Zone = "Porta Nova - Colinas da Boavista" },
                new MarketArea { Id = 143, Country = "Portugal", District = "Faro", Municipality = "Tavira", Town = "Tavira", Zone = "Pegada - Mato Santo Espírito - Vale Carangueijo" },
                new MarketArea { Id = 144, Country = "Portugal", District = "Faro", Municipality = "Tavira", Town = "Tavira", Zone = "Quinta da Foz - Escolas" },
                new MarketArea { Id = 145, Country = "Portugal", District = "Faro", Municipality = "Tavira", Town = "Tavira", Zone = "Colina de Asseca - Quinta de Perogil - São Pedro" },
                new MarketArea { Id = 146, Country = "Portugal", District = "Faro", Municipality = "Tavira", Town = "Tavira", Zone = "Serra" },
                // Vila do Bispo
                new MarketArea { Id = 147, Country = "Portugal", District = "Faro", Municipality = "Vila do Bispo", Town = "Barão de São Miguel", Zone = null },
                new MarketArea { Id = 148, Country = "Portugal", District = "Faro", Municipality = "Vila do Bispo", Town = "Budens", Zone = null },
                new MarketArea { Id = 149, Country = "Portugal", District = "Faro", Municipality = "Vila do Bispo", Town = "Sagres", Zone = null },
                new MarketArea { Id = 150, Country = "Portugal", District = "Faro", Municipality = "Vila do Bispo", Town = "Vila do Bispo e Raposeira", Zone = null },
                // Vila Real de Santo António
                new MarketArea { Id = 151, Country = "Portugal", District = "Faro", Municipality = "Vila Real de Santo António", Town = "Monte Gordo", Zone = null },
                new MarketArea { Id = 152, Country = "Portugal", District = "Faro", Municipality = "Vila Real de Santo António", Town = "Vila Nova de Cacela", Zone = null },
                new MarketArea { Id = 153, Country = "Portugal", District = "Faro", Municipality = "Vila Real de Santo António", Town = "Vila Real de Santo António", Zone = null }
            );
        }
    }
}
