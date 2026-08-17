namespace hanhchinhso.OrganizationService.Permissions;

public static class OrganizationServicePermissions
{
    public const string GroupName = "HanhChinhSo.Organization";
    public const string MasterData = GroupName + ".MasterData";
    public const string Create = MasterData + ".Create";
    public const string Update = MasterData + ".Update";
    public const string Delete = MasterData + ".Delete";

    public static class Units
    {
        public const string Default = GroupName + ".Units";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class Positions
    {
        public const string Default = GroupName + ".Positions";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

}
