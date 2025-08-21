using Avalonia;
using Avalonia.Controls;
using System;

namespace DraggingService;

/// <summary>
/// Provides attached properties to configure drag-and-drop behavior in Avalonia controls.
/// </summary>
public class DraggingServiceAttached {
  public static readonly AttachedProperty<DraggingServiceInstanceWrapper?> DraggingServiceInstanceProperty =
      AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, DraggingServiceInstanceWrapper?>(
          "DraggingServiceInstance", defaultValue: null);

  public static readonly AttachedProperty<bool> IsRootOfDraggingInstanceProperty =
      AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Panel, bool>(
          "IsRootOfDraggingInstance", defaultValue: false);

  public static readonly AttachedProperty<DraggingServiceDropEvent> DropCallbackProperty =
      AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, DraggingServiceDropEvent>(
          "DropCallback", defaultValue: (e) => { });

  public static readonly AttachedProperty<DraggingServiceDragEvent> DragCallbackProperty =
      AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, DraggingServiceDragEvent>(
          "DragCallback", defaultValue: (e) => { });

  public static readonly AttachedProperty<bool> IsDropEnableProperty =
      AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, bool>(
          "IsDropEnable", defaultValue: false);

  public static readonly AttachedProperty<bool> IsDragEnableProperty =
      AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, bool>(
          "IsDragEnable", defaultValue: false);

  public static readonly AttachedProperty<bool> IsSelectedForMultiDragProperty =
      AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, bool>(
          "IsSelectedForMultiDrag", defaultValue: false);

  public static readonly AttachedProperty<DraggingServiceSelectionEvent> SelectedForMultiDragCallbackProperty =
      AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, DraggingServiceSelectionEvent>(
          "SelectedForMultiDragCallback", defaultValue: (args) => { });

  static DraggingServiceAttached() {
    IsRootOfDraggingInstanceProperty.Changed.AddClassHandler<Panel>(OnIsRootOfDraggingInstanceChanged);
    DropCallbackProperty.Changed.AddClassHandler<Control>(OnDropCallbackChanged);
    DragCallbackProperty.Changed.AddClassHandler<Control>(OnDragCallbackChanged);
    IsSelectedForMultiDragProperty.Changed.AddClassHandler<Control>(OnIsSelectedForMultiDragChanged);
    SelectedForMultiDragCallbackProperty.Changed.AddClassHandler<Control>(OnSelectedForMultiDragCallbackChanged);
  }

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

  public static void SetDraggingServiceInstance(Control control, DraggingServiceInstanceWrapper? instance)
    => control.SetValue(DraggingServiceInstanceProperty, instance);
  public static DraggingServiceInstanceWrapper? GetDraggingServiceInstance(Control control)
      => control.GetValue(DraggingServiceInstanceProperty);

  // IsRootOfDraggingInstance
  public static void SetIsRootOfDraggingInstance(Panel panel, bool value)
      => panel.SetValue(IsRootOfDraggingInstanceProperty, value);
  public static bool GetIsRootOfDraggingInstance(Panel panel)
      => panel.GetValue(IsRootOfDraggingInstanceProperty);

  // DropCallback
  public static void SetDropCallback(Control control, DraggingServiceDropEvent callback)
      => control.SetValue(DropCallbackProperty, callback);
  public static DraggingServiceDropEvent GetDropCallback(Control control)
      => control.GetValue(DropCallbackProperty);

  // DragCallback
  public static void SetDragCallback(Control control, DraggingServiceDragEvent callback)
      => control.SetValue(DragCallbackProperty, callback);
  public static DraggingServiceDragEvent GetDragCallback(Control control)
      => control.GetValue(DragCallbackProperty);

  // IsDropEnable
  public static void SetIsDropEnable(Control control, bool value)
      => control.SetValue(IsDropEnableProperty, value);
  public static bool GetIsDropEnable(Control control)
      => control.GetValue(IsDropEnableProperty);

  // IsDragEnable
  public static void SetIsDragEnable(Control control, bool value)
      => control.SetValue(IsDragEnableProperty, value);
  public static bool GetIsDragEnable(Control control)
      => control.GetValue(IsDragEnableProperty);

  // IsSelectedForMultiDrag
  public static void SetIsSelectedForMultiDrag(Control control, bool value)
      => control.SetValue(IsSelectedForMultiDragProperty, value);
  public static bool GetIsSelectedForMultiDrag(Control control)
      => control.GetValue(IsSelectedForMultiDragProperty);

  // SelectedForMultiDragCallback
  public static void SetSelectedForMultiDragCallback(Control control, DraggingServiceSelectionEvent callback)
      => control.SetValue(SelectedForMultiDragCallbackProperty, callback);
  public static DraggingServiceSelectionEvent GetSelectedForMultiDragCallback(Control control)
      => control.GetValue(SelectedForMultiDragCallbackProperty);
  internal static void CleanProperties(Control control) {
    control.ClearValue(IsRootOfDraggingInstanceProperty);
    control.ClearValue(DropCallbackProperty);
    control.ClearValue(DragCallbackProperty);
    control.ClearValue(IsSelectedForMultiDragProperty);
    control.ClearValue(DraggingServiceInstanceProperty);
  }
}
