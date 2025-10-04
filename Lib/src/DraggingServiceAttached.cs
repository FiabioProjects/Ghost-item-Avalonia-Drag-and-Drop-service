using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace DraggingService;

/// <summary>
/// Provides attached properties to configure drag-and-drop behavior in Avalonia controls.
/// </summary>
public class DraggingServiceAttached {
  /// <summary>
  /// Holds a reference to the <see cref="DraggingServiceInstanceWrapper"/> associated with a control.
  /// This wrapper contains the instance and its relative depth from the root panel.
  /// </summary>
  public static readonly AttachedProperty<DraggingServiceInstanceWrapper?> DraggingServiceInstanceProperty =
      AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, DraggingServiceInstanceWrapper?>(
          "DraggingServiceInstance", defaultValue: null);

  /// <summary>
  /// Marks a <see cref="Panel"/> as the root of a <see cref="DraggingServiceInstance"/>.
  /// The root manages all drag operations within its visual tree.
  /// </summary>
  public static readonly AttachedProperty<bool> IsRootOfDraggingInstanceProperty =
      AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Panel, bool>(
          "IsRootOfDraggingInstance", defaultValue: false);

  /// <summary>
  /// Registers a callback invoked when a dragged control is dropped onto this control.
  /// </summary>
  public static readonly AttachedProperty<DraggingServiceDropEvent> DropCallbackProperty =
      AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, DraggingServiceDropEvent>(
          "DropCallback", defaultValue: (e) => { });

  /// <summary>
  /// Registers a callback invoked when this control initiates or participates in a drag operation.
  /// </summary>
  public static readonly AttachedProperty<DraggingServiceDragEvent> DragCallbackProperty =
      AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, DraggingServiceDragEvent>(
          "DragCallback", defaultValue: (e) => { });


  /// <summary>
  /// Registers a callback invoked when a dragging operation ends for a control.
  /// </summary>
  public static readonly AttachedProperty<DraggingServiceDragEvent> EndDragCallbackProperty =
      AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, DraggingServiceDragEvent>(
          "EndDragCallback", defaultValue: (e) => { });

  /// <summary>
  /// Determines whether this control can act as a drop target in a drag-and-drop operation.
  /// </summary>
  public static readonly AttachedProperty<bool> IsDropEnableProperty =
      AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, bool>(
          "IsDropEnable", defaultValue: false);

  /// <summary>
  /// Determines whether this control can be dragged in a drag-and-drop operation.
  /// </summary>
  public static readonly AttachedProperty<bool> IsDragEnableProperty =
      AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, bool>(
          "IsDragEnable", defaultValue: false);

  /// <summary>
  /// Indicates whether this control is currently selected for a multi-drag operation.
  /// </summary>
  public static readonly AttachedProperty<bool> IsSelectedForMultiDragProperty =
      AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, bool>(
          "IsSelectedForMultiDrag", defaultValue: false);

  /// <summary>
  /// Registers a callback invoked when the selection state of this control in a multi-drag operation changes.
  /// </summary>
  public static readonly AttachedProperty<DraggingServiceSelectionEvent> SelectedForMultiDragCallbackProperty =
      AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, DraggingServiceSelectionEvent>(
          "SelectedForMultiDragCallback", defaultValue: (args) => { });

  /// <summary>
  /// Defines the <see cref="RoutingStrategies"/> used when drag events bubble or tunnel through the visual tree.
  /// </summary>
  public static readonly AttachedProperty<RoutingStrategies> DragEventRoutingStrategyProperty =
      AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, RoutingStrategies>(
          "DragEventRoutingStrategy", defaultValue: RoutingStrategies.Bubble);



  static DraggingServiceAttached() {
    IsRootOfDraggingInstanceProperty.Changed.AddClassHandler<Panel>(OnIsRootOfDraggingInstanceChanged);
    DropCallbackProperty.Changed.AddClassHandler<Control>(OnDropCallbackChanged);
    DragCallbackProperty.Changed.AddClassHandler<Control>(OnDragCallbackChanged);
    EndDragCallbackProperty.Changed.AddClassHandler<Control>(OnEndDragCallbackChanged);
    IsSelectedForMultiDragProperty.Changed.AddClassHandler<Control>(OnIsSelectedForMultiDragChanged);
    SelectedForMultiDragCallbackProperty.Changed.AddClassHandler<Control>(OnSelectedForMultiDragCallbackChanged);

  }

  /// <summary>
  /// Handles changes to <see cref="IsRootOfDraggingInstanceProperty"/> by creating or disposing a <see cref="DraggingServiceInstance"/>.
  /// </summary>
  private static void OnIsRootOfDraggingInstanceChanged(Panel element, AvaloniaPropertyChangedEventArgs e) {
    if( e.NewValue is not bool value )
      return;
    if( value ) {
      element.SetValue(DraggingServiceInstanceProperty, new DraggingServiceInstanceWrapper(new DraggingServiceInstance(element), 0));
    } else {
      element.GetValue(DraggingServiceInstanceProperty)?.Instance.Dispose();
      element.SetValue(DraggingServiceInstanceProperty, null);
    }
  }

