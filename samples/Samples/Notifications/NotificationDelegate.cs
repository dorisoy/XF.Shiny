using System;
using System.Threading.Tasks;
using Samples.Models;
using Shiny;
using Shiny.Notifications;


namespace Samples.Notifications
{
    public class NotificationDelegate : INotificationDelegate
    {
        readonly SampleSqliteConnection conn;
        readonly IMessageBus messageBus;
        readonly INotificationManager notifications;


        public NotificationDelegate(SampleSqliteConnection conn,
                                    INotificationManager notifications,
                                    IMessageBus messageBus)
        {
            this.conn = conn;
            this.notifications = notifications;
            this.messageBus = messageBus;
        }


        public async Task OnEntry(NotificationResponse response)
        {
            var @event = new NotificationEvent
            {
                NotificationId = response.Notification.Id,
                NotificationTitle = response.Notification.Title ?? response.Notification.Message,
                Action = response.ActionIdentifier,
                ReplyText = response.Text,
                Timestamp = DateTime.Now
            };
            await this.conn.InsertAsync(@event);
            this.messageBus.Publish(@event);

            await this.DoChat(response);
        }


        async Task DoChat(NotificationResponse response)
        {
            var cat = response.Notification.Channel;
            if (cat?.StartsWith("Chat") ?? false)
            {
                cat = cat.Replace("Chat", String.Empty).ToLower();
                switch (cat)
                {
                    //名称
                    case "name":
                        var name = "czhcom@163";
                        if (!response.Text.IsEmpty())
                            name = response.Text!.Trim();

                        await notifications.Send(
                            "聊天室",
                            $"嗨 {name}, 你喜欢我吗?",
                            "ChatAnswer"
                        );
                        break;
                        //回答
                    case "answer":
                        switch (response.ActionIdentifier?.ToLower())
                        {
                            case "是":
                                await this.notifications.Send("聊天室", "耶!!");
                                break;

                            case "否":
                                await this.notifications.Send("聊天室", "那就离开吧!");
                                break;
                        }
                        break;
                }
            }
        }
    }
}
