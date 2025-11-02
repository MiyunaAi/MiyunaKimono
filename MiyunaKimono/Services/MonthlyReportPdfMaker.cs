using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System;
// เพิ่ม Alias สำหรับ MigraDoc
using Md = MigraDoc.DocumentObjectModel;

namespace MiyunaKimono.Services
{
    public static class MonthlyReportPdfMaker
    {
        public static string Create(MonthlyReportData data, string selectedMonthName, string selectedYear)
        {
            var doc = new Document();
            doc.Info.Title = $"{selectedMonthName} {selectedYear} Report";

            var section = doc.AddSection();
            section.PageSetup.LeftMargin = "1.5cm";
            section.PageSetup.RightMargin = "1.5cm";
            section.PageSetup.TopMargin = "1.5cm";
            section.PageSetup.BottomMargin = "1.5cm";

            // ใช้ฟอนต์ที่รองรับภาษาไทย (ถ้ามีในเครื่อง) ถ้าไม่มีจะใช้ฟอนต์ Default
            var defaultFont = new Font("Tahoma", 10);
            try
            {
                defaultFont = new Font("Leelawadee UI", 10);
            }
            catch
            {
                try { defaultFont = new Font("Tahoma", 10); } catch { }
            }

            // ✅✅✅ นี่คือจุดที่แก้ไข ✅✅✅
            // ❌ ลบบรรทัดเดิม: doc.DefaultPageSetup.HeaderDistance = "1cm";
            // ✅ เปลี่ยนเป็นบรรทัดนี้แทน:
            section.PageSetup.HeaderDistance = "1cm";

            doc.Styles[StyleNames.Normal].Font = defaultFont;


            // === 1. Header (โลโก้ซ้าย, ไตเติ้ลขวา) ===
            var headerTable = section.AddTable();
            headerTable.Borders.Width = 0; // ไม่มีเส้นขอบ

            var colLeft = headerTable.AddColumn("8cm");
            colLeft.Format.Alignment = ParagraphAlignment.Left;

            var colRight = headerTable.AddColumn("8cm");
            colRight.Format.Alignment = ParagraphAlignment.Right;

            var headerRow = headerTable.AddRow();

            // --- เซลล์ซ้าย (โลโก้) ---
            var leftCell = headerRow.Cells[0];
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_miyunaa.png");
            if (File.Exists(logoPath))
            {
                var logo = leftCell.AddImage(logoPath);
                logo.Width = "2.5cm";
                logo.LockAspectRatio = true;
            }
            var pLogo = leftCell.AddParagraph("Miyuna Kimono");
            pLogo.Format.Font.Size = 14;
            pLogo.Format.Font.Bold = true;

            // --- เซลล์ขวา (ไตเติ้ล) ---
            var rightCell = headerRow.Cells[1];
            rightCell.VerticalAlignment = VerticalAlignment.Bottom;

            string yearDisplay = selectedYear == "All Years" ? $"{DateTime.Now.Year}" : selectedYear;
            string monthDisplay = selectedMonthName == "All Month" ? "Annual" : selectedMonthName;

            var pTitle = rightCell.AddParagraph($"{monthDisplay} {yearDisplay}");
            pTitle.Format.Font.Size = 16;
            pTitle.Format.Font.Bold = true;
            var pSub = rightCell.AddParagraph("Monthly Report");
            pSub.Format.Font.Size = 12;
            pSub.Format.Font.Color = Colors.Gray;

            section.AddParagraph().AddLineBreak(); // เว้นวรรค

            // === 2. ตาราง Orders ===
            var pOrdersTitle = section.AddParagraph("Order Summary");
            pOrdersTitle.Format.Font.Size = 14;
            pOrdersTitle.Format.Font.Bold = true;
            pOrdersTitle.Format.SpaceAfter = "0.3cm";

            var orderTable = section.AddTable();
            orderTable.Borders.Width = 0.5;
            orderTable.Borders.Color = Colors.LightGray;

            // Columns: Date, No., Customer, Status, Total
            orderTable.AddColumn("3.5cm").Format.Alignment = ParagraphAlignment.Left; // Date
            orderTable.AddColumn("1cm").Format.Alignment = ParagraphAlignment.Center; // No.
            orderTable.AddColumn("5.5cm").Format.Alignment = ParagraphAlignment.Left; // Customer
            orderTable.AddColumn("3cm").Format.Alignment = ParagraphAlignment.Left; // Status
            orderTable.AddColumn("3cm").Format.Alignment = ParagraphAlignment.Right; // Total

            // Header Row
            var orderHeader = orderTable.AddRow();
            orderHeader.Shading.Color = Colors.LightGray; // สีเทาอ่อน
            orderHeader.Format.Font.Bold = true;
            orderHeader.Cells[0].AddParagraph("Date");
            orderHeader.Cells[1].AddParagraph("No.");
            orderHeader.Cells[2].AddParagraph("Customer");
            orderHeader.Cells[3].AddParagraph("Status");
            orderHeader.Cells[4].AddParagraph("Total");

            // Data Rows
            int orderNo = 1;
            foreach (var order in data.Orders)
            {
                var row = orderTable.AddRow();
                row.Cells[0].AddParagraph(order.Date.ToString("dd/MM/yyyy HH:mm"));
                row.Cells[1].AddParagraph((orderNo++).ToString());
                row.Cells[2].AddParagraph(order.CustomerName);
                row.Cells[3].AddParagraph(order.Status);
                row.Cells[4].AddParagraph($"{order.Total:N0} THB");
            }

            section.AddParagraph().AddLineBreak(); // เว้นวรรค

            // === 3. ตาราง Products ===
            var pProductsTitle = section.AddParagraph("Product Sales Summary");
            pProductsTitle.Format.Font.Size = 14;
            pProductsTitle.Format.Font.Bold = true;
            pProductsTitle.Format.SpaceAfter = "0.3cm";

            var prodTable = section.AddTable();
            prodTable.Borders.Width = 0.5;
            prodTable.Borders.Color = Colors.LightGray;

            // Columns: Product, Brand, Category, Item off, Total Sale
            prodTable.AddColumn("5cm").Format.Alignment = ParagraphAlignment.Left; // Product
            prodTable.AddColumn("2.5cm").Format.Alignment = ParagraphAlignment.Left; // Brand
            prodTable.AddColumn("2.5cm").Format.Alignment = ParagraphAlignment.Left; // Category
            prodTable.AddColumn("2cm").Format.Alignment = ParagraphAlignment.Center; // Item off (Qty)
            prodTable.AddColumn("4cm").Format.Alignment = ParagraphAlignment.Right; // Total Sale

            // Header Row
            var prodHeader = prodTable.AddRow();
            prodHeader.Shading.Color = Colors.LightGray; // สีเทาอ่อน
            prodHeader.Format.Font.Bold = true;
            prodHeader.Cells[0].AddParagraph("Product");
            prodHeader.Cells[1].AddParagraph("Brand");
            prodHeader.Cells[2].AddParagraph("Category");
            prodHeader.Cells[3].AddParagraph("Item off");
            prodHeader.Cells[4].AddParagraph("Total Sale");

            // Data Rows
            foreach (var prod in data.Products)
            {
                var row = prodTable.AddRow();
                row.Cells[0].AddParagraph(prod.ProductName);
                row.Cells[1].AddParagraph(prod.Brand ?? "-");
                row.Cells[2].AddParagraph(prod.Category ?? "-");
                row.Cells[3].AddParagraph(prod.ItemsOff.ToString());
                row.Cells[4].AddParagraph($"{prod.TotalSale:N0} THB");
            }

            section.AddParagraph().AddLineBreak(); // เว้นวรรค

            // === 4. ยอดรวมทั้งหมด ===
            var pGrandTotal = section.AddParagraph($"Sale Total in {monthDisplay} {data.GrandTotalSale:N0} THB");
            pGrandTotal.Format.Font.Size = 14;
            pGrandTotal.Format.Font.Bold = true;
            pGrandTotal.Format.Alignment = ParagraphAlignment.Right;

            // === สร้างไฟล์ PDF ===
            var renderer = new PdfDocumentRenderer(true); // true = Unicode
            renderer.Document = doc;
            renderer.RenderDocument();

            string tempPath = Path.Combine(Path.GetTempPath(), $"MonthlyReport_{yearDisplay}_{monthDisplay}.pdf");
            renderer.Save(tempPath);

            return tempPath;
        }
    }
}