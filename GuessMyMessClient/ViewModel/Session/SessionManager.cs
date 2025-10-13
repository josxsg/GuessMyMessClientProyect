using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuessMyMessClient.ViewModel.Session
{
    public class SessionManager : ViewModelBase
    {
        // Instancia Singleton
        private static SessionManager _instance;
        public static SessionManager Instance => _instance ?? (_instance = new SessionManager());

        // Propiedad que contiene el username del usuario logueado
        private string _currentUsername;
        public string CurrentUsername
        {
            get => _currentUsername;
            set { _currentUsername = value; OnPropertyChanged(); }
        }

        // Propiedad booleana para saber si hay sesión activa
        public bool IsLoggedIn => !string.IsNullOrEmpty(CurrentUsername);

        private SessionManager() { }

        public void StartSession(string username)
        {
            CurrentUsername = username;
            // Opcional: Cargar datos de perfil aquí
        }

        public void CloseSession()
        {
            CurrentUsername = null;
        }
    }
}
