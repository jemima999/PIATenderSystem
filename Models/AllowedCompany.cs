using System.Collections.Generic;

namespace PIATenderSystem.Models
{
    public static class AllowedCompanies
    {
        public static readonly Dictionary<string, string> Companies = new Dictionary<string, string>
        {
            { "Sky Aviation Services", "skyaviation.com" },
            { "National Logistics Corp", "nlc.com" },
            { "Falcon Engineering", "falconeng.com" },
            { "Al-Habib Traders", "alhabibtraders.com" },
            { "Zenith Contractors", "zenithcontractors.com" }
        };
    }
}