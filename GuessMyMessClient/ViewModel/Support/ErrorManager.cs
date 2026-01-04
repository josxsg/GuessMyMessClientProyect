using GuessMyMessClient.AuthService;
using GuessMyMessClient.Properties.Langs;

namespace GuessMyMessClient.ViewModel.Support
{
    public static class ErrorManager
    {
        public static (string Title, string Message) GetErrorMessage(ServiceErrorType errorType)
        {
            switch (errorType)
            {
                case ServiceErrorType.None:
                    return (Lang.alertSuccessTitle, "Success");
                case ServiceErrorType.DatabaseError:
                    return (Lang.alertErrorTitle, Lang.alertServerErrorMessage);
                case ServiceErrorType.ConnectionTimeout:
                    return (Lang.alertConnectionErrorTitle, Lang.alertConnectionErrorMessage);
                case ServiceErrorType.OperationFailed:
                    return (Lang.alertErrorTitle, Lang.alertUnknownErrorMessage);
                case ServiceErrorType.InvalidCredentials:
                    return (Lang.alertLoginErrorTitle, Lang.alertInvalidUsernameOrPassword);
                case ServiceErrorType.AccountNotVerified:
                    return (Lang.alertVerificationErrorTitle, Lang.alertVerificationErrorTitle);
                case ServiceErrorType.UserAlreadyExists:
                case ServiceErrorType.DuplicateRecord: 
                    return (Lang.alertRegistrationErrorTitle, Lang.alertUserAlreadyExists);
                case ServiceErrorType.EmailAlreadyRegistered:
                    return (Lang.alertRegistrationErrorTitle, Lang.alertEmailAlreadyExists);
                case ServiceErrorType.LobbyFull:
                    return (Lang.alertMatchmakingError, Lang.alertCannotJoinMatch);
                case ServiceErrorType.GameInProgress:
                    return (Lang.alertMatchmakingError, Lang.alertCannotJoinMatch);
                case ServiceErrorType.NotFound:
                    return (Lang.alertErrorTitle, Lang.alertPrivateMatchesErrorNoCode);
                case ServiceErrorType.Unknown:
                default:
                    return (Lang.alertErrorTitle, Lang.alertUnknownErrorMessage);
            }
        }
    }
}