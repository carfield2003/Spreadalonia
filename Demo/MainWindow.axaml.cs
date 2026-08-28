using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Spreadalonia;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Demo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Load some demo data with formulas
            LoadDemoData();
        }

        private void BasicDemoButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            LoadDemoData();
        }

        private void FeaturesButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            LoadFeatureDemo();
        }

        private void RgfButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            LoadRgfDemo();
        }

        private void LoadDemoData()
        {
            // Clear all formatting features and reset the table bounds
            SpreadsheetControl.MergedRanges = null;
            SpreadsheetControl.CellBackgrounds = null;
            SpreadsheetControl.CellBorders = null;
            SpreadsheetControl.MaxTableWidth = int.MaxValue - 2;
            SpreadsheetControl.MaxTableHeight = int.MaxValue - 2;
            SpreadsheetControl.CellFontSize?.Clear();
            SpreadsheetControl.CellTypefaces?.Clear();
            SpreadsheetControl.CellForeground?.Clear();
            SpreadsheetControl.CellTextAlignment?.Clear();
            SpreadsheetControl.CellVerticalAlignment?.Clear();
            SpreadsheetControl.CellMargin?.Clear();

            var data = new Dictionary<(int, int), string>
            {
                // Row 0: headers
                [(0, 0)] = "Item",
                [(1, 0)] = "Quantity",
                [(2, 0)] = "Unit Price",
                [(3, 0)] = "Total",

                // Row 1: first item
                [(0, 1)] = "Widget A",
                [(1, 1)] = "10",
                [(2, 1)] = "25.5",
                [(3, 1)] = "=B2*C2",

                // Row 2: second item
                [(0, 2)] = "Widget B",
                [(1, 2)] = "5",
                [(2, 2)] = "42.0",
                [(3, 2)] = "=B3*C3",

                // Row 3: third item
                [(0, 3)] = "Widget C",
                [(1, 3)] = "8",
                [(2, 3)] = "15.75",
                [(3, 3)] = "=B4*C4",

                // Row 5: summary
                [(0, 5)] = "Summary:",
                [(1, 5)] = "=SUM(B2:B4)",
                [(2, 5)] = "=AVERAGE(C2:C4)",
                [(3, 5)] = "=SUM(D2:D4)",

                // Row 6: count
                [(0, 6)] = "Count:",
                [(1, 6)] = "=COUNT(B2:B4)",
                [(3, 6)] = "=IF(B6>5,\"Many items\",\"Few items\")",

                // Row 7: max/min
                [(0, 7)] = "MAX Price:",
                [(2, 7)] = "=MAX(C2:C4)",
                [(0, 8)] = "MIN Price:",
                [(2, 8)] = "=MIN(C2:C4)",
            };

            // Register custom business functions
            SpreadsheetControl.FormulaEngine.Functions.Register("TAX", args =>
            {
                // Custom function: TAX(amount) - calculates 13% tax
                double amount = args.Count > 0 && args[0] != null
                    ? System.Convert.ToDouble(args[0], System.Globalization.CultureInfo.InvariantCulture)
                    : 0.0;
                return amount * 0.13;
            });

            SpreadsheetControl.FormulaEngine.Functions.Register("MY_FORMULA", args =>
            {
                // Example: a custom formula that could query your business system
                // MY_FORMULA("account001")
                string param = args.Count > 0 ? args[0]?.ToString() ?? "" : "";
                return $"Result for {param}";
            });

            // Load the demo data into the spreadsheet
            SpreadsheetControl.SetData(data);
        }

        private void LoadFeatureDemo()
        {
            // Clear formatting features, reset table bounds, then build a formatted report using the new API
            SpreadsheetControl.MergedRanges = null;
            SpreadsheetControl.CellBackgrounds = null;
            SpreadsheetControl.CellBorders = null;
            SpreadsheetControl.MaxTableWidth = int.MaxValue - 2;
            SpreadsheetControl.MaxTableHeight = int.MaxValue - 2;
            SpreadsheetControl.CellFontSize?.Clear();
            SpreadsheetControl.CellTypefaces?.Clear();
            SpreadsheetControl.CellForeground?.Clear();
            SpreadsheetControl.CellTextAlignment?.Clear();
            SpreadsheetControl.CellVerticalAlignment?.Clear();
            SpreadsheetControl.CellMargin?.Clear();

            var data = new Dictionary<(int, int), string>
            {
                [(0, 0)] = "销售报表 Sales Report",

                [(0, 1)] = "项目",
                [(1, 1)] = "数量",
                [(2, 1)] = "单价",
                [(3, 1)] = "金额",

                [(0, 2)] = "产品A",
                [(1, 2)] = "10",
                [(2, 2)] = "25.50",
                [(3, 2)] = "255.00",

                [(0, 3)] = "产品B",
                [(1, 3)] = "20",
                [(2, 3)] = "12.00",
                [(3, 3)] = "240.00",

                [(4, 2)] = "备注区域",
                [(4, 3)] = "此区域纵向合并展示说明文字",
            };

            SpreadsheetControl.SetData(data);

            // ---- Merged ranges ----
            SpreadsheetControl.MergedRanges = new List<SelectionRange>
            {
                new SelectionRange(0, 0, 4, 0),   // title spanning all 5 columns
                new SelectionRange(4, 2, 4, 3),   // vertical note area
            };

            // ---- Cell backgrounds ----
            var titleBlue = new SolidColorBrush(Color.Parse("#4472C4"));
            var headerLightBlue = new SolidColorBrush(Color.Parse("#D9E2F3"));
            var noteYellow = new SolidColorBrush(Color.Parse("#FFF2CC"));

            SpreadsheetControl.CellBackgrounds = new List<CellBackground>
            {
                new CellBackground(new SelectionRange(0, 0, 4, 0), titleBlue),
                new CellBackground(new SelectionRange(0, 1, 3, 1), headerLightBlue),
                new CellBackground(new SelectionRange(4, 2, 4, 3), noteYellow),
            };

            // ---- Cell borders ----
            var black = new SolidColorBrush(Color.Parse("#000000"));

            SpreadsheetControl.CellBorders = new List<CellBorder>
            {
                // outer frame
                new CellBorder(true, 0, 0, 4, black, 2),
                new CellBorder(true, 4, 0, 4, black, 2),
                new CellBorder(false, 0, 0, 4, black, 2),
                new CellBorder(false, 5, 0, 4, black, 2),

                // title bottom (thick)
                new CellBorder(true, 1, 0, 4, black, 2),

                // header bottom (thick)
                new CellBorder(true, 2, 0, 4, black, 2),

                // data row separators (thin, dashed to demonstrate the line-style API)
                new CellBorder(true, 3, 0, 4, black, 1, new DashStyle(new double[] { 4, 2 }, 0)),

                // vertical separators in the data area
                new CellBorder(false, 1, 1, 3, black, 1),
                new CellBorder(false, 2, 1, 3, black, 1),
                new CellBorder(false, 3, 1, 3, black, 1),
            };

            // ---- Cell-level styles ----
            var white = new SolidColorBrush(Color.Parse("#FFFFFF"));
            var darkRed = new SolidColorBrush(Color.Parse("#C00000"));

            // Title: bold white text, 18pt, centred
            SpreadsheetControl.CellTypefaces[(0, 0)] = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
            SpreadsheetControl.CellForeground[(0, 0)] = white;
            SpreadsheetControl.CellFontSize[(0, 0)] = 18;
            SpreadsheetControl.CellTextAlignment[(0, 0)] = TextAlignment.Center;
            SpreadsheetControl.CellVerticalAlignment[(0, 0)] = VerticalAlignment.Center;

            // Header row: bold, centred
            for (int col = 0; col <= 3; col++)
            {
                SpreadsheetControl.CellTypefaces[(col, 1)] = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
                SpreadsheetControl.CellTextAlignment[(col, 1)] = TextAlignment.Center;
                SpreadsheetControl.CellVerticalAlignment[(col, 1)] = VerticalAlignment.Center;
            }

            // Numbers right-aligned
            SpreadsheetControl.CellTextAlignment[(1, 2)] = TextAlignment.Right;
            SpreadsheetControl.CellTextAlignment[(2, 2)] = TextAlignment.Right;
            SpreadsheetControl.CellTextAlignment[(3, 2)] = TextAlignment.Right;
            SpreadsheetControl.CellTextAlignment[(1, 3)] = TextAlignment.Right;
            SpreadsheetControl.CellTextAlignment[(2, 3)] = TextAlignment.Right;
            SpreadsheetControl.CellTextAlignment[(3, 3)] = TextAlignment.Right;

            // Emphasise the last amount
            SpreadsheetControl.CellTypefaces[(3, 3)] = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
            SpreadsheetControl.CellForeground[(3, 3)] = darkRed;

            // Note cell: vertically centred
            SpreadsheetControl.CellVerticalAlignment[(4, 2)] = VerticalAlignment.Center;
            SpreadsheetControl.CellVerticalAlignment[(4, 3)] = VerticalAlignment.Center;

            // Redraw with the new style dictionaries applied
            SpreadsheetControl.Refresh();
        }

        private void LoadRgfDemo()
        {
            string rgfXml = @"<rgf width=""5"" height=""5"" left=""0"" top=""0"" right=""4"" bottom=""4"">
  <styles>
    <style name=""default"" type=""cell"">
      <font family=""Microsoft YaHei"" size=""9"" bold=""0"" italic=""0"" underline=""0""/>
      <align h=""left"" v=""center""/>
    </style>
    <style name=""title"" type=""cell"">
      <font family=""Microsoft YaHei"" size=""16"" bold=""1"" italic=""0"" underline=""0""/>
      <align h=""center"" v=""center""/>
    </style>
    <style name=""header"" type=""cell"">
      <font family=""Microsoft YaHei"" size=""10"" bold=""1"" italic=""0"" underline=""0""/>
      <align h=""center"" v=""center""/>
    </style>
    <style name=""right"" type=""cell"">
      <font family=""Microsoft YaHei"" size=""9"" bold=""0"" italic=""0"" underline=""0""/>
      <align h=""right"" v=""center""/>
    </style>
  </styles>
  <cols>
    <col from=""0"" to=""4"" width=""90""/>
  </cols>
  <rows>
    <row from=""0"" to=""4"" height=""30""/>
  </rows>
  <v-borders>
    <span from=""0"" size=""5""><border sides=""all"" style=""solid"" color=""#000000"" width=""1""/></span>
  </v-borders>
  <h-borders>
    <span from=""0"" size=""5""><border sides=""all"" style=""solid"" color=""#000000"" width=""1""/></span>
  </h-borders>
  <spans>
    <span from=""0"" size=""1"">
      <cell from=""0"" size=""5"" colspan=""5"" rowspan=""1"" style=""title"" background=""#4472C4"">季度销售汇总</cell>
    </span>
  </spans>
  <texts>
    <text from=""1"" size=""1"">
      <cell from=""0"" size=""1"" style=""header"" background=""#D9E2F3"">项目</cell>
      <cell from=""1"" size=""1"" style=""header"" background=""#D9E2F3"">Q1</cell>
      <cell from=""2"" size=""1"" style=""header"" background=""#D9E2F3"">Q2</cell>
      <cell from=""3"" size=""1"" style=""header"" background=""#D9E2F3"">Q3</cell>
      <cell from=""4"" size=""1"" style=""header"" background=""#D9E2F3"">Q4</cell>
    </text>
    <text from=""2"" size=""1"">
      <cell from=""0"" size=""1"" style=""default"">产品A</cell>
      <cell from=""1"" size=""1"" style=""right"">120</cell>
      <cell from=""2"" size=""1"" style=""right"">150</cell>
      <cell from=""3"" size=""1"" style=""right"">180</cell>
      <cell from=""4"" size=""1"" style=""right"" background=""#FFF2CC"">450</cell>
    </text>
    <text from=""3"" size=""1"">
      <cell from=""0"" size=""1"" style=""default"">产品B</cell>
      <cell from=""1"" size=""1"" style=""right"">80</cell>
      <cell from=""2"" size=""1"" style=""right"">95</cell>
      <cell from=""3"" size=""1"" style=""right"">110</cell>
      <cell from=""4"" size=""1"" style=""right"" background=""#FFF2CC"">285</cell>
    </text>
    <text from=""4"" size=""1"">
      <cell from=""0"" size=""1"" style=""default"" background=""#E2EFDA"">合计</cell>
      <cell from=""1"" size=""1"" style=""right"" background=""#E2EFDA"">200</cell>
      <cell from=""2"" size=""1"" style=""right"" background=""#E2EFDA"">245</cell>
      <cell from=""3"" size=""1"" style=""right"" background=""#E2EFDA"">290</cell>
      <cell from=""4"" size=""1"" style=""right"" background=""#E2EFDA"">735</cell>
    </text>
  </texts>
</rgf>";

            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(rgfXml)))
            {
                SpreadsheetControl.LoadRgf(stream);
            }
        }
    }
}
