﻿using Avalonia;
using Avalonia.Controls;
using System;
namespace DraggingService;

/// <summary>
/// Provides attached properties to configure drag-and-drop behavior in Avalonia controls.
/// </summary>
public class DraggingServiceAttached {
  /// <summary>
  /// Holds a reference to the <see cref="DraggingServiceInstanceWrapper"/> associated with a control.
  /// </summary>
  public static readonly AttachedProperty<DraggingServiceInstanceWrapper?> DraggingServiceInstanceProperty =             //reference to the instance used in the control (could be any child of the instance root)
      AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, DraggingServiceInstanceWrapper?>(
          "DraggingServiceInstance", defaultValue: null);

  /// <summary>
  /// Marks a Panel as the root of a <see cref="DraggingServiceInstance"/>.
  /// </summary>
  public static readonly AttachedProperty<bool> IsRootOfDraggingInstanceProperty =                      //check if the panel is the instance root
     AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Panel, bool>(
         "IsRootOfDraggingInstance",
         defaultValue: false);

  /// <summary>
  /// Registers a callback invoked when a dragged control is dropped onto this control.
  /// </summary>
  public static readonly AttachedProperty<DraggingServiceDropEvent> AllowDropProperty =
     AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, DraggingServiceDropEvent>(
         "AllowDrop", defaultValue: (e) => { });
  /// <summary>
  /// Registers a callback invoked when this control starts a drag operation.
  /// </summary>
  public static readonly AttachedProperty<DraggingServiceDragEvent> AllowDragProperty =
     AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, DraggingServiceDragEvent>(
         "AllowDrag", defaultValue: (e) => { });

  /// <summary>
  /// Info about the current selection state of the control in a multi-drag operation.
  /// </summary>
  public static readonly AttachedProperty<bool> IsSelectedForMultiDragProperty =
    AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, bool>(
        "IsSelectedForMultiDrag", defaultValue: false);

  /// <summary>
  ///  Registers a callback invoked when this control selection state for multi-drag operation changes. (you can use a conditional expression in the callback to check if the control is selected or not)
  /// </summary>
  public static readonly AttachedProperty<DraggingServiceSelectionEvent> SelectedForMultiDragProperty =
    AvaloniaProperty.RegisterAttached<DraggingServiceAttached, Control, DraggingServiceSelectionEvent>(
        "SelectedForMultiDrag", defaultValue: (args) => { });
  static DraggingServiceAttached() {
    IsRootOfDraggingInstanceProperty.Changed.AddClassHandler<Panel>(OnIsRootOfDraggingInstanceChanged);
    AllowDropProperty.Changed.AddClassHandler<Control>(OnAllowDropChanged);
    AllowDragProperty.Changed.AddClassHandler<Control>(OnAllowDragChanged);
    IsSelectedForMultiDragProperty.Changed.AddClassHandler<Control>(OnIsSelectedForMultiDragChanged);
    SelectedForMultiDragProperty.Changed.AddClassHandler<Control>(OnSelectedForMultiDragChanged);
  }

  private static void OnIsRootOfDraggingInstanceChanged(Panel element, AvaloniaPropertyChangedEventArgs e) { //this is called when the root of the dragging service is set in the xaml
    if( e.NewValue is not bool value ) {
      return;
    }
    if( value ) {
      element.SetValue(DraggingServiceInstanceProperty, new DraggingServiceInstanceWrapper(new DraggingServiceInstance(element), 0));
    } else {
      element.GetValue(DraggingServiceInstanceProperty)?.Instance.Dispose();  //if the root is set to false then dispose the instance
      element.SetValue(DraggingServiceInstanceProperty, null);  //and remove the instance from the control
    }
  }
  private static void OnAllowDropChanged(Control control, AvaloniaPropertyChangedEventArgs e) {
    if( e.NewValue is not DraggingServiceDropEvent dse ) {
      return;
    }
    DraggingServiceInstanceWrapper? instanceRecord = control.GetValue(DraggingServiceInstanceProperty);
    instanceRecord ??= FindRootInstance(control);  //if instance is not set traverse the visual tree to find it
    if( instanceRecord == null )
      throw new InvalidOperationException("Can't set AllowDropProperty on: " + nameof(control) + " instance root not found");
    if( !instanceRecord.Instance.IsDisposing ) {
      control.SetValue(DraggingServiceInstanceProperty, instanceRecord);  //cache for later use
      instanceRecord.Instance.AllowDrop(control, instanceRecord.DepthFromRoot);
    }
  }
  private static void OnAllowDragChanged(Control control, AvaloniaPropertyChangedEventArgs e) {
    if( e.NewValue is not DraggingServiceDragEvent dse ) {
      return;
    }
    DraggingServiceInstanceWrapper? instanceRecord = control.GetValue(DraggingServiceInstanceProperty);
    instanceRecord ??= FindRootInstance(control);  //if instance is not set traverse the visual tree to find it
    if( instanceRecord == null )
      throw new InvalidOperationException("Can't set AllowDragProperty on: " + nameof(control) + " instance root not found");
    if( !instanceRecord.Instance.IsDisposing ) {
      control.SetValue(DraggingServiceInstanceProperty, instanceRecord);  //cache for later use
      instanceRecord.Instance.AllowDrag(control);
    }
  }
  private static void OnIsSelectedForMultiDragChanged(Control control, AvaloniaPropertyChangedEventArgs e) {
    if( e.NewValue is not bool newState ) {
      return;
    }
    DraggingServiceInstanceWrapper? instanceRecord = control.GetValue(DraggingServiceInstanceProperty);
    instanceRecord ??= FindRootInstance(control);  //if instance is not set traverse the visual tree to find it
    if( instanceRecord == null )
      throw new InvalidOperationException("Can't set IsSelectedForMultiDrag on: " + nameof(control) + " instance root not found");
    if( !instanceRecord.Instance.IsDisposing ) {
      control.SetValue(DraggingServiceInstanceProperty, instanceRecord);
      instanceRecord.Instance.SetControlMultiDragState(control, newState);
    }
  }

