using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Spreadalonia;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
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

            // Demonstrate the LinkClick event raised by LinkCell / NumericLinkCell.
            SpreadsheetControl.LinkClick += (s, e) =>
            {
                Console.WriteLine($"[LinkClick] cell ({e.Left}, {e.Top}) -> {e.LinkPara}");
            };

            // Tree / numeric cell demo events (TreeExpand, InvalidInput, CellEditEnded).
            WireTreeCellEvents();

            // Demonstrate the CellTapped event raised by ButtonCell: instead of editing the text
            // the host opens its own picker and writes the chosen auxiliary item back.
            SpreadsheetControl.CellTapped += (s, e) =>
            {
                Console.WriteLine($"[CellTapped] cell ({e.Column}, {e.Row}) type='{e.CellType}' tag='{e.Tag}'");

                PickAuxiliaryItem(e.Column, e.Row);
            };

            SpreadsheetControl.CellDoubleTapped += (s, e) =>
            {
                Console.WriteLine($"[CellDoubleTapped] cell ({e.Column}, {e.Row}) type='{e.CellType}'");
            };

            // Automated screenshot test for the TitleCell demo.
            if (Environment.GetCommandLineArgs().Any(a => a == "--title-cell-test"))
            {
                Opened += async (_, _) =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(500);
                        TitleCellButton_Click(null!, null!);
                        await System.Threading.Tasks.Task.Delay(800);
                        SaveScreenshot(@"C:\Users\89664\Desktop\title_cell_test.png");
                        Console.WriteLine("[Test] TitleCell screenshot saved.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Test] EXCEPTION: " + ex);
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                };
            }

            // Automated screenshot test for the LinkCell demo.
            if (Environment.GetCommandLineArgs().Any(a => a == "--link-cell-test"))
            {
                Opened += async (_, _) =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(500);
                        LinkCellButton_Click(null!, null!);
                        await System.Threading.Tasks.Task.Delay(800);
                        SaveScreenshot(@"C:\Users\89664\Desktop\link_cell_test.png");
                        Console.WriteLine("[Test] LinkCell screenshot saved.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Test] EXCEPTION: " + ex);
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                };
            }

            // Automated screenshot test for the TreeGradeCell / NumericCell demo.
            if (Environment.GetCommandLineArgs().Any(a => a == "--tree-cell-test"))
            {
                Opened += async (_, _) =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(500);
                        TreeCellButton_Click(null!, null!);
                        await System.Threading.Tasks.Task.Delay(800);
                        SaveScreenshot(@"C:\Users\89664\Desktop\tree_cell_test.png");
                        Console.WriteLine("[Test] TreeCell screenshot saved.");

                        // Collapse the first level-1 node and shoot again.
                        SpreadsheetControl.ToggleTreeCell(0, 1);
                        await System.Threading.Tasks.Task.Delay(500);
                        SaveScreenshot(@"C:\Users\89664\Desktop\tree_cell_collapsed.png");
                        Console.WriteLine("[Test] TreeCell collapsed screenshot saved.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Test] EXCEPTION: " + ex);
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                };
            }

            // Automated screenshot test for the hidden-column / ButtonCell demo: shoots the sheet
            // with all columns visible and again after hiding 年初余额 / 累计发生 (columns 2..5).
            if (Environment.GetCommandLineArgs().Any(a => a == "--hidden-column-test"))
            {
                Opened += async (_, _) =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(500);
                        HiddenColumnButton_Click(null!, null!);
                        await System.Threading.Tasks.Task.Delay(800);
                        SaveScreenshot(@"C:\Users\89664\Desktop\hidden_column_all.png");
                        Console.WriteLine("[Test] All columns screenshot saved.");

                        // Scroll the auxiliary-item column (8) into view: the RefCell style
                        // magnifier icons at its right edge must be visible.
                        SpreadsheetControl.Selection = ImmutableList.Create(new SelectionRange(8, 2, 8, 2));
                        await System.Threading.Tasks.Task.Delay(600);
                        SaveScreenshot(@"C:\Users\89664\Desktop\hidden_column_aux_icons.png");
                        Console.WriteLine("[Test] Auxiliary icon column screenshot saved.");

                        SpreadsheetControl.SetColumnsVisible(new[] { 2, 3, 4, 5 }, false);
                        await System.Threading.Tasks.Task.Delay(500);
                        SaveScreenshot(@"C:\Users\89664\Desktop\hidden_column_hidden.png");
                        Console.WriteLine("[Test] Hidden columns screenshot saved.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Test] EXCEPTION: " + ex);
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                };
            }

            // Automated screenshot test for the numeric ellipsis truncation: narrow columns show
            // "1,120,7…" style ellipses instead of clipping the formatted value mid-glyph, for
            // both NumericCell and NumericLinkCell.
            if (Environment.GetCommandLineArgs().Any(a => a == "--numeric-ellipsis-test"))
            {
                Opened += async (_, _) =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(500);

                        SpreadsheetControl.MergedRanges = null;
                        SpreadsheetControl.CellBackgrounds = null;
                        SpreadsheetControl.CellBorders = null;
                        SpreadsheetControl.MaxTableWidth = 3;
                        SpreadsheetControl.MaxTableHeight = int.MaxValue - 2;
                        SpreadsheetControl.TitleCells?.Clear();
                        SpreadsheetControl.LinkCells?.Clear();
                        SpreadsheetControl.TreeCells?.Clear();
                        SpreadsheetControl.NumericCells?.Clear();
                        SpreadsheetControl.ButtonCells?.Clear();
                        SpreadsheetControl.CellFontSize?.Clear();
                        SpreadsheetControl.CellTypefaces?.Clear();
                        SpreadsheetControl.CellForeground?.Clear();
                        SpreadsheetControl.CellTextAlignment?.Clear();
                        SpreadsheetControl.CellVerticalAlignment?.Clear();
                        SpreadsheetControl.CellMargin?.Clear();
                        SpreadsheetControl.ClearContent();
                        SpreadsheetControl.UnfreezePanes();
                        SpreadsheetControl.ShowAllRows();
                        SpreadsheetControl.ShowAllColumns();

                        var data = new Dictionary<(int, int), string>
                        {
                            [(0, 0)] = "编码", [(1, 0)] = "期初余额", [(2, 0)] = "调整金额",
                            [(0, 1)] = "101",  [(1, 1)] = "1120700.00", [(2, 1)] = "1120700.5",
                            [(0, 2)] = "102",  [(1, 2)] = "564773",      [(2, 2)] = "56477.36",
                            [(0, 3)] = "113",  [(1, 3)] = "104500.12",   [(2, 3)] = "104500.12",
                            [(0, 4)] = "115",  [(1, 4)] = "2820435.55",  [(2, 4)] = "282043.55",
                            [(0, 5)] = "118",  [(1, 5)] = "9025.00",     [(2, 5)] = "9025.00",
                            [(0, 6)] = "119",  [(1, 6)] = "-4500.5",     [(2, 6)] = "-4500.5",
                        };

                        SpreadsheetControl.SetData(data);

                        for (int c = 0; c < 3; c++)
                        {
                            SpreadsheetControl.CellTypefaces[(c, 0)] = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
                            SpreadsheetControl.CellTextAlignment[(c, 0)] = TextAlignment.Center;
                            SpreadsheetControl.CellVerticalAlignment[(c, 0)] = VerticalAlignment.Center;
                        }

                        for (int r = 1; r <= 6; r++)
                        {
                            SpreadsheetControl.CellTextAlignment[(0, r)] = TextAlignment.Left;

                            // Column 1: plain NumericCell; column 2: NumericLinkCell with the same
                            // formatting, both narrow enough to force the ellipsis on big values.
                            SpreadsheetControl.NumericCells[(1, r)] = new NumericCell(2) { NegativeRed = true };
                            SpreadsheetControl.NumericCells[(2, r)] = new NumericCell(2) { NegativeRed = true };
                            SpreadsheetControl.LinkCells[(2, r)] = new NumericLinkCell("link" + r, false, false, 2);
                        }

                        SpreadsheetControl.SetWidth(new Dictionary<int, double>
                        {
                            [0] = 60, [1] = 64, [2] = 64,
                        });

                        SpreadsheetControl.Refresh();

                        await System.Threading.Tasks.Task.Delay(500);
                        SaveScreenshot(@"C:\Users\89664\Desktop\numeric_ellipsis.png");
                        Console.WriteLine("[Test] Numeric ellipsis screenshot saved.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Test] EXCEPTION: " + ex);
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                };
            }

            // Automated test for the empty-value placeholder of a NumericLinkCell: a zero, missing
            // or invalid amount is drawn as "…" and stays clickable over the whole cell, while the
            // exported value stays empty.
            if (Environment.GetCommandLineArgs().Any(a => a == "--numeric-link-empty-test"))
            {
                Opened += async (_, _) =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(500);

                        SpreadsheetControl.MergedRanges = null;
                        SpreadsheetControl.CellBackgrounds = null;
                        SpreadsheetControl.CellBorders = null;
                        SpreadsheetControl.MaxTableWidth = 3;
                        SpreadsheetControl.MaxTableHeight = 8;
                        SpreadsheetControl.TitleCells?.Clear();
                        SpreadsheetControl.LinkCells?.Clear();
                        SpreadsheetControl.TreeCells?.Clear();
                        SpreadsheetControl.NumericCells?.Clear();
                        SpreadsheetControl.ButtonCells?.Clear();
                        SpreadsheetControl.CellFontSize?.Clear();
                        SpreadsheetControl.CellTypefaces?.Clear();
                        SpreadsheetControl.CellForeground?.Clear();
                        SpreadsheetControl.CellTextAlignment?.Clear();
                        SpreadsheetControl.CellVerticalAlignment?.Clear();
                        SpreadsheetControl.CellMargin?.Clear();
                        SpreadsheetControl.ClearContent();
                        SpreadsheetControl.UnfreezePanes();
                        SpreadsheetControl.ShowAllRows();
                        SpreadsheetControl.ShowAllColumns();

                        var data = new Dictionary<(int, int), string>
                        {
                            [(0, 0)] = "科目", [(1, 0)] = "期初余额", [(2, 0)] = "调整金额",
                            [(0, 1)] = "1001 库存现金", [(1, 1)] = "0", [(2, 1)] = "0",
                            [(0, 2)] = "1122 应收账款", [(1, 2)] = "104.50", [(2, 2)] = "104.50",
                            [(0, 3)] = "2202 应付账款", [(1, 3)] = "-3200.75", [(2, 3)] = "abc",
                            [(0, 4)] = "1403 原材料", [(1, 4)] = "123456789012.55", [(2, 4)] = "0.004",
                            // Row 5 carries no data at all: the placeholder still has to be drawn.
                            [(0, 5)] = "5001 主营业务收入",
                        };

                        SpreadsheetControl.SetData(data);

                        for (int c = 0; c < 3; c++)
                        {
                            SpreadsheetControl.CellTypefaces[(c, 0)] = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
                            SpreadsheetControl.CellTextAlignment[(c, 0)] = TextAlignment.Center;
                        }

                        for (int r = 1; r <= 5; r++)
                        {
                            SpreadsheetControl.CellTextAlignment[(1, r)] = TextAlignment.Right;
                            SpreadsheetControl.CellTextAlignment[(2, r)] = TextAlignment.Right;

                            // No argument other than the link parameter: the placeholder and the
                            // cell-wide hit area of an empty value have to be on by default.
                            SpreadsheetControl.LinkCells[(1, r)] = new NumericLinkCell("debit/" + r);
                            SpreadsheetControl.LinkCells[(2, r)] = new NumericLinkCell("credit/" + r);
                        }

                        SpreadsheetControl.SetWidth(new Dictionary<int, double>
                        {
                            [0] = 150, [1] = 110, [2] = 110,
                        });

                        SpreadsheetControl.Refresh();

                        await System.Threading.Tasks.Task.Delay(500);
                        SaveScreenshot(@"C:\Users\89664\Desktop\numeric_link_empty.png");
                        Console.WriteLine("[Test] NumericLinkCell empty placeholder screenshot saved.");

                        ReportNumericLinkEmpty();

                        // R7: the placeholder is display only; the exported workbook must contain
                        // the underlying empty value, not the "…" character.
                        string xlsxPath = @"C:\Users\89664\Desktop\numeric_link_empty.xlsx";
                        SpreadsheetControl.SaveXlsx(xlsxPath);
                        using (var workbook = new ClosedXML.Excel.XLWorkbook(xlsxPath))
                        {
                            var sheet = workbook.Worksheet(1);
                            string b2 = sheet.Cell(2, 2).GetValue<string>();
                            string c2 = sheet.Cell(2, 3).GetValue<string>();
                            string b3 = sheet.Cell(3, 2).GetValue<string>();
                            Console.WriteLine("[Test] xlsx B2 (placeholder) = '" + b2 + "' (should be empty)");
                            Console.WriteLine("[Test] xlsx C2 (placeholder) = '" + c2 + "' (should be empty)");
                            Console.WriteLine("[Test] xlsx B3 (value) = '" + b3 + "' (should be 104.50)");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Test] EXCEPTION: " + ex);
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                };
            }

            // Automated screenshot test for the frozen rows / columns demo: shoots the sheet at the
            // top-left corner, after scrolling to the bottom-right corner (the frozen header row and
            // the 月 / 日 columns have to stay in place), and with a single axis frozen.
            if (Environment.GetCommandLineArgs().Any(a => a == "--sheet-state-test"))
            {
                Opened += async (_, _) =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(500);

                        SpreadsheetControl.MergedRanges = null;
                        SpreadsheetControl.CellBackgrounds = null;
                        SpreadsheetControl.CellBorders = null;
                        SpreadsheetControl.MaxTableWidth = 6;
                        SpreadsheetControl.MaxTableHeight = 20;
                        SpreadsheetControl.ClearContent();
                        SpreadsheetControl.UnfreezePanes();
                        SpreadsheetControl.ShowAllRows();
                        SpreadsheetControl.ShowAllColumns();

                        // --- Worksheet A: the implicit first worksheet ---
                        SpreadsheetControl.SetData(new Dictionary<(int, int), string>
                        {
                            [(0, 0)] = "A-00", [(1, 0)] = "A-01",
                            [(0, 1)] = "A-10", [(1, 4)] = "A-14",
                        });
                        SpreadsheetControl.SetWidth(new Dictionary<int, double> { [0] = 200, [1] = 120 });
                        SpreadsheetControl.FreezePanes(2, 0);
                        SpreadsheetControl.HideColumn(2);
                        SpreadsheetControl.MergedRanges = System.Collections.Immutable.ImmutableList.Create(new SelectionRange(0, 0, 2, 0));
                        SpreadsheetControl.Selection = System.Collections.Immutable.ImmutableList.Create(new SelectionRange(1, 4));

                        Console.WriteLine($"[SheetState] sheets={SpreadsheetControl.Sheets.Count} current='{SpreadsheetControl.CurrentSheet?.Name}' index={SpreadsheetControl.CurrentSheetIndex}");

                        // --- Worksheet B ---
                        SpreadsheetSheet sheetB = SpreadsheetControl.AddSheet("B");
                        Console.WriteLine($"[SheetState] added '{sheetB.Name}' at {sheetB.Index}; sheets={SpreadsheetControl.Sheets.Count}");

                        SpreadsheetControl.CurrentSheetIndex = 1;
                        await System.Threading.Tasks.Task.Delay(100);

                        SpreadsheetControl.SetData(new Dictionary<(int, int), string>
                        {
                            [(0, 0)] = "B-00", [(1, 1)] = "B-11",
                        });
                        SpreadsheetControl.SetWidth(new Dictionary<int, double> { [0] = 80 });
                        SpreadsheetControl.Selection = System.Collections.Immutable.ImmutableList.Create(new SelectionRange(3, 6));

                        double bWidth = SpreadsheetControl.GetCellSize(0, 0).width;
                        bool bOk = bWidth == 80
                            && SpreadsheetControl.FrozenRowCount == 0
                            && SpreadsheetControl.HiddenColumns.Count == 0
                            && SpreadsheetControl.MergedRanges.Count == 0
                            && SpreadsheetControl.Data[(0, 0)] == "B-00";
                        Console.WriteLine($"[SheetState] B isolated: width={bWidth} frozen={SpreadsheetControl.FrozenRowCount} hiddenCols={SpreadsheetControl.HiddenColumns.Count} merged={SpreadsheetControl.MergedRanges.Count} A1='{SpreadsheetControl.Data[(0, 0)]}' -> {(bOk ? "PASS" : "FAIL")}");

                        // --- Back to worksheet A: everything has to be restored ---
                        SpreadsheetControl.CurrentSheetIndex = 0;
                        await System.Threading.Tasks.Task.Delay(100);

                        double aWidth = SpreadsheetControl.GetCellSize(0, 0).width;
                        bool aOk = aWidth == 200
                            && SpreadsheetControl.FrozenRowCount == 2
                            && SpreadsheetControl.HiddenColumns.Contains(2)
                            && SpreadsheetControl.MergedRanges.Count == 1
                            && SpreadsheetControl.Selection.Count == 1 && SpreadsheetControl.Selection[0].Left == 1 && SpreadsheetControl.Selection[0].Top == 4
                            && SpreadsheetControl.Data[(0, 0)] == "A-00"
                            && SpreadsheetControl.Data[(1, 4)] == "A-14";
                        Console.WriteLine($"[SheetState] A restored: width={aWidth} frozen={SpreadsheetControl.FrozenRowCount} hiddenCols=[{string.Join(",", SpreadsheetControl.HiddenColumns)}] merged={SpreadsheetControl.MergedRanges.Count} selection={SpreadsheetControl.Selection[0].Left},{SpreadsheetControl.Selection[0].Top} A1='{SpreadsheetControl.Data[(0, 0)]}' B5='{SpreadsheetControl.Data[(1, 4)]}' -> {(aOk ? "PASS" : "FAIL")}");

                        SpreadsheetControl.CurrentSheetIndex = 1;
                        await System.Threading.Tasks.Task.Delay(100);
                        bool bStillOk = SpreadsheetControl.Data[(0, 0)] == "B-00" && SpreadsheetControl.Data[(1, 1)] == "B-11";
                        Console.WriteLine($"[SheetState] B intact after switching back -> {(bStillOk ? "PASS" : "FAIL")}");

                        Console.WriteLine($"[SheetState] RESULT {(aOk && bOk && bStillOk ? "PASS" : "FAIL")}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SheetState] EXCEPTION {ex}");
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                };
            }

            // Automated test for the sheet tab strip: visibility (single sheet is hidden by default
            // and shown after a second sheet is added), click switching, hidden sheets and the
            // rename / delete / move API.
            if (Environment.GetCommandLineArgs().Any(a => a == "--sheet-tab-test"))
            {
                Opened += async (_, _) =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(500);

                        SpreadsheetControl.MergedRanges = null;
                        SpreadsheetControl.CellBackgrounds = null;
                        SpreadsheetControl.CellBorders = null;
                        SpreadsheetControl.MaxTableWidth = 6;
                        SpreadsheetControl.MaxTableHeight = 20;
                        SpreadsheetControl.ClearContent();
                        SpreadsheetControl.UnfreezePanes();
                        SpreadsheetControl.ShowAllRows();
                        SpreadsheetControl.ShowAllColumns();

                        // 1) Single sheet by default: the strip must be hidden and take no space.
                        SpreadsheetSheet original = SpreadsheetControl.Sheets[0];
                        bool singleHidden = !SpreadsheetControl.SheetTabStripControlInternal.IsVisible
                            && SpreadsheetControl.SheetTabStripControlInternal.TabButtons.Count == 0
                            && SpreadsheetControl.Sheets.Count == 1;
                        Console.WriteLine($"[SheetTab] single-sheet hidden: visible={SpreadsheetControl.SheetTabStripControlInternal.IsVisible} tabs={SpreadsheetControl.SheetTabStripControlInternal.TabButtons.Count} -> {(singleHidden ? "PASS" : "FAIL")}");

                        // 2) Add a second sheet: the strip must show two tabs and the new sheet must
                        // not become the current one.
                        SpreadsheetControl.AddSheet("B");
                        SpreadsheetControl.AddSheet("C");
                        bool visibleOk = SpreadsheetControl.SheetTabStripControlInternal.IsVisible
                            && SpreadsheetControl.SheetTabStripControlInternal.TabButtons.Count == 3
                            && SpreadsheetControl.CurrentSheet == original;
                        Console.WriteLine($"[SheetTab] 3-sheet visible: visible={SpreadsheetControl.SheetTabStripControlInternal.IsVisible} tabs={SpreadsheetControl.SheetTabStripControlInternal.TabButtons.Count} current='{SpreadsheetControl.CurrentSheet.Name}' -> {(visibleOk ? "PASS" : "FAIL")}");

                        // 3) Click the second tab to switch to "B" -> the tab selection must follow. The
                        // tabs are plain Borders with a pointer handler (no Button.Click), so the press
                        // and the release are simulated explicitly.
                        SimulateTabClick(SpreadsheetControl.SheetTabStripControlInternal.TabButtons[1]);
                        await System.Threading.Tasks.Task.Delay(100);
                        bool clickOk = SpreadsheetControl.CurrentSheetIndex == 1
                            && SpreadsheetControl.CurrentSheet.Name == "B";
                        Console.WriteLine($"[SheetTab] click switches: index={SpreadsheetControl.CurrentSheetIndex} name='{SpreadsheetControl.CurrentSheet.Name}' -> {(clickOk ? "PASS" : "FAIL")}");

                        // 4) Ctrl+PageDown goes to "C" and wraps back to the first sheet.
                        SpreadsheetControl.MoveToNextSheet();
                        await System.Threading.Tasks.Task.Delay(100);
                        bool pageDownOk = SpreadsheetControl.CurrentSheetIndex == 2;
                        Console.WriteLine($"[SheetTab] Ctrl+PageDown: index={SpreadsheetControl.CurrentSheetIndex} -> {(pageDownOk ? "PASS" : "FAIL")}");

                        SpreadsheetControl.MoveToNextSheet();
                        await System.Threading.Tasks.Task.Delay(100);
                        bool wrapOk = SpreadsheetControl.CurrentSheetIndex == 0;
                        Console.WriteLine($"[SheetTab] Ctrl+PageDown wraps: index={SpreadsheetControl.CurrentSheetIndex} -> {(wrapOk ? "PASS" : "FAIL")}");

                        // 5) Hidden sheets are not shown unless asked for, but the current one
                        // always is.
                        SpreadsheetSheet b = SpreadsheetControl.Sheets[1];
                        b.IsHidden = true;
                        SpreadsheetControl.RefreshSheetTabs();
                        bool hiddenOk = SpreadsheetControl.SheetTabStripControlInternal.TabButtons.Count == 2
                            && !SpreadsheetControl.SheetTabStripControlInternal.TabSheets.Contains(b);
                        Console.WriteLine($"[SheetTab] hidden by default: tabs={SpreadsheetControl.SheetTabStripControlInternal.TabButtons.Count} hasHidden={SpreadsheetControl.SheetTabStripControlInternal.TabSheets.Contains(b)} -> {(hiddenOk ? "PASS" : "FAIL")}");

                        // Jumping to a hidden worksheet in code still gives it a tab, so that the
                        // user can always find the way back to the sheet that is being displayed.
                        SpreadsheetControl.CurrentSheetIndex = 1;
                        await System.Threading.Tasks.Task.Delay(100);
                        bool hiddenCurrentOk = SpreadsheetControl.CurrentSheet == b
                            && SpreadsheetControl.SheetTabStripControlInternal.TabSheets.Contains(b);
                        Console.WriteLine($"[SheetTab] hidden current keeps a tab: current='{SpreadsheetControl.CurrentSheet?.Name}' tabs={SpreadsheetControl.SheetTabStripControlInternal.TabButtons.Count} -> {(hiddenCurrentOk ? "PASS" : "FAIL")}");

                        SpreadsheetControl.SheetTabHiddenSheetsVisible = true;
                        bool hiddenShownOk = SpreadsheetControl.SheetTabStripControlInternal.TabButtons.Count == 3;
                        Console.WriteLine($"[SheetTab] hidden shown when asked: tabs={SpreadsheetControl.SheetTabStripControlInternal.TabButtons.Count} -> {(hiddenShownOk ? "PASS" : "FAIL")}");

                        b.IsHidden = false;
                        SpreadsheetControl.SheetTabHiddenSheetsVisible = false;
                        SpreadsheetControl.CurrentSheetIndex = 0;
                        SpreadsheetControl.RefreshSheetTabs();
                        await System.Threading.Tasks.Task.Delay(100);

                        // 6) Rename / delete / move from the API.
                        string newName = SpreadsheetControl.RenameSheet(SpreadsheetControl.Sheets[2], "Budget");
                        string tabText = (SpreadsheetControl.SheetTabStripControlInternal.TabButtons[2].Child as TextBlock)?.Text;
                        bool renameOk = newName == "Budget" && SpreadsheetControl.Sheets[2].Name == "Budget" && tabText == "Budget";
                        Console.WriteLine($"[SheetTab] rename: name='{newName}' tab='{tabText}' -> {(renameOk ? "PASS" : "FAIL")}");

                        bool movedOk = SpreadsheetControl.MoveSheet(SpreadsheetControl.Sheets[2], 0)
                            && SpreadsheetControl.Sheets[0].Name == "Budget"
                            && SpreadsheetControl.CurrentSheet == original
                            && SpreadsheetControl.CurrentSheetIndex == SpreadsheetControl.IndexOfSheet(original);
                        Console.WriteLine($"[SheetTab] move: sheets[0]='{SpreadsheetControl.Sheets[0].Name}' current='{SpreadsheetControl.CurrentSheet?.Name}' index={SpreadsheetControl.CurrentSheetIndex} -> {(movedOk ? "PASS" : "FAIL")}");

                        SpreadsheetControl.SheetTabAllowDelete = true;
                        bool removedOk = SpreadsheetControl.RemoveSheet(SpreadsheetControl.Sheets[0]) && SpreadsheetControl.Sheets.Count == 2;
                        bool secondRemoved = SpreadsheetControl.RemoveSheet(SpreadsheetControl.Sheets[0]) && SpreadsheetControl.Sheets.Count == 1;
                        bool lastKept = !SpreadsheetControl.RemoveSheet(SpreadsheetControl.Sheets[0]) && SpreadsheetControl.Sheets.Count == 1
                            && !SpreadsheetControl.SheetTabStripControlInternal.IsVisible;
                        Console.WriteLine($"[SheetTab] delete: first={removedOk} second={secondRemoved} lastKept={lastKept} count={SpreadsheetControl.Sheets.Count} visible={SpreadsheetControl.SheetTabStripControlInternal.IsVisible} -> {(removedOk && secondRemoved && lastKept ? "PASS" : "FAIL")}");

                        bool resultOk = singleHidden && visibleOk && clickOk && pageDownOk && wrapOk && hiddenOk && hiddenCurrentOk && hiddenShownOk && renameOk && movedOk && removedOk && secondRemoved && lastKept;
                        Console.WriteLine($"[SheetTab] RESULT {(resultOk ? "PASS" : "FAIL")}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SheetTab] EXCEPTION {ex}");
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                };
            }

            // Automated test for the sheet-specific API: writing to, sizing and re-shaping a
            // worksheet that is NOT the one being displayed.
            if (Environment.GetCommandLineArgs().Any(a => a == "--sheet-api-test"))
            {
                Opened += async (_, _) =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(500);

                        SpreadsheetControl.MergedRanges = null;
                        SpreadsheetControl.CellBackgrounds = null;
                        SpreadsheetControl.CellBorders = null;
                        SpreadsheetControl.MaxTableWidth = 8;
                        SpreadsheetControl.MaxTableHeight = 20;
                        SpreadsheetControl.ClearContent();
                        SpreadsheetControl.UnfreezePanes();
                        SpreadsheetControl.ShowAllRows();
                        SpreadsheetControl.ShowAllColumns();

                        SpreadsheetSheet a = SpreadsheetControl.CurrentSheet;
                        SpreadsheetSheet b = SpreadsheetControl.AddSheet("Second");

                        // 1) Write into a worksheet that is not being displayed.
                        SpreadsheetControl.SetData(b, new Dictionary<(int, int), string>
                        {
                            [(0, 0)] = "B-00", [(1, 0)] = "B-10", [(0, 1)] = "=1+2"
                        });
                        SpreadsheetControl.SetWidth(b, new Dictionary<int, double> { [0] = 111 });

                        bool isolated = SpreadsheetControl.CurrentSheet == a
                            && a.Data.Count == 0
                            && b.Data.Count == 3;
                        Console.WriteLine($"[SheetApi] write to non-current: current='{SpreadsheetControl.CurrentSheet?.Name}' aData={a.Data.Count} bData={b.Data.Count} -> {(isolated ? "PASS" : "FAIL")}");

                        // 2) Switching to it shows exactly what was written, formula included.
                        SpreadsheetControl.CurrentSheetIndex = 1;
                        await System.Threading.Tasks.Task.Delay(100);
                        object formulaValue = b.FormulaCells.TryGetValue((0, 1), out Spreadalonia.Formula.CellData fc) ? fc.CachedValue : null;
                        bool shown = SpreadsheetControl.Data[(0, 0)] == "B-00" && SpreadsheetControl.Data[(1, 0)] == "B-10"
                            && Math.Abs(SpreadsheetControl.GetCellSize(0, 0).width - 111) < 0.01
                            && formulaValue != null && Convert.ToDouble(formulaValue) == 3;
                        Console.WriteLine($"[SheetApi] non-current becomes visible: A1='{SpreadsheetControl.Data[(0, 0)]}' B1='{SpreadsheetControl.Data[(1, 0)]}' width={SpreadsheetControl.GetCellSize(0, 0).width} formula={formulaValue} -> {(shown ? "PASS" : "FAIL")}");

                        // 3) InsertColumns / DeleteColumns on a sheet that is not being displayed.
                        SpreadsheetControl.CurrentSheetIndex = 0;
                        await System.Threading.Tasks.Task.Delay(100);

                        SpreadsheetControl.InsertColumns(b, 0, 1);
                        object movedFormula = b.FormulaCells.TryGetValue((1, 1), out Spreadalonia.Formula.CellData fc2) ? fc2.CachedValue : null;
                        bool inserted = !b.Data.ContainsKey((0, 0)) && !b.Data.ContainsKey((0, 1))
                            && b.Data[(1, 0)] == "B-00" && b.Data[(2, 0)] == "B-10" && b.Data[(1, 1)] == "=1+2"
                            && b.ColumnWidths.TryGetValue(1, out double w) && w == 111
                            && movedFormula != null && Convert.ToDouble(movedFormula) == 3;
                        Console.WriteLine($"[SheetApi] insert column on non-current: B1='{(b.Data.ContainsKey((1, 0)) ? b.Data[(1, 0)] : "<none>")}' width1={(b.ColumnWidths.TryGetValue(1, out double w2) ? w2 : -1)} formula={movedFormula} -> {(inserted ? "PASS" : "FAIL")}");

                        SpreadsheetControl.DeleteColumns(b, 0, 1);
                        bool deleted = b.Data[(0, 0)] == "B-00" && b.Data[(1, 0)] == "B-10" && b.Data[(0, 1)] == "=1+2"
                            && b.ColumnWidths.TryGetValue(0, out double w3) && w3 == 111;
                        Console.WriteLine($"[SheetApi] delete column on non-current: A1='{(b.Data.ContainsKey((0, 0)) ? b.Data[(0, 0)] : "<none>")}' B1='{(b.Data.ContainsKey((1, 0)) ? b.Data[(1, 0)] : "<none>")}' width0={(b.ColumnWidths.TryGetValue(0, out double w4) ? w4 : -1)} -> {(deleted ? "PASS" : "FAIL")}");

                        Console.WriteLine($"[SheetApi] RESULT {(isolated && shown && inserted && deleted ? "PASS" : "FAIL")}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SheetApi] EXCEPTION {ex}");
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                };
            }

            // Automated test for the xlsx round trip: a workbook with three worksheets (the last one
            // hidden) is written, read back and written again; the worksheets must match the ones
            // that were exported, index by index.
            if (Environment.GetCommandLineArgs().Any(a => a == "--sheet-xlsx-test"))
            {
                Opened += async (_, _) =>
                {
                    string path = Path.Combine(Path.GetTempPath(), "spreadalonia_sheet_xlsx_test.xlsx");

                    try
                    {
                        await System.Threading.Tasks.Task.Delay(500);

                        SpreadsheetControl.MergedRanges = null;
                        SpreadsheetControl.CellBackgrounds = null;
                        SpreadsheetControl.CellBorders = null;
                        SpreadsheetControl.ClearContent();

                        SpreadsheetSheet alpha = SpreadsheetControl.CurrentSheet;
                        SpreadsheetControl.RenameSheet(alpha, "Alpha");
                        SpreadsheetControl.SetData(alpha, new Dictionary<(int, int), string>
                        {
                            [(0, 0)] = "Alpha-00",
                            [(1, 0)] = "=1+2",
                            [(0, 1)] = "Alpha-01"
                        });

                        SpreadsheetSheet beta = SpreadsheetControl.AddSheet("Beta");
                        SpreadsheetControl.SetData(beta, new Dictionary<(int, int), string>
                        {
                            [(0, 0)] = "Beta-00",
                            [(0, 1)] = "Beta-01"
                        });
                        SpreadsheetControl.SetWidth(beta, new Dictionary<int, double> { [0] = 120 });

                        SpreadsheetSheet gamma = SpreadsheetControl.AddSheet("Gamma");
                        gamma.IsHidden = true;
                        SpreadsheetControl.SetData(gamma, new Dictionary<(int, int), string> { [(0, 0)] = "Gamma-00" });

                        using (FileStream output = File.Create(path))
                        {
                            SpreadsheetControl.SaveXlsx(output, SpreadsheetControl.Sheets);
                        }

                        // 1) One worksheet per worksheet of the workbook, same order, name, visibility
                        //    and index (the hidden one included).
                        IReadOnlyList<SpreadsheetSheet> loaded = SpreadsheetControl.LoadXlsx(path);

                        bool namesOk = loaded.Count == 3
                            && loaded[0].Name == "Alpha" && loaded[1].Name == "Beta" && loaded[2].Name == "Gamma"
                            && loaded[0].Index == 0 && loaded[2].Index == 2
                            && !loaded[0].IsHidden && !loaded[1].IsHidden && loaded[2].IsHidden;
                        Console.WriteLine($"[SheetXlsx] names: [{string.Join(", ", loaded.Select(s => s.Name + (s.IsHidden ? "(hidden)" : "")))}] -> {(namesOk ? "PASS" : "FAIL")}");

                        // 2) The first worksheet that is not hidden is the one being displayed.
                        bool currentOk = SpreadsheetControl.CurrentSheet == loaded[0] && SpreadsheetControl.CurrentSheetIndex == 0;
                        Console.WriteLine($"[SheetXlsx] current: '{SpreadsheetControl.CurrentSheet?.Name}' index={SpreadsheetControl.CurrentSheetIndex} -> {(currentOk ? "PASS" : "FAIL")}");

                        // 3) The content of every worksheet, formulas included, is evaluated again.
                        double formulaValue = loaded[0].FormulaCells.TryGetValue((1, 0), out Spreadalonia.Formula.CellData formula) ? Convert.ToDouble(formula.CachedValue) : -1;
                        bool dataOk = loaded[0].Data[(0, 0)] == "Alpha-00" && loaded[0].Data[(0, 1)] == "Alpha-01"
                            && loaded[1].Data[(0, 0)] == "Beta-00" && loaded[1].Data[(0, 1)] == "Beta-01"
                            && loaded[2].Data[(0, 0)] == "Gamma-00"
                            && formulaValue == 3;
                        Console.WriteLine($"[SheetXlsx] data: A1='{loaded[0].Data[(0, 0)]}' formula={formulaValue} beta='{loaded[1].Data[(0, 1)]}' gamma='{loaded[2].Data[(0, 0)]}' -> {(dataOk ? "PASS" : "FAIL")}");

                        // 4) The geometry survives the round trip (DIP -> Excel characters -> DIP).
                        bool widthOk = loaded[1].ColumnWidths.TryGetValue(0, out double width) && Math.Abs(width - 120) <= 7.5;
                        Console.WriteLine($"[SheetXlsx] column width: {width} (was 120) -> {(widthOk ? "PASS" : "FAIL")}");

                        // 5) The imported workbook can be exported and read back again.
                        bool roundTripOk;
                        using (MemoryStream again = new MemoryStream())
                        {
                            SpreadsheetControl.SaveXlsx(again, SpreadsheetControl.Sheets);
                            again.Position = 0;

                            IReadOnlyList<SpreadsheetSheet> reloaded = SpreadsheetControl.LoadXlsx(again);
                            roundTripOk = reloaded.Count == 3 && reloaded[2].IsHidden
                                && reloaded[0].Name == "Alpha" && reloaded[2].Name == "Gamma"
                                && reloaded[1].Data.ContainsKey((0, 1)) && reloaded[1].Data[(0, 1)] == "Beta-01";
                            Console.WriteLine($"[SheetXlsx] re-export: [{string.Join(", ", reloaded.Select(s => s.Name))}] -> {(roundTripOk ? "PASS" : "FAIL")}");
                        }

                        Console.WriteLine($"[SheetXlsx] RESULT {(namesOk && currentOk && dataOk && widthOk && roundTripOk ? "PASS" : "FAIL")}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SheetXlsx] EXCEPTION {ex}");
                    }
                    finally
                    {
                        try { File.Delete(path); } catch { }

                        Environment.Exit(0);
                    }
                };
            }

            // Diagnostic for the grid lines of the first row and column: an empty sheet and a sheet
            // with content are rendered and the pixels along the expected separators are checked, so
            // that a missing line can be reported without looking at the screenshot by hand.
            if (Environment.GetCommandLineArgs().Any(a => a == "--grid-line-test"))
            {
                Opened += async (_, _) =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(500);

                        SpreadsheetControl.MergedRanges = null;
                        SpreadsheetControl.CellBackgrounds = null;
                        SpreadsheetControl.CellBorders = null;
                        SpreadsheetControl.UnfreezePanes();
                        SpreadsheetControl.ClearContent();
                        SpreadsheetControl.MaxTableWidth = 8;
                        SpreadsheetControl.MaxTableHeight = 14;

                        // 1) An empty sheet: every separator has to be drawn, the first ones included.
                        await System.Threading.Tasks.Task.Delay(600);
                        SaveScreenshot(@"C:\Users\89664\Desktop\grid_empty.png");
                        CheckGridLines("empty");

                        // 2) The same sheet with content in its first cells.
                        SpreadsheetControl.SetData(SpreadsheetControl.CurrentSheet, new Dictionary<(int, int), string>
                        {
                            [(0, 0)] = "A1", [(1, 0)] = "B1", [(2, 0)] = "C1",
                            [(0, 1)] = "A2", [(1, 1)] = "B2", [(2, 1)] = "C2"
                        });
                        await System.Threading.Tasks.Task.Delay(600);
                        SaveScreenshot(@"C:\Users\89664\Desktop\grid_content.png");
                        CheckGridLines("content");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[GridLine] EXCEPTION {ex}");
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                };
            }

            // Automated test for the per-worksheet display / interaction options: read-only, text
            // overflow, dragging cells and the style of the selection frame. Each option belongs to
            // one worksheet only.
            if (Environment.GetCommandLineArgs().Any(a => a == "--sheet-options-test"))
            {
                Opened += async (_, _) =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(500);

                        SpreadsheetControl.MergedRanges = null;
                        SpreadsheetControl.CellBackgrounds = null;
                        SpreadsheetControl.CellBorders = null;
                        SpreadsheetControl.ClearContent();
                        SpreadsheetControl.MaxTableWidth = 6;
                        SpreadsheetControl.MaxTableHeight = 10;

                        string longText = "A text far too long for the column it lives in";

                        SpreadsheetSheet locked = SpreadsheetControl.CurrentSheet;
                        SpreadsheetControl.RenameSheet(locked, "Locked");
                        SpreadsheetControl.SetData(locked, new Dictionary<(int, int), string>
                        {
                            [(0, 0)] = longText,
                            [(0, 1)] = "Second row"
                        });
                        locked.IsReadOnly = true;
                        locked.AllowDragMoveCells = false;
                        locked.AllowTextOverflow = true;
                        locked.SelectionStyle = SheetSelectionStyle.FocusRect;

                        SpreadsheetSheet free = SpreadsheetControl.AddSheet("Free");
                        SpreadsheetControl.SetData(free, new Dictionary<(int, int), string>
                        {
                            [(0, 0)] = longText,
                            [(0, 1)] = "Second row"
                        });
                        free.IsReadOnly = false;
                        free.AllowDragMoveCells = true;
                        free.AllowTextOverflow = false;
                        free.SelectionStyle = SheetSelectionStyle.Fill;

                        // 1) A read-only worksheet protects every one of its cells; the other one
                        //    stays editable. The option follows the worksheet that is displayed.
                        SpreadsheetControl.CurrentSheetIndex = 0;
                        await System.Threading.Tasks.Task.Delay(100);
                        bool lockedReadonly = SpreadsheetControl.IsCellReadonly(0, 0);

                        SpreadsheetControl.CurrentSheetIndex = 1;
                        await System.Threading.Tasks.Task.Delay(100);
                        bool freeReadonly = SpreadsheetControl.IsCellReadonly(0, 0);

                        bool readOnlyOk = lockedReadonly && !freeReadonly;
                        Console.WriteLine($"[SheetOptions] read-only: locked={lockedReadonly} free={freeReadonly} -> {(readOnlyOk ? "PASS" : "FAIL")}");

                        // 2) A read-only worksheet can still be written programmatically.
                        SpreadsheetControl.SetData(locked, new Dictionary<(int, int), string> { [(1, 3)] = "written by the host" });
                        bool programmaticOk = locked.Data[(1, 3)] == "written by the host" && locked.IsReadOnly;
                        Console.WriteLine($"[SheetOptions] programmatic write into a read-only sheet: '{locked.Data[(1, 3)]}' -> {(programmaticOk ? "PASS" : "FAIL")}");

                        // 3) The options of one worksheet never leak into another one.
                        bool isolated = locked.AllowDragMoveCells != free.AllowDragMoveCells
                            && locked.AllowTextOverflow != free.AllowTextOverflow
                            && locked.SelectionStyle != free.SelectionStyle
                            && free.AllowDragMoveCells && !free.AllowTextOverflow && free.SelectionStyle == SheetSelectionStyle.Fill;
                        Console.WriteLine($"[SheetOptions] isolation: locked[drag={locked.AllowDragMoveCells}, overflow={locked.AllowTextOverflow}, style={locked.SelectionStyle}] free[drag={free.AllowDragMoveCells}, overflow={free.AllowTextOverflow}, style={free.SelectionStyle}] -> {(isolated ? "PASS" : "FAIL")}");

                        // 4) Screenshots: the overflowing text and the focus rectangle on the first
                        //    worksheet, the clipped text and the filled selection on the second one.
                        SpreadsheetControl.CurrentSheetIndex = 0;
                        SpreadsheetControl.Selection = ImmutableList.Create(new SelectionRange(0, 0));
                        await System.Threading.Tasks.Task.Delay(600);
                        SaveScreenshot(@"C:\Users\89664\Desktop\sheet_options_locked.png");

                        SpreadsheetControl.CurrentSheetIndex = 1;
                        SpreadsheetControl.Selection = ImmutableList.Create(new SelectionRange(0, 0));
                        await System.Threading.Tasks.Task.Delay(600);
                        SaveScreenshot(@"C:\Users\89664\Desktop\sheet_options_free.png");
                        Console.WriteLine("[SheetOptions] screenshots saved.");

                        Console.WriteLine($"[SheetOptions] RESULT {(readOnlyOk && programmaticOk && isolated ? "PASS" : "FAIL")}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SheetOptions] EXCEPTION {ex}");
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                };
            }

            if (Environment.GetCommandLineArgs().Any(a => a == "--freeze-panes-test"))
            {
                Opened += async (_, _) =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(500);
                        FreezePanesButton_Click(null!, null!);
                        await System.Threading.Tasks.Task.Delay(800);
                        SaveScreenshot(@"C:\Users\89664\Desktop\freeze_panes_top_left.png");
                        Console.WriteLine("[Test] Frozen row + columns screenshot saved.");

                        // Selecting a cell far away scrolls the sheet: the frozen strips must not move.
                        SpreadsheetControl.Selection = ImmutableList.Create(new SelectionRange(13, 70, 13, 70));
                        await System.Threading.Tasks.Task.Delay(600);
                        SaveScreenshot(@"C:\Users\89664\Desktop\freeze_panes_scrolled.png");
                        Console.WriteLine("[Test] Scrolled screenshot saved.");

                        SpreadsheetControl.FreezePanes(0, 3);
                        await System.Threading.Tasks.Task.Delay(600);
                        SaveScreenshot(@"C:\Users\89664\Desktop\freeze_columns_only.png");
                        Console.WriteLine("[Test] Frozen columns only screenshot saved.");

                        SpreadsheetControl.FreezePanes(3, 0);
                        await System.Threading.Tasks.Task.Delay(600);
                        SaveScreenshot(@"C:\Users\89664\Desktop\freeze_rows_only.png");
                        Console.WriteLine("[Test] Frozen rows only screenshot saved.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Test] EXCEPTION: " + ex);
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                };
            }

            // Automated screenshot test for a merged cell that crosses the frozen column boundary:
            // the title (row 1 merged over columns A..D) must be drawn exactly once, anchored in the
            // frozen strip, and must not repeat or drift when a frozen column is resized.
            if (Environment.GetCommandLineArgs().Any(a => a == "--freeze-merge-test"))
            {
                Opened += async (_, _) =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(500);

                        LoadFreezeMergeDemo();
                        await System.Threading.Tasks.Task.Delay(600);
                        SaveScreenshot(@"C:\Users\89664\Desktop\freeze_merge_initial.png");
                        Console.WriteLine("[Test] Freeze + merged title screenshot saved.");

                        // Resize a frozen column: the merged title must stay a single copy.
                        SpreadsheetControl.SetWidth(new Dictionary<int, double> { [1] = 160 });
                        SpreadsheetControl.Refresh();
                        await System.Threading.Tasks.Task.Delay(600);
                        SaveScreenshot(@"C:\Users\89664\Desktop\freeze_merge_resized.png");
                        Console.WriteLine("[Test] Freeze + merged title after column resize screenshot saved.");

                        // And a scrollable column as well.
                        SpreadsheetControl.SetWidth(new Dictionary<int, double> { [2] = 150 });
                        SpreadsheetControl.Refresh();
                        await System.Threading.Tasks.Task.Delay(600);
                        SaveScreenshot(@"C:\Users\89664\Desktop\freeze_merge_resized_scrollable.png");
                        Console.WriteLine("[Test] Freeze + merged title after scrollable column resize screenshot saved.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Test] EXCEPTION: " + ex);
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                };
            }

            // Simulates column resizing in the header for the freeze + merged title scene when one of
            // the columns after the frozen strip is hidden: after a real render the header publishes
            // the right edge of every drawn column, and the pointer is placed on each of those edges
            // (plus on the right edge of the viewport) to see which column the header would resize.
            // The last visible column must be resizable from its right edge in every case.
            if (Environment.GetCommandLineArgs().Any(a => a == "--freeze-hidden-resize-test"))
            {
                Opened += async (_, _) =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(500);

                        foreach (int hiddenColumn in new int[] { -1, 2, 3, 4, 1 })
                        {
                            LoadFreezeMergeDemo();

                            if (hiddenColumn >= 0)
                            {
                                SpreadsheetControl.HideColumn(hiddenColumn);
                            }

                            SpreadsheetControl.Refresh();
                            await System.Threading.Tasks.Task.Delay(400);

                            // A real render publishes the drawn column edges of the header.
                            SaveScreenshot($@"C:\Users\89664\Desktop\freeze_hidden_resize_{hiddenColumn}.png");

                            ReportHeaderHitTest(hiddenColumn);
                            ReportLastVisibleEdge(hiddenColumn);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Test] EXCEPTION: " + ex);
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                };
            }

            // Simulates resizing the last column / row of a table whose size is fixed (MaxTableWidth
            // / MaxTableHeight set to the data extent, like the 辅助核算 pages do): the last drawn
            // edge is then the end of the table and there is no next column / row beyond it. The
            // pointer is placed around the last drawn edges to check that every grab position
            // (including slightly past the end of the table) resizes / selects the last column or
            // row.
            if (Environment.GetCommandLineArgs().Any(a => a == "--fixed-size-resize-test"))
            {
                Opened += async (_, _) =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(500);

                        LoadFreezeMergeDemo();
                        SpreadsheetControl.MaxTableWidth = 8;
                        SpreadsheetControl.MaxTableHeight = 13;
                        SpreadsheetControl.Refresh();
                        await System.Threading.Tasks.Task.Delay(400);

                        SaveScreenshot(@"C:\Users\89664\Desktop\fixed_size_resize.png");

                        ReportFixedSizeHitTest();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Test] EXCEPTION: " + ex);
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                };
            }

            // Automated screenshot test for the tree expand / collapse animation: shoots the sheet
            // mid-animation (rows are still growing / shrinking and fading) and after the animation
            // has finished (final state must match the non-animated result).
            if (Environment.GetCommandLineArgs().Any(a => a == "--tree-animation-test"))
            {
                Opened += async (_, _) =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(500);
                        TreeCellButton_Click(null!, null!);
                        await System.Threading.Tasks.Task.Delay(800);
                        SaveScreenshot(@"C:\Users\89664\Desktop\tree_animation_initial.png");

                        SpreadsheetControl.ToggleTreeCell(0, 1);
                        await System.Threading.Tasks.Task.Delay(80);
                        SaveScreenshot(@"C:\Users\89664\Desktop\tree_animation_collapse_mid.png");
                        await System.Threading.Tasks.Task.Delay(500);
                        SaveScreenshot(@"C:\Users\89664\Desktop\tree_animation_collapse_done.png");

                        SpreadsheetControl.ToggleTreeCell(0, 1);
                        await System.Threading.Tasks.Task.Delay(80);
                        SaveScreenshot(@"C:\Users\89664\Desktop\tree_animation_expand_mid.png");
                        await System.Threading.Tasks.Task.Delay(500);
                        SaveScreenshot(@"C:\Users\89664\Desktop\tree_animation_expand_done.png");

                        // Scrolled down: collapsing a node must not move the viewport. The animation
                        // only interpolates row heights, it never touches the scroll offset, so the
                        // rows above the toggled node have to stay exactly where they are.
                        int scrolledParent = Enumerable.Range(40, 80).First(r =>
                            SpreadsheetControl.TreeCells.TryGetValue((0, r), out TreeGradeCell parent) && parent != null && !parent.IsLeaf &&
                            SpreadsheetControl.TreeCells.TryGetValue((0, r + 1), out TreeGradeCell children) && children != null && children.Level > parent.Level);

                        SpreadsheetControl.Selection = ImmutableList.Create(new SelectionRange(0, scrolledParent + 4, 0, scrolledParent + 4));
                        await System.Threading.Tasks.Task.Delay(500);
                        SaveScreenshot(@"C:\Users\89664\Desktop\tree_animation_scroll_before.png");

                        SpreadsheetControl.ToggleTreeCell(0, scrolledParent);
                        await System.Threading.Tasks.Task.Delay(80);
                        SaveScreenshot(@"C:\Users\89664\Desktop\tree_animation_scroll_mid.png");
                        await System.Threading.Tasks.Task.Delay(500);
                        SaveScreenshot(@"C:\Users\89664\Desktop\tree_animation_scroll_after.png");
                        Console.WriteLine($"[Test] Scrolled animation test toggled row {scrolledParent}.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Test] EXCEPTION: " + ex);
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                };
            }

            // Automated repro for the "second LoadRgf keeps showing the first file" bug.
            if (Environment.GetCommandLineArgs().Any(a => a == "--load-two-rgf"))
            {
                Opened += async (_, _) =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(500);
                        LoadRgfFile(@"C:\Users\89664\Desktop\GL.rgf");
                        await System.Threading.Tasks.Task.Delay(500);
                        SaveScreenshot(@"C:\Users\89664\Desktop\rgf_test_1_GL.png");
                        LoadRgfFile(@"C:\Users\89664\Desktop\GLQ.rgf");
                        await System.Threading.Tasks.Task.Delay(500);
                        SaveScreenshot(@"C:\Users\89664\Desktop\rgf_test_2_GLQ.png");
                        Console.WriteLine("[Test] Screenshots saved.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Test] EXCEPTION: " + ex);
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                };
            }

            // Automated xlsx export smoke test.
            if (Environment.GetCommandLineArgs().Any(a => a == "--xlsx-export-test"))
            {
                Opened += async (_, _) =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(400);
                        string path = BuildAndExportXlsxDemo();
                        Console.WriteLine("[Test] Xlsx export saved: " + path);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Test] EXCEPTION: " + ex);
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                };
            }
        }


        /// <summary>
        /// Renders the window and checks that every separator of the table is painted as a solid one
        /// device pixel line. A line that straddles two device pixels (which happens on displays with a
        /// fractional render scaling, e.g. 150%) is painted as two faint grey lines: it is still
        /// "there", but it is far lighter than the grid colour, which is what made the first row and
        /// column look like they had no separator at all.
        /// </summary>
        private void CheckGridLines(string label)
        {
            const int solidLimit = 235;   // #DCDCDC is 220: anything lighter than this is not a solid line

            var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(
                new PixelSize((int)this.Bounds.Width, (int)this.Bounds.Height),
                new Vector(96, 96));
            bitmap.Render(this);

            using (MemoryStream pixelStream = new MemoryStream())
            {
                bitmap.Save(pixelStream);
                pixelStream.Position = 0;

                using (var decoded = new Avalonia.Media.Imaging.Bitmap(pixelStream))
                {
                    int width = decoded.PixelSize.Width;
                    int height = decoded.PixelSize.Height;
                    int stride = width * 4;

                    var buffer = new byte[stride * height];
                    var handle = System.Runtime.InteropServices.GCHandle.Alloc(buffer, System.Runtime.InteropServices.GCHandleType.Pinned);

                    try
                    {
                        decoded.CopyPixels(new PixelRect(decoded.PixelSize), handle.AddrOfPinnedObject(), buffer.Length, stride);
                    }
                    finally
                    {
                        handle.Free();
                    }

                    double scaling = this.RenderScaling;
                    double rowHeight = SpreadsheetControl.DefaultRowHeight * scaling;
                    double columnWidth = SpreadsheetControl.DefaultColumnWidth * scaling;

                    Point controlOrigin = SpreadsheetControl.TranslatePoint(new Point(0, 0), this) ?? new Point(0, 0);
                    int cellTop = (int)Math.Round((controlOrigin.Y + 27) * scaling);
                    int cellLeft = (int)Math.Round((controlOrigin.X + 41) * scaling);

                    // Locate the top edge of the table: the first solid line below the column header.
                    int tableTop = -1;

                    for (int y = cellTop; y < Math.Min(height, cellTop + 3 * rowHeight) && tableTop < 0; y++)
                    {
                        if (IsSolidGray(buffer, stride, (int)(cellLeft + columnWidth * 0.5), y))
                        {
                            tableTop = y;
                        }
                    }

                    if (tableTop < 0)
                    {
                        Console.WriteLine($"[GridLine] {label}: no horizontal grid line found below the column header");
                        Console.WriteLine($"[GridLine] {label}: RESULT FAIL");
                        return;
                    }

                    // Locate the left edge of the table the same way, on a row that is not a separator.
                    int sampleY = tableTop + (int)(rowHeight * 0.5);
                    int tableLeft = -1;

                    for (int x = cellLeft; x < Math.Min(width, cellLeft + 3 * columnWidth) && tableLeft < 0; x++)
                    {
                        if (IsSolidGray(buffer, stride, x, sampleY))
                        {
                            tableLeft = x;
                        }
                    }

                    if (tableLeft < 0)
                    {
                        Console.WriteLine($"[GridLine] {label}: no vertical grid line found right of the row header");
                        Console.WriteLine($"[GridLine] {label}: RESULT FAIL");
                        return;
                    }

                    // The first line found is the separator after the first column / row.
                    tableLeft -= (int)Math.Round(columnWidth);
                    tableTop -= (int)Math.Round(rowHeight);

                    int failures = 0;

                    // Every column separator is expected one column width apart from the first one, and
                    // it has to be a fully opaque grid pixel somewhere in its two device pixel band.
                    int firstSeparatorX = tableLeft + (int)Math.Round(columnWidth);
                    int sampleX = tableLeft + (int)(columnWidth * 0.5);

                    for (int offset = 0; offset < 5; offset++)
                    {
                        int x = firstSeparatorX + (int)Math.Round(columnWidth * offset);

                        if (!HasSolidLineNear(buffer, stride, width, height, x, sampleY, 2, (int)(rowHeight / 2)))
                        {
                            Console.WriteLine($"[GridLine] {label}: separator after column {offset} is missing or too faint (x≈{x}, sample y={sampleY})");
                            failures++;
                        }
                    }

                    int firstSeparatorY = tableTop + (int)Math.Round(rowHeight);

                    for (int offset = 0; offset < 6; offset++)
                    {
                        int y = firstSeparatorY + (int)Math.Round(rowHeight * offset);

                        if (!HasSolidLineNear(buffer, stride, width, height, sampleX, y, (int)(columnWidth / 2), 2))
                        {
                            Console.WriteLine($"[GridLine] {label}: separator after row {offset} is missing or too faint (y≈{y}, sample x={sampleX})");
                            failures++;
                        }
                    }

                    Console.WriteLine($"[GridLine] {label}: RESULT {(failures == 0 ? "PASS" : "FAIL")} ({failures} missing)");
                }
            }
        }

        /// <summary>
        /// Checks whether a solid grid line pixel is found within the specified band around a point.
        /// </summary>
        private static bool HasSolidLineNear(byte[] buffer, int stride, int width, int height, int x, int y, int spanX, int spanY)
        {
            for (int dx = -spanX; dx <= spanX; dx++)
            {
                for (int dy = -spanY; dy <= spanY; dy++)
                {
                    if (IsSolidGray(buffer, stride, x + dx, y + dy, width, height))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the pixel is a grid line pixel that is solid enough to be seen (the grid
        /// colour is #DCDCDC; a line split over two device pixels is much lighter than that).
        /// </summary>
        private static bool IsSolidGray(byte[] buffer, int stride, int x, int y)
        {
            return IsSolidGray(buffer, stride, x, y, stride / 4, buffer.Length / stride);
        }

        private static bool IsSolidGray(byte[] buffer, int stride, int x, int y, int width, int height)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return false;
            }

            int i = y * stride + x * 4;

            if (i + 3 >= buffer.Length)
            {
                return false;
            }

            // Grey, and not lighter than the solid grid colour: the grid is #DCDCDC (220), a line that
            // straddles two device pixels comes out at about 240.
            return Math.Abs(buffer[i + 2] - buffer[i + 1]) <= 3
                && Math.Abs(buffer[i + 1] - buffer[i]) <= 3
                && buffer[i] <= 235;
        }

        /// <summary>
        /// Simulates a left button press and release on a worksheet tab, which is a plain
        /// <see cref="Border"/> with pointer handlers rather than a <see cref="Button"/>.
        /// </summary>
        private static void SimulateTabClick(Border tab)
        {
            Point position = new Point(tab.Bounds.Width / 2, tab.Bounds.Height / 2);

            tab.RaiseEvent(new PointerPressedEventArgs(tab, null, tab, position, 0, new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
            tab.RaiseEvent(new PointerReleasedEventArgs(tab, null, tab, position, 0, new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
        }

        private void SaveScreenshot(string path)
        {
            var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(
                new PixelSize((int)this.Bounds.Width, (int)this.Bounds.Height),
                new Vector(96, 96));
            bitmap.Render(this);
            bitmap.Save(path);
            Console.WriteLine("[Test] Saved screenshot: " + path);
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

        // Test hook: load two different RGF files in succession and log diagnostics.
        // Reproduces the reported bug where the second LoadRgf call keeps showing the first file.
        private void LoadTwoRgfButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                LoadRgfFile(@"C:\Users\89664\Desktop\GL.rgf");
                LoadRgfFile(@"C:\Users\89664\Desktop\GLQ.rgf");
                Console.WriteLine("[Test] Done. The spreadsheet should now show GLQ.rgf (10 columns).");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Test] EXCEPTION: " + ex);
            }
        }

        private void LoadRgfFile(string path)
        {
            Console.WriteLine("[Test] Loading " + path + " ...");
            using (FileStream stream = File.OpenRead(path))
            {
                SpreadsheetControl.LoadRgf(stream);
            }
            var data = SpreadsheetControl.Data;
            Console.WriteLine("[Test] Loaded. Cells=" + (data?.Count ?? -1)
                + ", MaxTableWidth=" + SpreadsheetControl.MaxTableWidth
                + ", MaxTableHeight=" + SpreadsheetControl.MaxTableHeight);
            int i = 0;
            foreach (var kv in data ?? new Dictionary<(int, int), string>())
            {
                if (i++ >= 6) break;
                Console.WriteLine("[Test]   (" + kv.Key.Item1 + "," + kv.Key.Item2 + ") = " + kv.Value);
            }
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

        private void TitleCellButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Clear formatting features and reset the table bounds
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
            SpreadsheetControl.TitleCells?.Clear();

            // Replace existing data completely so previous demos do not leave rows behind.
            SpreadsheetControl.Data?.Clear();

            var data = new Dictionary<(int, int), string>
            {
                [(0, 0)] = "", // title cell body is rendered by TitleCell

                [(0, 1)] = "期间",
                [(1, 1)] = "凭证字号",
                [(2, 1)] = "摘要",
                [(3, 1)] = "借方",
                [(4, 1)] = "贷方",
                [(5, 1)] = "方向",
                [(6, 1)] = "余额",

                [(0, 2)] = "2024.01",
                [(1, 2)] = "",
                [(2, 2)] = "本月合计",
                [(3, 2)] = "8,412.00",
                [(4, 2)] = "15,392.00",
                [(5, 2)] = "贷",
                [(6, 2)] = "6,980.00",

                [(0, 3)] = "2024.01",
                [(1, 3)] = "",
                [(2, 3)] = "当前累计",
                [(3, 3)] = "8,412.00",
                [(4, 3)] = "15,392.00",
                [(5, 3)] = "贷",
                [(6, 3)] = "6,980.00",
            };

            SpreadsheetControl.SetData(data);

            // Row heights: title row is taller to hold two lines of text
            SpreadsheetControl.SetHeight(new Dictionary<int, double>
            {
                [0] = 70,
                [1] = 28,
                [2] = 26,
                [3] = 26,
            });

            // Column widths
            var columnWidths = new Dictionary<int, double>();
            for (int c = 0; c <= 6; c++)
            {
                columnWidths[c] = c == 2 ? 150 : 90;
            }
            SpreadsheetControl.SetWidth(columnWidths);

            // Merge the title row across all 7 columns
            SpreadsheetControl.MergedRanges = new List<SelectionRange>
            {
                new SelectionRange(0, 0, 6, 0)
            };

            // Title cell with two chunks: main title + subtitle
            double titleOrgHeight = 70;
            var titleCell = new TitleCell(new List<TitleCell.Chunk>
            {
                new TitleCell.Chunk
                {
                    Text = "总分类账",
                    FontName = "微软雅黑",
                    FontSize = 22,
                    Color = Brushes.Black,
                    HorAlign = TextAlignment.Center,
                    VerAlign = VerticalAlignment.Bottom,
                    Top = 0,
                    Height = titleOrgHeight * 0.62
                },
                new TitleCell.Chunk
                {
                    Text = "2024.1-2024.1",
                    FontName = "微软雅黑",
                    FontSize = 11,
                    Color = Brushes.Black,
                    HorAlign = TextAlignment.Center,
                    VerAlign = VerticalAlignment.Top,
                    Top = titleOrgHeight * 0.62,
                    Height = titleOrgHeight * 0.38
                }
            })
            {
                OrgHeight = titleOrgHeight
            };

            SpreadsheetControl.TitleCells[(0, 0)] = titleCell;

            // White title background
            SpreadsheetControl.CellBackgrounds = new List<CellBackground>
            {
                new CellBackground(new SelectionRange(0, 0, 6, 0), Brushes.White),
                new CellBackground(new SelectionRange(0, 1, 6, 1), new SolidColorBrush(Color.Parse("#E7E6E6"))),
                new CellBackground(new SelectionRange(0, 3, 6, 3), new SolidColorBrush(Color.Parse("#CCFFCC")))
            };

            // Outer frame + header separator + row separators
            var black = new SolidColorBrush(Color.Parse("#000000"));
            SpreadsheetControl.CellBorders = new List<CellBorder>
            {
                new CellBorder(true, 0, 0, 6, black, 1),
                new CellBorder(true, 1, 0, 6, black, 1),
                new CellBorder(true, 2, 0, 6, black, 1),
                new CellBorder(true, 3, 0, 6, black, 1),
                new CellBorder(true, 4, 0, 6, black, 1),

                new CellBorder(false, 0, 1, 4, black, 1),
                new CellBorder(false, 7, 1, 4, black, 1),
                new CellBorder(false, 1, 1, 4, black, 1),
                new CellBorder(false, 2, 1, 4, black, 1),
                new CellBorder(false, 3, 1, 4, black, 1),
                new CellBorder(false, 4, 1, 4, black, 1),
                new CellBorder(false, 5, 1, 4, black, 1),
                new CellBorder(false, 6, 1, 4, black, 1),
            };

            // Header style
            for (int c = 0; c <= 6; c++)
            {
                SpreadsheetControl.CellTypefaces[(c, 1)] = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
                SpreadsheetControl.CellTextAlignment[(c, 1)] = TextAlignment.Center;
                SpreadsheetControl.CellVerticalAlignment[(c, 1)] = VerticalAlignment.Center;
            }

            // Right-align amounts and centre direction
            for (int r = 2; r <= 3; r++)
            {
                SpreadsheetControl.CellTextAlignment[(3, r)] = TextAlignment.Right;
                SpreadsheetControl.CellTextAlignment[(4, r)] = TextAlignment.Right;
                SpreadsheetControl.CellTextAlignment[(6, r)] = TextAlignment.Right;
                SpreadsheetControl.CellTextAlignment[(5, r)] = TextAlignment.Center;
                SpreadsheetControl.CellVerticalAlignment[(5, r)] = VerticalAlignment.Center;
            }

            SpreadsheetControl.Refresh();
        }

        private void LinkCellButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Clear formatting features and reset the table bounds
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
            SpreadsheetControl.TitleCells?.Clear();
            SpreadsheetControl.LinkCells?.Clear();

            // Replace existing data completely so previous demos do not leave rows behind.
            SpreadsheetControl.Data?.Clear();

            var data = new Dictionary<(int, int), string>
            {
                [(0, 0)] = "日期",
                [(1, 0)] = "摘要",
                [(2, 0)] = "借方",
                [(3, 0)] = "贷方",

                [(0, 1)] = "2024.01.05",
                [(1, 1)] = "采购办公用品",
                [(2, 1)] = "8412",
                [(3, 1)] = "0",

                [(0, 2)] = "2024.01.12",
                [(1, 2)] = "销售货款",
                [(2, 2)] = "0",
                [(3, 2)] = "15392",

                [(0, 3)] = "2024.01.31",
                [(1, 3)] = "结转损益",
                [(2, 3)] = "6980",
                [(3, 3)] = "6980",

                [(0, 4)] = "",
                [(1, 4)] = "打开财务报表",
            };

            SpreadsheetControl.SetData(data);

            // Column widths
            var columnWidths = new Dictionary<int, double>
            {
                [0] = 90,
                [1] = 160,
                [2] = 90,
                [3] = 90,
            };
            SpreadsheetControl.SetWidth(columnWidths);

            // Row heights
            SpreadsheetControl.SetHeight(new Dictionary<int, double>
            {
                [0] = 28,
                [1] = 26,
                [2] = 26,
                [3] = 26,
                [4] = 26,
            });

            // Header style
            for (int c = 0; c <= 3; c++)
            {
                SpreadsheetControl.CellTypefaces[(c, 0)] = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
                SpreadsheetControl.CellTextAlignment[(c, 0)] = TextAlignment.Center;
                SpreadsheetControl.CellVerticalAlignment[(c, 0)] = VerticalAlignment.Center;
            }

            // Right-align amounts
            for (int r = 1; r <= 3; r++)
            {
                SpreadsheetControl.CellTextAlignment[(2, r)] = TextAlignment.Right;
                SpreadsheetControl.CellTextAlignment[(3, r)] = TextAlignment.Right;
                SpreadsheetControl.CellVerticalAlignment[(2, r)] = VerticalAlignment.Center;
                SpreadsheetControl.CellVerticalAlignment[(3, r)] = VerticalAlignment.Center;
            }

            // Numeric link cells: formatted, clickable amounts.
            // Values of zero render as empty (or "…" when ResponseEmpty is set) but stay clickable.
            SpreadsheetControl.LinkCells[(2, 1)] = new NumericLinkCell("https://example.com/debit/8412", false, false, 2);
            SpreadsheetControl.LinkCells[(3, 1)] = new NumericLinkCell("https://example.com/credit/0", true, false, 2);
            SpreadsheetControl.LinkCells[(2, 2)] = new NumericLinkCell("https://example.com/debit/0", true, false, 2);
            SpreadsheetControl.LinkCells[(3, 2)] = new NumericLinkCell("https://example.com/credit/15392", false, false, 2);
            SpreadsheetControl.LinkCells[(2, 3)] = new NumericLinkCell("https://example.com/debit/6980", false, false, 2);
            SpreadsheetControl.LinkCells[(3, 3)] = new NumericLinkCell("https://example.com/credit/6980", false, false, 2);

            // Plain link cell with strikethrough in the summary row.
            SpreadsheetControl.LinkCells[(1, 4)] = new LinkCell("打开财务报表", "https://example.com/report/2024", true);
            SpreadsheetControl.CellTextAlignment[(1, 4)] = TextAlignment.Left;
            SpreadsheetControl.CellVerticalAlignment[(1, 4)] = VerticalAlignment.Center;

            SpreadsheetControl.Refresh();
        }

        // Parent row of every tree node, used by the demo to roll leaf amounts up.
        private readonly Dictionary<int, int> treeParentRow = new Dictionary<int, int>();

        // The leaf rows that belong to each level-1 group.
        private readonly Dictionary<int, List<int>> groupLeaves = new Dictionary<int, List<int>>();

        /// <summary>
        /// Builds an "opening balances" sheet: a three level account tree in column 0, numeric
        /// amount columns, read-only derived columns and live recalculation through
        /// <see cref="Spreadsheet.CellEditEnded"/>.
        /// </summary>
        private void TreeCellButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Clear formatting features and reset the table bounds
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
            SpreadsheetControl.TitleCells?.Clear();
            SpreadsheetControl.LinkCells?.Clear();
            SpreadsheetControl.TreeCells?.Clear();
            SpreadsheetControl.NumericCells?.Clear();
            SpreadsheetControl.ClearContent();

            // A previous demo may have frozen rows or columns: start from a clean slate.
            SpreadsheetControl.UnfreezePanes();

            treeParentRow.Clear();
            groupLeaves.Clear();

            IBrush parentBackground = new SolidColorBrush(Color.Parse("#D9FFF5"));
            IBrush leafBackground = new SolidColorBrush(Color.Parse("#B9F5D8"));

            string[] headers = { "科目编码", "科目名称", "金额", "期初借方", "期初贷方", "数量", "单价" };

            var data = new Dictionary<(int, int), string>();

            for (int c = 0; c < headers.Length; c++)
            {
                data[(c, 0)] = headers[c];
                SpreadsheetControl.CellTypefaces[(c, 0)] = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
                SpreadsheetControl.CellTextAlignment[(c, 0)] = TextAlignment.Center;
            }

            var heights = new Dictionary<int, double> { [0] = 28 };
            var widths = new Dictionary<int, double>
            {
                [0] = 220,
                [1] = 180,
                [2] = 110,
                [3] = 110,
                [4] = 110,
                [5] = 90,
                [6] = 90,
            };

            int row = 1;
            var leafRowsByGroup = new Dictionary<int, List<int>>();
            int groupCount = 12;

            // Even groups are three levels deep (L1 -> L2 -> leaves), odd groups four levels
            // (L1 -> L2 -> L3 -> leaves), so both tree depths can be compared side by side.
            for (int g = 0; g < groupCount; g++)
            {
                string groupCode = (1001 + g).ToString();
                int groupRow = row;
                bool deep = g % 2 == 1;

                // Level 1
                data[(0, groupRow)] = groupCode;
                data[(1, groupRow)] = "一级科目 " + groupCode;
                SpreadsheetControl.TreeCells[(0, groupRow)] = new TreeGradeCell(groupCode, 1, false)
                {
                    BackColor = parentBackground,
                    IsExpanded = true
                };
                heights[groupRow] = 26;
                row++;

                leafRowsByGroup[groupRow] = new List<int>();

                int subCount = deep ? 2 : 3;

                for (int s = 0; s < subCount; s++)
                {
                    string subCode = groupCode + (s + 1).ToString("00");
                    int subRow = row;

                    data[(0, subRow)] = subCode;
                    data[(1, subRow)] = "二级科目 " + subCode;
                    SpreadsheetControl.TreeCells[(0, subRow)] = new TreeGradeCell(subCode, 2, false)
                    {
                        BackColor = parentBackground,
                        IsExpanded = true
                    };
                    treeParentRow[subRow] = groupRow;
                    heights[subRow] = 26;
                    row++;

                    if (deep)
                    {
                        // Four-level group: L2 -> L3 -> leaves.
                        for (int t = 0; t < 2; t++)
                        {
                            string thirdCode = subCode + (t + 1).ToString("00");
                            int thirdRow = row;

                            data[(0, thirdRow)] = thirdCode;
                            data[(1, thirdRow)] = "三级科目 " + thirdCode;
                            SpreadsheetControl.TreeCells[(0, thirdRow)] = new TreeGradeCell(thirdCode, 3, false)
                            {
                                BackColor = parentBackground,
                                IsExpanded = true
                            };
                            treeParentRow[thirdRow] = subRow;
                            heights[thirdRow] = 26;
                            row++;

                            for (int l = 0; l < 3; l++)
                            {
                                string leafCode = thirdCode + (l + 1).ToString("000");
                                int leafRow = row;

                                data[(0, leafRow)] = leafCode;
                                data[(1, leafRow)] = "末级科目 " + leafCode;
                                SpreadsheetControl.TreeCells[(0, leafRow)] = new TreeGradeCell(leafCode, 4, true)
                                {
                                    BackColor = leafBackground
                                };
                                treeParentRow[leafRow] = thirdRow;
                                leafRowsByGroup[groupRow].Add(leafRow);
                                heights[leafRow] = 26;
                                row++;
                            }
                        }
                    }
                    else
                    {
                        // Three-level group: L2 -> leaves.
                        for (int l = 0; l < 4; l++)
                        {
                            string leafCode = subCode + (l + 1).ToString("000");
                            int leafRow = row;

                            data[(0, leafRow)] = leafCode;
                            data[(1, leafRow)] = "末级科目 " + leafCode;
                            SpreadsheetControl.TreeCells[(0, leafRow)] = new TreeGradeCell(leafCode, 3, true)
                            {
                                BackColor = leafBackground
                            };
                            treeParentRow[leafRow] = subRow;
                            leafRowsByGroup[groupRow].Add(leafRow);
                            heights[leafRow] = 26;
                            row++;
                        }
                    }
                }
            }

            foreach (KeyValuePair<int, List<int>> kvp in leafRowsByGroup)
            {
                groupLeaves[kvp.Key] = kvp.Value;
            }

            int maxRow = row - 1;

            // Amount columns: 2 decimals for money, 4 for quantities.
            for (int r = 1; r <= maxRow; r++)
            {
                for (int c = 2; c <= 6; c++)
                {
                    int digits = c == 5 ? 4 : 2;
                    SpreadsheetControl.NumericCells[(c, r)] = new NumericCell(digits) { NegativeRed = true };
                }

                SpreadsheetControl.CellTextAlignment[(0, r)] = TextAlignment.Left;
                SpreadsheetControl.CellVerticalAlignment[(0, r)] = VerticalAlignment.Center;
            }

            // Derived (read-only) columns: amounts of non-leaf rows, plus the whole 金额 column.
            SpreadsheetControl.SetRangeReadonly(new SelectionRange(2, 0, 2, maxRow), true);

            for (int r = 1; r <= maxRow; r++)
            {
                bool isLeaf = SpreadsheetControl.TreeCells[(0, r)].IsLeaf;

                if (!isLeaf)
                {
                    SpreadsheetControl.SetRangeReadonly(new SelectionRange(3, r, 6, r), true);
                }
            }

            SpreadsheetControl.SetData(data);
            SpreadsheetControl.SetHeight(heights);
            SpreadsheetControl.SetWidth(widths);
            SpreadsheetControl.Refresh();

            Console.WriteLine($"[TreeCell] {maxRow} rows built; click a +/- box to collapse, edit a leaf amount to see CellEditEnded.");
        }

        /// <summary>
        /// Turns the tree expand / collapse animation on and off, so that the animated and the
        /// instant behaviour of the TreeGrade / NumericCell demo can be compared side by side.
        /// </summary>
        private void TreeAnimationCheckBox_IsCheckedChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            SpreadsheetControl.IsTreeAnimationEnabled = TreeAnimationCheckBox.IsChecked == true;

            Console.WriteLine($"[TreeAnimation] {(SpreadsheetControl.IsTreeAnimationEnabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Wires the tree / numeric events used by the demo sheet.
        /// </summary>
        private void WireTreeCellEvents()
        {
            SpreadsheetControl.TreeExpand += (s, e) =>
            {
                Console.WriteLine($"[TreeExpand] ({e.Column}, {e.Row}) level {e.Cell.Level} -> {(e.IsExpanded ? "expanded" : "collapsed")}");
            };

            SpreadsheetControl.InvalidInput += (s, e) =>
            {
                Console.WriteLine($"[InvalidInput] ({e.Column}, {e.Row}) '{e.InputText}' rejected: {e.Reason}");
            };

            SpreadsheetControl.CellEditEnded += (s, e) =>
            {
                Console.WriteLine($"[CellEditEnded] ({e.Column}, {e.Row}) '{e.OldText}' -> '{e.NewText}' cancelled={e.Cancelled}");

                if (e.Cancelled || e.Row <= 0)
                {
                    return;
                }

                // Debit and credit are mutually exclusive.
                if (e.Column == 3 && !string.IsNullOrEmpty(e.NewText))
                {
                    SpreadsheetControl.SetData(new Dictionary<(int, int), string> { [(4, e.Row)] = "" });
                }
                else if (e.Column == 4 && !string.IsNullOrEmpty(e.NewText))
                {
                    SpreadsheetControl.SetData(new Dictionary<(int, int), string> { [(3, e.Row)] = "" });
                }

                // quantity x unit price -> amount
                if (e.Column == 5 || e.Column == 6)
                {
                    decimal quantity = ReadNumber(5, e.Row);
                    decimal price = ReadNumber(6, e.Row);

                    SpreadsheetControl.SetData(new Dictionary<(int, int), string>
                    {
                        [(2, e.Row)] = (quantity * price).ToString(System.Globalization.CultureInfo.InvariantCulture)
                    });
                }

                RollUpGroup(e.Row);
                SpreadsheetControl.Refresh();
            };
        }

        private decimal ReadNumber(int column, int row)
        {
            if (SpreadsheetControl.Data != null &&
                SpreadsheetControl.Data.TryGetValue((column, row), out string text) &&
                decimal.TryParse(text, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out decimal value))
            {
                return value;
            }

            return 0m;
        }

        private void RollUpGroup(int row)
        {
            if (!treeParentRow.TryGetValue(row, out int parent))
            {
                return;
            }

            while (treeParentRow.TryGetValue(parent, out int grandParent))
            {
                parent = grandParent;
            }

            if (!groupLeaves.TryGetValue(parent, out List<int> leaves))
            {
                return;
            }

            decimal total = 0m;

            foreach (int leaf in leaves)
            {
                total += ReadNumber(2, leaf);
            }

            SpreadsheetControl.SetData(new Dictionary<(int, int), string>
            {
                [(2, parent)] = total.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
        }

        // The auxiliary items offered by the simulated picker of the hidden-column demo.
        private static readonly string[] AuxiliaryItems =
        {
            "销售部", "财务部", "采购部", "生产车间", "研发中心"
        };

        // Populated by HiddenColumnButton_Click: the column that holds the ButtonCells.
        private int auxColumn = 8;

        /// <summary>
        /// Builds a trial-balance style sheet: the "年初余额" and "累计发生" columns are hidden
        /// the way SSFinance hides them when the account book starts in the current period, and
        /// the last column holds <see cref="ButtonCell"/>s that open an auxiliary-item picker
        /// (department / employee / customer / supplier / project) through
        /// <see cref="Spreadsheet.CellTapped"/>.
        /// </summary>
        private void HiddenColumnButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            SpreadsheetControl.MergedRanges = null;
            SpreadsheetControl.CellBackgrounds = null;
            SpreadsheetControl.CellBorders = null;
            SpreadsheetControl.MaxTableWidth = 9;
            SpreadsheetControl.MaxTableHeight = int.MaxValue - 2;
            SpreadsheetControl.CellFontSize?.Clear();
            SpreadsheetControl.CellTypefaces?.Clear();
            SpreadsheetControl.CellForeground?.Clear();
            SpreadsheetControl.CellTextAlignment?.Clear();
            SpreadsheetControl.CellVerticalAlignment?.Clear();
            SpreadsheetControl.CellMargin?.Clear();
            SpreadsheetControl.TitleCells?.Clear();
            SpreadsheetControl.LinkCells?.Clear();
            SpreadsheetControl.TreeCells?.Clear();
            SpreadsheetControl.NumericCells?.Clear();
            SpreadsheetControl.ButtonCells?.Clear();
            SpreadsheetControl.ClearContent();

            // A previous demo may have frozen rows or columns: start from a clean slate.
            SpreadsheetControl.UnfreezePanes();

            // A previous demo may have hidden columns: start from a clean slate.
            SpreadsheetControl.ShowAllRows();
            SpreadsheetControl.ShowAllColumns();

            string[] headers =
            {
                "科目编码", "科目名称",
                "年初借方", "年初贷方",     // 年初余额 – hidden when the book starts this period
                "累计借方", "累计贷方",     // 累计发生 – hidden when the book starts this period
                "期初借方", "期初贷方",
                "辅助项"
            };

            var data = new Dictionary<(int, int), string>();

            for (int c = 0; c < headers.Length; c++)
            {
                data[(c, 0)] = headers[c];
                SpreadsheetControl.CellTypefaces[(c, 0)] = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
                SpreadsheetControl.CellTextAlignment[(c, 0)] = TextAlignment.Center;
                SpreadsheetControl.CellVerticalAlignment[(c, 0)] = VerticalAlignment.Center;
            }

            string[,] rows =
            {
                { "1001", "库存现金",     "12,000.00", "",           "84,120.00", "51,400.00", "44,720.00", "",           "" },
                { "1002", "银行存款",     "860,000.00", "",          "1,530,000.00", "980,000.00", "1,410,000.00", "",    "" },
                { "1122", "应收账款",     "320,000.00", "",          "640,000.00", "410,000.00", "550,000.00", "",       "" },
                { "2202", "应付账款",     "",           "210,000.00", "390,000.00", "520,000.00", "",           "340,000.00", "" },
                { "5001", "主营业务收入", "",           "0.00",       "2,180,000.00", "2,180,000.00", "",       "0.00",  "" },
            };

            for (int r = 0; r < rows.GetLength(0); r++)
            {
                for (int c = 0; c < headers.Length; c++)
                {
                    data[(c, r + 1)] = rows[r, c];
                }

                SpreadsheetControl.CellTextAlignment[(0, r + 1)] = TextAlignment.Left;
                SpreadsheetControl.CellTextAlignment[(1, r + 1)] = TextAlignment.Left;

                for (int c = 2; c <= 7; c++)
                {
                    SpreadsheetControl.CellTextAlignment[(c, r + 1)] = TextAlignment.Right;
                    SpreadsheetControl.NumericCells[(c, r + 1)] = new NumericCell(2) { NegativeRed = true };
                }
            }

            SpreadsheetControl.SetData(data);

            SpreadsheetControl.SetHeight(new Dictionary<int, double>
            {
                [0] = 30, [1] = 26, [2] = 26, [3] = 26, [4] = 26, [5] = 26,
            });

            SpreadsheetControl.SetWidth(new Dictionary<int, double>
            {
                [0] = 110, [1] = 160, [2] = 110, [3] = 110,
                [4] = 110, [5] = 110, [6] = 110, [7] = 110, [8] = 140,
            });

            // Header background + grid borders.
            SpreadsheetControl.CellBackgrounds = new List<CellBackground>
            {
                new CellBackground(new SelectionRange(0, 0, 8, 0), new SolidColorBrush(Color.Parse("#D9E2F3"))),
            };

            // Auxiliary item picker: a RefCell style icon button per data row in the last column.
            // The cell keeps its normal text (editable, like the WPF RefCell) and a magnifier
            // icon is drawn at the right edge; clicking the icon raises CellTapped.
            auxColumn = 8;

            for (int r = 1; r <= rows.GetLength(0); r++)
            {
                SpreadsheetControl.ButtonCells[(auxColumn, r)] = new ButtonCell(null, "AuxItem", "row" + r)
                {
                    Mode = ButtonCellMode.Icon,
                    SuppressEdit = false,
                };

                SpreadsheetControl.CellTextAlignment[(auxColumn, r)] = TextAlignment.Left;
                SpreadsheetControl.CellVerticalAlignment[(auxColumn, r)] = VerticalAlignment.Center;
            }

            SpreadsheetControl.Refresh();

            Console.WriteLine("[HiddenColumn] Sheet built. Call HideColumn(2..5) to drop 年初余额 / 累计发生.");
        }

        /// <summary>
        /// Simulates the auxiliary-item dialog that a real application would open from the
        /// <see cref="Spreadsheet.CellTapped"/> handler: it picks the next item of
        /// <see cref="AuxiliaryItems"/> and writes it back into the cell, which repaints it.
        /// </summary>
        private void PickAuxiliaryItem(int column, int row)
        {
            string current = SpreadsheetControl.Data != null && SpreadsheetControl.Data.TryGetValue((column, row), out string text)
                ? text
                : "";

            int index = Array.IndexOf(AuxiliaryItems, current);
            string next = AuxiliaryItems[(index + 1) % AuxiliaryItems.Length];

            SpreadsheetControl.SetData(new Dictionary<(int, int), string> { [(column, row)] = next });

            if (SpreadsheetControl.ButtonCells != null &&
                SpreadsheetControl.ButtonCells.TryGetValue((column, row), out ButtonCell cell) &&
                cell != null &&
                cell.Mode == ButtonCellMode.Button)
            {
                // Icon mode cells display the raw cell data, so no Text update is needed.
                cell.Text = next;
            }

            SpreadsheetControl.Refresh();
            Console.WriteLine($"[CellTapped] ({column}, {row}) -> '{next}' written back.");
        }

        private void FreezeMergeButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            LoadFreezeMergeDemo();
        }

        /// <summary>
        /// Exercises the header hit testing for the scene that is currently loaded: once the header
        /// has been really rendered (so it knows the drawn edges of every visible column), the
        /// pointer is placed on each of those edges and on the right edge of the viewport, and the
        /// column the header would resize from there is reported. The last visible column must be
        /// resizable from its own right edge in every case.
        /// </summary>
        private void ReportHeaderHitTest(int hiddenColumn)
        {
            HorizontalHeader header = SpreadsheetControl.HorizontalHeaderControlInternal;

            int lastColumn = header.LastDrawnLeft + header.LastDrawnWidth;

            Console.WriteLine("[Test] ===== hidden column: " + (hiddenColumn < 0 ? "none" : hiddenColumn.ToString())
                + ", header width=" + header.Bounds.Width.ToString("0.#")
                + ", frozen columns=" + SpreadsheetControl.FrozenColumnCount
                + ", drawn columns " + header.LastDrawnLeft + ".." + lastColumn + " =====");

            double[] xs = header.LastDrawnXs;

            if (xs == null)
            {
                Console.WriteLine("[Test] the header has not been rendered yet");
                return;
            }

            bool lastColumnResizable = false;

            for (int i = 0; i < xs.Length; i++)
            {
                double screenX = xs[i] + header.LastDrawnDelta;
                (int column, bool isResize) = header.ResolveColumnPress(screenX);

                Console.WriteLine("[Test]   right edge of column " + (header.LastDrawnLeft + i)
                    + " at x=" + screenX.ToString("0.#")
                    + " -> resize column " + column + " (isResize=" + isResize + ")");

                if (isResize && column == lastColumn)
                {
                    lastColumnResizable = true;
                }
            }

            double viewportRight = header.Bounds.Width - 0.5;
            (int edgeColumn, bool edgeResize) = header.ResolveColumnPress(viewportRight);

            Console.WriteLine("[Test]   right edge of the viewport at x=" + viewportRight.ToString("0.#")
                + " -> resize column " + edgeColumn + " (isResize=" + edgeResize + ")");

            if (edgeResize && edgeColumn == lastColumn)
            {
                lastColumnResizable = true;
            }

            Console.WriteLine("[Test] last visible column = " + lastColumn
                + ", resizable from one of the drawn edges: " + lastColumnResizable);
        }

        /// <summary>
        /// Reports, for the scene that is currently loaded, which column would be resized by a drag
        /// that starts on the last column edge that is visible inside the viewport, and how much of
        /// that column is actually visible (a column whose right edge is outside the viewport grows
        /// only outside the viewport, so dragging its left border produces no visible feedback).
        /// </summary>
        private void ReportLastVisibleEdge(int hiddenColumn)
        {
            HorizontalHeader header = SpreadsheetControl.HorizontalHeaderControlInternal;
            double[] xs = header.LastDrawnXs;

            if (xs == null)
            {
                return;
            }

            double delta = header.LastDrawnDelta;
            double viewportRight = header.Bounds.Width;
            double viewportLeft = header.ClampedFrozenWidth;

            int edge = -1;

            for (int i = 0; i < xs.Length; i++)
            {
                if (xs[i] + delta < viewportRight)
                {
                    edge = i;
                }
            }

            if (edge < 0)
            {
                return;
            }

            double edgeX = xs[edge] + delta;
            (int target, bool isResize) = header.ResolveColumnPress(edgeX);

            Console.WriteLine("[Test]   hidden=" + (hiddenColumn < 0 ? "none" : hiddenColumn.ToString())
                + ": last edge inside the viewport is x=" + edgeX.ToString("0.#")
                + " (right edge of column " + (header.LastDrawnLeft + edge) + ")"
                + " -> drag resizes column " + target + " (isResize=" + isResize + ")");

            if (target >= header.LastDrawnLeft && target <= header.LastDrawnLeft + header.LastDrawnWidth)
            {
                int j = target - header.LastDrawnLeft;
                double left = (j == 0 ? 0 : xs[j - 1]) + delta;
                double right = xs[j] + delta;
                double visibleLeft = Math.Max(left, viewportLeft);
                double visibleRight = Math.Min(right, viewportRight);

                Console.WriteLine("[Test]     column " + target + " spans x=" + left.ToString("0.#") + ".." + right.ToString("0.#")
                    + ", visible part x=" + visibleLeft.ToString("0.#") + ".." + visibleRight.ToString("0.#")
                    + " (visible width " + Math.Max(0, visibleRight - visibleLeft).ToString("0.#")
                    + "), right edge inside the viewport: " + (right < viewportRight));
            }
        }

        /// <summary>
        /// Exercises the header hit testing around the last drawn column / row edges of a
        /// fixed-size table: every grab position around an edge (on it, on either side within the
        /// 5px tolerance, and just outside it) must resolve to the last column / row, both for the
        /// resize action and (further away) for the selection.
        /// </summary>
        private void ReportFixedSizeHitTest()
        {
            HorizontalHeader columnHeader = SpreadsheetControl.HorizontalHeaderControlInternal;
            VerticalHeader rowHeader = SpreadsheetControl.VerticalHeaderControlInternal;

            Console.WriteLine("[Test] ===== fixed-size table: MaxTableWidth=" + SpreadsheetControl.MaxTableWidth
                + ", MaxTableHeight=" + SpreadsheetControl.MaxTableHeight
                + ", header " + columnHeader.Bounds.Width.ToString("0.#") + "x" + rowHeader.Bounds.Height.ToString("0.#")
                + ", frozen columns=" + SpreadsheetControl.FrozenColumnCount + " =====");

            double[] xs = columnHeader.LastDrawnXs;
            double[] ys = rowHeader.LastDrawnYs;

            if (xs == null || ys == null)
            {
                Console.WriteLine("[Test] the headers have not been rendered yet");
                return;
            }

            int lastColumn = columnHeader.LastDrawnLeft + columnHeader.LastDrawnWidth;
            int lastRow = rowHeader.LastDrawnTop + rowHeader.LastDrawnHeight;

            double lastColumnEdge = xs[xs.Length - 1] + columnHeader.LastDrawnDelta;
            double lastRowEdge = ys[ys.Length - 1] + rowHeader.LastDrawnDelta;

            Console.WriteLine("[Test] drawn columns " + columnHeader.LastDrawnLeft + ".." + lastColumn
                + ", last right edge at x=" + lastColumnEdge.ToString("0.#")
                + "; drawn rows " + rowHeader.LastDrawnTop + ".." + lastRow
                + ", last bottom edge at y=" + lastRowEdge.ToString("0.#"));

            bool allColumnsOk = true;

            foreach (double dx in new double[] { -8, -3, 0, 3, 8, 30 })
            {
                (int column, bool isResize) = columnHeader.ResolveColumnPress(lastColumnEdge + dx);
                (int oldColumn, bool oldResize) = ResolveColumnPressLegacy(columnHeader, lastColumnEdge + dx);

                // Every position hits the last column; grabbing within 5px of the edge resizes it.
                allColumnsOk &= column == lastColumn && isResize == (Math.Abs(dx) <= 5);

                Console.WriteLine("[Test]   column edge x=" + (lastColumnEdge + dx).ToString("0.#") + " (dx=" + dx + ")"
                    + " -> column " + column + " (isResize=" + isResize + ")"
                    + " | v1.7.4: column " + oldColumn + " (isResize=" + oldResize + ")");
            }

            Console.WriteLine("[Test] last column " + lastColumn + " hit from every grab position and resizable within 5px: " + allColumnsOk);

            bool allRowsOk = true;

            foreach (double dy in new double[] { -8, -3, 0, 3, 8, 30 })
            {
                (int row, bool isResize) = rowHeader.ResolveRowPress(lastRowEdge + dy);
                (int oldRow, bool oldResize) = ResolveRowPressLegacy(rowHeader, lastRowEdge + dy);

                allRowsOk &= row == lastRow && isResize == (Math.Abs(dy) <= 5);

                Console.WriteLine("[Test]   row edge y=" + (lastRowEdge + dy).ToString("0.#") + " (dy=" + dy + ")"
                    + " -> row " + row + " (isResize=" + isResize + ")"
                    + " | v1.7.4: row " + oldRow + " (isResize=" + oldResize + ")");
            }

            Console.WriteLine("[Test] last row " + lastRow + " hit from every grab position and resizable within 5px: " + allRowsOk);
        }

        /// <summary>
        /// Reports the placeholder text and the hit rectangle of every NumericLinkCell of the
        /// empty-value demo: an empty value has to show the placeholder and react over the whole
        /// cell, while the exported value has to stay empty.
        /// </summary>
        private void ReportNumericLinkEmpty()
        {
            HorizontalHeader columnHeader = SpreadsheetControl.HorizontalHeaderControlInternal;
            VerticalHeader rowHeader = SpreadsheetControl.VerticalHeaderControlInternal;

            double[] xs = columnHeader.LastDrawnXs;
            double[] ys = rowHeader.LastDrawnYs;

            Console.WriteLine("[Test] ===== NumericLinkCell empty placeholder =====");
            Console.WriteLine("[Test] EmptyNumericLinkText='" + SpreadsheetControl.EmptyNumericLinkText + "'");

            if (xs == null || ys == null)
            {
                Console.WriteLine("[Test] the headers have not been rendered yet");
                return;
            }

            bool allPlaceholdersClickable = true;

            for (int r = 1; r <= 5; r++)
            {
                for (int c = 1; c <= 2; c++)
                {
                    if (!SpreadsheetControl.LinkCells.TryGetValue((c, r), out LinkCell link) || link == null)
                    {
                        continue;
                    }

                    SpreadsheetControl.Data.TryGetValue((c, r), out string raw);

                    int ci = c - columnHeader.LastDrawnLeft;
                    int ri = r - rowHeader.LastDrawnTop;

                    double cellLeft = ci == 0 ? 0 : xs[ci - 1];
                    double cellTop = ri == 0 ? 0 : ys[ri - 1];
                    double cellRight = xs[ci];
                    double cellBottom = ys[ri];

                    // Probe the left edge, the middle and the wide empty space on the right.
                    double middleY = (cellTop + cellBottom) * 0.5;
                    double[] probeX = new double[] { cellLeft + 2, (cellLeft + cellRight) * 0.5, cellRight - 8 };

                    bool allHit = true;
                    foreach (double px in probeX)
                    {
                        allHit &= link.HitRect.Contains(new Point(px, middleY));
                    }

                    string display = link.GetDisplayText(raw);
                    string exported = link.GetExportText(raw);

                    if (display == "…")
                    {
                        allPlaceholdersClickable &= allHit;
                    }

                    Console.WriteLine("[Test] cell (" + c + ", " + r + ") raw='" + (raw ?? "<none>") + "'"
                        + " display='" + display + "' export='" + exported + "'"
                        + " cell " + (cellRight - cellLeft).ToString("0.#") + "x" + (cellBottom - cellTop).ToString("0.#")
                        + " hit " + link.HitRect.Width.ToString("0.#") + "x" + link.HitRect.Height.ToString("0.#")
                        + " allProbesHit=" + allHit);
                }
            }

            Console.WriteLine("[Test] every placeholder cell is clickable over its whole width: " + allPlaceholdersClickable);
        }

        /// <summary>
        /// Replays the hit testing of v1.7.4 (no clamping to the last drawn edge, and a resize drag
        /// that is ignored as soon as the resolved column / row is -1), so that the fixed-size test
        /// can report the behaviour before and after the fix side by side.
        /// </summary>
        private (int column, bool isResize) ResolveColumnPressLegacy(HorizontalHeader header, double screenX)
        {
            double[] xs = header.LastDrawnXs;
            double xPos = screenX - header.LastDrawnDelta;

            double closest = double.MaxValue;
            int clickedColumn = -1;

            for (int i = 0; i < xs.Length; i++)
            {
                if (xs[i] > xPos && xPos >= (i == 0 ? 0 : xs[i - 1]))
                {
                    clickedColumn = i + header.LastDrawnLeft;
                }

                closest = Math.Min(closest, Math.Abs(xs[i] - xPos));

                if (closest <= 5 && clickedColumn >= 0)
                {
                    break;
                }
            }

            if (closest > 5)
            {
                return (clickedColumn, false);
            }

            double columnLeftEdge = clickedColumn > header.LastDrawnLeft ? xs[clickedColumn - header.LastDrawnLeft - 1] : 0;

            if (xPos - columnLeftEdge <= 5)
            {
                clickedColumn--;
            }

            return (clickedColumn, true);
        }

        private (int row, bool isResize) ResolveRowPressLegacy(VerticalHeader header, double screenY)
        {
            double[] ys = header.LastDrawnYs;
            double yPos = screenY - header.LastDrawnDelta;

            double closest = double.MaxValue;
            int clickedRow = -1;

            for (int i = 0; i < ys.Length; i++)
            {
                if (ys[i] > yPos && yPos >= (i == 0 ? 0 : ys[i - 1]))
                {
                    clickedRow = i + header.LastDrawnTop;
                }

                closest = Math.Min(closest, Math.Abs(ys[i] - yPos));

                if (closest <= 5 && clickedRow >= 0)
                {
                    break;
                }
            }

            if (closest > 5)
            {
                return (clickedRow, false);
            }

            double rowTopEdge = clickedRow > header.LastDrawnTop ? ys[clickedRow - header.LastDrawnTop - 1] : 0;

            if (yPos - rowTopEdge <= 5)
            {
                clickedRow--;
            }

            return (clickedRow, true);
        }






        /// <summary>
        /// Builds the small 试算平衡表 sheet used by the "--freeze-merge-test" screenshot test: the
        /// row-1 title is merged over columns A..D while only A:B are frozen, so the merged cell
        /// crosses the frozen column boundary. Used by the demo button above and by the automated
        /// screenshot test, so both always show exactly the same scene.
        /// </summary>
        private void LoadFreezeMergeDemo()
        {
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
            SpreadsheetControl.TitleCells?.Clear();
            SpreadsheetControl.LinkCells?.Clear();
            SpreadsheetControl.TreeCells?.Clear();
            SpreadsheetControl.NumericCells?.Clear();
            SpreadsheetControl.ButtonCells?.Clear();
            SpreadsheetControl.ClearContent();

            SpreadsheetControl.ShowAllRows();
            SpreadsheetControl.ShowAllColumns();
            SpreadsheetControl.UnfreezePanes();

            var data = new Dictionary<(int, int), string>
            {
                [(0, 0)] = "试算平衡表",
                [(0, 1)] = "科目编码",
                [(1, 1)] = "科目名称",
                [(2, 1)] = "年初余额",
                [(2, 2)] = "借方",
                [(3, 2)] = "贷方",
                [(0, 3)] = "101",
                [(1, 3)] = "现金",
                [(0, 4)] = "102",
                [(1, 4)] = "银行存款",
                [(0, 5)] = "113",
                [(1, 5)] = "应收账款",
                [(0, 6)] = "116",
                [(1, 6)] = "固定资产",
                [(0, 7)] = "118",
                [(1, 7)] = "存货",
            };

            SpreadsheetControl.SetData(data);
            SpreadsheetControl.MergedRanges = new List<SelectionRange>
            {
                new SelectionRange(0, 0, 3, 0), // title across the frozen boundary (A:B frozen)
                new SelectionRange(2, 1, 3, 1), // 年初余额 header inside the scrollable columns
            };
            SpreadsheetControl.FreezePanes(0, 2);
            SpreadsheetControl.Refresh();
        }

        /// <summary>
        /// Builds a wide 明细账 (sub-ledger) sheet and freezes its header row together with the
        /// 凭证 date columns, which is what SSFinance needs for the voucher / ledger pages: the
        /// caption row and the 月 / 日 columns stay visible while the rest of the sheet scrolls.
        /// </summary>
        private void FreezePanesButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            SpreadsheetControl.MergedRanges = null;
            SpreadsheetControl.CellBackgrounds = null;
            SpreadsheetControl.CellBorders = null;
            SpreadsheetControl.MaxTableWidth = 14;
            SpreadsheetControl.MaxTableHeight = 200;
            SpreadsheetControl.CellFontSize?.Clear();
            SpreadsheetControl.CellTypefaces?.Clear();
            SpreadsheetControl.CellForeground?.Clear();
            SpreadsheetControl.CellTextAlignment?.Clear();
            SpreadsheetControl.CellVerticalAlignment?.Clear();
            SpreadsheetControl.CellMargin?.Clear();
            SpreadsheetControl.TitleCells?.Clear();
            SpreadsheetControl.LinkCells?.Clear();
            SpreadsheetControl.TreeCells?.Clear();
            SpreadsheetControl.NumericCells?.Clear();
            SpreadsheetControl.ButtonCells?.Clear();
            SpreadsheetControl.ClearContent();

            SpreadsheetControl.ShowAllRows();
            SpreadsheetControl.ShowAllColumns();
            SpreadsheetControl.UnfreezePanes();

            string[] headers =
            {
                "月", "日", "凭证号", "摘要", "科目编码", "科目名称",
                "借方金额", "贷方金额", "方向", "余额", "数量", "单价", "部门", "备注"
            };

            string[] summaries =
            {
                "收到货款", "采购原材料", "支付运费", "计提折旧", "发放工资",
                "报销差旅费", "缴纳增值税", "结转成本", "收到投资款", "支付租金"
            };

            (string Code, string Name)[] subjects =
            {
                ("1122", "应收账款"), ("2202", "应付账款"), ("1001", "库存现金"),
                ("1002", "银行存款"), ("5001", "主营业务收入"), ("6601", "销售费用")
            };

            string[] departments = { "销售部", "采购部", "财务部", "生产部", "行政部" };

            var data = new Dictionary<(int, int), string>();

            for (int c = 0; c < headers.Length; c++)
            {
                data[(c, 0)] = headers[c];
                SpreadsheetControl.CellTypefaces[(c, 0)] = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
                SpreadsheetControl.CellTextAlignment[(c, 0)] = TextAlignment.Center;
                SpreadsheetControl.CellVerticalAlignment[(c, 0)] = VerticalAlignment.Center;
            }

            Random rnd = new Random(20261013);
            decimal balance = 0m;

            for (int r = 1; r <= 80; r++)
            {
                (string code, string name) = subjects[r % subjects.Length];

                decimal debit = rnd.Next(0, 2) == 0 ? rnd.Next(1, 90) * 100m : 0m;
                decimal credit = debit == 0 ? rnd.Next(1, 60) * 100m : 0m;

                balance += debit - credit;

                data[(0, r)] = ((r % 12) + 1).ToString();
                data[(1, r)] = ((r % 28) + 1).ToString();
                data[(2, r)] = "记-" + (1000 + r).ToString();
                data[(3, r)] = summaries[r % summaries.Length];
                data[(4, r)] = code;
                data[(5, r)] = name;
                data[(6, r)] = debit > 0 ? debit.ToString("N2") : "";
                data[(7, r)] = credit > 0 ? credit.ToString("N2") : "";
                data[(8, r)] = balance >= 0 ? "借" : "贷";
                data[(9, r)] = Math.Abs(balance).ToString("N2");
                data[(10, r)] = rnd.Next(1, 500).ToString();
                data[(11, r)] = (rnd.Next(10, 300) / 10m).ToString("N2");
                data[(12, r)] = departments[r % departments.Length];
                data[(13, r)] = "";

                SpreadsheetControl.CellTextAlignment[(0, r)] = TextAlignment.Center;
                SpreadsheetControl.CellTextAlignment[(1, r)] = TextAlignment.Center;
                SpreadsheetControl.CellTextAlignment[(3, r)] = TextAlignment.Left;
                SpreadsheetControl.CellTextAlignment[(4, r)] = TextAlignment.Left;
                SpreadsheetControl.CellTextAlignment[(5, r)] = TextAlignment.Left;
                SpreadsheetControl.CellTextAlignment[(8, r)] = TextAlignment.Center;
                SpreadsheetControl.CellTextAlignment[(12, r)] = TextAlignment.Left;

                for (int c = 6; c <= 11; c++)
                {
                    SpreadsheetControl.CellTextAlignment[(c, r)] = TextAlignment.Right;
                    SpreadsheetControl.NumericCells[(c, r)] = new NumericCell(2) { NegativeRed = true };
                }
            }

            SpreadsheetControl.SetData(data);

            SpreadsheetControl.SetWidth(new Dictionary<int, double>
            {
                [0] = 45, [1] = 45, [2] = 90, [3] = 150, [4] = 100, [5] = 160,
                [6] = 110, [7] = 110, [8] = 55, [9] = 120,
                [10] = 80, [11] = 90, [12] = 110, [13] = 140,
            });

            SpreadsheetControl.CellBackgrounds = new List<CellBackground>
            {
                new CellBackground(new SelectionRange(0, 0, 13, 0), new SolidColorBrush(Color.Parse("#D9E2F3"))),
            };

            // Freeze the header row and the 月 / 日 columns: they stay put while the ledger scrolls.
            SpreadsheetControl.FreezePanes(1, 2);

            SpreadsheetControl.Refresh();

            Console.WriteLine("[FreezePanes] Sheet built: FrozenRowCount = 1, FrozenColumnCount = 2.");
        }

        private void MultiSheetButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                if (!this.multiSheetDemoWired)
                {
                    // Whenever the user picks another sheet tab, the title bar shows the worksheet
                    // that is being displayed now (switching sheets) together with the content of its
                    // first cell (reading the current sheet).
                    SpreadsheetControl.CurrentSheetChanged += (_, args) =>
                    {
                        string firstCell = args.NewSheet != null && args.NewSheet.Data.TryGetValue((0, 0), out string value) ? value : "(empty)";
                        this.Title = $"Multi-Sheet Demo — current sheet: '{args.NewSheet?.Name}' (index {args.NewIndex}), A1='{firstCell}'";
                        Console.WriteLine($"[MultiSheet] switched: '{args.OldSheet?.Name}' -> '{args.NewSheet?.Name}'");
                    };

                    this.multiSheetDemoWired = true;
                }

                // 1) Build a small "external" workbook: three worksheets, the last one hidden, so the
                //    import can be exercised the way an arbitrary .xlsx file would be.
                string path = Path.Combine(Path.GetTempPath(), "spreadalonia_multi_sheet_demo.xlsx");

                using (FileStream output = File.Create(path))
                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    ClosedXML.Excel.IXLWorksheet summary = workbook.Worksheets.Add("Summary");
                    summary.Cell(1, 1).Value = "Quarterly report";
                    summary.Cell(2, 1).Value = "Region";
                    summary.Cell(2, 2).Value = "Amount";
                    summary.Cell(3, 1).Value = "North";
                    summary.Cell(3, 2).Value = 1234.5;
                    summary.Cell(4, 1).Value = "South";
                    summary.Cell(4, 2).Value = 2345.6;
                    summary.Column(1).Width = 20;
                    summary.Column(2).Width = 15;

                    ClosedXML.Excel.IXLWorksheet detail = workbook.Worksheets.Add("Detail");
                    detail.Cell(1, 1).Value = "Date";
                    detail.Cell(1, 2).Value = "Item";
                    detail.Cell(2, 1).Value = "2026-10-01";
                    detail.Cell(2, 2).Value = "Widget";

                    ClosedXML.Excel.IXLWorksheet raw = workbook.Worksheets.Add("Raw data");
                    raw.Cell(1, 1).Value = "internal only";
                    raw.Hide();

                    workbook.SaveAs(output);
                }

                // 2) Import: one SpreadsheetSheet per worksheet, same order, name, visibility and index.
                IReadOnlyList<SpreadsheetSheet> sheets = SpreadsheetControl.LoadXlsx(path);

                Console.WriteLine($"[MultiSheet] imported {sheets.Count} sheets: {string.Join(", ", sheets.Select(s => s.Name + (s.IsHidden ? " (hidden)" : "")))}");

                // 3) Read the current sheet (the first one that is not hidden).
                SpreadsheetSheet current = SpreadsheetControl.CurrentSheet;
                Console.WriteLine($"[MultiSheet] current sheet: '{current?.Name}' (index {SpreadsheetControl.CurrentSheetIndex}), A1='{(current != null && current.Data.TryGetValue((0, 0), out string a1) ? a1 : null)}'");

                this.Title = $"Multi-Sheet Demo — current sheet: '{SpreadsheetControl.CurrentSheet?.Name}' (index {SpreadsheetControl.CurrentSheetIndex}) — switch tabs to read the other sheets";
            }
            catch (Exception ex)
            {
                Console.WriteLine("[MultiSheet] EXCEPTION: " + ex);
            }
        }

        private bool multiSheetDemoWired;

        private void XlsxExportButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                string path = BuildAndExportXlsxDemo();
                Console.WriteLine("[Xlsx] Saved: " + path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Xlsx] EXCEPTION: " + ex);
            }
        }

        /// <summary>
        /// Builds a small report that exercises every cell type supported by the xlsx exporter
        /// (TitleCell, LinkCell, formulas, merged ranges, backgrounds, borders and cell styles),
        /// then saves it as an Excel .xlsx file on the Desktop.
        /// </summary>
        private string BuildAndExportXlsxDemo()
        {
            // Reset formatting and table bounds.
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
            SpreadsheetControl.TitleCells?.Clear();
            SpreadsheetControl.LinkCells?.Clear();
            SpreadsheetControl.ClearContent();

            // A previous demo may have frozen rows or columns: start from a clean slate.
            SpreadsheetControl.UnfreezePanes();

            var data = new Dictionary<(int, int), string>
            {
                // Row 0: TitleCell body lives in (0,0), merged over columns 0..4.
                [(0, 0)] = "",

                // Row 1: headers
                [(0, 1)] = "产品",
                [(1, 1)] = "数量",
                [(2, 1)] = "单价",
                [(3, 1)] = "金额",
                [(4, 1)] = "详情",

                // Row 2: item A (Excel row 3)
                [(0, 2)] = "产品A",
                [(1, 2)] = "12",
                [(2, 2)] = "25.5",
                [(3, 2)] = "=C3*B3",

                // Row 3: item B (Excel row 4)
                [(0, 3)] = "产品B",
                [(1, 3)] = "8",
                [(2, 3)] = "42.0",
                [(3, 3)] = "=C4*B4",

                // Row 4: summary (Excel row 5)
                [(0, 4)] = "合计",
                [(1, 4)] = "=SUM(B3:B4)",
                [(2, 4)] = "=SUM(C3:C4)",
                [(3, 4)] = "=SUM(D3:D4)",
            };

            SpreadsheetControl.SetData(data);

            // Row heights and column widths.
            SpreadsheetControl.SetHeight(new Dictionary<int, double>
            {
                [0] = 60,
                [1] = 26,
                [2] = 26,
                [3] = 26,
                [4] = 26,
            });
            SpreadsheetControl.SetWidth(new Dictionary<int, double>
            {
                [0] = 110,
                [1] = 90,
                [2] = 90,
                [3] = 120,
                [4] = 100,
            });

            // Merge the title row across all five columns.
            SpreadsheetControl.MergedRanges = new List<SelectionRange>
            {
                new SelectionRange(0, 0, 4, 0)
            };

            // TitleCell with a main line and a subtitle line.
            double titleOrgHeight = 60;
            var titleCell = new TitleCell(new List<TitleCell.Chunk>
            {
                new TitleCell.Chunk
                {
                    Text = "2026年销售汇总报表",
                    FontName = "微软雅黑",
                    FontSize = 20,
                    Color = Brushes.White,
                    HorAlign = TextAlignment.Center,
                    VerAlign = VerticalAlignment.Bottom,
                    Top = 0,
                    Height = titleOrgHeight * 0.62
                },
                new TitleCell.Chunk
                {
                    Text = "2026.1 - 2026.9",
                    FontName = "微软雅黑",
                    FontSize = 11,
                    Color = new SolidColorBrush(Color.Parse("#DCE6F1")),
                    HorAlign = TextAlignment.Center,
                    VerAlign = VerticalAlignment.Top,
                    Top = titleOrgHeight * 0.62,
                    Height = titleOrgHeight * 0.38
                }
            })
            {
                OrgHeight = titleOrgHeight
            };
            SpreadsheetControl.TitleCells[(0, 0)] = titleCell;

            // Link cells in the "详情" column.
            SpreadsheetControl.LinkCells[(4, 2)] = new LinkCell("查看产品A", "https://example.com/products/A");
            SpreadsheetControl.LinkCells[(4, 3)] = new LinkCell("查看产品B", "https://example.com/products/B", true);

            // Colours.
            var titleBlue = new SolidColorBrush(Color.Parse("#4472C4"));
            var headerLightBlue = new SolidColorBrush(Color.Parse("#D9E2F3"));
            var summaryGreen = new SolidColorBrush(Color.Parse("#E2EFDA"));
            var black = new SolidColorBrush(Color.Parse("#000000"));

            SpreadsheetControl.CellBackgrounds = new List<CellBackground>
            {
                new CellBackground(new SelectionRange(0, 0, 4, 0), titleBlue),
                new CellBackground(new SelectionRange(0, 1, 4, 1), headerLightBlue),
                new CellBackground(new SelectionRange(0, 4, 3, 4), summaryGreen),
            };

            SpreadsheetControl.CellBorders = new List<CellBorder>
            {
                // Outer frame (rows 0..4, columns 0..4).
                new CellBorder(true, 0, 0, 4, black, 1),
                new CellBorder(true, 5, 0, 4, black, 1),
                new CellBorder(false, 0, 0, 4, black, 1),
                new CellBorder(false, 5, 0, 4, black, 1),

                // Title bottom and header bottom.
                new CellBorder(true, 1, 0, 4, black, 1),
                new CellBorder(true, 2, 0, 4, black, 1),

                // Vertical separators in the data area.
                new CellBorder(false, 1, 1, 4, black, 1),
                new CellBorder(false, 2, 1, 4, black, 1),
                new CellBorder(false, 3, 1, 4, black, 1),
                new CellBorder(false, 4, 1, 4, black, 1),
            };

            // Header row: bold white-ish text on the light blue background, centred.
            for (int c = 0; c <= 4; c++)
            {
                SpreadsheetControl.CellTypefaces[(c, 1)] = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
                SpreadsheetControl.CellTextAlignment[(c, 1)] = TextAlignment.Center;
                SpreadsheetControl.CellVerticalAlignment[(c, 1)] = VerticalAlignment.Center;
            }

            // Right-align numbers, centre the details links.
            for (int r = 2; r <= 4; r++)
            {
                SpreadsheetControl.CellTextAlignment[(1, r)] = TextAlignment.Right;
                SpreadsheetControl.CellTextAlignment[(2, r)] = TextAlignment.Right;
                SpreadsheetControl.CellTextAlignment[(3, r)] = TextAlignment.Right;
                SpreadsheetControl.CellTextAlignment[(4, r)] = TextAlignment.Center;
                SpreadsheetControl.CellVerticalAlignment[(4, r)] = VerticalAlignment.Center;
            }

            // Emphasise the grand-total amount.
            SpreadsheetControl.CellTypefaces[(3, 4)] = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
            SpreadsheetControl.CellFontSize[(3, 4)] = 12;

            SpreadsheetControl.Refresh();

            string folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (string.IsNullOrEmpty(folder))
            {
                folder = AppContext.BaseDirectory;
            }
            string path = Path.Combine(folder, "spreadalonia_export_demo.xlsx");

            SpreadsheetControl.SaveXlsx(path, new XlsxExportOptions
            {
                ExportLinkAsHyperlink = true,
                WarningHandler = msg => Console.WriteLine("[Xlsx] warning: " + msg),
            });

            return path;
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
