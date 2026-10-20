# Spreadalonia 多工作表（Sheet）功能需求与实施计划（v1.8.0 / v1.9.0）

> 需求来源：`C:\Users\89664\source\repos\carfield2003\SCloud\SSFinance\Docs\Spreadalonia多Sheet功能需求文档.md`
> 目标：让 SSFinance Avalonia 版 `ImportData` 页用 Spreadalonia 替掉临时 DataGrid 方案
> 整理日期：2026-10-19

---

## 1. 现状（已在 Spreadalonia 源码中核实）

| 事实 | 证据 | 影响 |
| --- | --- | --- |
| `Table : Control` 持有全部数据与格式状态 | `Table.cs:133-231`（`Data`、`RowHeights`、`ColumnWidths`、`CellTypefaces`、`CellForeground`、`HiddenRows`、`HiddenColumns`、`ReadonlyRanges`…） | 状态外置时这些字段都要改为「当前 Sheet 的转发属性」 |
| 合并区域是 `Table` 上的 StyledProperty | `Table.cs:339-350` `MergedRangesProperty` | 切表时需要写回/读取 |
| 冻结行列是 `Table` 的私有字段 | `Table.cs:1097-1157` | 同上 |
| 滚动偏移是 `Table.Offset`（StyledProperty，带动画） | `Table.cs:58-63`、`Spreadsheet.axaml.cs:1239-1295` 同步 4 个辅助控件 | 每表保存 `ScrollOffset`，切表时恢复并暂停动画 |
| **撤销/重做是 `Spreadsheet` 的成员，共 12 个并行栈** | `Spreadsheet.axaml.cs:1160-1185`；记录类型 `StackFrame` / `ValueStackFrame` 在 `SelectionRange.cs:255-297`；`Table`/`VerticalHeader` 到处 `Container.UndoStackXxx.Push(...)` | 按表隔离需整组保存/恢复，成本高 |
| 选择与公式引擎也在 `Spreadsheet` 层 | `Selection`（StyledProperty，`Spreadsheet.axaml.cs:301`）、`FormulaEngine`（`:156`） | 切表需切换引用 |
| 无打印 / PDF / SVG 导出 API | 全库检索无相关公开成员 | 「多 Sheet 打印」不在本期范围 |
| 导出入口 `XlsxExporter.Export(Table, stream, options)`，依赖 ClosedXML | `XlsxExporter.cs:177/202` | 多表导出需改为按 Sheet 逐个写入 |
| 相关类型均为 `public` | `SelectionRange`、`CellBackground`、`CellBorder`、`FormulaEngine`、`CellData`、`TitleCell`、`LinkCell`、`ButtonCell`、`TreeGradeCell`、`NumericCell` | 新建的 `SpreadsheetSheet` 可以安全公开这些类型 |

---

## 2. 已确认的关键决策

| # | 议题 | 决策 |
| --- | --- | --- |
| 1 | 状态架构 | **状态外置**：新增 `SpreadsheetSheet` 持有全部状态，`Table` 成为「渲染当前 Sheet 的引擎」，现有 `Table.Data` 等成员保留为转发属性（最小 diff） |
| 2 | 撤销栈 | **切表即清空** Undo/Redo 栈（不跨表回滚） |
| 3 | 发版节奏 | **分两批**：1.8.0 = 模型+状态隔离+兼容+页签栏+管理 API+索引对齐；1.9.0 = xlsx 导入、多表导出、每表开关 |
| 4 | 隐藏 Sheet | **建表但默认不显示页签**：`Index` 仍与源文件 1:1，提供 `IsHidden` 与 `SheetTabHiddenSheetsVisible` 开关，代码可切到隐藏表 |

### 其余默认处理（需求文档与源码不一致处，按源码修正）

