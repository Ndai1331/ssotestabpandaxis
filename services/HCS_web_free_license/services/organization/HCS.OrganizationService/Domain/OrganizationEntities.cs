using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace HCS.OrganizationService.Domain;

public abstract class CodedAggregate : AuditedAggregateRoot<Guid>
{
    public string Code { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public int SortOrder { get; protected set; }
    public bool IsActive { get; protected set; }

    protected CodedAggregate() { }

    protected CodedAggregate(Guid id) : base(id) { }

    protected void SetCommon(string code, string name, int sortOrder, bool isActive)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), OrganizationConsts.MaxCodeLength).Trim();
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), OrganizationConsts.MaxNameLength).Trim();
        if (sortOrder is < 0 or > OrganizationConsts.MaxSortOrder)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), sortOrder,
                $"Sort order must be between 0 and {OrganizationConsts.MaxSortOrder}.");
        }

        SortOrder = sortOrder;
        IsActive = isActive;
    }
}

public sealed class Department : CodedAggregate
{
    public Guid? ParentId { get; private set; }

    private Department() { }

    public Department(Guid id, string code, string name, Guid? parentId, int sortOrder, bool isActive = true)
        : base(id) => Update(code, name, parentId, sortOrder, isActive);

    public void Update(string code, string name, Guid? parentId, int sortOrder, bool isActive)
    {
        if (parentId == Id)
        {
            throw new BusinessException(OrganizationErrorCodes.DepartmentCannotBeOwnParent);
        }

        SetCommon(code, name, sortOrder, isActive);
        ParentId = parentId;
    }
}

public sealed class Unit : CodedAggregate
{
    public Guid DepartmentId { get; private set; }

    private Unit() { }

    public Unit(Guid id, Guid departmentId, string code, string name, int sortOrder, bool isActive = true)
        : base(id) => Update(departmentId, code, name, sortOrder, isActive);

    public void Update(Guid departmentId, string code, string name, int sortOrder, bool isActive)
    {
        DepartmentId = Check.NotDefaultOrNull<Guid>(departmentId, nameof(departmentId));
        SetCommon(code, name, sortOrder, isActive);
    }
}

public sealed class Position : CodedAggregate
{
    public int SignOrder { get; private set; }

    private Position() { }

    public Position(Guid id, string code, string name, int signOrder, int sortOrder, bool isActive = true)
        : base(id) => Update(code, name, signOrder, sortOrder, isActive);

    public void Update(string code, string name, int signOrder, int sortOrder, bool isActive)
    {
        if (signOrder is < 0 or > OrganizationConsts.MaxSignOrder)
        {
            throw new ArgumentOutOfRangeException(nameof(signOrder), signOrder,
                $"Sign order must be between 0 and {OrganizationConsts.MaxSignOrder}.");
        }

        SetCommon(code, name, sortOrder, isActive);
        SignOrder = signOrder;
    }
}

public sealed class MasterDataItem : CodedAggregate
{
    public string Type { get; private set; } = string.Empty;

    private MasterDataItem() { }

    public MasterDataItem(Guid id, string type, string code, string name, int sortOrder, bool isActive = true)
        : base(id) => Update(type, code, name, sortOrder, isActive);

    public void Update(string type, string code, string name, int sortOrder, bool isActive)
    {
        Type = Check.NotNullOrWhiteSpace(type, nameof(type), OrganizationConsts.MaxTypeLength).Trim();
        SetCommon(code, name, sortOrder, isActive);
    }
}

public abstract class OrderedReferenceAggregate : AuditedAggregateRoot<Guid>
{
    public int SortOrder { get; protected set; }

    protected OrderedReferenceAggregate() { }

    protected OrderedReferenceAggregate(Guid id) : base(id) { }

    protected void SetSortOrder(int sortOrder)
    {
        if (sortOrder is < 0 or > OrganizationConsts.MaxSortOrder)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), sortOrder,
                $"Sort order must be between 0 and {OrganizationConsts.MaxSortOrder}.");
        }

        SortOrder = sortOrder;
    }
}

