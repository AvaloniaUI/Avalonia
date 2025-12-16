# Calendar Control Memory Allocation Analysis

## Executive Summary

The Calendar control allocates approximately **2.65 MB** per instance creation. This analysis investigates whether the allocations are due to layout system issues or template/control structure issues.

**Conclusion: The allocations are primarily a TEMPLATE/CONTROL STRUCTURE issue, not a layout issue.**

---

## Benchmark Results

| Branch | CreateCalendar Allocations |
|--------|---------------------------|
| Master | 2,777.61 KB |
| Optimized (LayoutQueue) | 2,651.3 KB |

The LayoutQueue optimization reduced allocations by ~126 KB (4.5%), but ~2.65 MB remains. This indicates the majority of allocations come from control/template instantiation, not the layout system.

---

## Calendar Control Structure Analysis

### Control Hierarchy

```
Calendar
└── CalendarItem (TemplatedControl)
    ├── HeaderButton (Button)
    ├── PreviousButton (Button)
    ├── NextButton (Button)
    ├── MonthView (Grid) - 7x7 = 49 cells
    │   ├── 7x DayTitleTemplate (TextBlock) - Row 0
    │   └── 42x CalendarDayButton - Rows 1-6
    └── YearView (Grid) - 3x4 = 12 cells
        └── 12x CalendarButton
```

### Control Count Per Calendar Instance

| Control Type | Count | Notes |
|-------------|-------|-------|
| Calendar | 1 | Root control |
| CalendarItem | 1 | Main templated control |
| Button | 3 | Header, Previous, Next |
| Grid | 2 | MonthView, YearView |
| TextBlock | 7 | Day title headers (Mon-Sun) |
| **CalendarDayButton** | **42** | 6 weeks × 7 days |
| **CalendarButton** | **12** | 3 rows × 4 columns (year/decade view) |
| **Total Controls** | **~68** | Excluding template parts |

### CalendarDayButton Template Structure

Each CalendarDayButton instantiates (from [CalendarDayButton.xaml](src/Avalonia.Themes.Fluent/Controls/CalendarDayButton.xaml)):

```
CalendarDayButton (inherits Button → ContentControl → TemplatedControl)
└── Panel
    ├── Border#Root
    │   └── ContentPresenter#PART_ContentPresenter
    └── Border#Border
```

**Per CalendarDayButton: ~5-6 visual elements**

### CalendarButton Template Structure

Each CalendarButton instantiates (from [CalendarButton.xaml](src/Avalonia.Themes.Fluent/Controls/CalendarButton.xaml)):

```
CalendarButton (inherits Button → ContentControl → TemplatedControl)
└── Panel
    ├── Border#Root
    │   └── ContentPresenter#PART_ContentPresenter
    └── Border#Border
```

**Per CalendarButton: ~5-6 visual elements**

---

## Total Visual Element Count

| Component | Count | Elements/Each | Total Elements |
|-----------|-------|---------------|----------------|
| CalendarDayButton | 42 | ~6 | ~252 |
| CalendarButton | 12 | ~6 | ~72 |
| Day titles (TextBlock) | 7 | 1 | 7 |
| Buttons (Header, Nav) | 3 | ~4 | ~12 |
| Grids (MonthView, YearView) | 2 | 1 | 2 |
| CalendarItem template | 1 | ~8 | ~8 |
| Calendar template | 1 | ~2 | ~2 |
| **Total Visual Elements** | | | **~355** |

---

## Allocation Sources (Estimated)

### 1. Control Objects (~30%)
- Each AvaloniaObject base: ~200-300 bytes
- 68 controls × ~250 bytes = ~17 KB base objects

### 2. Styled Properties Value Storage (~25%)
- Each control has multiple styled properties
- Property value dictionaries, priority queues
- Estimated: ~500 KB

### 3. Template Instantiation (~20%)
- ControlTemplate parsing and building
- XAML compiled templates have lower overhead
- Style application and selector matching

### 4. Visual Tree Building (~15%)
- Visual parent/child relationships
- Bounds calculation structures
- Transform matrices

