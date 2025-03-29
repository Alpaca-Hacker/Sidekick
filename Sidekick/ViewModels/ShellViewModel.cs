using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sidekick.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    // Tool ViewModels (inject later?)
    private readonly GuidGeneratorViewModel _guidGeneratorViewModel;
    
    [ObservableProperty]
    private ObservableObject? _currentToolViewModel;
    
    public ICommand NavigateToGuidGeneratorCommand { get; }
    public ICommand NavigateToBuildMonitorCommand { get; }
    
    public ShellViewModel(GuidGeneratorViewModel guidGeneratorViewModel)
    {
        _guidGeneratorViewModel = guidGeneratorViewModel;
             // _buildMonitorViewModel = buildMonitorViewModel;

             // Set up navigation commands
             NavigateToGuidGeneratorCommand = new RelayCommand(NavigateToGuid);
             NavigateToBuildMonitorCommand = new RelayCommand(NavigateToBuildMonitor, CanNavigateToBuildMonitor); // Example with CanExecute
        
             // Set the initial view
             CurrentToolViewModel = _guidGeneratorViewModel;
        }

        private void NavigateToGuid()
        {
            CurrentToolViewModel = _guidGeneratorViewModel;
    }
    
    private void NavigateToBuildMonitor()
    {
        // CurrentToolViewModel = _buildMonitorViewModel;
    }
    
    private bool CanNavigateToBuildMonitor()
    {
        //return _buildMonitorViewModel != null;
        return false;
    }

}