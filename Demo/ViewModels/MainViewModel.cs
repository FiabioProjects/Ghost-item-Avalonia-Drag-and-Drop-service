using DraggingService;
using System.Diagnostics;

namespace AvaloniaApplication1.Demo.ViewModels;

public partial class MainViewModel {
  public string Greeting => "Welcome to Avalonia!";

  public static DraggingServiceDropEvent DropCallback => ( args => { Debug.WriteLine("dropping callback Mainviewmodel"); } );
  public static DraggingServiceDragEvent DragCallback => ( args => { Debug.WriteLine("dragging callback Mainviewmodel"); } );
}
