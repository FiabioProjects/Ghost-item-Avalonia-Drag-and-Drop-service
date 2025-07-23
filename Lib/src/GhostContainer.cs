using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;

namespace DraggingService;
internal class GhostContainer: Canvas {
  private readonly HashSet<Control> _draggingControls = [];
  public IReadOnlyCollection<Control> DraggingControls => _draggingControls;

  private Point _lastAddedAt = new Point(0, 0); // This is used to store the last position where a control was added to the ghost container
  protected override void ArrangeCore(Rect finalRect) {
    base.ArrangeCore(new Rect(0, 0, Width, Height));  //Width and Height are set by the DraggingServiceInstance to match exactly the size of the root
  }
  internal void UpdateSize(Avalonia.Size newSize) { //this is called by the DraggingServiceInstance to update the size of the ghost container to match the size of the root
    Width = newSize.Width;
    Height = newSize.Height;
  }

  internal void AddChild(Control child, Avalonia.Point point) {
    if( _draggingControls.Add(child) ) {
      //if the child is not already added
      static RenderTargetBitmap BitmapRenderingWorkaround(Control control) {
        Rect originalBounds = control.Bounds;
        control.Arrange(new Rect(0, 0, originalBounds.Width, originalBounds.Height));
        var pixelSize = new PixelSize(( int ) control.Bounds.Width, ( int ) control.Bounds.Height);
        var bmp = new RenderTargetBitmap(pixelSize);
        bmp.Render(control);
        control.Arrange(originalBounds);
        return bmp;
      }
      var bmp = BitmapRenderingWorkaround(child);
      Image ghost = new Image {
        Width = child.Bounds.Width,
        Height = child.Bounds.Height,
        Source = bmp,
      };
      Canvas.SetLeft(ghost, point.X);
      Canvas.SetTop(ghost, point.Y);
      _lastAddedAt = point;
      Children.Add(ghost);

    } else {
      throw new InvalidOperationException("Control already added to the ghost container.");
    }
  }
  internal void ClearDraggingControls() {
    foreach( Control ghost in Children ) {
      if( ghost is not Image ghostImage )
        throw new InvalidOperationException("Ghost container should only contain Image controls.");
      if( ghostImage.Source is IDisposable bitmap ) {
        ghostImage.Source = null;
        bitmap.Dispose();
      }
    }
    _draggingControls.Clear();
    Children.Clear();
  }

  internal void Render(Avalonia.Size rootSize, Avalonia.Point ptrPos) {
    Width = rootSize.Width;
    Height = rootSize.Height;
    RenderTransform = new TranslateTransform(ptrPos.X - _lastAddedAt.X, ptrPos.Y - _lastAddedAt.Y); // Center the ghost container on the pointer position
  }
}

