# Spreadalonia 增强需求说明书 v1.5.0
## 目标：支持 SSFinance《期初余额》的原生树形折叠表与数值单元格

> 本文档面向 Spreadalonia 控件维护者（实现方），用于在控件端新增能力，供 Avalonia 项目 SSFinance 移植 WPF《期初余额》功能时消费。
> 所有 WPF 对照均给出精确出处（文件 + 行号），所有"缺口"结论均基于 Spreadalonia 源码核查，请勿臆造 API。

---

### 0. 背景与现状

- **消费方**：Avalonia 项目 `C:\Users\89664\source\repos\carfield2003\SCloud\SSFinance`，当前引用 `Spreadalonia.Carfield2003 1.4.0`（`Directory.Packages.props` 第 29 行）。
- **移植来源**：WPF 项目 `C:\Users\89664\source\repos\carfield2003\SSFinanceWin`，页面 `ArchivesPage/BeginningBalances.xaml(.cs)`，底层表格控件为 ReoGrid（`unvell.ReoGrid`）。
- **Spreadalonia 现有能力**：`TitleCell`、`LinkCell`、`NumericLinkCell`（`Spreadalonia/LinkCell.cs`、`Spreadalonia/TitleCell.cs`）；挂载方式为 `Spreadsheet.TitleCells` / `Spreadsheet.LinkCells`，key = `(列, 行)`，由 `Table.cs` 负责渲染，点击抛 `Spreadsheet.LinkClick`。**新增类型请沿用这套"挂载对象 + 字典"风格。**
- **已确认的缺口**（源码级核查结论）：

| # | 缺口 | 核查依据 |
|---|---|---|
| 1 | 无行隐藏 / 行分组机制 | `Table.cs` 无 `HiddenRows`；渲染只按 `RowHeights` 累加（见 `GetVisibleLineSegments`、`GetHeight`） |
| 2 | 无单元格 / 区域只读 | 源码中 `ReadOnly` 关键字无命中 |
| 3 | 无冻结行列 | 源码中 `Freeze` 关键字无命中 |
| 4 | 无数值单元格与输入校验 | 仅有 `NumericLinkCell` 的显示格式化，无输入校验 |
| 5 | 无"编辑结束"对外事件 | 只有 `Spreadsheet.axaml.cs` 行 932–1014 的 `EditingBox.LostFocus` 内部逻辑，宿主无法挂钩 |

---

## 需求 A：TreeGradeCell（科目树形折叠单元格）

### A.1 WPF 参考实现（逐条对照）

类 `ExpendTreeGradeCell : CellBody`，文件 `SSFinanceWin/PublicClass/ControlClass.cs` 行 20–236。

**构造参数**：级次 `grade`、文本 `text`（科目编码）、是否末级 `isEnd`、是否本层最后一行 `bLastNode`、背景 `backColor`（父级行 `#D9FFF5`，末级行 `#B9F5D8`）。

**绘制常量**：`linelength = 6`（+/- 笔画长度）、`rectlength = 11`（方块边长）、`marginunit = 4`（每级缩进量）。

**坐标公式**（`H` = 当前行的实际行高，`W` = 列宽；**`H` 必须取运行时的真实行高，不得写死常量**）：

| 元素 | 坐标 |
|---|---|
| 方块左上角 | `x = grade*4 + (grade-1)*11`，`y = (H-11)/2` |
| 方块内横线（`-`） | `(x+2, H/2) → (x+8, H/2)` |
| 展开态额外竖线（构成 `+`） | `(x+5, (H-6)/2) → (x+5, (H-6)/2+6)` |
| 文本矩形 | `X = (grade+1)*4 + grade*11`，宽 `W - (grade+1)*4 - (grade-1)*11`，左对齐、垂直居中 |

**虚线规则**（1px，`DashStyle.Dot`，黑色）。对每个 `i = 1..grade`，记 `xi = i*4 + (i-1)*11`：

| 情形 | 画法 |
|---|---|
| `i < grade`（祖先层）且该层还有后续兄弟 | 满高竖线 `(xi+5, Top) → (xi+5, Bottom)` |
| `i < grade` 且是本层最后一个节点 | 只画上半段 `(xi+5, Top) → (xi+5, H/2)` |
| `i == grade`，非末级，折叠态 | 上半段 + 下半段（从方块底边到 `Bottom`） |
| `i == grade`，非末级，展开态 | 水平连接线 `((i-1)*4+(i-2)*11+5, H/2) → (xi, H/2)`（`grade > 1` 时） |
| `i == grade`，末级 | 只画从上一层到本行中点的水平连接线，不画竖线 |

