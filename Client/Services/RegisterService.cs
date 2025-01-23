using De.Hsfl.LoomChat.Client.Global;
using De.Hsfl.LoomChat.Common.Dtos;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace De.Hsfl.LoomChat.Client.Services
{
    internal class RegisterService
    {

        public async Task<bool> Register(RegisterRequest request)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var url = "http://localhost:5232/Auth/register";
                    var jsonContent = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, jsonContent);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var responseObj = JsonConvert.DeserializeObject<RegisterResponse>(responseBody);
                        SessionStore.User = new Common.Models.User(responseObj.UserID, responseObj.Username)
                        {
                            Token = responseObj.Token
                        };
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Registrieren: {ex.Message}");
                    return false;
                }
            }
        }
    }
}
