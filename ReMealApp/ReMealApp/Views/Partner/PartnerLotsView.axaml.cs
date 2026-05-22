using Avalonia.Controls;
using Avalonia.Interactivity;
using ReMealApp.ViewModels.Partner;

namespace ReMealApp.Views.Partner
{
    public partial class PartnerLotsView : UserControl
    {
        public PartnerLotsView()
        {
            InitializeComponent();
        }

        private async void LotRow_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not PartnerLotsViewModel viewModel ||
                sender is not Control { Tag: PartnerLotListItemViewModel lot })
            {
                return;
            }

            if (viewModel.OpenDetailsCommand.CanExecute(lot))
                await viewModel.OpenDetailsCommand.ExecuteAsync(lot);
        }
    }
}
