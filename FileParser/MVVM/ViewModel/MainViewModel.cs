using CommunityToolkit.Mvvm.ComponentModel;

namespace FileParser.MVVM.ViewModel
{
    public class MainViewModel : ObservableObject
    {
        private object? _currentView;

        public object? CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public MainViewModel()
        {
            CurrentView = new ParserViewModel();
        }
    }
}