// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace AutoStartConfirm.Models
{
    public class ThirdPartyLicenseEntry
    {
        public string? PackageId { get; set; }

        public string? PackageVersion { get; set; }

        public string? Authors { get; set; }

        public string? PackageProjectUrl { get; set; }

        public string? LicenseFile { get; set; }

        [JsonIgnore]
        public string? LicenseFilePath { get; set; }

        [JsonIgnore]
        public bool HasLicenseFile => !string.IsNullOrEmpty(LicenseFilePath);

        [JsonIgnore]
        public bool HasPackageProjectUrl => !string.IsNullOrEmpty(PackageProjectUrl);
    }
}
