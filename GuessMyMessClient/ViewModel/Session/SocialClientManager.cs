using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using GuessMyMessClient.SocialService;
using System.Windows;

namespace GuessMyMessClient.ViewModel.Session
{
    public class SocialClientManager : ISocialServiceCallback
    {
        private const string SOCIAL_SERVICE_ENDPOINT_NAME = "NetTcpBinding_ISocialService";
        private SocialServiceClient _client;

        private static readonly Lazy<SocialClientManager> _instance =
            new Lazy<SocialClientManager>(() => new SocialClientManager());
        public static SocialClientManager Instance => _instance.Value;

        public SocialServiceClient Client => _client;

        public event Action<string> OnFriendRequest;
        public event Action<string, bool> OnFriendResponse;
        public event Action<string, string> OnFriendStatusChanged;
        public event Action<DirectMessageDto> OnMessageReceived;

        private SocialClientManager() { } 

        public void Initialize()
        {
            if (_client != null && _client.State == CommunicationState.Opened)
            {
                Console.WriteLine("SocialClientManager ya inicializado.");
                return;
            }

            try
            {
                _client = new SocialServiceClient(new InstanceContext(this), SOCIAL_SERVICE_ENDPOINT_NAME);
                _client.Open();

                if (_client.State == CommunicationState.Opened)
                {
                    _client.Connect(SessionManager.Instance.CurrentUsername);
                    Console.WriteLine($"SocialClientManager inicializado y conectado como {SessionManager.Instance.CurrentUsername}.");
                }
                else
                {
                    Console.WriteLine($"SocialClientManager falló al abrir el canal. Estado: {_client.State}");
                    throw new InvalidOperationException($"No se pudo abrir el canal WCF. Estado: {_client.State}");
                }

            }
            catch (InvalidOperationException ioEx) 
            {
                MessageBox.Show($"Error CRÍTICO al conectar con SocialService: Endpoint '{SOCIAL_SERVICE_ENDPOINT_NAME}' no encontrado o contrato incorrecto en App.config.\n\nDetalles: {ioEx.Message}", "Error de Configuración WCF", MessageBoxButton.OK, MessageBoxImage.Error);
                CloseClientSafely(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar SocialClientManager: {ex.Message}", "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
                CloseClientSafely(); 
            }
        }

        public void Cleanup()
        {
            Console.WriteLine("Iniciando Cleanup de SocialClientManager...");
            if (_client != null && _client.State == CommunicationState.Opened)
            {
                try
                {
                    Console.WriteLine($"Intentando desconectar a {SessionManager.Instance.CurrentUsername}...");
                    if (!string.IsNullOrEmpty(SessionManager.Instance.CurrentUsername))
                    {
                        _client.Disconnect(SessionManager.Instance.CurrentUsername);
                        Console.WriteLine("Disconnect llamado (OneWay, servidor procesará).");
                    }
                    else { Console.WriteLine("Username vacío, no se puede llamar a Disconnect."); }
                }
                catch (CommunicationException commEx) 
                {
                    Console.WriteLine($"Error de comunicación durante Disconnect (puede ser normal si ya estaba cerrado): {commEx.Message}");
                }
                catch (Exception ex) 
                {
                    Console.WriteLine($"Error inesperado durante Disconnect: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Cliente no estaba abierto ({_client?.State}). Saltando Disconnect.");
            }

            CloseClientSafely();
            Console.WriteLine("Cleanup de SocialClientManager completado.");
        }

        private void CloseClientSafely()
        {
            if (_client == null) return;
            Console.WriteLine($"Cerrando cliente WCF. Estado actual: {_client.State}");
            try
            {
                if (_client.State != CommunicationState.Faulted && _client.State != CommunicationState.Closed)
                {
                    _client.Close();
                    Console.WriteLine("Cliente WCF cerrado correctamente.");
                }
                else
                {
                    _client.Abort();
                    Console.WriteLine("Cliente WCF abortado (estaba Faulted o Closed).");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Excepción al cerrar/abortar cliente WCF: {ex.Message}. Abortando...");
                _client.Abort(); 
            }
            finally 
            {
                _client = null;
            }
        }


        public void NotifyFriendRequest(string fromUsername)
        {
            Console.WriteLine($"Callback recibido: NotifyFriendRequest de {fromUsername}");
            OnFriendRequest?.Invoke(fromUsername);
        }

        public void NotifyFriendResponse(string fromUsername, bool accepted)
        {
            Console.WriteLine($"Callback recibido: NotifyFriendResponse de {fromUsername}, Aceptado: {accepted}");
            OnFriendResponse?.Invoke(fromUsername, accepted);
        }

        public void NotifyFriendStatusChanged(string friendUsername, string status)
        {
            Console.WriteLine($"Callback recibido: NotifyFriendStatusChanged para {friendUsername}, Estado: {status}");
            OnFriendStatusChanged?.Invoke(friendUsername, status);
        }

        public void NotifyMessageReceived(DirectMessageDto message)
        {
            Console.WriteLine($"Callback recibido: NotifyMessageReceived de {message?.SenderUsername}");
            if (message != null) 
            {
                OnMessageReceived?.Invoke(message);
            }
            else
            {
                Console.WriteLine("Error: NotifyMessageReceived recibió un mensaje nulo.");
            }
        }
    }
}
