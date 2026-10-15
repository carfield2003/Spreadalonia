# Spreadalonia 增强批次验收记录 v1.6.0 – v1.7.5
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
| 1.7.2 | 数值单元格省略号截断（NumericCell / NumericLinkCell） | P1 | 已完成并验证 |
| 1.7.3 | 合并单元格跨冻结边界重复渲染修复 | P0 | 已完成并验证 |
| 1.7.4 | 跨冻结边界合并单元格整体居中 + 冻结态表头拖拽改尺寸修复 | P0 | 已完成并验证 |
| 1.7.5 | 固定尺寸表格（MaxTableWidth/MaxTableHeight）最后一列/行无法调整尺寸修复 | P0 | 已完成并验证 |

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

## 六、数值省略号截断（v1.7.2）

参照 SSFinance（WPF）：格式化后的数值显示不下（列宽不足）时，右侧显示省略号
（如 `1,120,7…`、`104,500…`），而不是把字符裁掉一半。

### 行为
- `NumericCell`（Table 主渲染路径）：格式化文本宽度超过单元格可用宽度时，
  二分查找能放下的最长前缀，尾部追加 "…" 重绘；放不下省略号本身时维持原裁剪。
- `NumericLinkCell`（`LinkCell.Render`）：`LinkCell` 新增虚属性
  `UseEllipsisWhenClipped`（默认 `false`，普通链接保持原裁剪行为），
  `NumericLinkCell` 覆写为 `true`。
- 共享实现：`Spreadalonia/TextTruncation.cs`（internal，测量复用单元格字体/字号）。
- 右对齐随截断后的宽度重新计算；删除线（Strikethrough）、负数红色等修饰跟随。
- 值能完整显示时行为完全不变（如 `9,025.00`）。

### 验收
- 新增 `--numeric-ellipsis-test`：两列 64px 窄列（NumericCell / NumericLinkCell），
  大值显示 `1,120,7…` / `282,04…`，小值 `9,025.00` 完整，负值红色正常；
  其余四组截图回归全部通过。

---

## 七、合并单元格跨冻结边界重复渲染修复（v1.7.3）

冻结前两列、第一行标题合并（如 `A1:D1` 显示"试算平衡表"）时，标题会在冻结区分隔线两侧
各绘制一次，调整列宽时两份文本还会各自错位漂移。

### 根因
- `Table.Render` 按四象限（滚动区 / 冻结行区 / 冻结列区 / 冻结角区）分别调用 `RenderRegion`，
  每个区域都绘制与自身范围相交的合并单元格。
- 跨冻结边界的合并单元格同时与冻结区、滚动区相交 → 文本被绘制两份；列宽变化时
  两区域按各自偏移重算，导致错位漂移。

### 行为（修复后）
- `RenderRegion` 新增 `frozenColumns` / `frozenRows` 标志；绘制合并单元格前做归属判断：
  只有在冻结列一侧起始的合并区（`Left < FrozenColumnCount`）才由冻结列区域绘制，
  滚动区不再绘制（内部网格线依旧被 `GetVisibleLineSegments` 跳过，与 Excel 一致）。
- 冻结行一侧同理（`Top < FrozenRowCount`）。
- 不冻结、或合并区不跨边界时行为完全不变（两个判断仅在冻结条带存在且跨界时生效）。

### 验收
- 新增 `--freeze-merge-test`：`Data.Clear()` 后构建"试算平衡表"（`A1:D1` 合并，冻结 2 列），
  三张截图分别覆盖初始状态、加宽冻结列 B、加宽滚动列 C，标题均只绘制一次且无漂移。
- `--freeze-panes-test`、`--hidden-column-test` 回归通过。

---

## 八、跨冻结边界合并单元格整体居中 + 冻结态表头拖拽修复（v1.7.4）

v1.7.3 消除了重复绘制，但标题文本不再跨冻结分隔线居中（各区域按自身可见片段居中），
且冻结后最右侧列无法拖拽调整宽度。

### 根因
- `RenderRegion` 用「本区域与合并区的交集」`visibleMerge` 作为居中矩形，合并区只有一部分
  落入该区域时，文本按可见片段居中而非按整个合并区居中。
- 合并区右/下边界越出当前绘制范围时，累加列宽从 `left + width` 起算，
  而 `xs[width]` 已包含该列，重复计入一列（差一错误）。
- `HorizontalHeader` / `VerticalHeader` 拖拽时：
  1. 位于冻结条带的位置仍减去滚动偏移（`lastDrawnDelta`），导致坐标错位；
  2. 用 `lastDrawnXs[clickedColumn - 1]` 取左边缘，隐含「绘制切片从第 0 列开始」，
     冻结后切片从 `FrozenColumnCount` 开始，索引整体偏移 → 抓不到最右列的右边缘。

