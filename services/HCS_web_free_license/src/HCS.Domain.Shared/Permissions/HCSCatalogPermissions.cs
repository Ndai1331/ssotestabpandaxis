namespace HCS.Permissions;

public static class HCSCatalogPermissions
{
    public const string Group = "HCS.Catalogs";
    public const string MasterData = Group + ".MasterData";
    public const string DocumentTypes = Group + ".DocumentTypes";
    public const string Sectors = Group + ".Sectors";
    public const string UrgencyLevels = Group + ".UrgencyLevels";
    public const string ConfidentialityLevels = Group + ".ConfidentialityLevels";
    public const string ProcessingMethods = Group + ".ProcessingMethods";
    public const string DocumentStatuses = Group + ".DocumentStatuses";
    public const string SigningMethods = Group + ".SigningMethods";
    public const string EventTypes = Group + ".EventTypes";
    public const string Icd10 = Group + ".ICD10";
    public const string BloodPressure = Group + ".BloodPressure";
    public const string BloodGlucose = Group + ".BloodGlucose";
    public const string Bmi = Group + ".BMI";
    public const string Countries = Group + ".Countries";
    public const string Provinces = Group + ".Provinces";
    public const string Communes = Group + ".Communes";

    public static readonly string[] All =
    [
        MasterData,
        DocumentTypes,
        Sectors,
        UrgencyLevels,
        ConfidentialityLevels,
        ProcessingMethods,
        DocumentStatuses,
        SigningMethods,
        EventTypes,
        Icd10,
        BloodPressure,
        BloodGlucose,
        Bmi,
        Countries,
        Provinces,
        Communes
    ];

    public static readonly string[] AllWithCrud = [.. HcsCrudPermissions.Expand(All)];

    public static string ForMasterType(string? type) => type switch
    {
        "DocumentType" => DocumentTypes,
        "Sector" => Sectors,
        "UrgencyLevel" => UrgencyLevels,
        "ConfidentialityLevel" => ConfidentialityLevels,
        "ProcessingMethod" => ProcessingMethods,
        "DocumentStatus" => DocumentStatuses,
        "SigningMethod" => SigningMethods,
        "EventType" => EventTypes,
        _ => MasterData
    };
}
