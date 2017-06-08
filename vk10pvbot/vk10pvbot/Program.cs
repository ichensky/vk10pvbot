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
        public string answer { get; internal set; }
        public int rose { get; set; }
        public bool kill { get; set; }
    }

    public class game {
        public player man { get; set; }
        public List<player> players { get; set; }
        public string question { get; set; }
        public player winner { get; set; }
    }

    public class play_game {
        private game game;

        public play_game() {
            this.game = new game();
            this.game.players = new List<player>();
        }

        public void add_man(player player)
        {
            this.game.man = player;
        }
        public void add_player(player player) {
            
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

        public void add_question(string question) {

            this.game.question = question;
        }

        public player add_answer(long userid, string answer)
        {
            foreach (var item in this.game.players)
            {
                if (item.userid == userid)
                {
                    item.answer = answer;
                    return item;
                }
            }
            return null;
        }

        public void rose(List<int> ids) {

            foreach (var id in ids)
            {
                var p = this.game.players.FirstOrDefault(x => x.id==id);
                if (p != null)
                {
                    p.rose++;
                }
            }
        }
        public void kill(List<int> ids)
        {
            foreach (var id in ids)
            {
                var p = this.game.players.FirstOrDefault(x => x.id == id);
                if (p != null)
                {
                    p.kill=true;
                }
            }
        }
        public void unkill(List<int> ids)
        {
            foreach (var id in ids)
            {
                var p = this.game.players.FirstOrDefault(x => x.id == id);
                if (p != null)
                {
                    p.kill = false;
                }
            }
        }

        public player play_round() {

            var c = this.game.players.Count(x => x.kill);

            this.game.question = null;
            foreach (var player in this.game.players)
            {
                if (string.IsNullOrWhiteSpace(player.answer))
                {
                    player.kill = true;
                }
                player.answer = null;
            }
            
            if (c==this.game.players.Count-1)
            {
                this.game.winner = this.game.players.FirstOrDefault(x=>!x.kill);
                return this.game.winner;
            }
            else if (c == this.game.players.Count)
            {
                this.game.winner = this.game.man;
                return this.game.winner;
            }

            return null;
        }

        public player player(long? userId)
        {
            return this.game.players.FirstOrDefault(x => x.userid == userId);
        }
        public player man()
        {
            return this.game.man;
        }

        public List<player> players()
        {
            return this.game.players;
        }
        public player winner()
        {
            return this.game.winner;
        }

        public string question()
        {
            return this.game.question;
        }
    }

    public class info
    {
        public VkNet.Model.Chat chat;
        public long chat_peerid;
    }
    
    public class strs
    {
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
    public class strc
    {
        public const string @new = "новая игра";
        public const string man = "жених";

        public const string add = "+";

        public const string round = "раунд";
        public const string q = "вопрос";
        public const string a = "ответ";
        public const string send_answers_to_man = "отправить";
        public const string kill = "убить ";
        public const string unkill = "воскресить";
        public const string rose = "роза";
        public const string status = "список";
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
            return this.str.ToLower().IndexOf(command) == 0;
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
            var man = game.man();
            var flag = this.message.UserId.Value == connector.vk.UserId.Value ||
                (man!=null && this.message.UserId.Value == man.userid);
            if (!flag)
            {
                log_warning(strs.you_cannot_use_this_command, strs.only_creator_or_man_can_use_command);
            }

            return flag;
        }
        private bool check_only_creator_or_man_or_players_can_use_command()
        {
            var man = game.man();
            var players = game.players();

            var flag = this.message.UserId.Value == connector.vk.UserId.Value ||
                (man!=null && this.message.UserId.Value == man.userid)
                || players.Any(x => x.userid == this.message.UserId.Value);
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
                log_info(strs.new_game_created);
            }
        }
       
        public void add_man()
        {
            if (check_only_creator_can_use_command())
            {
                var screenname = this.body.Substring(this.body.LastIndexOf("/") + 1);
                var user = connector.user(screenname);

                if (user == null)
                {
                    log_warning(strs.vk_person_do_not_presetn, strs.to_create_man);
                    return;
                }
                var nick = "Жених";

                game.add_man(new player { nick=nick, userid = user.Id, firstname = user.FirstName, lastname = user.LastName });
                log_info($"{strs.man_added} [{user.Id}, {user.FirstName} {user.LastName}]");
            }
        }
        public void add_player()
        {
            var player = connector.user(message.UserId.Value);
            var nick = this.body;
            if (string.IsNullOrEmpty(nick))
            {
                nick = $"{player.FirstName} {player.LastName}";
            }

            game.add_player(new player { userid = player.Id, nick = nick, firstname = player.FirstName, lastname = player.LastName });
            log_info($"{strs.players_added} [{player.Id}, {nick}, {player.FirstName} {player.LastName}]");
        }

        public void round()
        {
            if (check_only_creator_can_use_command())
            {
                var winner = game.play_round();
                if (winner!=null)
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
                if (!string.IsNullOrWhiteSpace(this.body))
                {
                    game.add_question(this.body);
                    log_info($"{strs.question_added} {this.body}");
                }
            }
        }
        public void add_answer()
        {
            var player = game.add_answer(this.message.UserId.Value, this.body);
            if (player!=null)
            {
                log_info($"{strs.answer_added} [{player.userid}, {player.nick}]");
                return;
            }
            else
            {
                log_warning($"{strs.this_is_not_player} [{this.message.UserId}]", "");
                return;
            }
        }
        public void send_answers_to_man()
        {
            if (check_only_creator_can_use_command())
            {
                var s = new StringBuilder();
                foreach (var player in game.players())
                {
                    s.AppendLine(player.id + ". " + player.answer);
                }
                connector.send_message(s.ToString(), game.man().userid);
                log_info(strs.answers_sent_to_man);
            }
        }

        public void unkill()
        {
            if (check_only_creator_or_man_can_use_command())
            {
                game.unkill(ids_from_body());
                log_info("Игроки воскрешены");
            }
        }
        public void kill()
        {
            if (check_only_creator_or_man_can_use_command())
            {
                game.kill(ids_from_body());
                log_info("Убиты игроки");
            }
        }
        public void rose()
        {
            if (check_only_creator_or_man_can_use_command())
            {
                game.rose(ids_from_body());
                log_info("Подарена роза");

            }
        }

        private List<int> ids_from_body()
        {
            var list = new List<int>();
            var ids = new Regex("[ ]{2,}", RegexOptions.None).Replace(this.body, " ").Split(' ');
            if (ids == null || ids.Length == 0)
            {
                return list;
            }
            foreach (var item in ids)
            {
                int id;
                if (int.TryParse(item, out id))
                {
                    list.Add(id);
                }
            }

            return list;
        }

        public void status()
        {
            if (check_only_creator_or_man_or_players_can_use_command())
            {

                var sb = new StringBuilder();
              
                    print_winner(sb);
                    print_man(sb);
                    print_players(sb);
                    print_q(sb);
                log_info(sb.ToString());


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
            var man = game.man();
            var winner = game.winner();
            var s = @"Жених: ";
            if (man == null)
            {
                sb.AppendLine(s + "??");
            }
            else if (winner != null ||
                (this.message.ChatId == null && this.message.UserId.Value == this.connector.vk.UserId))
            {
                sb.AppendLine($"{s}[{man.firstname} {man.lastname}] https://vk.com/id{man.userid}");
            }
            else
            {
                sb.AppendLine(s + man.firstname[0]+ "..💕");
            }
        }
        private void print_winner(StringBuilder sb)
        {
            var winner = game.winner();

            var s = "♥";
            if (winner != null)
            {
                sb.AppendLine($"{s} {winner.nick} {s}");
            }
        }
        private void print_players(StringBuilder sb)
        {
            var players = game.players();
            if (players.Count == 0)
            {
                sb.AppendLine(@"Невесты: ??");
            }
            else
            {
                var flag = string.IsNullOrWhiteSpace(this.game.question());
                foreach (var player in players)
                {
                    var star = "•";
                    if (!flag)
                    {
                        if (!string.IsNullOrWhiteSpace(player.answer))
                        {
                            star = "+";
                        }
                    }

                    var kr = "";                    

                    if (player.kill)
                    {
                        kr = "🔪";
                    }
                    else {
                        if (player.rose>3)
                        {
                            kr = "🌹🌹🌹...";
                        }
                        else
                        {
                            for (int i = 0; i < player.rose; i++)
                            {
                                kr += "🌹";
                            }
                        }
                    }
                    sb.AppendLine(star + " " + player.nick+ " "+kr);
                }
            }
        }
 
        private void print_q(StringBuilder sb)
        {
            var question = this.game.question();
            var s = "Вопрос: ";

            if (string.IsNullOrWhiteSpace(question))
            {
                    sb.AppendLine(s + "??");
            }
            else
            {
                sb.AppendLine(s + question);
            }
        }
    }

    public class processor
    {

        vk_connector connector;
        public processor(vk_connector connector)
        {
            this.connector = connector;
        }



        public void go()
        {
            var vk_commands = new vk_commands(connector);
            var cm = new command_messsage(connector);
            while (true)
            {
                try
                {
                    var commands = vk_commands.commands();
                    if (commands.Count > 0)
                    {
                        foreach (var item in commands)
                        {
                            cm.init_message(item);
                            if (cm.check(strc.@new))
                            {
                                cm.new_game();
                            }
                            else if (cm.check(strc.man))
                            {
                                cm.add_man();
                            }
                            else if (cm.check(strc.add) )
                               
                            {
                                cm.add_player();
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
                            else if (cm.check(strc.round))
                            {
                                cm.round();
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
            player flag = null;
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

            play.add_question("вопрос 1");
            play.add_answer(player1.userid, "1 ответ 1");
            play.add_answer(player2.userid, "2 ответ 1");
            play.add_answer(player3.userid, "3 ответ 1");
            play.add_answer(player4.userid, "4 ответ 1");
            play.add_answer(player5.userid, "5 ответ 1");

            play.kill(new List<int>() { player1.id, player2.id });
            play.rose(new List<int>() { player3.id, player4.id });

           flag= play.play_round();

            play.add_question("вопрос 2");
            play.add_answer(player3.userid, "1 ответ 1");
            play.add_answer(player4.userid, "2 ответ 1");
            play.add_answer(player5.userid, "3 ответ 1");

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

            log.info_command("","Теперь можно написать себе в вк в лс комманду /status.");
            var processor = new processor(connector);
            processor.go();

            error:;
            Console.ReadLine();
            return;
        }
    }




      

   



}
