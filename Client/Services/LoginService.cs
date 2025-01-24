using De.Hsfl.LoomChat.Client.Global;
using De.Hsfl.LoomChat.Common.Dtos;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace De.Hsfl.LoomChat.Client.Services
{
    internal class LoginService
    {
        /// <summary>
        /// Führt ein Login gegen den Auth-Service durch
        /// und speichert die Daten (User, JWT) im SessionStore.
        /// </summary>
        public async Task<bool> Login(LoginRequest request)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var url = "http://localhost:5232/Auth/login";
                    var jsonContent = new StringContent(
                        JsonConvert.SerializeObject(request),
                        Encoding.UTF8,
                        "application/json"
                    );

                    var response = await client.PostAsync(url, jsonContent);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var responseObj = JsonConvert.DeserializeObject<LoginResponse>(responseBody);

                        // SessionStore aktualisieren
                        SessionStore.User = new Common.Models.User(responseObj.UserID, responseObj.Username)
                        {
                            Token = responseObj.Token
                        };
                        SessionStore.JwtToken = responseObj.Token;

                        return true;
                    }
                    else
                    {
                        MessageBox.Show("Login fehlgeschlagen. HTTP-" + response.StatusCode);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Login: {ex.Message}");
                    return false;
                }
            }
        }
    }
}
