# Spreadalonia 增强批次验收记录 v1.6.0 – v1.7.1
## 目标：补齐 SSFinance 从 WPF（ReoGrid）迁移到 Avalonia（Spreadalonia）的四项能力

> 本文档记录本批次（列隐藏、辅助项选择单元格、冻结行列、树形展开/折叠动画）的需求、API 与验收结果。
> API 只增不改，向后兼容；全部改动以 Demo 截图冒烟验证。

---

## 批次总览

| 版本 | 功能 | 优先级 | 状态 |
|---|---|---|---|
| 1.6.0 | 列隐藏（真正的隐藏机制） | P0 | 已完成并验证 |
| 1.6.0 | 辅助项选择单元格（ButtonCell + CellTapped/CellDoubleTapped） | P1 | 已完成并验证 |
| 1.6.1 | 冻结行列（FreezePanes） | P1 | 已完成并验证 |
| 1.7.0 | TreeGradeCell 展开/折叠动画 | P2 | 已完成并验证 |
| 1.7.1 | ButtonCell 图标模式（RefCell 风格） | P1 | 已完成并验证 |

---

## 一、列隐藏（v1.6.0）

### API（与既有行隐藏命名对齐）
- `Spreadsheet.HiddenColumns` / `Table.HiddenColumns`：`HashSet<int>`，键为列索引（不重排）。
- `IsColumnHidden(int)` / `IsColumnVisible(int)` / `SetColumnVisible(int, bool)` /
  `SetColumnsVisible(IEnumerable<int>, bool)` / `ShowAllColumns()` / `NextVisibleColumn(int, int)`。
- 别名：`HideColumn` / `ShowColumn`、`HideRow` / `ShowRow`。

### 行为
- 隐藏列宽度返回 0：数据、列标头字母、边框线全部不可见，相邻列无缝拼接。
- `(column, row)` 字典键语义不变，数据绑定刷新后稳定。
- 渲染、表头、`GetRangeX`/`GetCell`、键盘导航、复制/清除链路全部跳过隐藏列。
- 修复 `GetRangeX`/`GetCell` 缺失的 `MaxTableWidth` 守卫（0 宽列会导致死循环）。
- `XlsxExporter` 将隐藏列写为 Excel 隐藏列并跳过内容，`ExportHiddenColumns` 默认 `false`。

### 验收
- `--hidden-column-test`：全部列 / 隐藏 2–5 列两张截图，隐藏列（含表头字母）完全不可见，相邻列无缝。

---

## 二、辅助项选择单元格（v1.6.0）

### API
- 新增 `Spreadalonia/ButtonCell.cs`：`Text`、`Tag`、`CellType`、`Background`、`Foreground`、
  `ShowBorder`、`SuppressEdit`（默认 `true`）、内部 `HitRect`（渲染时写回）。
- 挂载：`Spreadsheet.ButtonCells[(column, row)]`，与 `TitleCell`/`LinkCell` 平级的字典范式。
- 事件：`Spreadsheet.CellTapped` / `CellDoubleTapped`，参数 `CellTappedEventArgs`
  携带 `Column`、`Row`、`CellType`、`Tag`。
- 命中优先于默认文本编辑：点击按钮单元格不进入编辑态，直接抛事件。

### 行为
- 下划线 + 手型光标样式，可自定义前景/背景/边框；选择结果回写后显示值正常刷新。

### 验收
- Demo「HiddenColumn / ButtonCell Demo」页：点击按钮单元格触发 `[CellTapped]`，
  不进入编辑；回写数据后正常显示。

---

## 三、冻结行列（v1.6.1）

### API
- `Spreadsheet.FrozenRowCount` / `FrozenColumnCount`（get/set）。
- `FreezePanes(int rows, int columns)` / `FreezeRows(int)` / `FreezeColumns(int)` / `UnfreezePanes()`。

### 行为
- 仅冻结行、仅冻结列、同时冻结均支持；冻结区滚动时停留顶部/左侧。
- `Table.Render` 分四象限绘制，各自 `PushClip`，`lastDrawnXs/lastDrawnYs` 拼接供命中测试。
- 表头（`HorizontalHeader`/`VerticalHeader`）分段绘制冻结段。
- `SetScrollbarMaximum` 扣减冻结区尺寸；滚动进入视野（scroll-into-view）停在冻结区边界。
- 冻结区与非冻结区之间绘制较粗分隔线。
- 不影响选择、复制、导出、打印；叠加高亮（Overlay）按冻结边界对齐。
- 新增 `GetSelectionColumnEdge` / `GetSelectionRowEdge`：冻结行列边缘取自冻结区坐标原点。