### 5. Event Handlers & Subscriptions (~10%)
- CalendarDayButton: MouseDown, MouseUp, PointerEntered, Click handlers
- CalendarButton: Similar handlers
- 54 buttons × 4 handlers = 216 event subscriptions

---

## Key Findings

### 1. **High Control Count**
The Calendar creates **54 button controls** (42 day buttons + 12 month/year buttons), each with its own template. This is the primary allocation driver.

### 2. **Template Complexity**
Each button has a template with:
- Panel (root)
- 2 Border controls
- ContentPresenter
- Implicit styling subscriptions

### 3. **Event Handler Allocations**
From [CalendarItem.cs](src/Avalonia.Controls/Calendar/CalendarItem.cs) lines 186-207:
```csharp
EventHandler<PointerPressedEventArgs> cellMouseLeftButtonDown = Cell_MouseLeftButtonDown;
EventHandler<PointerReleasedEventArgs> cellMouseLeftButtonUp = Cell_MouseLeftButtonUp;
EventHandler<PointerEventArgs> cellMouseEntered = Cell_MouseEntered;
EventHandler<RoutedEventArgs> cellClick = Cell_Click;

// Applied to 42 CalendarDayButtons
cell.CalendarDayButtonMouseDown += cellMouseLeftButtonDown;
cell.CalendarDayButtonMouseUp += cellMouseLeftButtonUp;
cell.PointerEntered += cellMouseEntered;
cell.Click += cellClick;
```

### 4. **Inefficient Button Base Class**
CalendarDayButton and CalendarButton inherit from `Button`, which brings:
- Full ContentControl functionality
- Command binding infrastructure
- Full keyboard/pointer input handling

---

## Potential Optimizations

### High Impact (Architectural Changes)

#### 1. **Virtualized Calendar View** (Est. -60% allocations)
Instead of creating 42 CalendarDayButtons, use a virtualized approach:
- Create a single custom control that paints all days
- Use hit-testing to determine which day was clicked
- Similar to how VirtualizingStackPanel works

#### 2. **Lightweight Day Cells** (Est. -40% allocations)
Replace CalendarDayButton (inherits Button) with a lighter alternative:
- Create `CalendarDayCell` that inherits directly from `Control` or `TemplatedControl`
- Remove unnecessary Button functionality (Command, ClickMode, etc.)
- Use simpler template (single Border + TextBlock)

#### 3. **Template Caching/Pooling** (Est. -20% allocations)
- Pool and reuse CalendarDayButton instances when switching months
- Currently, controls are recreated each time

### Medium Impact

