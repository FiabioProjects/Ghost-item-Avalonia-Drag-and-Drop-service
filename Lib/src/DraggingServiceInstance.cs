using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DraggingService;
/// <summary>
/// Manages the drag-and-drop logic for controls within a designated root panel.
/// </summary>
public class DraggingServiceInstance: IDisposable {
  private bool _disposed = false;
  internal bool IsDisposing { get; private set; } // This is used to prevent attached props calls when the instance is being disposed
  private readonly Panel _root;
  private readonly Cursor? _defaultCursor;
  private readonly GhostContainer _ghostContainer = new GhostContainer();
  private Control? _droppingTo = null;
  private readonly List<(Control, int)> _dropAllowedControlsSorted = []; // This list is used to sort the controls by distance to the root for hit testing purposes
  private readonly HashSet<Control> _multiDraggedControls = [];          // This is used to store the controls that are currently selected for multi-dragging 
  /// <summary>
  /// Initializes a new instance of the <see cref="DraggingServiceInstance"/> class.
  /// </summary>
  /// <param name="panel">The root panel where dragging operations are handled.</param>
  /// <param name="GhostOpacity">Opacity of the ghost image during drag.</param>
  public DraggingServiceInstance(Panel panel, double GhostOpacity = 0.75) {
    GetInstanceRootDistance(panel); // Ensure the panel is the root of the dragging service instance and get the distance to the root
    SetBackgroundAndHitTesting(panel);
    _ghostContainer.Opacity = GhostOpacity;
    _root = panel;
    _defaultCursor = _root.Cursor;
    //From Avalonia Docs: an event starts always from the root and goes down to the target and then back up. So Bubbling handlers are called at the end. 
    panel.AddHandler(Panel.PointerMovedEvent, Drag, RoutingStrategies.Bubble, true);
    panel.AddHandler(Panel.PointerReleasedEvent, EndDrag, RoutingStrategies.Bubble, true);
    panel.AddHandler(Panel.PointerEnteredEvent, OnPointerEntered, RoutingStrategies.Direct, true);
    _root.Children.Add(_ghostContainer);
  }
  private void OnPointerEntered(object? sender, PointerEventArgs e) {
    if( _ghostContainer.DraggingControls.Count > 0 ) {
      e.Pointer.Capture(_root);  //this is needed to listen to call EndDrag when the pointer is outside the root bounds (in the Drag method the pointer capture is released)
    }
  }
  private void Drag(object? sender, PointerEventArgs e) {
    static bool IsDescendantOf(StyledElement? child, StyledElement ancestor) {
      StyledElement? ptr = child;
      while( ptr != null && ptr != ancestor ) {
        ptr = ptr.Parent; // Traverse the ancestry chain until we find the ancestor or reach the root
      }
      return ptr == ancestor;
    }

    if( _ghostContainer.DraggingControls.Count == 0 ) {
      return;
    }
    if( _root.InputHitTest(e.GetPosition(_root)) is not Control controlUnderPointer ) {  //if the pointer is outside the root bounds, we don't do anything
      return;
    }
    if( !e.GetCurrentPoint(_root).Properties.IsLeftButtonPressed ) {   //if for some reason (like unreliability of PointerExit) dragging without mouse pressing occurs the latter is aborted
      EndDrag(sender, e);
      return;
    }
    e.Pointer.Capture(null); //little workaround to allow cursor styling
    _ghostContainer.IsVisible = true;
    _ghostContainer.Render(new Size(_root.Bounds.Width, _root.Bounds.Height), e.GetPosition(_root));
    _root.Cursor = new Cursor(StandardCursorType.No);
    _droppingTo = null;

    foreach( (Control dropTarget, int _) in _dropAllowedControlsSorted ) {
      if( IsDescendantOf(controlUnderPointer, dropTarget) ) {
        _root.Cursor = new Cursor(StandardCursorType.DragCopy);
        _droppingTo = dropTarget;
        return;
      }
    }
  }
  private void EndDrag(object? sender, PointerEventArgs e) {
    if( _droppingTo != null && _root.InputHitTest(e.GetPosition(_root)) != null ) {
      DraggingServiceDropEvent callback = _droppingTo.GetValue(DraggingServiceAttached.AllowDropProperty);
      callback.Invoke(new DraggingServiceDropEventsArgs(e, _ghostContainer.DraggingControls, _droppingTo));
    }
    _ghostContainer.IsVisible = false;
    _root.Cursor = _defaultCursor;
    _ghostContainer.ClearDraggingControls();
    _droppingTo = null;
    e.Pointer.Capture(null);
  }

