namespace ReMealApp.ViewModels.Shell
{
    public sealed class ModulePlaceholderViewModel : ViewModelBase
    {
        public ModulePlaceholderViewModel(string title, string message, string iconPath)
        {
            Title = title;
            Message = message;
            IconPath = iconPath;
        }

        public string Title { get; }

        public string Message { get; }

        public string IconPath { get; }
    }
}
