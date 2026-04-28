// Project comment: EmailTemplateHelper.cs is part of the IT Form application source code.

namespace IT_Form.Helpers
{
    /// <summary>
    /// Defines the EmailTemplateHelper class used by the IT Form application.
    /// </summary>
    public static class EmailTemplateHelper
    {
        /// <summary>
        /// Gets the data required by the GetProfessionalHtmlTemplate operation.
        /// </summary>
        public static string GetProfessionalHtmlTemplate(string title, string greeting, string paramsRows, string ctaUrl, string ctaLabel)
        {
            var year = System.DateTime.UtcNow.Year;

            return $@"<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'>
<html xmlns='http://www.w3.org/1999/xhtml' xmlns:v='urn:schemas-microsoft-com:vml' xmlns:o='urn:schemas-microsoft-com:office:office'>
<head>
    <meta http-equiv='Content-Type' content='text/html; charset=UTF-8' />
    <!--[if !mso]><!--><meta http-equiv='X-UA-Compatible' content='IE=edge' /><!--<![endif]-->
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>{System.Text.Encodings.Web.HtmlEncoder.Default.Encode(title)}</title>
    <!--[if mso]>
    <xml>
        <o:OfficeDocumentSettings>
            <o:AllowPNG/>
            <o:PixelsPerInch>96</o:PixelsPerInch>
        </o:OfficeDocumentSettings>
    </xml>
    <style type='text/css'>
        table {{ border-collapse: collapse; }}
        body, table, td, a {{ font-family: Arial, Helvetica, sans-serif !important; }}
    </style>
    <![endif]-->
</head>
<body style='margin:0; padding:0; background-color:#F4F7FA;'>
    <center style='width: 100%; table-layout: fixed; background-color: #F4F7FA; padding-bottom: 40px;'>
        <div style='max-width: 600px; margin: 0 auto;'>
            <!--[if mso]>
            <table align='center' border='0' cellspacing='0' cellpadding='0' width='600' style='width:600px;'>
            <tr>
            <td align='center' valign='top' width='600'>
            <![endif]-->
            <table align='center' border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color: #ffffff; border-collapse: collapse; max-width: 600px; width: 100%; margin: 20px 0; border: 1px solid #E1E8F0; box-shadow: 0 4px 12px rgba(0,0,0,0.05);'>
                
                <!-- Brand Header -->
                <tr>
                    <td style='padding: 40px 40px 30px 40px; border-bottom: 1px solid #F0F4F8; background-color: #ffffff;'>
                        <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                            <tr>
                                <td>
                                    <div style='color: #64748B; font-family: sans-serif; font-size: 11px; font-weight: 800; text-transform: uppercase; letter-spacing: 2px; margin-bottom: 4px;'>YTC IT PORTAL</div>
                                    <h1 style='margin: 0; color: #1A52A3; font-family: sans-serif; font-size: 26px; font-weight: 800; line-height: 1.2;'>{System.Text.Encodings.Web.HtmlEncoder.Default.Encode(title)}</h1>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>

                <!-- Content Body -->
                <tr>
                    <td style='padding: 40px 40px 10px 40px;'>
                        <p style='margin: 0; color: #475569; font-family: sans-serif; font-size: 16px; line-height: 1.6;'>
                            {greeting}
                        </p>
                        
                        <!-- Details Table -->
                        <table border='0' cellpadding='0' cellspacing='0' width='100%' style='margin-top: 30px; border: 1px solid #E2E8F0; border-radius: 8px;'>
                            <tr style='background-color: #F8FAFC;'>
                                <td width='35%' style='padding: 14px 15px; font-family: sans-serif; font-size: 12px; color: #64748B; font-weight: 700; text-transform: uppercase; border-bottom: 1px solid #E2E8F0;'>Parameter</td>
                                <td width='65%' style='padding: 14px 15px; font-family: sans-serif; font-size: 12px; color: #64748B; font-weight: 700; text-transform: uppercase; border-bottom: 1px solid #E2E8F0;'>Details</td>
                            </tr>
                            {paramsRows}
                        </table>

                        <!-- CTA Call to Action -->
                        <table border='0' cellpadding='0' cellspacing='0' width='100%' style='margin-top: 40px; margin-bottom: 20px;'>
                            <tr>
                                <td align='center'>
                                    <div>
                                        <!--[if mso]>
                                        <v:roundrect xmlns:v='urn:schemas-microsoft-com:vml' xmlns:o='urn:schemas-microsoft-com:office:office' href='{ctaUrl}' style='height:54px;v-text-anchor:middle;width:240px;' arcsize='12%' stroke='f' fillcolor='#6f42c1'>
                                            <w:anchorlock/>
                                            <center style='color:#ffffff;font-family:sans-serif;font-size:16px;font-weight:700;'>{ctaLabel}</center>
                                        </v:roundrect>
                                        <![endif]-->
                                        <!--[if !mso]><!-->
                                        <a href='{ctaUrl}' target='_blank' style='background-color:#6f42c1;border-radius:6px;color:#ffffff;display:inline-block;font-family:sans-serif;font-size:16px;font-weight:700;line-height:54px;text-align:center;text-decoration:none;width:240px;-webkit-text-size-adjust:none;'>{ctaLabel}</a>
                                        <!--<![endif]-->
                                    </div>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>

                <!-- SENDER_BLOCK_PLACEHOLDER -->

                <!-- Footer -->
                <tr>
                    <td style='padding: 30px 40px; background-color: #F8FAFC; border-top: 1px solid #F0F4F8;'>
                        <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                            <tr>
                                <td align='center' style='padding-bottom: 8px;'>
                                    <p style='margin: 0; font-family: sans-serif; font-size: 12px; color: #94A3B8; font-weight: 600;'>
                                        &copy; {year} Yeng Tong Construction (Precast) Sdn Bhd.
                                    </p>
                                </td>
                            </tr>
                            <tr>
                                <td align='center'>
                                    <p style='margin: 0; font-family: sans-serif; font-size: 11px; color: #CBD5E1;'>
                                        This is an automated system notification. Please do not reply.
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
            <!--[if mso]>
            </td>
            </tr>
            </table>
            <![endif]-->
        </div>
    </center>
</body>
</html>";
        }

        /// <summary>
        /// Gets the data required by the GetProfessionalHtmlTemplate operation.
        /// </summary>
        public static string GetProfessionalHtmlTemplate(string title, string greeting, string paramsRows, string ctaUrl, string ctaLabel, string? senderName, string? senderTitle, string? senderSignatureImage = null)
        {
            var senderBlock = "";
            
            if (!string.IsNullOrWhiteSpace(senderSignatureImage))
            {
                senderBlock = $@"
                <!-- Sender Signature Block -->
                <tr>
                    <td style='padding: 0 40px 40px 40px;'>
                        <div style='border-top: 1px solid #EDF2F7; padding-top: 25px;'>
                            <img src='cid:signature' alt='Signature' border='0' width='400' style='max-width: 450px; width: 100%; height: auto; display: block; border: 0;' />
                        </div>
                    </td>
                </tr>";
            }

            var baseHtml = GetProfessionalHtmlTemplate(title, greeting, paramsRows, ctaUrl, ctaLabel);
            return baseHtml.Replace("<!-- SENDER_BLOCK_PLACEHOLDER -->", senderBlock);
        }
    }
}
