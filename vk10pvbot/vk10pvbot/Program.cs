using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
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

            round = 3,
            new_round = 4,

            question_set = 5,
            answers_set = 6,

            played = 7,
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
            this.status = statuses.round;
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
            if (this.status != statuses.question_set || this.status != statuses.answers_set)
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

        public void add_rose(List<player> players) {
            if (this.status != statuses.answers_set)
            {
                throw new Exception();
            }
            this.game.round.rose = players.Select(x => x).ToList();

        }
        public void add_kill(List<player> players)
        {
            if (this.status != statuses.answers_set)
            {
                throw new Exception();
            }
            this.game.round.kill = players.Select(x => x).ToList();
        }

        public void remove_players_who_do_not_answered() {
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
        }

        public void round_played() {
            if (this.game == null || this.game.round == null || this.game.round.kill == null)
            {
                throw new Exception();
            }
            this.status = statuses.round;
        }


        public bool play_round() {
            if (this.status != statuses.round)
            {
                throw new Exception();
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
                    return vk.Users.Get(model.Id.Value);
                }
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
        public const string first_play_round = "Cначала сыграйте раунд.";
        public const string to_play_round = "Используйте комманду:  /round";
        public const string to_create_players = "Для добавлени невесты исспользуйте комманду: /add nick";
        public const string only_creator_can_use_command = "Только создатель игры может испльзовать эту команду.";
        public const string only_creator_and_man_can_use_command = "Только создатель игры или игрок(жених) может испльзовать эту команду.";
        public const string this_is_not_player = "Этот пользователь не учавствует в игре.";
        public const string not_play_in_round = "Этот пользователь не учавствует в раунде.";

        public const string answers_sent_to_man = "Ответы отправлены жениху.";
        public const string players_inited = "Игроки для раунда иницилизированны, можно добавлять вопрос, ответы.";
        public const string players_added = "Игроки добалены, можно играть первый раунд.";
        public const string question_added = "Вопрос добавлен.";
        public const string answer_added = "Вопрос добавлен.";
    }
    public class strc {
        public const string @new = "/new";
        public const string stop = "/stop";
        public const string man = "/man ";
        public const string add = "/add ";
        public const string round = "/round";
        public const string q = "/q ";
        public const string a = "/a ";
        public const string send_answers_to_man = "/saman ";
    }

    public class processor {

        vk_connector connector;
        public processor(vk_connector connector) {
           this.connector = connector;
        }

      
        public void go() {
            var processor_commands = new processor_commands(connector);

            play_game game = new play_game() ;
            while (true)
            {
                try
                {
                    var commands = processor_commands.go();
                    if (commands.Count > 0)
                    {
                        foreach (var item in commands)
                        {
                            var str = item.Body.Trim();
                            if (str.IndexOf(strc.@new) == 0)
                            {
                                if (check_only_creator_can_use_command(connector, item, str))
                                {
                                    game = new play_game();
                                    game.new_game();
                                    log.info_command(str, strs.new_game_created);
                                }
                            }
                            else if (str.IndexOf(strc.stop) == 0)
                            {
                                if (check_only_creator_can_use_command(connector, item, str))
                                {
                                    game = new play_game();
                                    log.info_command(str, strs.game_stoped);
                                }
                            }
                            else if (str.IndexOf(strc.man) == 0)
                            {
                                if (check_only_creator_can_use_command(connector, item, str))
                                {
                                    if ((game.status != play_game.statuses.new_game
                                        && game.status != play_game.statuses.man_set))
                                    {
                                        log.warning_command(str,$"{strs.you_cannot_use_this_command} {strs.create_new_game}", strs.to_create_new_game);
                                        continue;
                                    }

                                    var url = str.Replace(strc.man, "").Trim();
                                    var screenname = url.Substring(url.LastIndexOf("/") + 1);
                                    VkNet.Model.User user = null;

                                    user = connector.user(screenname);

                                    if (user == null)
                                    {
                                        log.warning_command(str, strs.vk_person_do_not_presetn, strs.to_create_man);
                                        continue;
                                    }

                                    game.add_man(new player { userid = user.Id });
                                    log.info_command(str, $"{strs.man_added} [{user.Id}, {user.FirstName} {user.LastName}]");
                                }
                            }
                            else if (str.IndexOf(strc.add) == 0)
                            {
                                if ((game.status != play_game.statuses.man_set))
                                {
                                    log.warning_command(str, $"{strs.you_cannot_use_this_command} {strs.first_add_man}", strs.to_create_man);
                                    continue;
                                }

                                var nick = str.Replace(strc.add, "").Trim();

                                game.add_player(new player { userid = item.UserId.Value, nick = nick });
                                log.info_command(str, $"{strs.players_added} [{item.UserId.Value}, {nick}]");
                            }
                            else if (str.IndexOf(strc.round) == 0)
                            {
                                if (!check_only_creator_can_use_command(connector, item, str))
                                {

                                    if ((game.status != play_game.statuses.man_set
                                        && game.status != play_game.statuses.round))
                                    {
                                        if (!game.is_man_set())
                                        {
                                            log.warning_command(str, $"{strs.you_cannot_use_this_command} {strs.first_add_man}", strs.to_create_man);
                                        }
                                        else
                                        {
                                            log.warning_command(str, $"{strs.you_cannot_use_this_command} {strs.first_play_round}", strs.to_play_round);
                                        }
                                        continue;
                                    }

                                    if (game.status == play_game.statuses.man_set)
                                    {
                                        if (!game.players_set())
                                        {
                                            log.warning_command(str, $"{strs.you_cannot_use_this_command} {strs.first_add_man_and_players}", $"{strs.to_create_man} {strs.to_create_players}");
                                            continue;
                                        }
                                        else
                                        {
                                            log.info_command(str, strs.players_added);
                                        }
                                    }

                                    if (game.play_round())
                                    {
                                        log.info_command(str, strs.game_played);
                                    }
                                    else
                                    {
                                        log.info_command(str,strs.players_inited);
                                    }
                                }
                            }
                            else if (str.IndexOf(strc.q) == 0)
                            {
                                if (item.UserId.Value != connector.vk.UserId.Value || 
                                    game.is_man_set()|| item.UserId.Value != game.game.man.userid)
                                {
                                    log.warning_command(str,strs.you_cannot_use_this_command, strs.only_creator_and_man_can_use_command);
                                }

                                var q = str.Replace(strc.q, "").Trim();
                                game.add_question(q);
                                log.info_command(str,$"{strs.question_added} {q}");
                            }
                            else if (str.IndexOf(strc.a) == 0)
                            {
                                var player = game.player(item.UserId);
                                if (player==null)
                                {
                                    log.warning_command(str, $"{strs.this_is_not_player} [{item.UserId}]", "");
                                    continue;
                                }
                                var a = str.Replace("/a ", "").Trim();
                                if (game.add_answer(player, a))
                                {
                                    log.info_command(str, $"{strs.answer_added} [{player.userid}, {player.nick}] {a}");
                                    continue;
                                }
                                else {
                                    log.warning_command(str, strs.not_play_in_round, "");
                                    continue;
                                }
                            }
                            else if (str.IndexOf(strc.send_answers_to_man) == 0)
                            {
                                if (!check_only_creator_can_use_command(connector,item,str))
                                {
                                    continue;
                                }
                                if (game.status!= play_game.statuses.answers_set)
                                {
                                    log.warning_command(str, strs.you_cannot_use_this_command, strs.first_players_should_answer_to_question);
                                    continue;
                                }
                                //TODO: send message to man
                                connector.send_message("", game.game.man.userid);
                                log.info_command(str, strs.answers_sent_to_man);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.error_command(ex, strs.send_log, game);

                }

                Thread.Sleep(1000);
            }
        }

        private bool check_only_creator_can_use_command(vk_connector connector, VkNet.Model.Message message, string command) {
            var flag = message.UserId.Value == connector.vk.UserId.Value;
            if (!flag)
            {
                log.warning_command(command, strs.you_cannot_use_this_command, strs.only_creator_can_use_command);
            }
            return flag;
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

           flag= play.play_round();

            play.add_question("вопрос 1");
            play.add_answer(player1, "1 ответ 1");
            play.add_answer(player2, "2 ответ 1");
            play.add_answer(player3, "3 ответ 1");
            play.add_answer(player4, "4 ответ 1");
            play.add_answer(player5, "5 ответ 1");

            play.add_kill(new List<player>() { player1, player2 });
            play.add_rose(new List<player>() { player3, player4 });
            play.remove_players_who_do_not_answered();
            play.round_played();

           flag= play.play_round();

            play.add_question("вопрос 2");
            play.add_answer(player3, "1 ответ 1");
            play.add_answer(player4, "2 ответ 1");
            play.add_answer(player5, "3 ответ 1");

            play.add_kill(new List<player>() { player3, player4 });
            play.add_rose(new List<player>() { player5 });
            play.remove_players_who_do_not_answered();
            play.round_played();

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




      

    //    public void send_chat_message(string message)
    //    {
    //        vk.Messages.Send(new VkNet.Model.RequestParams.MessagesSendParams()
    //        {
    //             PeerId =info.chat_peerid,
    //             Message=message
    //        });
    //        info.chat_start_message_id++;
    //    }




}