- 坐标键统一为 **(column, row)**（需求文档写作 Row/Column，实为 column 在前）。
- `CellBackgrounds` / `CellBorders` 是 `List<T>` 而非字典；`NumericCells` 是 `Dictionary<(int,int), NumericCell>`；`CellMargin` 是 `Dictionary<(int,int), Thickness>`；`Selection` 是 `ImmutableList<SelectionRange>`。
- 打印不在范围；跨表公式（`Sheet2!A1`）本期不支持。
- 控件未初始化时的 Sheet 调用**延迟生效**（现有 `IsInitialized=false` 静默忽略是坑）。
- 页签栏用 Avalonia 原生控件组合（`ScrollViewer` + `ItemsControl` + `Button`），便于适配 Semi/Fluent 与深色主题；位置在最底部，水平滚动条移到其上方。
- 现有事件参数只**增加可空 `Sheet` 属性**，不改签名、不新增事件。
- `IsReadOnly` 只拦 UI 编辑/粘贴/清除，程序化写入不受限。
- 导入先做同步 + `MaxRowsPerSheet` 截断，不引入后台线程。

### 待确认（实现中暴露的新问题）

- **公式引擎隔离 vs 自定义函数注册**：现状 `Spreadsheet.FormulaEngine` 是**全局单例**，宿主在启动时注册的自定义函数只对它生效；R4 要求每表独立（否则依赖图/缓存互相污染、误算）。
  **拟采用**：每表一个 `FormulaEngine`，但**共享同一个 `FunctionRegistry`**（改造 `FormulaEngine` 允许注入外部 registry，或 `AddSheet` 时复制已注册函数）—— 这样 `Spreadsheet.FormulaEngine.Functions` 的注册对所有表保持有效，不破坏 SSFinance 现有代码。

---

## 3. 任务清单

### 阶段 1 —— 工作表模型 + 状态隔离 + 兼容（R1 / R4 / R12）

- [x] 1.1 新增 `Spreadalonia/SpreadsheetSheet.cs`（数据/格式/几何/视图状态/自定义单元格/`Index`/`IsHidden`/`Tag`/`IsReadOnly`）
- [x] 1.2 `Table` 增加 `SpreadsheetSheet Sheet`，把 133-231 的字段改为转发属性（`Data` 等仍返回同一字典引用，`this.Data.Move(…)` 类调用无需改动）
- [x] 1.3 处理 `MergedRanges` / `FrozenRowCount` / `FrozenColumnCount` / `MaxTableWidth` / `MaxTableHeight` / `AutoManageRows` 的按表存取
- [x] 1.4 `Spreadsheet` 新增 `Sheets` / `CurrentSheet` / `IndexOfSheet` / `CurrentSheetIndex`（StyledProperty，TwoWay）/ `CurrentSheetChanged`
- [x] 1.5 切表同步清单：`Table.Offset`、4 个辅助控件 `Owner`、`Selection`、`FormulaEngine`、编辑框状态、撤销栈清空、重绘
- [x] 1.6 兼容：隐式单表 `Sheet1`，现有成员全部作用于 `CurrentSheet`；未初始化调用延迟生效
- [x] 1.7 验收：`dotnet build` 0 错误；Demo `--sheet-state-test`（A/B 表列宽/冻结/隐藏/合并/选择/滚动互不影响且完全恢复）已 PASS；既有 `--tree-cell-test`、`--freeze-panes-test`、`--hidden-column-test`、`--link-cell-test`、`--numeric-link-empty-test`、`--numeric-ellipsis-test` 回归

### 阶段 2 —— 页签栏（R2 / R3 / R8 / R9）

- [x] 2.1 `SheetTabStrip` 控件 + 底部布局（单表且 `SheetTabVisibleWhenSingle=False` 时零占位：`IsVisible=false` 且 `Height=0`）
- [x] 2.2 点击切换、横向滚动（滚轮 + 左右按钮）、选中态/Hover、长名省略 + Tooltip
- [x] 2.3 键盘 `Ctrl+PageUp/PageDown`（`MoveToNextSheet` / `MoveToPreviousSheet`）；`CurrentSheetIndex` 双向绑定验证
- [x] 2.4 开关：`SheetTabVisible` / `SheetTabVisibleWhenSingle` / `SheetTabNewButtonVisible` / `SheetTabAllowRename` / `SheetTabAllowDelete` / `SheetTabAllowInsert` / `SheetTabAllowReorder` / `SheetTabHiddenSheetsVisible`
- [x] 2.5 样式属性（`SheetTabHeight` / `SheetTabBackground` / `SheetTabForeground` / `SheetTabSelectedBackground` / `SheetTabBorderBrush`）+ 深色主题（页签用原生 `Button`，未显式设置画刷时跟随主题）
- [x] 2.6 验收：Demo `--sheet-tab-test`