public abstract class CodedReferenceAggregate : OrderedReferenceAggregate
{
    public string Code { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;

    protected CodedReferenceAggregate() { }

    protected CodedReferenceAggregate(Guid id) : base(id) { }

    protected void SetCodeName(string code, string name)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), OrganizationConsts.MaxCodeLength).Trim();
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), OrganizationConsts.MaxNameLength).Trim();
    }
}

public abstract class TitledReferenceAggregate : OrderedReferenceAggregate
{
    public string Title { get; protected set; } = string.Empty;
    public string Description { get; protected set; } = string.Empty;

    protected TitledReferenceAggregate() { }

    protected TitledReferenceAggregate(Guid id) : base(id) { }

    protected void SetTitleDescription(string title, string? description)
    {
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), OrganizationConsts.MaxTitleLength).Trim();
        Description = (description ?? string.Empty).Trim();
        if (Description.Length > OrganizationConsts.MaxDescriptionLength)
        {
            throw new ArgumentException(
                $"Description must not exceed {OrganizationConsts.MaxDescriptionLength} characters.", nameof(description));
        }
    }
}

public sealed class Icd10 : CodedReferenceAggregate
{
    public string DiseaseGroup { get; private set; } = string.Empty;
    public bool IsChronic { get; private set; }

    private Icd10() { }

    public Icd10(Guid id, string code, string name, string diseaseGroup, bool isChronic, int sortOrder)
        : base(id) => Update(code, name, diseaseGroup, isChronic, sortOrder);

    public void Update(string code, string name, string diseaseGroup, bool isChronic, int sortOrder)
    {
        SetCodeName(code, name);
        DiseaseGroup = Check.NotNullOrWhiteSpace(diseaseGroup, nameof(diseaseGroup), OrganizationConsts.MaxDiseaseGroupLength).Trim();
        IsChronic = isChronic;
        SetSortOrder(sortOrder);
    }
}

public sealed class BloodPressureRange : TitledReferenceAggregate
{
    public int HATTMin { get; private set; }
    public int HATTMax { get; private set; }
    public int HATTrMin { get; private set; }
    public int HATTrMax { get; private set; }

    private BloodPressureRange() { }

    public BloodPressureRange(Guid id, int hattMin, int hattMax, int hatTrMin, int hatTrMax,
        string title, string? description, int sortOrder) : base(id)
        => Update(hattMin, hattMax, hatTrMin, hatTrMax, title, description, sortOrder);

    public void Update(int hattMin, int hattMax, int hatTrMin, int hatTrMax,
        string title, string? description, int sortOrder)
    {
        EnsureRange(hattMin, hattMax, nameof(hattMin), nameof(hattMax));
        EnsureRange(hatTrMin, hatTrMax, nameof(hatTrMin), nameof(hatTrMax));
        HATTMin = hattMin;
        HATTMax = hattMax;
        HATTrMin = hatTrMin;
        HATTrMax = hatTrMax;
        SetTitleDescription(title, description);
        SetSortOrder(sortOrder);
    }

    private static void EnsureRange(int min, int max, string minName, string maxName)
    {
        if (min < 0 || max < 0 || min > OrganizationConsts.MaxBloodPressureValue
            || max > OrganizationConsts.MaxBloodPressureValue || min > max)
        {
            throw new BusinessException(OrganizationErrorCodes.InvalidRange)
                .WithData("MinField", minName).WithData("MaxField", maxName);
        }
    }
}

public sealed class BloodGlucoseRange : TitledReferenceAggregate
{
    public decimal MinValue { get; private set; }
    public decimal MaxValue { get; private set; }
    public bool BeforeMeal { get; private set; }

    private BloodGlucoseRange() { }

    public BloodGlucoseRange(Guid id, string title, decimal minValue, decimal maxValue,
        string? description, bool beforeMeal, int sortOrder) : base(id)
        => Update(title, minValue, maxValue, description, beforeMeal, sortOrder);

    public void Update(string title, decimal minValue, decimal maxValue, string? description,
        bool beforeMeal, int sortOrder)
    {
        EnsureMeasurementRange(minValue, maxValue);
        MinValue = minValue;
        MaxValue = maxValue;
        BeforeMeal = beforeMeal;
        SetTitleDescription(title, description);
        SetSortOrder(sortOrder);
    }

