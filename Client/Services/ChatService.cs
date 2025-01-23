using De.Hsfl.LoomChat.Client.Global;
using De.Hsfl.LoomChat.Common.Dtos;
using De.Hsfl.LoomChat.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace De.Hsfl.LoomChat.Client.Services
{
    internal class ChatService
    {

        public async Task<List<ChannelDto>> LoadChannels(GetChannelsRequest request)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var url = "http://localhost:5115/Chat/channels";
                    var jsonContent = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, jsonContent);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var responseObj = JsonConvert.DeserializeObject<GetChannelsResponse>(responseBody);
                        return responseObj.Channels;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Fetchen der Channels: {ex.Message}");
                    return null;
                }
            }
        }

        public async Task<List<ChannelDto>> LoadDirectChannels(GetDirectChannelsRequest request)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var url = "http://localhost:5115/Chat/dms";
                    var jsonContent = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, jsonContent);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var responseObj = JsonConvert.DeserializeObject<GetDirectChannelsResponse>(responseBody);
                        return responseObj.Channels;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Fetchen der DM's: {ex.Message}");
                    return null;
                }
            }
        }

        public async Task<List<User>> LoadAllUsers(GetUsersRequest request)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var url = "http://localhost:5115/Chat/users";
                    var jsonContent = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, jsonContent);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var responseObj = JsonConvert.DeserializeObject<GetUsersResponse>(responseBody);
                        return responseObj.Users;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Fetchen der User: {ex.Message}");
                    return null;
                }
            }
        }

        public async Task<ChannelDto> OpenChatWithUser(OpenChatWithUserRequest request)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var url = "http://localhost:5115/Chat/openDm";
                    var jsonContent = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, jsonContent);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var responseObj = JsonConvert.DeserializeObject<OpenChatWithUserResponse>(responseBody);
                        return responseObj.Channel;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim öffnen des DmChats: {ex.Message}");
                    return null;
                }
            }
        }

        public async Task<ChannelDto> CreateNewChannel(CreateChannelRequest request)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var url = "http://localhost:5115/Chat/createChannel";
                    var jsonContent = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, jsonContent);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var responseObj = JsonConvert.DeserializeObject<CreateChannelResponse>(responseBody);
                        return responseObj.Channel;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Erstellen des Chats: {ex.Message}");
                    return null;
                }
            }
        }

        public async Task<ChannelDto> SendMessage(SendMessageRequest request)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var url = "http://localhost:5115/Chat/newMessage";
                    var jsonContent = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, jsonContent);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var responseObj = JsonConvert.DeserializeObject<SendMessageResponse>(responseBody);
                        return responseObj.Channel;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Erstellen des Chats: {ex.Message}");
                    return null;
                }
            }
        }
    }
}
