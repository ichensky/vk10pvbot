using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VkNet;
using VkNet.Enums.Filters;

namespace vk10pvbot
{
    //    в вк, есть апи, можно написать такого бота: 

    //1. Тот кто управляет ботом(ведущи) пишет ему команду для создания новой игры: 
    ///new https://vk.com/ссылка на страницу жениха. 
    //2. Дальше невесты, пишут: 
    ///add Чима 9999 
    ///add Персик 69 
    //.. 
    //3. Когда все добавились.Ведущий пишет команду для начала игры: 
    ///game start
    //4. Жених пишет в лс Боту/Ведущему вопросы: 
    ///q 1 Расскажи о себе, детка? 
    ///q 2 Что бы мы сделали, если бы остались вдвоем? 
    //... 
    //5. Невесты пишут ответы в лс Боту/Ведущему 
    ///answer 1 милая, ахаха 
    ///answer 2 любовь* мур~мур
    //6. Бот пересылает сообщения от невест жениху, жених выбирает/убивает кого-то 
    ///kill 9999 
    ///rose 69. 
    //7. В конце игры бот пересылает страницу невесты, жениху.



    public static class log {

        internal static void error(string message, string note)
        {
            ConsoleColor currentForeground = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR|" + message + "|" + note);
            Console.ForegroundColor = currentForeground;
        }
        internal static void error_command(Exception ex, string message, object obj)
        {
            ConsoleColor currentForeground = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            var sz = ex == null ? "" : JsonConvert.SerializeObject(ex).ToString();
            var szobj = obj == null ? "" : JsonConvert.SerializeObject(obj).ToString();
            Console.WriteLine("ERROR|" + message + "|" + szobj + "|" + sz);
            Console.ForegroundColor = currentForeground;
        }

        internal static void warning_command(string command, string message, string note)
        {
            ConsoleColor currentForeground = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("WARNING|" + command + "|" + message + "|" + note);
            Console.ForegroundColor = currentForeground;
        }
        internal static void info_command(string command, string message)
        {
            Console.WriteLine("INFO|" + command + "|" + message);
        }
    }
    public class auth {
        public ulong app_id { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public string chatname { get; set; }
    }


    public class player {
        public long userid { get; set; }
        public string nick { get; set; }
        public int id { get; set; }
        public string firstname { get; internal set; }
        public string lastname { get; internal set; }
    }
    public class player_answer
    {
        public player player { get; set; }
        public string answer { get; set; }
    }

    public class round {

        public List<player_answer> players_answers { get; set; }
        public List<player> kill { get; set; }
        public List<player> rose { get; set; }
        public string question { get; set; }
    }

    public class game {
        public player man { get; set; }
        public bool no_winners { get; set; }
        public List<player> players { get; set; }
        public round round { get; set; }
        public player winner { get; set; }
    }

    public class play_game {
        public game game;
        public enum statuses {
            none = 0,
            new_game = 1,
            man_set = 2,

            new_round = 3,

            question_set = 4,
            answers_set = 5,

            played = 6,
        }
        public statuses status { get; private set; } = statuses.none;

        public play_game() {
            this.status = statuses.none;
        }
        public void new_game() {
            this.game = new game();
            this.game.players = new List<player>();
            this.status = statuses.new_game;
        }

        public void add_man(player player)
        {
            if (this.status != statuses.new_game
                && this.status != statuses.man_set)
            {
                throw new Exception();
            }
            this.game.man = player;
            this.game.round = new round { };
            this.status = statuses.man_set;
        }
        public void add_player(player player) {
            if (this.status != statuses.man_set)
            {
                throw new Exception();
            }
            foreach (var item in this.game.players)
            {
                if (item.userid == player.userid)
                {
                    item.nick = player.nick;
                    return;
                }
            }
            player.id = this.game.players.Count;
            this.game.players.Add(player);
        }

