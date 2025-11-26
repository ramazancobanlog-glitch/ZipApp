using Microsoft.AspNetCore.SignalR;

namespace login.Hubs
{
    public class ChatHub : Hub
    {
        private static Dictionary<string, CustomerInfo> ConnectedCustomers = new();
        private static Dictionary<string, List<dynamic>> ChatHistory = new();

        public class CustomerInfo
        {
            public required string ConnectionId { get; set; }
            public required string CustomerName { get; set; }
            public required string IpAddress { get; set; }
            public DateTime ConnectedAt { get; set; }
            public bool IsTyping { get; set; }
            public bool HasUnreadMessages { get; set; }
        }

        public async Task SendMessageToAdmin(string message, string userName)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var customerId = Context.ConnectionId;

            // Müşterinin okunmamış mesaj flag'ini ayarla
            if (ConnectedCustomers.ContainsKey(customerId))
            {
                ConnectedCustomers[customerId].HasUnreadMessages = true;
            }

            // Chat geçmişine ekle
            if (!ChatHistory.ContainsKey(customerId))
            {
                ChatHistory[customerId] = new List<dynamic>();
            }
            ChatHistory[customerId].Add(new
            {
                SenderName = userName ?? "Ziyaretçi",
                Message = message,
                Timestamp = DateTime.Now,
                IsAdmin = false
            });

            await Clients.Group("admins").SendAsync("ReceiveCustomerMessage", new
            {
                message,
                userName = userName ?? "Ziyaretçi",
                customerId,
                timestamp,
                isAdmin = false
            });
        }

        public async Task SendMessageToCustomer(string message, string adminName, string customerId)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            var timestamp = DateTime.Now.ToString("HH:mm:ss");

            // Chat geçmişine ekle
            if (!ChatHistory.ContainsKey(customerId))
            {
                ChatHistory[customerId] = new List<dynamic>();
            }
            ChatHistory[customerId].Add(new
            {
                SenderName = adminName ?? "Destek Ekibi",
                Message = message,
                Timestamp = DateTime.Now,
                IsAdmin = true
            });

            // Müşteriye mesaj gönder
            await Clients.Client(customerId).SendAsync("ReceiveAdminMessage", new
            {
                message,
                adminName = adminName ?? "Destek Ekibi",
                timestamp,
                isAdmin = true
            });

            // Admin paneline de gönder (mesaj geçmişi için)
            await Clients.Group("admins").SendAsync("AdminMessageSent", new
            {
                message,
                adminName,
                customerId,
                timestamp
            });
        }

        // Chat geçmişini getir
        public async Task GetChatHistory(string customerId)
        {
            var messages = new List<dynamic>();
            
            if (ChatHistory.ContainsKey(customerId))
            {
                foreach (var msg in ChatHistory[customerId])
                {
                    messages.Add(new
                    {
                        msg.SenderName,
                        msg.Message,
                        msg.Timestamp,
                        msg.IsAdmin
                    });
                }
            }

            await Clients.Caller.SendAsync("LoadChatHistory", messages);
        }

        public async Task CustomerTyping(string userName)
        {
            var customerId = Context.ConnectionId;
            await Clients.Group("admins").SendAsync("ShowCustomerTyping", new
            {
                customerName = userName ?? "Müşteri",
                customerId
            });
        }

        public async Task AdminTyping(string adminName, string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId)) return;
            await Clients.Client(customerId).SendAsync("ShowAdminTyping", adminName);
        }

  public async Task StopTyping(string role)
{
    try
    {
        if (role == "admin")
        {
            // Admin yazmayı durdurduğunda tüm müşterilere bildir
            await Clients.All.SendAsync("StopTyping", "admin");
        }
        else
        {
            // Müşteri yazmayı durdurduğunda admin grubuna bildir
            await Clients.Group("admins").SendAsync("StopTyping", "customer");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("StopTyping hatası: " + ex.Message);
    }
}


        public override async Task OnConnectedAsync()
        {
            // Admin check - Context.User veya Session
            var isAdmin = Context.User?.Identity?.IsAuthenticated ?? false;
            
            // Eğer Context.User authenticated değilse, query string'den token veya başka yol check et
            if (!isAdmin)
            {
                var httpContext = Context.GetHttpContext();
                var adminParam = httpContext?.Request.Query["isAdmin"].ToString();
                isAdmin = adminParam == "true" || (httpContext?.Session?.GetString("IsAdmin") == "True");
            }
            
            // IP adresini al
            var ipAddress = Context.GetHttpContext()?.Connection?.RemoteIpAddress?.ToString();
            
            // IPv6'dan IPv4'e dönüş yapılabilir
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1")
            {
                ipAddress = "127.0.0.1"; // localhost
            }
            
            ipAddress = ipAddress ?? "Bilinmiyor";
            
            Console.WriteLine($"🔗 Bağlantı: {Context.ConnectionId}, IsAdmin: {isAdmin}, User: {Context.User?.Identity?.Name ?? "Anonymous"}");
            
            if (isAdmin)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
                Console.WriteLine($"✅ Admin bağlandı: {Context.ConnectionId}");
            }
            else
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "customers");

                var customerInfo = new CustomerInfo
                {
                    ConnectionId = Context.ConnectionId,
                    CustomerName = "Müşteri",
                    IpAddress = ipAddress,
                    ConnectedAt = DateTime.Now,
                    IsTyping = false,
                    HasUnreadMessages = false
                };

                ConnectedCustomers[Context.ConnectionId] = customerInfo;

                // Admin'lere müşteri bağlanma bildirimi gönder
                await Clients.Group("admins").SendAsync("CustomerConnected", new
                {
                    customerId = Context.ConnectionId,
                    customerName = customerInfo.CustomerName,
                    ipAddress = ipAddress,
                    connectedAt = customerInfo.ConnectedAt
                });

                Console.WriteLine($"✅ Müşteri bağlandı: {Context.ConnectionId} - IP: {ipAddress}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var customerId = Context.ConnectionId;

            if (ConnectedCustomers.ContainsKey(customerId))
            {
                ConnectedCustomers.Remove(customerId);
                await Clients.Group("admins").SendAsync("CustomerDisconnected", customerId);
                Console.WriteLine($"❌ Müşteri ayrıldı: {customerId}");
            }
            else
            {
                Console.WriteLine($"❌ Admin bağlantısı kesildi: {customerId}");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
