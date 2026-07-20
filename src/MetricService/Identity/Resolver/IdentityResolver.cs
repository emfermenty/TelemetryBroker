namespace MetricService.Identity.Resolver;

public static class IdentityResolver
{
    public static bool TryResolve(OpenTelemetry.Proto.Resource.V1.Resource? resource, out ModuleIdentity identity)
    {
        var serviceName = FindAttribute(resource, ResourceAttributeKeys.ServiceName);
        if (string.IsNullOrEmpty(serviceName))
        {
            identity = default;
            return false;
        }

        var licenseName = FindAttribute(resource, ResourceAttributeKeys.LicenseName);
        var kind = string.IsNullOrEmpty(licenseName) ? "hwid" : "license";

        identity = new ModuleIdentity(kind, serviceName);
        return true;
    }
    private static string? FindAttribute(OpenTelemetry.Proto.Resource.V1.Resource? resource, string key)
    {
        if (resource == null)
        {
            return null;
        }
        foreach (var attribute in resource.Attributes)
        {
            if (attribute.Key == key && attribute.Value.HasStringValue)
            {
                return attribute.Value.StringValue;
            }
        }
        return null;
    }
}
