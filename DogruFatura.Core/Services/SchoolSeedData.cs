using DogruFatura.Core.Models;

namespace DogruFatura.Core.Services;

public static class SchoolSeedData
{
#if DOGRUFATURA_CLEAN_DISTRIBUTION
    public static IReadOnlyList<SchoolRecord> Create()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "InitialData", "Okullar.json");
        if (!File.Exists(path)) return Array.Empty<SchoolRecord>();
        var schema = DataFileSchemaService.Read(File.ReadAllText(path), DataFileKinds.Schools);
        return System.Text.Json.JsonSerializer.Deserialize<List<SchoolRecord>>(schema.Data.GetRawText())?
            .Select(SchoolValidationService.Normalize).ToArray() ?? Array.Empty<SchoolRecord>();
    }
#else
    public static IReadOnlyList<SchoolRecord> Create() => new List<SchoolRecord>
    {
        new("İlkokul", "Cumhuriyet İlkokulu", "2160367474", "00001121370", "2766181015", "8847516331", "00000180"),
        new("İlkokul", "Recep Dinç İlkokulu", "7710242642", "00001107990", "", "", ""),
        new("İlkokul", "Yayalar İlkokulu", "7710242619", "00001504930", "2766586511", "1864027813", ""),
        new("İlkokul", "Atatürk İlkokulu", "0990318581", "00001028900", "2766181661", "1864001380", "00001923"),
        new("İlkokul", "Ağaçbeyli İlkokulu", "0090366221", "00004003970", "2766287011", "1864018311", ""),
        new("İlkokul", "Cinoğlu İlkokulu Eski", "2110670582", "00002401120", "2766848357", "", ""),
        new("İlkokul", "Cinoğlu İlkokulu Yeni", "2110670582", "00001824967", "", "", ""),
        new("İlkokul", "Avu. Nurettin-Meliha Akarsu İlkokulu", "6320233558", "", "", "", ""),
        new("İlkokul", "Muammer Yazgan İlkokulu", "7710208917", "00001036750", "2766183474", "1864001383", "00001615"),
        new("İlkokul", "Mevlüt Kara İlkokulu", "7710242723", "00002604070", "2766848359", "", ""),
        new("İlkokul", "Samatlar İlkokulu", "7420435774", "00001801380", "2766386149", "1864062475", ""),
        new("İlkokul", "Erice İlkokulu", "3590325154", "00002201830", "2766412253", "1864051242", ""),
        new("İlkokul", "Erice İlkokulu Lojman", "3590325154", "00002200440", "", "", ""),
        new("İlkokul", "Hanoğlu İlkokulu", "4571340205", "00002700660", "", "", ""),
        new("İlkokul", "Budaklar İlkokulu", "1880503884", "00002501380", "2766386247", "1864035603", ""),
        new("Ortaokul", "31 Ağustos Ortaokulu", "7710327776", "00001036830", "2766181022", "1864001379", "00000123"),
        new("Ortaokul", "Cumhuriyet Ortaokulu", "7710327662", "00001049250", "2766183507", "1864092603", "00000000"),
        new("Ortaokul", "Recep Dinç Ortaokulu", "7710327670", "00001107170", "2766181748", "1864001384", "00000150"),
        new("Ortaokul", "Yayalar Ortaokulu", "7710327867", "00001506920", "2766586761", "", ""),
        new("Ortaokul", "Şehit Ruhi Çetin Ortaokulu", "7710327800", "00001601770", "2766386056", "8851932065", "67110133"),
        new("Ortaokul", "Ağaçbeyli Ortaokulu", "7710327719", "00004002490", "2766287011", "", ""),
        new("Ortaokul", "Avu. Nurettin-Meliha Akarsu Ortaokulu", "1050582135", "00001204690", "2766612035", "1864013706", "00000356"),
        new("Ortaokul", "Tatar Ortaokulu", "7710242597", "00000012330", "2766485005", "1864011828", "00000831"),
        new("Anasınıfı/Anaokulu", "Recep Dinç Anaokulu", "7710327670", "00011012710", "", "", ""),
        new("Anasınıfı/Anaokulu", "Ağaçbeyli Anasınıfı", "", "00004001720", "", "", ""),
        new("Anasınıfı/Anaokulu", "Budaklar Ana Okulu", "1880503884", "00002500840", "", "", ""),
        new("Kurum", "Sivaslı İlçe Milli Eğitim Müdürlüğü", "4710101139", "", "2766181007", "8821991202", "")
    }.Select(SchoolValidationService.Normalize).ToArray();
#endif
}
