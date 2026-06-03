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
  private void ToggleSelectionT2(object? sender, RoutedEventArgs e) {
    DraggingServiceAttached.SetIsSelectedForMultiDrag(T2, !DraggingServiceAttached.GetIsSelectedForMultiDrag(T2));
  }
  private void TextBlock_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e) {
    Debug.WriteLine("Textbloc pointer pressed");
  }

  private void TextBlock_PointerPressed_1(object? sender, Avalonia.Input.PointerPressedEventArgs e) {
    Debug.WriteLine("pointer pressed on ", sender);
  }
  private void StackPanel_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e) {
    Debug.WriteLine("pointer Released on ", sender);
  }
}