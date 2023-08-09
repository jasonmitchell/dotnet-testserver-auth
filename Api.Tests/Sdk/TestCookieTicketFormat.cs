using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Api.Tests.Sdk;

// Custom "secure" data format to create basic cookies for test
// Essentially just creates key-value pairs from the claims and then base64 encodes them

// This easily manipulated format allows our tests to create cookies with any claims we want
// and the server will be able to decode the code and access the claims
internal class TestCookieTicketFormat : ISecureDataFormat<AuthenticationTicket>
{
    public string Protect(AuthenticationTicket data)
    {
        var claims = data.Principal.Claims.Select(x => $"{x.Type}={x.Value}").ToArray();
        var claimsString = string.Join("&", claims);
        var ticketBytes = Encoding.UTF8.GetBytes(claimsString);

        return Convert.ToBase64String(ticketBytes);
    }

    public string Protect(AuthenticationTicket data, string? purpose) => this.Protect(data);

    public AuthenticationTicket? Unprotect(string? protectedText)
    {
        if (string.IsNullOrWhiteSpace(protectedText))
        {
            return null;
        }

        var ticketBytes = Convert.FromBase64String(protectedText);
        var claimsString = Encoding.UTF8.GetString(ticketBytes);
        var claims = claimsString.Split('&').Select(x =>
        {
            var claimParts = x.Split('=');
            return new Claim(claimParts[0], claimParts[1]);
        });

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authenticationTicket = new AuthenticationTicket(new ClaimsPrincipal(claimsIdentity), CookieAuthenticationDefaults.AuthenticationScheme);

        return authenticationTicket;
    }

    public AuthenticationTicket? Unprotect(string? protectedText, string? purpose) => this.Unprotect(protectedText);
}