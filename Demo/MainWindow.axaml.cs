using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;

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

        private void LoadDemoData()
        {
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
    }
}