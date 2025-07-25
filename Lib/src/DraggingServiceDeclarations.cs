using Avalonia.Controls;
using Avalonia.Input;
using System.Collections.Generic;

namespace DraggingService;

/// <summary>
/// Contains event data for a drop action, including pointer info, dragged controls, and the drop target.
/// </summary>
/// <param name="pointerEvent">The pointer event that triggered the drop.</param>
/// <param name="draggedControls">The collection of controls being dragged.</param>
/// <param name="dropTarget">The control where the drop occurred.</param>
public record DraggingServiceDropEventsArgs(PointerEventArgs pointerEvent, IReadOnlyCollection<Control> draggedControls, Control dropTarget) {
  /// <summary>
  /// The pointer event that triggered the drop.
  /// </summary>
  public readonly PointerEventArgs PointerEvent = pointerEvent;

  /// <summary>
  /// The controls that were dragged during the operation.
  /// </summary>
  public readonly IReadOnlyCollection<Control> DraggedControls = draggedControls;

  /// <summary>
  /// The control that accepted the drop.
  /// </summary>
  public readonly Control DropTarget = dropTarget;
}

/// <summary>
/// Contains event data for a drag action, including pointer info and dragged controls.
/// </summary>
/// <param name="pointerEvent">The pointer event that triggered the drag.</param>
/// <param name="draggedControls">The collection of controls being dragged.</param>
public record DraggingServiceDragEventsArgs(PointerEventArgs pointerEvent, IReadOnlyCollection<Control> draggedControls) {
  /// <summary>
  /// The pointer event that initiated or continues the drag.
  /// </summary>
  public readonly PointerEventArgs PointerEvent = pointerEvent;

  /// <summary>
  /// The controls that are currently being dragged.
  /// </summary>
  public readonly IReadOnlyCollection<Control> DraggedControls = draggedControls;
}
/// <summary>
/// Contains event data for a selection action, i.e. the control being selected or unselected.
/// You can use <c>DraggingServiceAttached.IsSelectedForMultiDragProperty</c> to check the actual selection state of the control.
/// </summary>
/// <param name="element">The Control which selection state  has changed.</param>
public record DraggingServiceSelectionEventArgs(Control element) {
  public readonly Control Element = element;
}

/// <summary>
/// Wraps a DraggingServiceInstance with its depth from the root control.
/// </summary>
public class DraggingServiceInstanceWrapper {
  /// <summary>
  /// The instance of the dragging service.
  /// </summary>
  public readonly DraggingServiceInstance Instance;

  /// <summary>
  /// The depth of the associated control from the dragging root.
  /// </summary>
  public readonly int DepthFromRoot;


  /// <summary>
  /// Initializes a new instance of the <see cref="DraggingServiceInstanceWrapper"/> class.
  /// </summary>
  /// <param name="instance">The dragging service instance.</param>
  /// <param name="depthFromRoot">The depth of the instance's root panel from the root of the visual tree.</param>
  public DraggingServiceInstanceWrapper(DraggingServiceInstance instance, int depthFromRoot) {
    Instance = instance;
    DepthFromRoot = depthFromRoot;
  }


}

/// <summary>
/// Delegate representing the callback method for a drop event.
/// </summary>
/// <param name="args">The drop event arguments.</param>
public delegate void DraggingServiceDropEvent(DraggingServiceDropEventsArgs args);

/// <summary>
/// Delegate representing the callback method for a drag event.
/// </summary>
/// <param name="args">The drag event arguments.</param>
public delegate void DraggingServiceDragEvent(DraggingServiceDragEventsArgs args);

/// <summary>
/// Delegate representing the callback method for a multi drag selection event.
/// </summary>
/// <param name="args">The drag event arguments.</param>
public delegate void DraggingServiceSelectionEvent(DraggingServiceSelectionEventArgs args);