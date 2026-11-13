// Project comment: CspNonceTagHelper.cs is part of the IT Form application source code.

using Microsoft.AspNetCore.Razor.TagHelpers;

namespace IT_Form.Security;

/// <summary>
/// Adds the request CSP nonce to Razor script elements so inline script blocks
/// are executable only when authorized by the response policy.
/// </summary>
[HtmlTargetElement("script")]
public sealed class CspNonceTagHelper : TagHelper
{
    public override int Order => int.MinValue;

    public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (!context.Items.TryGetValue(SecurityHeadersMiddleware.CspNonceItemKey, out var nonce) ||
            nonce is not string nonceValue ||
            string.IsNullOrWhiteSpace(nonceValue))
        {
            return Task.CompletedTask;
        }

        output.Attributes.SetAttribute("nonce", nonceValue);
        return Task.CompletedTask;
    }
}

