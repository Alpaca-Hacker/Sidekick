using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sidekick.ViewModels;

public partial class GuidGeneratorViewModel : ObservableObject
{
    private bool _guidGenerated = false;
    [ObservableProperty]
    private string _generatedGuid = "(Click 'Generate' to create a new GUID)";
    
    [RelayCommand]
    private void Generate()
    {
        _guidGenerated = true;
        GeneratedGuid = Guid.NewGuid().ToString();
    }
    
    [RelayCommand]
    private void CopyToClipboard()
    {
        if (!_guidGenerated)
        {
            return;
        }
        
        try
        {
            Clipboard.SetText(GeneratedGuid);
                    // Add user feedback (e.g., temporary message)
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error copying GUID to clipboard: {ex.Message}", "Clipboard Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}