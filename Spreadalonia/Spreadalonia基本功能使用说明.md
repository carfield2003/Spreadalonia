# Spreadalonia 基本功能使用说明

> **版本**: 1.9.1 | **目标框架**: .NET Standard 2.0 | **UI 框架**: Avalonia 11  
> **许可证**: LGPL v3 | **作者**: Giorgio Bianchini, University of Bristol; carfield2003  
> 本文档涵盖公式引擎以外的所有基本功能，公式引擎的使用请参考 `公式与自定义函数使用说明.md`。
> v1.9.0 新增：多工作表（第 11 章）与 Excel `.xlsx` 导入/导出（第 12 章）。

---

## 目录

1. [快速入门](#快速入门)
2. [控件属性总览](#控件属性总览)
3. [数据操作](#数据操作)
4. [选择与导航](#选择与导航)
5. [行与列操作](#行与列操作)
6. [外观与格式](#外观与格式)
7. [合并单元格与表格线](#合并单元格与表格线)
8. [剪贴板操作](#剪贴板操作)
9. [撤销与重做](#撤销与重做)
10. [序列化与加载](#序列化与加载)
11. [多工作表（Sheet）](#多工作表sheet)
12. [Excel xlsx 导入与导出](#excel-xlsx-导入与导出)
13. [事件](#事件)
14. [键盘快捷键](#键盘快捷键)
15. [右键菜单](#右键菜单)
16. [其他交互功能](#其他交互功能)
17. [SelectionRange 说明](#selectionrange-结构)
18. [完整 API 速查表](#完整-api-速查表)

---

## 快速入门

### 安装

通过 NuGet 安装：

```
Install-Package Spreadalonia.Carfield2003
```

### 在 XAML 中使用

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:spreadalonia="clr-namespace:Spreadalonia;assembly=Spreadalonia"
        x:Class="Demo.MainWindow">
    <spreadalonia:Spreadsheet x:Name="SpreadsheetControl" />
</Window>
```

### 在 C# 代码中加载数据

```csharp
// 数据以 Dictionary<(int column, int row), string> 格式存储
// column 和 row 都从 0 开始计数
var data = new Dictionary<(int, int), string>
{
    [(0, 0)] = "姓名",
    [(1, 0)] = "年龄",
    [(0, 1)] = "张三",
    [(1, 1)] = "28",
    [(0, 2)] = "李四",
    [(1, 2)] = "35",
};

SpreadsheetControl.SetData(data);
```

> **重要**: 请始终使用 `SetData()` 方法设置数据，**不要**直接修改 `Data` 属性返回的字典，否则会破坏撤销/重做栈。

---

## 控件属性总览

### 数据相关

| 属性               | 类型                              | 默认值                | 说明                   |
| ---------------- | ------------------------------- | ------------------ | -------------------- |
| `Data`           | `Dictionary<(int,int), string>` | -                  | 只读，获取表格数据，**请勿直接修改** |
| `MaxTableWidth`  | `int`                           | `int.MaxValue - 2` | 表格最大列数               |
| `MaxTableHeight` | `int`                           | `int.MaxValue - 2` | 表格最大行数               |

### 行列默认值

| 属性                   | 类型          | 默认值  | 说明       |
| -------------------- | ----------- | ---- | -------- |
| `DefaultRowHeight`   | `double`    | `23` | 默认行高（像素） |
| `DefaultColumnWidth` | `double`    | `65` | 默认列宽（像素） |
| `DefaultCellMargin`  | `Thickness` | `3`  | 单元格默认内边距 |

### 文本对齐

| 属性                         | 类型                  | 默认值      | 说明     |
| -------------------------- | ------------------- | -------- | ------ |
| `DefaultTextAlignment`     | `TextAlignment`     | `Left`   | 默认水平对齐 |
| `DefaultVerticalAlignment` | `VerticalAlignment` | `Center` | 默认垂直对齐 |

### 表头外观

| 属性                 | 类型           | 默认值                  | 说明     |
| ------------------ | ------------ | -------------------- | ------ |
| `HeaderFontFamily` | `FontFamily` | `FontFamily.Default` | 表头字体   |
| `HeaderFontSize`   | `double`     | `14`                 | 表头字号   |
| `HeaderForeground` | `IBrush`     | `Black`              | 表头文字颜色 |
| `HeaderBackground` | `Color`      | `#F0F0F0`            | 表头背景色  |

### 表格外观

| 属性                      | 类型                | 默认值       | 说明         |
| ----------------------- | ----------------- | --------- | ---------- |
| `GridColor`             | `Color`           | `#DCDCDC` | 网格线颜色      |
| `SpreadsheetBackground` | `SolidColorBrush` | `White`   | 表格背景色      |
| `SelectionAccent`       | `SolidColorBrush` | `#0072B0` | 选中区域边框颜色   |
| `ShowColorPreview`      | `bool`            | `true`    | 是否显示颜色预览方块 |

### 分隔符

| 属性                | 类型       | 默认值  | 说明              |
| ----------------- | -------- | ---- | --------------- |
| `ColumnSeparator` | `Regex`  | `\t` | 列分隔符（序列化/粘贴时使用） |
| `RowSeparator`    | `Regex`  | `\n` | 行分隔符（序列化/粘贴时使用） |
| `QuoteSymbol`     | `string` | `"`  | 引号符号（包裹含分隔符的值）  |

### 选择与编辑状态

| 属性          | 类型                              | 默认值       | 说明            |
| ----------- | ------------------------------- | --------- | ------------- |
| `Selection` | `ImmutableList<SelectionRange>` | `[(0,0)]` | 当前选中的单元格区域    |
| `IsEditing` | `bool`                          | `false`   | 是否正在编辑单元格（只读） |
| `CanUndo`   | `bool`                          | `false`   | 是否可以撤销（只读）    |
| `CanRedo`   | `bool`                          | `false`   | 是否可以重做（只读）    |

---

## 数据操作

### 设置数据

```csharp
// 批量设置单元格数据
var data = new Dictionary<(int, int), string>
{
    [(0, 0)] = "Hello",
    [(1, 0)] = "World",
};
spreadsheet.SetData(data);

// 值为 null 的条目等同于清除该单元格
var data2 = new Dictionary<(int, int), string>
{
    [(0, 0)] = null,  // 将清除 (0,0) 单元格
};
spreadsheet.SetData(data2);
```

### 清除内容

```csharp
// 清除当前选中单元格的内容
spreadsheet.ClearContents();
```

### 获取选中数据

```csharp
// 获取当前选中区域的数据（合并为矩形数组）
string[,] data = spreadsheet.GetSelectedData(out (int, int)[,] coordinates);
// data[row, col] 获取值，coordinates[row, col] 获取对应的原始坐标
// 空单元格为 null，不属于选中区域的坐标值为负数
```

### 递归重算

```csharp
spreadsheet.RecalculateAll(); // 强制重新计算公式引擎中的所有公式
```

---

## 选择与导航

### Selection 属性

`Selection` 是一个 `ImmutableList<SelectionRange>`，支持多选（多个不连续或重叠的选区）。

```csharp
// 设置选中 A1 单元格
spreadsheet.Selection = ImmutableList.Create(new SelectionRange(0, 0));

// 选择多个不连续区域
spreadsheet.Selection = ImmutableList.Create(
    new SelectionRange(0, 0, 2, 5),   // A1:C6
    new SelectionRange(5, 0, 7, 5)    // F1:H6
);
```

### SelectionRange 结构

| 属性       | 说明                        |
| -------- | ------------------------- |
| `Left`   | 选区左边界列索引（包含）              |
| `Top`    | 选区上边界行索引（包含）              |
| `Right`  | 选区右边界列索引（包含）              |
| `Bottom` | 选区下边界行索引（包含）              |
| `Width`  | 选区宽度 = `Right - Left + 1` |
| `Height` | 选区高度 = `Bottom - Top + 1` |

**特殊选择区域**：

- **整行选中**: `Left = 0` 且 `Right = MaxTableWidth`
- **整列选中**: `Top = 0` 且 `Bottom = MaxTableHeight`
- **全选**: `Left = 0 & Top = 0` 且 `Right = MaxTableWidth & Bottom = MaxTableHeight`

> 格式设置遵循优先级：**单元格级 > 行/列级 > 全局默认**。选择有限区域设置格式时作用于单元格，选择整行/列时作用于行/列默认值，全选时作用于全局默认值。

### 滚动

```csharp
// 滚动到表格左上角
spreadsheet.ScrollTopLeft();
```

---

## 行与列操作

### 插入

```csharp
// 在当前选中列之前插入列（插入数量 = 选中列数）
spreadsheet.InsertColumns();

// 在当前选中行之前插入行（插入数量 = 选中行数）
spreadsheet.InsertRows();
```

> 要求当前选区为单一连续的整列/整行选区。

### 删除

```csharp
// 删除选中的列
spreadsheet.DeleteColumns();

// 删除选中的行
spreadsheet.DeleteRows();
```

> 要求当前选区为单一连续的整列/整行选区。

### 调整列宽/行高

```csharp
// 自动调整选中列的宽度以适应内容
spreadsheet.AutoFitWidth();

// 自动调整选中行的高度以适应内容
spreadsheet.AutoFitHeight();

// 重置选中列宽为默认值
spreadsheet.ResetWidth();

// 重置选中行高为默认值
spreadsheet.ResetHeight();

// 精确设置指定列的宽度
spreadsheet.SetWidth(new Dictionary<int, double>
{
    [0] = 100.0,
    [1] = 150.0,
    [3] = 80.0,
});

// 精确设置指定行的高度
spreadsheet.SetHeight(new Dictionary<int, double>
{
    [0] = 30.0,
    [2] = 25.0,
});
```

### 获取单元格尺寸

```csharp
(double width, double height) = spreadsheet.GetCellSize(column, row);
```

---

## 外观与格式

### 设置字体样式（Typeface）

```csharp
// 设置选中单元格的字体、样式和粗细
spreadsheet.SetTypeface(new Typeface("Arial", FontStyle.Italic, FontWeight.Bold));

// 获取指定单元格的字体
Typeface typeface = spreadsheet.GetTypeface(column, row);
```

### 设置前景色

```csharp
// 设置选中单元格的文字颜色
spreadsheet.SetForeground(new SolidColorBrush(Colors.Red));
```

### 设置文本对齐

```csharp
// 设置水平对齐
spreadsheet.SetTextAlignment(TextAlignment.Center);
spreadsheet.SetTextAlignment(TextAlignment.Left);
spreadsheet.SetTextAlignment(TextAlignment.Right);

// 设置垂直对齐
spreadsheet.SetVerticalAlignment(VerticalAlignment.Top);
spreadsheet.SetVerticalAlignment(VerticalAlignment.Center);
spreadsheet.SetVerticalAlignment(VerticalAlignment.Bottom);

// 获取指定单元格的对齐方式
(TextAlignment hAlign, VerticalAlignment vAlign) = spreadsheet.GetAlignment(column, row);
```

### 颜色预览

当 `ShowColorPreview = true`（默认）时，内容为 `#RRGGBB` 或 `#RRGGBBAA` 格式的单元格会自动显示一个小颜色预览方块。

### 重置格式

```csharp
// 重置选中单元格/行/列的格式（字体、颜色、对齐等）
spreadsheet.ResetFormat();
```

---

## 合并单元格与表格线

### 合并单元格

Spreadalonia 支持将多个相邻单元格合并为一个区域（类似 Excel 的"合并后居中"）。合并规则：

- 合并区域的**左上角单元格**内容会在整个区域内水平居中显示
- 合并区域**内部**的网格线自动隐藏
- 点击合并区域内的任意单元格，都会选中整个合并区域，编辑操作作用于左上角单元格

```csharp
// 方式一：直接设置合并区域列表（可一次性设置多个）
spreadsheet.MergedRanges = new List<SelectionRange>
{
    new SelectionRange(0, 0, 4, 0),   // 将第 0 行的第 0~4 列合并为一个标题区
    new SelectionRange(4, 2, 4, 3),   // 将第 4 列的第 2~3 行纵向合并
};

// 方式二：逐个添加
spreadsheet.MergeCells(new SelectionRange(0, 0, 4, 0));

// 取消某个合并
spreadsheet.UnmergeCells(new SelectionRange(0, 0, 4, 0));

// 清空所有合并区域
spreadsheet.MergedRanges = null;
```

**SelectionRange 构造**：

- `new SelectionRange(left, top, right, bottom)`：矩形区域（列、行索引均从 0 开始，含右边界与下边界）
- `new SelectionRange(x, y)`：单个单元格

> 注意：合并区域内只有左上角单元格的数据会被显示，其余单元格请保持为空。

### 画表格线（单元格边框）

`CellBorder` 描述一条独立的边框线段（水平线或垂直线），多条线段组合可以构成完整的外框、内部分隔线、虚线等效果。边框绘制在网格线**之上**。

**CellBorder 属性**：

| 属性 | 类型 | 说明 |
| --- | --- | --- |
| `IsHorizontal` | `bool` | `true` = 水平线（行分隔线）；`false` = 垂直线（列分隔线） |
| `LineIndex` | `int` | 分隔线索引：垂直线中 `0` = 第 0 列左侧、`1` = 第 0/1 列之间…；水平线中 `0` = 第 0 行上方、`1` = 第 0/1 行之间… |
| `From` | `int` | 线段起始单元格索引（垂直线 = 起始行；水平线 = 起始列） |
| `To` | `int` | 线段结束单元格索引，**包含**（垂直线 = 结束行；水平线 = 结束列） |
| `Brush` | `IBrush` | 线条颜色 |
| `Thickness` | `double` | 线宽（像素） |
| `DashStyle` | `IDashStyle?` | 虚线样式，`null` 表示实线 |

```csharp
// 方式一：直接设置边框列表
var black = new SolidColorBrush(Colors.Black);

spreadsheet.CellBorders = new List<CellBorder>
{
    // 外框：顶部水平线（行分隔线 0，覆盖第 0~4 列，线宽 2）
    new CellBorder(true, 0, 0, 4, black, 2),
    // 外框：底部水平线（行分隔线 4，覆盖第 0~4 列）
    new CellBorder(true, 4, 0, 4, black, 2),
    // 外框：左侧垂直线（列分隔线 0，覆盖第 0~4 行）
    new CellBorder(false, 0, 0, 4, black, 2),
    // 外框：右侧垂直线（列分隔线 5，覆盖第 0~4 行）
    new CellBorder(false, 5, 0, 4, black, 2),
    // 表头分隔线（行分隔线 2，粗线）
    new CellBorder(true, 2, 0, 4, black, 2),
    // 数据区分隔线（行分隔线 3，虚线：4px 实线 + 2px 间隙）
    new CellBorder(true, 3, 0, 4, black, 1, new DashStyle(new double[] { 4, 2 }, 0)),
    // 数据区竖向分隔线（列分隔线 1~3，覆盖第 1~3 行）
    new CellBorder(false, 1, 1, 3, black, 1),
    new CellBorder(false, 2, 1, 3, black, 1),
    new CellBorder(false, 3, 1, 3, black, 1),
};

// 方式二：逐个添加 / 清除
spreadsheet.AddCellBorder(new CellBorder(true, 0, 0, 4, black, 2));
spreadsheet.ClearCellBorders();   // 清除所有边框线段
```

**快速理解 `LineIndex` / `From` / `To`**：把表格想象成棋盘，水平线与垂直线都画在单元格之间的"缝隙"上。以 5×5 表格为例：

- 顶部框线 = 水平线，`LineIndex = 0`（第 0 行上方），`From = 0`、`To = 4`（横跨第 0~4 列）
- 左框线 = 垂直线，`LineIndex = 0`（第 0 列左侧），`From = 0`、`To = 4`（纵跨第 0~4 行）
- 表格底部框线 = 水平线，`LineIndex = 5`（第 5 行上方，即第 4 行下方）

### 单元格背景

为单元格区域设置填充色（绘制在网格线之下，适合做表头/标题底色）：

```csharp
// 方式一：设置背景列表
spreadsheet.CellBackgrounds = new List<CellBackground>
{
    new CellBackground(new SelectionRange(0, 0, 4, 0), new SolidColorBrush(Color.Parse("#4472C4"))),
    new CellBackground(new SelectionRange(0, 1, 3, 1), new SolidColorBrush(Color.Parse("#D9E2F3"))),
};

// 方式二：逐个添加 / 清除
spreadsheet.AddCellBackground(range, brush);
spreadsheet.ClearCellBackgrounds();
```

### 单元格级样式字典

合并、边框、背景之外，还可以直接操作单元格级样式字典（修改后需要调用 `Refresh()` 重绘）：

```csharp
spreadsheet.CellFontSize[(0, 0)] = 18;                                                  // 字号
spreadsheet.CellTypefaces[(0, 0)] = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
spreadsheet.CellForeground[(0, 0)] = new SolidColorBrush(Colors.White);                  // 文字颜色
spreadsheet.CellTextAlignment[(0, 0)] = TextAlignment.Center;                            // 水平对齐
spreadsheet.CellVerticalAlignment[(0, 0)] = VerticalAlignment.Center;                    // 垂直对齐
spreadsheet.CellMargin[(0, 0)] = new Thickness(3);                                       // 内边距

spreadsheet.Refresh();   // 强制重绘（修改字典后调用）
```

---

## 剪贴板操作

### 复制

```csharp
// 将选中单元格内容复制到系统剪贴板
spreadsheet.Copy();
```

使用 `ColumnSeparator`（默认 `\t`）和 `RowSeparator`（默认 `\n`）拼接单元格内容，可与其他电子表格软件兼容。

### 剪切

```csharp
// 复制选中内容后清除单元格
spreadsheet.Cut();
```

### 粘贴

```csharp
// 从系统剪贴板粘贴（overwriteEmpty=true 时覆盖空单元格）
await spreadsheet.Paste(overwriteEmpty: true);

// 粘贴指定文本
spreadsheet.Paste(
    text: "A\tB\nC\tD",
    overwriteEmpty: false,
    rowSeparator: new Regex("\n"),
    columnSeparator: new Regex("\t")
);
```

- `overwriteEmpty = false`: 粘贴内容中的空单元格不会覆盖目标单元格
- `overwriteEmpty = true`: 粘贴内容中的空单元格会清除目标单元格

---

## 撤销与重做

```csharp
// 检查是否可撤销
if (spreadsheet.CanUndo)
    spreadsheet.Undo();

// 检查是否可重做
if (spreadsheet.CanRedo)
    spreadsheet.Redo();
```

撤销/重做栈覆盖：数据变更、格式变更（字体、颜色、对齐、边距）、行列调整（高度、宽度）、插入/删除行列。所有通过公开 API 执行的操作都会被记录。

> **多工作表时**：切换当前工作表会清空撤销/重做栈，撤销不会跨表回滚（详见[第 11 章](#多工作表sheet)）。

---

## 序列化与加载

### 序列化数据

```csharp
// 序列化整个表格为字符串（使用默认分隔符）
string csv = spreadsheet.SerializeData();

// 序列化指定选区
string selectedText = spreadsheet.GetTextRepresentation(selection);
```

### 序列化格式

```csharp
// 序列化格式信息
string formatInfo = spreadsheet.SerializeFormat();
```

### 加载数据

```csharp
// 加载之前序列化的数据和格式
spreadsheet.Load(serializedData, serializedFormat);
```

此方法首先清除所有现有数据，然后加载指定的数据和格式。数据和格式必须使用相同的分隔符序列化生成。

### 加载 RGF 报表

Spreadalonia 支持加载 RGF（Report Generator Format）文档，一次性恢复**数据、列宽/行高、合并区域、单元格背景、边框、字体样式、对齐方式**等全部信息。

`RgfParser` 会自动识别以下两种格式（无需手动转换）：

1. **ReoGrid v3 原生格式**（根节点 `<grid>`，含 `<head>`/`<rows>`/`<cols>`/`<v-borders>`/`<h-borders>`/`<cells>`）——即 ReoGrid 通过 `grid.Save(stream, FileFormat.ReoGridFormat)` 或 `grid.Load(stream, FileFormat.ReoGridFormat)` 使用的格式，支持**明文 XML 与 GZip 压缩**两种形态，可直接加载 ReoGrid 导出的 `.rgf` 模板文件（如金蝶报表模板、自行用 ReoGrid 设计的模板）。
2. **旧版 Spreadalonia `<rgf>` XML 格式**——向后兼容保留。

```csharp
// 直接加载 ReoGrid v3 导出的 .rgf 文件（明文或 GZip 压缩均可）
using (FileStream fs = File.OpenRead("GL.rgf"))
{
    spreadsheet.LoadRgf(fs);
}
```

也可以先用 `RgfParser` 解析为 `RgfData` 对象再自行处理：

```csharp
using (FileStream fs = File.OpenRead("GL.rgf"))
{
    RgfData data = RgfParser.Load(fs);
    // data.Data / data.MergedRanges / data.CellBorders / data.CellBackgrounds ...
}
```

> 提示：ReoGrid 的 `v-border`/`h-border` 元素中的 `pos` 属性（`left`/`right`/`top`/`bottom`/`all`）会按表格边界自动换算为 Spreadalonia 的 `CellBorder` 分隔线索引；行/列/单元格的背景色（`bgcolor`）、字体、字号、对齐等样式会按「全局 → 行 → 列 → 单元格」的优先级继承。

旧版 `<rgf>` 格式的文档示例（XML）可参考 `Demo/MainWindow.axaml.cs` 中的 `LoadRgfDemo()`。

### 文本分割工具方法

```csharp
// 静态方法：按行列分隔符分割文本
string[][] rows = Spreadsheet.SplitData(
    text: "a,b\nc,d",
    rowSeparator: "\n",
    columnSeparator: ",",
    quote: "\"",
    out int maxWidth
);
// rows = [["a","b"], ["c","d"]]
// maxWidth = 2
```

---

## 多工作表（Sheet）

自 v1.9.0 起，一个 `Spreadsheet` 控件可以包含多张工作表（`SpreadsheetSheet`），底部自带页签栏（`SheetTabStrip`）供用户点击切换。

### 基本概念

- **隐式单表**：不调用任何工作表 API 时，控件内部只有一张名为 `Sheet1` 的工作表，所有旧 API 的行为完全不变。
- **状态按表隔离**：每张表各自保存数据、格式（字体/前景色/对齐/边距/背景/边框）、列宽行高、合并区域、冻结行列、隐藏行列、自定义单元格、选区、滚动偏移等全部状态；切换工作表会完整恢复该表的状态，互不影响。
- **撤销栈按表清空**：切换当前表时 Undo/Redo 栈被清空，撤销不会跨表回滚。
- **公式引擎按表独立**：每张表有自己的引擎，依赖图与缓存互不污染；但宿主注册的自定义函数（`Spreadsheet.FormulaEngine.Functions`）是共享的，注册一次对所有表生效。
- **坐标键统一为 `(column, row)`**，与单表 API 完全一致。

### 快速示例

```csharp
// 第一张表（控件默认已创建）
SpreadsheetSheet summary = SpreadsheetControl.CurrentSheet;
SpreadsheetControl.RenameSheet(summary, "汇总");
SpreadsheetControl.SetData(new Dictionary<(int, int), string>
{
    [(0, 0)] = "科目",
    [(1, 0)] = "金额",
});

// 追加第二张表并填充：AddSheet 返回新表，并使其成为当前表
SpreadsheetSheet detail = SpreadsheetControl.AddSheet("明细");
SpreadsheetControl.SetData(detail, new Dictionary<(int, int), string>
{
    [(0, 0)] = "明细数据",
});

// 用代码切回第一张表（也支持双向绑定）
SpreadsheetControl.CurrentSheetIndex = 0;
```

### 工作表管理 API

| 方法                                       | 返回值                | 说明                                    |
| ---------------------------------------- | ------------------ | ------------------------------------- |
| `AddSheet(string name)`                  | `SpreadsheetSheet` | 在末尾追加工作表，并切换为当前表                      |
| `InsertSheet(int index, string name)`    | `SpreadsheetSheet` | 在指定位置插入工作表                            |
| `RemoveSheet(SpreadsheetSheet sheet)`    | `bool`             | 删除指定工作表；控件始终保留至少一张表                   |
| `RemoveSheetAt(int index)`               | `bool`             | 删除指定位置的工作表                            |
| `RenameSheet(SpreadsheetSheet, string)`  | `string`           | 重命名，返回实际使用的名称（重名自动追加序号，如 `Sheet1 (2)`）|
| `MoveSheet(SpreadsheetSheet sheet, int)` | `bool`             | 把工作表移动到指定位置                           |
| `IndexOfSheet(SpreadsheetSheet sheet)`   | `int`              | 查询工作表索引，不属于该控件时返回 `-1`                |
| `MoveToNextSheet()` / `MoveToPreviousSheet()` | `void`        | 切到下一张 / 上一张表（对应 `Ctrl+PageDown/PageUp`）|
| `RefreshSheetTabs()`                     | `void`             | 手动刷新页签栏（增删表后会自动刷新）                    |

### 索引与隐藏工作表

- `SpreadsheetSheet.Index` 是工作表在集合中的位置，**与导入源文件的顺序 1:1 对应（含隐藏表）**，可用于回查源文件中的工作表。
- `SpreadsheetSheet.IsHidden` 标记隐藏表：隐藏表**保留索引**，默认不在页签栏显示；代码仍可通过 `CurrentSheetIndex` 切换到它。
- `SpreadsheetSheet.Tag` 供宿主挂自定义数据（例如源文件工作表的原始索引）。
- 页签栏开关 `SheetTabHiddenSheetsVisible = true` 时，隐藏表也会以灰显页签出现。

```csharp
SpreadsheetSheet hidden = SpreadsheetControl.AddSheet("原始数据");
hidden.IsHidden = true;      // 页签不显示，索引仍然是 2

SpreadsheetControl.CurrentSheetIndex = hidden.Index;   // 代码仍可切过去
```

### 页签栏开关与样式

| 属性                             | 类型       | 默认值    | 说明                    |
| ------------------------------ | -------- | ------ | --------------------- |
| `SheetTabVisible`              | `bool`   | `true` | 是否显示页签栏                |
| `SheetTabVisibleWhenSingle`    | `bool`   | `false`| 只有一张表时是否也显示页签栏         |
| `SheetTabNewButtonVisible`     | `bool`   | `true` | 是否显示「+」新建按钮           |
| `SheetTabHiddenSheetsVisible`  | `bool`   | `false`| 是否显示隐藏表的页签             |
| `SheetTabAllowRename`          | `bool`   | `true` | 允许双击页签重命名             |
| `SheetTabAllowDelete`          | `bool`   | `false`| 允许从页签删除工作表            |
| `SheetTabAllowInsert`          | `bool`   | `true` | 允许从页签插入工作表            |
| `SheetTabAllowReorder`         | `bool`   | `true` | 允许拖拽页签调整顺序            |
| `SheetTabHeight`               | `double` | `28`   | 页签栏高度                 |
| `SheetTabBackground`           | `IBrush` | `null` | 页签栏背景（`null` 跟随主题）     |
| `SheetTabForeground`           | `IBrush` | `null` | 页签文字颜色                 |
| `SheetTabSelectedBackground`   | `IBrush` | `null` | 选中页签背景                 |
| `SheetTabBorderBrush`          | `IBrush` | `null` | 页签边框颜色                 |

> 单表且 `SheetTabVisibleWhenSingle = false` 时页签栏高度为 0、不占位。

### 每表交互开关

| 属性                   | 类型                     | 默认值     | 说明                                        |
| -------------------- | ---------------------- | ------- | ----------------------------------------- |
| `IsReadOnly`         | `bool`                 | `false` | 整表只读：用户不能编辑、粘贴、清除该表单元格，**程序化写入不受限制**       |
| `AllowDragMoveCells` | `bool`                 | `true`  | 是否允许拖拽选区边框移动内容                            |
| `AllowTextOverflow`  | `bool`                 | `false` | 是否允许超宽文本溢出到右侧空单元格（类似 Excel）               |
| `SelectionStyle`     | `SheetSelectionStyle`  | `Fill`  | 选区高亮样式：`Fill`（半透明填充）或 `FocusRect`（只画边框）    |

```csharp
SpreadsheetSheet imported = SpreadsheetControl.AddSheet("导入数据");
imported.IsReadOnly = true;                              // 用户不可改
imported.AllowDragMoveCells = false;                     // 不能被误拖动
imported.AllowTextOverflow = true;                       // 长文本溢出显示
imported.SelectionStyle = SheetSelectionStyle.FocusRect; // 只画选区边框

SpreadsheetControl.SetData(imported, data);              // 宿主仍可程序化写入
```

### 指定工作表的 API 重载

多数 API 都有带 `sheet` 参数的重载，可对**非当前表**直接操作，无需切换：

```csharp
SpreadsheetControl.SetData(sheet, data);                    // 写入指定表
SpreadsheetControl.SetWidth(sheet, new Dictionary<int, double> { [0] = 120 });
SpreadsheetControl.SetHeight(sheet, rowHeights);
SpreadsheetControl.InsertColumns(sheet, 0, 1);              // 在指定表插入一列（如用于写校验错误列）
SpreadsheetControl.DeleteColumns(sheet, 0, 1);
```

### 工作表事件

| 事件                    | 参数类型                            | 触发时机               |
| --------------------- | ------------------------------- | ------------------ |
| `CurrentSheetChanged` | `CurrentSheetChangedEventArgs`  | 当前显示的工作表变化（点页签或代码赋值）|
| `SheetInserted`       | `SheetEventArgs`                | 新增工作表              |
| `SheetRemoved`        | `SheetEventArgs`                | 删除工作表              |
| `SheetRenamed`        | `SheetRenamedEventArgs`         | 重命名（`OldName`/`NewName`）|
| `SheetMoved`          | `SheetMovedEventArgs`           | 调整顺序（`OldIndex`/`NewIndex`）|

```csharp
SpreadsheetControl.CurrentSheetChanged += (sender, e) =>
{
    // e.OldSheet / e.NewSheet、e.OldIndex / e.NewIndex
    Console.WriteLine($"切换到：{e.NewSheet?.Name}（索引 {e.NewIndex}）");
};
```

---

## Excel xlsx 导入与导出

### 导出

```csharp
// 导出当前显示的工作表
using (FileStream fs = File.Create("sheet.xlsx"))
{
    SpreadsheetControl.SaveXlsx(fs);
}

// 导出全部工作表（每张表一个 Excel 工作表，保留名称与可见性）
using (FileStream fs = File.Create("book.xlsx"))
{
    SpreadsheetControl.SaveXlsx(fs, SpreadsheetControl.Sheets);
}

// 带导出选项
var options = new XlsxExportOptions
{
    ExportFormulas = true,          // 默认 true：保留 Excel 兼容公式
    ExportLinkAsHyperlink = false,  // 默认 false：链接单元格导出为纯文本
    ExportHiddenRows = false,       // 默认 false：折叠行不导出
    ExportHiddenColumns = false,    // 默认 false：隐藏列不导出
};
SpreadsheetControl.SaveXlsx("book.xlsx", SpreadsheetControl.Sheets, options);
```

### 导入

```csharp
// 从文件导入：每个 Excel 工作表对应一张 SpreadsheetSheet，顺序、名称、可见性一致
IReadOnlyList<SpreadsheetSheet> sheets = SpreadsheetControl.LoadXlsx("book.xlsx");

// 从流导入，并指定导入选项
var importOptions = new XlsxImportOptions
{
    ImportHiddenSheets = true,      // 默认 true：隐藏工作表也导入（保留索引 1:1）
    ImportFormulas = true,          // 默认 true：公式导入后由本表引擎重算
    ImportStyles = true,            // 默认 true：字体/颜色/对齐/背景
    ImportMergedRanges = true,      // 默认 true：合并区域
    ImportColumnWidths = true,      // 默认 true：列宽
    ImportRowHeights = true,        // 默认 true：行高
    ImportHiddenRowsColumns = true, // 默认 true：隐藏行列
    ClearExistingSheets = true,     // 默认 true：先清空现有工作表
    MaxRowsPerSheet = 0,            // 0 = 不限制
    MaxColumnsPerSheet = 0,         // 0 = 不限制
};
using (FileStream fs = File.OpenRead("book.xlsx"))
{
    SpreadsheetControl.LoadXlsx(fs, importOptions);
}
```

导入后显示**第一张未隐藏**的工作表；`Sheet.Index` 与源文件中的工作表索引一一对应（隐藏表也占位），因此可以用 `Index` 回查源文件的对应表。

### 典型流程：导入 → 切表 → 读当前表

```csharp
SpreadsheetControl.LoadXlsx(fileName);

foreach (SpreadsheetSheet sheet in SpreadsheetControl.Sheets)
{
    Console.WriteLine($"{sheet.Index}: {sheet.Name}{(sheet.IsHidden ? "（隐藏）" : "")}，共 {sheet.Data.Count} 个单元格");
}

// 读当前表
SpreadsheetSheet current = SpreadsheetControl.CurrentSheet;
string a1 = current.Data.TryGetValue((0, 0), out string value) ? value : null;

// 切到指定表并读取
SpreadsheetControl.CurrentSheetIndex = 2;
var rows = SpreadsheetControl.Sheets[2].Data;
```

---

## 事件

### CellSizeChanged

当选中单元格通过拖拽行列标题改变大小时触发。

```csharp
spreadsheet.CellSizeChanged += (sender, e) =>
{
    // e.Left, e.Top - 发生改变的单元格坐标
    // e.Width, e.Height - 新的尺寸
    Console.WriteLine($"Cell ({e.Left},{e.Top}) size: {e.Width}x{e.Height}");
};
```

### ColorDoubleTapped

当 `ShowColorPreview = true` 时，用户双击颜色预览方块触发。

```csharp
spreadsheet.ColorDoubleTapped += (sender, e) =>
{
    // e.Left, e.Top - 单元格坐标
    // e.Color - 当前颜色值
    // 设置 e.Handled = true 可阻止进入编辑模式（适合显示颜色选择器）
    var colorPicker = new ColorPickerDialog(e.Color);
    e.Handled = true;
};
```

### 工作表事件

`CurrentSheetChanged`、`SheetInserted`、`SheetRemoved`、`SheetRenamed`、`SheetMoved` 详见[第 11 章「工作表事件」](#工作表事件)。

```csharp
SpreadsheetControl.SheetRemoved += (sender, e) =>
{
    // e.Sheet、e.Index
    Console.WriteLine($"工作表被删除：{e.Sheet?.Name}");
};
```

---

## 键盘快捷键

### 导航模式（非编辑状态）

| 快捷键                                         | 功能             |
| ------------------------------------------- | -------------- |
| `↑` `↓` `←` `→`                             | 移动选中单元格        |
| `Tab`                                       | 向右移动一个单元格      |
| `Shift + Tab`                               | 向左移动一个单元格      |
| `Enter`                                     | 开始编辑当前单元格      |
| `F2`                                        | 开始编辑当前单元格      |
| `Delete`                                    | 清除选中单元格内容      |
| `Backspace`                                 | 清除选中单元格内容并进入编辑 |
| `Ctrl + A`                                  | 全选整个表格         |
| `Ctrl + C` / `Ctrl + Insert`                | 复制             |
| `Ctrl + X`                                  | 剪切             |
| `Ctrl + V` / `Shift + Insert`               | 粘贴（覆盖空值）       |
| `Ctrl + Shift + V` / `Alt + Shift + Insert` | 粘贴（跳过空值）       |
| `Ctrl + Z`                                  | 撤销             |
| `Ctrl + Y`                                  | 重做             |
| `Ctrl + PageDown`                           | 切换到下一张工作表      |
| `Ctrl + PageUp`                             | 切换到上一张工作表      |

### 编辑模式

编辑模式下方向键和大部分快捷键直接在编辑框内操作，`Enter` 或 `Tab` 提交编辑并退出编辑模式。

> macOS 上 `Ctrl` 键会被替换为 `Meta`（Command）键。

---

## 右键菜单

表格提供内置的右键菜单，包含以下功能：

| 菜单项                 | 功能       |
| ------------------- | -------- |
| Cut                 | 剪切选中内容   |
| Copy                | 复制选中内容   |
| Paste               | 粘贴（覆盖空值） |
| Paste (skip blanks) | 粘贴（跳过空值） |
| Insert columns      | 插入列      |
| Insert rows         | 插入行      |
| Delete columns      | 删除列      |
| Delete rows         | 删除行      |
| Clear contents      | 清除内容     |
| Reset format        | 重置格式     |
| AutoFit width       | 自动调整列宽   |
| Reset width         | 重置列宽     |
| AutoFit height      | 自动调整行高   |
| Reset height        | 重置行高     |

---

## 其他交互功能

### 自动填充

选中一个或多个单元格后，可以拖拽选区右下角的填充手柄进行自动填充。Spreadalonia 支持智能序列填充（如识别数字序列、日期序列等）。

### 拖拽移动

选中单元格区域后，可以拖拽选区边框将内容移动到新位置。

### 行列标题拖拽

拖拽行标题或列标题的边缘可以调整行高和列宽。双击标题边缘可自动调整到最佳尺寸。

---

## 完整 API 速查表

### 属性

| API                        | 类型                              | 默认值              | 读写  |
| -------------------------- | ------------------------------- | ---------------- | --- |
| `Data`                     | `Dictionary<(int,int),string>`  | -                | 只读  |
| `FormulaEngine`            | `FormulaEngine`                 | -                | 只读  |
| `DefaultTextAlignment`     | `TextAlignment`                 | `Left`           | 读写  |
| `DefaultVerticalAlignment` | `VerticalAlignment`             | `Center`         | 读写  |
| `DefaultRowHeight`         | `double`                        | `23`             | 读写  |
| `DefaultColumnWidth`       | `double`                        | `65`             | 读写  |
| `DefaultCellMargin`        | `Thickness`                     | `3`              | 读写  |
| `HeaderFontFamily`         | `FontFamily`                    | `Default`        | 读写  |
| `HeaderFontSize`           | `double`                        | `14`             | 读写  |
| `HeaderForeground`         | `IBrush`                        | `Black`          | 读写  |
| `HeaderBackground`         | `Color`                         | `#F0F0F0`        | 读写  |
| `GridColor`                | `Color`                         | `#DCDCDC`        | 读写  |
| `SpreadsheetBackground`    | `SolidColorBrush`               | `White`          | 读写  |
| `SelectionAccent`          | `SolidColorBrush`               | `#0072B0`        | 读写  |
| `Selection`                | `ImmutableList<SelectionRange>` | `[(0,0)]`        | 读写  |
| `IsEditing`                | `bool`                          | `false`          | 只读  |
| `CanUndo`                  | `bool`                          | `false`          | 只读  |
| `CanRedo`                  | `bool`                          | `false`          | 只读  |
| `ColumnSeparator`          | `Regex`                         | `\t`             | 读写  |
| `RowSeparator`             | `Regex`                         | `\n`             | 读写  |
| `QuoteSymbol`              | `string`                        | `"`              | 读写  |
| `MaxTableWidth`            | `int`                           | `int.MaxValue-2` | 读写  |
| `MaxTableHeight`           | `int`                           | `int.MaxValue-2` | 读写  |
| `ShowColorPreview`         | `bool`                          | `true`           | 读写  |
| `MergedRanges`             | `IReadOnlyList<SelectionRange>` | 空                | 读写  |
| `CellBorders`              | `List<CellBorder>`              | 空                | 读写  |
| `CellBackgrounds`          | `List<CellBackground>`          | 空                | 读写  |
| `CellFontSize`             | `Dictionary<(int,int), double>` | 空                | 读写  |
| `CellTypefaces`            | `Dictionary<(int,int), Typeface>` | 空              | 读写  |
| `CellForeground`           | `Dictionary<(int,int), IBrush>` | 空                | 读写  |
| `CellTextAlignment`        | `Dictionary<(int,int), TextAlignment>` | 空          | 读写  |
| `CellVerticalAlignment`    | `Dictionary<(int,int), VerticalAlignment>` | 空      | 读写  |
| `CellMargin`               | `Dictionary<(int,int), Thickness>` | 空             | 读写  |
| `Sheets`                   | `IReadOnlyList<SpreadsheetSheet>` | 1 张 `Sheet1` | 只读  |
| `CurrentSheet`             | `SpreadsheetSheet`     | -                | 只读  |
| `CurrentSheetIndex`        | `int`                   | `0`              | 读写（支持双向绑定） |
| `SheetTabVisible`          | `bool`                  | `true`           | 读写  |
| `SheetTabVisibleWhenSingle`| `bool`                  | `false`          | 读写  |
| `SheetTabNewButtonVisible` | `bool`                  | `true`           | 读写  |
| `SheetTabHiddenSheetsVisible` | `bool`               | `false`          | 读写  |
| `SheetTabAllowRename`      | `bool`                  | `true`           | 读写  |
| `SheetTabAllowDelete`      | `bool`                  | `false`          | 读写  |
| `SheetTabAllowInsert`      | `bool`                  | `true`           | 读写  |
| `SheetTabAllowReorder`     | `bool`                  | `true`           | 读写  |
| `SheetTabHeight`           | `double`                | `28`             | 读写  |

### 方法

| 方法                                            | 返回值                                  | 说明             |
| --------------------------------------------- | ------------------------------------ | -------------- |
| `SetData(data)`                               | `void`                               | 批量设置单元格数据      |
| `ClearContents()`                             | `void`                               | 清除选中内容         |
| `GetSelectedData(out coords)`                 | `string[,]`                          | 获取选中区域的矩形数据    |
| `Copy()`                                      | `void`                               | 复制到剪贴板         |
| `Cut()`                                       | `void`                               | 剪切到剪贴板         |
| `Paste(overwriteEmpty)`                       | `Task`                               | 从剪贴板粘贴         |
| `Paste(text, overwriteEmpty, rowSep, colSep)` | `void`                               | 粘贴指定文本         |
| `Undo()`                                      | `void`                               | 撤销             |
| `Redo()`                                      | `void`                               | 重做             |
| `InsertColumns()`                             | `void`                               | 插入列            |
| `DeleteColumns()`                             | `void`                               | 删除列            |
| `InsertRows()`                                | `void`                               | 插入行            |
| `DeleteRows()`                                | `void`                               | 删除行            |
| `AutoFitWidth()`                              | `void`                               | 自适应列宽          |
| `AutoFitHeight()`                             | `void`                               | 自适应行高          |
| `ResetWidth()`                                | `void`                               | 重置列宽           |
| `ResetHeight()`                               | `void`                               | 重置行高           |
| `SetWidth(columnWidths)`                      | `void`                               | 精确设置列宽         |
| `SetHeight(rowHeights)`                       | `void`                               | 精确设置行高         |
| `SetTypeface(typeface)`                       | `void`                               | 设置字体           |
| `GetTypeface(col, row)`                       | `Typeface`                           | 获取字体           |
| `SetForeground(brush)`                        | `void`                               | 设置前景色          |
| `SetTextAlignment(align)`                     | `void`                               | 设置水平对齐         |
| `SetVerticalAlignment(align)`                 | `void`                               | 设置垂直对齐         |
| `GetAlignment(col, row)`                      | `(TextAlignment, VerticalAlignment)` | 获取对齐方式         |
| `GetCellSize(col, row)`                       | `(double, double)`                   | 获取单元格尺寸        |
| `ResetFormat()`                               | `void`                               | 重置格式           |
| `SerializeData()`                             | `string`                             | 序列化数据          |
| `SerializeFormat()`                           | `string`                             | 序列化格式          |
| `GetTextRepresentation(selection)`            | `string`                             | 序列化指定选区        |
| `Load(data, format)`                          | `void`                               | 加载序列化的数据和格式    |
| `ScrollTopLeft()`                             | `void`                               | 滚动到左上角         |
| `RecalculateAll()`                            | `void`                               | 强制重新计算所有公式     |
| `MergeCells(range)`                           | `void`                               | 合并指定区域单元格       |
| `UnmergeCells(range)`                         | `void`                               | 取消指定区域合并        |
| `AddCellBorder(border)`                       | `void`                               | 添加一条边框线段        |
| `ClearCellBorders()`                          | `void`                               | 清除所有边框线段        |
| `AddCellBackground(range, brush)`             | `void`                               | 为区域添加背景色        |
| `ClearCellBackgrounds()`                      | `void`                               | 清除所有背景色         |
| `Refresh()`                                   | `void`                               | 修改样式字典后强制重绘     |
| `LoadRgf(stream)`                             | `void`                               | 加载 RGF 报表文档      |
| `SplitData(text, ...)`                        | `static string[][]`                  | 静态方法，分割文本为二维数组 |
| `AddSheet(name)`                              | `SpreadsheetSheet`                   | 追加工作表并切换为当前表   |
| `InsertSheet(index, name)`                    | `SpreadsheetSheet`                   | 在指定位置插入工作表     |
| `RemoveSheet(sheet)`                          | `bool`                               | 删除指定工作表         |
| `RemoveSheetAt(index)`                        | `bool`                               | 删除指定位置的工作表      |
| `RenameSheet(sheet, name)`                    | `string`                             | 重命名工作表，返回实际名称   |
| `MoveSheet(sheet, index)`                     | `bool`                               | 移动工作表到指定位置      |
| `IndexOfSheet(sheet)`                         | `int`                                | 查询工作表索引         |
| `MoveToNextSheet()` / `MoveToPreviousSheet()` | `void`                               | 切换到下一张/上一张表     |
| `RefreshSheetTabs()`                          | `void`                               | 刷新页签栏           |
| `SetData(sheet, data)`                        | `void`                               | 写入指定工作表         |
| `SetWidth(sheet, widths)`                     | `void`                               | 设置指定工作表的列宽      |
| `SetHeight(sheet, heights)`                   | `void`                               | 设置指定工作表的行高      |
| `InsertColumns(sheet, index, count)`          | `void`                               | 在指定工作表插入列       |
| `DeleteColumns(sheet, index, count)`          | `void`                               | 在指定工作表删除列       |
| `SaveXlsx(stream / fileName, options)`        | `void`                               | 导出当前工作表到 xlsx   |
| `SaveXlsx(stream / fileName, sheets, options)`| `void`                               | 导出多张工作表到 xlsx   |
| `LoadXlsx(stream / fileName, options)`        | `IReadOnlyList<SpreadsheetSheet>`    | 导入 xlsx 工作簿      |

### 事件

| 事件                  | 参数类型                         | 说明      |
| ------------------- | ---------------------------- | ------- |
| `CellSizeChanged`   | `CellSizeChangedEventArgs`   | 单元格尺寸变化 |
| `ColorDoubleTapped` | `ColorDoubleTappedEventArgs` | 双击颜色预览  |
| `CurrentSheetChanged` | `CurrentSheetChangedEventArgs` | 当前工作表变化 |
| `SheetInserted`     | `SheetEventArgs`             | 新增工作表   |
| `SheetRemoved`      | `SheetEventArgs`             | 删除工作表   |
| `SheetRenamed`      | `SheetRenamedEventArgs`      | 工作表重命名  |
| `SheetMoved`        | `SheetMovedEventArgs`        | 工作表顺序调整 |
