using Avalonia.Controls;
using VolumetricSelection2077.ViewModels;

namespace VolumetricSelection2077.Views;

public partial class BenchmarkResultComparer : Window
{
    private BenchmarkResultComparerViewModel ViewModel;
    
    public BenchmarkResultComparer()
    {
        InitializeComponent();
        
        DataContext = new BenchmarkResultComparerViewModel();
        ViewModel = (DataContext as BenchmarkResultComparerViewModel)!;
    }

    private void ResultComboBoxLeft_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox cb)
            return;

        ViewModel.SetBenchmarkResult(BenchmarkResultSide.Left, (string)cb.SelectedValue!);
    }
    
    private void ResultComboBoxRight_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox cb)
            return;

        ViewModel.SetBenchmarkResult(BenchmarkResultSide.Right, (string)cb.SelectedValue!);
    }
}