### 验收
- `--freeze-panes-test`：冻结首行 + 前 3 列 → 选中远处单元格滚动 → 冻结区纹丝不动；
  仅冻结列、仅冻结行两种模式均通过。

---

## 四、TreeGradeCell 展开/折叠动画（v1.7.0）

### API
- `Spreadsheet.IsTreeAnimationEnabled` / `Table.IsTreeAnimationEnabled`（默认 `true`）。
- `Table.TreeAnimationDuration`（默认 200ms）、`Table.TreeAnimationMaxRows`（默认 1000）。

### 行为
- 展开：子行高度由 0 平滑增长到实际高度并淡入；折叠：平滑收缩到 0 并淡出。
- 实现方式：`DispatcherTimer`（16ms，Render 优先级）+ Stopwatch 计时，ease-out quad 插值；
  插值高度写入 `Table.GetHeight`（行高唯一来源），渲染、表头、命中测试、键盘导航自动跟随。
- 折叠行在完全收缩后才写入 `HiddenRows`，避免"先消失再动画"。
- 动画期间不触碰滚动偏移，视口保持稳定。
- 动画进行中再次展开/折叠：先将上一个动画瞬间置为终态，避免两个动画争抢同一批行。
- `ShowAllRows()` 时取消（而非完成）动画，避免把仍在收缩的行重新隐藏。
- 参与动画的行数超过 `TreeAnimationMaxRows` 时自动降级为无动画（直接应用终态）。

### 验收
- `--tree-animation-test`：
  - 折叠/展开动画中帧截图显示子行半高、半透明（淡入淡出生效）；
  - 动画结束帧与无动画终态一致；
  - 滚动到中部（第 42 行节点）折叠：折叠前后第 30–43 行位置逐像素不变（滚动稳定）。
- Demo 工具栏新增「Tree animation」复选框，可实时开关动画对比。

---

## 五、ButtonCell 图标模式（v1.7.1，RefCell 风格）

参照 WPF 项目 `SSFinanceWin/PublicClass/RefCell`（ReoGrid CellBody）：单元格文本正常显示，
右侧有一个小图标按钮（默认放大镜），悬停高亮，点击图标弹出选择对话框。

### API（`ButtonCell` 新增，全部可选，向后兼容）
- `Mode`：`ButtonCellMode.Button`（默认，整格按钮）/ `ButtonCellMode.Icon`（RefCell 风格）。
- `IconSize`：图标边长，默认 20（与 RefCell 一致）。
- `IconPath`：SVG path 数据，默认 RefCell 的放大镜路径。
- `IconFill` / `IconHoverFill`：图标颜色，默认 `#CCC` / 悬停 `#777`（与 RefCell 一致）。
- `IconHoverBackground`：悬停时图标底色，默认 `#E0E0E0`（与 RefCell 一致）。

### 行为
- 图标模式下单元格文本照常绘制（取自单元格数据，支持正常编辑，除非设 `SuppressEdit = true`），
  文本区域自动避开图标；只有图标矩形可点击（`HitRect` 仅覆盖图标）。
- 悬停图标时高亮（底色 + 图标变深），`Table` 通过 `OnPointerMoved` / `OnPointerExited`
  跟踪悬停状态并触发重绘。
- 点击图标在释放时抛 `CellTapped` / `CellDoubleTapped`；文本区域保持普通单元格行为
  （可选中、可编辑）。

### 验收
- `--hidden-column-test` 新增第三张截图（滚动到辅助项列）：每行右侧显示放大镜图标，
  与 WPF RefCell 截图观感一致；点击图标触发 `[CellTapped]` 回写数据。

---

## 回归清单（每批次执行）

- `dotnet build Spreadalonia.sln` 过滤 `error CS` 无输出，`read_lints` 干净。
- Demo 截图冒烟：`--tree-cell-test`、`--hidden-column-test`、`--freeze-panes-test`、`--tree-animation-test`。
- 既有 API（`HiddenRows`、`SetRowVisible`、`TitleCell`、`LinkCell`、`NumericCell`）行为不变。
