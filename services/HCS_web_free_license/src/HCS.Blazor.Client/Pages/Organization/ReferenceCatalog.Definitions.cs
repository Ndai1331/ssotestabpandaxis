using System;
using HCS.Permissions;

namespace HCS.Blazor.Client.Pages.Organization;

public partial class ReferenceCatalog
{
    private ReferenceCatalogPageDefinition Definition => Kind switch
    {
        ReferenceCatalogKind.Icd10 => new(Kind, L["Catalog:ICD10Title"].Value, L["Catalog:ICD10Description"].Value, "fa fa-heart-pulse", HCSCatalogPermissions.Icd10),
        ReferenceCatalogKind.BloodPressure => new(Kind, L["Catalog:BloodPressureTitle"].Value, L["Catalog:BloodPressureDescription"].Value, "fa fa-heart-circle-plus", HCSCatalogPermissions.BloodPressure),
        ReferenceCatalogKind.BloodGlucose => new(Kind, L["Catalog:BloodGlucoseTitle"].Value, L["Catalog:BloodGlucoseDescription"].Value, "fa fa-droplet", HCSCatalogPermissions.BloodGlucose),
        ReferenceCatalogKind.Bmi => new(Kind, L["Catalog:BMITitle"].Value, L["Catalog:BMIDescription"].Value, "fa fa-weight-scale", HCSCatalogPermissions.Bmi),
        ReferenceCatalogKind.Country => new(Kind, L["Catalog:CountryTitle"].Value, L["Catalog:CountryDescription"].Value, "fa fa-earth-americas", HCSCatalogPermissions.Countries),
        ReferenceCatalogKind.Province => new(Kind, L["Catalog:ProvinceTitle"].Value, L["Catalog:ProvinceDescription"].Value, "fa fa-map-location-dot", HCSCatalogPermissions.Provinces),
        ReferenceCatalogKind.Commune => new(Kind, L["Catalog:CommuneTitle"].Value, L["Catalog:CommuneDescription"].Value, "fa fa-location-dot", HCSCatalogPermissions.Communes),
        _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, null)
    };

    private bool IsCodeCatalog => Kind is ReferenceCatalogKind.Icd10 or ReferenceCatalogKind.Country
        or ReferenceCatalogKind.Province or ReferenceCatalogKind.Commune;

    private bool IsLocationCatalog => Kind is ReferenceCatalogKind.Country or ReferenceCatalogKind.Province or ReferenceCatalogKind.Commune;
}