#### 4. **Shared Event Handlers** (Est. -5% allocations)
Event handlers are already cached per [CalendarItem.cs](src/Avalonia.Controls/Calendar/CalendarItem.cs#L186-189):
```csharp
EventHandler<PointerPressedEventArgs> cellMouseLeftButtonDown = Cell_MouseLeftButtonDown;
```
This is already optimized ✓

#### 5. **Reduce Template Complexity** (Est. -10% allocations)
Current template has redundant nesting:
```xaml
<Panel>
  <Border Name="Root">
    <ContentPresenter/>
  </Border>
  <Border Name="Border"/>  <!-- Just for border effect -->
</Panel>
```

Could be simplified to:
```xaml
<Border Name="Root">
  <ContentPresenter/>
</Border>
```
With border effects handled via pseudo-classes on Root.

### Low Impact (Already Optimized by LayoutQueue changes)

#### 6. **Layout System** (Already -55% on layout allocations)
The LayoutQueue optimization already addressed layout-related allocations:
- Master: 77.1 KB layout allocations
- Optimized: 34.4 KB layout allocations

---

## Recommendations

### Short-term (Low Risk)
1. Simplify CalendarDayButton/CalendarButton templates
2. Review if all styled properties are necessary

### Medium-term (Medium Risk)
1. Create lightweight `CalendarDayCell` control
2. Implement control recycling when switching months

### Long-term (High Risk, High Reward)
1. Implement fully virtualized calendar rendering
2. Consider using composition/drawing API for day cells instead of discrete controls

---

## Comparison with Other Controls

| Control | Allocations | Notes |
|---------|-------------|-------|
| Button | 21.59 KB | Single templated control |
| ScrollViewer | 60.01 KB | Complex, but limited child count |
| TextBox | 188.5 KB | Text editing infrastructure |
| **Calendar** | **2,651 KB** | 54+ buttons with templates |

Calendar allocates **~123x more than a Button** due to its high control count.

---

## Files Analyzed

- [Calendar.cs](src/Avalonia.Controls/Calendar/Calendar.cs) - Main control (RowsPerMonth=7, ColumnsPerMonth=7)
- [CalendarItem.cs](src/Avalonia.Controls/Calendar/CalendarItem.cs) - Creates day/month buttons
- [CalendarDayButton.cs](src/Avalonia.Controls/Calendar/CalendarDayButton.cs) - Day cell (inherits Button)
- [CalendarButton.cs](src/Avalonia.Controls/Calendar/CalendarButton.cs) - Month/year cell (inherits Button)
- [Calendar.xaml](src/Avalonia.Themes.Fluent/Controls/Calendar.xaml) - Calendar theme
- [CalendarItem.xaml](src/Avalonia.Themes.Fluent/Controls/CalendarItem.xaml) - CalendarItem theme
- [CalendarDayButton.xaml](src/Avalonia.Themes.Fluent/Controls/CalendarDayButton.xaml) - Day button theme
- [CalendarButton.xaml](src/Avalonia.Themes.Fluent/Controls/CalendarButton.xaml) - Month button theme

---

## WPF Comparison Analysis

### WPF Implementation Similarities

WPF's Calendar control uses the **exact same architecture** as Avalonia:

1. **Same Control Hierarchy**:
   - `Calendar` → `CalendarItem` → `CalendarDayButton` / `CalendarButton`
   
2. **Same Control Counts**:
   - 42 `CalendarDayButton` controls (6 rows × 7 columns)
   - 12 `CalendarButton` controls (3 rows × 4 columns)
   
3. **Same Base Class**:
   - `CalendarDayButton : Button`
   - `CalendarButton : Button`

4. **Same Population Pattern** (from WPF's `CalendarItem.PopulateGrids()`):
   ```csharp
   // WPF: CalendarItem.cs lines 935-970
   private void PopulateGrids()
   {
       for (int i = 1; i < ROWS; i++)
       {
           for (int j = 0; j < COLS; j++)
           {
               CalendarDayButton dayCell = new CalendarDayButton
               {
                   Owner = this.Owner
               };
               // Add event handlers, bindings, etc.
               this._monthView.Children.Add(dayCell);
           }
       }
   }
   ```

### Key WPF Differences

1. **Template Optimization via FrameworkElementFactory**:
   WPF has `FrameworkElementFactory` which provides optimized template instantiation with:
   - Pre-compiled property setters
   - Cached child index tracking
   - Known type factories for fast instantiation

2. **KnownType Factory Pattern**:
   ```csharp
   // WPF: FrameworkElementFactory.cs
   WpfKnownType knownType = XamlReader.BamlSharedSchemaContext.GetKnownXamlType(_type);
   _knownTypeFactory = knownType?.DefaultConstructor;
   
   // Fast path for known types
   if (_knownTypeFactory != null)
       return _knownTypeFactory.Invoke() as DependencyObject;
   ```

3. **Deferred Initialization**:
   WPF uses `BeginInit()`/`EndInit()` pattern more aggressively to batch property changes.

4. **StyleHelper Optimization**:
   WPF has `StyleHelper.CreateChildIndexFromChildName()` which pre-computes indices for faster template lookups.

### WPF Alternative: Native MonthCalendar

For Win32 interop scenarios, WPF also supports the native Windows `MonthCalendar` control through:
- `WindowsCalendar` proxy class (UIAutomation)
- Creates `CalendarDay` objects **on-demand** for accessibility, not pre-created

This shows a hybrid approach is possible: use the rich managed Calendar for full styling, or a native control for minimal allocations.

### Learnings for Avalonia

1. **Architecture is Correct**: Both WPF and Avalonia made the same design decision - this isn't a design flaw, it's the cost of a fully-stylable Calendar.

2. **WPF Mitigations We Could Adopt**:
   - Known type factory caching for faster control instantiation
   - More aggressive property batching during template application
   - Consider implementing `ISupportInitialize` pattern more broadly

3. **Alternative Approaches (Not in WPF)**:
   - Composition API for custom drawing (Avalonia's `DrawingContext`)
   - True virtualization with cell pooling
   - Lighter-weight `CalendarDayCell` base class

### Benchmark Expectation

**WPF would have similar allocation patterns** for Calendar since it uses the same architecture. Any significant reduction requires changing the fundamental approach of using discrete Button controls.

---

## Optimization Attempts (perf/visual-tree-optimizations branch)

### Attempted: ISupportInitialize BeginInit/EndInit Batching

**Hypothesis**: Wrapping property changes in `BeginInit()`/`EndInit()` would defer styling until all properties are set, reducing redundant work.

**Implementation** (in CalendarItem.PopulateGrids):
```csharp
for (int i = 1; i < Calendar.RowsPerMonth; i++)
{
    for (int j = 0; j < Calendar.ColumnsPerMonth; j++)
    {
        var cell = new CalendarDayButton();
        cell.BeginInit();  // Defer styling
        
        if (Owner != null) cell.Owner = Owner;
        cell.SetValue(Grid.RowProperty, i);
        cell.SetValue(Grid.ColumnProperty, j);
        // ... event subscriptions ...
        
        cell.EndInit();  // Apply styling
        children.Add(cell);
    }
}
```

**Result**: **No measurable improvement**

| Metric | Before | After |
|--------|--------|-------|
| Allocated | 2.59 MB | 2.59 MB |
| Mean Time | 2.628 ms | 3.112 ms |

**Why it didn't help**: The `BeginInit()`/`EndInit()` pattern in Avalonia only affects styling application when the control is attached to the logical tree. Since buttons are created and configured *before* being added to `MonthView.Children`, the styling deferral has no effect - styling doesn't apply until `AddRange(children)` anyway.

### Attempted: Event Handler Caching

**Status**: Already implemented in current code.

The event handlers are already cached outside the loop:
```csharp
EventHandler<PointerPressedEventArgs> cellMouseLeftButtonDown = Cell_MouseLeftButtonDown;
EventHandler<PointerReleasedEventArgs> cellMouseLeftButtonUp = Cell_MouseLeftButtonUp;
EventHandler<PointerEventArgs> cellMouseEntered = Cell_MouseEntered;
EventHandler<RoutedEventArgs> cellClick = Cell_Click;
```

This prevents delegate allocation per button - the same delegate instance is reused for all 42 buttons. **Already optimized**.

### Not Attempted: Known Type Factory Caching

**Why not attempted**: XamlX compiler already emits direct `newobj` IL instructions for compiled XAML. This is already the most efficient instantiation pattern - factory caching provides no benefit when IL directly calls constructors.

For code-created controls (like in `PopulateGrids`), the `new CalendarDayButton()` calls are already direct constructor invocations.

### Potential Future Optimizations

1. **Simplified Template**: Current CalendarDayButton has Panel→2 Borders→ContentPresenter. Could be simplified to Border→ContentPresenter with pseudo-class styling for effects.

2. **Control Pooling**: Cache and reuse CalendarDayButton/CalendarButton instances between month changes instead of recreating.

3. **Lightweight Cell Type**: Create `CalendarDayCell : Control` instead of inheriting full Button functionality.

4. **Virtualized Drawing**: Single custom control that renders all days via `DrawingContext`, with hit-testing for interactions.

---

## Conclusion

The Calendar control's high memory allocation is **fundamentally a template/control structure issue**, not a layout issue. The layout system contributes only ~3-4% of total allocations.

The primary cause is creating 54+ fully-featured Button controls, each with complex templates containing multiple visual elements. This is the **same architecture WPF uses**, validating that it's not a design mistake but rather the cost of a fully-stylable Calendar control.

To significantly reduce allocations, the control architecture would need to change to use lighter-weight cell representations or virtualization - changes that would also improve WPF if applied there.

The LayoutQueue optimization in this branch provides marginal improvement (~5%) for Calendar, but the real gains would come from architectural changes to the Calendar control itself.
