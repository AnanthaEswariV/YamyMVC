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
        private const string BarGreen = "#a5b592";
        private const string BarBlue = "#809ec2";
        private const string BarYellow = "#e7bc29";
        private const string BarDark = "#444d26";
        private const string LineBlue = "#4472C4";
        private const string StripGreen = "#70AD47";
        private const string StripGray = "#A5A5A5";

        public static byte[] Generate(
            CompanyReportDto company,
            SaleReportDto sale,
            List<SaleItemReportDto> items)
        {
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

                    // ══ HEADER ══
                    page.Header().Column(hCol =>
                    {
                        hCol.Item().Height(95).Row(hRow =>
                        {
                            hRow.ConstantColumn(22).Background(BarGreen);
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(68).Background(BarBlue); });
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(48).Background(BarYellow); });
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(28).Background(BarDark); });
                            hRow.RelativeColumn();
                            hRow.ConstantColumn(140).AlignCenter().AlignMiddle().Element(el =>
                            {
                                if (logo != null) el.MaxHeight(90).MaxWidth(140).Image(logo).FitArea();
                            });
                        });
                        hCol.Item().LineHorizontal(1).LineColor(LineBlue);
                    });

                    // ══ WATERMARK ══
                    byte[] wm = MakeFaintWatermark(logo, alpha: 15);
                    if (wm != null)
                        page.Foreground().AlignCenter().AlignMiddle().Width(240).Height(240).Image(wm).FitArea();

                    // ══ CONTENT ══
                    page.Content().PaddingTop(6).Column(col =>
                    {
                        col.Item().AlignCenter().Text("SALES QUOTATION").Bold().FontSize(14).FontColor("#1F3864");
                        col.Item().AlignCenter().PaddingBottom(4).Text("Quotation").SemiBold().FontSize(10).FontColor("#555555");
                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(LineBlue);

                        col.Item().PaddingBottom(5).Row(row =>
                        {
                            row.RelativeColumn().Column(c =>
                            {
                                InfoLine(c, "Customer", sale.CustomerName ?? "");
                                if (!string.IsNullOrEmpty(sale.City)) InfoLine(c, "City", sale.City);
                                if (!string.IsNullOrEmpty(company.TRN)) InfoLine(c, "TRN", company.TRN);
                            });
                            row.RelativeColumn().Column(c =>
                            {
                                InfoLine(c, "Quotation No", sale.InvoiceNo ?? "");
                                InfoLine(c, "Date", sale.Date.ToString("dd-MM-yyyy"));
                                if (!string.IsNullOrEmpty(sale.SalesMan)) InfoLine(c, "Sales Man", sale.SalesMan);
                            });
                        });

                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(LineBlue);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(26); cols.RelativeColumn(3);
                                cols.ConstantColumn(42); cols.ConstantColumn(42);
                                cols.ConstantColumn(52); cols.ConstantColumn(46); cols.ConstantColumn(56);
                            });
                            table.Header(h =>
                            {
                                TH(h, "S/N", false); TH(h, "Item Name", false);
                                TH(h, "Qty", true); TH(h, "Unit", false);
                                TH(h, "Price", true); TH(h, "Disc", true); TH(h, "Total", true);
                            });
                            int i = 1;
                            foreach (var item in items)
                            {
                                string bg = (i % 2 == 0) ? "#EBF3FB" : "#FFFFFF";
                                TD(table, i.ToString(), bg, false);
                                TD(table, item.Name ?? "", bg, false);
                                TD(table, item.Qty.ToString("N2"), bg, true);
                                TD(table, item.UnitName ?? "", bg, false);
                                TD(table, item.Price.ToString("N2"), bg, true);
                                TD(table, (item.Discount ?? 0).ToString("N2"), bg, true);
                                TD(table, item.Total.ToString("N2"), bg, true);
                                i++;
                            }
                        });

                        decimal disc = items.Sum(x => x.Discount ?? 0m);
                        col.Item().AlignRight().PaddingTop(6).Width(240).Column(t =>
                        {
                            TotalRow(t, "SUB TOTAL", sale.Total.ToString("N2"), "#F2F2F2", false);
                            TotalRow(t, "VAT", sale.Vat.ToString("N2"), "#FFFFFF", false);
                            TotalRow(t, "TOTAL DISCOUNT", disc.ToString("N2"), "#F2F2F2", false);
                            TotalRow(t, "GRAND TOTAL", sale.Net.ToString("N2"), LineBlue, true, "#FFFFFF");
                        });

                        col.Item().PaddingTop(14).LineHorizontal(1).LineColor("#CCCCCC");
                        col.Item().PaddingTop(6).Row(sig =>
                        {
                            sig.RelativeColumn().Column(c =>
                            {
                                c.Item().Text("Prepared By").SemiBold();
                                c.Item().PaddingTop(20).Width(110).LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().PaddingTop(2).Text("Signature").FontSize(7.5f).FontColor("#888888");
                            });
                            sig.RelativeColumn().AlignRight().Column(c =>
                            {
                                c.Item().AlignRight().Text("Approved By").SemiBold();
                                c.Item().AlignRight().PaddingTop(20).Width(110).LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().AlignRight().PaddingTop(2).Text("Signature").FontSize(7.5f).FontColor("#888888");
                            });
                        });
                    });

                    // ══ FOOTER ══
                    PageFooter(page, company);
                });
            }).GeneratePdf();
        }

        private static void PageFooter(PageDescriptor page, CompanyReportDto company)
        {
            page.Footer().Column(fCol =>
            {
                fCol.Item().LineHorizontal(1).LineColor(LineBlue);
                fCol.Item().Height(62).Row(fRow =>
                {
                    fRow.ConstantColumn(30).AlignBottom().PaddingBottom(3).Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(7f).FontColor("#999999"));
                        t.CurrentPageNumber(); t.Span(" / "); t.TotalPages();
                    });
                    fRow.RelativeColumn().AlignRight().AlignMiddle().Column(contact =>
                    {
                        if (!string.IsNullOrEmpty(company.Phone))
                            contact.Item().AlignRight().Text($"Land Line: {company.Phone}").FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Phone2))
                            contact.Item().AlignRight().Text($"Phone: {company.Phone2}").FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Email))
                            contact.Item().AlignRight().Text(company.Email).FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Website))
                            contact.Item().AlignRight().Text(company.Website).FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Address))
                            contact.Item().AlignRight().Text(company.Address).FontSize(7.5f).FontColor("#333333");
                    });
                    fRow.ConstantColumn(8);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(42).Height(20).Background(BarYellow); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(28).Height(34).Background(StripGray); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(14).Height(48).Background(StripGreen); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(0).Height(62).Background(LineBlue); });
                });
            });
        }

        private static byte[] MakeFaintWatermark(byte[] imageBytes, byte alpha = 15)
        {
            if (imageBytes == null || imageBytes.Length == 0) return null;
            try
            {
                using var original = SKBitmap.Decode(imageBytes);
                if (original == null) return null;
                using var bmp = new SKBitmap(original.Width, original.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var skCanvas = new SKCanvas(bmp);
                skCanvas.Clear(SKColors.Transparent);
                using var paint = new SKPaint { ColorFilter = SKColorFilter.CreateBlendMode(SKColors.White.WithAlpha(alpha), SKBlendMode.DstIn) };
                skCanvas.DrawBitmap(original, 0, 0, paint);
                using var img = SKImage.FromBitmap(bmp);
                using var data = img.Encode(SKEncodedImageFormat.Png, 100);
                return data?.ToArray();
            }
            catch { return null; }
        }

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
            var cell = h.Cell().Background(LineBlue).PaddingVertical(4).PaddingHorizontal(3);
            var txt = cell.Text(text).Bold().FontSize(8).FontColor("#FFFFFF");
            if (right) txt.AlignRight();
        }

        private static void TD(TableDescriptor table, string text, string bg, bool right)
        {
            var cell = table.Cell().Background(bg).BorderBottom(1).BorderColor("#DDDDDD").PaddingVertical(3).PaddingHorizontal(3);
            var txt = cell.Text(text).FontSize(8);
            if (right) txt.AlignRight();
        }

        private static void TotalRow(ColumnDescriptor col, string label, string value, string bg, bool bold, string textColor = "#000000")
        {
            col.Item().Background(bg).PaddingVertical(3).PaddingHorizontal(5).Row(r =>
            {
                var lbl = r.RelativeColumn().Text(label).FontSize(8).FontColor(textColor);
                var val = r.ConstantColumn(72).AlignRight().Text(value).FontSize(8).FontColor(textColor);
                if (bold) { lbl.Bold(); val.Bold(); }
            });
        }
    }

    public static class SalesOrderPdfGenerator
    {
        private const string BarGreen = "#a5b592";
        private const string BarBlue = "#809ec2";
        private const string BarYellow = "#e7bc29";
        private const string BarDark = "#444d26";
        private const string LineBlue = "#4472C4";
        private const string StripGreen = "#70AD47";
        private const string StripGray = "#A5A5A5";

        public static byte[] Generate(
            CompanyReportDto company,
            SaleReportDto sale,
            List<SaleItemReportDto> items)
        {
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

                    // ══ HEADER ══
                    page.Header().Column(hCol =>
                    {
                        hCol.Item().Height(95).Row(hRow =>
                        {
                            hRow.ConstantColumn(22).Background(BarGreen);
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(68).Background(BarBlue); });
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(48).Background(BarYellow); });
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(28).Background(BarDark); });
                            hRow.RelativeColumn();
                            hRow.ConstantColumn(140).AlignCenter().AlignMiddle().Element(el =>
                            {
                                if (logo != null) el.MaxHeight(90).MaxWidth(140).Image(logo).FitArea();
                            });
                        });
                        hCol.Item().LineHorizontal(1).LineColor(LineBlue);
                    });

                    // ══ WATERMARK ══
                    byte[] wm = MakeFaintWatermark(logo, alpha: 15);
                    if (wm != null)
                        page.Foreground().AlignCenter().AlignMiddle().Width(240).Height(240).Image(wm).FitArea();

                    // ══ CONTENT ══
                    page.Content().PaddingTop(6).Column(col =>
                    {
                        col.Item().AlignCenter().Text("SALES ORDER").Bold().FontSize(14).FontColor("#1F3864");
                        col.Item().AlignCenter().PaddingBottom(4).Text("Sales Order").SemiBold().FontSize(10).FontColor("#555555");
                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(LineBlue);

                        col.Item().PaddingBottom(5).Row(row =>
                        {
                            row.RelativeColumn().Column(c =>
                            {
                                InfoLine(c, "Customer", sale.CustomerName ?? "");
                                if (!string.IsNullOrEmpty(sale.City)) InfoLine(c, "City", sale.City);
                                if (!string.IsNullOrEmpty(company.TRN)) InfoLine(c, "TRN", company.TRN);
                            });
                            row.RelativeColumn().Column(c =>
                            {
                                InfoLine(c, "Order No", sale.InvoiceNo ?? "");
                                InfoLine(c, "Date", sale.Date.ToString("dd-MM-yyyy"));
                                if (!string.IsNullOrEmpty(sale.SalesMan)) InfoLine(c, "Sales Man", sale.SalesMan);
                                if (!string.IsNullOrEmpty(sale.PaymentMethod)) InfoLine(c, "Pay Method", sale.PaymentMethod);
                            });
                        });

                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(LineBlue);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(26); cols.RelativeColumn(3);
                                cols.ConstantColumn(42); cols.ConstantColumn(42);
                                cols.ConstantColumn(52); cols.ConstantColumn(46); cols.ConstantColumn(56);
                            });
                            table.Header(h =>
                            {
                                TH(h, "S/N", false); TH(h, "Item Name", false);
                                TH(h, "Qty", true); TH(h, "Unit", false);
                                TH(h, "Price", true); TH(h, "Disc", true); TH(h, "Total", true);
                            });
                            int i = 1;
                            foreach (var item in items)
                            {
                                string bg = (i % 2 == 0) ? "#EBF3FB" : "#FFFFFF";
                                TD(table, i.ToString(), bg, false);
                                TD(table, item.Name ?? "", bg, false);
                                TD(table, item.Qty.ToString("N2"), bg, true);
                                TD(table, item.UnitName ?? "", bg, false);
                                TD(table, item.Price.ToString("N2"), bg, true);
                                TD(table, (item.Discount ?? 0).ToString("N2"), bg, true);
                                TD(table, item.Total.ToString("N2"), bg, true);
                                i++;
                            }
                        });

                        decimal disc = items.Sum(x => x.Discount ?? 0m);
                        col.Item().AlignRight().PaddingTop(6).Width(240).Column(t =>
                        {
                            TotalRow(t, "SUB TOTAL", sale.Total.ToString("N2"), "#F2F2F2", false);
                            TotalRow(t, "VAT", sale.Vat.ToString("N2"), "#FFFFFF", false);
                            TotalRow(t, "TOTAL DISCOUNT", disc.ToString("N2"), "#F2F2F2", false);
                            TotalRow(t, "GRAND TOTAL", sale.Net.ToString("N2"), LineBlue, true, "#FFFFFF");
                        });

                        col.Item().PaddingTop(14).LineHorizontal(1).LineColor("#CCCCCC");
                        col.Item().PaddingTop(6).Row(sig =>
                        {
                            sig.RelativeColumn().Column(c =>
                            {
                                c.Item().Text("Prepared By").SemiBold();
                                c.Item().PaddingTop(20).Width(110).LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().PaddingTop(2).Text("Signature").FontSize(7.5f).FontColor("#888888");
                            });
                            sig.RelativeColumn().AlignRight().Column(c =>
                            {
                                c.Item().AlignRight().Text("Approved By").SemiBold();
                                c.Item().AlignRight().PaddingTop(20).Width(110).LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().AlignRight().PaddingTop(2).Text("Signature").FontSize(7.5f).FontColor("#888888");
                            });
                        });
                    });

                    PageFooter(page, company);
                });
            }).GeneratePdf();
        }

        private static void PageFooter(PageDescriptor page, CompanyReportDto company)
        {
            page.Footer().Column(fCol =>
            {
                fCol.Item().LineHorizontal(1).LineColor(LineBlue);
                fCol.Item().Height(62).Row(fRow =>
                {
                    fRow.ConstantColumn(30).AlignBottom().PaddingBottom(3).Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(7f).FontColor("#999999"));
                        t.CurrentPageNumber(); t.Span(" / "); t.TotalPages();
                    });
                    fRow.RelativeColumn().AlignRight().AlignMiddle().Column(contact =>
                    {
                        if (!string.IsNullOrEmpty(company.Phone)) contact.Item().AlignRight().Text($"Land Line: {company.Phone}").FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Phone2)) contact.Item().AlignRight().Text($"Phone: {company.Phone2}").FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Email)) contact.Item().AlignRight().Text(company.Email).FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Website)) contact.Item().AlignRight().Text(company.Website).FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Address)) contact.Item().AlignRight().Text(company.Address).FontSize(7.5f).FontColor("#333333");
                    });
                    fRow.ConstantColumn(8);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(42).Height(20).Background(BarDark); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(28).Height(34).Background(StripGray); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(14).Height(48).Background(StripGreen); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(0).Height(62).Background(LineBlue); });
                });
            });
        }

        private static byte[] MakeFaintWatermark(byte[] imageBytes, byte alpha = 15)
        {
            if (imageBytes == null || imageBytes.Length == 0) return null;
            try
            {
                using var original = SKBitmap.Decode(imageBytes);
                if (original == null) return null;
                using var bmp = new SKBitmap(original.Width, original.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var skCanvas = new SKCanvas(bmp);
                skCanvas.Clear(SKColors.Transparent);
                using var paint = new SKPaint { ColorFilter = SKColorFilter.CreateBlendMode(SKColors.White.WithAlpha(alpha), SKBlendMode.DstIn) };
                skCanvas.DrawBitmap(original, 0, 0, paint);
                using var img = SKImage.FromBitmap(bmp);
                using var data = img.Encode(SKEncodedImageFormat.Png, 100);
                return data?.ToArray();
            }
            catch { return null; }
        }

        private static void InfoLine(ColumnDescriptor col, string label, string value)
        {
            col.Item().PaddingBottom(2).Text(t => { t.Span($"{label}: ").Bold().FontSize(8.5f); t.Span(value).FontSize(8.5f); });
        }

        private static void TH(TableCellDescriptor h, string text, bool right)
        {
            var cell = h.Cell().Background(LineBlue).PaddingVertical(4).PaddingHorizontal(3);
            var txt = cell.Text(text).Bold().FontSize(8).FontColor("#FFFFFF");
            if (right) txt.AlignRight();
        }

        private static void TD(TableDescriptor table, string text, string bg, bool right)
        {
            var cell = table.Cell().Background(bg).BorderBottom(1).BorderColor("#DDDDDD").PaddingVertical(3).PaddingHorizontal(3);
            var txt = cell.Text(text).FontSize(8);
            if (right) txt.AlignRight();
        }

        private static void TotalRow(ColumnDescriptor col, string label, string value, string bg, bool bold, string textColor = "#000000")
        {
            col.Item().Background(bg).PaddingVertical(3).PaddingHorizontal(5).Row(r =>
            {
                var lbl = r.RelativeColumn().Text(label).FontSize(8).FontColor(textColor);
                var val = r.ConstantColumn(72).AlignRight().Text(value).FontSize(8).FontColor(textColor);
                if (bold) { lbl.Bold(); val.Bold(); }
            });
        }
    }

    public static class SalesProformaPdfGenerator
    {
        private const string BarGreen = "#a5b592";
        private const string BarBlue = "#809ec2";
        private const string BarYellow = "#e7bc29";
        private const string BarDark = "#444d26";
        private const string LineBlue = "#4472C4";
        private const string StripGreen = "#70AD47";
        private const string StripGray = "#A5A5A5";

        public static byte[] Generate(
            CompanyReportDto company,
            SaleReportDto sale,
            List<SaleItemReportDto> items)
        {
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

                    // ══ HEADER ══
                    page.Header().Column(hCol =>
                    {
                        hCol.Item().Height(95).Row(hRow =>
                        {
                            hRow.ConstantColumn(22).Background(BarGreen);
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(68).Background(BarBlue); });
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(48).Background(BarYellow); });
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(28).Background(BarDark); });
                            hRow.RelativeColumn();
                            hRow.ConstantColumn(140).AlignCenter().AlignMiddle().Element(el =>
                            {
                                if (logo != null) el.MaxHeight(90).MaxWidth(140).Image(logo).FitArea();
                            });
                        });
                        hCol.Item().LineHorizontal(1).LineColor(LineBlue);
                    });

                    // ══ WATERMARK ══
                    byte[] wm = MakeFaintWatermark(logo, alpha: 15);
                    if (wm != null)
                        page.Foreground().AlignCenter().AlignMiddle().Width(240).Height(240).Image(wm).FitArea();

                    // ══ CONTENT ══
                    page.Content().PaddingTop(6).Column(col =>
                    {
                        col.Item().AlignCenter().Text("SALES PROFORMA INVOICE").Bold().FontSize(14).FontColor("#1F3864");
                        col.Item().AlignCenter().PaddingBottom(4).Text("Proforma Invoice").SemiBold().FontSize(10).FontColor("#555555");
                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(LineBlue);

                        col.Item().PaddingBottom(5).Row(row =>
                        {
                            row.RelativeColumn().Column(c =>
                            {
                                InfoLine(c, "Customer", sale.CustomerName ?? "");
                                if (!string.IsNullOrEmpty(sale.City)) InfoLine(c, "City", sale.City);
                                if (!string.IsNullOrEmpty(company.TRN)) InfoLine(c, "TRN", company.TRN);
                            });
                            row.RelativeColumn().Column(c =>
                            {
                                InfoLine(c, "Proforma No", sale.InvoiceNo ?? "");
                                InfoLine(c, "Date", sale.Date.ToString("dd-MM-yyyy"));
                                if (!string.IsNullOrEmpty(sale.SalesMan)) InfoLine(c, "Sales Man", sale.SalesMan);
                                if (!string.IsNullOrEmpty(sale.PaymentMethod)) InfoLine(c, "Pay Method", sale.PaymentMethod);
                            });
                        });

                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(LineBlue);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(26); cols.RelativeColumn(3);
                                cols.ConstantColumn(42); cols.ConstantColumn(42);
                                cols.ConstantColumn(52); cols.ConstantColumn(46); cols.ConstantColumn(56);
                            });
                            table.Header(h =>
                            {
                                TH(h, "S/N", false); TH(h, "Item Name", false);
                                TH(h, "Qty", true); TH(h, "Unit", false);
                                TH(h, "Price", true); TH(h, "Disc", true); TH(h, "Total", true);
                            });
                            int i = 1;
                            foreach (var item in items)
                            {
                                string bg = (i % 2 == 0) ? "#EBF3FB" : "#FFFFFF";
                                TD(table, i.ToString(), bg, false);
                                TD(table, item.Name ?? "", bg, false);
                                TD(table, item.Qty.ToString("N2"), bg, true);
                                TD(table, item.UnitName ?? "", bg, false);
                                TD(table, item.Price.ToString("N2"), bg, true);
                                TD(table, (item.Discount ?? 0).ToString("N2"), bg, true);
                                TD(table, item.Total.ToString("N2"), bg, true);
                                i++;
                            }
                        });

                        decimal disc = items.Sum(x => x.Discount ?? 0m);
                        col.Item().AlignRight().PaddingTop(6).Width(240).Column(t =>
                        {
                            TotalRow(t, "SUB TOTAL", sale.Total.ToString("N2"), "#F2F2F2", false);
                            TotalRow(t, "VAT", sale.Vat.ToString("N2"), "#FFFFFF", false);
                            TotalRow(t, "TOTAL DISCOUNT", disc.ToString("N2"), "#F2F2F2", false);
                            TotalRow(t, "GRAND TOTAL", sale.Net.ToString("N2"), LineBlue, true, "#FFFFFF");
                        });

                        col.Item().PaddingTop(14).LineHorizontal(1).LineColor("#CCCCCC");
                        col.Item().PaddingTop(6).Row(sig =>
                        {
                            sig.RelativeColumn().Column(c =>
                            {
                                c.Item().Text("Prepared By").SemiBold();
                                c.Item().PaddingTop(20).Width(110).LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().PaddingTop(2).Text("Signature").FontSize(7.5f).FontColor("#888888");
                            });
                            sig.RelativeColumn().AlignRight().Column(c =>
                            {
                                c.Item().AlignRight().Text("Approved By").SemiBold();
                                c.Item().AlignRight().PaddingTop(20).Width(110).LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().AlignRight().PaddingTop(2).Text("Signature").FontSize(7.5f).FontColor("#888888");
                            });
                        });
                    });

                    PageFooter(page, company);
                });
            }).GeneratePdf();
        }

        private static void PageFooter(PageDescriptor page, CompanyReportDto company)
        {
            page.Footer().Column(fCol =>
            {
                fCol.Item().LineHorizontal(1).LineColor(LineBlue);
                fCol.Item().Height(62).Row(fRow =>
                {
                    fRow.ConstantColumn(30).AlignBottom().PaddingBottom(3).Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(7f).FontColor("#999999"));
                        t.CurrentPageNumber(); t.Span(" / "); t.TotalPages();
                    });
                    fRow.RelativeColumn().AlignRight().AlignMiddle().Column(contact =>
                    {
                        if (!string.IsNullOrEmpty(company.Phone)) contact.Item().AlignRight().Text($"Land Line: {company.Phone}").FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Phone2)) contact.Item().AlignRight().Text($"Phone: {company.Phone2}").FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Email)) contact.Item().AlignRight().Text(company.Email).FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Website)) contact.Item().AlignRight().Text(company.Website).FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Address)) contact.Item().AlignRight().Text(company.Address).FontSize(7.5f).FontColor("#333333");
                    });
                    fRow.ConstantColumn(8);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(42).Height(20).Background(BarDark); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(28).Height(34).Background(StripGray); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(14).Height(48).Background(StripGreen); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(0).Height(62).Background(LineBlue); });
                });
            });
        }

        private static byte[] MakeFaintWatermark(byte[] imageBytes, byte alpha = 15)
        {
            if (imageBytes == null || imageBytes.Length == 0) return null;
            try
            {
                using var original = SKBitmap.Decode(imageBytes);
                if (original == null) return null;
                using var bmp = new SKBitmap(original.Width, original.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var skCanvas = new SKCanvas(bmp);
                skCanvas.Clear(SKColors.Transparent);
                using var paint = new SKPaint { ColorFilter = SKColorFilter.CreateBlendMode(SKColors.White.WithAlpha(alpha), SKBlendMode.DstIn) };
                skCanvas.DrawBitmap(original, 0, 0, paint);
                using var img = SKImage.FromBitmap(bmp);
                using var data = img.Encode(SKEncodedImageFormat.Png, 100);
                return data?.ToArray();
            }
            catch { return null; }
        }

        private static void InfoLine(ColumnDescriptor col, string label, string value)
        {
            col.Item().PaddingBottom(2).Text(t => { t.Span($"{label}: ").Bold().FontSize(8.5f); t.Span(value).FontSize(8.5f); });
        }

        private static void TH(TableCellDescriptor h, string text, bool right)
        {
            var cell = h.Cell().Background(LineBlue).PaddingVertical(4).PaddingHorizontal(3);
            var txt = cell.Text(text).Bold().FontSize(8).FontColor("#FFFFFF");
            if (right) txt.AlignRight();
        }

        private static void TD(TableDescriptor table, string text, string bg, bool right)
        {
            var cell = table.Cell().Background(bg).BorderBottom(1).BorderColor("#DDDDDD").PaddingVertical(3).PaddingHorizontal(3);
            var txt = cell.Text(text).FontSize(8);
            if (right) txt.AlignRight();
        }

        private static void TotalRow(ColumnDescriptor col, string label, string value, string bg, bool bold, string textColor = "#000000")
        {
            col.Item().Background(bg).PaddingVertical(3).PaddingHorizontal(5).Row(r =>
            {
                var lbl = r.RelativeColumn().Text(label).FontSize(8).FontColor(textColor);
                var val = r.ConstantColumn(72).AlignRight().Text(value).FontSize(8).FontColor(textColor);
                if (bold) { lbl.Bold(); val.Bold(); }
            });
        }
    }

    public static class SalesReturnPdfGenerator
    {
        private const string BarGreen = "#a5b592";
        private const string BarBlue = "#809ec2";
        private const string BarYellow = "#e7bc29";
        private const string BarDark = "#444d26";
        private const string LineBlue = "#4472C4";
        private const string StripGreen = "#70AD47";
        private const string StripGray = "#A5A5A5";

        public static byte[] Generate(
            CompanyReportDto company,
            SaleReportDto sale,
            List<SaleItemReportDto> items)
        {
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

                    // ══ HEADER ══
                    page.Header().Column(hCol =>
                    {
                        hCol.Item().Height(95).Row(hRow =>
                        {
                            hRow.ConstantColumn(22).Background(BarGreen);
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(68).Background(BarBlue); });
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(48).Background(BarYellow); });
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(28).Background(BarDark); });
                            hRow.RelativeColumn();
                            hRow.ConstantColumn(140).AlignCenter().AlignMiddle().Element(el =>
                            {
                                if (logo != null) el.MaxHeight(90).MaxWidth(140).Image(logo).FitArea();
                            });
                        });
                        hCol.Item().LineHorizontal(1).LineColor(LineBlue);
                    });

                    // ══ WATERMARK ══
                    byte[] wm = MakeFaintWatermark(logo, alpha: 15);
                    if (wm != null)
                        page.Foreground().AlignCenter().AlignMiddle().Width(240).Height(240).Image(wm).FitArea();

                    // ══ CONTENT ══
                    page.Content().PaddingTop(6).Column(col =>
                    {
                        col.Item().AlignCenter().Text("SALES RETURN INVOICE").Bold().FontSize(14).FontColor("#1F3864");
                        col.Item().AlignCenter().PaddingBottom(4).Text("Sales Return").SemiBold().FontSize(10).FontColor("#555555");
                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(LineBlue);

                        col.Item().PaddingBottom(5).Row(row =>
                        {
                            row.RelativeColumn().Column(c =>
                            {
                                InfoLine(c, "Customer", sale.CustomerName ?? "");
                                if (!string.IsNullOrEmpty(sale.City)) InfoLine(c, "City", sale.City);
                                if (!string.IsNullOrEmpty(company.TRN)) InfoLine(c, "TRN", company.TRN);
                            });
                            row.RelativeColumn().Column(c =>
                            {
                                InfoLine(c, "Invoice No", sale.InvoiceNo ?? "");
                                InfoLine(c, "Date", sale.Date.ToString("dd-MM-yyyy"));
                                if (!string.IsNullOrEmpty(sale.SalesMan)) InfoLine(c, "Sales Man", sale.SalesMan);
                                if (!string.IsNullOrEmpty(sale.PaymentMethod)) InfoLine(c, "Pay Method", sale.PaymentMethod);
                            });
                        });

                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(LineBlue);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(26); cols.RelativeColumn(3);
                                cols.ConstantColumn(42); cols.ConstantColumn(42);
                                cols.ConstantColumn(52); cols.ConstantColumn(46); cols.ConstantColumn(56);
                            });
                            table.Header(h =>
                            {
                                TH(h, "S/N", false); TH(h, "Item Name", false);
                                TH(h, "Qty", true); TH(h, "Unit", false);
                                TH(h, "Price", true); TH(h, "Disc", true); TH(h, "Total", true);
                            });
                            int i = 1;
                            foreach (var item in items)
                            {
                                string bg = (i % 2 == 0) ? "#EBF3FB" : "#FFFFFF";
                                TD(table, i.ToString(), bg, false);
                                TD(table, item.Name ?? "", bg, false);
                                TD(table, item.Qty.ToString("N2"), bg, true);
                                TD(table, item.UnitName ?? "", bg, false);
                                TD(table, item.Price.ToString("N2"), bg, true);
                                TD(table, (item.Discount ?? 0).ToString("N2"), bg, true);
                                TD(table, item.Total.ToString("N2"), bg, true);
                                i++;
                            }
                        });

                        decimal disc = items.Sum(x => x.Discount ?? 0m);
                        col.Item().AlignRight().PaddingTop(6).Width(240).Column(t =>
                        {
                            TotalRow(t, "SUB TOTAL", sale.Total.ToString("N2"), "#F2F2F2", false);
                            TotalRow(t, "VAT", sale.Vat.ToString("N2"), "#FFFFFF", false);
                            TotalRow(t, "TOTAL DISCOUNT", disc.ToString("N2"), "#F2F2F2", false);
                            TotalRow(t, "GRAND TOTAL", sale.Net.ToString("N2"), LineBlue, true, "#FFFFFF");
                        });

                        col.Item().PaddingTop(14).LineHorizontal(1).LineColor("#CCCCCC");
                        col.Item().PaddingTop(6).Row(sig =>
                        {
                            sig.RelativeColumn().Column(c =>
                            {
                                c.Item().Text("Prepared By").SemiBold();
                                c.Item().PaddingTop(20).Width(110).LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().PaddingTop(2).Text("Signature").FontSize(7.5f).FontColor("#888888");
                            });
                            sig.RelativeColumn().AlignRight().Column(c =>
                            {
                                c.Item().AlignRight().Text("Approved By").SemiBold();
                                c.Item().AlignRight().PaddingTop(20).Width(110).LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().AlignRight().PaddingTop(2).Text("Signature").FontSize(7.5f).FontColor("#888888");
                            });
                        });
                    });

                    PageFooter(page, company);
                });
            }).GeneratePdf();
        }

        private static void PageFooter(PageDescriptor page, CompanyReportDto company)
        {
            page.Footer().Column(fCol =>
            {
                fCol.Item().LineHorizontal(1).LineColor(LineBlue);
                fCol.Item().Height(62).Row(fRow =>
                {
                    fRow.ConstantColumn(30).AlignBottom().PaddingBottom(3).Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(7f).FontColor("#999999"));
                        t.CurrentPageNumber(); t.Span(" / "); t.TotalPages();
                    });
                    fRow.RelativeColumn().AlignRight().AlignMiddle().Column(contact =>
                    {
                        if (!string.IsNullOrEmpty(company.Phone)) contact.Item().AlignRight().Text($"Land Line: {company.Phone}").FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Phone2)) contact.Item().AlignRight().Text($"Phone: {company.Phone2}").FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Email)) contact.Item().AlignRight().Text(company.Email).FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Website)) contact.Item().AlignRight().Text(company.Website).FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Address)) contact.Item().AlignRight().Text(company.Address).FontSize(7.5f).FontColor("#333333");
                    });
                    fRow.ConstantColumn(8);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(42).Height(20).Background(BarDark); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(28).Height(34).Background(StripGray); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(14).Height(48).Background(StripGreen); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(0).Height(62).Background(LineBlue); });
                });
            });
        }

        private static byte[] MakeFaintWatermark(byte[] imageBytes, byte alpha = 15)
        {
            if (imageBytes == null || imageBytes.Length == 0) return null;
            try
            {
                using var original = SKBitmap.Decode(imageBytes);
                if (original == null) return null;
                using var bmp = new SKBitmap(original.Width, original.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var skCanvas = new SKCanvas(bmp);
                skCanvas.Clear(SKColors.Transparent);
                using var paint = new SKPaint { ColorFilter = SKColorFilter.CreateBlendMode(SKColors.White.WithAlpha(alpha), SKBlendMode.DstIn) };
                skCanvas.DrawBitmap(original, 0, 0, paint);
                using var img = SKImage.FromBitmap(bmp);
                using var data = img.Encode(SKEncodedImageFormat.Png, 100);
                return data?.ToArray();
            }
            catch { return null; }
        }

        private static void InfoLine(ColumnDescriptor col, string label, string value)
        {
            col.Item().PaddingBottom(2).Text(t => { t.Span($"{label}: ").Bold().FontSize(8.5f); t.Span(value).FontSize(8.5f); });
        }

        private static void TH(TableCellDescriptor h, string text, bool right)
        {
            var cell = h.Cell().Background(LineBlue).PaddingVertical(4).PaddingHorizontal(3);
            var txt = cell.Text(text).Bold().FontSize(8).FontColor("#FFFFFF");
            if (right) txt.AlignRight();
        }

        private static void TD(TableDescriptor table, string text, string bg, bool right)
        {
            var cell = table.Cell().Background(bg).BorderBottom(1).BorderColor("#DDDDDD").PaddingVertical(3).PaddingHorizontal(3);
            var txt = cell.Text(text).FontSize(8);
            if (right) txt.AlignRight();
        }

        private static void TotalRow(ColumnDescriptor col, string label, string value, string bg, bool bold, string textColor = "#000000")
        {
            col.Item().Background(bg).PaddingVertical(3).PaddingHorizontal(5).Row(r =>
            {
                var lbl = r.RelativeColumn().Text(label).FontSize(8).FontColor(textColor);
                var val = r.ConstantColumn(72).AlignRight().Text(value).FontSize(8).FontColor(textColor);
                if (bold) { lbl.Bold(); val.Bold(); }
            });
        }
    }

    public static class PurchaseInvoicePdfGenerator
    {
        private const string BarGreen = "#a5b592";
        private const string BarBlue = "#809ec2";
        private const string BarYellow = "#e7bc29";
        private const string BarDark = "#444d26";
        private const string LineBlue = "#4472C4";
        private const string StripGreen = "#70AD47";
        private const string StripGray = "#A5A5A5";

        public static byte[] Generate(
            CompanyReportDto company,
            PurchaseReportDto purchase,
            List<PurchaseItemReportDto> items)
        {
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

                    // ══ HEADER ══
                    page.Header().Column(hCol =>
                    {
                        hCol.Item().Height(95).Row(hRow =>
                        {
                            hRow.ConstantColumn(22).Background(BarGreen);
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(68).Background(BarBlue); });
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(48).Background(BarYellow); });
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(28).Background(BarDark); });
                            hRow.RelativeColumn();
                            hRow.ConstantColumn(140).AlignCenter().AlignMiddle().Element(el =>
                            {
                                if (logo != null) el.MaxHeight(90).MaxWidth(140).Image(logo).FitArea();
                            });
                        });
                        hCol.Item().LineHorizontal(1).LineColor(LineBlue);
                    });

                    // ══ WATERMARK ══
                    byte[] wm = MakeFaintWatermark(logo, alpha: 15);
                    if (wm != null)
                        page.Foreground().AlignCenter().AlignMiddle().Width(240).Height(240).Image(wm).FitArea();

                    // ══ CONTENT ══
                    page.Content().PaddingTop(6).Column(col =>
                    {
                        col.Item().AlignCenter().Text("PURCHASE INVOICE").Bold().FontSize(14).FontColor("#1F3864");
                        col.Item().AlignCenter().PaddingBottom(4).Text("Purchase Invoice").SemiBold().FontSize(10).FontColor("#555555");
                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(LineBlue);

                        col.Item().PaddingBottom(5).Row(row =>
                        {
                            row.RelativeColumn().Column(c =>
                            {
                                InfoLine(c, "Vendor", purchase.VendorName ?? "");
                                if (!string.IsNullOrEmpty(purchase.City)) InfoLine(c, "City", purchase.City);
                                if (!string.IsNullOrEmpty(purchase.VendorMobile)) InfoLine(c, "Mobile", purchase.VendorMobile);
                                if (!string.IsNullOrEmpty(purchase.VendorTRN)) InfoLine(c, "TRN", purchase.VendorTRN);
                            });
                            row.RelativeColumn().Column(c =>
                            {
                                InfoLine(c, "Invoice No", purchase.InvoiceNo ?? "");
                                InfoLine(c, "Date", purchase.Date.ToString("dd-MM-yyyy"));
                                if (!string.IsNullOrEmpty(purchase.PaymentMethod)) InfoLine(c, "Payment", purchase.PaymentMethod);
                            });
                        });

                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(LineBlue);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(26); cols.RelativeColumn(3);
                                cols.ConstantColumn(42); cols.ConstantColumn(42);
                                cols.ConstantColumn(52); cols.ConstantColumn(46); cols.ConstantColumn(56);
                            });
                            table.Header(h =>
                            {
                                TH(h, "S/N", false); TH(h, "Item Name", false);
                                TH(h, "Qty", true); TH(h, "Unit", false);
                                TH(h, "Price", true); TH(h, "Disc", true); TH(h, "Total", true);
                            });
                            int i = 1;
                            foreach (var item in items)
                            {
                                string bg = (i % 2 == 0) ? "#EBF3FB" : "#FFFFFF";
                                TD(table, i.ToString(), bg, false);
                                TD(table, item.Name ?? "", bg, false);
                                TD(table, item.Qty.ToString("N2"), bg, true);
                                TD(table, item.UnitName ?? "", bg, false);
                                TD(table, item.Price.ToString("N2"), bg, true);
                                TD(table, item.Discount.ToString("N2"), bg, true);
                                TD(table, item.Total.ToString("N2"), bg, true);
                                i++;
                            }
                        });

                        decimal disc = items.Sum(x => x.Discount);
                        col.Item().AlignRight().PaddingTop(6).Width(240).Column(t =>
                        {
                            TotalRow(t, "SUB TOTAL", purchase.Total.ToString("N2"), "#F2F2F2", false);
                            TotalRow(t, "VAT", purchase.Vat.ToString("N2"), "#FFFFFF", false);
                            TotalRow(t, "TOTAL DISCOUNT", disc.ToString("N2"), "#F2F2F2", false);
                            TotalRow(t, "GRAND TOTAL", purchase.Net.ToString("N2"), LineBlue, true, "#FFFFFF");
                        });

                        col.Item().PaddingTop(14).LineHorizontal(1).LineColor("#CCCCCC");
                        col.Item().PaddingTop(6).Row(sig =>
                        {
                            sig.RelativeColumn().Column(c =>
                            {
                                c.Item().Text("Prepared By").SemiBold();
                                c.Item().PaddingTop(20).Width(110).LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().PaddingTop(2).Text("Signature").FontSize(7.5f).FontColor("#888888");
                            });
                            sig.RelativeColumn().AlignRight().Column(c =>
                            {
                                c.Item().AlignRight().Text("Approved By").SemiBold();
                                c.Item().AlignRight().PaddingTop(20).Width(110).LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().AlignRight().PaddingTop(2).Text("Signature").FontSize(7.5f).FontColor("#888888");
                            });
                        });
                    });

                    PageFooter(page, company);
                });
            }).GeneratePdf();
        }

        private static void PageFooter(PageDescriptor page, CompanyReportDto company)
        {
            page.Footer().Column(fCol =>
            {
                fCol.Item().LineHorizontal(1).LineColor(LineBlue);
                fCol.Item().Height(62).Row(fRow =>
                {
                    fRow.ConstantColumn(30).AlignBottom().PaddingBottom(3).Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(7f).FontColor("#999999"));
                        t.CurrentPageNumber(); t.Span(" / "); t.TotalPages();
                    });
                    fRow.RelativeColumn().AlignRight().AlignMiddle().Column(contact =>
                    {
                        if (!string.IsNullOrEmpty(company.Phone)) contact.Item().AlignRight().Text($"Land Line: {company.Phone}").FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Phone2)) contact.Item().AlignRight().Text($"Phone: {company.Phone2}").FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Email)) contact.Item().AlignRight().Text(company.Email).FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Website)) contact.Item().AlignRight().Text(company.Website).FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Address)) contact.Item().AlignRight().Text(company.Address).FontSize(7.5f).FontColor("#333333");
                    });
                    fRow.ConstantColumn(8);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(42).Height(20).Background(BarDark); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(28).Height(34).Background(StripGray); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(14).Height(48).Background(StripGreen); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(0).Height(62).Background(LineBlue); });
                });
            });
        }

        private static byte[] MakeFaintWatermark(byte[] imageBytes, byte alpha = 15)
        {
            if (imageBytes == null || imageBytes.Length == 0) return null;
            try
            {
                using var original = SKBitmap.Decode(imageBytes);
                if (original == null) return null;
                using var bmp = new SKBitmap(original.Width, original.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var skCanvas = new SKCanvas(bmp);
                skCanvas.Clear(SKColors.Transparent);
                using var paint = new SKPaint { ColorFilter = SKColorFilter.CreateBlendMode(SKColors.White.WithAlpha(alpha), SKBlendMode.DstIn) };
                skCanvas.DrawBitmap(original, 0, 0, paint);
                using var img = SKImage.FromBitmap(bmp);
                using var data = img.Encode(SKEncodedImageFormat.Png, 100);
                return data?.ToArray();
            }
            catch { return null; }
        }

        private static void InfoLine(ColumnDescriptor col, string label, string value)
        {
            col.Item().PaddingBottom(2).Text(t => { t.Span($"{label}: ").Bold().FontSize(8.5f); t.Span(value).FontSize(8.5f); });
        }

        private static void TH(TableCellDescriptor h, string text, bool right)
        {
            var cell = h.Cell().Background(LineBlue).PaddingVertical(4).PaddingHorizontal(3);
            var txt = cell.Text(text).Bold().FontSize(8).FontColor("#FFFFFF");
            if (right) txt.AlignRight();
        }

        private static void TD(TableDescriptor table, string text, string bg, bool right)
        {
            var cell = table.Cell().Background(bg).BorderBottom(1).BorderColor("#DDDDDD").PaddingVertical(3).PaddingHorizontal(3);
            var txt = cell.Text(text).FontSize(8);
            if (right) txt.AlignRight();
        }

        private static void TotalRow(ColumnDescriptor col, string label, string value, string bg, bool bold, string textColor = "#000000")
        {
            col.Item().Background(bg).PaddingVertical(3).PaddingHorizontal(5).Row(r =>
            {
                var lbl = r.RelativeColumn().Text(label).FontSize(8).FontColor(textColor);
                var val = r.ConstantColumn(72).AlignRight().Text(value).FontSize(8).FontColor(textColor);
                if (bold) { lbl.Bold(); val.Bold(); }
            });
        }
    }

    public static class PurchaseReceiveNotePdfGenerator
    {
        private const string BarGreen = "#a5b592";
        private const string BarBlue = "#809ec2";
        private const string BarYellow = "#e7bc29";
        private const string BarDark = "#444d26";
        private const string LineBlue = "#4472C4";
        private const string StripGreen = "#70AD47";
        private const string StripGray = "#A5A5A5";

        public static byte[] Generate(
            CompanyReportDto company,
            PurchaseReportDto purchase,
            List<PurchaseItemReportDto> items)
        {
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

                    // ══ HEADER ══
                    page.Header().Column(hCol =>
                    {
                        hCol.Item().Height(95).Row(hRow =>
                        {
                            hRow.ConstantColumn(22).Background(BarGreen);
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(68).Background(BarDark); });
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(48).Background(BarYellow); });
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(28).Background(BarDark); });
                            hRow.RelativeColumn();
                            hRow.ConstantColumn(140).AlignCenter().AlignMiddle().Element(el =>
                            {
                                if (logo != null) el.MaxHeight(90).MaxWidth(140).Image(logo).FitArea();
                            });
                        });
                        hCol.Item().LineHorizontal(1).LineColor(LineBlue);
                    });

                    // ══ WATERMARK ══
                    byte[] wm = MakeFaintWatermark(logo, alpha: 15);
                    if (wm != null)
                        page.Foreground().AlignCenter().AlignMiddle().Width(240).Height(240).Image(wm).FitArea();

                    // ══ CONTENT ══
                    page.Content().PaddingTop(6).Column(col =>
                    {
                        col.Item().AlignCenter().Text("RECEIVED NOTE").Bold().FontSize(14).FontColor("#1F3864");
                        col.Item().AlignCenter().PaddingBottom(4).Text("Receiver Note").SemiBold().FontSize(10).FontColor("#555555");
                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(LineBlue);

                        col.Item().PaddingBottom(5).Row(row =>
                        {
                            row.RelativeColumn().Column(c =>
                            {
                                InfoLine(c, "Vendor", purchase.VendorName ?? "");
                                if (!string.IsNullOrEmpty(purchase.VendorMobile)) InfoLine(c, "Mobile", purchase.VendorMobile);
                            });
                            row.RelativeColumn().Column(c =>
                            {
                                InfoLine(c, "GRN No", purchase.InvoiceNo ?? "");
                                InfoLine(c, "Date", purchase.Date.ToString("dd-MM-yyyy"));
                            });
                        });

                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(LineBlue);

                        // Receive note: only S/N, Item Name, Qty, Unit columns
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(36);
                                cols.RelativeColumn(4);
                                cols.ConstantColumn(70);
                                cols.ConstantColumn(90);
                            });
                            table.Header(h =>
                            {
                                TH(h, "S/N", false);
                                TH(h, "Item Name", false);
                                TH(h, "Qty", true);
                                TH(h, "Unit", false);
                            });
                            int i = 1;
                            foreach (var item in items)
                            {
                                string bg = (i % 2 == 0) ? "#EBF3FB" : "#FFFFFF";
                                TD(table, i.ToString(), bg, false);
                                TD(table, item.Name ?? "", bg, false);
                                TD(table, item.Qty.ToString("N2"), bg, true);
                                TD(table, item.UnitName ?? "", bg, false);
                                i++;
                            }
                        });

                        col.Item().PaddingTop(14).LineHorizontal(1).LineColor("#CCCCCC");
                        col.Item().PaddingTop(6).Row(sig =>
                        {
                            sig.RelativeColumn().Column(c =>
                            {
                                c.Item().Text("Received By").SemiBold();
                                c.Item().PaddingTop(20).Width(110).LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().PaddingTop(2).Text("Signature").FontSize(7.5f).FontColor("#888888");
                            });
                            sig.RelativeColumn().AlignRight().Column(c =>
                            {
                                c.Item().AlignRight().Text("Approved By").SemiBold();
                                c.Item().AlignRight().PaddingTop(20).Width(110).LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().AlignRight().PaddingTop(2).Text("Signature").FontSize(7.5f).FontColor("#888888");
                            });
                        });
                    });

                    PageFooter(page, company);
                });
            }).GeneratePdf();
        }

        private static void PageFooter(PageDescriptor page, CompanyReportDto company)
        {
            page.Footer().Column(fCol =>
            {
                fCol.Item().LineHorizontal(1).LineColor(LineBlue);
                fCol.Item().Height(62).Row(fRow =>
                {
                    fRow.ConstantColumn(30).AlignBottom().PaddingBottom(3).Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(7f).FontColor("#999999"));
                        t.CurrentPageNumber(); t.Span(" / "); t.TotalPages();
                    });
                    fRow.RelativeColumn().AlignRight().AlignMiddle().Column(contact =>
                    {
                        if (!string.IsNullOrEmpty(company.Phone)) contact.Item().AlignRight().Text($"Land Line: {company.Phone}").FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Phone2)) contact.Item().AlignRight().Text($"Phone: {company.Phone2}").FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Email)) contact.Item().AlignRight().Text(company.Email).FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Website)) contact.Item().AlignRight().Text(company.Website).FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Address)) contact.Item().AlignRight().Text(company.Address).FontSize(7.5f).FontColor("#333333");
                    });
                    fRow.ConstantColumn(8);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(42).Height(20).Background(BarYellow); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(28).Height(34).Background(StripGray); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(14).Height(48).Background(StripGreen); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(0).Height(62).Background(LineBlue); });
                });
            });
        }

        private static byte[] MakeFaintWatermark(byte[] imageBytes, byte alpha = 15)
        {
            if (imageBytes == null || imageBytes.Length == 0) return null;
            try
            {
                using var original = SKBitmap.Decode(imageBytes);
                if (original == null) return null;
                using var bmp = new SKBitmap(original.Width, original.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var skCanvas = new SKCanvas(bmp);
                skCanvas.Clear(SKColors.Transparent);
                using var paint = new SKPaint { ColorFilter = SKColorFilter.CreateBlendMode(SKColors.White.WithAlpha(alpha), SKBlendMode.DstIn) };
                skCanvas.DrawBitmap(original, 0, 0, paint);
                using var img = SKImage.FromBitmap(bmp);
                using var data = img.Encode(SKEncodedImageFormat.Png, 100);
                return data?.ToArray();
            }
            catch { return null; }
        }

        private static void InfoLine(ColumnDescriptor col, string label, string value)
        {
            col.Item().PaddingBottom(2).Text(t => { t.Span($"{label}: ").Bold().FontSize(8.5f); t.Span(value).FontSize(8.5f); });
        }

        private static void TH(TableCellDescriptor h, string text, bool right)
        {
            var cell = h.Cell().Background(LineBlue).PaddingVertical(4).PaddingHorizontal(3);
            var txt = cell.Text(text).Bold().FontSize(8).FontColor("#FFFFFF");
            if (right) txt.AlignRight();
        }

        private static void TD(TableDescriptor table, string text, string bg, bool right)
        {
            var cell = table.Cell().Background(bg).BorderBottom(1).BorderColor("#DDDDDD").PaddingVertical(3).PaddingHorizontal(3);
            var txt = cell.Text(text).FontSize(8);
            if (right) txt.AlignRight();
        }
    }

    public static class PurchaseOrderInvoicePdfGenerator
    {
        private const string BarGreen = "#a5b592";
        private const string BarBlue = "#809ec2";
        private const string BarYellow = "#e7bc29";
        private const string BarDark = "#444d26";
        private const string LineBlue = "#4472C4";
        private const string StripGreen = "#70AD47";
        private const string StripGray = "#A5A5A5";

        public static byte[] Generate(
            CompanyReportDto company,
            PurchaseReportDto purchase,
            List<PurchaseItemReportDto> items)
        {
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

                    // ══ HEADER ══
                    page.Header().Column(hCol =>
                    {
                        hCol.Item().Height(95).Row(hRow =>
                        {
                            hRow.ConstantColumn(22).Background(BarGreen);
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(68).Background(BarBlue); });
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(48).Background(BarYellow); });
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(28).Background(BarDark); });
                            hRow.RelativeColumn();
                            hRow.ConstantColumn(140).AlignCenter().AlignMiddle().Element(el =>
                            {
                                if (logo != null) el.MaxHeight(90).MaxWidth(140).Image(logo).FitArea();
                            });
                        });
                        hCol.Item().LineHorizontal(1).LineColor(LineBlue);
                    });

                    // ══ WATERMARK ══
                    byte[] wm = MakeFaintWatermark(logo, alpha: 15);
                    if (wm != null)
                        page.Foreground().AlignCenter().AlignMiddle().Width(240).Height(240).Image(wm).FitArea();

                    // ══ CONTENT ══
                    page.Content().PaddingTop(6).Column(col =>
                    {
                        col.Item().AlignCenter().Text("LOCAL PURCHASE ORDER").Bold().FontSize(14).FontColor("#1F3864");
                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(LineBlue);

                        col.Item().PaddingBottom(5).Row(row =>
                        {
                            row.RelativeColumn().Column(c =>
                            {
                                InfoLine(c, "No", purchase.InvoiceNo ?? "");
                                InfoLine(c, "Date", purchase.Date.ToString("dd-MM-yyyy"));
                                InfoLine(c, "To M/S", purchase.VendorName ?? "");
                                if (!string.IsNullOrEmpty(purchase.VendorPhone)) InfoLine(c, "Tel No", purchase.VendorPhone);
                                if (!string.IsNullOrEmpty(purchase.City)) InfoLine(c, "Project", purchase.City);
                            });
                            row.RelativeColumn().Column(c =>
                            {
                                if (!string.IsNullOrEmpty(company.TRN)) InfoLine(c, "Tax Reg No", company.TRN);
                            });
                        });

                        col.Item().PaddingBottom(5).PaddingTop(4)
                            .Border(1).BorderColor(LineBlue).Padding(5)
                            .Text("Please supply the mentioned items below").SemiBold().FontSize(8.5f);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(30);
                                cols.RelativeColumn(5);
                                cols.RelativeColumn(2);
                                cols.ConstantColumn(46);
                                cols.ConstantColumn(56);
                                cols.ConstantColumn(56);
                            });
                            table.Header(h =>
                            {
                                TH(h, "No.", false);
                                TH(h, "Description", false);
                                TH(h, "Unit", false);
                                TH(h, "Qty", true);
                                TH(h, "Rate", true);
                                TH(h, "Total", true);
                            });
                            int i = 1;
                            foreach (var item in items)
                            {
                                string bg = (i % 2 == 0) ? "#EBF3FB" : "#FFFFFF";
                                TD(table, i.ToString(), bg, false);
                                TD(table, item.Name ?? "", bg, false);
                                TD(table, item.UnitName ?? "", bg, false);
                                TD(table, item.Qty.ToString("N0"), bg, true);
                                TD(table, item.CostPrice.ToString("N2"), bg, true);
                                TD(table, item.Total.ToString("N2"), bg, true);
                                i++;
                            }
                        });

                        col.Item().AlignRight().PaddingTop(6).Width(240).Column(t =>
                        {
                            TotalRow(t, "TOTAL AMOUNT", purchase.Total.ToString("N2"), "#F2F2F2", false);
                            TotalRow(t, "DISCOUNT", purchase.Pay.ToString("N2"), "#FFFFFF", false);
                            TotalRow(t, "NET AMOUNT", (purchase.Total - purchase.Pay).ToString("N2"), "#F2F2F2", false);
                            TotalRow(t, "VAT 5%", purchase.Vat.ToString("N2"), "#FFFFFF", false);
                            TotalRow(t, "GRAND TOTAL", purchase.Net.ToString("N2"), LineBlue, true, "#FFFFFF");
                        });

                        col.Item().PaddingTop(8).Text($"Contact Person for Delivery: {purchase.VendorName ?? ""}").FontSize(8.5f);

                        col.Item().PaddingTop(14).LineHorizontal(1).LineColor("#CCCCCC");
                        col.Item().PaddingTop(6).Row(sig =>
                        {
                            sig.RelativeColumn().Column(c =>
                            {
                                c.Item().Text("Prepared By").SemiBold();
                                c.Item().PaddingTop(20).Width(110).LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().PaddingTop(2).Text("Signature").FontSize(7.5f).FontColor("#888888");
                            });
                            sig.RelativeColumn().AlignRight().Column(c =>
                            {
                                c.Item().AlignRight().Text("Approved By").SemiBold();
                                c.Item().AlignRight().PaddingTop(20).Width(110).LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().AlignRight().PaddingTop(2).Text("Signature").FontSize(7.5f).FontColor("#888888");
                            });
                        });
                    });

                    PageFooter(page, company);
                });
            }).GeneratePdf();
        }

        private static void PageFooter(PageDescriptor page, CompanyReportDto company)
        {
            page.Footer().Column(fCol =>
            {
                fCol.Item().LineHorizontal(1).LineColor(LineBlue);
                fCol.Item().Height(62).Row(fRow =>
                {
                    fRow.ConstantColumn(30).AlignBottom().PaddingBottom(3).Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(7f).FontColor("#999999"));
                        t.CurrentPageNumber(); t.Span(" / "); t.TotalPages();
                    });
                    fRow.RelativeColumn().AlignRight().AlignMiddle().Column(contact =>
                    {
                        if (!string.IsNullOrEmpty(company.Phone)) contact.Item().AlignRight().Text($"Land Line: {company.Phone}").FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Phone2)) contact.Item().AlignRight().Text($"Phone: {company.Phone2}").FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Email)) contact.Item().AlignRight().Text(company.Email).FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Website)) contact.Item().AlignRight().Text(company.Website).FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Address)) contact.Item().AlignRight().Text(company.Address).FontSize(7.5f).FontColor("#333333");
                    });
                    fRow.ConstantColumn(8);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(42).Height(20).Background(BarDark); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(28).Height(34).Background(StripGray); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(14).Height(48).Background(StripGreen); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(0).Height(62).Background(LineBlue); });
                });
            });
        }

        private static byte[] MakeFaintWatermark(byte[] imageBytes, byte alpha = 15)
        {
            if (imageBytes == null || imageBytes.Length == 0) return null;
            try
            {
                using var original = SKBitmap.Decode(imageBytes);
                if (original == null) return null;
                using var bmp = new SKBitmap(original.Width, original.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var skCanvas = new SKCanvas(bmp);
                skCanvas.Clear(SKColors.Transparent);
                using var paint = new SKPaint { ColorFilter = SKColorFilter.CreateBlendMode(SKColors.White.WithAlpha(alpha), SKBlendMode.DstIn) };
                skCanvas.DrawBitmap(original, 0, 0, paint);
                using var img = SKImage.FromBitmap(bmp);
                using var data = img.Encode(SKEncodedImageFormat.Png, 100);
                return data?.ToArray();
            }
            catch { return null; }
        }

        private static void InfoLine(ColumnDescriptor col, string label, string value)
        {
            col.Item().PaddingBottom(2).Text(t => { t.Span($"{label}: ").Bold().FontSize(8.5f); t.Span(value).FontSize(8.5f); });
        }

        private static void TH(TableCellDescriptor h, string text, bool right)
        {
            var cell = h.Cell().Background(LineBlue).PaddingVertical(4).PaddingHorizontal(3);
            var txt = cell.Text(text).Bold().FontSize(8).FontColor("#FFFFFF");
            if (right) txt.AlignRight();
        }

        private static void TD(TableDescriptor table, string text, string bg, bool right)
        {
            var cell = table.Cell().Background(bg).BorderBottom(1).BorderColor("#DDDDDD").PaddingVertical(3).PaddingHorizontal(3);
            var txt = cell.Text(text).FontSize(8);
            if (right) txt.AlignRight();
        }

        private static void TotalRow(ColumnDescriptor col, string label, string value, string bg, bool bold, string textColor = "#000000")
        {
            col.Item().Background(bg).PaddingVertical(3).PaddingHorizontal(5).Row(r =>
            {
                var lbl = r.RelativeColumn().Text(label).FontSize(8).FontColor(textColor);
                var val = r.ConstantColumn(72).AlignRight().Text(value).FontSize(8).FontColor(textColor);
                if (bold) { lbl.Bold(); val.Bold(); }
            });
        }
    }

    public static class PurchaseReturnInvoicePdfGenerator
    {
        private const string BarGreen = "#a5b592";
        private const string BarBlue = "#809ec2";
        private const string BarYellow = "#e7bc29";
        private const string BarDark = "#444d26";
        private const string LineBlue = "#4472C4";
        private const string StripGreen = "#70AD47";
        private const string StripGray = "#A5A5A5";

        public static byte[] Generate(
            CompanyReportDto company,
            PurchaseReportDto purchase,
            List<PurchaseItemReportDto> items)
        {
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

                    // ══ HEADER ══
                    page.Header().Column(hCol =>
                    {
                        hCol.Item().Height(95).Row(hRow =>
                        {
                            hRow.ConstantColumn(22).Background(BarGreen);
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(68).Background(BarBlue); });
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(48).Background(BarYellow); });
                            hRow.ConstantColumn(3);
                            hRow.ConstantColumn(22).Column(c => { c.Item().Height(28).Background(BarDark); });
                            hRow.RelativeColumn();
                            hRow.ConstantColumn(140).AlignCenter().AlignMiddle().Element(el =>
                            {
                                if (logo != null) el.MaxHeight(90).MaxWidth(140).Image(logo).FitArea();
                            });
                        });
                        hCol.Item().LineHorizontal(1).LineColor(LineBlue);
                    });

                    // ══ WATERMARK ══
                    byte[] wm = MakeFaintWatermark(logo, alpha: 15);
                    if (wm != null)
                        page.Foreground().AlignCenter().AlignMiddle().Width(240).Height(240).Image(wm).FitArea();

                    // ══ CONTENT ══
                    page.Content().PaddingTop(6).Column(col =>
                    {
                        col.Item().AlignCenter().Text("PURCHASE RETURN INVOICE").Bold().FontSize(14).FontColor("#1F3864");
                        col.Item().AlignCenter().PaddingBottom(4).Text("Purchase Return").SemiBold().FontSize(10).FontColor("#555555");
                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(LineBlue);

                        col.Item().PaddingBottom(5).Row(row =>
                        {
                            row.RelativeColumn().Column(c =>
                            {
                                InfoLine(c, "Vendor", purchase.VendorName ?? "");
                                if (!string.IsNullOrEmpty(purchase.BillTo)) InfoLine(c, "Bill To", purchase.BillTo);
                                if (!string.IsNullOrEmpty(purchase.City)) InfoLine(c, "City", purchase.City);
                                if (!string.IsNullOrEmpty(purchase.VendorPhone)) InfoLine(c, "Phone", purchase.VendorPhone);
                                if (!string.IsNullOrEmpty(purchase.VendorEmail)) InfoLine(c, "Email", purchase.VendorEmail);
                                if (!string.IsNullOrEmpty(purchase.VendorTRN)) InfoLine(c, "TRN", purchase.VendorTRN);
                            });
                            row.RelativeColumn().Column(c =>
                            {
                                InfoLine(c, "Invoice No", purchase.InvoiceNo ?? "");
                                InfoLine(c, "Date", purchase.Date.ToString("dd-MM-yyyy"));
                                if (!string.IsNullOrEmpty(purchase.SalesMan)) InfoLine(c, "Sales Man", purchase.SalesMan);
                                if (purchase.ShipDate.HasValue) InfoLine(c, "Ship Date", purchase.ShipDate.Value.ToString("dd-MM-yyyy"));
                                if (!string.IsNullOrEmpty(purchase.ShipVia)) InfoLine(c, "Ship Via", purchase.ShipVia);
                                if (!string.IsNullOrEmpty(purchase.ShipTo)) InfoLine(c, "Ship To", purchase.ShipTo);
                                if (!string.IsNullOrEmpty(purchase.PoNumber)) InfoLine(c, "PO Number", purchase.PoNumber);
                                if (!string.IsNullOrEmpty(purchase.PaymentMethod)) InfoLine(c, "Payment Method", purchase.PaymentMethod);
                                if (!string.IsNullOrEmpty(purchase.PaymentTerms)) InfoLine(c, "Payment Terms", purchase.PaymentTerms);
                                if (purchase.PaymentDate.HasValue) InfoLine(c, "Payment Date", purchase.PaymentDate.Value.ToString("dd-MM-yyyy"));
                            });
                        });

                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(LineBlue);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(26); cols.RelativeColumn(3);
                                cols.ConstantColumn(42); cols.ConstantColumn(42);
                                cols.ConstantColumn(52); cols.ConstantColumn(46); cols.ConstantColumn(56);
                            });
                            table.Header(h =>
                            {
                                TH(h, "S/N", false); TH(h, "Item Name", false);
                                TH(h, "Qty", true); TH(h, "Unit", false);
                                TH(h, "Price", true); TH(h, "Discount", true); TH(h, "Total", true);
                            });
                            int i = 1;
                            foreach (var item in items)
                            {
                                string bg = (i % 2 == 0) ? "#EBF3FB" : "#FFFFFF";
                                TD(table, i.ToString(), bg, false);
                                TD(table, item.Name ?? "", bg, false);
                                TD(table, item.Qty.ToString("N2"), bg, true);
                                TD(table, item.UnitName ?? "-", bg, false);
                                TD(table, item.Price.ToString("N2"), bg, true);
                                TD(table, item.Discount.ToString("N2"), bg, true);
                                TD(table, item.Total.ToString("N2"), bg, true);
                                i++;
                            }
                        });

                        decimal disc = items.Sum(x => x.Discount);
                        col.Item().AlignRight().PaddingTop(6).Width(240).Column(t =>
                        {
                            TotalRow(t, "TOTAL AMOUNT", purchase.Total.ToString("N2"), "#F2F2F2", false);
                            TotalRow(t, "DISCOUNT", disc.ToString("N2"), "#FFFFFF", false);
                            TotalRow(t, "VAT", purchase.Vat.ToString("N2"), "#F2F2F2", false);
                            TotalRow(t, "GRAND TOTAL", purchase.Net.ToString("N2"), LineBlue, true, "#FFFFFF");
                        });

                        col.Item().PaddingTop(14).LineHorizontal(1).LineColor("#CCCCCC");
                        col.Item().PaddingTop(6).Row(sig =>
                        {
                            sig.RelativeColumn().Column(c =>
                            {
                                c.Item().Text("Prepared By").SemiBold();
                                c.Item().PaddingTop(20).Width(110).LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().PaddingTop(2).Text("Signature").FontSize(7.5f).FontColor("#888888");
                            });
                            sig.RelativeColumn().AlignRight().Column(c =>
                            {
                                c.Item().AlignRight().Text("Approved By").SemiBold();
                                c.Item().AlignRight().PaddingTop(20).Width(110).LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().AlignRight().PaddingTop(2).Text("Signature").FontSize(7.5f).FontColor("#888888");
                            });
                        });
                    });

                    PageFooter(page, company);
                });
            }).GeneratePdf();
        }

        private static void PageFooter(PageDescriptor page, CompanyReportDto company)
        {
            page.Footer().Column(fCol =>
            {
                fCol.Item().LineHorizontal(1).LineColor(LineBlue);
                fCol.Item().Height(62).Row(fRow =>
                {
                    fRow.ConstantColumn(30).AlignBottom().PaddingBottom(3).Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(7f).FontColor("#999999"));
                        t.CurrentPageNumber(); t.Span(" / "); t.TotalPages();
                    });
                    fRow.RelativeColumn().AlignRight().AlignMiddle().Column(contact =>
                    {
                        if (!string.IsNullOrEmpty(company.Phone)) contact.Item().AlignRight().Text($"Land Line: {company.Phone}").FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Phone2)) contact.Item().AlignRight().Text($"Phone: {company.Phone2}").FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Email)) contact.Item().AlignRight().Text(company.Email).FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Website)) contact.Item().AlignRight().Text(company.Website).FontSize(7.5f).FontColor("#333333");
                        if (!string.IsNullOrEmpty(company.Address)) contact.Item().AlignRight().Text(company.Address).FontSize(7.5f).FontColor("#333333");
                    });
                    fRow.ConstantColumn(8);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(42).Height(20).Background(BarDark); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(28).Height(34).Background(StripGray); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(14).Height(48).Background(StripGreen); });
                    fRow.ConstantColumn(3);
                    fRow.ConstantColumn(14).Column(c => { c.Item().PaddingTop(0).Height(62).Background(LineBlue); });
                });
            });
        }

        private static byte[] MakeFaintWatermark(byte[] imageBytes, byte alpha = 15)
        {
            if (imageBytes == null || imageBytes.Length == 0) return null;
            try
            {
                using var original = SKBitmap.Decode(imageBytes);
                if (original == null) return null;
                using var bmp = new SKBitmap(original.Width, original.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var skCanvas = new SKCanvas(bmp);
                skCanvas.Clear(SKColors.Transparent);
                using var paint = new SKPaint { ColorFilter = SKColorFilter.CreateBlendMode(SKColors.White.WithAlpha(alpha), SKBlendMode.DstIn) };
                skCanvas.DrawBitmap(original, 0, 0, paint);
                using var img = SKImage.FromBitmap(bmp);
                using var data = img.Encode(SKEncodedImageFormat.Png, 100);
                return data?.ToArray();
            }
            catch { return null; }
        }

        private static void InfoLine(ColumnDescriptor col, string label, string value)
        {
            col.Item().PaddingBottom(2).Text(t => { t.Span($"{label}: ").Bold().FontSize(8.5f); t.Span(value).FontSize(8.5f); });
        }

        private static void TH(TableCellDescriptor h, string text, bool right)
        {
            var cell = h.Cell().Background(LineBlue).PaddingVertical(4).PaddingHorizontal(3);
            var txt = cell.Text(text).Bold().FontSize(8).FontColor("#FFFFFF");
            if (right) txt.AlignRight();
        }

        private static void TD(TableDescriptor table, string text, string bg, bool right)
        {
            var cell = table.Cell().Background(bg).BorderBottom(1).BorderColor("#DDDDDD").PaddingVertical(3).PaddingHorizontal(3);
            var txt = cell.Text(text).FontSize(8);
            if (right) txt.AlignRight();
        }

        private static void TotalRow(ColumnDescriptor col, string label, string value, string bg, bool bold, string textColor = "#000000")
        {
            col.Item().Background(bg).PaddingVertical(3).PaddingHorizontal(5).Row(r =>
            {
                var lbl = r.RelativeColumn().Text(label).FontSize(8).FontColor(textColor);
                var val = r.ConstantColumn(72).AlignRight().Text(value).FontSize(8).FontColor(textColor);
                if (bold) { lbl.Bold(); val.Bold(); }
            });
        }
    }


}
