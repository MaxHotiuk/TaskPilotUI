namespace UI.Services;

public interface IPublicDomainService
{
    bool IsPublicDomain(string domain);
}

public class PublicDomainService : IPublicDomainService
{
    private static readonly HashSet<string> PublicDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        // Microsoft
        "outlook.com",
        "hotmail.com",
        "live.com",
        "msn.com",
        
        // Google
        "gmail.com",
        "googlemail.com",
        
        // Yahoo
        "yahoo.com",
        "yahoo.co.uk",
        "yahoo.fr",
        "yahoo.de",
        "ymail.com",
        
        // Other popular email providers
        "aol.com",
        "icloud.com",
        "me.com",
        "mac.com",
        "protonmail.com",
        "zoho.com",
        "gmx.com",
        "gmx.net",
        "mail.com",
        "tutanota.com",
        "fastmail.com",
        "hushmail.com",
        
        // Ukrainian providers
        "ukr.net",
        "i.ua",
        "meta.ua",
        "bigmir.net",
        
        // Polish providers
        "wp.pl",
        "o2.pl",
        "onet.pl",
        "interia.pl",
        
        // German providers
        "web.de",
        "t-online.de",
        "freenet.de",
        
        // French providers
        "free.fr",
        "orange.fr",
        "laposte.net",
        "wanadoo.fr",
        
        // Other
        "qq.com",
        "163.com",
        "126.com"
    };

    public bool IsPublicDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        var normalizedDomain = domain.Trim().ToLowerInvariant();
        return PublicDomains.Contains(normalizedDomain);
    }
}
