using IT_Form.Models;
using IT_Form.Models.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace IT_Form.Services
{
    public interface IPdfService
    {
        byte[] GeneratePdf(OnboardingRequest request);
        byte[] GenerateOffboardingPdf(OffboardingRequest request);
    }

    public class PdfService : IPdfService
    {
        private readonly ILogger<PdfService> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IT_Form.Data.ApplicationDbContext _context;

        private readonly ISignatureIntegrityService _integrityService;

        public PdfService(ILogger<PdfService> logger, IWebHostEnvironment env, IT_Form.Data.ApplicationDbContext context, ISignatureIntegrityService integrityService)
        {
            _logger = logger;
            _env = env;
            _context = context;
            _integrityService = integrityService;
        }

        private byte[]? GetCompanyLogo(string? branch)
        {
            if (string.IsNullOrEmpty(branch)) return GetDefaultLogo(null);

            var company = _context.Companies
                .AsNoTracking()
                .FirstOrDefault(c => c.Name == branch || (c.Name + " (" + c.Lot + ")") == branch || branch.StartsWith(c.Name + " ("));

            if (company != null && !string.IsNullOrEmpty(company.LogoPath))
            {
                var logoPath = Path.Combine(_env.WebRootPath, company.LogoPath.TrimStart('/'));
                if (File.Exists(logoPath))
                    return File.ReadAllBytes(logoPath);
            }

            return GetDefaultLogo(branch);
        }

        private byte[]? GetDefaultLogo(string? branch)
        {
            var isYTC = branch != null && branch.Contains("YTC", StringComparison.OrdinalIgnoreCase);
            var fileName = isYTC ? "logo-ytc.jpg" : "logo-pyt.png";
            var logoPath = Path.Combine(_env.WebRootPath, "images", fileName);
            if (File.Exists(logoPath))
                return File.ReadAllBytes(logoPath);
            return null;
        }

        private string GetCompanyName(string? branch)
        {
            if (string.IsNullOrEmpty(branch)) return "PEMBINAAN YENG TONG SDN BHD";

            var company = _context.Companies
                .AsNoTracking()
                .FirstOrDefault(c => c.Name == branch || (c.Name + " (" + c.Lot + ")") == branch || branch.StartsWith(c.Name + " ("));

            if (company != null)
            {
                return company.Name.ToUpper();
            }

            if (branch.Contains("YTC", StringComparison.OrdinalIgnoreCase))
                return "YTC PRECAST SDN BHD (1392485-P)";
            return "PEMBINAAN YENG TONG SDN BHD";
        }

        public byte[] GeneratePdf(OnboardingRequest request)
        {
            var assignedSoftwareNames = GetAssignedSoftwareNames(request);
            var document = Document.Create(container =>
            {
                // PAGE 1: IT EQUIPMENT ISSUANCE FORM
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(inner =>
                            {
                                inner.Item().AlignCenter().Text(GetCompanyName(request.Branch))
                                    .FontSize(8).FontColor(Colors.Grey.Medium);
                                inner.Item().AlignCenter().Text("IT EQUIPMENT ISSUANCE FORM")
                                    .FontSize(14).Bold();
                                inner.Item().AlignCenter().Text("Borang Pengeluaran Peralatan IT")
                                    .FontSize(9).FontColor(Colors.Grey.Darken1);
                            });
                            var logo = GetCompanyLogo(request.Branch);
                            if (logo != null)
                            {
                                row.ConstantItem(80).AlignRight().AlignMiddle()
                                    .Image(logo).FitArea();
                            }
                        });
                        col.Item().PaddingTop(3).LineHorizontal(1.5f).LineColor(Colors.Black);
                    });

                    page.Content().PaddingTop(5).Column(col =>
                    {
                        // Section A: Employee Information
                        col.Item().PaddingTop(5).Text("A. Employee Information")
                            .FontSize(10).Bold().BackgroundColor(Colors.Grey.Lighten3);

                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(7);
                            });

                            AddInfoRow(table, "Full Name", request.FullName ?? "—");
                            AddInfoRow(table, "Employee ID", request.EmployeeID ?? "—");
                            AddInfoRow(table, "Designation", request.Designation ?? "—");
                            AddInfoRow(table, "Department", $"{request.Department} {(request.Department == "Others" && !string.IsNullOrEmpty(request.OtherDepartmentDetails) ? $"({request.OtherDepartmentDetails})" : "")}");
                            AddInfoRow(table, "Company", $"{request.Branch} {(request.Branch == "Others" && !string.IsNullOrEmpty(request.OtherBranchDetails) ? $"({request.OtherBranchDetails})" : "")}");
                            AddInfoRow(table, "Desk Number", request.DeskNumber ?? "—");
                            AddInfoRow(table, "Date Hire", request.JoinDate?.ToString("dd/MM/yyyy") ?? "—");
                            AddInfoRow(table, "Email Address", request.EmailAddress ?? "—");
                            AddInfoRow(table, "Head of Department", request.HODEmail ?? "—");
                        });

                        // Section B: IT Equipment Issued
                        col.Item().PaddingTop(8).Text("B. IT Equipment Issued")
                            .FontSize(10).Bold().BackgroundColor(Colors.Grey.Lighten3);

                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(25);
                                c.RelativeColumn(2);
                                c.ConstantColumn(30);
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Border(1).Padding(2).Text("No.").Bold().FontSize(8);
                                header.Cell().Border(1).Padding(2).Text("Item").Bold().FontSize(8);
                                header.Cell().Border(1).Padding(2).Text("Qty").Bold().FontSize(8);
                                header.Cell().Border(1).Padding(2).Text("Brand & Model").Bold().FontSize(8);
                                header.Cell().Border(1).Padding(2).Text("Serial No.").Bold().FontSize(8);
                            });

                            if (request.IssuedAssets != null && request.IssuedAssets.Any())
                            {
                                for (int i = 0; i < request.IssuedAssets.Count; i++)
                                {
                                    var asset = request.IssuedAssets[i];
                                    table.Cell().Border(1).Padding(2).Text($"{i + 1}").FontSize(8);
                                    table.Cell().Border(1).Padding(2).Text(asset.AssetType.ToString()).FontSize(8);
                                    table.Cell().Border(1).Padding(2).Text(asset.Qty?.ToString() ?? "").FontSize(8);
                                    table.Cell().Border(1).Padding(2).Text(asset.BrandModel ?? "").FontSize(8);
                                    table.Cell().Border(1).Padding(2).Text(asset.SerialNumber ?? "").FontSize(8);
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(5).Border(1).Padding(3).AlignCenter()
                                    .Text("No equipment issued").FontColor(Colors.Grey.Medium).FontSize(8);
                            }
                        });

                        if (!string.IsNullOrEmpty(request.AssetRemarks))
                        {
                            col.Item().PaddingTop(2).Text($"Asset Remarks: {request.AssetRemarks}")
                                .FontSize(7).Italic();
                        }

                        // Section C: Software Entitlements
                        col.Item().PaddingTop(8).Text("C. Software Entitlements")
                            .FontSize(10).Bold().BackgroundColor(Colors.Grey.Lighten3);

                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(5);
                                c.RelativeColumn(5);
                            });

                            if (assignedSoftwareNames.Any())
                            {
                                foreach (var softwareName in assignedSoftwareNames)
                                {
                                    table.Cell().ColumnSpan(2).Padding(1).Text($"✓ {softwareName}").FontSize(8);
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(2).Padding(1).Text("No specific software requested.").FontSize(8).Italic();
                            }
                        });

                        if (!string.IsNullOrEmpty(request.OtherSoftwareDetails))
                        {
                            col.Item().PaddingTop(2).Text($"Other Software: {request.OtherSoftwareDetails}").FontSize(8);
                        }

                        if (!string.IsNullOrEmpty(request.SoftwareRemarks))
                        {
                            col.Item().PaddingTop(2).Text($"Software Remarks: {request.SoftwareRemarks}")
                                .FontSize(7).Italic();
                        }

                        // Section D: IT Checklist
                        col.Item().PaddingTop(8).Text("D. Checklist for IT Personal to follow")
                            .FontSize(10).Bold().BackgroundColor(Colors.Grey.Lighten3);

                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(25);
                                c.RelativeColumn(5);
                                c.RelativeColumn(3);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Border(1).Padding(2).Text("No.").Bold().FontSize(7);
                                header.Cell().Border(1).Padding(2).Text("Particulars").Bold().FontSize(7);
                                header.Cell().Border(1).Padding(2).AlignCenter().Text("Onboarding Phase").Bold().FontSize(7);
                            });

                            var items = new (string Name, bool Value, string Label)[] {
                                ("Physical Checks & Clean on IT Equipment", request.CheckPhysicalClean, "Checks complete"),
                                ("MS O365 Account Setup", request.CheckO365Setup, "Setup successfully"),
                                ("Windows Account & Login", request.CheckWindowsLogin, "Login successfully"),
                                ("Outlook & Email Account Setup", request.CheckEmailSetup, "Access successfully"),
                                ("Specific Software / Application", request.CheckSpecificSoftware, "Setup successfully"),
                                ("General Software / Application", request.CheckGeneralSoftware, "Setup successfully"),
                                ("Add user to ADs Group", request.CheckAbcGroup, "Add successfully"),
                                ("Add user to Email Mail Group", request.CheckEmailMailGroup, "Add successfully"),
                                ("Device Name & Domain Join", request.CheckDomainJoin, "Join successfully"),
                                ("Shared Drive mapping", request.CheckSharedDrive, "Access successfully"),
                                ("Primary Location Wi-Fi Setup", request.CheckWiFiSetup, "Connect successfully"),
                                ("Primary Location Printer Setup", request.CheckPrinterSetup, "Print successfully"),
                                ("Primary Location Scan Folder Setup", request.CheckScanFolder, "Access successfully"),
                                ("VPN Account Setup", request.CheckVPNSetup, "Connect successfully")
                            };

                            for (int i = 0; i < items.Length; i++)
                            {
                                table.Cell().Border(1).Padding(2).Text($"{i + 1}").FontSize(7);
                                table.Cell().Border(1).Padding(2).Text(items[i].Name).FontSize(7);
                                table.Cell().Border(1).Padding(2).AlignCenter()
                                    .Text($"{(items[i].Value ? "✓" : "")} {items[i].Label}").FontSize(7);
                            }
                        });

                        if (!string.IsNullOrEmpty(request.ChecklistRemarks))
                        {
                            col.Item().PaddingTop(2).Text($"Remarks: {request.ChecklistRemarks}")
                                .FontSize(7).Italic();
                        }

                        // Signatures
                        col.Item().PaddingTop(12).Text("E. Signatures")
                            .FontSize(10).Bold().BackgroundColor(Colors.Grey.Lighten3);

                        col.Item().PaddingTop(5).Row(row =>
                        {
                            AddVerifiedSignatureBlock(row, "HR Signature", request.RequesterSignature, request.RequesterSignatureHmac, "OnboardingRequest", request.Id.ToString(), request.RequesterSignerName ?? "Unknown", request.RequesterSignedAt);
                            row.ConstantItem(15);
                            AddVerifiedSignatureBlock(row, "IT Department", request.ITAdminSignature, request.ITAdminSignatureHmac, "OnboardingRequest", request.Id.ToString(), request.ITAdminSignerName ?? "Unknown", request.ITAdminSignedAt);
                        });
                    });

                    page.Footer().AlignCenter()
                        .Text("YTC IT Department - Confidential | Page 1 of 2")
                        .FontSize(7).FontColor(Colors.Grey.Medium);
                });

                // PAGE 2: SERVER ACCESS FORM
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(inner =>
                            {
                                inner.Item().AlignCenter().Text(GetCompanyName(request.Branch))
                                    .FontSize(8).FontColor(Colors.Grey.Medium);
                                inner.Item().AlignCenter().Text("SERVER ACCESS & SYSTEM REQUEST FORM")
                                    .FontSize(14).Bold();
                                inner.Item().AlignCenter().Text("Borang Permohonan Akses Pelayan & Sistem")
                                    .FontSize(9).FontColor(Colors.Grey.Darken1);
                            });
                            var logo = GetCompanyLogo(request.Branch);
                            if (logo != null)
                            {
                                row.ConstantItem(80).AlignRight().AlignMiddle()
                                    .Image(logo).FitArea();
                            }
                        });
                        col.Item().PaddingTop(3).LineHorizontal(1.5f).LineColor(Colors.Black);
                    });

                    page.Content().PaddingTop(5).Column(col =>
                    {
                        // Section A: Applicant Info
                        col.Item().PaddingTop(5).Text("A. Applicant Information")
                            .FontSize(10).Bold().BackgroundColor(Colors.Grey.Lighten3);

                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(7);
                            });
                            AddInfoRow(table, "Full Name", request.FullName ?? "—");
                            AddInfoRow(table, "Department", request.Department ?? "—");
                            AddInfoRow(table, "Designation", request.Designation ?? "—");
                        });

                        // Section B: Email Groups
                        col.Item().PaddingTop(8).Text("B. Email Distribution Groups")
                            .FontSize(10).Bold().BackgroundColor(Colors.Grey.Lighten3);

                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(7);
                            });
                            if (request.RequestEmailGroups != null && request.RequestEmailGroups.Any())
                            {
                                foreach (var group in request.RequestEmailGroups)
                                {
                                    AddInfoRow(table, group.GroupName, "✓ Yes");
                                }
                            }
                            else
                            {
                                AddInfoRow(table, "Selected Groups", "None");
                            }
                        });

                        // Section C: Shared Mailboxes
                        col.Item().PaddingTop(8).Text("C. Shared Mailbox Access")
                            .FontSize(10).Bold().BackgroundColor(Colors.Grey.Lighten3);

                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(5);
                                c.RelativeColumn(5);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Border(1).Padding(2).Text("Mailbox").Bold().FontSize(8);
                                header.Cell().Border(1).Padding(2).AlignCenter().Text("Access").Bold().FontSize(8);
                            });

                            if (request.RequestSharedMailboxes != null && request.RequestSharedMailboxes.Any())
                            {
                                foreach (var mb in request.RequestSharedMailboxes)
                                {
                                    table.Cell().Border(1).Padding(2).Text(mb.MailboxName).FontSize(8);
                                    table.Cell().Border(1).Padding(2).AlignCenter()
                                        .Text("✓").FontSize(9);
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(2).Border(1).Padding(2).Text("None Selected").FontSize(8).Italic();
                            }

                            if (!string.IsNullOrEmpty(request.OtherSharedMailboxDetails))
                            {
                                table.Cell().Border(1).Padding(2).Text($"Other: {request.OtherSharedMailboxDetails}").FontSize(8);
                                table.Cell().Border(1).Padding(2).AlignCenter().Text("✓").FontSize(9);
                            }
                        });

                        // Section D: Server Folder Permissions
                        col.Item().PaddingTop(8).Text("D. Server Folder Access Permissions")
                            .FontSize(10).Bold().BackgroundColor(Colors.Grey.Lighten3);

                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(5);
                                c.RelativeColumn(2.5f);
                                c.RelativeColumn(2.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Border(1).Padding(2).Text("Folder Name").Bold().FontSize(8);
                                header.Cell().Border(1).Padding(2).AlignCenter().Text("Read").Bold().FontSize(8);
                                header.Cell().Border(1).Padding(2).AlignCenter().Text("Modify").Bold().FontSize(8);
                            });

                            if (request.FolderPermissions != null && request.FolderPermissions.Any())
                            {
                                foreach (var fp in request.FolderPermissions)
                                {
                                    table.Cell().Border(1).Padding(2).Text(fp.FolderName).FontSize(8);
                                    table.Cell().Border(1).Padding(2).AlignCenter()
                                        .Text(fp.CanRead ? "✓" : "").FontSize(9);
                                    table.Cell().Border(1).Padding(2).AlignCenter()
                                        .Text(fp.CanModify ? "✓" : "").FontSize(9);
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(3).Border(1).Padding(3).AlignCenter()
                                    .Text("No folder permissions assigned").FontColor(Colors.Grey.Medium).FontSize(8);
                            }

                            if (!string.IsNullOrEmpty(request.OtherFolderDetails))
                            {
                                table.Cell().Border(1).Padding(2).Text($"Other Folder: {request.OtherFolderDetails}").FontSize(8);
                                table.Cell().Border(1).Padding(2).AlignCenter().Text("✓").FontSize(9);
                                table.Cell().Border(1).Padding(2).Text("").FontSize(9);
                            }
                        });

                        if (!string.IsNullOrEmpty(request.FolderRemarks))
                        {
                            col.Item().PaddingTop(2).Text($"Folder Remarks: {request.FolderRemarks}")
                                .FontSize(7).Italic();
                        }

                        // Issuance Acknowledgment
                        col.Item().PaddingTop(10).Text("Issuance Acknowledgment")
                            .FontSize(9).Bold();
                        col.Item().PaddingTop(2).Text(
                            "By signing this checklist, I hereby certify that the above service(s) has been carried out to my satisfaction, " +
                            "and I acknowledge receipt of the service rendered and/or equipment in good order. I understand that all the " +
                            "devices and accessories listed above will always remain the property of YTC Precast and are to be returned " +
                            "upon demand or exchanged by the company. I acknowledge and accept the terms and conditions for the use of the Company assets.")
                            .FontSize(6).Italic().FontColor(Colors.Grey.Darken1);

                        // Section E: Signatures
                        col.Item().PaddingTop(8).Text("E. Authorization Signatures")
                            .FontSize(10).Bold().BackgroundColor(Colors.Grey.Lighten3);

                        col.Item().PaddingTop(5).Row(row =>
                        {
                            AddVerifiedSignatureBlock(row, $"Employee Signature: {request.EmployeeSignerName ?? request.FullName ?? "Unknown"}", request.EmployeeSignature, request.EmployeeSignatureHmac, "OnboardingRequest", request.Id.ToString(), request.EmployeeSignerName ?? "Employee", request.EmployeeSignedAt);
                            row.ConstantItem(15);
                            AddVerifiedSignatureBlock(row, "Verified by IT", request.ITAdminSignature, request.ITAdminSignatureHmac, "OnboardingRequest", request.Id.ToString(), request.ITAdminSignerName ?? "Unknown", request.ITAdminSignedAt);
                        });
                    });

                    page.Footer().AlignCenter()
                        .Text("YTC IT Department - Confidential | Page 2 of 2")
                        .FontSize(7).FontColor(Colors.Grey.Medium);
                });
            });

            var pdfBytes = document.GeneratePdf();
            _logger.LogInformation("PDF generated successfully ({Size} bytes)", pdfBytes.Length);
            return pdfBytes;
        }

        private static List<string> GetAssignedSoftwareNames(OnboardingRequest request)
        {
            var issuedSoftwareNames = request.IssuedSoftware?
                .Select(sw => !string.IsNullOrWhiteSpace(sw.SoftwareName)
                    ? sw.SoftwareName!.Trim()
                    : sw.SoftwareLicense?.SoftwareName?.Trim())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToList();

            if (issuedSoftwareNames != null && issuedSoftwareNames.Any())
            {
                return issuedSoftwareNames;
            }

            return request.RequestSoftware?
                .Select(sw => sw.SoftwareName?.Trim())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .Cast<string>()
                .ToList()
                ?? new List<string>();
        }

        private void AddInfoRow(TableDescriptor table, string label, string value)
        {
            table.Cell().Border(1).Padding(3).Text(label).Bold().FontSize(8)
                .BackgroundColor(Colors.Grey.Lighten4);
            table.Cell().Border(1).Padding(3).Text(value ?? "—").FontSize(8);
        }

        private void AddVerifiedSignatureBlock(RowDescriptor row, string label, string? signatureBase64, string? hmac, string entityType, string entityId, string signer, DateTime? signedAt)
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Height(40).Background(Colors.White).Container().AlignCenter().AlignMiddle().Element(container =>
                {
                    if (!string.IsNullOrEmpty(signatureBase64))
                    {
                        // VERIFY INTEGRITY
                        bool isValid = false;
                        if (signedAt.HasValue && !string.IsNullOrEmpty(hmac))
                        {
                            isValid = _integrityService.Verify(entityType, entityId, signer, signedAt.Value, signatureBase64, hmac);
                        }

                        if (!isValid && !string.IsNullOrEmpty(hmac))
                        {
                            container.AlignCenter().Text("INVALID SIGNATURE").FontSize(8).Bold().FontColor(Colors.Red.Medium);
                        }
                        else if (signatureBase64.Contains("base64,"))
                        {
                            try
                            {
                                var base64Data = signatureBase64.Split("base64,")[1];
                                var imageBytes = Convert.FromBase64String(base64Data);
                                container.Image(imageBytes).FitArea();
                            }
                            catch
                            {
                                // Empty box on error
                            }
                        }
                        else
                        {
                            container.Text(signatureBase64).FontSize(8).Italic().FontColor(Colors.Grey.Medium);
                        }
                    }
                });
                c.Item().LineHorizontal(1).LineColor(Colors.Black);
                c.Item().PaddingTop(2).AlignCenter().Text(label).FontSize(7);
                if (signedAt.HasValue)
                {
                    c.Item().AlignCenter().Text($"Date: {signedAt.Value:dd/MM/yyyy}").FontSize(6).FontColor(Colors.Grey.Medium);
                }
            });
        }

        public byte[] GenerateOffboardingPdf(OffboardingRequest request)
        {
            var document = Document.Create(container =>
            {
                // PAGE 1: OFFBOARDING CHECKLIST
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(inner =>
                            {
                                inner.Item().AlignCenter().Text(GetCompanyName(request.Branch))
                                    .FontSize(8).FontColor(Colors.Grey.Medium);
                                inner.Item().AlignCenter().Text("OFFBOARDING & EXIT CLEARANCE FORM")
                                    .FontSize(14).Bold();
                                inner.Item().AlignCenter().Text("Borang Pelepasan Keluar & Penamatan Perkhidmatan")
                                    .FontSize(9).FontColor(Colors.Grey.Darken1);
                            });
                            var logo = GetCompanyLogo(request.Branch);
                            if (logo != null)
                            {
                                row.ConstantItem(80).AlignRight().AlignMiddle()
                                    .Image(logo).FitArea();
                            }
                        });
                        col.Item().PaddingTop(3).LineHorizontal(1.5f).LineColor(Colors.Black);
                        col.Item().AlignRight().Text($"Ref: YTC/IT/OB-001 | Date: {request.CreatedAt:dd/MM/yyyy}")
                            .FontSize(7).FontColor(Colors.Grey.Medium);
                    });

                    page.Content().PaddingTop(5).Column(col =>
                    {
                        // Section A: Employee Information
                        col.Item().PaddingTop(5).Text("A. Employee Information")
                            .FontSize(10).Bold().BackgroundColor(Colors.Grey.Lighten3);

                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(7);
                            });

                            AddInfoRow(table, "Full Name", request.FullName ?? "—");
                            AddInfoRow(table, "Employee ID", request.EmployeeID ?? "—");
                            AddInfoRow(table, "Department", request.Department ?? "—");
                            AddInfoRow(table, "Company", request.Branch ?? "—");
                            AddInfoRow(table, "Email Address", request.EmailAddress ?? "—");
                            AddInfoRow(table, "Last Day of Work", request.LastDayOfWork.ToString("dd/MM/yyyy"));
                            AddInfoRow(table, "Head of Department", request.HODEmail ?? "—");
                        });

                        // Section B: Clearance Items
                        col.Item().PaddingTop(8).Text("B. Clearance Items")
                            .FontSize(10).Bold().BackgroundColor(Colors.Grey.Lighten3);

                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(25);
                                c.RelativeColumn(5);
                                c.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Border(1).Padding(2).Text("No.").Bold().FontSize(7);
                                header.Cell().Border(1).Padding(2).Text("Particulars").Bold().FontSize(7);
                                header.Cell().Border(1).Padding(2).AlignCenter().Text("Returned").Bold().FontSize(7);
                            });

                            var clearanceItems = new (string Name, bool Value)[] {
                                ("Laptop / Desktop / IT Devices", request.ClearedLaptop),
                                ("Mobile / Device", request.ClearedMobile),
                                ("Keys / Cards", request.ClearedKeys),
                                ("Other Assets", request.ClearedOtherAssets)
                            };

                            for (int i = 0; i < clearanceItems.Length; i++)
                            {
                                table.Cell().Border(1).Padding(2).Text($"{i + 1}").FontSize(7);
                                table.Cell().Border(1).Padding(2).Text(clearanceItems[i].Name).FontSize(7);
                                table.Cell().Border(1).Padding(2).AlignCenter()
                                    .Text(clearanceItems[i].Value ? "✓ Yes" : "No").FontSize(7);
                            }
                        });

                        if (!string.IsNullOrEmpty(request.ClearanceRemarks))
                        {
                            col.Item().PaddingTop(2).Text($"Remarks: {request.ClearanceRemarks}")
                                .FontSize(7).Italic();
                        }

                        if (request.MissingItemFines.HasValue && request.MissingItemFines.Value > 0)
                        {
                            col.Item().PaddingTop(2).Text($"Fine for Missing/Damaged Items: RM {request.MissingItemFines.Value:0.00} {(string.IsNullOrEmpty(request.FinesRemarks) ? "" : $"- {request.FinesRemarks}")}")
                                .FontSize(7).Bold().FontColor(Colors.Red.Darken2);
                        }

                        // Section C: System & Network Access (To Revoke)
                        col.Item().PaddingTop(8).Text("C. System & Network Access (To Revoke)")
                            .FontSize(10).Bold().BackgroundColor(Colors.Grey.Lighten3);

                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(7);
                            });

                            if (request.OnboardingRequest != null)
                            {
                                var oReq = request.OnboardingRequest;

                                // Software
                                var softwareList = new List<string>();
                                if (oReq.HasAdobe) softwareList.Add("Adobe");
                                if (oReq.HasMSProject) softwareList.Add("MS Project");
                                if (oReq.HasAutoDesk) softwareList.Add("AutoDesk");
                                if (oReq.HasGStarCAD) softwareList.Add("G-Star CAD");
                                if (oReq.HasFortiClientVPN) softwareList.Add("FortiClient VPN");
                                if (oReq.HasSQLAccounting) softwareList.Add("SQL Accounting");
                                if (oReq.HasMSVisio) softwareList.Add("MS Visio");
                                if (oReq.HasAutoCAD) softwareList.Add("AutoCAD");
                                if (oReq.HasSketchUp) softwareList.Add("SketchUp");
                                if (oReq.HasMSOffice) softwareList.Add("MS Office");
                                if (oReq.HasGoogleChrome) softwareList.Add("Google Chrome");
                                if (oReq.HasMSTeams) softwareList.Add("MS Teams");
                                if (oReq.HasAntivirus) softwareList.Add("Antivirus");
                                if (oReq.HasDWGTrueView) softwareList.Add("DWG TrueView");
                                if (oReq.HasIFlexiHRMS) softwareList.Add("i-Flexi HRMS");
                                if (oReq.HasEShare) softwareList.Add("EShare");
                                if (oReq.HasOS) softwareList.Add("Operating System");

                                if (oReq.RequestSoftware != null && oReq.RequestSoftware.Any())
                                    softwareList.AddRange(oReq.RequestSoftware.Select(s => s.SoftwareName));
                                if (!string.IsNullOrEmpty(oReq.OtherSoftwareDetails))
                                    softwareList.Add($"Other: {oReq.OtherSoftwareDetails}");
                                AddInfoRow(table, "Assigned Software", softwareList.Any() ? string.Join(", ", softwareList) : "None");

                                // Email Groups
                                var emailGroups = oReq.RequestEmailGroups != null && oReq.RequestEmailGroups.Any()
                                    ? string.Join(", ", oReq.RequestEmailGroups.Select(g => g.GroupName))
                                    : "None";
                                AddInfoRow(table, "Email Distribution Groups", emailGroups);

                                // Shared Mailboxes
                                var mailboxesList = new List<string>();
                                if (oReq.MailboxAccount) mailboxesList.Add("M365 Account");
                                if (oReq.MailboxLogistic) mailboxesList.Add("Logistic");
                                if (oReq.MailboxInfo) mailboxesList.Add("Info");
                                if (oReq.MailboxAdmin) mailboxesList.Add("Admin");
                                if (oReq.MailboxIntern) mailboxesList.Add("Intern");
                                if (oReq.MailboxIT) mailboxesList.Add("IT");
                                if (oReq.MailboxSupervisor) mailboxesList.Add("Supervisor");
                                if (oReq.MailboxStore) mailboxesList.Add("Store");
                                if (oReq.PrecastGroup) mailboxesList.Add("Precast Group");

                                if (oReq.RequestSharedMailboxes != null && oReq.RequestSharedMailboxes.Any())
                                    mailboxesList.AddRange(oReq.RequestSharedMailboxes.Select(m => m.MailboxName));
                                if (!string.IsNullOrEmpty(oReq.OtherSharedMailboxDetails))
                                    mailboxesList.Add($"Other: {oReq.OtherSharedMailboxDetails}");
                                AddInfoRow(table, "Shared Mailbox Access", mailboxesList.Any() ? string.Join(", ", mailboxesList) : "None");

                                // Server Folders
                                var activeFolders = _context.ServerFolders.Where(f => f.IsActive).ToList();
                                if (activeFolders.Any())
                                {
                                    var fList = new List<string>();
                                    foreach (var folder in activeFolders)
                                    {
                                        var perm = oReq.FolderPermissions?.FirstOrDefault(f => f.FolderName == folder.FolderName);
                                        if (perm != null && (perm.CanRead || perm.CanModify))
                                        {
                                            fList.Add($"✓ {folder.FolderName}");
                                        }
                                        else
                                        {
                                            fList.Add($"  {folder.FolderName}");
                                        }
                                    }
                                    AddInfoRow(table, "Server Folder Access", string.Join("\n", fList));
                                }
                                else
                                {
                                    var folders = oReq.FolderPermissions != null && oReq.FolderPermissions.Any()
                                        ? string.Join(", ", oReq.FolderPermissions.Select(f => f.FolderName))
                                        : "None";
                                    AddInfoRow(table, "Server Folder Access", folders);
                                }
                            }
                            else
                            {
                                AddInfoRow(table, "Assigned Software", "Unknown (Legacy record)");
                                AddInfoRow(table, "Email Distribution Groups", "Unknown (Legacy record)");
                                AddInfoRow(table, "Shared Mailbox Access", "Unknown (Legacy record)");
                                AddInfoRow(table, "Server Folder Access", "Unknown (Legacy record)");
                            }
                        });

                        // Section D: IT Checklist
                        col.Item().PaddingTop(8).Text("D. Checklist for IT Personal to follow")
                            .FontSize(10).Bold().BackgroundColor(Colors.Grey.Lighten3);

                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(20);
                                c.RelativeColumn(4);
                                c.RelativeColumn(3);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Border(1).Padding(2).Text("No.").Bold().FontSize(6);
                                header.Cell().Border(1).Padding(2).Text("Particulars").Bold().FontSize(6);
                                header.Cell().Border(1).Padding(2).AlignCenter().Text("Offboarding Phase").Bold().FontSize(6);
                            });

                            var checkItems = new (string Name, bool Value, string Label)[] {
                                ("Physical Checks & Clean on IT Equipment", request.CheckPhysicalClean, "Checks complete"),
                                ("MS O365 Account — Deactivate completely", request.CheckO365Deactivate, "Deactivate completely"),
                                ("Windows Account & Login — Disable completely", request.CheckWindowsDisable, "Disable completely"),
                                ("Outlook & Email — Sign-out & forwarded", request.CheckEmailSignOut, "Sign-out & forwarded"),
                                ("Specific Software / Application — Deactivate", request.CheckSpecificSoftwareDeactivate, "Deactivate completely"),
                                ("General Software / Application — Deactivate", request.CheckGeneralSoftwareDeactivate, "Deactivate completely"),
                                ("Remove user from ADs Group", request.CheckAbcGroupRemove, "Remove completely"),
                                ("Remove user from Email Mail Group", request.CheckEmailMailGroupRemove, "Remove completely"),
                                ("Device Name & Domain — Disjoin", request.CheckDomainDisjoin, "Disjoin successfully"),
                                ("Shared Drive mapping — Remove", request.CheckSharedDriveRemove, "Remove completely"),
                                ("Primary Location Wi-Fi — Disable", request.CheckWiFiDisable, "Disable successfully"),
                                ("Primary Location Printer — Remove", request.CheckPrinterRemove, "Remove completely"),
                                ("Primary Location Scan Folder — Remove", request.CheckScanFolderRemove, "Remove completely"),
                                ("VPN Account — Disable", request.CheckVPNDisable, "Disable completely")
                            };

                            for (int i = 0; i < checkItems.Length; i++)
                            {
                                table.Cell().Border(1).Padding(1).Text($"{i + 1}").FontSize(6);
                                table.Cell().Border(1).Padding(1).Text(checkItems[i].Name).FontSize(6);
                                table.Cell().Border(1).Padding(1).AlignCenter()
                                    .Text($"{(checkItems[i].Value ? "✓" : "")} {checkItems[i].Label}").FontSize(6);
                            }
                        });

                        if (!string.IsNullOrEmpty(request.ChecklistRemarks))
                        {
                            col.Item().PaddingTop(2).Text($"Remarks: {request.ChecklistRemarks}")
                                .FontSize(7).Italic();
                        }

                        // Section E: Follow-up Offboarding Actions
                        col.Item().PaddingTop(8).Text("E. Follow-up Offboarding Actions")
                            .FontSize(10).Bold().BackgroundColor(Colors.Grey.Lighten3);

                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(20);
                                c.RelativeColumn(5);
                                c.RelativeColumn(2);
                            });

                            var followUp = new (string Name, bool Value)[] {
                                ("Company Data Backup", request.DataBackupDone),
                                ("Email Backup & Automatic Reply setup", request.EmailBackupDone),
                                ("Email Forwarding Setup", request.EmailForwardingDone),
                                ("Removal of assigned licenses", request.LicenseRemovalDone)
                            };

                            for (int i = 0; i < followUp.Length; i++)
                            {
                                table.Cell().Border(1).Padding(2).Text($"{i + 1}").FontSize(7);
                                table.Cell().Border(1).Padding(2).Text(followUp[i].Name).FontSize(7);
                                table.Cell().Border(1).Padding(2).AlignCenter()
                                    .Text(followUp[i].Value ? "✓ Task complete" : "Pending").FontSize(7);
                            }
                        });

                        if (!string.IsNullOrEmpty(request.FollowUpRemarks))
                        {
                            col.Item().PaddingTop(2).Text($"Remarks: {request.FollowUpRemarks}")
                                .FontSize(7).Italic();
                        }

                        // Clearance Acknowledgment
                        col.Item().PaddingTop(8).Text("Clearance Acknowledgment")
                            .FontSize(9).Bold();
                        col.Item().PaddingTop(2).Text(
                            "By signing this form, I hereby acknowledge that I have completed the Employee Clearance Form and understand " +
                            "the responsibilities associated with the clearance process at YTC Precast. I confirm that I have returned all " +
                            "company property, settled any outstanding obligations, and had no further claims against the company.")
                            .FontSize(6).Italic().FontColor(Colors.Grey.Darken1);

                        // Section F: Signatures
                        col.Item().PaddingTop(8).Text("F. Authorization Signatures")
                            .FontSize(10).Bold().BackgroundColor(Colors.Grey.Lighten3);

                        col.Item().PaddingTop(5).Column(sigCol =>
                        {
                            sigCol.Item().Row(row =>
                            {
                                AddVerifiedSignatureBlock(row, "HR Signature", request.HRSignature, request.HRSignatureHmac, "OffboardingRequest", request.Id.ToString(), request.HRSignerName ?? "Unknown", request.HRSignedAt);
                                row.ConstantItem(10);
                                AddVerifiedSignatureBlock(row, "IT Personnel's Signature", request.ITAdminSignature, request.ITAdminSignatureHmac, "OffboardingRequest", request.Id.ToString(), request.ITAdminSignerName ?? "Unknown", request.ITAdminSignedAt);
                                row.ConstantItem(10);
                                AddVerifiedSignatureBlock(row, "Manager's Signature", request.HODSignature, request.HODSignatureHmac, "OffboardingRequest", request.Id.ToString(), request.HODSignerName ?? "Unknown", request.HODSignedAt);
                            });

                            sigCol.Item().PaddingTop(15).Row(row =>
                            {
                                AddVerifiedSignatureBlock(row, $"Onboarding Acknowledgement: {request.OnboardingRequest?.EmployeeSignerName ?? request.OnboardingRequest?.FullName ?? "Unknown"}", request.OnboardingRequest?.EmployeeSignature, request.OnboardingRequest?.EmployeeSignatureHmac, "OnboardingRequest", request.OnboardingRequest?.Id.ToString() ?? "0", request.OnboardingRequest?.EmployeeSignerName ?? "Employee", request.OnboardingRequest?.EmployeeSignedAt);
                                row.ConstantItem(15);
                                AddVerifiedSignatureBlock(row, $"Offboarding Acknowledgement: {request.EmployeeSignerName ?? request.FullName ?? "Unknown"}", request.EmployeeSignature, request.EmployeeSignatureHmac, "OffboardingRequest", request.Id.ToString(), request.EmployeeSignerName ?? "Employee", request.EmployeeSignedAt);
                            });
                        });
                    });

                    page.Footer().AlignCenter()
                        .Text("YTC IT Department - Confidential | Page 1 of 1")
                        .FontSize(7).FontColor(Colors.Grey.Medium);
                });
            });

            var pdfBytes = document.GeneratePdf();
            _logger.LogInformation("Offboarding PDF generated successfully ({Size} bytes)", pdfBytes.Length);
            return pdfBytes;
        }
    }
}

