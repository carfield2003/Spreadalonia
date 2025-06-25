# Spreadalonia 基本功能使用说明

> **版本**: 1.1.0 | **目标框架**: .NET Standard 2.0 | **UI 框架**: Avalonia 11  
> **许可证**: LGPL v3 | **作者**: Giorgio Bianchini, University of Bristol  
> 本文档涵盖公式引擎以外的所有基本功能，公式引擎的使用请参考 `公式与自定义函数使用说明.md`。

---

## 目录

1. [快速入门](#快速入门)
2. [控件属性总览](#控件属性总览)
3. [数据操作](#数据操作)
4. [选择与导航](#选择与导航)
5. [行与列操作](#行与列操作)
6. [外观与格式](#外观与格式)
7. [剪贴板操作](#剪贴板操作)
8. [撤销与重做](#撤销与重做)
9. [序列化与加载](#序列化与加载)
10. [事件](#事件)
11. [键盘快捷键](#键盘快捷键)
12. [右键菜单](#右键菜单)
13. [SelectionRange 说明](#selectionrange-说明)
14. [完整 API 速查表](#完整-api-速查表)

---

## 快速入门

### 安装

通过 NuGet 安装：

```
Install-Package Spreadalonia
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
| `SplitData(text, ...)`                        | `static string[][]`                  | 静态方法，分割文本为二维数组 |

### 事件

| 事件                  | 参数类型                         | 说明      |
| ------------------- | ---------------------------- | ------- |
| `CellSizeChanged`   | `CellSizeChangedEventArgs`   | 单元格尺寸变化 |
| `ColorDoubleTapped` | `ColorDoubleTappedEventArgs` | 双击颜色预览  |
