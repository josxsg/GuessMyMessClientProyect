using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using GuessMyMessClient.ViewModel.Support.Navigation;

namespace GuessMyMessClient.ViewModel.Support
{
    public static class ServiceLocator
    {
        public static INavigationService Navigation { get; set; }
    }
}
