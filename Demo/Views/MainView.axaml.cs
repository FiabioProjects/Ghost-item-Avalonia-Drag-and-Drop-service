using Avalonia.Controls;
using Avalonia.Interactivity;
using DraggingService;
using System.Diagnostics;

namespace AvaloniaApplication1.Demo.Views;

public partial class MainView: UserControl {
  public MainView() {
    InitializeComponent();

  }
  private void SetIsRootToFalse(object? sender, RoutedEventArgs e) {
    DraggingServiceAttached.SetIsRootOfDraggingInstance(MainGrid, false);
  }

  private void TextBlock_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e) {
    Debug.WriteLine("Textbloc pointer pressed");
  }
}