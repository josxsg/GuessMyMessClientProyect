using GuessMyMessClient.Model;
using GuessMyMessClient.ProfileService; 
using GuessMyMessClient.Properties.Langs;
using GuessMyMessClient.ViewModel.Session;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GuessMyMessClient.ViewModel.Lobby
{
    public class RankingViewModel : ViewModelBase
    {
        public ObservableCollection<RankingModel> RankingList { get; set; }

        public ICommand RefreshCommand { get; }

        public RankingViewModel()
        {
            RankingList = new ObservableCollection<RankingModel>();
            RefreshCommand = new RelayCommand((o) => Task.Run(() => LoadRankingAsync()));

            Task.Run(() => LoadRankingAsync());
        }

        private async Task LoadRankingAsync()
        {
            try
            {
                var client = new UserProfileServiceClient();
                var result = await client.GetGlobalRankingAsync();
                client.Close();

                string currentUser = SessionManager.Instance.CurrentUsername;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    RankingList.Clear();
                    if (result != null)
                    {
                        foreach (var item in result)
                        {
                            RankingList.Add(new RankingModel
                            {
                                Rank = item.Rank ?? 0,
                                Username = item.Username,
                                TotalScore = item.Score,
                                IsCurrentUser = item.Username == currentUser
                            });
                        }
                    }
                });
            }
            catch (Exception)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Lang.rankingErrorLoading, Lang.alertErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }
    }
}