### A.1.1 竖线连续性要求（宿主特别提出，必须满足）

1. **高度随行高动态变化**：竖直点线的两个端点必须取当前行的**完整上下边界**（`Top = 0`、`Bottom = H`），`H` 用运行时实际行高。行高从 26 改为 40 时，点线长度必须同步变化，不得写死像素。
2. **相邻行首尾相接、视觉连续**：绘制时点线的**上下 Padding / Margin 一律为 0**；不得因单元格内边距、行分隔线留白或裁剪矩形收缩，导致上下相邻行的竖线出现断口。
3. **DPI 缩放对齐**：在 RenderScaling / `DPIAwareBox` 缩放下需对齐半像素，避免 1px 虚线被缩放后变模糊或与相邻行错位。

### A.2 折叠语义（WPF `levelCell_CheckChanged`，`BeginningBalances.xaml.cs` 行 1214–1254）

- **折叠**：从被点击行 `r+1` 起向下，凡科目编码以 `r` 的编码为前缀的行全部隐藏；遇到第一个前缀不匹配的行停止。
- **展开**：`r+1` 起向下，前缀匹配的行中——
  - 级次 `== grade+1` 的直接子行：显示，并取其自身展开态作为 `bshow`；
  - 级次 `> grade+1` 的更深行：仅当 `bshow == 展开` 才显示。

即「父展开只恢复到各子组自身折叠状态」，**嵌套状态必须保留**。

### A.3 建议 API

```csharp
public class TreeGradeCell
{
    /// <summary>树单元格显示的文本（科目编码或名称）。</summary>
    public string Text { get; set; }

    /// <summary>级次，1 起（对应 WPF grade）。</summary>
    public int Level { get; set; } = 1;

    /// <summary>末级节点：不画 +/- 方块，只画连接线。</summary>
    public bool IsLeaf { get; set; }

    /// <summary>本层最后一行：祖先层竖线只画上半段。</summary>
    public bool IsLastNode { get; set; }

    /// <summary>是否已展开。请勿沿用 WPF 中反直觉的 isChecked 命名。</summary>
    public bool IsExpanded { get; set; } = true;

    /// <summary>行背景（父级 #D9FFF5 / 末级 #B9F5D8，由宿主设置）。</summary>
    public IBrush BackColor { get; set; }
}

// Spreadsheet / Table 上暴露，与 LinkCells 一致（key = (列, 行)）
public Dictionary<(int, int), TreeGradeCell> TreeCells { get; }

// 点击 +/- 方块时抛出
public event EventHandler<TreeCellExpandedEventArgs> TreeExpand;
// TreeCellExpandedEventArgs：(int Column, int Row, TreeGradeCell Cell, bool IsExpanded)
```

**两种工作模式（请都实现，默认自动）**：

- **自动模式**：控件按 A.2 的级次规则自动计算隐藏行；
- **回调模式**（`AutoManageRows = false`）：只抛 `TreeExpand` 事件，由宿主调用 `SetRowVisible(row, visible)` 自行控制——宿主可能用「科目编码前缀」而非级次判断（WPF 实际逻辑就是按编码前缀）。

### A.4 行可见性基础设施（核心工作量）

现状：`Table.GetVisibleLineSegments` / `GetHeight(row)` / 行号累加全部假设行连续可见。引入隐藏行后需同步改造，请实现者**全局排查所有按行索引遍历 / 定位的代码**：

- 渲染循环（可见性判定、y 坐标累加、行分隔线绘制）
- 命中测试（PointerPressed 定位行、LinkCell 命中、方块命中）
- 选区（Shift 范围选、Ctrl+A、拖拽框选、行头/列头选择）
- 键盘导航（↑↓ / PgUp / PgDn / Enter / Tab 换行）——**必须跳过隐藏行**
- 滚动条最大值、滚轮、滚动定位
- 编辑框 y 坐标定位
- 复制 / 粘贴 / 填充柄（隐藏行不参与）
- 查找替换、`XlsxExporter` 导出（建议按 Excel outline 写入折叠层级，或提供 `ExportHiddenRows` 开关，默认不导出隐藏行）

