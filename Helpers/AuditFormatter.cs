using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IT_Form.Helpers
{
    public static class AuditFormatter
    {
        public static string FormatChanges(string? changesJson)
        {
            if (string.IsNullOrWhiteSpace(changesJson)) return "No data changes recorded.";
            
            // If it doesn't look like JSON, it's already plain text
            var trimmed = changesJson.TrimStart();
            if (!trimmed.StartsWith("{") && !trimmed.StartsWith("[")) 
                return changesJson;

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var changesDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(changesJson, options);
                if (changesDict == null || !changesDict.Any()) 
                    return "Administrative update.";

                var sb = new StringBuilder();
                
                foreach (var kvp in changesDict)
                {
                    var propertyName = kvp.Key;
                    
                    // Skip technical or very verbose fields
                    if (propertyName.Contains("Hmac", StringComparison.OrdinalIgnoreCase) || 
                        propertyName.Contains("Signature", StringComparison.OrdinalIgnoreCase) || 
                        propertyName == "Discriminator" || 
                        propertyName == "Id" || 
                        propertyName == "ConcurrencyStamp" ||
                        propertyName == "PasswordHash" ||
                        propertyName == "SecurityStamp" ||
                        propertyName == "AccessFailedCount" ||
                        propertyName == "LockoutCount" ||
                        propertyName == "LockoutEnabled" ||
                        propertyName == "LockoutEnd" ||
                        propertyName == "NormalizedEmail" ||
                        propertyName == "NormalizedUserName" ||
                        propertyName == "EmailConfirmed" ||
                        propertyName == "PhoneNumberConfirmed" ||
                        propertyName == "TwoFactorEnabled" ||
                        propertyName == "MustChangePassword" ||
                        propertyName == "LastLockoutAlertSentAt") continue;

                    var friendlyName = GetFriendlyName(propertyName);
                    var value = kvp.Value;

                    if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("From", out var from) && value.TryGetProperty("To", out var to))
                    {
                        var fromStr = GetFriendlyValue(propertyName, from);
                        var toStr = GetFriendlyValue(propertyName, to);

                        // Special case for dates (especially signatures)
                        if (propertyName.EndsWith("SignedAt", StringComparison.OrdinalIgnoreCase) || propertyName.EndsWith("ConsentedAt", StringComparison.OrdinalIgnoreCase))
                        {
                            if (from.ValueKind == JsonValueKind.Null && to.ValueKind != JsonValueKind.Null)
                            {
                                sb.AppendLine($"• {friendlyName} completed at {toStr}");
                            }
                            else if (to.ValueKind == JsonValueKind.Null && from.ValueKind != JsonValueKind.Null)
                            {
                                sb.AppendLine($"• {friendlyName} was removed/resent.");
                            }
                        }
                        // Only show if the data actually changed
                        if (fromStr != toStr)
                        {
                            sb.AppendLine($"• {friendlyName} changed from '{fromStr}' to '{toStr}'");
                        }
                    }
                    else
                    {
                        var valStr = GetFriendlyValue(propertyName, value);
                        sb.AppendLine($"• {friendlyName}: {valStr}");
                    }
                }

                if (sb.Length == 0)
                {
                    // Fallback for signing actions - only if the signature was actually added (changing from null)
                    foreach (var sigKey in new[] { "RequesterSignedAt", "HRSignedAt", "HODSignedAt", "ITAdminSignedAt", "EmployeeSignedAt", "PdpaConsentedAt" })
                    {
                        if (changesDict.TryGetValue(sigKey, out var val) && val.ValueKind == JsonValueKind.Object)
                        {
                            if (val.TryGetProperty("From", out var from) && from.ValueKind == JsonValueKind.Null &&
                                val.TryGetProperty("To", out var to) && to.ValueKind != JsonValueKind.Null)
                            {
                                return sigKey switch {
                                    "RequesterSignedAt" => "Signature completed by HR / Requester.",
                                    "HRSignedAt" => "Signature completed by HR.",
                                    "HODSignedAt" => "Signature completed by HOD.",
                                    "ITAdminSignedAt" => "Signature completed by IT Admin.",
                                    "EmployeeSignedAt" => "Signature completed by Employee.",
                                    "PdpaConsentedAt" => "PDPA Consent Signature completed.",
                                    _ => "Signature completed."
                                };
                            }
                        }
                    }
                    
                    return "Administrative update.";
                }

                return sb.ToString().Trim();
            }
            catch
            {
                return changesJson;
            }
        }

        private static string GetFriendlyName(string name)
        {
            return name switch
            {
                "FullName" => "Full Name",
                "RequesterSignedAt" => "HR / Requester Signature",
                "EmployeeSignedAt" => "Employee Signature",
                "ITAdminSignedAt" => "IT Admin Signature",
                "HRSignedAt" => "HR Signature",
                "HODSignedAt" => "HOD Signature",
                "PdpaConsentedAt" => "PDPA Consent",
                "ReportingManagerId" => "Reporting Manager",
                "MachineName" => "Device Name",
                "SoftwareName" => "Software Name",
                "AssetTag" => "Asset Tag",
                _ => SplitCamelCase(name)
            };
        }

        private static string GetFriendlyValue(string name, JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Null) return "none";
            
            if (name == "Status")
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var statusId))
                {
                    return statusId switch {
                        0 => "Pending IT",
                        1 => "Pending HR",
                        2 => "Rejected",
                        3 => "In Progress",
                        5 => "Completed",
                        7 => "Pending Employee Signature",
                        _ => statusId.ToString()
                    };
                }
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                var str = value.GetString();
                if (DateTime.TryParse(str, out var dt))
                    return dt.ToLocalTime().ToString("dd MMM yyyy, hh:mm tt");
                
                return str ?? "none";
            }

            return value.ToString();
        }

        public static string PrettyPrintJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return string.Empty;
            try
            {
                using var jsonDoc = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return json ?? string.Empty;
            }
        }

        private static string SplitCamelCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            // Adds space before each capital letter except the first one
            return Regex.Replace(input, "([a-z])([A-Z])", "$1 $2", RegexOptions.Compiled);
        }
    }
}
