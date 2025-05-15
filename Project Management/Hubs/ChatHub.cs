using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Project_Management.Data;
using Project_Management.Models;
using Project_Management.Models.DTO;
using System.Security.Claims;

namespace Project_Management.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ChatHub(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task SendMessage(string senderId, string receiverId, string message)
        {
            var id = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Name); 

            var sender = await _db.applicationUsers
                .FirstOrDefaultAsync(x => x.Id == senderId);

            if (sender == null)
            {
                await Clients.Caller.SendAsync("ChatError", "Unauthorized");
                return;
            }
            var receiver = await _db.applicationUsers
                .FirstOrDefaultAsync(x => x.Id == receiverId);
            if (receiver == null)
            { 
                await Clients.Caller.SendAsync("GetMessagesError", "Can not find the receiver");
                return;
            }
            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Message = message,
            };
            _db.chatMessages.Add(chatMessage);
            await _db.SaveChangesAsync();
            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message);
        }
        public async Task GetMessages(string receiverUserId)
        {
            var senderUserId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Name);
            var messages = _db.chatMessages
                .Where(m => m.ReceiverId == receiverUserId && m.SenderId == senderUserId)
                .OrderByDescending(m => m.CreatedAt)
                .ToList();
            var unreadMessages =await _db.chatMessages
                .Where(m => m.SenderId == receiverUserId && m.ReceiverId == senderUserId && !m.IsRead)
                .ToListAsync();

            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }
            await _db.SaveChangesAsync();

            await Clients.User(senderUserId).SendAsync("GetMessages", messages);
        }
        public async Task LoadChatHome()
        {
            var userid = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrEmpty(userid))
            {
                await Clients.Caller.SendAsync("ChatHomeError", "Unauthorized");
                return;
            }

            var messages = await _db.chatMessages
                .Where(x => x.SenderId == userid || x.ReceiverId == userid)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
            List<HomeChatDTO> UsersDTO = new List<HomeChatDTO>();
            foreach (var message in messages)
            {
                var userDTO = new HomeChatDTO();
                if (userid == message.SenderId)
                {
                    var user = await _db.applicationUsers.FirstOrDefaultAsync(x => x.Id == message.ReceiverId);
                    userDTO.Id = user.Id;
                    userDTO.UserName = user.UserName;
                    userDTO.Message = message.Message;
                    UsersDTO.Add(userDTO);
                }
                else if (userid == message.ReceiverId)
                {
                    var user = await _db.applicationUsers.FirstOrDefaultAsync(x => x.Id == message.SenderId);
                    userDTO.Id = user.Id;
                    userDTO.UserName = user.UserName;
                    userDTO.Message = message.Message;
                    UsersDTO.Add(userDTO);
                }
            }
            UsersDTO = UsersDTO.GroupBy(u => u.Id).Select(g => g.First()).ToList();
            await Clients.Caller.SendAsync("ReceiveChatHome", UsersDTO);
        }
    }
}