    private static void EnsureMeasurementRange(decimal minValue, decimal maxValue)
    {
        if (minValue is < 0 or > OrganizationConsts.MaxMeasurementValue
            || maxValue is < 0 or > OrganizationConsts.MaxMeasurementValue || minValue > maxValue)
        {
            throw new BusinessException(OrganizationErrorCodes.InvalidRange);
        }
    }
}

public sealed class BmiRange : TitledReferenceAggregate
{
    public string Gender { get; private set; } = string.Empty;
    public decimal MinValue { get; private set; }
    public decimal MaxValue { get; private set; }

    private BmiRange() { }

    public BmiRange(Guid id, string title, string gender, decimal minValue, decimal maxValue,
        string? description, int sortOrder) : base(id)
        => Update(title, gender, minValue, maxValue, description, sortOrder);

    public void Update(string title, string gender, decimal minValue, decimal maxValue,
        string? description, int sortOrder)
    {
        EnsureMeasurementRange(minValue, maxValue);
        Gender = Check.NotNullOrWhiteSpace(gender, nameof(gender), OrganizationConsts.MaxGenderLength).Trim();
        MinValue = minValue;
        MaxValue = maxValue;
        SetTitleDescription(title, description);
        SetSortOrder(sortOrder);
    }

    private static void EnsureMeasurementRange(decimal minValue, decimal maxValue)
    {
        if (minValue is < 0 or > OrganizationConsts.MaxMeasurementValue
            || maxValue is < 0 or > OrganizationConsts.MaxMeasurementValue || minValue > maxValue)
        {
            throw new BusinessException(OrganizationErrorCodes.InvalidRange);
        }
    }
}

public sealed class Country : CodedReferenceAggregate
{
    public string CountryCode { get; private set; } = string.Empty;

    private Country() { }

    public Country(Guid id, string code, string name, string countryCode, int sortOrder)
        : base(id) => Update(code, name, countryCode, sortOrder);

    public void Update(string code, string name, string countryCode, int sortOrder)
    {
        SetCodeName(code, name);
        CountryCode = Check.NotNullOrWhiteSpace(countryCode, nameof(countryCode), OrganizationConsts.MaxCountryCodeLength).Trim();
        SetSortOrder(sortOrder);
    }
}

public sealed class Province : CodedReferenceAggregate
{
    public Guid CountryId { get; private set; }

    private Province() { }

    public Province(Guid id, string code, string name, Guid countryId, int sortOrder)
        : base(id) => Update(code, name, countryId, sortOrder);

    public void Update(string code, string name, Guid countryId, int sortOrder)
    {
        CountryId = Check.NotDefaultOrNull<Guid>(countryId, nameof(countryId));
        SetCodeName(code, name);
        SetSortOrder(sortOrder);
    }
}

public sealed class Commune : CodedReferenceAggregate
{
    public Guid ProvinceId { get; private set; }

    private Commune() { }

    public Commune(Guid id, string code, string name, Guid provinceId, int sortOrder)
        : base(id) => Update(code, name, provinceId, sortOrder);

    public void Update(string code, string name, Guid provinceId, int sortOrder)
    {
        ProvinceId = Check.NotDefaultOrNull<Guid>(provinceId, nameof(provinceId));
        SetCodeName(code, name);
        SetSortOrder(sortOrder);
    }
}

public sealed class UserOrganizationMapping : AuditedAggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public Guid? UnitId { get; private set; }
    public Guid? PositionId { get; private set; }
    public bool IsPrimary { get; private set; }

    private UserOrganizationMapping() { }

    public UserOrganizationMapping(
        Guid id, Guid userId, Guid departmentId, Guid? unitId, Guid? positionId, bool isPrimary) : base(id)
        => Update(userId, departmentId, unitId, positionId, isPrimary);

    public void Update(Guid userId, Guid departmentId, Guid? unitId, Guid? positionId, bool isPrimary)
    {
        UserId = Check.NotDefaultOrNull<Guid>(userId, nameof(userId));
        DepartmentId = Check.NotDefaultOrNull<Guid>(departmentId, nameof(departmentId));
        UnitId = unitId;
        PositionId = positionId;
        IsPrimary = isPrimary;
    }
}