### 行为（修复后）
- 合并单元格统一按**整个合并区**的几何位置计算矩形：`mergeX0/mergeX1`、`mergeY0/mergeY1`
  由 `xs/ys` 与 `GetWidth/GetHeight` 推导出完整范围（含越出当前区域的部分），
  各区域只负责绘制落在自己裁剪范围内的那部分，文本连续且居中，`TitleCell`/`LinkCell`/普通文本一致。
- 表头拖拽：冻结条带使用表格坐标（`IsOverFrozenColumns` / `GetFrozenColumnIndex` / `GetColumnStart`，
  行方向同理），滚动切片按 `lastDrawnLeft` / `lastDrawnTop` 修正索引偏移。

### 验收
- Demo 新增「Freeze + Merged Title Demo」按钮，与 `--freeze-merge-test` 共用同一方法
  `LoadFreezeMergeDemo()`，可交互验证：标题跨冻结线居中且只有一份，可拖动任意列分隔线（含最右列）。
- `--freeze-merge-test`、`--freeze-panes-test`、`--hidden-column-test`、`--tree-cell-test`、
  `--numeric-ellipsis-test` 回归通过。

---

## 九、固定尺寸表格最后一列/行无法调整尺寸（v1.7.5）

实际业务页（如辅助核算）会把 `MaxTableWidth` / `MaxTableHeight` 设成数据的列数/行数
（例如 8 列 × 13 行）。此时表格末端就在视口内部，最后一列的列宽、最后一行的行高完全拖不动。

### 根因
- `MaxTableWidth` / `MaxTableHeight` 是**列数/行数上限**：`GetRangeX` / `GetRangeY` 用它截断绘制
  循环，因此最后一条绘制线就是表格终点，其外侧不再存在「下一列/下一行」。
- 命中的段判定为 `xs[i] > xPos`（指针必须严格落在某个列段内）。指针落在最后一条线上或其外侧时
  `clickedColumn = -1`（行方向 `clickedRow = -1`），而 `OnPointerMoved` 有
  `if (clickedColumn >= 0)` / `if (clickedRow >= 0)` 的门 → 拖拽被整体忽略；`OnPointerReleased`
  只会把宽度写进 `ColumnWidths[-1]` / `RowHeights[-1]`，没有任何可见效果。
- Demo 复现不了：`LoadFreezeMergeDemo()` 把 `MaxTableWidth/Height` 放开到 `int.MaxValue`，
  最后一条线外侧永远是空白列/行，从右侧抓边缘会命中「下一列的左边缘」并回退到前一可见列。

### 修复
- `HorizontalHeader.ResolveColumnHit`：位于/越过最后一条绘制线时，一律钳制到**最后绘制列**
  （原先只在 `closest <= 5` 时兜底），点击表格外侧的空白区也选中最后一列而不是 `-1`。
- `VerticalHeader` 抽出与列方向对称的 `ResolveRowHit` / `ResolveRowPress`（原先是两处重复代码），
  加同样的钳制，并沿用 `PreviousVisibleRow` 跳过隐藏行。
- 两个表头的 `OnPointerMoved`：正在进行的改尺寸拖拽不再依赖命中判定（指针越过表格末端也继续跟随）；
  但**仍以按钮状态判定是否为拖拽**（`pointerPressedAction == 2 && IsLeftButtonPressed`），
  `OnPointerReleased` 末尾复位 `pointerPressedAction = 0`，避免悬停即改尺寸。
- `Spreadsheet` 暴露 `VerticalHeaderControlInternal`（内部，仅测试用）。

### 验收
- Demo 新增 `--fixed-size-resize-test`：按业务页方式设 8 列 × 13 行（A:B 冻结），
  在最后一条列线/行线的 ±8 / ±3 / 0 / +30 处重放命中逻辑，并对照重放 v1.7.4 的旧逻辑。
  修复后所有位置都命中最后列（7）/最后行（12），其中 ±5px 内进入改尺寸；
  旧逻辑在线上及其外侧一律得到 `-1`（拖拽无效），只有从线左侧抓才有效。
- `--freeze-merge-test`、`--freeze-panes-test`、`--hidden-column-test`、`--tree-cell-test`、
  `--numeric-ellipsis-test` 回归通过。

---

## 回归清单（每批次执行）

- `dotnet build Spreadalonia.sln` 过滤 `error CS` 无输出，`read_lints` 干净。
- Demo 截图冒烟：`--tree-cell-test`、`--hidden-column-test`、`--freeze-panes-test`、`--tree-animation-test`、`--numeric-ellipsis-test`、`--freeze-merge-test`。
- 既有 API（`HiddenRows`、`SetRowVisible`、`TitleCell`、`LinkCell`、`NumericCell`）行为不变。
