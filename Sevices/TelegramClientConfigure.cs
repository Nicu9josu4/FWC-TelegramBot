using System;
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
using Keyboards = BookmakerTelegramBot.Models.MainKeyboards;
using TotalPages = BookmakerTelegramBot.Models.TotalPages;
using Users = BookmakerTelegramBot.Models.Users;
using BookmakerTelegramBot.Controllers;
using Microsoft.Extensions.Configuration;

namespace BookmakerTelegramBot.Sevices
{
    public class TelegramClientConfigure
    {
        public DbMethodsController Controller = new DbMethodsController();
        public NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public Users users = new Users();
        public TotalPages pages = new TotalPages();
        public Keyboards keyboards = new Keyboards();
        public string connectionString;
        public string Token;
        //public string Configuration.GetConnectionString("DefaultConnection") = Configuration.GetConnectionString("DefaultConnection");
        public ResourceManager ResManager = new ResourceManager("BookmakerTelegramBot.Resources.Resources", Assembly.GetExecutingAssembly());

        public async Task StartClient()
        {
            var bot = new TelegramBotClient(Token);
            users.SetValuesFromDb(connectionString);
            
            Controller.users = users;
            Controller.pages = pages;
            Controller.keyboards = keyboards;

            bot.StartReceiving(
                    HandleUpdatesAsync,
                    HandleErrorAsync,
                    new ReceiverOptions { AllowedUpdates = { } },
                    cancellationToken: new CancellationTokenSource().Token);

            var me = await bot.GetMeAsync();
            Console.WriteLine($"Bot started: @{me.Username}");
            Console.ReadLine();
            new CancellationTokenSource().Cancel();
        }