        public bool players_set() {
            if (this.status != statuses.man_set)
            {
                throw new Exception();
            }
            if (this.game.man == null || this.game.players.Count == 0)
            {
                return false;
            }
            this.game.round.players_answers = this.game.players.Select(x => new player_answer { player = x }).ToList();
            this.status = statuses.new_round;
            return true;
        }

        public void add_question(string question) {
            if (this.status != statuses.new_round)
            {
                throw new Exception();
            }

            this.game.round.question = question;
            status = statuses.question_set;
        }

        public bool add_answer(player player, string answer)
        {
            if (this.status != statuses.question_set && this.status != statuses.answers_set)
            {
                throw new Exception();
            }

            foreach (var item in this.game.round.players_answers)
            {
                if (item.player.userid == player.userid)
                {
                    this.status = statuses.answers_set;
                    item.answer = answer;
                    return true;
                }
            }
            return false;
        }

        public void rose(List<int> ids) {
            this.game.round.rose = kill_rose(ids);
        }
        public void kill(List<int> ids)
        {
            this.game.round.kill = kill_rose(ids);
        }

        private List<player> kill_rose(List<int> ids) {
            if (this.status != statuses.answers_set)
            {
                throw new Exception();
            }
            var list = new List<player>();
            foreach (var id in ids)
            {
                var pa = this.game.round.players_answers.FirstOrDefault(x => x.player.id == id);
                if (pa != null)
                {
                    list.Add(pa.player);
                }
            }
            return list;
        }

     
        public bool play_round() {
            
            if (this.status != statuses.answers_set)
            {
                throw new Exception();
            }

            begin:
            for (int i = 0; i < this.game.round.players_answers.Count; i++)
            {
                var item = this.game.round.players_answers[i];
                if (string.IsNullOrWhiteSpace(item.answer))
                {
                    this.game.round.players_answers.RemoveAt(i);
                    goto begin;
                }
            }

            var prev_round = this.game.round;
            this.game.round = new round
            {
                players_answers = prev_round.players_answers.Select(x => new player_answer { player = x.player }).ToList(),
            };

            if (prev_round.kill != null)
            {
                foreach (var kill in prev_round.kill)
                {
                    for (int i = 0; i < this.game.round.players_answers.Count; i++)
                    {
                        var item = this.game.round.players_answers[i];
                        if (item.player.userid == kill.userid)
                        {
                            this.game.round.players_answers.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            if (this.game.round.players_answers.Count == 1)
            {
                this.game.winner = this.game.round.players_answers[0].player;
                this.status = statuses.played;
                return true;
            }
            if (this.game.round.players_answers.Count == 0)
            {
                this.game.no_winners = true;
                this.status = statuses.played;
                return true;
            }

            this.status = statuses.new_round;
            return false;
        }

        internal bool is_man_set()
        {
            return game.man != null;
        }

        internal player player(long? userId)
        {
            if (this.game.players == null || this.game.players.Count == 0)
            {
                return null;
            };
            return this.game.players.FirstOrDefault(x => x.userid == userId);
        }
    }

    public class info {
        public VkNet.Model.Chat chat;
        public long chat_peerid;
    }

    public class vk_connector {
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
    
    public class processor_commands
    {
        vk_connector connector;
        long recived_ticks;
        DateTime last_processed_recived_message_date;
        long send_ticks;
        DateTime last_processed_send_message_date;

        public processor_commands(vk_connector connector)
        {
            this.connector = connector;
            var date = DateTime.Now;
            recived_ticks = date.Ticks;
            last_processed_recived_message_date = date;
            send_ticks = date.Ticks;
            last_processed_send_message_date = date;
        }

        public List<VkNet.Model.Message> go()
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

                    if (str.IndexOf("/") == 0)
                    {
                        list.Add(item);
                    }
                }
            }
            return list;
        }

    }
    public class strs{
        public const string file_not_found = "Файл не найден.";
        public const string how_to_use_app = "Запустите программу из коммандной строки: vk10pvbot \"C:\\path\\to_file.txt\"";
        public const string check_file_content = "Проверьте правильноcnm содержимого файла.";
        public const string check_creds = "Не удалоcь залогинится. Проверьте правильность login, password и appid.";
        public const string chat_not_found = "Чат не найден.";

        public const string send_log = "Проститииииии. Эта ошибка в программе, скопируйте лог файл и скиньте, автору(мне) программы.";
        public const string new_game_created = "Новая игра coздана успешно.";
        public const string game_stoped = "Игра закончена успешно.";
        public const string game_played = "Игра сыграна.";
        public const string you_cannot_use_this_command = "Вы не можете использовать эту комманду.";
        public const string create_new_game = "Создайте новую игру.";
        public const string to_create_new_game = "Для создания новой игры исспользуте комманду: /new";
        public const string first_players_should_answer_to_question = "Сначала невесты должны ответить на вопрос жениха.";
        public const string vk_person_do_not_presetn = "Такой вк пользователь не существует.";
        public const string to_create_man = "Используйте комманду:  /man https://vk.com/idxxx";
        public const string man_added = "Вк пользователь(жених) добавлен успешно.";
        public const string first_add_man = "Cначала добавьте жениха.";
        public const string first_add_man_and_players = "Cначала добавьте жениха и невест.";
        public const string player_added = "Вк пользователь(невеста) добавлен успешно.";
        public const string first_set_answers = "Cначала невесты должны ответить на вопросы жениха.";
        public const string to_play_round = "Используйте комманду:  /round";
        public const string to_question = "Используйте комманду:  /q тут вопрос";
        public const string to_answer = "Используйте комманду:  /a тут ответ";
        public const string to_create_players = "Для добавлени невесты исспользуйте комманду: /add nick";
        public const string only_creator_can_use_command = "Только создатель игры может испльзовать эту команду.";
        public const string only_creator_or_man_can_use_command = "Только создатель игры или игрок(жених) может испльзовать эту команду.";
        public const string only_creator_or_man_or_players_can_use_command = "Только создатель игры или игрок(жених) или игроки(невесты) могут испльзовать эту команду.";
        public const string this_is_not_player = "Этот пользователь не учавствует в игре.";
        public const string not_play_in_round = "Этот пользователь не учавствует в раунде.";
        public const string not_correct_used_of_command = "Комманда исспользуется не верно.";
        public const string game_not_initialized = "Игра не инициализированна.";

        public const string answers_sent_to_man = "Ответы отправлены жениху.";
        public const string round_played = "Раунд сыгран.";
        public const string players_added = "Игроки добалены. Жених может написать свой первый вопрос.";
        public const string question_added = "Вопрос добавлен.";
        public const string answer_added = "Вопрос добавлен.";
    }
    public class strc {
        public const string @new = "/new";
        public const string stop = "/stop";
        public const string man = "/man ";
        public const string add = "/add ";
        public const string players = "/players";
        public const string round = "/round";
        public const string q = "/q ";
        public const string a = "/a ";
        public const string send_answers_to_man = "/sent";
        public const string kill = "/kill ";
        public const string rose = "/rose ";
        public const string status = "/status";
    }

    public class command_messsage
    {
        public play_game game { get; private set; }
        public VkNet.Model.Message message { get; private set; }
        public vk_connector connector { get; private set; }
        public string str { get; private set; }
        public string body { get; private set; }
        public string command { get; private set; }
        public command_messsage(vk_connector connector)
        {
            this.game = new play_game();
            this.connector = connector;
        }
        public void init_message(VkNet.Model.Message message)
        {
            this.message = message;
            this.str = this.message.Body.Trim();
        }

        public bool check(string command)
        {
            this.command = command;
            this.body = this.str.Replace(this.command, "");
            return this.str.IndexOf(command) == 0;
        }
        private bool check_only_creator_can_use_command()
        {
            var flag = this.message.UserId.Value == connector.vk.UserId.Value;
            if (!flag)
            {
                log_warning(strs.you_cannot_use_this_command, strs.only_creator_can_use_command);
            }
            return flag;
        }
        private bool check_only_creator_or_man_can_use_command()
        {
            var flag = this.message.UserId.Value == connector.vk.UserId.Value ||
                (game.is_man_set() && this.message.UserId.Value == game.game.man.userid);
            if (!flag)
            {
                log_warning(strs.you_cannot_use_this_command, strs.only_creator_or_man_can_use_command);
            }

            return flag;
        }
        private bool check_only_creator_or_man_or_players_can_use_command()
        {
            var flag = this.message.UserId.Value == connector.vk.UserId.Value ||
                (game.is_man_set() && this.message.UserId.Value == game.game.man.userid)
                || (game.game.players != null && game.game.players.Any(x => x.id == this.message.UserId.Value));
            if (!flag)
            {
                log_warning(strs.you_cannot_use_this_command, strs.only_creator_or_man_or_players_can_use_command);
            }

            return flag;
        }

        private void log_warning(string error, string note)
        {
            log.warning_command(this.str, error, note);
        }
        private void log_info(string note)
        {
            log.info_command(this.str, note);
        }

        public void new_game()
        {
            if (check_only_creator_can_use_command())
            {
                this.game = new play_game();
                this.game.new_game();
                log_info(strs.new_game_created);
            }
        }
        public void stop_game()
        {
            if (check_only_creator_can_use_command())
            {
                game = new play_game();
                log_info(strs.game_stoped);
            }
        }
        public void add_man()
        {
            if (check_only_creator_can_use_command())
            {
                if ((game.status != play_game.statuses.new_game
                                         && game.status != play_game.statuses.man_set))
                {
                    log_warning($"{strs.you_cannot_use_this_command} {strs.create_new_game}", strs.to_create_new_game);
                    return;
                }

                var screenname = this.body.Substring(this.body.LastIndexOf("/") + 1);
                VkNet.Model.User user = null;

                user = connector.user(screenname);

                if (user == null)
                {
                    log_warning(strs.vk_person_do_not_presetn, strs.to_create_man);
                    return;
                }

                game.add_man(new player { userid = user.Id, firstname = user.FirstName, lastname = user.LastName });
                log_info($"{strs.man_added} [{user.Id}, {user.FirstName} {user.LastName}]");
            }
        }
        public void add_player()
        {
            if ((game.status != play_game.statuses.man_set))
            {
                log_warning($"{strs.you_cannot_use_this_command} {strs.first_add_man}", strs.to_create_man);
                return;
            }

            var player = connector.user(message.UserId.Value);
            var nick = this.body;
            if (string.IsNullOrEmpty(nick))
            {
                nick = $"{player.FirstName} {player.LastName}";
            }

            game.add_player(new player { userid = player.Id, nick = this.body, firstname = player.FirstName, lastname = player.LastName });
            log_info($"{strs.players_added} [{player.Id}, {nick}, {player.FirstName} {player.LastName}]");
        }

        public void players()
        {
            if (check_only_creator_can_use_command())
            {
                if (game.status != play_game.statuses.man_set)
                {
                    log_warning($"{strs.you_cannot_use_this_command} {strs.first_add_man}", strs.to_create_man);
                    return;
                }
                if (!game.players_set())
                {
                    log_warning($"{strs.you_cannot_use_this_command} {strs.first_add_man_and_players}", $"{strs.to_create_man} {strs.to_create_players}");
                    return;
                }
                else
                {
                    log_info(strs.players_added);
                }
            }
        }
        public void round()
        {
            if (check_only_creator_can_use_command())
            {
                if (game.status != play_game.statuses.answers_set)
                {
                    log_warning($"{strs.you_cannot_use_this_command} {strs.first_players_should_answer_to_question}", $"{strs.to_question} {strs.to_answer}");
                    return;
                }

                if (game.play_round())
                {
                    log_info(strs.game_played);
                }
                else
                {
                    log_info(strs.round_played);
                }
            }
        }
        public void add_question()
        {
            if (check_only_creator_or_man_can_use_command())
            {
                if (this.game.status != play_game.statuses.new_round)
                {
                    log_warning(strs.you_cannot_use_this_command, "Сначала /players /round");
                    return;
                }
                game.add_question(this.body);
                log_info($"{strs.question_added} {this.body}");
            }
        }
        public void add_answer()
        {
            if (this.game.status != play_game.statuses.question_set && this.game.status != play_game.statuses.answers_set)
            {
                log_warning(strs.you_cannot_use_this_command, "Сначала /q /a");
                return;
            }
            var player = game.player(this.message.UserId);
            if (player == null)
            {
                log_warning($"{strs.this_is_not_player} [{this.message.UserId}]", "");
                return;
            }
            var a = str.Replace("/a ", "").Trim();
            if (game.add_answer(player, a))
            {
                log_info($"{strs.answer_added} [{player.userid}, {player.nick}] {a}");
                return;
            }
            else
            {
                log_warning(strs.not_play_in_round, "");
                return;
            }
        }
        public void send_answers_to_man()
        {
            if (check_only_creator_can_use_command())
            {
                if (game.status != play_game.statuses.answers_set)
                {
                    log_warning(strs.you_cannot_use_this_command, strs.first_players_should_answer_to_question);
                    return;
                }
                var s = new StringBuilder();
                foreach (var pa in game.game.round.players_answers)
                {
                    s.AppendLine(pa.player.id + ". " + pa.answer);
                }
                connector.send_message(s.ToString(), game.game.man.userid);
                log_info(strs.answers_sent_to_man);
            }
        }

        public void kill()
        {
            kill_rose(x => game.kill(x));

        }
        public void rose()
        {
            kill_rose(x => game.rose(x));
        }

        private void kill_rose(Action<List<int>> action)
        {
            if (check_only_creator_or_man_can_use_command())
            {
                if (this.game.status != play_game.statuses.answers_set)
                {
                    log_warning(strs.you_cannot_use_this_command, "Сначала /a");
                    return;
                }
                var ids = new Regex("[ ]{2,}", RegexOptions.None).Replace(this.body, " ").Split(' ');
                if (ids == null || ids.Length == 0)
                {
                    log_warning(strs.not_correct_used_of_command, command + " 1 2 3 4 5");
                    return;
                }
                var list = new List<int>();
                foreach (var item in ids)
                {
                    int id;
                    if (!int.TryParse(item, out id))
                    {
                        log_warning(strs.not_correct_used_of_command, command + " 1 2 3 4 5");
                        return;
                    }
                    list.Add(id);
                }
                if (list.Count > 0)
                {
                    action.Invoke(list);
                    var sb = new StringBuilder("Игрокам выставлен статус." + $" {command}. ");
                    foreach (var item in list)
                    {
                        sb.Append(item.ToString() + " ");
                    }
                    log_info(sb.ToString());
                }
            }
        }

        public void status()
        {
            if (check_only_creator_or_man_or_players_can_use_command())
            {
                log_info(game.status.ToString());
                if (this.game.game!=null&&this.game.game.man != null && this.message.UserId == this.game.game.man.id)
                {
                    return;
                }

                var sb = new StringBuilder();
                if (game.status == play_game.statuses.none)
                {
                    sb.AppendLine(strs.game_not_initialized);
                }
                else {
                    print_winner(sb);
                    print_man(sb);
                    print_players(sb);
                    print_pas(sb);
                    print_q(sb);
                }
            
                if (this.message.ChatId == null)
                {
                    this.connector.send_message(sb.ToString(), this.message.UserId.Value);
                }
                else
                {
                    this.connector.send_chat_message(sb.ToString());
                }
            }
        }
        private void print_man(StringBuilder sb)
        {
            if (this.game.status== play_game.statuses.answers_set 
                || this.game.status == play_game.statuses.man_set
                || this.game.status == play_game.statuses.new_round
                || this.game.status == play_game.statuses.played
                || this.game.status == play_game.statuses.question_set
                )
            {
                
            }
            var man = game.game.man;
            var s = @"¯\_(ツ)_/¯ . ";
            if (man == null)
            {
                sb.AppendLine(s + "??");
            }
            else if (game.game.winner != null ||
                (this.message.ChatId == null && this.message.UserId.Value == this.connector.vk.UserId))
            {
                sb.AppendLine($"{s}[{man.firstname} {man.lastname}] https://vk.com/id{man.userid}");
            }
            else
            {
                sb.AppendLine(s + "+");
            }
        }
        private void print_winner(StringBuilder sb)
        {
            if (this.game.status == play_game.statuses.played)
            {
                var s = "♥ ";
                if (game.game.winner != null)
                {
                    sb.AppendLine(s + game.game.winner.nick);
                }
                else
                {
                    sb.AppendLine(s + ".. упс, победителей нет ??.");
                }
            }

        }
        private void print_players(StringBuilder sb)
        {
            if (this.game.status == play_game.statuses.man_set)
            {
                if (this.game.game.players.Count == 0)
                {
                    sb.AppendLine(@"ｫｫｫ . ??");
                }
                else
                {
                    foreach (var item in this.game.game.players)
                    {
                        sb.AppendLine(item.id + ". " + item.nick);
                    }

                }
            }
        }
        private void print_pas(StringBuilder sb)
        {
            if (this.game.status == play_game.statuses.answers_set
              || this.game.status == play_game.statuses.new_round
              || this.game.status == play_game.statuses.question_set
              )
            {
                if (this.game.game.round.players_answers.Count == 0)
                {
                    sb.AppendLine(@"ｫｫｫ . ??");
                }
                else
                {
                    if (this.game.status == play_game.statuses.new_round)
                    {
                        foreach (var item in this.game.game.round.players_answers)
                        {
                            var kr = "";
                            if (this.game.game.round.rose != null && this.game.game.round.rose.Any(x => x.userid == item.player.userid))
                            {
                                kr = "🌹";
                            }
                            if (this.game.game.round.kill != null && this.game.game.round.kill.Any(x => x.userid == item.player.userid))
                            {
                                kr = "🔪";
                            }
                            sb.AppendLine(item.player.id + ". " + item.player.nick + " " + kr);
                        }
                    }
                    else
                    {
                        foreach (var item in this.game.game.round.players_answers)
                        {
                            var kr = "";
                            if (!string.IsNullOrEmpty(item.answer))
                            {
                                kr = "☆";
                            }
                            sb.AppendLine(item.player.id + ". " + item.player.nick + " " + kr);
                        }
                    }
                    
                }
            }
        }
        private void print_q(StringBuilder sb)
        {
            if (this.game.status == play_game.statuses.answers_set
              || this.game.status == play_game.statuses.new_round
              || this.game.status == play_game.statuses.question_set
              )
            {
                var s = "Вопрос . ";
                if (string.IsNullOrEmpty(this.game.game.round.question))
                {
                    sb.AppendLine(s+"??");
                }
                else
                {
                    sb.AppendLine(s+this.game.game.round.question);
                }

            }
        }
    }

        public class processor {

        vk_connector connector;
        public processor(vk_connector connector) {
           this.connector = connector;
        }

        
      
        public void go() {
            var processor_commands = new processor_commands(connector);

            var cm = new command_messsage(connector);
            while (true)
            {
                try
                {
                    var commands = processor_commands.go();
                    if (commands.Count > 0)
                    {
                        foreach (var item in commands)
                        {
                            cm.init_message(item);
                            if (cm.check(strc.@new))
                            {
                                cm.new_game();
                            }
                            else if (cm.check(strc.stop))
                            {
                                cm.stop_game();
                            }
                            else if (cm.check(strc.man)) {
                                cm.add_man();
                            }
                            else if (cm.check(strc.add))
                            {
                                cm.add_player();
                            }
                            else if (cm.check(strc.players))
                            {
                                cm.players();
                            }
                            else if (cm.check(strc.round))
                            {
                                cm.round();
                            }
                            else if (cm.check(strc.q))
                            {
                                cm.add_question();
                            }
                            else if (cm.check(strc.a))
                            {
                                cm.add_answer();
                            }
                            else if (cm.check(strc.send_answers_to_man))
                            {
                                cm.send_answers_to_man();
                            }
                            else if (cm.check(strc.kill))
                            {
                                cm.kill();
                            }
                            else if (cm.check(strc.rose))
                            {
                                cm.rose();
                            }
                            else if (cm.check(strc.status))
                            {
                                cm.status();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.error_command(ex, strs.send_log, cm);

                }

                Thread.Sleep(1000);
            }
        }

       
    }

    class Program
    {
        static void test_proof_of_game() {
            var flag = false;
            var man = new player { userid = 100 };
            var player1 = new player { userid = 201 };
            var player2 = new player { userid = 202 };
            var player3 = new player { userid = 203 };
            var player4 = new player { userid = 204 };
            var player5 = new player { userid = 205 };
            var player6 = new player { userid = 206 };
            var player7 = new player { userid = 207 };
            var player8 = new player { userid = 208 };
            var player9 = new player { userid = 209 };

            var play = new play_game();
            play.new_game();
            play.add_man(man);
            play.add_player(player1);
            play.add_player(player2);
            play.add_player(player3);
            play.add_player(player4);
            play.add_player(player5);
            play.add_player(player6);
            play.add_player(player7);
            play.add_player(player8);
            play.add_player(player9);
            play.players_set();

            play.add_question("вопрос 1");
            play.add_answer(player1, "1 ответ 1");
            play.add_answer(player2, "2 ответ 1");
            play.add_answer(player3, "3 ответ 1");
            play.add_answer(player4, "4 ответ 1");
            play.add_answer(player5, "5 ответ 1");

            play.kill(new List<int>() { player1.id, player2.id });
            play.rose(new List<int>() { player3.id, player4.id });

           flag= play.play_round();

            play.add_question("вопрос 2");
            play.add_answer(player3, "1 ответ 1");
            play.add_answer(player4, "2 ответ 1");
            play.add_answer(player5, "3 ответ 1");

            play.kill(new List<int>() { player3.id, player4.id });
            play.rose(new List<int>() { player5.id });

           flag= play.play_round();
        }
        static void Main(string[] args)
        {
            test_proof_of_game();

              //if (args.Length !=1)
              //{
              //    log.error("Запуск программы не верный.", "Запустите программу из коммандной строки: vk10pvbot \"C:\\path\\to_file.txt\"");
              //    goto error;
              //}
              var path = @"C:\Users\john\Desktop\file.txt";// args[0];
            if (!File.Exists(path))
            {
                log.error($"{strs.file_not_found} {path}", strs.how_to_use_app);
                goto error;
            }
            auth auth = null;

            try
            {
                var str = File.ReadAllText(path);
                auth = JsonConvert.DeserializeObject(str,typeof(auth)) as auth;
            }
            catch (Exception)
            {
            }
            if (auth == null)
            {
                log.error($"{strs.check_file_content} {path}", "");
                goto error;
            }


            vk_connector connector = new vk_connector();

            if (!connector.login(auth))
            {
                auth.password = "******";
                log.error_command(null, strs.check_creds, auth);
                goto error;
            }
            if (!connector.login(auth))
            {
                auth.password = "******";
                log.error_command(null, strs.check_creds, auth);
                goto error;
            }
            if (!connector.add_chat(auth.chatname))
            {
                log.error_command(null, $"{strs.chat_not_found} {auth.chatname}", null);
                goto error;
            }

            var processor = new processor(connector);
            processor.go();

            error:;
            Console.ReadLine();
            return;
        }
    }




      

   



}
