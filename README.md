# Avalonia Dragging Service
https://github.com/FiabioProjects/Ghost-item-Avalonia-Drag-and-Drop-service
A lightweight, cross-platform **drag-and-drop interaction service** for [AvaloniaUI](https://avaloniaui.net/), designed for flexibility and extensibility.  
Supports multiple independent or nested instances, complex drop precedence, and custom drag/drop logic via callbacks.

> ✅ **Compatible with Avalonia 11+ and .NET 8.0+**

---

## ✨ Features

- ✅ **Multiple independent instances** within the same visual tree
- ✅ **Nested instance support** with inner precedence resolution
- ✅ **Overlap-aware drop target and dragged control resolution**, prioritizing deeper controls
- ✅ **Custom drag/drop callbacks** via attached properties (`DragCallback`, `DropCallback`)
- ✅ **Multi-selection support** for drag operations
- ✅ **Pointer-aware drag context**: includes full `PointerEventArgs` and dragged control set
- ✅ **Customizable Routing Strategy** for Drag/Drop events callbacks
- ✅ **Dynamic service management** via `IDisposable` pattern
- ✅ **XAML/Binding-ready**: Easy to integrate via attached properties
- ✅ **Cross-platform**: Fully compatible with all Avalonia-supported platforms

---

## 🛠️ How to Use

### 1. Declare the namespace to include the library and mark a container (that inherits from Panel control) as the root of a drag instance

```xml
xmlns:ds="clr-namespace:DraggingService;assembly=Simple-Avalonia-DragnDrop-Service"
<StackPanel ds:DraggingServiceAttached.IsRootOfDraggingInstance="True">
```

### 2. Enable Dragging/Dropping on your controls and eventually attach some events handlers:
```xml
<Button Content="Drag me!"
	local:DraggingServiceAttached.IsDragEnable="true"
        local:DraggingServiceAttached.DragCallback="{Binding OnDragCallback}"
         local:DraggingServiceAttached.DragCallback="{Binding OnEndDragCallback}"/>
```
```xml
<TextBlock Content="Drop on me!"
	local:DraggingServiceAttached.IsDropEnable="true"
        local:DraggingServiceAttached.DropCallback="{Binding OnDropCallback}" />
```


### 3. Implement the callbacks in your ViewModel (better for an MVVM approach) or code-behind:
```csharp 
public DraggingServiceDragEvent OnDragCallback { get; } =
     (DraggingServiceDragEventsArgs args) => {
       // Access dragged controls and pointer info
    var controls = args.DraggedControls;
    var pointer = args.PointerEvent;
    // Logic here
};
public DraggingServiceDragEvent OnEndDragCallback { get; } =
     (DraggingServiceDragEventsArgs args) => {
       // Access dragged controls and pointer info
    var controls = args.DraggedControls;
    var pointer = args.PointerEvent;
    // Logic here
};
public DraggingServiceDropEvent OnDropCallback { get; } =
     (DraggingServiceDropEventsArgs args) => {
       // Access dragged controls and pointer info
    var controls = args.DraggedControls;
    var pointer = args.PointerEvent;
    var container = args.DropTarget;
    var offset = args.offsetLambda;
    // Logic here
};
```
Data can be read via the datacontext of the dragged controls/drop targets for a better MVVM approach

### 4. Dispose the service when no longer needed by setting `IsRootOfDraggingInstance` property to `false` on the root control:

```csharp
    DraggingServiceAttached.SetIsRootOfDraggingInstance(control, false);
```

### 🧲 Multi-Control Dragging

The service supports dragging multiple controls at once using the `IsSelectedForMultiDrag` attached property. It is recommended to set this property in the code-behind to dynamically change the selections

#### 🔧 How it Works

- Any control with `IsSelectedForMultiDrag` set to `true` becomes part of the **multi-drag group**.
- When the user starts dragging a control in this group:
  - All other selected controls will be dragged together.
  - Only **one** `DraggingServiceDragEvent` will be triggered for the entire group.
  - The `DraggingServiceDragEventsArgs` will contain **all** dragged controls in `DraggedControls`.
- **Important**: the group will not be cleared after an operation (you can manually clear it in the drag callback)
- All the controls in the group will be dragged, even if `IsDragEnableProperty` is set to false (the latter handles only the drag operation that starts everything)
- You can access the position of each dragged control via the `offsetLambda` function in the `DraggingServiceDropEventsArgs` during the drop event.
- If the user drags a control that has `IsSelectedForMultiDrag` set to `false`:
  - Only that control will be dragged, **even if** other controls are selected.
- You can also register `DraggingServiceSelectionEvent` callbacks to respond to selection changes.



### 📝 Notes

- ⚠️ The service will automatically:
  - Set a **transparent `Background`** on controls if one is not already set.
  - Enable the `IsHitTestVisible` property on all controls participating in drag-and-drop.
- ❌ **Do not call `Dispose()` directly** on the service instance.  
- The `EndDragCallback` is called BEFORE the `DropCallback` when a valid drop occurs.
  ✅ Instead, set `IsRootOfDraggingInstance` to `false` on the root container to dispose of the instance properly.
- 🧩 **Customize the Routing Strategy for Drag event handlers** using the `DragEventRoutingStrategy` attached property (e.g.`ds:DraggingServiceAttached.DragEventRoutingStrategy="Tunnel"`)
- 🧩 All attached properties, such as:
  - `AllowDrag`
  - `AllowDrop`
  - `SelectedForMultiDrag`
  - `IsSelectedForMultiDrag`
  - `IsRootOfDraggingInstance`  
  — can be used both in **XAML** and **code-behind**, allowing for flexible integration in different design patterns (MVVM, code-first, etc). (See `DraggingServiceAttached` static class)

#### Roadmap and improvements 🚧:
- Add options to tunneling or bubbling events for overlapped dragged controls or drop targets
- Add support for dynamic visual tree changes 
- Add support for async callbacks