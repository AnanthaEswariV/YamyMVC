using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace YamyProject.Core.Models
{
    public static class SapInvoicePdfGenerator
    {
        // ── Letterhead accent colours (matched from the .docx) ───────────────────
        private static readonly string AccentGreen = "#a5b592";
        private static readonly string AccentBlue = "#809ec2";
        private static readonly string AccentYellow = "#e7bc29";
        private static readonly string AccentDark = "#444d26";
        private static readonly string FooterBlue = "#4472C4";
        private static readonly string FooterGreen2 = "#70AD47";
        private static readonly string FooterGray = "#A5A5A5";

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
                    page.MarginTop(15);
                    page.MarginBottom(10);
                    page.MarginLeft(20);
                    page.MarginRight(20);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    // ══════════════════════════════════════════════════════════
                    // HEADER
                    // ══════════════════════════════════════════════════════════
                    page.Header().Column(headerCol =>
                    {
                        headerCol.Item().Height(70).Row(headerRow =>
                        {
                            // ── Decorative colour bars (fixed heights, no ExtendVertical) ──
                            headerRow.ConstantColumn(6).Background(AccentGreen);
                            headerRow.ConstantColumn(2);
                            headerRow.ConstantColumn(6).Column(c =>
                            {
                                c.Item().Height(55).Background(AccentBlue);
                                c.Item().Extend();
                            });
                            headerRow.ConstantColumn(2);
                            headerRow.ConstantColumn(6).Column(c =>
                            {
                                c.Item().Height(42).Background(AccentYellow);
                                c.Item().Extend();
                            });
                            headerRow.ConstantColumn(2);
                            headerRow.ConstantColumn(5).Column(c =>
                            {
                                c.Item().Height(28).Background(AccentDark);
                                c.Item().Extend();
                            });
                            headerRow.ConstantColumn(6);

                            // ── Company info ────────────────────────────────────
                            headerRow.RelativeColumn().PaddingLeft(4).Column(info =>
                            {
                                info.Item().Text(company.Name ?? "")
                                    .Bold().FontSize(12).FontColor("#1F3864");
                                if (!string.IsNullOrEmpty(company.Address))
                                    info.Item().Text(company.Address)
                                        .FontSize(7.5f).FontColor("#555555");
                                if (!string.IsNullOrEmpty(company.Phone))
                                    info.Item().Text($"Tel: {company.Phone}")
                                        .FontSize(7.5f).FontColor("#555555");
                                if (!string.IsNullOrEmpty(company.Email))
                                    info.Item().Text(company.Email)
                                        .FontSize(7.5f).FontColor("#555555");
                                if (!string.IsNullOrEmpty(company.TRN))
                                    info.Item().Text($"TRN: {company.TRN}")
                                        .FontSize(7.5f).FontColor("#555555");
                            });

                            // ── Logo ────────────────────────────────────────────
                            headerRow.ConstantColumn(72).AlignCenter().AlignMiddle()
                                .Element(el =>
                                {
                                    if (company.Logo != null && company.Logo.Length > 0)
                                        el.MaxHeight(65).MaxWidth(72)
                                          .Image(company.Logo).FitArea();
                                });

                            // ── QR code ─────────────────────────────────────────
                            headerRow.ConstantColumn(65).AlignCenter().AlignMiddle()
                                .Element(el =>
                                {
                                    var qr = GetQrCode(company.QrCode);
                                    if (qr != null)
                                        el.MaxHeight(65).MaxWidth(65)
                                          .Image(qr).FitArea();
                                });
                        });

                        // Blue underline
                        headerCol.Item().LineHorizontal(2).LineColor(FooterBlue);
                    });

                    // ══════════════════════════════════════════════════════════
                    // WATERMARK – uses page.Foreground() to avoid layout conflicts
                    // ══════════════════════════════════════════════════════════
                    if (company.Logo != null && company.Logo.Length > 0)
                    {
                        page.Foreground()
                            .AlignCenter().AlignMiddle()
                            .Width(250).Height(250)
                            //.Opacity(0.06f)
                            .Image(company.Logo).FitArea();
                    }

                    // ══════════════════════════════════════════════════════════
                    // CONTENT
                    // ══════════════════════════════════════════════════════════
                    page.Content().PaddingTop(8).Column(col =>
                    {
                        // ── Title ─────────────────────────────────────────────
                        col.Item().AlignCenter()
                            .Text("TAX INVOICE")
                            .Bold().FontSize(14).FontColor("#1F3864");

                        col.Item().AlignCenter().PaddingBottom(4)
                            .Text("Sale").SemiBold().FontSize(10).FontColor("#555555");

                        col.Item().PaddingBottom(6).LineHorizontal(1).LineColor(FooterBlue);

                        // ── Customer + Bill info ──────────────────────────────
                        col.Item().PaddingBottom(6).Row(row =>
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

                        col.Item().PaddingBottom(6).LineHorizontal(1).LineColor(FooterBlue);

                        // ── Items table ───────────────────────────────────────
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(26);
                                columns.RelativeColumn(3);
                                columns.ConstantColumn(42);
                                columns.ConstantColumn(42);
                                columns.ConstantColumn(52);
                                columns.ConstantColumn(46);
                                columns.ConstantColumn(56);
                            });

                            table.Header(h =>
                            {
                                HeaderCell(h, "S/N", right: false);
                                HeaderCell(h, "Item Name", right: false);
                                HeaderCell(h, "Qty", right: true);
                                HeaderCell(h, "Unit", right: false);
                                HeaderCell(h, "Price", right: true);
                                HeaderCell(h, "Disc", right: true);
                                HeaderCell(h, "Net", right: true);
                            });

                            int i = 1;
                            foreach (var item in items)
                            {
                                string bg = (i % 2 == 0) ? "#EBF3FB" : "#FFFFFF";
                                DataCell(table, i.ToString(), bg, right: false);
                                DataCell(table, item.Name ?? "", bg, right: false);
                                DataCell(table, item.Qty.ToString("N2"), bg, right: true);
                                DataCell(table, item.UnitName ?? "", bg, right: false);
                                DataCell(table, item.Price.ToString("N2"), bg, right: true);
                                DataCell(table, (item.Discount ?? 0).ToString("N2"), bg, right: true);
                                DataCell(table, item.Total.ToString("N2"), bg, right: true);
                                i++;
                            }
                        });

                        // ── Totals ────────────────────────────────────────────
                        decimal totalDiscount = items.Sum(x => x.Discount ?? 0m);
                        col.Item().AlignRight().PaddingTop(8).Width(200).Column(totals =>
                        {
                            TotalRow(totals, "TOTAL", sale.Total.ToString("N2"), "#F2F2F2", bold: false);
                            TotalRow(totals, "TOTAL VAT", sale.Vat.ToString("N2"), "#FFFFFF", bold: false);
                            TotalRow(totals, "TOTAL DISCOUNT", totalDiscount.ToString("N2"), "#F2F2F2", bold: false);
                            TotalRow(totals, "TOTAL AMOUNT", sale.Net.ToString("N2"), FooterBlue, bold: true, textColor: "#FFFFFF");
                        });

                        // ── Signatures ────────────────────────────────────────
                        col.Item().PaddingTop(16).LineHorizontal(1).LineColor("#CCCCCC");
                        col.Item().PaddingTop(8).Row(sig =>
                        {
                            sig.RelativeColumn().Column(c =>
                            {
                                c.Item().Text("Created By").SemiBold();
                                c.Item().PaddingTop(22).Width(110)
                                    .LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().PaddingTop(3)
                                    .Text("Signature").FontSize(7.5f).FontColor("#888888");
                            });
                            sig.RelativeColumn().AlignRight().Column(c =>
                            {
                                c.Item().AlignRight().Text("Approved By").SemiBold();
                                c.Item().AlignRight().PaddingTop(22).Width(110)
                                    .LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().AlignRight().PaddingTop(3)
                                    .Text("Signature").FontSize(7.5f).FontColor("#888888");
                            });
                        });
                    });

                    // ══════════════════════════════════════════════════════════
                    // FOOTER
                    // ══════════════════════════════════════════════════════════
                    page.Footer().Column(footerCol =>
                    {
                        footerCol.Item().LineHorizontal(2).LineColor(FooterBlue);
                        footerCol.Item().PaddingTop(4).Row(footerRow =>
                        {
                            // Contact info
                            footerRow.RelativeColumn().Column(details =>
                            {
                                if (!string.IsNullOrEmpty(company.Phone))
                                    details.Item().Text($"Tel: {company.Phone}")
                                        .FontSize(7.5f).FontColor("#333333");
                                if (!string.IsNullOrEmpty(company.Email))
                                    details.Item().Text($"Email: {company.Email}")
                                        .FontSize(7.5f).FontColor("#333333");
                                if (!string.IsNullOrEmpty(company.Address))
                                    details.Item().Text($"Address: {company.Address}")
                                        .FontSize(7.5f).FontColor("#333333");
                            });

                            // Page number
                            footerRow.ConstantColumn(60).AlignCenter().AlignBottom()
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(s => s.FontSize(7.5f).FontColor("#777777"));
                                    t.CurrentPageNumber();
                                    t.Span(" / ");
                                    t.TotalPages();
                                });

                            // Colour strips
                            footerRow.ConstantColumn(44).AlignRight().Row(strips =>
                            {
                                strips.ConstantColumn(9).Background(FooterBlue);
                                strips.ConstantColumn(2);
                                strips.ConstantColumn(9).Background(FooterGreen2);
                                strips.ConstantColumn(2);
                                strips.ConstantColumn(9).Background(FooterGray);
                                strips.ConstantColumn(2);
                                strips.ConstantColumn(9).Background(AccentYellow);
                            });
                        });
                    });
                });
            }).GeneratePdf();
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

        private static void HeaderCell(TableCellDescriptor h,
            string text, bool right)
        {
            var cell = h.Cell().Background(FooterBlue)
                .PaddingVertical(4).PaddingHorizontal(3);
            var txt = cell.Text(text).Bold().FontSize(8).FontColor("#FFFFFF");
            if (right) txt.AlignRight();
        }

        private static void DataCell(TableDescriptor table,
            string text, string bg, bool right)
        {
            var cell = table.Cell().Background(bg)
                .BorderBottom(1).BorderColor("#DDDDDD")
                .PaddingVertical(3).PaddingHorizontal(3);
            var txt = cell.Text(text).FontSize(8);
            if (right) txt.AlignRight();
        }

        private static void TotalRow(ColumnDescriptor col,
            string label, string value,
            string bg, bool bold,
            string textColor = "#000000")
        {
            col.Item().Background(bg)
                .PaddingVertical(3).PaddingHorizontal(5)
                .Row(r =>
                {
                    var lbl = r.RelativeColumn().Text(label).FontSize(8).FontColor(textColor);
                    var val = r.ConstantColumn(70).AlignRight().Text(value).FontSize(8).FontColor(textColor);
                    if (bold) { lbl.Bold(); val.Bold(); }
                });
        }

        private static byte[] GetQrCode(byte[] qrCode)
        {
            if (qrCode != null && qrCode.Length > 0)
                return qrCode;

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot", "assets", "images", "DefaultQR.jpg");

            return System.IO.File.Exists(path)
                ? System.IO.File.ReadAllBytes(path)
                : null;
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
