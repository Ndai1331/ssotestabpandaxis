using System;
using System.Collections.Generic;

namespace HCS.OrganizationService.Domain;

public static class OrganizationConsts
{
    public const int MaxCodeLength = 50;
    public const int MaxNameLength = 256;
    public const int MaxTypeLength = 50;
    public const int MaxDiseaseGroupLength = 256;
    public const int MaxTitleLength = 256;
    public const int MaxDescriptionLength = 2000;
    public const int MaxGenderLength = 50;
    public const int MaxCountryCodeLength = 10;
    public const int MaxSortOrder = 10_000;
    public const int MaxSignOrder = 100;
    public const int MaxBloodPressureValue = 1000;
    public const decimal MaxMeasurementValue = 1_000_000m;

    public static readonly IReadOnlySet<string> AllowedMasterDataTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "DocumentType",
        "Sector",
        "UrgencyLevel",
        "ConfidentialityLevel",
        "ProcessingMethod",
        "DocumentStatus",
        "SigningMethod",
        "EventType"
    };
}

public static class OrganizationErrorCodes
{
    public const string DepartmentCannotBeOwnParent = "HCS.Organization:DepartmentCannotBeOwnParent";
    public const string DepartmentHierarchyCycle = "HCS.Organization:DepartmentHierarchyCycle";
    public const string DuplicateCode = "HCS.Organization:DuplicateCode";
    public const string InvalidDepartment = "HCS.Organization:InvalidDepartment";
    public const string InvalidMasterDataType = "HCS.Organization:InvalidMasterDataType";
    public const string UnitDepartmentMismatch = "HCS.Organization:UnitDepartmentMismatch";
    public const string MultiplePrimaryMappings = "HCS.Organization:MultiplePrimaryMappings";
    public const string DuplicateUserMapping = "HCS.Organization:DuplicateUserMapping";
    public const string InvalidRange = "HCS.Organization:InvalidRange";
    public const string InvalidCountry = "HCS.Organization:InvalidCountry";
    public const string InvalidProvince = "HCS.Organization:InvalidProvince";
    public const string ReferenceInUse = "HCS.Organization:ReferenceInUse";
}