  private static void OnSelectedForMultiDragChanged(Control control, AvaloniaPropertyChangedEventArgs e) {
    if( e.NewValue is not DraggingServiceSelectionEvent dse ) {
      return;
    }
    DraggingServiceInstanceWrapper? instanceRecord = control.GetValue(DraggingServiceInstanceProperty);
    instanceRecord ??= FindRootInstance(control);  //if instance is not set traverse the visual tree to find it
    if( instanceRecord == null )
      throw new InvalidOperationException("Can't set SelectedForMultiDrag on: " + nameof(control) + " instance root not found");
    if( !instanceRecord.Instance.IsDisposing ) {
      control.SetValue(DraggingServiceInstanceProperty, instanceRecord);  //cache for later use
    }
  }

  private static DraggingServiceInstanceWrapper? FindRootInstance(StyledElement element) {
    DraggingServiceInstanceWrapper? instance = null;
    StyledElement? ptr = element;
    int depth = 0;
    while( ptr != null && instance == null ) {
      if( ptr.GetValue(IsRootOfDraggingInstanceProperty) ) {
        instance = new DraggingServiceInstanceWrapper(ptr.GetValue(DraggingServiceInstanceProperty)!.Instance, depth);  //if the ptr is the root then it has the instance set
      }
      ptr = ptr.Parent;
      depth++;
    }
    return instance;
  }

  /// <summary>
  /// Sets the drop callback for a control.
  /// </summary>
  public static void SetAllowDrop(Control c, DraggingServiceDropEvent e) { c.SetValue(AllowDropProperty, e); }
  /// <summary>
  /// Sets whether a panel is the root of a drag instance.
  /// </summary>
  public static void SetIsRootOfDraggingInstance(Panel p, bool b) { p.SetValue(IsRootOfDraggingInstanceProperty, b); }
  /// <summary>
  /// Sets the drag callback for a control.
  /// </summary>
  public static void SetAllowDrag(Control c, DraggingServiceDragEvent e) { c.SetValue(AllowDragProperty, e); }

  /// <summary>
  /// Sets the selection state for a control in a multi-drag operation.
  /// </summary>
  public static void SetIsSelectedForMultiDrag(Control c, bool isSelected) { c.SetValue(IsSelectedForMultiDragProperty, isSelected); }

  /// <summary>
  /// Gets the selection state for a control in a multi-drag operation.
  /// </summary>
  public static bool GetIsSelectedForMultiDrag(Control c) { return c.GetValue(IsSelectedForMultiDragProperty); }

  /// <summary>
  /// Sets the selection callback for a control.
  /// </summary>
  public static void SetSelectedForMultiDrag(Control c, DraggingServiceSelectionEvent e) { c.SetValue(SelectedForMultiDragProperty, e); }
  internal static void CleanProperties(Control control) {

    control.ClearValue(IsRootOfDraggingInstanceProperty);
    control.ClearValue(AllowDropProperty);
    control.ClearValue(AllowDragProperty);
    control.ClearValue(IsSelectedForMultiDragProperty);
    control.ClearValue(DraggingServiceInstanceProperty);   //important, this has to be the last property to clear, otherwise the instance will not be disposed properly
  }
}