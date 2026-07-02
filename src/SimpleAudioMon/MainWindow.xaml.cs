using System.Windows;
using SimpleAudioMon.ViewModels;

namespace SimpleAudioMon;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => ViewModel.Start();
        Closed += (_, _) => ViewModel.Dispose();
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;
}
