using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TotalPages = BookmakerTelegramBot.Models.TotalPages;
using Users = BookmakerTelegramBot.Models.Users;
using Keyboards = BookmakerTelegramBot.Models.MainKeyboards;
using BookmakerTelegramBot.Models;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace BookmakerTelegramBot.Sevices
{
    public class BotMethods
    {


        private static async Task Methods()
        {
            var bot = new TelegramBotClient("5473318521:AAGbg2EC66-PapJncwwDydhJDGopygOWZ3M");
            //var connectionString = ConfigurationManager.ConnectionStrings["orcldb"].ConnectionString;
            var connectionString = "Data Source=localhost:1521/orcl;Persist Security Info=True;User ID=FWC;Password=Nicu9josu4";
            ResourceManager ResManager = new ResourceManager("BookmakerTelegramBot.Resources.Resources", Assembly.GetExecutingAssembly());

            NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
            List<Users> user = new List<Users>();
            TotalPages pages = new TotalPages();
            Keyboards keyboards = new Keyboards();

            // Initialize object Users
            
        //    keyboards.menuKeyboardSelectFinalWinnerTeam = new InlineKeyboardMarkup(new[]
        //    {
        //    new[]
        //        {
        //            InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"), "mainMenu")
        //        }
        //});
        //    keyboards.menuKeyboardSelectWinner = new InlineKeyboardMarkup(new[]
        //    {
        //    new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"), "mainMenu") }
        //});
        //    keyboards.menuKeyboardSelectMatch = new InlineKeyboardMarkup(new[]
        //    {
        //    new[]
        //                {
        //                    InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"), "mainMenu")
        //                }
        //});
        //    keyboards.menuKeyboardSelectPlayer = new InlineKeyboardMarkup(new[]
        //    {
        //    new[]
        //                {
        //                    InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"), "mainMenu")
        //                }
        //});
        //    keyboards.menuKeyboardSelectTopVoter = new InlineKeyboardMarkup(new[]
        //    {
        //    new[]
        //        {
        //            InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"), "mainMenu")
        //        }
        //});
        //    keyboards.menuKeyboardHistory = new InlineKeyboardMarkup(new[]{
        //            new[]{InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"), "mainMenu")}});

            //bot.StartReceiving(
            //    HandleUpdatesAsync,
            //    HandleErrorAsync,
            //    new ReceiverOptions { AllowedUpdates = { } },
            //    cancellationToken: new CancellationTokenSource().Token);

            ///Functions block
            ///
            void getVoters(long ChatID) // Get parameters for voters function
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    OracleCommand cmd = new OracleCommand("Get_VotersFunc", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                    cmd.ExecuteNonQuery();

                    OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                    var a = myClob.Value;
                    var d = JObject.Parse(a);
                    JArray array = (JArray)d["Result"];
                    var totalData = array.Count;
                    for (int i = 0; i < totalData; i++)
                        if (user.Exists(existentUser => existentUser.UserID == ChatID))
                            if (user.Exists(existentUser => existentUser.UserID == ChatID && existentUser.UserID == Convert.ToInt64(d["Result"][i]["ChatID"])))
                            {
                                Users currentUser = user.Where(existentUser => existentUser.UserID == ChatID).First();
                                currentUser.Language = d["Result"][i]["Language"].ToString();
                                currentUser.FirstName = d["Result"][i]["FirstName"].ToString();
                                currentUser.LastName = d["Result"][i]["LastName"].ToString();
                                currentUser.VotedFinalTeam = d["Result"][i]["TeamName"].ToString();
                            }
                }
            } // End get parameters for voters function
            bool Set_New_Voter(string Name, string Surname, string Phone, string Language, long ChatID) // Set new voter function
            {
                try
                {
                    using (OracleConnection conn = new OracleConnection(connectionString))
                    {
                        conn.Open();
                        using (OracleCommand cmd = conn.CreateCommand())
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.CommandText = "Set_New_Voter";
                            cmd.Parameters.Add("Voter_Name", OracleDbType.Varchar2).Value = Name;
                            cmd.Parameters.Add("Voter_Surname", OracleDbType.Varchar2).Value = Surname;
                            cmd.Parameters.Add("Voter_Phone", OracleDbType.Varchar2).Value = Phone;
                            cmd.Parameters.Add("Voter_Language", OracleDbType.Varchar2).Value = Language;
                            cmd.Parameters.Add("ChatID", OracleDbType.Int64).Value = ChatID;
                            cmd.ExecuteScalar();
                            if (user.Exists(existentUser => existentUser.UserID == ChatID))
                            {
                                Users currentUser = user.Where(existentUser => existentUser.UserID == ChatID).First();

                                if (Name != null && Name != "")
                                {
                                    currentUser.FirstName = Name;
                                }
                                if (Surname != null && Surname != "")
                                {
                                    currentUser.LastName = Surname;
                                }
                                if (Language != null)
                                {
                                    currentUser.Language = Language.ToString();
                                }
                            }
                            else
                            {
                                user.Add(new Users() { UserID = Convert.ToInt64(ChatID) });
                            }
                            return true;
                        }
                    }
                }
                catch (Exception exception)
                {
                    WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                    WebResponse response = request.GetResponse();
                    logger.Error(exception.Message);
                    return false;
                }
            } // End set new voter function
            void changeSelectWinnerTeamPage(long ChatID, int? curPage) // Select winner final team page, start with 0 end with totalWinnerPages
            {
                try
                {
                    using (OracleConnection conn = new OracleConnection(connectionString))
                    {
                        if (curPage == null) curPage = 0;
                        conn.Open();
                        OracleCommand cmd = new OracleCommand("Get_TeamsFunc", conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        OracleParameter returnParameter = new OracleParameter();
                        cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                        cmd.Parameters.Add("P_Row", OracleDbType.Int32).Value = curPage;
                        cmd.ExecuteNonQuery();

                        OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                        var valuesFromClob = myClob.Value;
                        var dataFromClob = JObject.Parse(valuesFromClob);
                        JArray array = (JArray)dataFromClob["Result"];
                        var totalData = array.Count;
                        pages.totalTeamPages = Convert.ToInt32(dataFromClob["TotalRows"]);

                        Users currentUser = user.Where(existentUser => existentUser.UserID == ChatID).First();
                        if (currentUser.Language == "ro")
                        {
                            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ro");
                        }
                        else if (currentUser.Language == "ru")
                        {
                            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ru");
                        }
                        else
                        {
                            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("en");
                        }

                        List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();


                        for (var i = 0; i < totalData; i++)
                        {
                            buttons.Add(InlineKeyboardButton.WithCallbackData(
                               $"{dataFromClob["Result"][i]["TeamName"]}",
                                   $"TeamID{dataFromClob["Result"][i]["TeamID"]}"
                            ));
                        }
                        buttons.Add(InlineKeyboardButton.WithCallbackData("❮❮", "WinnerTeam FirstPage"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("❮", "WinnerTeam PreventPage"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData($"{curPage + 1} / {pages.totalTeamPages}", "PageNow"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("❯", "WinnerTeam NextPage"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("❯❯", "WinnerTeam LastPage"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData(ResManager.GetString("Back"), "backToPlayMenu"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"), "mainMenu"));

                        var menu = new List<InlineKeyboardButton[]>();
                        for (int i = 0; i < buttons.Count - 1; i++)
                        {
                            if (buttons.Count - 2 == i)
                            {
                                menu.Add(new[] { buttons[i], buttons[i + 1] });
                                i += 1;
                            }
                            if (buttons.Count - 7 == i)
                            {
                                menu.Add(new[] { buttons[i], buttons[i + 1], buttons[i + 2], buttons[i + 3], buttons[i + 4] });
                                i += 4;
                            }
                            else if (i != buttons.Count - 1)
                            {
                                menu.Add(new[] { buttons[i] });
                            }
                        }
                        keyboards.MenuKeyboardSelectFinalWinnerTeam = new InlineKeyboardMarkup(menu.ToArray());
                    }
                }
                catch (Exception exception)
                {
                    WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                    WebResponse response = request.GetResponse();
                    logger.Warn(exception.Message);
                }
            } // End select winner final team page, start with 0 end with totalWinnerPages
            string changeSelectHistoryPage(long ChatID, int? curPage) // Get history form all prognoses
            {
                string historyDetails = "";
                try
                {
                    using (OracleConnection conn = new OracleConnection(connectionString))
                    {
                        conn.Open();
                        OracleCommand cmd = new OracleCommand("Get_HistoryFunc", conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        OracleParameter returnParameter = new OracleParameter();
                        cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                        cmd.Parameters.Add("P_Row", OracleDbType.Int32).Value = curPage;
                        cmd.Parameters.Add("P_UserID", OracleDbType.Int32).Value = ChatID;
                        cmd.ExecuteNonQuery();

                        OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                        var valuesFromClob = myClob.Value;
                        var dataFromClob = JObject.Parse(valuesFromClob);
                        JArray array = (JArray)dataFromClob["Result"];
                        var totalData = array.Count;
                        pages.totalHistoryPages = Convert.ToInt32(dataFromClob["TotalRows"]) == 0 ? 1 : Convert.ToInt32(dataFromClob["TotalRows"]);

                        Users currentUser = user.Where(existentUser => existentUser.UserID == ChatID).First();
                        if (currentUser.Language == "ro")
                        {
                            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ro");
                        }
                        else if (currentUser.Language == "ru")
                        {
                            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ru");
                        }
                        else
                        {
                            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("en");
                        }

                        List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();

                        //var keyboardInline = new InlineKeyboardButton[totalData][];

                        if (totalData != 0)
                        {
                            for (var i = 0; i < totalData; i++)
                            {
                                historyDetails += dataFromClob["Result"][i]["PrognosedDate"].ToString();
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 1) // First team selected
                                {
                                    historyDetails += $" - {ResManager.GetString("You have select")} {dataFromClob["Result"][i]["PrognosedTeam"]} {ResManager.GetString("from match between")} {dataFromClob["Result"][i]["MatchFTeam"]} {ResManager.GetString("and")} {dataFromClob["Result"][i]["MatchSTeam"]}\n";
                                }
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 2) // Second team selected
                                {
                                    historyDetails += $" - {ResManager.GetString("You have select")} {dataFromClob["Result"][i]["PrognosedTeam"]} {ResManager.GetString("from match between")} {dataFromClob["Result"][i]["MatchFTeam"]} {ResManager.GetString("and")} {dataFromClob["Result"][i]["MatchSTeam"]}\n";
                                }
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 3) // Equality selected
                                {
                                    historyDetails += $" - {ResManager.GetString("You have select equal from match between")} {dataFromClob["Result"][i]["MatchFTeam"]} {ResManager.GetString("and")} {dataFromClob["Result"][i]["MatchSTeam"]}\n";
                                }
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 4) // Total score selected
                                {
                                    historyDetails += $" - {ResManager.GetString("You have select score")} {dataFromClob["Result"][i]["FirstTeamScore"]}-{dataFromClob["Result"][i]["SecondTeamScore"]} {ResManager.GetString("from match between")} {dataFromClob["Result"][i]["MatchFTeam"]} {ResManager.GetString("and")} {dataFromClob["Result"][i]["MatchSTeam"]}\n";
                                }
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 5) // Player selected
                                {
                                    historyDetails += $" - {ResManager.GetString("You have select player")} {dataFromClob["Result"][i]["PlayerName"]} {ResManager.GetString("from team")} {dataFromClob["Result"][i]["PlayerTeam"]} {ResManager.GetString("from match between")} {dataFromClob["Result"][i]["MatchFTeam"]} {ResManager.GetString("and")} {dataFromClob["Result"][i]["MatchSTeam"]}\n";
                                }
                                if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 6) // Team selected
                                {
                                    historyDetails += $" - {ResManager.GetString("You have chosen the team that will come out in the end")} {dataFromClob["Result"][i]["TeamName"]}\n";
                                }
                            }
                        }
                        else
                        {
                            historyDetails = ResManager.GetString("You have taken no action");
                        }
                        buttons.Add(InlineKeyboardButton.WithCallbackData("❮❮", "History FirstPage"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("❮", "History PreventPage"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData($"{curPage + 1} / {pages.totalHistoryPages}", "prognoseHistory"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("❯", "History NextPage"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("❯❯", "History LastPage"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData(ResManager.GetString("Back"), "mainMenu"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"), "mainMenu"));

                        var menu = new List<InlineKeyboardButton[]>();
                        for (int i = 0; i < buttons.Count - 1; i++)
                        {
                            if (buttons.Count - 2 == i)
                            {
                                menu.Add(new[] { buttons[i], buttons[i + 1] });
                                i += 1;
                            }
                            if (buttons.Count - 7 == i)
                            {
                                menu.Add(new[] { buttons[i], buttons[i + 1], buttons[i + 2], buttons[i + 3], buttons[i + 4] });
                                i += 4;
                            }
                            else if (i != buttons.Count - 1)
                            {
                                menu.Add(new[] { buttons[i] });
                            }
                        }
                        keyboards.MenuKeyboardHistory = new InlineKeyboardMarkup(menu.ToArray());
                    }
                    return historyDetails;
                }
                catch (Exception exception)
                {
                    WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                    WebResponse response = request.GetResponse();
                    logger.Warn(exception.Message);
                    return "Something went wrong";
                }
            } // End get history form all prognoses
            string SelectPrognoseFromMatch(long ChatID, int? matchID) // Get prognose for one of selected match
            {
                string prognoseDetails = "";
                try
                {
                    using (OracleConnection conn = new OracleConnection(connectionString))
                    {
                        if (matchID == null) matchID = 0;
                        conn.Open();
                        OracleCommand cmd = new OracleCommand("Get_PrognoseFunc", conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        OracleParameter returnParameter = new OracleParameter();
                        cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                        cmd.Parameters.Add("P_UserID", OracleDbType.Int32).Value = ChatID;
                        cmd.Parameters.Add("P_MatchID", OracleDbType.Int32).Value = matchID;
                        cmd.ExecuteNonQuery();

                        OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                        var valuesFromClob = myClob.Value;
                        var dataFromClob = JObject.Parse(valuesFromClob);
                        JArray array = (JArray)dataFromClob["Result"];
                        var totalData = array.Count;
                        JArray array1 = (JArray)dataFromClob["Players"];
                        var totalPlayersData = array1.Count;
                        List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();

                        Users currentUser = user.Where(existentUser => existentUser.UserID == ChatID).First();
                        if (currentUser.Language == "ro")
                        {
                            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ro");
                        }
                        else if (currentUser.Language == "ru")
                        {
                            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ru");
                        }
                        else
                        {
                            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("en");
                        }

                        string firstTypeSelected = ResManager.GetString("None");
                        string secondTypeSelected = ResManager.GetString("None");
                        string thirdTypeSelected = ResManager.GetString("None");
                        //var keyboardInline = new InlineKeyboardButton[totalData][];
                        if (totalData != 0)
                        {
                            if (Convert.ToDateTime(dataFromClob["MatchStart"]) < DateTime.Now)
                            {
                                for (var i = 0; i < totalData; i++)
                                {
                                    //prognoseDetails += d["Result"][i]["PrognosedDate"].ToString();
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 1) // First team selected
                                    {
                                        // ✅
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyFteamPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"{ResManager.GetString("the winning team")}: {dataFromClob["Result"][i]["PrognosedTeam"]} ✅";
                                        }
                                        // ❌
                                        else
                                        {
                                            firstTypeSelected = $"{ResManager.GetString("the winning team")}: {dataFromClob["Result"][i]["PrognosedTeam"]} ❌";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 2) // Second team selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlySteamPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"{ResManager.GetString("the winning team")}: {dataFromClob["Result"][i]["PrognosedTeam"]} ✅";
                                        }
                                        else
                                        {
                                            firstTypeSelected = $"{ResManager.GetString("the winning team")}: {dataFromClob["Result"][i]["PrognosedTeam"]} ❌";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 3) // Equality selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyEqualPrognose"]) == 1)
                                        {
                                            firstTypeSelected = $"{ResManager.GetString("You have select equal")} ✅";
                                        }
                                        else
                                        {
                                            firstTypeSelected = $"{ResManager.GetString("You have select equal")} ❌";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 4) // Total score selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyTotalPrognose"]) == 1)
                                        {
                                            secondTypeSelected = $"{dataFromClob["Result"][i]["FirstTeamScore"]} - {dataFromClob["Result"][i]["SecondTeamScore"]} ✅";
                                        }
                                        else
                                        {
                                            secondTypeSelected = $"{dataFromClob["Result"][i]["FirstTeamScore"]} - {dataFromClob["Result"][i]["SecondTeamScore"]} ❌";
                                        }
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 5) // Player selected
                                    {
                                        if (Convert.ToInt32(dataFromClob["Result"][i]["CorrectlyPlayerPrognose"]) == 1)
                                        {
                                            thirdTypeSelected = $"{ResManager.GetString("You have select player")}: {dataFromClob["Result"][i]["PlayerName"]} {ResManager.GetString("from team")} {dataFromClob["Result"][i]["PlayerTeam"]} ✅";
                                        }
                                        else
                                        {
                                            thirdTypeSelected = $"{ResManager.GetString("You have select player")}: {dataFromClob["Result"][i]["PlayerName"]} {ResManager.GetString("from team")} {dataFromClob["Result"][i]["PlayerTeam"]} ❌";
                                        }
                                    }
                                }
                                prognoseDetails = $"{ResManager.GetString("In the match between:")} {currentUser.VotedFirstTeam} {ResManager.GetString("and")} {currentUser.VotedSecondTeam}\n";
                                if (firstTypeSelected != ResManager.GetString("None"))
                                {
                                    prognoseDetails += $"\n- {firstTypeSelected}";
                                }
                                if (secondTypeSelected != ResManager.GetString("None"))
                                {
                                    prognoseDetails += $"\n- {ResManager.GetString("Total")}: {secondTypeSelected}";
                                }
                                if (thirdTypeSelected != ResManager.GetString("None"))
                                {
                                    prognoseDetails += $"\n- {thirdTypeSelected}";
                                }
                                if (firstTypeSelected == ResManager.GetString("None") && secondTypeSelected == ResManager.GetString("None") && thirdTypeSelected == ResManager.GetString("None"))
                                {
                                    prognoseDetails += "\n" + ResManager.GetString("None");
                                }

                                prognoseDetails += $"\n\n{ResManager.GetString("The final score")}\n{dataFromClob["TotalScore"][0]["FirstTeamName"]} - {dataFromClob["TotalScore"][0]["FirstTeamScore"]}\n" +
                                        $"{dataFromClob["TotalScore"][0]["SecondTeamName"]} - {dataFromClob["TotalScore"][0]["SecondTeamScore"]}\n\n{ResManager.GetString("Players with goals:")}\n";
                                for (int i = 0; i < totalPlayersData; i++)
                                    prognoseDetails += $"{dataFromClob["Players"][i]["PlayerName"]} {ResManager.GetString("from team")} {dataFromClob["Players"][i]["TeamName"]}\n";
                                //prognoseDetails = $"{ResManager.GetString("You have vote:")}" +
                                //    $"\n- {ResManager.GetString("Teams or Equal")} - {firstTypeSelected}" +
                                //    $"\n- {ResManager.GetString("Total")} - {secondTypeSelected}" +
                                //    $"\n- {ResManager.GetString("Players")} - {thirdTypeSelected}";
                            }
                            else
                            {
                                for (var i = 0; i < totalData; i++)
                                {
                                    //prognoseDetails += d["Result"][i]["PrognosedDate"].ToString();
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 1) // First team selected
                                    {
                                        // ✅ // ❌
                                        firstTypeSelected = $"{ResManager.GetString("the winning team")}: {dataFromClob["Result"][i]["PrognosedTeam"]}";
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 2) // Second team selected
                                    {
                                        firstTypeSelected = $"{ResManager.GetString("the winning team")}: {dataFromClob["Result"][i]["PrognosedTeam"]} ";
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 3) // Equality selected
                                    {
                                        firstTypeSelected = $"{ResManager.GetString("You have select equal")}";
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 4) // Total score selected
                                    {
                                        secondTypeSelected = $"{dataFromClob["Result"][i]["FirstTeamScore"]} - {dataFromClob["Result"][i]["SecondTeamScore"]} ";
                                    }
                                    if (Convert.ToInt32(dataFromClob["Result"][i]["PrognosedType"]) == 5) // Player selected
                                    {
                                        thirdTypeSelected = $"{ResManager.GetString("You have select player")}: {dataFromClob["Result"][i]["PlayerName"]} {ResManager.GetString("from team")} {dataFromClob["Result"][i]["PlayerTeam"]} ";
                                    }
                                }
                                prognoseDetails = $"{ResManager.GetString("In the match between:")} {currentUser.VotedFirstTeam} {ResManager.GetString("and")} {currentUser.VotedSecondTeam}\n";
                                if (firstTypeSelected != ResManager.GetString("None"))
                                {
                                    prognoseDetails += $"\n- {firstTypeSelected}";
                                }
                                if (secondTypeSelected != ResManager.GetString("None"))
                                {
                                    prognoseDetails += $"\n- {ResManager.GetString("Total")}: {secondTypeSelected}";
                                }
                                if (thirdTypeSelected != ResManager.GetString("None"))
                                {
                                    prognoseDetails += $"\n- {thirdTypeSelected}";
                                }
                            }
                        }
                        else
                        {
                            //prognoseDetails += $"{ResManager.GetString("You have vote:")}\n- {ResManager.GetString("Teams or Equal")} - {ResManager.GetString("None")}\n- {ResManager.GetString("Total")} - {ResManager.GetString("None")}\n- {ResManager.GetString("Players")} - {ResManager.GetString("None")}";
                            if (Convert.ToDateTime(dataFromClob["MatchStart"]) > DateTime.Now)
                            {
                                prognoseDetails += $"{ResManager.GetString("vote please")}";
                            }
                            else if (Convert.ToDateTime(dataFromClob["MatchStart"]) < DateTime.Now)
                            {
                                prognoseDetails += $"Nu ati introdus datele";/*{ResManager.GetString("vote please")}*/
                                prognoseDetails += $"\n\n{ResManager.GetString("The final score")}\n{dataFromClob["TotalScore"][0]["FirstTeamName"]} - {dataFromClob["TotalScore"][0]["FirstTeamScore"]}\n" +
                                        $"{dataFromClob["TotalScore"][0]["SecondTeamName"]} - {dataFromClob["TotalScore"][0]["SecondTeamScore"]}\n\n{ResManager.GetString("Players with goals:")}\n";
                                for (int i = 0; i < totalPlayersData; i++)
                                    prognoseDetails += $"{dataFromClob["Players"][i]["PlayerName"]} {ResManager.GetString("from team")} {dataFromClob["Players"][i]["TeamName"]}\n";
                            }
                        }
                    }
                    return prognoseDetails;
                }
                catch (Exception exception)
                {
                    WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                    WebResponse response = request.GetResponse();
                    logger.Warn(exception.Message);
                    return "Something went wrong";
                }
            } // End get prognose for one of selected match
            void changeSelectWinnerPage(long ChatID, int? curPage) // Select winner page
            {
                try
                {
                    using (OracleConnection conn = new OracleConnection(connectionString))
                    {
                        if (curPage == null) curPage = 0;
                        conn.Open();
                        OracleCommand cmd = new OracleCommand("Get_MatchesFunc", conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        OracleParameter returnParameter = new OracleParameter();
                        cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                        cmd.Parameters.Add("P_Match_ID", OracleDbType.Int32).Value = curPage;
                        //OracleClob Clob = new OracleClob(conn);
                        cmd.ExecuteNonQuery();

                        OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                        var valuesFromClob = myClob.Value;
                        var dataFromClob = JObject.Parse(valuesFromClob);
                        JArray array = (JArray)dataFromClob["Result"];
                        var totalData = array.Count;
                        pages.totalMatchPages = Convert.ToInt32(dataFromClob["TotalRows"]);

                        List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
                        Users currentUser = user.Where(existentUser => existentUser.UserID == ChatID).First();
                        if (currentUser.Language == "ro")
                        {
                            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ro");
                        }
                        else if (currentUser.Language == "ru")
                        {
                            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ru");
                        }
                        else
                        {
                            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("en");
                        }
                        for (var i = 0; i < totalData; i++)
                        {
                            buttons.Add(InlineKeyboardButton.WithCallbackData(
                               $"{dataFromClob["Result"][i]["FirstTeam"]} VS {dataFromClob["Result"][i]["SecondTeam"]} \n\r ( {dataFromClob["Result"][i]["StartDate"]} )",
                                   $"MatchID{dataFromClob["Result"][i]["MatchID"]}"
                            ));
                        }
                        buttons.Add(InlineKeyboardButton.WithCallbackData("❮❮", "Winner FirstPage"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("❮", "Winner PreventPage"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData($"{curPage + 1} / {pages.totalMatchPages}", "selectWinner"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("❯", "Winner NextPage"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("❯❯", "Winner LastPage"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData(ResManager.GetString("Back"), "backToPlayMenu"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"), "mainMenu"));

                        var menu = new List<InlineKeyboardButton[]>();
                        for (int i = 0; i < buttons.Count - 1; i++)
                        {
                            if (buttons.Count - 2 == i)
                            {
                                menu.Add(new[] { buttons[i], buttons[i + 1] });
                                i += 1;
                            }
                            if (buttons.Count - 7 == i)
                            {
                                menu.Add(new[] { buttons[i], buttons[i + 1], buttons[i + 2], buttons[i + 3], buttons[i + 4] });
                                i += 4;
                            }
                            else if (i != buttons.Count - 1)
                            {
                                menu.Add(new[] { buttons[i] });
                            }
                        }
                        keyboards.MenuKeyboardSelectWinner = new InlineKeyboardMarkup(menu.ToArray());
                    }
                }
                catch (Exception exception)
                {
                    WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                    WebResponse response = request.GetResponse();
                    logger.Warn(exception.Message);
                }
            } // End select winner page
            void changeSelectTopVotersPage(long ChatID, int? curPage) // Select topVoters page
            {
                try
                {
                    using (OracleConnection conn = new OracleConnection(connectionString))
                    {
                        //if (curPage == null) curPage = 0;
                        conn.Open();
                        OracleCommand cmd = new OracleCommand("Get_TopVotersFunc", conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        OracleParameter returnParameter = new OracleParameter();
                        cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                        cmd.Parameters.Add("P_Row", OracleDbType.Int32).Value = curPage;
                        //OracleClob Clob = new OracleClob(conn);
                        cmd.ExecuteNonQuery();

                        OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                        var valuesFromClob = myClob.Value;
                        var dataFromClob = JObject.Parse(valuesFromClob);
                        JArray array = (JArray)dataFromClob["Result"];
                        var totalData = array.Count;
                        pages.totalTopVotersPages = Convert.ToInt32(dataFromClob["TotalRows"]);

                        Users currentUser = user.Where(existentUser => existentUser.UserID == ChatID).First();
                        if (currentUser.Language == "ro")
                        {
                            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ro");
                        }
                        else if (currentUser.Language == "ru")
                        {
                            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ru");
                        }
                        else
                        {
                            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("en");
                        }
                        List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();

                        for (var i = 0; i < totalData; i++)
                        {
                            buttons.Add(InlineKeyboardButton.WithCallbackData(
                               $"{dataFromClob["Result"][i]["FirstName"]} {dataFromClob["Result"][i]["SecondName"]} Score: ( {dataFromClob["Result"][i]["Score"]} )",
                                   "topVoters"
                            ));
                        }
                        buttons.Add(InlineKeyboardButton.WithCallbackData("❮❮", "TopVoters FirstPage"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("❮", "TopVoters PreventPage"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData($"{curPage + 1} / {pages.totalTopVotersPages}", "topVoters"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("❯", "TopVoters NextPage"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("❯❯", "TopVoters LastPage"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData(ResManager.GetString("Back"), "mainMenu"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"), "mainMenu"));

                        var menu = new List<InlineKeyboardButton[]>();
                        for (int i = 0; i < buttons.Count - 1; i++)
                        {
                            if (buttons.Count - 2 == i)
                            {
                                menu.Add(new[] { buttons[i], buttons[i + 1] });
                                i += 1;
                            }
                            if (buttons.Count - 7 == i)
                            {
                                menu.Add(new[] { buttons[i], buttons[i + 1], buttons[i + 2], buttons[i + 3], buttons[i + 4] });
                                i += 4;
                            }
                            else if (i != buttons.Count - 1)
                            {
                                menu.Add(new[] { buttons[i] });
                            }
                        }
                        keyboards.MenuKeyboardSelectTopVoter = new InlineKeyboardMarkup(menu.ToArray());
                    }
                }
                catch (Exception exception)
                {
                    WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                    WebResponse response = request.GetResponse();
                    logger.Warn(exception.Message);
                }
            } // End select topVoters page
            void changeSelectMatchPage(long ChatID, int matchID) // Select match pages
            {
                try
                {
                    using (OracleConnection conn = new OracleConnection(connectionString))
                    {
                        conn.Open();
                        OracleCommand cmd = new OracleCommand("Get_MenuMatchesFunc", conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        OracleParameter returnParameter = new OracleParameter();
                        cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                        cmd.Parameters.Add("P_Match_ID", OracleDbType.Int32).Value = matchID;
                        cmd.ExecuteNonQuery();

                        OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                        var valuesFromClob = myClob.Value;
                        var dataFromClob = JObject.Parse(valuesFromClob);
                        if (user.Exists(existentUser => existentUser.UserID == ChatID))
                        {
                            Users currentUser = user.Where(existentUser => existentUser.UserID == ChatID).First();
                            if (currentUser.Language == "ro")
                            {
                                Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ro");
                            }
                            else if (currentUser.Language == "ru")
                            {
                                Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ru");
                            }
                            else
                            {
                                Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("en");
                            }
                            currentUser.VotedFirstTeam = dataFromClob["Result"][0]["FirstTeamName"].ToString();
                            currentUser.VotedSecondTeam = dataFromClob["Result"][0]["SecondTeamName"].ToString();
                        }
                        List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();

                        var menu = new List<InlineKeyboardButton[]>();
                        var date = DateTime.Now;
                        if (Convert.ToDateTime(dataFromClob["Result"][0]["StartTime"]) > date)
                        {
                            buttons.Add(InlineKeyboardButton.WithCallbackData($"{dataFromClob["Result"][0]["FirstTeamName"]}", $"VoteFirstTeamName--{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData($"{dataFromClob["Result"][0]["SecondTeamName"]}", $"VoteSecondTeamName--{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData(ResManager.GetString("Equal"), $"VoteEqual--{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData($"👉🏻 {ResManager.GetString("Select final score of the match")} 👈🏻", $"VoteTotal--{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData($"{dataFromClob["Result"][0]["FirstTeamName"]} {ResManager.GetString("Players")}", $"VotePlayers+{dataFromClob["Result"][0]["FirstTeamName"]}+{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData($"{dataFromClob["Result"][0]["SecondTeamName"]} {ResManager.GetString("Players")}", $"VotePlayers+{dataFromClob["Result"][0]["SecondTeamName"]}+{matchID}"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData(ResManager.GetString("Back"), "backToWinnerMenu"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"), "mainMenu"));

                            for (int i = 0; i < buttons.Count - 1; i++)
                            {
                                if (buttons.Count - 2 == i)
                                {
                                    menu.Add(new[] { buttons[i], buttons[i + 1] });
                                    i += 1;
                                }
                                else if (buttons.Count - 4 == i)
                                {
                                    menu.Add(new[] { buttons[i], buttons[i + 1] });
                                    i += 1;
                                }
                                else if (buttons.Count - 8 == i)
                                {
                                    menu.Add(new[] { buttons[i], buttons[i + 1], buttons[i + 2] });
                                    i += 2;
                                }
                                else if (i != buttons.Count - 1)
                                {
                                    menu.Add(new[] { buttons[i] });
                                }
                            }
                            keyboards.MenuKeyboardSelectMatch = new InlineKeyboardMarkup(menu.ToArray());
                        }
                        else
                        {
                            buttons.Add(InlineKeyboardButton.WithCallbackData(ResManager.GetString("Back"), "backToWinnerMenu"));
                            buttons.Add(InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"), "mainMenu"));

                            menu.Add(new[] { buttons[0], buttons[1] });
                            keyboards.MenuKeyboardSelectMatch = new InlineKeyboardMarkup(menu.ToArray());
                        }
                    }
                }
                catch (Exception exception)
                {
                    WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                    WebResponse response = request.GetResponse();
                    logger.Warn(exception.Message);
                }
            } // End select match pages
            void changeSelectPlayersPage(long ChatID, string teamName, int? curPage) // Select players page, voters can select who player has get a goal
            {
                try
                {
                    using (OracleConnection conn = new OracleConnection(connectionString))
                    {
                        if (curPage == null) curPage = 0;
                        conn.Open();
                        OracleCommand cmd = new OracleCommand("Get_PlayersFunc", conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        OracleParameter returnParameter = new OracleParameter();
                        cmd.Parameters.Add("Res", OracleDbType.Clob, ParameterDirection.ReturnValue);
                        cmd.Parameters.Add("TeamName", OracleDbType.Varchar2).Value = teamName;
                        cmd.Parameters.Add("P_Row", OracleDbType.Int32).Value = curPage;
                        cmd.ExecuteNonQuery();

                        OracleClob myClob = (OracleClob)cmd.Parameters["Res"].Value;
                        var valuesFromClob = myClob.Value;
                        var dataFromClob = JObject.Parse(valuesFromClob);
                        JArray array = (JArray)dataFromClob["Result"];
                        var totalData = array.Count;
                        pages.totalPlayersPages = Convert.ToInt32(dataFromClob["TotalRows"]);

                        if (user.Exists(existentUser => existentUser.UserID == ChatID))
                        {
                            Users currentUser = user.Where(existentUser => existentUser.UserID == ChatID).First();
                            currentUser.VotedPlayer = dataFromClob["Result"][0]["PlayerName"].ToString();
                        }

                        List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();

                        for (var i = 0; i < totalData; i++)
                        {
                            buttons.Add(InlineKeyboardButton.WithCallbackData(
                               $"{dataFromClob["Result"][i]["PlayerName"]}",
                                   $"VotePlayerID{dataFromClob["Result"][i]["PlayerID"]}"
                            ));
                        }
                        buttons.Add(InlineKeyboardButton.WithCallbackData("❮❮", "Players FirstPage"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("❮", "Players PreventPage"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData($"{curPage + 1} / {pages.totalPlayersPages}", "VotePlayers"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("❯", "Players NextPage"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData("❯❯", "Players LastPage"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData(ResManager.GetString("Back"), "backToSelectedMatchMenu"));
                        buttons.Add(InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"), "mainMenu"));

                        var menu = new List<InlineKeyboardButton[]>();
                        for (int i = 0; i < buttons.Count - 1; i++)
                        {
                            if (buttons.Count - 2 == i)
                            {
                                menu.Add(new[] { buttons[i], buttons[i + 1] });
                                i += 1;
                            }
                            if (buttons.Count - 7 == i)
                            {
                                menu.Add(new[] { buttons[i], buttons[i + 1], buttons[i + 2], buttons[i + 3], buttons[i + 4] });
                                i += 4;
                            }
                            else if (i != buttons.Count - 1)
                            {
                                menu.Add(new[] { buttons[i] });
                            }
                        }
                        keyboards.MenuKeyboardSelectPlayer = new InlineKeyboardMarkup(menu.ToArray());
                    }
                }
                catch (Exception exception)
                {
                    WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                    WebResponse response = request.GetResponse();
                    logger.Warn(exception.Message);
                }
            } // End select players page, voters can select who player has get a goal
            void voteFinalTeam(long ChatID, int teamID) // Vote final team function
            {
                try
                {
                    using (OracleConnection conn = new OracleConnection(connectionString))
                    {
                        conn.Open();
                        OracleCommand cmd = new OracleCommand("PrognoseFinalTeam", conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("VoterChatID", OracleDbType.Int32).Value = ChatID;
                        cmd.Parameters.Add("TeamID", OracleDbType.Int32).Value = teamID;
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception exception)
                {
                    WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                    WebResponse response = request.GetResponse();
                    logger.Warn(exception.Message);
                }
            } // End vote final team function
            void voteFromMatch(long ChatID, int? matchID, int? voteType, int? teamScore_1, int? teamScore_2, int? votedPlayer, int? votedTeam) // Vote from match function
            {
                try
                {
                    using (OracleConnection conn = new OracleConnection(connectionString))
                    {
                        conn.Open();
                        OracleCommand cmd = new OracleCommand("PrognoseVote", conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("VoterChatID", OracleDbType.Int32).Value = ChatID;
                        cmd.Parameters.Add("MatchID", OracleDbType.Int32).Value = matchID;
                        cmd.Parameters.Add("Vote_Type", OracleDbType.Int32).Value = voteType;
                        cmd.Parameters.Add("Team_Score1", OracleDbType.Int32).Value = teamScore_1;
                        cmd.Parameters.Add("Team_Score2", OracleDbType.Int32).Value = teamScore_2;
                        cmd.Parameters.Add("Voted_Player", OracleDbType.Int32).Value = votedPlayer;
                        cmd.Parameters.Add("Voted_Team", OracleDbType.Int32).Value = votedTeam;
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                    logger.Warn(exception.Message);
                    WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                    WebResponse response = request.GetResponse();
                }
            } // End vote from match function
            ///End functions block
            ///
            
           

            

           

            var me = await bot.GetMeAsync();

            Console.WriteLine($"Bot started: @{me.Username}");
            Console.ReadLine();
            new CancellationTokenSource().Cancel();
        }
    }
}