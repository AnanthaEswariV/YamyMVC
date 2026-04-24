using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SkiaSharp;
namespace YamyProject.Core.Models
{
    public static class SapInvoicePdfGenerator
    {
        // ── Colours matched from letterhead .docx ────────────────────────────────
        private const string BarGreen = "#a5b592";
        private const string BarBlue = "#809ec2";
        private const string BarYellow = "#e7bc29";
        private const string BarDark = "#444d26";
        private const string LineBlue = "#4472C4";
        private const string StripGreen = "#70AD47";
        private const string StripGray = "#A5A5A5";

        private static byte[] GetQrCode(byte[] qrCode)
        {
            if (qrCode != null && qrCode.Length > 0) return qrCode;
            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot", "assets", "images", "DefaultQR.jpg");
            return System.IO.File.Exists(path)
                ? System.IO.File.ReadAllBytes(path)
                : null;
        }

        // ─────────────────────────────────────────────────────────────────────────
        public static byte[] Generate(
            CompanyReportDto company,
            SaleReportDto sale,
            List<SaleItemReportDto> items)
        {
            // Logo from tbl_company.logoComp (already byte[] in company.Logo)
            byte[] logo = company.Logo;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginTop(10);
                    page.MarginBottom(8);
                    page.MarginLeft(18);
                    page.MarginRight(18);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    // ══════════════════════════════════════════════════════════
                    // HEADER
                    // Letterhead layout:
                    //   LEFT  → 4 staircase colour bars (decreasing height)
                    //   RIGHT → PRO ART logo (large)
                    //   No company name / address / phone in header
                    // ══════════════════════════════════════════════════════════
                    page.Header().Column(hCol =>
                    {
                        // Fixed height row — NO Extend() inside columns (causes QuestPDF layout error)
                        hCol.Item().Height(95).Row(hRow =>
                        {
                            // Bar 1 – Green (tallest = full 95pt)
                            hRow.ConstantColumn(22).Background(BarGreen);
                            hRow.ConstantColumn(3);

                            // Bar 2 – Blue (68pt)
                            hRow.ConstantColumn(22).Column(c =>
                            {
                                c.Item().Height(68).Background(BarBlue);
                            });
                            hRow.ConstantColumn(3);

                            // Bar 3 – Yellow (48pt)
                            hRow.ConstantColumn(22).Column(c =>
                            {
                                c.Item().Height(48).Background(BarYellow);
                            });
                            hRow.ConstantColumn(3);

                            // Bar 4 – Dark (shortest = 28pt)
                            hRow.ConstantColumn(22).Column(c =>
                            {
                                c.Item().Height(28).Background(BarDark);
                            });

                            // Empty middle
                            hRow.RelativeColumn();

                            // PRO ART Logo – right side
                            hRow.ConstantColumn(140).AlignCenter().AlignMiddle()
                                .Element(el =>
                                {
                                    if (logo != null)
                                        el.MaxHeight(90).MaxWidth(140)
                                          .Image(logo).FitArea();
                                });
                        });

                        // Thin blue line under header
                        hCol.Item().LineHorizontal(1).LineColor(LineBlue);
                    });

                    // ══════════════════════════════════════════════════════════
                    // WATERMARK — very faint logo centred behind content
                    // Uses SkiaSharp Canvas because IContainer has no Opacity()
                    // ══════════════════════════════════════════════════════════
                    // Pre-process logo to 6% opacity using SkiaSharp, then use normal .Image()
                    byte[] watermarkBytes = MakeFaintWatermark(logo, alpha: 15);
                    if (watermarkBytes != null)
                    {
                        page.Foreground()
                            .AlignCenter().AlignMiddle()
                            .Width(240).Height(240)
                            .Image(watermarkBytes).FitArea();
                    }

                    // ══════════════════════════════════════════════════════════
                    // CONTENT
                    // ══════════════════════════════════════════════════════════
                    page.Content().PaddingTop(6).Column(col =>
                    {
                        // Title
                        col.Item().AlignCenter()
                            .Text("TAX INVOICE").Bold().FontSize(14).FontColor("#1F3864");

                        col.Item().AlignCenter().PaddingBottom(4)
                            .Text("Sale").SemiBold().FontSize(10).FontColor("#555555");

                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(LineBlue);

                        // Customer + Bill info
                        col.Item().PaddingBottom(5).Row(row =>
                        {
                            row.RelativeColumn().Column(c =>
                            {
                                InfoLine(c, "Name", sale.CustomerName ?? "");
                                InfoLine(c, "Address", sale.City ?? "");
                                if (!string.IsNullOrEmpty(company.TRN))
                                    InfoLine(c, "TRN", company.TRN);
                            });
                            row.RelativeColumn().Column(c =>
                            {
                                InfoLine(c, "Bill No", sale.InvoiceNo ?? "");
                                InfoLine(c, "Date", sale.Date.ToString("dd-MM-yyyy"));
                                InfoLine(c, "Amount", sale.Total.ToString("N2"));
                                InfoLine(c, "Sales Man", sale.SalesMan ?? "");
                                if (!string.IsNullOrEmpty(sale.PaymentMethod))
                                    InfoLine(c, "Pay Method", sale.PaymentMethod);
                            });
                        });

                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(LineBlue);

                        // Items table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(26);   // S/N
                                cols.RelativeColumn(3);    // Item Name
                                cols.ConstantColumn(42);   // Qty
                                cols.ConstantColumn(42);   // Unit
                                cols.ConstantColumn(52);   // Price
                                cols.ConstantColumn(46);   // Disc
                                cols.ConstantColumn(56);   // Net
                            });

                            table.Header(h =>
                            {
                                TH(h, "S/N", right: false);
                                TH(h, "Item Name", right: false);
                                TH(h, "Qty", right: true);
                                TH(h, "Unit", right: false);
                                TH(h, "Price", right: true);
                                TH(h, "Disc", right: true);
                                TH(h, "Net", right: true);
                            });

                            int i = 1;
                            foreach (var item in items)
                            {
                                string bg = (i % 2 == 0) ? "#EBF3FB" : "#FFFFFF";
                                TD(table, i.ToString(), bg, right: false);
                                TD(table, item.Name ?? "", bg, right: false);
                                TD(table, item.Qty.ToString("N2"), bg, right: true);
                                TD(table, item.UnitName ?? "", bg, right: false);
                                TD(table, item.Price.ToString("N2"), bg, right: true);
                                TD(table, (item.Discount ?? 0).ToString("N2"), bg, right: true);
                                TD(table, item.Total.ToString("N2"), bg, right: true);
                                i++;
                            }
                        });