  /// <summary>
  /// Handles changes to <see cref="DropCallbackProperty"/> by registering the control as a drop target within the drag instance.
  /// </summary>
  private static void OnDropCallbackChanged(Control control, AvaloniaPropertyChangedEventArgs e) {
    if( e.NewValue is not DraggingServiceDropEvent dse )
      return;
    var instanceRecord = control.GetValue(DraggingServiceInstanceProperty) ?? FindRootInstance(control);
    if( instanceRecord == null )
      throw new InvalidOperationException("Can't set DropCallback on: " + nameof(control) + " instance root not found");
    if( !instanceRecord.Instance.IsDisposing ) {
      control.SetValue(DraggingServiceInstanceProperty, instanceRecord);
      instanceRecord.Instance.AllowDrop(control, instanceRecord.DepthFromRoot);
    }
  }

  /// <summary>
  /// Handles changes to <see cref="DragCallbackProperty"/> by registering the control as draggable within the drag instance.
  /// </summary>
  private static void OnDragCallbackChanged(Control control, AvaloniaPropertyChangedEventArgs e) {
    if( e.NewValue is not DraggingServiceDragEvent dse )
      return;
    var instanceRecord = control.GetValue(DraggingServiceInstanceProperty) ?? FindRootInstance(control);
    if( instanceRecord == null )
      throw new InvalidOperationException("Can't set DragCallback on: " + nameof(control) + " instance root not found");
    if( !instanceRecord.Instance.IsDisposing ) {
      control.SetValue(DraggingServiceInstanceProperty, instanceRecord);
      instanceRecord.Instance.AllowDrag(control);
    }
  }
  /// <summary>
  /// Handles changes to <see cref="EndDragCallbackProperty"/> by registering the control as draggable within the drag instance.
  /// </summary>
  private static void OnEndDragCallbackChanged(Control control, AvaloniaPropertyChangedEventArgs e) {
    if( e.NewValue is not DraggingServiceDragEvent dse )
      return;
    var instanceRecord = control.GetValue(DraggingServiceInstanceProperty) ?? FindRootInstance(control);
    if( instanceRecord == null )
      throw new InvalidOperationException("Can't set DragCallback on: " + nameof(control) + " instance root not found");
    if( !instanceRecord.Instance.IsDisposing ) {
      control.SetValue(DraggingServiceInstanceProperty, instanceRecord);
      instanceRecord.Instance.AllowDrag(control);
    }
  }
  /// <summary>
  /// Handles changes to <see cref="IsSelectedForMultiDragProperty"/> by updating the control’s selection state in the drag instance.
  /// </summary>
  private static void OnIsSelectedForMultiDragChanged(Control control, AvaloniaPropertyChangedEventArgs e) {
    if( e.NewValue is not bool newState )
      return;
    var instanceRecord = control.GetValue(DraggingServiceInstanceProperty) ?? FindRootInstance(control);
    if( instanceRecord == null )
      throw new InvalidOperationException("Can't set IsSelectedForMultiDrag on: " + nameof(control) + " instance root not found");
    if( !instanceRecord.Instance.IsDisposing ) {
      control.SetValue(DraggingServiceInstanceProperty, instanceRecord);
      instanceRecord.Instance.SetControlMultiDragState(control, newState);
    }
  }

  /// <summary>
  /// Handles changes to <see cref="SelectedForMultiDragCallbackProperty"/> by ensuring the control is associated with the current drag instance.
  /// </summary>
  private static void OnSelectedForMultiDragCallbackChanged(Control control, AvaloniaPropertyChangedEventArgs e) {
    if( e.NewValue is not DraggingServiceSelectionEvent dse )
      return;
    var instanceRecord = control.GetValue(DraggingServiceInstanceProperty) ?? FindRootInstance(control);
    if( instanceRecord == null )
      throw new InvalidOperationException("Can't set SelectedForMultiDragCallback on: " + nameof(control) + " instance root not found");
    if( !instanceRecord.Instance.IsDisposing ) {
      control.SetValue(DraggingServiceInstanceProperty, instanceRecord);
    }
  }

  /// <summary>
  /// Traverses the visual tree upward to find the nearest ancestor marked as the root of a drag instance.
  /// </summary>
  private static DraggingServiceInstanceWrapper? FindRootInstance(StyledElement element) {
    DraggingServiceInstanceWrapper? instance = null;
    StyledElement? ptr = element;
    int depth = 0;
    while( ptr != null && instance == null ) {
      if( ptr.GetValue(IsRootOfDraggingInstanceProperty) ) {
        instance = new DraggingServiceInstanceWrapper(ptr.GetValue(DraggingServiceInstanceProperty)!.Instance, depth);
      }
      ptr = ptr.Parent;
      depth++;
    }
    return instance;
  }

  /// <summary>
  /// Associates a <see cref="DraggingServiceInstanceWrapper"/> with a control.
  /// </summary>
  public static void SetDraggingServiceInstance(Control control, DraggingServiceInstanceWrapper? instance)
    => control.SetValue(DraggingServiceInstanceProperty, instance);

