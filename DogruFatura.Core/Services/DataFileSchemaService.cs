using System.Text.Json;

namespace DogruFatura.Core.Services;

public static class DataFileKinds
{
    public const string ActiveInvoices = "active-invoices";
    public const string Schools = "schools";
    public const string AuthorityProfile = "authority-profile";
    public const string InvoiceArchive = "invoice-archive";
}

public sealed record DataFileReadResult(
    JsonElement Data,
    int SourceSchemaVersion,
    bool RequiresUpgrade);

/// <summary>
/// Kalıcı JSON dosyalarının şema sürümünü tek noktadan yönetir.
/// Şema 0, DoğruFatura 1.0.x sürümlerindeki zarf içermeyen eski JSON biçimidir.
/// Şema 1, SchemaVersion/DataType/Data zarfını kullanır.
/// </summary>
public static class DataFileSchemaService
{
    public const int LegacySchemaVersion = 0;
    public const int CurrentSchemaVersion = 1;

    public static DataFileReadResult Read(string json, string expectedDataType)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedDataType);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!TryReadEnvelope(root, out var schemaVersion, out var dataType, out var payload))
        {
            var migratedLegacy = ApplyMigrations(LegacySchemaVersion, root.Clone());
            return new DataFileReadResult(
                migratedLegacy.Data,
                LegacySchemaVersion,
                RequiresUpgrade: true);
        }

        if (!string.Equals(dataType, expectedDataType, StringComparison.Ordinal))
            throw new JsonException($"Veri dosyası türü beklenen türle eşleşmiyor. Beklenen: {expectedDataType}, bulunan: {dataType}.");

        if (schemaVersion < LegacySchemaVersion)
            throw new JsonException($"Geçersiz veri şema sürümü: {schemaVersion}.");

        if (schemaVersion > CurrentSchemaVersion)
            throw new JsonException(
                $"Bu veri dosyası daha yeni bir DoğruFatura sürümüyle oluşturulmuş (şema {schemaVersion}). "
                + $"Bu sürüm en fazla şema {CurrentSchemaVersion} okuyabilir.");

        var migrated = ApplyMigrations(schemaVersion, payload.Clone());
        return new DataFileReadResult(
            migrated.Data,
            schemaVersion,
            RequiresUpgrade: schemaVersion != CurrentSchemaVersion);
    }

    public static string SerializeCurrent<T>(
        string dataType,
        T data,
        JsonSerializerOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataType);
        ArgumentNullException.ThrowIfNull(options);

        var envelope = new DataFileEnvelope<T>(
            CurrentSchemaVersion,
            dataType,
            data);
        return JsonSerializer.Serialize(envelope, options);
    }

    private static MigrationResult ApplyMigrations(int sourceVersion, JsonElement data)
    {
        var version = sourceVersion;
        var currentData = data;

        while (version < CurrentSchemaVersion)
        {
            (version, currentData) = version switch
            {
                // 1.0.x dosyaları zarf içermiyordu. V0 -> V1 geçişinde iş verisi
                // değişmez; yalnızca sonraki Save işleminde sürümlü zarf eklenir.
                0 => (1, currentData),
                _ => throw new JsonException($"Şema {version} için tanımlı migration adımı bulunamadı.")
            };
        }

        return new MigrationResult(version, currentData);
    }

    private static bool TryReadEnvelope(
        JsonElement root,
        out int schemaVersion,
        out string dataType,
        out JsonElement data)
    {
        schemaVersion = LegacySchemaVersion;
        dataType = string.Empty;
        data = default;

        if (root.ValueKind != JsonValueKind.Object
            || !TryGetPropertyIgnoreCase(root, "SchemaVersion", out var schemaProperty))
            return false;

        if (schemaProperty.ValueKind != JsonValueKind.Number
            || !schemaProperty.TryGetInt32(out schemaVersion))
            throw new JsonException("SchemaVersion alanı geçerli bir tam sayı olmalıdır.");

        if (!TryGetPropertyIgnoreCase(root, "DataType", out var typeProperty)
            || typeProperty.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(typeProperty.GetString()))
            throw new JsonException("Sürümlü veri dosyasında DataType alanı eksik veya geçersiz.");

        if (!TryGetPropertyIgnoreCase(root, "Data", out var dataProperty))
            throw new JsonException("Sürümlü veri dosyasında Data alanı eksik.");

        dataType = typeProperty.GetString()!;
        data = dataProperty;
        return true;
    }

    private static bool TryGetPropertyIgnoreCase(
        JsonElement element,
        string propertyName,
        out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                continue;

            value = property.Value;
            return true;
        }

        value = default;
        return false;
    }

    private sealed record DataFileEnvelope<T>(
        int SchemaVersion,
        string DataType,
        T Data);

    private sealed record MigrationResult(
        int SchemaVersion,
        JsonElement Data);
}