建议内部实现：`HashSet<int> HiddenRows` + `NextVisibleRow(int row, int dir)` + 「行索引 → y 偏移」缓存，避免每行 O(n) 扫描导致卡顿（期初余额可达数千行）。

### A.5 自动折叠算法（与 A.2 等价）

```
Collapse(row):
  level = TreeCells[row].Level
  for r = row+1 .. maxRow:
     if TreeCells[r].Level <= level: break
     HideRow(r)

Expand(row):
  level = TreeCells[row].Level
  childExpanded = true
  for r = row+1 .. maxRow:
     l = TreeCells[r].Level
     if l <= level: break
     if l == level+1:
        ShowRow(r)
        childExpanded = TreeCells[r].IsExpanded    // 子组自身的展开态
     else:
        if childExpanded: ShowRow(r)
```

### A.6 交互细节

- 命中区 `Rect(x, (H-11)/2, 11, 11)`；命中时鼠标指针设为 Hand（可复用 `Spreadalonia.Cursors`）。
- 点击方块**不进入单元格编辑**（与 `LinkCell` 行为一致）。
- 折叠后垂直表头行号连续编号（隐藏行的行号不显示），滚动条范围同步缩短。
- 选中行按 Space / Enter 不触发折叠，避免与编辑冲突。
- DPI 缩放下坐标需换算，不能糊。

### A.7 视觉验收标准

1. 第 0 列左侧出现 11×11 方框，展开显示 `−`、折叠显示 `+`；
2. 每深一级整体右移 15px（4 + 11）；
3. 祖先层竖直点线**从行顶贯穿到行底**，与上下相邻行首尾相接、无断口；本层最后一个节点的点线只画到中线；
4. 行高从 26 改为 40 后，点线高度同步变化且仍然连续；
5. 末级节点无方框，只有一条水平点线连到父层竖线；
6. 父级行背景 `#D9FFF5`、末级行 `#B9F5D8`（由宿主设置 `BackColor`，控件负责整行填充）；
7. 文本左对齐、垂直居中。

---

## 需求 B：NumericCell（数值单元格 + 只允许输入数值）

### B.1 WPF 参考实现

类 `NumericCell : CellBody`，`ControlClass.cs` 行 237–318；用法见 `BeginningBalances.xaml.cs` 行 361–393：`sheet[row, col] = new NumericCell(digits)`，`digits` 取科目 `digitdecimal` / 币别 `currencydecimal` / 数量 2 位。

- `OnPaint`：`Math.Floor(d*10^n + 0.5)/10^n` 四舍五入，格式 `#,#0.00…`（负数段 `;-#,#0.00…`）；值为 0 显示**空串**；可选红色 2px 删除线。
- `OnSetData`：用 `decimal.Parse` 校验，**非法值返回旧值（拒绝写入）**；值为 0 写入 `null`。即「提交时校验、非法回退」，而非逐字符过滤。

### B.2 需求规格

**显示**：固定小数位 + 千分位、右对齐；0 或空显示为空（`ZeroAsEmpty`，默认 `true`）；可选 `Strikethrough`（红色删除线，WPF 用于已记账/被冲销数据）；可选 `NegativeRed`（WPF 未做，建议新增）。

**编辑（必须项）**：

1. 进入编辑时，编辑框内容必须是**未格式化的原始数值文本**（即 `Data` 中存的 `1234.5`，而不是 `1,234.50`），否则用户无法编辑——这是硬性要求；
2. 提交时（LostFocus / Enter / Tab 确认）按 `Digits` 校验：
   - 空串 → 清除（等价 0），允许；
   - 可解析为 `decimal`（允许负号；允许输入千分位逗号，解析前 Strip）→ 按 `Digits` 四舍五入后存**规范化数值文本**（如 `1234.57`），显示时再格式化；
   - 非法（含字母、多个小数点、形如 `--`、超出 `decimal` 范围）→ **拒绝提交**：保留原值、不写入 `Data`、不进 Undo 栈，并抛 `InvalidInput` 事件（携带 Column / Row / 输入文本 / 原因）。**不要静默清空**；
3. 可选 `FilterWhileTyping`（默认 `true`）：编辑框输入期只允许 `[0-9.\-]`，限制最多一个小数点、负号仅允许在开头、小数位上限；关闭时由第 2 条的提交校验兜底。

