using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ReMealApp.ViewModels;
using System;

namespace ReMealApp
{
    public class ViewLocator : IDataTemplate
    {
        public Control? Build(object? param)
        {
            if (param is null)
                return null;

            var viewName = param.GetType().FullName!
                .Replace("ViewModel", "View", StringComparison.Ordinal)
                .Replace(".ViewModels.", ".Views.", StringComparison.Ordinal);

            var type = Type.GetType(viewName);
            if (type is not null)
                return (Control)Activator.CreateInstance(type)!;

            return new TextBlock { Text = $"View not found: {viewName}" };
        }

        public bool Match(object? data) => data is ViewModelBase;
    }
}
