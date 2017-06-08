using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vk10pvbot
{
    /// <summary>
    /// Convert users vk messages to commands
    /// </summary>
    public class vk_commands
    {
        vk_connector connector;
        long recived_ticks;
        DateTime last_processed_recived_message_date;
        long send_ticks;
        DateTime last_processed_send_message_date;

        public vk_commands(vk_connector connector)
        {
            this.connector = connector;
            var date = DateTime.Now;
            recived_ticks = date.Ticks;
            last_processed_recived_message_date = date;
            send_ticks = date.Ticks;
            last_processed_send_message_date = date;
        }

        public List<VkNet.Model.Message> commands()
        {
            var recived_messages = messages(connector,
                ref recived_ticks,
                ref last_processed_recived_message_date,
                VkNet.Enums.MessageType.Received);
            recived_messages.Reverse();

            var recived_commands = commands(recived_messages);

            var send_messages = messages(connector,
                    ref send_ticks,
                    ref last_processed_send_message_date,
                    VkNet.Enums.MessageType.Sended);
            send_messages.Reverse();

            var send_commands = commands(send_messages);

            send_commands.AddRange(recived_commands);
            send_commands.Sort((x, y) => x.Date.Value.Ticks < y.Date.Value.Ticks ? 0 : 1);

            return send_commands;
        }

        private List<VkNet.Model.Message> messages(vk_connector connector,
          ref long ticks,
          ref DateTime last_processed_message_date,
          VkNet.Enums.MessageType messagetype)
        {
            var date = DateTime.Now;
            var timeoffset = (ulong)TimeSpan.FromTicks((date.Ticks - ticks)).TotalSeconds;

            var messsages = connector.messages(timeoffset, messagetype);
            if (messsages == null)
            {
                return null;
            }
            if (messsages.Count > 0)
            {
                ticks = messsages[0].Date.Value.Ticks;
            }

            for (int i = messsages.Count - 1; i > -1; i--)
            {
                if (messsages[i].Date.Value.Ticks <= last_processed_message_date.Ticks)
                {
                    messsages.RemoveAt(i);
                }
                else
                {
                    break;
                }
            }
            if (messsages.Count > 0)
            {
                ticks = messsages[0].Date.Value.Ticks;
                last_processed_message_date = messsages[0].Date.Value;
            }
            return messsages;
        }

        private List<VkNet.Model.Message> commands(List<VkNet.Model.Message> messages)
        {
            var list = new List<VkNet.Model.Message>();
            foreach (var item in messages)
            {
                var str = item.Body;
                if (!string.IsNullOrEmpty(str))
                {
                    str = item.Body.Trim();
                    list.Add(item);

                    //if (str.IndexOf("/") == 0)
                    //{
                    //}
                }
            }
            return list;
        }

    }
}
