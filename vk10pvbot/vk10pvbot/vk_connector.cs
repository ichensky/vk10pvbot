using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VkNet;
using VkNet.Enums.Filters;

namespace vk10pvbot
{
    public class vk_connector
    {
        public readonly VkApi vk = new VkApi();
        private info info;

        public bool login(auth auth)
        {
            try
            {
                vk.Authorize(new ApiAuthParams
                {
                    ApplicationId = auth.app_id,
                    Login = auth.email,
                    Password = auth.password,
                    Settings = Settings.Messages,
                });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool add_chat(string chat_name)
        {
            try
            {
                var chat = (vk.Messages.SearchDialogs(chat_name)).Chats[0];
                this.info = new info()
                {
                    chat = chat,
                    chat_peerid = get_peer_id(chat.Id),
                };
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        private long get_peer_id(long chatid)
        {
            return 2000000000 + chatid;
        }

        public List<VkNet.Model.Message> messages(ulong timeoffset, VkNet.Enums.MessageType messagetype)
        {
            try
            {
                return vk.Messages.Get(new VkNet.Model.RequestParams.MessagesGetParams
                {
                    Filters = VkNet.Enums.MessagesFilter.All,
                    Offset = 0,
                    Out = messagetype,
                    PreviewLength = 0,
                    Count = 20,
                    TimeOffset = timeoffset
                }).Messages.ToList();
            }
            catch (Exception ex)
            {
            }

            return null;
        }

        public VkNet.Model.User user(string screenname)
        {
            try
            {
                var model = vk.Utils.ResolveScreenName(screenname);
                if (model.Type == VkNet.Enums.VkObjectType.User)
                {
                    return user(model.Id.Value);
                }
            }
            catch (Exception)
            {
            }
            return null;
        }
        public VkNet.Model.User user(long id)
        {
            try
            {
                return vk.Users.Get(id);
            }
            catch (Exception)
            {
            }
            return null;
        }
        public void send_message(string message, long userid)
        {
            vk.Messages.Send(new VkNet.Model.RequestParams.MessagesSendParams()
            {
                UserId = userid,
                Message = message
            });
        }
        public void send_chat_message(string message)
        {
            vk.Messages.Send(new VkNet.Model.RequestParams.MessagesSendParams()
            {
                PeerId = info.chat_peerid,
                Message = message
            });
        }

    }
}