```csharp
public class NumericCell
{
    public int Digits { get; set; } = 2;
    public bool ZeroAsEmpty { get; set; } = true;
    public bool Strikethrough { get; set; }
    public bool AllowNegative { get; set; } = true;
    public bool NegativeRed { get; set; }
    public bool FilterWhileTyping { get; set; } = true;

    public string GetDisplayText(string rawData);
    public bool TryValidate(string input, out string normalized, out string error);
}

// 与 LinkCells 一致（key = (列, 行)）
public Dictionary<(int, int), NumericCell> NumericCells { get; }

// 非法输入被拒绝时抛出
public event EventHandler<InvalidInputEventArgs> InvalidInput;
// InvalidInputEventArgs：(int Column, int Row, string InputText, string Reason)
```

**其它要求**：

- 渲染时数值单元格优先按 `NumericCell` 格式化显示（覆盖普通文本渲染）。
- `NumericLinkCell` 的格式化逻辑与 `NumericCell` **必须共用同一套内部实现**（`#,#0.000`、0→空、四舍五入），避免两套实现漂移。

### B.3 验收标准

1. 数值列显示带千分位、固定小数位，0 显示为空；
2. 双击进入编辑，编辑框显示原始数字（无千分位）；
3. 输入 `abc` / `1.2.3` / `-` 后确认：单元格**保持原值**，并抛 `InvalidInput` 事件；
4. 输入 `1234.567`（`Digits = 2`）→ 显示 `1,234.57`，实际存 `1234.57`；
5. 输入 `-500` → 显示 `-500.00`；
6. 清空后确认 → 单元格变空，`Data` 中移除该键。

---

## 需求 C1：单元格 / 区域只读

- **新增能力**：`SetRangeReadonly(SelectionRange range, bool readOnly)`（沿用现有 `SelectionRange` 类型），或暴露 `Spreadsheet.ReadonlyRanges`；同时提供 `bool IsCellReadonly(int column, int row)` 便于宿主判断。
- **WPF 对照**：`sheet.Ranges[row, col, rows, cols].IsReadonly = true`（`BeginningBalances.xaml.cs` 行 270、305、356 等处）。
- **用途**：非末级科目行禁止直接录入金额（金额只能由末级汇总生成）；年初余额、年初数量等派生列禁止编辑。
- **行为**：只读单元格**不可进入编辑**、粘贴时跳过、Delete 无效；但**仍可选中和复制**。

---

## 需求 C3：单元格编辑结束事件

- **新增事件**：

```csharp
public event EventHandler<CellEditEndedEventArgs> CellEditEnded;
// CellEditEndedEventArgs：(int Column, int Row, string OldText, string NewText, bool Cancelled)
```

- **触发时机**：编辑框提交后统一抛出（LostFocus / Enter / Tab 确认，或 Esc 取消）；Esc 时 `Cancelled = true`。
- **用途（关键）**：期初余额需要在用户输入后立即联动计算——
  - 输入借方自动算贷方（反之亦然）；
  - 外币金额 × 汇率 → 本位币；
  - 数量 × 单价 → 金额；
  - 末级行汇总到父级行；
  - 试算平衡表实时刷新。

  宿主必须能在该回调中读取新值并**改写其它单元格**。
- **要求**：事件在 `Data` 已写入之后触发；允许宿主在回调内调用 `SetData` 修改其它单元格（不得重入崩溃、不得重置选区与滚动位置）。

---

## 版本与交付建议

| 版本 | 内容 |
|---|---|
| **v1.5.0（最小可用集）** | A（树形 + 行可见性）、B（数值单元格）、C1（只读）、C3（编辑结束事件） |
| **v1.5.1** | 冻结行列（WPF `sheet.FreezeToCell(2, 4)`，对应期初余额工具栏"锁定列"开关）、导出 / 打印对隐藏行的处理、树形键盘操作（← → 折叠 / 展开） |

**其它交付要求**：

1. 请在 `Spreadalonia/Demo` 增加演示页：3 级科目约 200 行，第 0 列树形，末级行金额列用 `NumericCell`（`Digits` = 2 / 4），用于验证——折叠展开、行高变化时点线连续、非法输入拒绝、`CellEditEnded`、只读列。
2. NuGet 包 `Spreadalonia.Carfield2003` 升至 `1.5.x`，目标框架不变；**API 只增不改，向后兼容**。
3. 我方（SSFinance）拿到新包后会在 `Directory.Packages.props` 提升版本并接入验收，发现问题会按本文档条款反馈。