  /// <summary>
  /// Retrieves the <see cref="DraggingServiceInstanceWrapper"/> associated with a control.
  /// </summary>
  public static DraggingServiceInstanceWrapper? GetDraggingServiceInstance(Control control)
      => control.GetValue(DraggingServiceInstanceProperty);

  /// <summary>
  /// Sets whether a <see cref="Panel"/> is the root of a drag instance.
  /// </summary>
  public static void SetIsRootOfDraggingInstance(Panel panel, bool value)
      => panel.SetValue(IsRootOfDraggingInstanceProperty, value);

  /// <summary>
  /// Gets whether a <see cref="Panel"/> is the root of a drag instance.
  /// </summary>
  public static bool GetIsRootOfDraggingInstance(Panel panel)
      => panel.GetValue(IsRootOfDraggingInstanceProperty);

  /// <summary>
  /// Sets the drop callback for a control.
  /// </summary>
  public static void SetDropCallback(Control control, DraggingServiceDropEvent callback)
      => control.SetValue(DropCallbackProperty, callback);

  /// <summary>
  /// Gets the drop callback for a control.
  /// </summary>
  public static DraggingServiceDropEvent GetDropCallback(Control control)
      => control.GetValue(DropCallbackProperty);

  /// <summary>
  /// Sets the drag callback for a control.
  /// </summary>
  public static void SetDragCallback(Control control, DraggingServiceDragEvent callback)
      => control.SetValue(DragCallbackProperty, callback);

  /// <summary>
  /// Sets the end drag callback for a control.
  /// </summary>
  public static void SetEndDragCallback(Control control, DraggingServiceDragEvent callback)
      => control.SetValue(EndDragCallbackProperty, callback);
  /// <summary>
  /// Gets the drag callback for a control.
  /// </summary>
  public static DraggingServiceDragEvent GetDragCallback(Control control)
      => control.GetValue(DragCallbackProperty);

  /// <summary>
  /// Gets the end drag callback for a control.
  /// </summary>
  public static DraggingServiceDragEvent GetEndDragCallback(Control control)
      => control.GetValue(EndDragCallbackProperty);

  /// <summary>
  /// Sets whether a control is enabled as a drop target.
  /// </summary>
  public static void SetIsDropEnable(Control control, bool value)
      => control.SetValue(IsDropEnableProperty, value);

  /// <summary>
  /// Gets whether a control is enabled as a drop target.
  /// </summary>
  public static bool GetIsDropEnable(Control control)
      => control.GetValue(IsDropEnableProperty);

  /// <summary>
  /// Sets whether a control is enabled as draggable.
  /// </summary>
  public static void SetIsDragEnable(Control control, bool value)
      => control.SetValue(IsDragEnableProperty, value);

  /// <summary>
  /// Gets whether a control is enabled as draggable.
  /// </summary>
  public static bool GetIsDragEnable(Control control)
      => control.GetValue(IsDragEnableProperty);

  /// <summary>
  /// Sets whether a control is selected for multi-drag.
  /// </summary>
  public static void SetIsSelectedForMultiDrag(Control control, bool value)
      => control.SetValue(IsSelectedForMultiDragProperty, value);

  /// <summary>
  /// Gets whether a control is selected for multi-drag.
  /// </summary>
  public static bool GetIsSelectedForMultiDrag(Control control)
      => control.GetValue(IsSelectedForMultiDragProperty);

  /// <summary>
  /// Sets the multi-drag selection callback for a control.
  /// </summary>
  public static void SetSelectedForMultiDragCallback(Control control, DraggingServiceSelectionEvent callback)
      => control.SetValue(SelectedForMultiDragCallbackProperty, callback);

  /// <summary>
  /// Gets the multi-drag selection callback for a control.
  /// </summary>
  public static DraggingServiceSelectionEvent GetSelectedForMultiDragCallback(Control control)
      => control.GetValue(SelectedForMultiDragCallbackProperty);

  /// <summary>
  /// Sets the routing strategy for drag events raised by a control.
  /// </summary>
  public static void SetDragEventRoutingStrategy(Control control, RoutingStrategies s)
     => control.SetValue(DragEventRoutingStrategyProperty, s);

  /// <summary>
  /// Gets the routing strategy for drag events raised by a control.
  /// </summary>
  public static RoutingStrategies GetDragEventRoutingStrategy(Control control)
      => control.GetValue(DragEventRoutingStrategyProperty);

  /// <summary>
  /// Clears attached drag-and-drop properties from a control.
  /// Useful when cleaning up dynamically created controls or resetting drag state.
  /// </summary>
  internal static void CleanProperties(Control control) {
    control.ClearValue(IsRootOfDraggingInstanceProperty);
    control.ClearValue(DropCallbackProperty);
    control.ClearValue(DragCallbackProperty);
    control.ClearValue(IsSelectedForMultiDragProperty);
    control.ClearValue(DraggingServiceInstanceProperty);
  }
}
