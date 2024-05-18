
using Firebase.Auth;
using Firebase.Storage;
using Microsoft.Extensions.Options;

namespace InventarioPro.Frontend.Services
{
    public class ServicioImagen : IServicioImagen
    {
        private readonly Firebase _credentials;

        public ServicioImagen(IOptions<Firebase> credentials)
        {
            _credentials = credentials.Value;
        }

        public async Task<string> SubirImagen(Stream archivo, string nombre)
        {
            string email = _credentials.Email;
            string clave = _credentials.Clave;
            string ruta = _credentials.Ruta;
            string api_key = _credentials.ApiKey;

            var auth = new FirebaseAuthProvider(new FirebaseConfig(api_key));
            var a = await auth.SignInWithEmailAndPasswordAsync(email, clave);

            var cancellation = new CancellationTokenSource();

            var task = new FirebaseStorage(
              ruta,
              new FirebaseStorageOptions
              {
                  AuthTokenAsyncFactory = () => Task.FromResult(a.FirebaseToken),
                  ThrowOnCancel = true
              })

               .Child("Fotos_Perfil")
               .Child(nombre)
               .PutAsync(archivo, cancellation.Token);

            var downloadURL = await task;
            return downloadURL;
        }
    }
}
