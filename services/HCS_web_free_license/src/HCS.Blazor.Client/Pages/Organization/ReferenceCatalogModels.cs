using System;
using System.Collections.Generic;

namespace HCS.Blazor.Client.Pages.Organization;

public enum ReferenceCatalogKind
{
    Icd10,
    BloodPressure,
    BloodGlucose,
    Bmi,
    Country,
    Province,
    Commune
}

public sealed record ReferenceCatalogPageDefinition(
    ReferenceCatalogKind Kind,
    string Title,
    string Description,
    string Icon,
    string Permission);

public sealed record ReferenceCatalogQuery(string? Filter, int SkipCount, int MaxResultCount);

public sealed record Icd10CatalogDto(Guid Id, string Code, string Name, string DiseaseGroup, bool IsChronic, int SortOrder);
public sealed record BloodPressureCatalogDto(Guid Id, int HATTMin, int HATTMax, int HATTrMin, int HATTrMax,
    string Title, string Description, int SortOrder);
public sealed record BloodGlucoseCatalogDto(Guid Id, string Title, decimal MinValue, decimal MaxValue,
    string Description, bool BeforeMeal, int SortOrder);
public sealed record BmiCatalogDto(Guid Id, string Title, string Gender, decimal MinValue, decimal MaxValue,
    string Description, int SortOrder);
public sealed record CountryCatalogDto(Guid Id, string Code, string Name, string CountryCode, int SortOrder);
public sealed record ProvinceCatalogDto(Guid Id, string Code, string Name, Guid CountryId, string CountryCode, int SortOrder);
public sealed record CommuneCatalogDto(Guid Id, string Code, string Name, Guid ProvinceId, string ProvinceCode, int SortOrder);

public sealed record Icd10UpsertRequest(string Code, string Name, string DiseaseGroup, bool IsChronic, int SortOrder);
public sealed record BloodPressureUpsertRequest(int HATTMin, int HATTMax, int HATTrMin, int HATTrMax,
    string Title, string? Description, int SortOrder);
public sealed record BloodGlucoseUpsertRequest(string Title, decimal MinValue, decimal MaxValue,
    string? Description, bool BeforeMeal, int SortOrder);
public sealed record BmiUpsertRequest(string Title, string Gender, decimal MinValue, decimal MaxValue,
    string? Description, int SortOrder);
public sealed record CountryUpsertRequest(string Code, string Name, string CountryCode, int SortOrder);
public sealed record ProvinceUpsertRequest(string Code, string Name, Guid CountryId, int SortOrder);
public sealed record CommuneUpsertRequest(string Code, string Name, Guid ProvinceId, int SortOrder);

public sealed class ReferenceCatalogFormModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DiseaseGroup { get; set; } = string.Empty;
    public bool IsChronic { get; set; }

    public int HATTMin { get; set; }
    public int HATTMax { get; set; }
    public int HATTrMin { get; set; }
    public int HATTrMax { get; set; }

    public string Title { get; set; } = string.Empty;
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool BeforeMeal { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string ParentId { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed record ReferenceCatalogRow(
    Guid Id,
    string Code,
    string Name,
    string DiseaseGroup,
    bool IsChronic,
    int HATTMin,
    int HATTMax,
    int HATTrMin,
    int HATTrMax,
    string Title,
    decimal MinValue,
    decimal MaxValue,
    string Description,
    bool BeforeMeal,
    string Gender,
    Guid? ParentId,
    string ParentCode,
    string CountryCode,
    int SortOrder);