                        // Totals — wider (240) so "TOTAL AMOUNT" never gets cut off
                        decimal disc = items.Sum(x => x.Discount ?? 0m);
                        col.Item().AlignRight().PaddingTop(6).Width(240).Column(t =>
                        {
                            TotalRow(t, "TOTAL", sale.Total.ToString("N2"), "#F2F2F2", bold: false);
                            TotalRow(t, "TOTAL VAT", sale.Vat.ToString("N2"), "#FFFFFF", bold: false);
                            TotalRow(t, "TOTAL DISCOUNT", disc.ToString("N2"), "#F2F2F2", bold: false);
                            TotalRow(t, "TOTAL AMOUNT", sale.Net.ToString("N2"), LineBlue, bold: true, textColor: "#FFFFFF");
                        });

                        // Signatures
                        col.Item().PaddingTop(14).LineHorizontal(1).LineColor("#CCCCCC");
                        col.Item().PaddingTop(6).Row(sig =>
                        {
                            sig.RelativeColumn().Column(c =>
                            {
                                c.Item().Text("Created By").SemiBold();
                                c.Item().PaddingTop(20).Width(110)
                                    .LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().PaddingTop(2)
                                    .Text("Signature").FontSize(7.5f).FontColor("#888888");
                            });
                            sig.RelativeColumn().AlignRight().Column(c =>
                            {
                                c.Item().AlignRight().Text("Approved By").SemiBold();
                                c.Item().AlignRight().PaddingTop(20).Width(110)
                                    .LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().AlignRight().PaddingTop(2)
                                    .Text("Signature").FontSize(7.5f).FontColor("#888888");
                            });
                        });
                    });

                    // ══════════════════════════════════════════════════════════
                    // FOOTER
                    // Letterhead layout:
                    //   Contact info → RIGHT aligned
                    //   Page number  → far left
                    //   Colour strips → far right (vertical stacked)
                    // ══════════════════════════════════════════════════════════
                    page.Footer().Column(fCol =>
                    {
                        fCol.Item().LineHorizontal(1).LineColor(LineBlue);

                        // Fixed height footer row so strips can fill full height
                        fCol.Item().Height(62).Row(fRow =>
                        {
                            // Page number – bottom left
                            fRow.ConstantColumn(30).AlignBottom().PaddingBottom(3)
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(s => s.FontSize(7f).FontColor("#999999"));
                                    t.CurrentPageNumber();
                                    t.Span(" / ");
                                    t.TotalPages();
                                });

                            // Contact info – right aligned, matches letterhead footer exactly
                            // Shows: LandLine | Phone | Email | Website | Address
                            fRow.RelativeColumn().AlignRight().AlignMiddle()
                                .Column(contact =>
                                {
                                    if (!string.IsNullOrEmpty(company.Phone))
                                        contact.Item().AlignRight()
                                            .Text($"Land Line: {company.Phone}")
                                            .FontSize(7.5f).FontColor("#333333");
                                    if (!string.IsNullOrEmpty(company.Phone2))
                                        contact.Item().AlignRight()
                                            .Text($"Phone: {company.Phone2}")
                                            .FontSize(7.5f).FontColor("#333333");
                                    if (!string.IsNullOrEmpty(company.Email))
                                        contact.Item().AlignRight()
                                            .Text(company.Email)
                                            .FontSize(7.5f).FontColor("#333333");
                                    if (!string.IsNullOrEmpty(company.Website))
                                        contact.Item().AlignRight()
                                            .Text(company.Website)
                                            .FontSize(7.5f).FontColor("#333333");
                                    if (!string.IsNullOrEmpty(company.Address))
                                        contact.Item().AlignRight()
                                            .Text(company.Address)
                                            .FontSize(7.5f).FontColor("#333333");
                                });

                            fRow.ConstantColumn(8);

                            // Colour strips – staircase: tallest LEFT → shortest RIGHT
                            // Reversed order so left=big, right=small (matching letterhead)
                            fRow.ConstantColumn(14).Column(c =>
                            {
                                c.Item().PaddingTop(42).Height(20).Background(BarDark); // shortest left
                            });
                            fRow.ConstantColumn(3);
                            fRow.ConstantColumn(14).Column(c =>
                            {
                                c.Item().PaddingTop(28).Height(34).Background(StripGray);
                            });
                            fRow.ConstantColumn(3);
                            fRow.ConstantColumn(14).Column(c =>
                            {
                                c.Item().PaddingTop(14).Height(48).Background(StripGreen);
                            });
                            fRow.ConstantColumn(3);
                            fRow.ConstantColumn(14).Column(c =>
                            {
                                c.Item().PaddingTop(0).Height(62).Background(LineBlue);   // tallest right
                            });
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ── Watermark pre-processor ──────────────────────────────────────────────────
        // Renders logo into a new RGBA bitmap at reduced alpha using SkiaSharp,
        // returns PNG bytes — no QuestPDF Canvas API needed.
        private static byte[] MakeFaintWatermark(byte[] imageBytes, byte alpha = 15)
        {
            if (imageBytes == null || imageBytes.Length == 0) return null;
            try
            {
                using var original = SKBitmap.Decode(imageBytes);
                if (original == null) return null;

                using var bmp = new SKBitmap(original.Width, original.Height,
                                             SKColorType.Rgba8888, SKAlphaType.Premul);
                using var skCanvas = new SKCanvas(bmp);
                skCanvas.Clear(SKColors.Transparent);

                using var paint = new SKPaint
                {
                    ColorFilter = SKColorFilter.CreateBlendMode(
                        SKColors.White.WithAlpha(alpha),
                        SKBlendMode.DstIn)
                };
                skCanvas.DrawBitmap(original, 0, 0, paint);

                using var img = SKImage.FromBitmap(bmp);
                using var data = img.Encode(SKEncodedImageFormat.Png, 100);
                return data?.ToArray();
            }
            catch { return null; }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static void InfoLine(ColumnDescriptor col, string label, string value)
        {
            col.Item().PaddingBottom(2).Text(t =>
            {
                t.Span($"{label}: ").Bold().FontSize(8.5f);
                t.Span(value).FontSize(8.5f);
            });
        }

        private static void TH(TableCellDescriptor h, string text, bool right)
        {
            var cell = h.Cell().Background(LineBlue)
                .PaddingVertical(4).PaddingHorizontal(3);
            var txt = cell.Text(text).Bold().FontSize(8).FontColor("#FFFFFF");
            if (right) txt.AlignRight();
        }

        private static void TD(TableDescriptor table, string text, string bg, bool right)
        {
            var cell = table.Cell().Background(bg)
                .BorderBottom(1).BorderColor("#DDDDDD")
                .PaddingVertical(3).PaddingHorizontal(3);
            var txt = cell.Text(text).FontSize(8);
            if (right) txt.AlignRight();
        }

        private static void TotalRow(
            ColumnDescriptor col, string label, string value,
            string bg, bool bold, string textColor = "#000000")
        {
            col.Item().Background(bg).PaddingVertical(3).PaddingHorizontal(5)
                .Row(r =>
                {
                    var lbl = r.RelativeColumn().Text(label).FontSize(8).FontColor(textColor);
                    var val = r.ConstantColumn(72).AlignRight().Text(value).FontSize(8).FontColor(textColor);
                    if (bold) { lbl.Bold(); val.Bold(); }
                });
        }
    }
    public static class SalesQuotationPdfGenerator
    {
        public static byte[] Generate(
            CompanyReportDto company,
            SaleReportDto sale,
            List<SaleItemReportDto> items)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // ================= CONTENT =================
                    page.Content().Column(col =>
                    {
                        // 🔹 HEADER
                        col.Item().Border(1).Padding(10).Row(row =>
                        {
                            // LEFT – Company Info
                            row.RelativeColumn(3).Column(c =>
                            {
                                c.Item().Text(company.Name).Bold().FontSize(11);
                                if (!string.IsNullOrEmpty(company.Phone))
                                    c.Item().Text(company.Phone);
                                if (!string.IsNullOrEmpty(company.Email))
                                    c.Item().Text(company.Email);
                                if (!string.IsNullOrEmpty(company.Address))
                                    c.Item().Text(company.Address);
                                if (!string.IsNullOrEmpty(company.TRN))
                                    c.Item().Text($"TRN : {company.TRN}");
                            });

                            // CENTER – Logo
                            row.RelativeColumn(4)
                                .AlignCenter()
                                .AlignMiddle()
                                .Height(70)
                                .Element(el =>
                                {
                                    if (company.Logo != null && company.Logo.Length > 0)
                                        el.Image(company.Logo).FitArea();
                                });

                            // RIGHT – QR
                            row.RelativeColumn(3)
                                .AlignRight()
                                .Height(70)
                                .Image(GetQrCode(company.QrCode))
                                .FitArea();
                        });

                        // 🔹 TITLE
                        col.Item().PaddingVertical(10)
                            .AlignCenter()
                            .Text("SALES QUOTATION")
                            .Bold()
                            .FontSize(14);

                        col.Item().AlignCenter().Text("Quotation").SemiBold();
                        col.Item().PaddingVertical(5).LineHorizontal(1);

                        // 🔹 CUSTOMER + QUOTATION INFO
                        col.Item().Row(row =>
                        {
                            row.RelativeColumn().Column(c =>
                            {
                                c.Item().Text($"Customer : {sale.CustomerName}");
                                if (!string.IsNullOrEmpty(sale.City))
                                    c.Item().Text($"City : {sale.City}");
                            });

                            row.RelativeColumn().Column(c =>
                            {
                                c.Item().Text($"Quotation No : {sale.InvoiceNo}");
                                c.Item().Text($"Date : {sale.Date:dd-MM-yyyy}");
                                if (!string.IsNullOrEmpty(sale.SalesMan))
                                    c.Item().Text($"Sales Man : {sale.SalesMan}");
                            });
                        });

                        col.Item().PaddingVertical(10).LineHorizontal(1);

                        // 🔹 ITEMS TABLE
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn(3);
                                columns.ConstantColumn(40);
                                columns.ConstantColumn(40);
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(40);
                                columns.ConstantColumn(60);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("S/N").Bold();
                                h.Cell().Text("Item Name").Bold();
                                h.Cell().Text("Qty").Bold();
                                h.Cell().Text("Unit").Bold();
                                h.Cell().Text("Price").Bold();
                                h.Cell().Text("Disc").Bold();
                                h.Cell().Text("Total").Bold();
                            });

                            int i = 1;
                            foreach (var item in items)
                            {
                                table.Cell().Text(i++.ToString());
                                table.Cell().Text(item.Name);
                                table.Cell().Text(item.Qty.ToString("N2"));
                                table.Cell().Text(item.UnitName);
                                table.Cell().Text(item.Price.ToString("N2"));
                                table.Cell().Text(item.Discount?.ToString("N2"));
                                table.Cell().Text(item.Total.ToString("N2"));
                            }
                        });

                        // 🔹 TOTALS
                        col.Item().AlignRight().PaddingTop(15).Column(c =>
                        {
                            c.Item().Text($"Sub Total : {sale.Total:N2}");
                            c.Item().Text($"VAT : {sale.Vat:N2}");
                            c.Item().Text($"Total Discount : {items.Sum(x => x.Discount):N2}");
                            c.Item().Text($"Grand Total : {sale.Net:N2}").Bold();
                        });

                        // 🔹 FOOTER
                        page.Footer().PaddingTop(20).Row(row =>
                        {
                            row.RelativeColumn().Column(c =>
                            {
                                c.Item().Text("Prepared By").SemiBold();
                                c.Item().PaddingTop(20).Text("Signature");
                            });

                            row.RelativeColumn().AlignRight().Column(c =>
                            {
                                c.Item().Text("Approved By").SemiBold().AlignRight();
                                c.Item().PaddingTop(20).AlignRight().Text("Signature");
                            });
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ================= DEFAULT QR HANDLER =================
        private static byte[] GetQrCode(byte[] qrCode)
        {
            if (qrCode != null && qrCode.Length > 0)
                return qrCode;

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "assets",
                "images",
                "DefaultQR.jpg");

            return File.Exists(path)
                ? File.ReadAllBytes(path)
                : null;
        }
    }

    public static class SalesOrderPdfGenerator
    {
        public static byte[] Generate(
            CompanyReportDto company,
            SaleReportDto sale,
            List<SaleItemReportDto> items)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Content().Column(col =>
                    {
                        // ================= HEADER =================
                        col.Item().Border(1).Padding(10).Row(row =>
                        {
                            // LEFT – Company Info
                            row.RelativeColumn(3).Column(c =>
                            {
                                c.Item().Text(company.Name).Bold().FontSize(11);
                                if (!string.IsNullOrEmpty(company.Phone))
                                    c.Item().Text(company.Phone);
                                if (!string.IsNullOrEmpty(company.Email))
                                    c.Item().Text(company.Email);
                                if (!string.IsNullOrEmpty(company.Address))
                                    c.Item().Text(company.Address);
                                if (!string.IsNullOrEmpty(company.TRN))
                                    c.Item().Text($"TRN : {company.TRN}");
                            });

                            // CENTER – Logo
                            row.RelativeColumn(4)
                                .AlignCenter()
                                .AlignMiddle()
                                .Height(70)
                                .Element(el =>
                                {
                                    if (company.Logo != null && company.Logo.Length > 0)
                                        el.Image(company.Logo).FitArea();
                                });

                            // RIGHT – QR
                            row.RelativeColumn(3)
                                .AlignRight()
                                .Height(70)
                                .Image(GetQrCode(company.QrCode))
                                .FitArea();
                        });

                        // ================= TITLE =================
                        col.Item().PaddingVertical(10)
                            .AlignCenter()
                            .Text("SALES ORDER")
                            .Bold()
                            .FontSize(14);

                        col.Item().AlignCenter().Text("Sales Order").SemiBold();
                        col.Item().PaddingVertical(5).LineHorizontal(1);

                        // ================= CUSTOMER + ORDER INFO =================
                        col.Item().Row(row =>
                        {
                            row.RelativeColumn().Column(c =>
                            {
                                c.Item().Text($"Customer : {sale.CustomerName}");
                                if (!string.IsNullOrEmpty(sale.City))
                                    c.Item().Text($"City : {sale.City}");
                            });

                            row.RelativeColumn().Column(c =>
                            {
                                c.Item().Text($"Order No : {sale.InvoiceNo}");
                                c.Item().Text($"Date : {sale.Date:dd-MM-yyyy}");
                                if (!string.IsNullOrEmpty(sale.SalesMan))
                                    c.Item().Text($"Sales Man : {sale.SalesMan}");
                            });
                        });

                        col.Item().PaddingVertical(10).LineHorizontal(1);

                        // ================= ITEMS TABLE =================
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn(3);
                                columns.ConstantColumn(40);
                                columns.ConstantColumn(40);
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(40);
                                columns.ConstantColumn(60);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("S/N").Bold();
                                h.Cell().Text("Item Name").Bold();
                                h.Cell().Text("Qty").Bold();
                                h.Cell().Text("Unit").Bold();
                                h.Cell().Text("Price").Bold();
                                h.Cell().Text("Disc").Bold();
                                h.Cell().Text("Total").Bold();
                            });

                            int i = 1;
                            foreach (var item in items)
                            {
                                table.Cell().Text(i++.ToString());
                                table.Cell().Text(item.Name);
                                table.Cell().Text(item.Qty.ToString("N2"));
                                table.Cell().Text(item.UnitName);
                                table.Cell().Text(item.Price.ToString("N2"));
                                table.Cell().Text(item.Discount?.ToString("N2"));
                                table.Cell().Text(item.Total.ToString("N2"));
                            }
                        });

                        // ================= TOTALS =================
                        col.Item().AlignRight().PaddingTop(15).Column(c =>
                        {
                            c.Item().Text($"Sub Total : {sale.Total:N2}");
                            c.Item().Text($"VAT : {sale.Vat:N2}");
                            c.Item().Text($"Total Discount : {items.Sum(x => x.Discount):N2}");
                            c.Item().Text($"Grand Total : {sale.Net:N2}").Bold();
                        });

                        // ================= FOOTER =================
                        //page.Footer().PaddingTop(20).Row(row =>
                        //{
                        //    row.RelativeColumn().Column(c =>
                        //    {
                        //        c.Item().Text("Prepared By").SemiBold();
                        //        c.Item().PaddingTop(20).Text("Signature");
                        //    });

                        //    row.RelativeColumn().AlignRight().Column(c =>
                        //    {
                        //        c.Item().Text("Approved By").SemiBold().AlignRight();
                        //        c.Item().PaddingTop(20).AlignRight().Text("Signature");
                        //    });
                        //});
                    });
                });
            }).GeneratePdf();
        }

        // ================= DEFAULT QR HANDLER =================
        private static byte[] GetQrCode(byte[] qrCode)
        {
            if (qrCode != null && qrCode.Length > 0)
                return qrCode;

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "assets",
                "images",
                "DefaultQR.jpg");

            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
    }

    public static class SalesProformaPdfGenerator
    {
        public static byte[] Generate(
            CompanyReportDto company,
            SaleReportDto sale,
            List<SaleItemReportDto> items)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Content().Column(col =>
                    {
                        // ================= HEADER =================
                        col.Item().Border(1).Padding(10).Row(row =>
                        {
                            // LEFT – Company Info
                            row.RelativeColumn(3).Column(c =>
                            {
                                c.Item().Text(company.Name).Bold().FontSize(11);
                                if (!string.IsNullOrEmpty(company.Phone))
                                    c.Item().Text(company.Phone);
                                if (!string.IsNullOrEmpty(company.Email))
                                    c.Item().Text(company.Email);
                                if (!string.IsNullOrEmpty(company.Address))
                                    c.Item().Text(company.Address);
                                if (!string.IsNullOrEmpty(company.TRN))
                                    c.Item().Text($"TRN : {company.TRN}");
                            });

                            // CENTER – Logo
                            row.RelativeColumn(4)
                                .AlignCenter()
                                .AlignMiddle()
                                .Height(70)
                                .Element(el =>
                                {
                                    if (company.Logo != null && company.Logo.Length > 0)
                                        el.Image(company.Logo).FitArea();
                                });

                            // RIGHT – QR
                            row.RelativeColumn(3)
                                .AlignRight()
                                .Height(70)
                                .Image(GetQrCode(company.QrCode))
                                .FitArea();
                        });

                        // ================= TITLE =================
                        col.Item().PaddingVertical(10)
                            .AlignCenter()
                            .Text("SALES PROFORMA INVOICE")
                            .Bold()
                            .FontSize(14);

                        col.Item().AlignCenter().Text("Proforma Invoice").SemiBold();
                        col.Item().PaddingVertical(5).LineHorizontal(1);

                        // ================= CUSTOMER + PROFORMA INFO =================
                        col.Item().Row(row =>
                        {
                            row.RelativeColumn().Column(c =>
                            {
                                c.Item().Text($"Customer : {sale.CustomerName}");
                                if (!string.IsNullOrEmpty(sale.City))
                                    c.Item().Text($"City : {sale.City}");
                            });

                            row.RelativeColumn().Column(c =>
                            {
                                c.Item().Text($"Proforma No : {sale.InvoiceNo}");
                                c.Item().Text($"Date : {sale.Date:dd-MM-yyyy}");
                                if (!string.IsNullOrEmpty(sale.SalesMan))
                                    c.Item().Text($"Sales Man : {sale.SalesMan}");
                            });
                        });

                        col.Item().PaddingVertical(10).LineHorizontal(1);

                        // ================= ITEMS TABLE =================
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn(3);
                                columns.ConstantColumn(40);
                                columns.ConstantColumn(40);
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(40);
                                columns.ConstantColumn(60);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("S/N").Bold();
                                h.Cell().Text("Item Name").Bold();
                                h.Cell().Text("Qty").Bold();
                                h.Cell().Text("Unit").Bold();
                                h.Cell().Text("Price").Bold();
                                h.Cell().Text("Disc").Bold();
                                h.Cell().Text("Total").Bold();
                            });

                            int i = 1;
                            foreach (var item in items)
                            {
                                table.Cell().Text(i++.ToString());
                                table.Cell().Text(item.Name);
                                table.Cell().Text(item.Qty.ToString("N2"));
                                table.Cell().Text(item.UnitName);
                                table.Cell().Text(item.Price.ToString("N2"));
                                table.Cell().Text(item.Discount?.ToString("N2"));
                                table.Cell().Text(item.Total.ToString("N2"));
                            }
                        });

                        // ================= TOTALS =================
                        col.Item().AlignRight().PaddingTop(15).Column(c =>
                        {
                            c.Item().Text($"Sub Total : {sale.Total:N2}");
                            c.Item().Text($"VAT : {sale.Vat:N2}");
                            c.Item().Text($"Total Discount : {items.Sum(x => x.Discount):N2}");
                            c.Item().Text($"Grand Total : {sale.Net:N2}").Bold();
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ================= DEFAULT QR HANDLER =================
        private static byte[] GetQrCode(byte[] qrCode)
        {
            if (qrCode != null && qrCode.Length > 0)
                return qrCode;

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "assets",
                "images",
                "DefaultQR.jpg");

            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
    }

    public static class SalesReturnPdfGenerator
    {
        public static byte[] Generate(
            CompanyReportDto company,
            SaleReportDto sale,
            List<SaleItemReportDto> items)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Content().Column(col =>
                    {
                        // ================= HEADER =================
                        col.Item().Border(1).Padding(10).Row(row =>
                        {
                            // LEFT – Company Info
                            row.RelativeColumn(3).Column(c =>
                            {
                                c.Item().Text(company.Name).Bold().FontSize(11);
                                if (!string.IsNullOrEmpty(company.Phone))
                                    c.Item().Text(company.Phone);
                                if (!string.IsNullOrEmpty(company.Email))
                                    c.Item().Text(company.Email);
                                if (!string.IsNullOrEmpty(company.Address))
                                    c.Item().Text(company.Address);
                                if (!string.IsNullOrEmpty(company.TRN))
                                    c.Item().Text($"TRN : {company.TRN}");
                            });

                            // CENTER – Logo
                            row.RelativeColumn(4)
                                .AlignCenter()
                                .AlignMiddle()
                                .Height(70)
                                .Element(el =>
                                {
                                    if (company.Logo != null && company.Logo.Length > 0)
                                        el.Image(company.Logo).FitArea();
                                });

                            // RIGHT – QR
                            row.RelativeColumn(3)
                                .AlignRight()
                                .Height(70)
                                .Image(GetQrCode(company.QrCode))
                                .FitArea();
                        });

                        // ================= TITLE =================
                        col.Item().PaddingVertical(10)
                            .AlignCenter()
                            .Text("SALES RETURN INVOICE")
                            .Bold()
                            .FontSize(14);

                        col.Item().AlignCenter().Text("Sales Return").SemiBold();
                        col.Item().PaddingVertical(5).LineHorizontal(1);

                        // ================= CUSTOMER + INVOICE INFO =================
                        col.Item().Row(row =>
                        {
                            row.RelativeColumn().Column(c =>
                            {
                                c.Item().Text($"Customer : {sale.CustomerName}");
                                if (!string.IsNullOrEmpty(sale.City))
                                    c.Item().Text($"City : {sale.City}");
                            });

                            row.RelativeColumn().Column(c =>
                            {
                                c.Item().Text($"Invoice No : {sale.InvoiceNo}");
                                c.Item().Text($"Date : {sale.Date:dd-MM-yyyy}");
                                if (!string.IsNullOrEmpty(sale.SalesMan))
                                    c.Item().Text($"Sales Man : {sale.SalesMan}");
                            });
                        });

                        col.Item().PaddingVertical(10).LineHorizontal(1);

                        // ================= ITEMS TABLE =================
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn(3);
                                columns.ConstantColumn(40);
                                columns.ConstantColumn(40);
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(40);
                                columns.ConstantColumn(60);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("S/N").Bold();
                                h.Cell().Text("Item Name").Bold();
                                h.Cell().Text("Qty").Bold();
                                h.Cell().Text("Unit").Bold();
                                h.Cell().Text("Price").Bold();
                                h.Cell().Text("Disc").Bold();
                                h.Cell().Text("Total").Bold();
                            });

                            int i = 1;
                            foreach (var item in items)
                            {
                                table.Cell().Text(i++.ToString());
                                table.Cell().Text(item.Name);
                                table.Cell().Text(item.Qty.ToString("N2"));
                                table.Cell().Text(item.UnitName);
                                table.Cell().Text(item.Price.ToString("N2"));
                                table.Cell().Text(item.Discount?.ToString("N2"));
                                table.Cell().Text(item.Total.ToString("N2"));
                            }
                        });

                        // ================= TOTALS =================
                        col.Item().AlignRight().PaddingTop(15).Column(c =>
                        {
                            c.Item().Text($"Sub Total : {sale.Total:N2}");
                            c.Item().Text($"VAT : {sale.Vat:N2}");
                            c.Item().Text($"Total Discount : {items.Sum(x => x.Discount):N2}");
                            c.Item().Text($"Grand Total : {sale.Net:N2}").Bold();
                        });

                    });
                });
            }).GeneratePdf();
        }

        // ================= DEFAULT QR HANDLER =================
        private static byte[] GetQrCode(byte[] qrCode)
        {
            if (qrCode != null && qrCode.Length > 0)
                return qrCode;

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "assets",
                "images",
                "DefaultQR.jpg");

            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
    }

    public static class PurchaseInvoicePdfGenerator
    {
        public static byte[] Generate(
            CompanyReportDto company,
            PurchaseReportDto purchase,
            List<PurchaseItemReportDto> items)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Content().Column(col =>
                    {
                        // ================= HEADER =================
                        col.Item().Border(1).Padding(10).Row(row =>
                        {
                            // LEFT – Company Info
                            row.RelativeColumn(3).Column(c =>
                            {
                                c.Item().Text(company.Name).Bold().FontSize(11);
                                if (!string.IsNullOrEmpty(company.Phone))
                                    c.Item().Text(company.Phone);
                                if (!string.IsNullOrEmpty(company.Email))
                                    c.Item().Text(company.Email);
                                if (!string.IsNullOrEmpty(company.Address))
                                    c.Item().Text(company.Address);
                                if (!string.IsNullOrEmpty(company.TRN))
                                    c.Item().Text($"TRN : {company.TRN}");
                            });

                            // CENTER – Logo
                            row.RelativeColumn(4)
                                .AlignCenter()
                                .AlignMiddle()
                                .Height(70)
                                .Element(el =>
                                {
                                    if (company.Logo?.Length > 0)
                                        el.Image(company.Logo).FitArea();
                                });

                            // RIGHT – Empty / QR
                            row.RelativeColumn(3);
                        });

                        // ================= TITLE =================
                        col.Item().PaddingVertical(10)
                            .AlignCenter()
                            .Text("PURCHASE INVOICE")
                            .Bold()
                            .FontSize(14);

                        col.Item().AlignCenter().Text("Purchase Invoice").SemiBold();
                        col.Item().PaddingVertical(5).LineHorizontal(1);

                        // ================= VENDOR + INVOICE INFO =================
                        col.Item().Row(row =>
                        {
                            row.RelativeColumn().Column(c =>
                            {
                                c.Item().Text($"Vendor : {purchase.VendorName}");
                                if (!string.IsNullOrEmpty(purchase.City))
                                    c.Item().Text($"City : {purchase.City}");
                                if (!string.IsNullOrEmpty(purchase.VendorMobile))
                                    c.Item().Text($"Mobile : {purchase.VendorMobile}");
                                if (!string.IsNullOrEmpty(purchase.VendorTRN))
                                    c.Item().Text($"TRN : {purchase.VendorTRN}");
                            });

                            row.RelativeColumn().Column(c =>
                            {
                                c.Item().Text($"Invoice No : {purchase.InvoiceNo}");
                                c.Item().Text($"Date : {purchase.Date:dd-MM-yyyy}");
                                if (!string.IsNullOrEmpty(purchase.PaymentMethod))
                                    c.Item().Text($"Payment : {purchase.PaymentMethod}");
                            });
                        });

                        col.Item().PaddingVertical(10).LineHorizontal(1);

                        // ================= ITEMS TABLE =================
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn(3);
                                columns.ConstantColumn(45);
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(45);
                                columns.ConstantColumn(60);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("S/N").Bold();
                                h.Cell().Text("Item").Bold();
                                h.Cell().Text("Qty").Bold();
                                h.Cell().Text("Unit").Bold();
                                h.Cell().Text("Price").Bold();
                                h.Cell().Text("Disc").Bold();
                                h.Cell().Text("Total").Bold();
                            });

                            int i = 1;
                            foreach (var item in items)
                            {
                                table.Cell().Text(i++.ToString());
                                table.Cell().Text(item.Name);
                                table.Cell().Text(item.Qty.ToString("N2"));
                                table.Cell().Text(item.UnitName);
                                table.Cell().Text(item.Price.ToString("N2"));
                                table.Cell().Text(item.Discount.ToString("N2"));
                                table.Cell().Text(item.Total.ToString("N2"));
                            }
                        });

                        // ================= TOTALS =================
                        col.Item().AlignRight().PaddingTop(15).Column(c =>
                        {
                            c.Item().Text($"Sub Total : {purchase.Total:N2}");
                            c.Item().Text($"VAT : {purchase.Vat:N2}");
                            c.Item().Text($"Total Discount : {items.Sum(x => x.Discount):N2}");
                            c.Item().Text($"Grand Total : {purchase.Net:N2}").Bold();
                        });
                    });
                });
            }).GeneratePdf();
        }
    }

    public static class PurchaseReceiveNotePdfGenerator
    {
        public static byte[] Generate(
            CompanyReportDto company,
            PurchaseReportDto purchase,
            List<PurchaseItemReportDto> items)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Content().Column(col =>
                    {
                        // ================= HEADER =================
                        col.Item().Border(1).Padding(10).Row(row =>
                        {
                            row.RelativeColumn(3).Column(c =>
                            {
                                c.Item().Text(company.Name).Bold().FontSize(11);
                                if (!string.IsNullOrEmpty(company.Phone))
                                    c.Item().Text(company.Phone);
                                if (!string.IsNullOrEmpty(company.Address))
                                    c.Item().Text(company.Address);
                            });

                            row.RelativeColumn(4)
                                .AlignCenter()
                                .AlignMiddle()
                                .Height(70)
                                .Element(el =>
                                {
                                    if (company.Logo?.Length > 0)
                                        el.Image(company.Logo).FitArea();
                                });

                            row.RelativeColumn(3);
                        });

                        // ================= TITLE =================
                        col.Item().PaddingVertical(10)
                            .AlignCenter()
                            .Text("RECEIVED NOTE")
                            .Bold()
                            .FontSize(14);

                        col.Item().AlignCenter().Text("Receiver Note").SemiBold();
                        col.Item().PaddingVertical(5).LineHorizontal(1);

                        // ================= VENDOR INFO =================
                        col.Item().Row(row =>
                        {
                            row.RelativeColumn().Column(c =>
                            {
                                c.Item().Text($"Vendor : {purchase.VendorName}");
                                if (!string.IsNullOrEmpty(purchase.VendorMobile))
                                    c.Item().Text($"Mobile : {purchase.VendorMobile}");
                            });

                            row.RelativeColumn().Column(c =>
                            {
                                c.Item().Text($"GRN No : {purchase.InvoiceNo}");
                                c.Item().Text($"Date : {purchase.Date:dd-MM-yyyy}");
                            });
                        });

                        col.Item().PaddingVertical(10).LineHorizontal(1);

                        // ================= ITEMS TABLE =================
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40);
                                columns.RelativeColumn(4);
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(80);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("S/N").Bold();
                                h.Cell().Text("Item Name").Bold();
                                h.Cell().Text("Qty").Bold();
                                h.Cell().Text("Unit").Bold();
                            });

                            int i = 1;
                            foreach (var item in items)
                            {
                                table.Cell().Text(i++.ToString());
                                table.Cell().Text(item.Name);
                                table.Cell().Text(item.Qty.ToString("N2"));
                                table.Cell().Text(item.UnitName);
                            }
                        });
                    });
                });
            }).GeneratePdf();
        }
    }

    public static class PurchaseOrderInvoicePdfGenerator
    {
        public static byte[] Generate(
            CompanyReportDto company,
            PurchaseReportDto purchase,
            List<PurchaseItemReportDto> items)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(9));
                    page.Content().Column(col =>
                    {
                        // HEADER: Company info left, Logo center, blank right (or QR if you want)
                        col.Item().Row(row =>
                        {
                            row.RelativeColumn(3).Column(c =>
                            {
                                c.Item().Text(company.Name).Bold().FontSize(11);
                                if (!string.IsNullOrEmpty(company.Phone))
                                    c.Item().Text($"Phone: {company.Phone}");
                                if (!string.IsNullOrEmpty(company.Email))
                                    c.Item().Text($"Email: {company.Email}");
                                if (!string.IsNullOrEmpty(company.Address))
                                    c.Item().Text(company.Address);
                                if (!string.IsNullOrEmpty(company.TRN))
                                    c.Item().Text($"TRN: {company.TRN}");
                            });

                            row.RelativeColumn(4)
                                .AlignCenter()
                                .AlignMiddle()
                                .Height(70)
                                .Element(el =>
                                {
                                    if (company.Logo != null && company.Logo.Length > 0)
                                        el.Image(company.Logo).FitArea();
                                });

                            row.RelativeColumn(3).AlignRight(); // Empty or QR code here if needed
                        });

                        col.Item().PaddingVertical(10).AlignCenter()
                            .Text("Local Purchase Order").Bold().FontSize(16);

                        // PURCHASE INFO section left aligned
                        col.Item().PaddingBottom(10).Column(c =>
                        {
                            c.Item().Text($"No : {purchase.InvoiceNo}").Bold();
                            c.Item().Text($"Tax Registration Number : {company.TRN ?? ""}");
                            c.Item().Text($"Date : {purchase.Date:MM/dd/yyyy}");
                            c.Item().Text($"To M/S : {purchase.VendorName}");
                            c.Item().Text($"Tel No : {purchase.VendorPhone ?? ""}");
                            c.Item().Text($"PROJECT : {purchase.City ?? ""}");
                        });

                        // Instruction note
                        col.Item().Padding(5).Border(1).Text("Please Send Supply the mentioned items below").SemiBold();

                        // ITEMS TABLE
                        col.Item().Table(table =>
                        {
                            // Columns: No., Description, Unit, Qty., Rate, Total
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);      // No.
                                columns.RelativeColumn(5);      // Description
                                columns.RelativeColumn(2);      // Unit
                                columns.ConstantColumn(40);     // Qty.
                                columns.ConstantColumn(50);     // Rate
                                columns.ConstantColumn(50);     // Total
                            });

                            // Table Header
                            table.Header(header =>
                            {
                                header.Cell().Text("No.").Bold().FontSize(10).AlignCenter();
                                header.Cell().Text("DESCRIPTION").Bold().FontSize(10);
                                header.Cell().Text("UNIT").Bold().FontSize(10).AlignCenter();
                                header.Cell().Text("Qty.").Bold().FontSize(10).AlignCenter();
                                header.Cell().Text("Rate").Bold().FontSize(10).AlignRight();
                                header.Cell().Text("Total").Bold().FontSize(10).AlignRight();
                            });

                            int index = 1;
                            foreach (var item in items)
                            {
                                table.Cell().Text(index++.ToString()).AlignCenter();
                                table.Cell().Text(item.Name).Underline();
                                table.Cell().Text(item.UnitName ?? "Unit").AlignCenter();
                                table.Cell().Text(item.Qty.ToString("N0")).AlignCenter();
                                table.Cell().Text(item.CostPrice.ToString("N2")).AlignRight();
                                table.Cell().Text(item.Total.ToString("N2")).AlignRight();
                            }
                        });

                        // Empty space between table and totals block
                        col.Item().PaddingVertical(10);

                        // TOTALS block right aligned with grey background
                        col.Item().AlignRight().Padding(5).Background("#d0d0d0").Width(250).Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                r.RelativeColumn().Text("Total Amount").Bold().AlignRight();
                                r.ConstantColumn(80).Text($"{purchase.Total:N2}").AlignRight();
                            });
                            c.Item().LineHorizontal(1);
                            c.Item().Row(r =>
                            {
                                r.RelativeColumn().Text("Discount").Bold().AlignRight();
                                r.ConstantColumn(80).Text($"{purchase.Pay:N2}").AlignRight();
                            });
                            c.Item().LineHorizontal(1);
                            c.Item().Row(r =>
                            {
                                r.RelativeColumn().Text("Total Amount").Bold().AlignRight();
                                // Calculate total amount after discount if needed or use purchase.Net
                                decimal netAmount = purchase.Total - purchase.Pay;
                                r.ConstantColumn(80).Text($"{netAmount:N2}").AlignRight();
                            });
                            c.Item().LineHorizontal(1);
                            c.Item().Row(r =>
                            {
                                r.RelativeColumn().Text("VAT 5%").Bold().AlignRight();
                                r.ConstantColumn(80).Text($"{purchase.Vat:N2}").AlignRight();
                            });
                            c.Item().LineHorizontal(1);
                            c.Item().Row(r =>
                            {
                                r.RelativeColumn().Text("Grand Total Amount").Bold().FontSize(11).AlignRight();
                                r.ConstantColumn(80).Text($"{purchase.Net:N2}").Bold().FontSize(11).AlignRight();
                            });
                        });

                        // Footer note left aligned
                        col.Item().PaddingTop(10).Text($"Contact Person for Delivery: {purchase.VendorName ?? ""}");
                    });
                });
            }).GeneratePdf();
        }
    }

    public static class PurchaseReturnInvoicePdfGenerator
    {
        public static byte[] Generate(
            CompanyReportDto company,
            PurchaseReportDto purchase,
            List<PurchaseItemReportDto> items)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // ================= CONTENT =================
                    page.Content().Column(col =>
                    {
                        // 🔹 HEADER
                        col.Item().Border(1).Padding(10).Row(row =>
                        {
                            // LEFT – Company Info
                            row.RelativeColumn(3).Column(c =>
                            {
                                c.Item().Text(company.Name).Bold().FontSize(11);
                                if (!string.IsNullOrEmpty(company.Phone))
                                    c.Item().Text(company.Phone);
                                if (!string.IsNullOrEmpty(company.Email))
                                    c.Item().Text(company.Email);
                                if (!string.IsNullOrEmpty(company.Address))
                                    c.Item().Text(company.Address);
                                if (!string.IsNullOrEmpty(company.TRN))
                                    c.Item().Text($"TRN : {company.TRN}");
                            });

                            // CENTER – Logo (small)
                            row.RelativeColumn(4)
                                .AlignCenter()
                                .AlignMiddle()
                                .Height(70)
                                .Element(el =>
                                {
                                    if (company.Logo != null && company.Logo.Length > 0)
                                        el.Image(company.Logo).FitArea();
                                });

                            // RIGHT – QR (default if null)
                            row.RelativeColumn(3)
                                .AlignRight()
                                .Height(70)
                                .Image(GetQrCode(company.QrCode))
                                .FitArea();
                        });

                        // 🔹 TITLE
                        col.Item().PaddingVertical(10)
                            .AlignCenter()
                            .Text("PURCHASE RETURN INVOICE")
                            .Bold()
                            .FontSize(14);

                        col.Item().AlignCenter().Text("Purchase Return").SemiBold();
                        col.Item().PaddingVertical(5).LineHorizontal(1);

                        // 🔹 VENDOR + PURCHASE INFO
                        col.Item().Row(row =>
                        {
                            row.RelativeColumn().Column(c =>
                            {
                                c.Item().Text($"Vendor Name : {purchase.VendorName}");
                                if (!string.IsNullOrEmpty(purchase.BillTo))
                                    c.Item().Text($"Bill To : {purchase.BillTo}");
                                if (!string.IsNullOrEmpty(purchase.City))
                                    c.Item().Text($"City : {purchase.City}");
                                if (!string.IsNullOrEmpty(purchase.VendorPhone))
                                    c.Item().Text($"Phone : {purchase.VendorPhone}");
                                if (!string.IsNullOrEmpty(purchase.VendorEmail))
                                    c.Item().Text($"Email : {purchase.VendorEmail}");
                                if (!string.IsNullOrEmpty(purchase.VendorTRN))
                                    c.Item().Text($"TRN : {purchase.VendorTRN}");
                            });

                            row.RelativeColumn().Column(c =>
                            {
                                c.Item().Text($"Invoice No : {purchase.InvoiceNo}");
                                c.Item().Text($"Date : {purchase.Date:dd-MM-yyyy}");
                                if (!string.IsNullOrEmpty(purchase.SalesMan))
                                    c.Item().Text($"Sales Man : {purchase.SalesMan}");
                                if (purchase.ShipDate.HasValue)
                                    c.Item().Text($"Ship Date : {purchase.ShipDate.Value:dd-MM-yyyy}");
                                if (!string.IsNullOrEmpty(purchase.ShipVia))
                                    c.Item().Text($"Ship Via : {purchase.ShipVia}");
                                if (!string.IsNullOrEmpty(purchase.ShipTo))
                                    c.Item().Text($"Ship To : {purchase.ShipTo}");
                                if (!string.IsNullOrEmpty(purchase.PoNumber))
                                    c.Item().Text($"PO Number : {purchase.PoNumber}");
                                if (!string.IsNullOrEmpty(purchase.PaymentMethod))
                                    c.Item().Text($"Payment Method : {purchase.PaymentMethod}");
                                if (!string.IsNullOrEmpty(purchase.PaymentTerms))
                                    c.Item().Text($"Payment Terms : {purchase.PaymentTerms}");
                                if (purchase.PaymentDate.HasValue)
                                    c.Item().Text($"Payment Date : {purchase.PaymentDate.Value:dd-MM-yyyy}");
                            });
                        });

                        col.Item().PaddingVertical(10).LineHorizontal(1);

                        // 🔹 ITEMS TABLE
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);    // S/N
                                columns.RelativeColumn(3);     // Item Name
                                columns.ConstantColumn(40);    // Qty
                                columns.ConstantColumn(50);    // Unit
                                columns.ConstantColumn(50);    // Price
                                columns.ConstantColumn(50);    // Discount
                                columns.ConstantColumn(60);    // Total
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("S/N").Bold();
                                header.Cell().Text("Item Name").Bold();
                                header.Cell().Text("Qty").Bold();
                                header.Cell().Text("Unit").Bold();
                                header.Cell().Text("Price").Bold();
                                header.Cell().Text("Discount").Bold();
                                header.Cell().Text("Total").Bold();
                            });

                            int i = 1;
                            foreach (var item in items)
                            {
                                table.Cell().Text(i++.ToString());
                                table.Cell().Text(item.Name);
                                table.Cell().Text(item.Qty.ToString("N2"));
                                table.Cell().Text(item.UnitName ?? "-");
                                table.Cell().Text(item.Price.ToString("N2"));
                                table.Cell().Text(item.Discount.ToString("N2"));
                                table.Cell().Text(item.Total.ToString("N2"));
                            }
                        });

                        // 🔹 TOTALS
                        col.Item().AlignRight().PaddingTop(15).Column(c =>
                        {
                            c.Item().Text($"Total Amount : {purchase.Total:N2}");
                            c.Item().Text($"Discount : {items.Sum(x => x.Discount):N2}");
                            c.Item().Text($"VAT ({purchase.Vat:P0}) : {purchase.Vat:N2}");
                            c.Item().Text($"Grand Total : {purchase.Net:N2}").Bold();
                        });

                        // 🔹 FOOTER
                        page.Footer().PaddingTop(20).Row(row =>
                        {
                            row.RelativeColumn().Column(c =>
                            {
                                c.Item().Text("Prepared By").SemiBold();
                                c.Item().PaddingTop(20).Text("Signature");
                            });

                            row.RelativeColumn().AlignRight().Column(c =>
                            {
                                c.Item().Text("Approved By").SemiBold().AlignRight();
                                c.Item().PaddingTop(20).AlignRight().Text("Signature");
                            });
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ================= DEFAULT QR HANDLER =================
        private static byte[] GetQrCode(byte[] qrCode)
        {
            if (qrCode != null && qrCode.Length > 0)
                return qrCode;

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "assets",
                "images",
                "DefaultQR.jpg");

            return File.Exists(path)
                ? File.ReadAllBytes(path)
                : null;
        }
    }


}
