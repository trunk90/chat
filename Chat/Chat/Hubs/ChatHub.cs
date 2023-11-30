using Microsoft.AspNetCore.SignalR;
using SignalRChat.Classes;
using SignalRChat.Hubs;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ChatHub : Hub<IChat>
{
    public static List<Connection> conexiones { get; set; } = new List<Connection>();
    private static List<string> palabrasProhibidas = new List<string> { "tonto", "feo", "capullo" };

    public async Task SendMessage(Message message)
    {
        if (!string.IsNullOrEmpty(message.Text))
        {
            if (ContienePalabraProhibida(message.Text))
            {
                string mensajeAnulado = $"Al usuario {message.User} se le ha anulado un mensaje por vocabulario inapropiado";
                await Clients.Client(Context.ConnectionId).GetMessage(new Message() { User = "Sistema", Text = mensajeAnulado });

                // Notificar al cliente que reproduzca el sonido
                await Clients.Client(Context.ConnectionId).SonidoPalabrota();
            }
            else
            {
                // Transmitir el mensaje al cliente y a todos los clientes conectados en la sala
                await Clients.Group(message.Room).GetMessage(message);
            }
        }
        else if (!string.IsNullOrEmpty(message.User))
        {
            conexiones.Add(new Connection { Id = Context.ConnectionId, User = message.User, Avatar = message.Avatar, Room = message.Room });
            await Groups.AddToGroupAsync(Context.ConnectionId, message.Room);
            await Clients.Group(message.Room).GetMessage(new Message() { User = message.User, Text = " se ha conectado!", Avatar = message.Avatar });
            await Clients.All.GetUsers(conexiones);
        }
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Client(Context.ConnectionId).GetMessage(new Message() { User = "Host", Text = "Hola, Bienvenido al Chat" });
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var conexion = conexiones.FirstOrDefault(x => x.Id == Context.ConnectionId);

        if (conexion != null)
        {
            await Clients.GroupExcept(conexion.Room, Context.ConnectionId).GetMessage(new Message() { User = "Host", Text = $"{conexion.User} ha salido del chat" });
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conexion.Room);
            conexiones.Remove(conexion);
            await Clients.All.GetUsers(conexiones);
        }
       
        await base.OnDisconnectedAsync(exception);
    }

    private bool ContienePalabraProhibida(string mensaje)
    {
        foreach (var palabraProhibida in palabrasProhibidas)
        {
            if (mensaje.Contains(palabraProhibida, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
