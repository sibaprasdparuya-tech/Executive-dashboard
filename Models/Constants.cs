namespace AcxiomCRM.Models;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string SalesExecutive = "SalesExecutive";
}

public static class CustomerStatuses
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Prospect = "Prospect";
}

public static class LeadStatuses
{
    public const string New = "New";
    public const string Contacted = "Contacted";
    public const string Qualified = "Qualified";
    public const string Unqualified = "Unqualified";
    public const string Converted = "Converted";
    public const string Lost = "Lost";

    public static readonly string[] All = [New, Contacted, Qualified, Unqualified, Converted, Lost];
}

public static class LeadSources
{
    public const string Website = "Website";
    public const string Referral = "Referral";
    public const string ColdCall = "Cold Call";
    public const string Advertisement = "Advertisement";
    public const string Partner = "Partner";
    public const string Other = "Other";

    public static readonly string[] All = [Website, Referral, ColdCall, Advertisement, Partner, Other];
}

public static class OpportunityStages
{
    public const string Qualification = "Qualification";
    public const string Proposal = "Proposal";
    public const string Negotiation = "Negotiation";
    public const string Won = "Won";
    public const string Lost = "Lost";

    public static readonly string[] All = [Qualification, Proposal, Negotiation, Won, Lost];
}

public static class OpportunityStatuses
{
    public const string Open = "Open";
    public const string Won = "Won";
    public const string Lost = "Lost";
}

public static class FollowUpTypes
{
    public const string Call = "Call";
    public const string Meeting = "Meeting";
    public const string Email = "Email";
    public const string Task = "Task";
    public const string Demo = "Demo";

    public static readonly string[] All = [Call, Meeting, Email, Task, Demo];
}

public static class FollowUpStatuses
{
    public const string Planned = "Planned";
    public const string Completed = "Completed";
    public const string Missed = "Missed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Planned, Completed, Missed, Cancelled];
}

public static class ActivityTypes
{
    public const string Call = "Call";
    public const string Meeting = "Meeting";
    public const string Email = "Email";
    public const string Task = "Task";

    public static readonly string[] All = [Call, Meeting, Email, Task];
}