        private async Task HandleUpdatesAsync(ITelegramBotClient bot, Update update, CancellationToken cts)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Text != null)
            {
                Console.WriteLine("Message:       ID:" + update.Message.Chat.Id + ", Text:        " + update.Message.Text);
                await HandleMessage(bot, update.Message);
                WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text={update.Message.Chat.Id} say: {update.Message.Text}");
                request.GetResponse();
                return;
                // an object reference is required for the non-static field
            }
            if (update?.Type == UpdateType.CallbackQuery)
            {
                Console.WriteLine("CallBackQuery: ID:" + update.CallbackQuery.From.Id + ", CallbackData:" + update.CallbackQuery.Data.ToString());
                await HandleCallbackQuery(bot, update.CallbackQuery);
                return;
            }
            return;
        }

        public async Task HandleMessage(ITelegramBotClient botClient, Message message)
        {
            if (users.UsersList.Exists(existentUser => existentUser.UserID == message.Chat.Id))
            {
                // Declare current User
                Users currentUser = users.UsersList.Where(existentUser => existentUser.UserID == message.Chat.Id).First();
                currentUser.TextMessage = message.Text;
                // verify users.UsersList language and change UIculture
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
                int number;
                // /register Command
                if (message.Text == "/register")
                {
                    currentUser.IntroduceTotal = false;
                    currentUser.Registration = true;
                    if (users.UsersList.Exists(existentUser => existentUser.UserID == message.Chat.Id && (existentUser.FirstName != null && existentUser.FirstName != "") && (existentUser.LastName != null && existentUser.LastName != "")))
                    {
                        var menuKeyboard = new InlineKeyboardMarkup(new[]
                        {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(ResManager.GetString("Yes"),"register"),
                            InlineKeyboardButton.WithCallbackData(ResManager.GetString("No"),"mainMenu")
                        }
                                });
                        await botClient.SendTextMessageAsync(message.Chat.Id, ResManager.GetString("You have registered, do you want to change the data?"), replyMarkup: menuKeyboard);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"{ResManager.GetString("Enter your name, surname and phone according to the model:")} Ivan - Ivanov - 79800000");
                    }

                    return;
                }
                // /menu or /start command
                if (message.Text == "/start" || message.Text == "/menu")
                {
                    currentUser.IntroduceTotal = false;
                    currentUser.Registration = false;
                    Controller.SetNewVoter(null, null, null, null, message.Chat.Id);
                    var menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("Start Prognose"),"playGame")},
                            new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("Top Voters"),"topVoters")},
                            new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("History"),"prognoseHistory")},
                            new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("Description"), "description"), InlineKeyboardButton.WithCallbackData(ResManager.GetString("Rules"),"rules")},
                            new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("Settings"), "settings")}});
                    var menuKeyboardSelectLanguage = new InlineKeyboardMarkup(new[]
                    {
                            InlineKeyboardButton.WithCallbackData(ResManager.GetString("Romanian"),"language-ro"),
                        InlineKeyboardButton.WithCallbackData(ResManager.GetString("Russian"),"language-ru"),
                            InlineKeyboardButton.WithCallbackData(ResManager.GetString("English"),"language-en")
                        });
                    if (users.UsersList.Exists(existentUser => existentUser.UserID == message.Chat.Id && existentUser.Language != null && existentUser.FirstName != null && existentUser.FirstName != "" && existentUser.LastName != null && existentUser.LastName != ""))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, ResManager.GetString("Main Menu"), replyMarkup: menuKeyboard);
                    }
                    else if (users.UsersList.Exists(existentUser => existentUser.UserID == message.Chat.Id && (existentUser.Language == null || existentUser.Language == "")))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, ResManager.GetString("Select Language"), replyMarkup: menuKeyboardSelectLanguage);
                    }
                    else if (users.UsersList.Exists(existentUser => existentUser.UserID == message.Chat.Id && (existentUser.FirstName == null || existentUser.FirstName == "") && (existentUser.LastName == null || existentUser.LastName == "")))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"{ResManager.GetString("Try command")} /register");
                    }
                    return;
                }
                // /description command
                if (message.Text == "/description")
                {
                    currentUser.IntroduceTotal = false;
                    await botClient.SendTextMessageAsync(message.Chat.Id, ResManager.GetString("DescriptionText"));
                    return;
                }
                // /rules command
                if (message.Text == "/rules")
                {
                    currentUser.IntroduceTotal = false;
                    await botClient.SendTextMessageAsync(message.Chat.Id, ResManager.GetString("RulesText"));
                    return;
                }
                // inserting total score of the match
                string[] totalScore = message.Text.Split('/', ':');
                if (currentUser.IntroduceTotal == true && totalScore.Length == 2)
                {
                    var menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[]
                                {
                                    InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"), "mainMenu")
                                }
                            });
                    if (totalScore[0] != null && totalScore[1] != null && totalScore[0] != "" && totalScore[1] != "")
                    {
                        if (int.TryParse(totalScore[0].Trim(), out number) && int.TryParse(totalScore[1].Trim(), out number) && Convert.ToInt32(totalScore[0].Trim()) >= 0 && Convert.ToInt32(totalScore[1].Trim()) >= 0)
                        {
                            var menuKeyboardVote = new InlineKeyboardMarkup(new[] {new[]
                                {
                                    InlineKeyboardButton.WithCallbackData(ResManager.GetString("Back"), "backToSelectedMatchMenu"),
                                    InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"),"mainMenu")
                                }
                            });
                            currentUser.IntroduceTotal = false;
                            Controller.VoteFromMatch(Convert.ToInt64(currentUser.UserID), currentUser.MatchID, 4, Convert.ToInt32(totalScore[0]), Convert.ToInt32(totalScore[1]), null, null);
                            await botClient.SendTextMessageAsync(currentUser.UserID, Controller.SelectPrognoseFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: keyboards.MenuKeyboardSelectMatch);
                            await botClient.DeleteMessageAsync(currentUser.UserID, (int)currentUser.MessageID);
                        }
                        else if (Convert.ToInt32(totalScore[0].Trim()) < 0 || Convert.ToInt32(totalScore[1].Trim()) < 0)
                        {
                            await botClient.SendTextMessageAsync(currentUser.UserID, ResManager.GetString("The score cannot be negative"));
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(currentUser.UserID, ResManager.GetString("You entered an incorrect score, try again"));
                        }
                        return;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(currentUser.UserID, ResManager.GetString("You entered an incorrect score, try again"));
                    }
                    return;
                }
                // Registration method
                if (message.Text.Split('-', '_', '.').Length > 2 && message.Text != null && currentUser.Registration == true)
                {
                    var registrationData = message.Text.Trim();
                    string[] subs = registrationData.Split('-', '_', '.');

                    if (subs.Length == 3)
                    {
                        if (Regex.Match(subs[0].Trim(), @"^[a-zA-Z]").Success && Regex.Match(subs[1].Trim(), @"^[a-zA-Z]").Success && int.TryParse(subs[2], out number))
                        {
                            if (Convert.ToInt32(subs[2].Trim().Length) >= 8 && Convert.ToInt32(subs[2].Trim().Length) < 10)
                            {
                                var resultNewVoter = Controller.SetNewVoter(subs[0].Trim(), subs[1].Trim(), subs[2].Trim(), currentUser.Language, Convert.ToInt64(message.Chat.Id));
                                if (resultNewVoter)
                                {
                                    //Console.WriteLine("Success");
                                    currentUser.Registration = false;
                                    await botClient.SendTextMessageAsync(message.Chat.Id, ResManager.GetString("You have registred successfully"), replyMarkup: keyboards.MenuKeyboardSelectFinalWinnerTeam);
                                    //currentUser.registration = false;
                                }
                            }
                            else if (Convert.ToInt32(subs[2].Trim().Length) < 8)
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, ResManager.GetString("Your number does not contain enough digits"));
                            }
                            else if (Convert.ToInt32(subs[2].Trim().Length) > 10)
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, ResManager.GetString("Your number contains too many digits"));
                            }
                        }
                        else if (!Regex.Match(subs[0].Trim(), @"^[a-zA-Z]").Success || !Regex.Match(subs[1].Trim(), @"^[a-zA-Z]").Success)
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, ResManager.GetString("You have entered an incorrect name"));
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, ResManager.GetString("You have entered an incorrect phone number, please try again"));
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, ResManager.GetString("You have not entered enough data, try again"));
                    }
                }
            }
            else // when users.UsersList is not exist is executing this block
            {
                // /menu or /start command
                if (message.Text == "/start" || message.Text == "/menu")
                {
                    // including in the database a new users.UsersList
                    Controller.SetNewVoter(null, null, null, null, message.Chat.Id);
                    // creating a 2 menu keyboards
                    // 1 - for menu with menu buttons
                    // 2 - for selecting language menu if is new users.UsersList
                    var menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("Start Prognose"),"playGame")},
                            new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("Top Voters"),"topVoters")},
                            new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("History"),"prognoseHistory")},
                            new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("Description"), "description"), InlineKeyboardButton.WithCallbackData(ResManager.GetString("Rules"),"rules")},
                            new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("Settings"), "settings")}});
                    var menuKeyboardSelectLanguage = new InlineKeyboardMarkup(new[]
                    {
                            InlineKeyboardButton.WithCallbackData(ResManager.GetString("Romanian"),"language-ro"),
                        InlineKeyboardButton.WithCallbackData(ResManager.GetString("Russian"),"language-ru"),
                            InlineKeyboardButton.WithCallbackData(ResManager.GetString("English"),"language-en")
                        });
                    // Verify if users.UsersList is exists
                    if (users.UsersList.Exists(existentUser => existentUser.UserID == message.Chat.Id && existentUser.Language != null && existentUser.FirstName != null && existentUser.FirstName != "" && existentUser.LastName != null && existentUser.LastName != ""))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, ResManager.GetString("Main Menu"), replyMarkup: menuKeyboard);
                    }
                    // verify if users.UsersList has a language
                    else if (users.UsersList.Exists(existentUser => existentUser.UserID == message.Chat.Id && (existentUser.Language == null || existentUser.Language == "")))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, ResManager.GetString("Select Language"), replyMarkup: menuKeyboardSelectLanguage);
                    }
                    // verify if users.UsersList is not registred and display message try register
                    else if (users.UsersList.Exists(existentUser => existentUser.UserID == message.Chat.Id && (existentUser.FirstName == null || existentUser.FirstName == "") && (existentUser.LastName == null || existentUser.LastName == "")))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"{ResManager.GetString("Try command")} /register");
                    }
                    return;
                }
                return;
            }
            return;
        }

        public async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery cq)
        {
            try
            {
                // verify if users.UsersList exists in database
                if (users.UsersList.Exists(existentUser => existentUser.UserID == cq.Message.Chat.Id && existentUser.FirstName != null && existentUser.FirstName != "" && existentUser.LastName != null && existentUser.LastName != ""))
                {
                    // creating a users.UsersList object for work with them
                    Users currentUser = users.UsersList.Where(existentUser => existentUser.UserID == cq.Message.Chat.Id).First();
                    // change UI culture, and displaing language for a other users.UsersList
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
                    currentUser.CallBackData = cq.Data;
                    currentUser.MessageID = cq.Message.MessageId;

                    currentUser.Registration = false;
                    currentUser.IntroduceTotal = false;
                    // Get Main menu interface //
                    if (currentUser.CallBackData == "mainMenu")
                    {
                        if (currentUser.Language == null)
                        {
                            var menuKeyboardSelectLanguage = new InlineKeyboardMarkup(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(ResManager.GetString("Romanian"),"language-ro"),
                            InlineKeyboardButton.WithCallbackData(ResManager.GetString("Russian"),"language-ru"),
                            InlineKeyboardButton.WithCallbackData(ResManager.GetString("English"),"language-en")
                        });
                            await botClient.EditMessageTextAsync(cq.Message.Chat.Id, cq.Message.MessageId, ResManager.GetString("Select Language") /*"Select Language:"*/, replyMarkup: menuKeyboardSelectLanguage);
                            return;
                        }
                        else
                        {
                            var menuKeyboard = new InlineKeyboardMarkup(new[]{
                                    new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("Start Prognose"),"playGame")},
                                    new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("Top Voters"),"topVoters")},
                                    new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("History"),"prognoseHistory")},
                                    new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("Description"), "description"), InlineKeyboardButton.WithCallbackData(ResManager.GetString("Rules"),"rules")},
                                    new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("Settings"), "settings")}});
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Main Menu") + ".", replyMarkup: menuKeyboard);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Main Menu"), replyMarkup: menuKeyboard);
                            return;
                        }
                    }
                    // Get Top voters list //
                    if (currentUser.CallBackData == "topVoters")
                    {
                        currentUser.CurentTopVotersPage = 0;
                        Controller.ChangeSelectTopVotersPage(currentUser.UserID, (int)currentUser.CurentTopVotersPage);
                        await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("See the best voters") + ".", replyMarkup: keyboards.MenuKeyboardSelectTopVoter);
                        await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("See the best voters"), replyMarkup: keyboards.MenuKeyboardSelectTopVoter);
                        return;
                    }
                    // Register a new voter or update old //
                    if (currentUser.CallBackData == "register")
                    {
                        currentUser.Registration = true;
                        await botClient.EditMessageTextAsync(cq.Message.Chat.Id, cq.Message.MessageId, $"{ResManager.GetString("Enter your name, surname and phone according to the model:")} Ivan - Ivanov - 79800000");
                        return;
                    }
                    // Show rules page //
                    if (currentUser.CallBackData == "rules")
                    {
                        var menuKeyboardRules = new InlineKeyboardMarkup(new[]
                        {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"),"mainMenu")
                    }
                            });
                        await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("RulesText"), replyMarkup: menuKeyboardRules);
                        return;
                    }
                    // Show description page //
                    if (currentUser.CallBackData == "description")
                    {
                        var menuKeyboardDescription = new InlineKeyboardMarkup(new[]
                        {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"),"mainMenu")
                    }
                            });
                        await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("DescriptionText"), replyMarkup: menuKeyboardDescription);
                        return;
                    }
                    // Show prognosedHistory page //
                    if (currentUser.CallBackData == "prognoseHistory")
                    {
                        currentUser.CurentHistoryPage = 0;
                        await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.ChangeSelectHistoryPage(currentUser.UserID, currentUser.CurentHistoryPage), replyMarkup: keyboards.MenuKeyboardHistory);
                        return;
                    }
                    // Show Settings menu //
                    if (currentUser.CallBackData == "settings")
                    {
                        var menuKeyboardSetting = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData(ResManager.GetString("Select Language"),"selectLanguage")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"),"mainMenu")
                            }
                            });
                        await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Settings"), replyMarkup: menuKeyboardSetting);
                        return;
                    }
                    // Show available languages from settings menu //
                    if (currentUser.CallBackData == "selectLanguage")
                    {
                        var menuKeyboardSelectLanguage = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData(ResManager.GetString("Romanian"),"language-ro"),
                                InlineKeyboardButton.WithCallbackData(ResManager.GetString("Russian"),"language-ru"),
                            InlineKeyboardButton.WithCallbackData(ResManager.GetString("English"),"language-en")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData(ResManager.GetString("Back"),"backToSettingsMenu"),
                                InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"),"mainMenu")
                            }
                            });
                        await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Select Language"), replyMarkup: menuKeyboardSelectLanguage);

                        return;
                    }
                    // Start voting //
                    if (currentUser.CallBackData == "playGame")
                    {
                        if (users.UsersList.Exists(existentUser => existentUser.UserID == cq.Message.Chat.Id))
                        {
                            var menuKeyboardPlayGame = new InlineKeyboardMarkup(new[]{
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(ResManager.GetString("Select Match"),"selectWinner")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(ResManager.GetString("Select Final Team"),"selectWinnerTeam")
                    },
                    new[]
                    {
                        //InlineKeyboardButton.WithCallbackData("Back","backToMenu"),
                        InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"),"mainMenu")
                    }
                        });
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("You can select match and get many points or you can try to select who was win in final"), replyMarkup: menuKeyboardPlayGame);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(currentUser.UserID, $"{ResManager.GetString("Register please on")} /register");
                        }
                        return;
                    }
                    // Select match menu, Display match List //
                    if (currentUser.CallBackData == "selectWinner")
                    {
                        currentUser.CurentMatchPage = 0;
                        Controller.ChangeSelectWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);

                        //await botClient.EditMessageTextAsync(currentUser.userID, cq.Message.MessageId, "Select Winne", replyMarkup: menuKeyboardSelectWinner);
                        await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register prognose for this matches"), replyMarkup: keyboards.MenuKeyboardSelectWinner);

                        return;
                    }
                    // Display vote options in Tis match //
                    if (currentUser.CallBackData.Contains("MatchID"))
                    {
                        if (cq.Data.Substring(7) != "" && cq.Data.Substring(7) != null)
                        {
                            currentUser.MatchID = Convert.ToInt32(cq.Data.Substring(7));
                            Controller.ChangeSelectMatchPage(currentUser.UserID, (int)currentUser.MatchID);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.SelectPrognoseFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: keyboards.MenuKeyboardSelectMatch);
                        }
                        else
                        {
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Something went wrong"), replyMarkup: keyboards.MenuKeyboardSelectMatch);
                            await Task.Delay(500);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.SelectPrognoseFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: keyboards.MenuKeyboardSelectMatch);
                        }

                        return;
                    }
                    // Display list of Teams when anyone can select them just 1 //
                    if (currentUser.CallBackData == "selectWinnerTeam")
                    {
                        currentUser.CurentTeamPage = 0;
                        Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                        Controller.GetVoters(currentUser.UserID);
                        if (currentUser.VotedFinalTeam != null || currentUser.VotedFinalTeam != "") await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register the final winner team") + ":\n" + ResManager.GetString("You have select") + ": " + currentUser.VotedFinalTeam, replyMarkup: keyboards.MenuKeyboardSelectFinalWinnerTeam);
                        else await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register the final winner team"), replyMarkup: keyboards.MenuKeyboardSelectFinalWinnerTeam);
                        return;
                    }
                    // Return to start voting page //
                    if (currentUser.CallBackData == "backToPlayMenu")
                    {
                        var menuKeyboardPlayGame = new InlineKeyboardMarkup(new[]
                        {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(ResManager.GetString("Select Match"),"selectWinner")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(ResManager.GetString("Select Final Team"),"selectWinnerTeam")
                        },
                        new[]
                        {
                            //InlineKeyboardButton.WithCallbackData("Back","backToMenu"),
                            InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"),"mainMenu")
                        }
                            });
                        await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("You can select match and get many points or you can try to select who was win in final"), replyMarkup: menuKeyboardPlayGame);
                        return;
                    }
                    // return to settings menu //
                    if (currentUser.CallBackData == "backToSettingsMenu")
                    {
                        var menuKeyboardSetting = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData(ResManager.GetString("Select Language"),"selectLanguage")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"),"mainMenu")
                            }
                            });
                        await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Settings"), replyMarkup: menuKeyboardSetting);
                        return;
                    }
                    // return to match list menu //
                    if (currentUser.CallBackData == "backToWinnerMenu")
                    {
                        currentUser.CurentMatchPage = 0;
                        Controller.ChangeSelectWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                        await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register prognose for this matches"), replyMarkup: keyboards.MenuKeyboardSelectWinner);
                        return;
                    }
                    // return to Vote menu //
                    if (currentUser.CallBackData == "backToSelectedMatchMenu")
                    {
                        currentUser.CurentMatchPage = 0;
                        if (currentUser.MatchID != null)
                        {
                            Controller.ChangeSelectMatchPage(currentUser.UserID, (int)currentUser.MatchID);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.SelectPrognoseFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: keyboards.MenuKeyboardSelectMatch);
                            return;
                        }
                        await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Something went wrong"), replyMarkup: keyboards.MenuKeyboardSelectMatch);
                        return;
                    }
                    // Select pages from list who have pagination //
                    if (currentUser.CallBackData.Contains("FirstPage") || currentUser.CallBackData.Contains("PreventPage") || currentUser.CallBackData.Contains("NextPage") || currentUser.CallBackData.Contains("LastPage"))
                    {
                        //if (paginationType == 1) // Change page to selectWinnerTeam

                        if (cq.Data == "WinnerTeam FirstPage")
                        {
                            currentUser.CurentTeamPage = 0;
                            Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register the final winner team") + ".", replyMarkup: keyboards.MenuKeyboardSelectFinalWinnerTeam);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register the final winner team"), replyMarkup: keyboards.MenuKeyboardSelectFinalWinnerTeam);
                            return;
                        }
                        if (cq.Data == "WinnerTeam PreventPage")
                        {
                            if (currentUser.CurentTeamPage >= 1)
                            {
                                currentUser.CurentTeamPage -= 1;
                                Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register the final winner team") + ".", replyMarkup: keyboards.MenuKeyboardSelectFinalWinnerTeam);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register the final winner team"), replyMarkup: keyboards.MenuKeyboardSelectFinalWinnerTeam);
                                return;
                            }
                            else
                            {
                                currentUser.CurentTeamPage = pages.totalTeamPages - 1;
                                Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register the final winner team") + ".", replyMarkup: keyboards.MenuKeyboardSelectFinalWinnerTeam);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register the final winner team"), replyMarkup: keyboards.MenuKeyboardSelectFinalWinnerTeam);
                                return;
                            }
                        }
                        if (cq.Data == "WinnerTeam NextPage")
                        {
                            if (currentUser.CurentTeamPage < pages.totalTeamPages - 1)
                            {
                                currentUser.CurentTeamPage += 1;
                                Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register the final winner team") + ".", replyMarkup: keyboards.MenuKeyboardSelectFinalWinnerTeam);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register the final winner team"), replyMarkup: keyboards.MenuKeyboardSelectFinalWinnerTeam);
                                return;
                            }
                            else
                            {
                                currentUser.CurentTeamPage = 0;
                                Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register the final winner team") + ".", replyMarkup: keyboards.MenuKeyboardSelectFinalWinnerTeam);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register the final winner team"), replyMarkup: keyboards.MenuKeyboardSelectFinalWinnerTeam);
                                return;
                            }
                        }
                        if (cq.Data == "WinnerTeam LastPage")
                        {
                            currentUser.CurentTeamPage = pages.totalTeamPages - 1;
                            Controller.ChangeSelectWinnerTeamPage(currentUser.UserID, (int)currentUser.CurentTeamPage);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register the final winner team") + ".", replyMarkup: keyboards.MenuKeyboardSelectFinalWinnerTeam);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register the final winner team"), replyMarkup: keyboards.MenuKeyboardSelectFinalWinnerTeam);
                            return;
                        }

                        //if (paginationType == 2) // Change page to select Winner From Match

                        if (cq.Data == "Winner FirstPage")
                        {
                            currentUser.CurentMatchPage = 0;
                            Controller.ChangeSelectWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register prognose for this matches") + ".", replyMarkup: keyboards.MenuKeyboardSelectWinner);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register prognose for this matches"), replyMarkup: keyboards.MenuKeyboardSelectWinner);
                            return;
                        }
                        if (cq.Data == "Winner PreventPage")
                        {
                            if (currentUser.CurentMatchPage >= 1)
                            {
                                currentUser.CurentMatchPage -= 1;
                                Controller.ChangeSelectWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register prognose for this matches") + ".", replyMarkup: keyboards.MenuKeyboardSelectWinner);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register prognose for this matches"), replyMarkup: keyboards.MenuKeyboardSelectWinner);
                                return;
                            }
                            else
                            {
                                currentUser.CurentMatchPage = pages.totalMatchPages - 1;
                                Controller.ChangeSelectWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register prognose for this matches") + ".", replyMarkup: keyboards.MenuKeyboardSelectWinner);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register prognose for this matches"), replyMarkup: keyboards.MenuKeyboardSelectWinner);
                                return;
                            }
                        }
                        if (cq.Data == "Winner NextPage")
                        {
                            if (currentUser.CurentMatchPage < pages.totalMatchPages - 1)
                            {
                                currentUser.CurentMatchPage += 1;
                                Controller.ChangeSelectWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register prognose for this matches") + ".", replyMarkup: keyboards.MenuKeyboardSelectWinner);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register prognose for this matches"), replyMarkup: keyboards.MenuKeyboardSelectWinner);
                                return;
                            }
                            else
                            {
                                currentUser.CurentMatchPage = 0;
                                Controller.ChangeSelectWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register prognose for this matches") + ".", replyMarkup: keyboards.MenuKeyboardSelectWinner);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register prognose for this matches"), replyMarkup: keyboards.MenuKeyboardSelectWinner);
                                return;
                            }
                        }
                        if (cq.Data == "Winner LastPage")
                        {
                            currentUser.CurentMatchPage = pages.totalMatchPages - 1;
                            Controller.ChangeSelectWinnerPage(currentUser.UserID, (int)currentUser.CurentMatchPage);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register prognose for this matches") + ".", replyMarkup: keyboards.MenuKeyboardSelectWinner);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Register prognose for this matches"), replyMarkup: keyboards.MenuKeyboardSelectWinner);
                            return;
                        }

                        //if (paginationType == 3) // Change page to select players

                        if (cq.Data == "Players FirstPage")
                        {
                            currentUser.CurentPlayersPage = 0;
                            Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, (int)currentUser.CurentPlayersPage);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{ResManager.GetString("Select player from team")} {currentUser.VotedPlayerTeam}.", replyMarkup: keyboards.MenuKeyboardSelectPlayer);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{ResManager.GetString("Select player from team")} {currentUser.VotedPlayerTeam}", replyMarkup: keyboards.MenuKeyboardSelectPlayer);
                            return;
                        }
                        if (cq.Data == "Players PreventPage")
                        {
                            if (currentUser.CurentPlayersPage >= 1)
                            {
                                currentUser.CurentPlayersPage -= 1;
                                Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, (int)currentUser.CurentPlayersPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{ResManager.GetString("Select player from team")} {currentUser.VotedPlayerTeam}.", replyMarkup: keyboards.MenuKeyboardSelectPlayer);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{ResManager.GetString("Select player from team")} {currentUser.VotedPlayerTeam}", replyMarkup: keyboards.MenuKeyboardSelectPlayer);
                                return;
                            }
                            else
                            {
                                currentUser.CurentPlayersPage = pages.totalPlayersPages - 1;
                                Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, (int)currentUser.CurentPlayersPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{ResManager.GetString("Select player from team")} {currentUser.VotedPlayerTeam}.", replyMarkup: keyboards.MenuKeyboardSelectPlayer);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{ResManager.GetString("Select player from team")} {currentUser.VotedPlayerTeam}", replyMarkup: keyboards.MenuKeyboardSelectPlayer);
                                return;
                            }
                        }
                        if (cq.Data == "Players NextPage")
                        {
                            if (currentUser.CurentPlayersPage < pages.totalPlayersPages - 1)
                            {
                                currentUser.CurentPlayersPage += 1;
                                Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, (int)currentUser.CurentPlayersPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{ResManager.GetString("Select player from team")} {currentUser.VotedPlayerTeam}.", replyMarkup: keyboards.MenuKeyboardSelectPlayer);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{ResManager.GetString("Select player from team")} {currentUser.VotedPlayerTeam}", replyMarkup: keyboards.MenuKeyboardSelectPlayer);
                                return;
                            }
                            else
                            {
                                currentUser.CurentPlayersPage = 0;
                                Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, (int)currentUser.CurentPlayersPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{ResManager.GetString("Select player from team")} {currentUser.VotedPlayerTeam}.", replyMarkup: keyboards.MenuKeyboardSelectPlayer);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{ResManager.GetString("Select player from team")} {currentUser.VotedPlayerTeam}", replyMarkup: keyboards.MenuKeyboardSelectPlayer);
                                return;
                            }
                        }
                        if (cq.Data == "Players LastPage")
                        {
                            currentUser.CurentPlayersPage = pages.totalPlayersPages - 1;
                            Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, (int)currentUser.CurentPlayersPage);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{ResManager.GetString("Select player from team")} {currentUser.VotedPlayerTeam}.", replyMarkup: keyboards.MenuKeyboardSelectPlayer);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{ResManager.GetString("Select player from team")} {currentUser.VotedPlayerTeam}", replyMarkup: keyboards.MenuKeyboardSelectPlayer);
                            return;
                        }

                        //if (paginationType == 4) // Change page to select topVoters

                        if (cq.Data == "TopVoters FirstPage")
                        {
                            currentUser.CurentTopVotersPage = 0;
                            Controller.ChangeSelectTopVotersPage(currentUser.UserID, (int)currentUser.CurentTopVotersPage);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("See the best voters") + ".", replyMarkup: keyboards.MenuKeyboardSelectTopVoter);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("See the best voters"), replyMarkup: keyboards.MenuKeyboardSelectTopVoter);
                            return;
                        }
                        if (cq.Data == "TopVoters PreventPage")
                        {
                            if (currentUser.CurentTopVotersPage >= 1)
                            {
                                currentUser.CurentTopVotersPage -= 1;
                                Controller.ChangeSelectTopVotersPage(currentUser.UserID, (int)currentUser.CurentTopVotersPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("See the best voters") + ".", replyMarkup: keyboards.MenuKeyboardSelectTopVoter);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("See the best voters"), replyMarkup: keyboards.MenuKeyboardSelectTopVoter);
                                return;
                            }
                            else
                            {
                                currentUser.CurentTopVotersPage = pages.totalTopVotersPages - 1;
                                Controller.ChangeSelectTopVotersPage(currentUser.UserID, (int)currentUser.CurentTopVotersPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("See the best voters") + ".", replyMarkup: keyboards.MenuKeyboardSelectTopVoter);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("See the best voters"), replyMarkup: keyboards.MenuKeyboardSelectTopVoter);
                                return;
                            }
                        }
                        if (cq.Data == "TopVoters NextPage")
                        {
                            if (currentUser.CurentTopVotersPage < pages.totalTopVotersPages - 1)
                            {
                                currentUser.CurentTopVotersPage += 1;
                                Controller.ChangeSelectTopVotersPage(currentUser.UserID, (int)currentUser.CurentTopVotersPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("See the best voters") + ".", replyMarkup: keyboards.MenuKeyboardSelectTopVoter);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("See the best voters"), replyMarkup: keyboards.MenuKeyboardSelectTopVoter);
                                return;
                            }
                            else
                            {
                                currentUser.CurentTopVotersPage = 0;
                                Controller.ChangeSelectTopVotersPage(currentUser.UserID, (int)currentUser.CurentTopVotersPage);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("See the best voters") + ".", replyMarkup: keyboards.MenuKeyboardSelectTopVoter);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("See the best voters"), replyMarkup: keyboards.MenuKeyboardSelectTopVoter);
                                return;
                            }
                        }
                        if (cq.Data == "TopVoters LastPage")
                        {
                            currentUser.CurentTopVotersPage = pages.totalTopVotersPages - 1;
                            Controller.ChangeSelectTopVotersPage(currentUser.UserID, (int)currentUser.CurentTopVotersPage);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("See the best voters") + ".", replyMarkup: keyboards.MenuKeyboardSelectTopVoter);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("See the best voters"), replyMarkup: keyboards.MenuKeyboardSelectTopVoter);
                            return;
                        }

                        //if (paginationType == 5) // Change page to select history

                        if (cq.Data == "History FirstPage")
                        {
                            currentUser.CurentHistoryPage = 0;
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.ChangeSelectHistoryPage(currentUser.UserID, (int)currentUser.CurentHistoryPage) + ".", replyMarkup: keyboards.MenuKeyboardHistory);
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.ChangeSelectHistoryPage(currentUser.UserID, (int)currentUser.CurentHistoryPage), replyMarkup: keyboards.MenuKeyboardHistory);
                        }
                        if (cq.Data == "History PreventPage")
                        {
                            if (currentUser.CurentHistoryPage >= 1)
                            {
                                currentUser.CurentHistoryPage -= 1;
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.ChangeSelectHistoryPage(currentUser.UserID, (int)currentUser.CurentHistoryPage), replyMarkup: keyboards.MenuKeyboardHistory);
                                return;
                            }
                            else
                            {
                                if (currentUser.CurentHistoryPage == pages.totalHistoryPages - 1) await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.ChangeSelectHistoryPage(currentUser.UserID, (int)currentUser.CurentHistoryPage) + ".", replyMarkup: keyboards.MenuKeyboardHistory);
                                currentUser.CurentHistoryPage = pages.totalHistoryPages - 1;
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.ChangeSelectHistoryPage(currentUser.UserID, (int)currentUser.CurentHistoryPage), replyMarkup: keyboards.MenuKeyboardHistory);
                                return;
                            }
                        }
                        if (cq.Data == "History NextPage")
                        {
                            if (currentUser.CurentHistoryPage < pages.totalHistoryPages - 1)
                            {
                                currentUser.CurentHistoryPage += 1;
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.ChangeSelectHistoryPage(currentUser.UserID, (int)currentUser.CurentHistoryPage), replyMarkup: keyboards.MenuKeyboardHistory);
                                return;
                            }
                            else
                            {
                                if (currentUser.CurentHistoryPage == 0) await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{Controller.ChangeSelectHistoryPage(currentUser.UserID, (int)currentUser.CurentHistoryPage)}.", replyMarkup: keyboards.MenuKeyboardHistory);
                                currentUser.CurentHistoryPage = 0;
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.ChangeSelectHistoryPage(currentUser.UserID, (int)currentUser.CurentHistoryPage), replyMarkup: keyboards.MenuKeyboardHistory);
                                return;
                            }
                        }
                        if (cq.Data == "History LastPage")
                        {
                            if (pages.totalHistoryPages != 0)
                            {
                                currentUser.CurentHistoryPage = pages.totalHistoryPages - 1;
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.ChangeSelectHistoryPage(currentUser.UserID, (int)currentUser.CurentHistoryPage) + ".", replyMarkup: keyboards.MenuKeyboardHistory);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.ChangeSelectHistoryPage(currentUser.UserID, (int)currentUser.CurentHistoryPage), replyMarkup: keyboards.MenuKeyboardHistory);
                                return;
                            }
                            else
                            {
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("There is no such page"), replyMarkup: keyboards.MenuKeyboardHistory);
                                return;
                            }
                        }
                    }
                    // Select vote for final team //
                    if (currentUser.CallBackData.Contains("TeamID"))
                    {
                        if (users.UsersList.Exists(existentUser => existentUser.UserID == cq.Message.Chat.Id))
                        {
                            int teamID = Convert.ToInt32(cq.Data.Substring(6));
                            Controller.VoteFinalTeam(currentUser.UserID, teamID);
                            Controller.VoteFromMatch(Convert.ToInt32(currentUser.UserID), null, 6, null, null, null, teamID);
                            Controller.GetVoters(currentUser.UserID);
                            var menuKeyboardPlayGame = new InlineKeyboardMarkup(new[]{ new[]
                                {
                                    InlineKeyboardButton.WithCallbackData(ResManager.GetString("Back"), "selectWinnerTeam"),
                                    InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"),"mainMenu")
                                }
                            });
                            await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("You have voted successfully") + ": " + currentUser.VotedFinalTeam, replyMarkup: menuKeyboardPlayGame);
                            return;
                        }
                        await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, "Please Register on /register");
                        return;
                    }
                    // Menu with vote options //
                    if (currentUser.CallBackData.Contains("Vote"))
                    {
                        if (users.UsersList.Exists(existentUser => existentUser.UserID == cq.Message.Chat.Id))
                        {
                            var menuKeyboardVote = new InlineKeyboardMarkup(new[] {
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData(ResManager.GetString("Back"), "backToSelectedMatchMenu"),
                                    InlineKeyboardButton.WithCallbackData(ResManager.GetString("Main Menu"),"mainMenu")
                                }
                            }
                            );
                            if (cq.Data.Contains("VoteFirstTeamName"))
                            {
                                currentUser.MatchID = Convert.ToInt32(currentUser.CallBackData.Substring(19));
                                Controller.VoteFromMatch(Convert.ToInt64(currentUser.UserID), currentUser.MatchID, 1, null, null, null, null);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.SelectPrognoseFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: keyboards.MenuKeyboardSelectMatch);

                                return;
                            }
                            if (cq.Data.Contains("VoteSecondTeamName"))
                            {
                                currentUser.MatchID = Convert.ToInt32(currentUser.CallBackData.Substring(20));
                                Controller.VoteFromMatch(Convert.ToInt64(currentUser.UserID), currentUser.MatchID, 2, null, null, null, null);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.SelectPrognoseFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: keyboards.MenuKeyboardSelectMatch);
                                return;
                            }
                            if (cq.Data.Contains("VoteEqual"))
                            {
                                currentUser.MatchID = Convert.ToInt32(currentUser.CallBackData.Substring(11));
                                Controller.VoteFromMatch(Convert.ToInt64(currentUser.UserID), currentUser.MatchID, 3, null, null, null, null);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.SelectPrognoseFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: keyboards.MenuKeyboardSelectMatch);
                                return;
                            }
                            if (cq.Data.Contains("VoteTotal"))
                            {
                                currentUser.MatchID = Convert.ToInt32(currentUser.CallBackData.Substring(11));
                                currentUser.IntroduceTotal = true;
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{ResManager.GetString("For teams")} {currentUser.VotedFirstTeam} {ResManager.GetString("and")} {currentUser.VotedSecondTeam} \n{ResManager.GetString("Introduce final score by model:")} 4/3", replyMarkup: menuKeyboardVote);
                                return;
                            }
                            if (cq.Data.Contains("VotePlayers"))
                            {
                                currentUser.VotedPlayerTeam = currentUser.CallBackData.Split('+')[1];
                                try
                                {
                                    currentUser.CurentPlayersPage = 0;
                                    currentUser.MatchID = Convert.ToInt32(currentUser.CallBackData.Split('+')[2]);
                                    Controller.ChangeSelectPlayersPage(currentUser.UserID, currentUser.VotedPlayerTeam, currentUser.CurentPlayersPage);
                                    await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, $"{ResManager.GetString("Select player from team")} {currentUser.VotedPlayerTeam}", replyMarkup: keyboards.MenuKeyboardSelectPlayer);
                                }
                                catch (Exception exception)
                                {
                                    WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                                    WebResponse response = request.GetResponse();
                                    logger.Error(exception);
                                }

                                return;
                            }
                            if (cq.Data.Contains("VotePlayerID"))
                            {
                                int PlayerID = Convert.ToInt32(currentUser.CallBackData.Substring(12));
                                Controller.VoteFromMatch(Convert.ToInt32(currentUser.UserID), currentUser.MatchID, 5, null, null, PlayerID, null);
                                await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, Controller.SelectPrognoseFromMatch(currentUser.UserID, (int)currentUser.MatchID), replyMarkup: keyboards.MenuKeyboardSelectMatch);
                                return;
                            }
                            return;
                        }
                        await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, "Please Register on /register");
                        return;
                    }
                    // Insert in Voter table his selected language if is changed //
                    if (currentUser.CallBackData.Contains("language"))
                    {
                        currentUser.Language = cq.Data.Substring(9);
                        if (currentUser.Language == "en")
                        {
                            Controller.SetNewVoter(null, null, null, "en", currentUser.UserID);
                            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("en");
                        }
                        if (currentUser.Language == "ro")
                        {
                            Controller.SetNewVoter(null, null, null, "ro", currentUser.UserID);
                            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ro");
                        }
                        if (currentUser.Language == "ru")
                        {
                            Controller.SetNewVoter(null, null, null, "ru", currentUser.UserID);
                            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ru");
                        }
                        var menuKeyboard = new InlineKeyboardMarkup(new[]{
                            new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("Start Prognose"),"playGame")},
                            new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("Top Voters"),"topVoters")},
                            new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("History"),"prognoseHistory")},
                            new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("Description"), "description"), InlineKeyboardButton.WithCallbackData(ResManager.GetString("Rules"),"rules")},
                            new[] { InlineKeyboardButton.WithCallbackData(ResManager.GetString("Settings"), "settings")}});
                        await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Main Menu") + ".", replyMarkup: menuKeyboard);
                        await botClient.EditMessageTextAsync(currentUser.UserID, cq.Message.MessageId, ResManager.GetString("Main Menu"), replyMarkup: menuKeyboard);
                        return;
                    }
                    return;
                }
                // if users.UsersList is not in database with name and surname display message try register
                // {Optional block if is change something in database and participants have a menu and can press any buttons}
                else
                {
                    try
                    {
                        Controller.SetNewVoter(null, null, null, "en", Convert.ToInt64(cq.Message.Chat.Id));
                        // creating a users.UsersList object for work with them
                        Users currentUser = users.UsersList.Where(existentUser => existentUser.UserID == cq.Message.Chat.Id).First();
                        // Insert in new Voter table language for display text
                        if (currentUser.CallBackData.Contains("language"))
                        {
                            currentUser.Language = cq.Data.Substring(9);
                            if (currentUser.Language == "en")
                            {
                                Controller.SetNewVoter(null, null, null, "en", currentUser.UserID);
                                Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("en");
                            }
                            if (currentUser.Language == "ro")
                            {
                                Controller.SetNewVoter(null, null, null, "ro", currentUser.UserID);
                                Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ro");
                            }
                            if (currentUser.Language == "ru")
                            {
                                Controller.SetNewVoter(null, null, null, "ru", currentUser.UserID);
                                Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ru");
                            }

                            await botClient.DeleteMessageAsync(currentUser.UserID, (int)currentUser.MessageID);
                            await botClient.SendTextMessageAsync(currentUser.UserID, $"{ResManager.GetString("Enter your name, surname and phone according to the model:")} Ivan - Ivanov - 79800000");
                            currentUser.Registration = true;
                            return;
                        }
                        else // if is not registred but have a language next step is register
                        {
                            await botClient.EditMessageTextAsync(cq.Message.Chat.Id, cq.Message.MessageId, $"{ResManager.GetString("Try command")} /register");
                            return;
                        }
                    }
                    catch (Exception exception)
                    {
                        WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                        WebResponse response = request.GetResponse();
                        logger.Warn(exception.Message);
                    }
                }
            }
            catch (Exception exception)
            {
                WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Exception: {exception.Message}");
                WebResponse response = request.GetResponse();
                logger.Error(exception.Message);
                return;
            }
            return;
        }

        private Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cts)
        {
            var errorMessage = exception switch
            {
                // catch an api error, Bigger exception is { <- 400 - the message text is not changed -> }
                ApiRequestException apiRequestException
                    => $"Telegram api error: \n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(exception);
            WebRequest request = WebRequest.Create($"https://api.telegram.org/bot5725547596:AAEBmtojOo-G3QrhkBEMc73MSCE3hGsrjT4/sendMessage?chat_id=840323224&text=Error: {exception.Message}");
            WebResponse response = request.GetResponse();
            logger.Error(errorMessage);
            return Task.CompletedTask;
        }

        
    }
}