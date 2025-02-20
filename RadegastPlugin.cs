using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Radegast;
using Radegast.Plugin;
using OpenMetaverse;

public class WebApiPlugin : IRadegastPlugin
{
    private RadegastInstance instance;
    private HttpListener listener;
    private List<string> chatMessages = new List<string>();
    private List<string> imMessages = new List<string>();

    public void StartPlugin(RadegastInstance instance)
    {
        this.instance = instance;
        listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/");
        listener.Start();
        Task.Run(() => HandleRequests());

        instance.Client.Self.ChatFromSimulator += Self_ChatFromSimulator;
        instance.Client.Self.IM += Self_IM;
    }

    public void StopPlugin(RadegastInstance instance)
    {
        listener.Stop();
        instance.Client.Self.ChatFromSimulator -= Self_ChatFromSimulator;
        instance.Client.Self.IM -= Self_IM;
    }

    private void Self_ChatFromSimulator(object sender, ChatEventArgs e)
    {
        chatMessages.Add($"{e.FromName}: {e.Message}");
    }

    private void Self_IM(object sender, InstantMessageEventArgs e)
    {
        imMessages.Add($"{e.IM.FromAgentName}: {e.IM.Message}");
    }

    private async Task HandleRequests()
    {
        while (listener.IsListening)
        {
            var context = await listener.GetContextAsync();
            var response = context.Response;

            string responseString = ProcessRequest(context.Request);
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }

    private string ProcessRequest(HttpListenerRequest request)
    {
        string path = request.Url.AbsolutePath;
        switch (path)
        {
            case "/login":
                return HandleLogin(request);
            case "/sendchat":
                return HandleSendChat(request);
            case "/getchat":
                return HandleGetChat();
            case "/sendim":
                return HandleSendIM(request);
            case "/getim":
                return HandleGetIM();
            case "/friends":
                return HandleFriends();
            default:
                return "Invalid endpoint";
        }
    }

    private string HandleLogin(HttpListenerRequest request)
    {
        // Implement login logic here
        // Example: instance.Login(username, password)
        return "Login successful";
    }

    private string HandleSendChat(HttpListenerRequest request)
    {
        using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
        {
            var json = reader.ReadToEnd();
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            instance.Client.Self.Chat(data["message"], 0, ChatType.Normal);
        }
        return "Chat sent";
    }

    private string HandleGetChat()
    {
        var messages = string.Join("\n", chatMessages);
        chatMessages.Clear();
        return messages;
    }

    private string HandleSendIM(HttpListenerRequest request)
    {
        using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
        {
            var json = reader.ReadToEnd();
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            var target = new UUID(data["target"]);
            instance.Client.Self.InstantMessage(target, data["message"]);
        }
        return "IM sent";
    }

    private string HandleGetIM()
    {
        var messages = string.Join("\n", imMessages);
        imMessages.Clear();
        return messages;
    }

    private string HandleFriends()
    {
        var friendList = new List<string>();
        foreach (var friend in instance.Client.Friends.FriendList)
        {
            friendList.Add(friend.Value.Name);
        }
        return Newtonsoft.Json.JsonConvert.SerializeObject(friendList);
    }
}