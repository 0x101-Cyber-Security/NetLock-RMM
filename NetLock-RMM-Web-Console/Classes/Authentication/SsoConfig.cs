namespace NetLock_RMM_Web_Console.Classes.Authentication
{
    /// <summary>
    /// Configuration models for SSO providers
    /// </summary>
    
    public class SsoConfig
    {
        public bool Enabled { get; set; }
        public AzureAdConfig? AzureAd { get; set; }
        public KeycloakConfig? Keycloak { get; set; }
        public GoogleIdentityConfig? GoogleIdentity { get; set; }
        public OktaConfig? Okta { get; set; }
        public Auth0Config? Auth0 { get; set; }
    }
    
    public class AzureAdConfig
    {
        public bool Enabled { get; set; }
        public string Instance { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string CallbackPath { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string SignedOutCallbackPath { get; set; } = string.Empty;
        public string ResponseType { get; set; } = string.Empty;
        public bool SaveTokens { get; set; }
    }
    
    public class KeycloakConfig
    {
        public bool Enabled { get; set; }
        public string Authority { get; set; } = string.Empty;
        public string Realm { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string CallbackPath { get; set; } = string.Empty;
        public string SignedOutCallbackPath { get; set; } = string.Empty;
        public string ResponseType { get; set; } = string.Empty;
        public bool SaveTokens { get; set; }
        public bool GetClaimsFromUserInfoEndpoint { get; set; }
        public bool RequireHttpsMetadata { get; set; }
    }
    
    public class GoogleIdentityConfig
    {
        public bool Enabled { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string CallbackPath { get; set; } = string.Empty;
        public string SignedOutCallbackPath { get; set; } = string.Empty;
        public bool SaveTokens { get; set; }
        public string? HostedDomain { get; set; } // Optional: Restrict to specific Google Workspace domain
    }
    
    public class OktaConfig
    {
        public bool Enabled { get; set; }
        public string Domain { get; set; } = string.Empty; // e.g., dev-12345.okta.com
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string CallbackPath { get; set; } = string.Empty;
        public string SignedOutCallbackPath { get; set; } = string.Empty;
        public bool SaveTokens { get; set; }
        public bool GetClaimsFromUserInfoEndpoint { get; set; }
        public string? AuthorizationServerId { get; set; } // Optional: default or custom authorization server ID
    }

    public class Auth0Config
    {
        public bool Enabled { get; set; }
        public string Domain { get; set; } = string.Empty; // e.g., your-tenant.auth0.com or your-tenant.eu.auth0.com
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string CallbackPath { get; set; } = string.Empty;
        public string SignedOutCallbackPath { get; set; } = string.Empty;
        public bool SaveTokens { get; set; }
        public string? Audience { get; set; } // Optional: API Identifier for custom API
    }
}

