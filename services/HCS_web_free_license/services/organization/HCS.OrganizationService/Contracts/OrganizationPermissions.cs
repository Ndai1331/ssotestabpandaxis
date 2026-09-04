namespace HCS.OrganizationService.Contracts;

public static class OrganizationPermissions
{
    public const string Group = HCS.Permissions.HCSOrganizationPermissions.Group;
    public const string Departments = HCS.Permissions.HCSOrganizationPermissions.Departments;
    public const string Units = HCS.Permissions.HCSOrganizationPermissions.Units;
    public const string Positions = HCS.Permissions.HCSOrganizationPermissions.Positions;
    public const string MasterData = HCS.Permissions.HCSOrganizationPermissions.MasterData;
    public const string UserMappings = HCS.Permissions.HCSOrganizationPermissions.UserMappings;
    public const string Icd10 = HCS.Permissions.HCSCatalogPermissions.Icd10;
    public const string BloodPressure = HCS.Permissions.HCSCatalogPermissions.BloodPressure;
    public const string BloodGlucose = HCS.Permissions.HCSCatalogPermissions.BloodGlucose;
    public const string Bmi = HCS.Permissions.HCSCatalogPermissions.Bmi;
    public const string Countries = HCS.Permissions.HCSCatalogPermissions.Countries;
    public const string Provinces = HCS.Permissions.HCSCatalogPermissions.Provinces;
    public const string Communes = HCS.Permissions.HCSCatalogPermissions.Communes;

    public static readonly string[] MasterDataAccess =
    [
        MasterData,
        ..HCS.Permissions.HCSCatalogPermissions.AllWithCrud
    ];
}