### 阶段 3 —— 管理 API 与索引对齐（R5 / R6）

- [x] 3.1 `AddSheet` / `InsertSheet` / `RemoveSheet` / `RemoveSheetAt` / `RenameSheet` / `MoveSheet` + 4 个事件（`SheetInserted` / `SheetRemoved` / `SheetRenamed` / `SheetMoved`）
- [x] 3.2 指定表版本：`SetData(sheet, …)` / `SetWidth(sheet, …)` / `SetHeight(sheet, …)` / `InsertColumns(sheet, index, count)` / `DeleteColumns(sheet, index, count)`（错误标注列）
- [x] 3.3 `Index` / `IsHidden` / `Tag` 与源文件 1:1（含隐藏表）
- [x] 3.4 验收：Demo `--sheet-api-test`

### 阶段 4 —— xlsx 导入 / 多表导出（R7 / R11，1.9.0）

- [x] 4.1 `XlsxImporter` + `XlsxImportOptions`
- [x] 4.2 `LoadXlsx(Stream)` / `LoadXlsx(path)`
- [x] 4.3 `SaveXlsx(stream, sheets, options)`
- [x] 4.4 验收：Demo `--sheet-xlsx-test`（3 表含隐藏表 round trip：顺序/名称/可见性/索引、首个非隐藏表为当前表、公式重算、列宽、二次导出均 PASS）

### 阶段 5 —— 每表交互开关（R10，1.9.0）

- [x] 5.1 `IsReadOnly`（只拦 UI 编辑/粘贴/清除，程序化写入不受限）/ `AllowTextOverflow`（默认 false）/ `AllowDragMoveCells`（默认 true）/ `SelectionStyle`（Fill 默认 / FocusRect，含表头联动）；验收 Demo `--sheet-options-test` PASS

### 阶段 6 —— 文档与发版

- [ ] 6.1 `docs/` 与 `Readme.md` 同步；XML 文档注释
- [ ] 6.2 Demo 多 Sheet 示例页（导入 → 切表 → 读当前表）
- [ ] 6.3 版本号 + `PackageReleaseNotes` → tag → Actions → NuGet

---

## 4. 验收清单（对应需求文档第 6 节）

| 需求文档条目 | 1.8.0 是否覆盖 |
| --- | --- |
| 1-3 工作表模型 / 增删改名移 | ✅ 阶段 1 + 3 |
| 4-8 页签栏交互与键盘 | ✅ 阶段 2 |
| 9-11 切换通知与双向绑定 | ✅ 阶段 1 + 2 |
| 12-14 每表状态 / 撤销隔离 / 公式隔离 | ✅ 阶段 1（撤销按「切表清空」、公式按表隔离） |
| 15-16 索引与源文件对齐（含隐藏表） | ✅ 阶段 3 |
| 17-19 xlsx 导入 | ✅ 阶段 4（`LoadXlsx`，索引含隐藏表 1:1） |
| 20-22 页签开关与样式 | ✅ 阶段 2 |
| 23 每表交互开关 | ✅ 阶段 5（`IsReadOnly` / `AllowTextOverflow` / `AllowDragMoveCells` / `SelectionStyle`） |
| 24 多表导出 | ✅ 阶段 4（`SaveXlsx(stream, sheets, options)`）；打印不在范围 |
| 25-27 跨平台 / netstandard2.0 / 文档与 Demo | ✅ 阶段 6 |