  private void StartControlDragging(object? sender, PointerPressedEventArgs e) {
    if( sender is not Control control )
      return;

    static StyledElement FindSubRootControl(Control control, Panel root) {
      if( control == root )
        return root;
      StyledElement ptr = control!;
      StyledElement? prev = ptr.Parent;
      while( prev != root ) {
        ptr = ptr!.Parent!;
        prev = ptr?.Parent;
      }
      return ptr!;
    }

    static void AddControlToGhostContainer(Control control, GhostContainer ghostContainer, Panel root) {
      var subRoot = FindSubRootControl(control, root);         //important to get the bounds of the control relative to the root
      if( subRoot is not Control c )
        throw new InvalidOperationException("Sub-root control must be a Control type.");

      ghostContainer.AddChild(control, new Point(c.Bounds.X, c.Bounds.Y));   //this throws If the control is already added to the ghost container

    }

    static void StartDragging(Control element, GhostContainer ghostContainer, Panel root, PointerPressedEventArgs e) {
      if( !element.IsVisible || !element.IsAttachedToVisualTree() )
        throw new InvalidOperationException(nameof(element) + " Control must be visible and attached to the visual tree to be dragged.");
      if( element.Bounds.Width <= 0 || element.Bounds.Height <= 0 ) {
        throw new InvalidOperationException(nameof(element) + "Control must have a non-zero size to be dragged.");
      }
      AddControlToGhostContainer(element, ghostContainer, root);
      element.GetValue(DraggingServiceAttached.AllowDragProperty).Invoke(new DraggingServiceDragEventsArgs(e, ghostContainer.DraggingControls));
    }

    if( control.GetValue(DraggingServiceAttached.IsSelectedForMultiDragProperty) ) {  //if the currently dragged element is selected for multi-dragging then add all the selected controls to the ghost container
      foreach( Control toDrag in _multiDraggedControls ) {
        if( toDrag != control ) {  //skip the current control, so it's dragged last
          StartDragging(toDrag, _ghostContainer, _root, e);
        }
      }
    }
    StartDragging(control, _ghostContainer, _root, e);

    e.Handled = true;
  }

  private static void SetBackgroundAndHitTesting(Control control) {
    Type type = control.GetType();
    PropertyInfo? prop = type.GetProperty("Background", BindingFlags.Public | BindingFlags.Instance);
    if( prop != null && prop.CanRead && prop.CanWrite && prop.GetValue(control) == null ) {
      prop.SetValue(control, Brushes.Transparent);
    }
    control.IsHitTestVisible = true;
  }

  /// <summary>
  /// Registers a control to be draggable.
  /// </summary>
  internal void AllowDrag(Control control) {
    SetBackgroundAndHitTesting(control);
    control.RemoveHandler(Control.PointerPressedEvent, StartControlDragging); // Remove any existing handler to avoid duplicates
    control.AddHandler(Control.PointerPressedEvent, StartControlDragging, RoutingStrategies.Bubble, false);
  }

  /// <summary>
  /// Registers a control to be multi draggable.
  /// </summary>
  internal void SetControlMultiDragState(Control control, bool isSelected) {
    if( isSelected ) {
      if( !_multiDraggedControls.Add(control) )
        throw new InvalidOperationException("Control is already selected for multi-drag.");
    } else {
      _multiDraggedControls.Remove(control);
    }
  }

  /// <summary>
  /// Registers a control to be a valid drop target.
  /// </summary>
  internal void AllowDrop(Control control, int rootDepth) {
    static void InsertControlBySortedDistance(List<(Control control, int distance)> list, (Control control, int distance) item) {
      int index = list.FindIndex(x => x.distance < item.distance);
      if( index == -1 ) {
        list.Add(item);
      } else {
        list.Insert(index, item);
      }
    }

    SetBackgroundAndHitTesting(control);
    InsertControlBySortedDistance(_dropAllowedControlsSorted, (control, rootDepth));
  }

  /// <summary>
  /// Gets the number of controls currently being dragged.
  /// </summary>


  private static (Panel, int) GetInstanceRootDistance(StyledElement child) {
    int height = 0;
    StyledElement? ptr = child;
    while( ptr != null && !ptr.GetValue(DraggingServiceAttached.IsRootOfDraggingInstanceProperty) ) {
      ptr = ptr.Parent;
      height++;
    }
    if( ptr is not Panel root )
      throw new InvalidOperationException("No Panel with IsRootOfDraggingInstanceProperty=true found in the ancestry for ." + nameof(child));
    return (root, height);
  }


  /// <summary>
  /// Disposes the dragging service and releases its resources.
  /// </summary>
  public void Dispose() {
    if( _root.GetValue(DraggingServiceAttached.IsRootOfDraggingInstanceProperty) ) {
      Console.WriteLine("Trying to forcefully dispose am element of the dragging service instance. Set IsRootOfDraggingInstanceProperty attached property to false in the root instead ");
      return;
    }
    IsDisposing = true; // This is used to prevent attached props calls when the instance is being disposed
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing) {
    void CleanupControl(Control c) {
      if( c == null )
        return;
      c.RemoveHandler(Control.PointerPressedEvent, StartControlDragging);
      DraggingServiceAttached.CleanProperties(c);
      foreach( Visual child in c.GetVisualChildren() ) {
        if( child is Control childC ) {
          CleanupControl(childC);
        }
      }
    }
    if( _disposed )
      return;

    if( disposing ) {
      _root.RemoveHandler(Panel.PointerMovedEvent, Drag);
      _root.RemoveHandler(Panel.PointerReleasedEvent, EndDrag);
      _root.RemoveHandler(Control.PointerPressedEvent, StartControlDragging);
      _root.RemoveHandler(Panel.PointerEnteredEvent, OnPointerEntered);
      foreach( Control c in _root.Children ) {
        CleanupControl(c);
      }
      _ghostContainer.ClearDraggingControls();
      _dropAllowedControlsSorted.Clear();
    }

    _disposed = true;
  }

  ~DraggingServiceInstance() {
    Dispose(false);
  }
}
