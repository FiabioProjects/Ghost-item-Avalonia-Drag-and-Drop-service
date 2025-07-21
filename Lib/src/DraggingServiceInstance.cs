using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
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
  private readonly Panel _root;
  private readonly Cursor? _defaultCursor;
  private readonly Canvas _ghostContainer = new Canvas {
    Background = Brushes.Transparent,
    IsVisible = false,
    IsHitTestVisible = false,
    ZIndex = Int32.MaxValue, // Ensure the ghost container is always on top of other controls
  };
  private Control? _droppingTo = null;
  private readonly List<(Control, int)> _dropAllowedControlsSorted = []; // This list is used to sort the controls by distance to the root for hit testing purposes
  private readonly HashSet<Control> _draggingControls = [];         // This is the collection of controls that are currently being dragged (useful because under the hood the dragging objects are images)

  /// <summary>
  /// Initializes a new instance of the <see cref="DraggingServiceInstance"/> class.
  /// </summary>
  /// <param name="panel">The root panel where dragging operations are handled.</param>
  /// <param name="GhostOpacity">Opacity of the ghost image during drag.</param>
  public DraggingServiceInstance(Panel panel, double GhostOpacity = 0.75) {
    GetInstanceRootDistance(panel); // Ensure the panel is the root of the dragging service instance and get the distance to the root
    SetBackgroundAndHitTesting(panel);
    _ghostContainer.Background = Brushes.Transparent;
    panel.Children.Add(_ghostContainer);
    _ghostContainer.Opacity = GhostOpacity;
    _root = panel;
    _defaultCursor = _root.Cursor;
    //From Avalonia Docs: an event starts always from the root and goes down to the target and then back up. So Tunneling handlers are called before. So the last parameter could be false.
    panel.AddHandler(Panel.PointerMovedEvent, Drag, RoutingStrategies.Bubble, true);
    panel.AddHandler(Panel.PointerReleasedEvent, EndDrag, RoutingStrategies.Bubble, true);
    panel.PointerExited += (s, e) => {
      if( DraggingControlsCount() > 0 ) {
        e.Pointer.Capture(_root);  //this is needed to listen to call EndDrag when the pointer is outside the root bounds (in the Drag method the pointer capture is released)
      }
    };
  }

  private void Drag(object? sender, PointerEventArgs e) {
    static bool IsDescendantOf(StyledElement? child, StyledElement ancestor) {
      StyledElement? ptr = child;
      while( ptr != null && ptr != ancestor ) {
        ptr = ptr.Parent; // Traverse the ancestry chain until we find the ancestor or reach the root
      }
      return ptr == ancestor;
    }

    if( DraggingControlsCount() == 0 ) {
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
    _ghostContainer.Width = _root.Bounds.Width;
    _ghostContainer.Height = _root.Bounds.Height;
    _ghostContainer.IsVisible = true;
    _ghostContainer.RenderTransform = new TranslateTransform(e.GetPosition(_root).X, e.GetPosition(_root).Y);
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
      callback.Invoke(new DraggingServiceDropEventsArgs(e, _draggingControls, _droppingTo));
    }
    _ghostContainer.IsVisible = false;
    _root.Cursor = _defaultCursor;
    ClearDraggingControls();
    _droppingTo = null;
    e.Pointer.Capture(null);
  }

  private void StartControlDragging(object? sender, PointerPressedEventArgs e) {
    if( sender is not Control control )
      return;
    if( !control.IsVisible || !control.IsAttachedToVisualTree() )
      throw new InvalidOperationException(nameof(control) + " Control must be visible and attached to the visual tree to be dragged.");
    if( !_draggingControls.Add(control) )
      throw new InvalidOperationException(nameof(control) + " Control already added for dragging.");
    if( control.Bounds.Width <= 0 || control.Bounds.Height <= 0 ) {
      throw new InvalidOperationException("Control must have a non-zero size to be dragged.");
    }

    static RenderTargetBitmap BitmapRenderingWorkaround(Control control) {
      Rect originalBounds = control.Bounds;
      control.Arrange(new Rect(0, 0, originalBounds.Width, originalBounds.Height));
      var pixelSize = new PixelSize(( int ) control.Bounds.Width, ( int ) control.Bounds.Height);
      var bmp = new RenderTargetBitmap(pixelSize);
      bmp.Render(control);
      control.Arrange(originalBounds);
      return bmp;
    }

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

    var subRoot = FindSubRootControl(control, _root);
    if( subRoot is not Control c )
      throw new InvalidOperationException("Sub-root control must be a Control type.");
    var bmp = BitmapRenderingWorkaround(control);
    Image ghost = new Image {
      Width = control.Bounds.Width,
      Height = control.Bounds.Height,
      Source = bmp,
    };
    Canvas.SetLeft(ghost, c.Bounds.X);
    Canvas.SetTop(ghost, c.Bounds.Y);
    _ghostContainer.Children.Add(ghost);
    control.GetValue(DraggingServiceAttached.AllowDragProperty).Invoke(new DraggingServiceDragEventsArgs(e, _draggingControls));
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
    control.AddHandler(Control.PointerPressedEvent, StartControlDragging, RoutingStrategies.Bubble, false);
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
  public int DraggingControlsCount() {
    return _draggingControls.Count;
  }

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

  private void ClearDraggingControls() {
    foreach( Control ghost in _ghostContainer.Children ) {
      if( ghost is not Image ghostImage )
        throw new InvalidOperationException("Ghost container should only contain Image controls.");
      if( ghostImage.Source is IDisposable bitmap ) {
        ghostImage.Source = null;
        bitmap.Dispose();
      }
    }
    _draggingControls.Clear();
    _ghostContainer.Children.Clear();
  }

  /// <summary>
  /// Disposes the dragging service and releases its resources.
  /// </summary>
  public void Dispose() {
    if( _root.GetValue(DraggingServiceAttached.IsRootOfDraggingInstanceProperty) ) {
      Console.WriteLine("Trying to forcefully dispose a root element of the dragging service instance. Set IsRootOfDraggingInstanceProperty attached property to root instead ");
      return;
    }
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing) {
    static void CleanupControl(Control c, Action<object?, PointerPressedEventArgs> dragStartHandler) {
      if( c == null )
        return;
      c.RemoveHandler(Control.PointerPressedEvent, dragStartHandler);
      DraggingServiceAttached.CleanProperties(c);
      foreach( Visual child in c.GetVisualChildren() ) {
        if( child is Control childC ) {
          CleanupControl(childC, dragStartHandler);
        }
      }
    }
    if( _disposed )
      return;

    if( disposing ) {
      _root.RemoveHandler(Panel.PointerMovedEvent, Drag);
      _root.RemoveHandler(Panel.PointerReleasedEvent, EndDrag);
      _root.RemoveHandler(Control.PointerPressedEvent, StartControlDragging);
      foreach( Control c in _root.Children ) {
        CleanupControl(c, StartControlDragging);
      }
      ClearDraggingControls();
      _dropAllowedControlsSorted.Clear();
    }

    _disposed = true;
  }

  ~DraggingServiceInstance() {
    Dispose(false);
  }
}
