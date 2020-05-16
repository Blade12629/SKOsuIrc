using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SKOsuIrc.Irc;
using SKOsuIrc.Args;

namespace SKOsuIrc
{
    /// <summary>
    /// Task based irc client, WriteAsync() is threadsafe
    /// </summary>
    public sealed class OsuIrcClient : IrcClient
    {
        public event EventHandler<string> OnRawDataRecieved;
        public event EventHandler<OsuIrcMessageArgs> OnIrcMessageRecieved;

        public event EventHandler<string> OnWelcomeMessageRecieved;
        public event EventHandler<OsuIrcChannelTopicArg> OnChannelTopicRecieved;

        public event EventHandler<OsuIrcPrivateMessageArg> OnPrivateMessageRecieved;
        public event EventHandler<OsuIrcChannelMessageArg> OnChannelMessageRecieved;

        public event EventHandler<OsuIrcPrivateMessageSendArg> OnBeforePrivateMessageSent;
        public event EventHandler<OsuIrcPrivateMessageSendArg> OnAfterPrivateMessageSent;

        public event EventHandler<OsuIrcChannelMessageSendArg> OnBeforeChannelMessageSent;
        public event EventHandler<OsuIrcChannelMessageSendArg> OnAfterChannelMessageSent;

        public event EventHandler<OsuIrcUserListArg> OnUserListRecieved;
        public event EventHandler OnUserListRecieveEnd;

        public event EventHandler<OsuIrcMotdArg> OnMotdRecieved;

        public event EventHandler<OsuIrcUserPartArg> OnUserPartRecieved;
        public event EventHandler<OsuIrcUserJoinArg> OnUserJoinRecieved;
        public event EventHandler<OsuIrcUserQuitArg> OnUserQuitRecieved;

        public event EventHandler<OsuIrcJoinSendArg> OnAfterJoinSent;
        public event EventHandler<OsuIrcJoinSendArg> OnBeforeJoinSent;

        public event EventHandler<OsuIrcPartSendArg> OnAfterPartSent;
        public event EventHandler<OsuIrcPartSendArg> OnBeforePartSent;

        public event EventHandler OnBeforeQuitSent;
        public event EventHandler OnAfterQuitSent;

        public event EventHandler<OsuIrcUserStatsArg> OnUserStatsRecieved;
        public event EventHandler OnUserRequestedNotFound;

        public event EventHandler<OsuIrcUserRollArg> OnUserRollRecieved;
        public event EventHandler<OsuIrcUserWhereArg> OnUserWhereRecieved;

        public IReadOnlyDictionary<string, OsuIrcChannel> Channels { get { return new ReadOnlyDictionary<string, OsuIrcChannel>(_channels); } }

        private event EventHandler<OsuIrcMessageArgs> OnPingRecieved;

#pragma warning disable IDE0044 // Add readonly modifier
        private List<string> _motd;
        private UserStatsInfo _userStats;

        private Dictionary<string, OsuIrcChannel> _channels;
        private object _channelLock = new object();
#pragma warning restore IDE0044 // Add readonly modifier

#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable CA1304 // Specify CultureInfo
        public OsuIrcClient(string nick, string pass, string host = "irc.ppy.sh", int port = 6667, bool ipv6 = false) : base(host, port, nick.ToLower(), pass, ipv6)
#pragma warning restore CA1304 // Specify CultureInfo
#pragma warning restore CA1062 // Validate arguments of public methods
        {
            _motd = new List<string>();
            _userStats = default;
            OnMessageRecieved += new EventHandler<string>(HandleIrcMessage);
            OnPingRecieved += new EventHandler<OsuIrcMessageArgs>(HandlePing);
            _channels = new Dictionary<string, OsuIrcChannel>();
        }

        private void HandleIrcMessage(object sender, string message)
        {
            try
            {
                OnRawDataRecieved?.Invoke(this, message);

                string msg = message.TrimStart(':').Replace("!", "");
                string ircSender = null;
                string ircFrom = null;
                string ircDestination = null;
                string ircChannel = null;
                string ircCommand = null;
                string ircParameters = null;

                string[] split;
                string firstPart = null;
                if ((firstPart = GetSubString(ref msg, "!")) != null)
                {
                    int index = firstPart.IndexOf(' ');

                    if (index > 0)
                    {
                        if (firstPart[index + 1] == '3' &&
                            firstPart[index + 2] == '3' &&
                            firstPart[index + 3] == '3')
                        {
                            split = firstPart.Split(' ');

                            ircSender = split[0];
                            ircCommand = split[1];
                            ircDestination = split[2];
                            ircChannel = split[3];
                            ircParameters = split[4] + "!" + msg;

                            goto createArgs;
                        }
                    }

                    ircSender = firstPart;

                    /*
                        message: ":cho.ppy.sh PONG BanchoBot!cho@cho.ppy.sh"
                                  :<sender> <command> <parameters>
                     */

                    string oldFirstPart = firstPart;
                    if ((firstPart = GetSubString(ref msg, ":")) != null)
                    {
                        ircParameters = msg;
                        split = firstPart.Split(' ');

                        if (split.Length >= 2)
                        {
                            ircFrom = split[0];
                            ircCommand = split[1];

                            if (split.Length >= 3)
                                ircChannel = split[2];
                        }
                        else
                            throw new NotSupportedException($"Unsupported split length {split.Length}");
                    }
                    else if (!msg.Contains(':') && !msg.Contains('+') && !oldFirstPart.Contains('+') && !oldFirstPart.Contains(':'))
                    {
                        split = oldFirstPart.Split(' ');

                        ircSender = split[0];
                        ircCommand = split[1];
                        ircParameters = split[2] + '!' + msg;
                    }
                    else
                    {
                        split = message.TrimStart(':').Split(' ');

                        ircFrom = split[0];
                        ircCommand = split[1];
                        ircChannel = split[2];
                        ircDestination = split[3];
                        ircParameters = split[4];
                    }
                }
                else
                {
                    if ((firstPart = GetSubString(ref msg, "=")) != null)
                    {
                        split = firstPart.Split(' ');

                        ircSender = split[0];
                        ircCommand = split[1];
                        ircDestination = split[2];

                        split = msg.Split(':');

                        ircChannel = split[0];
                        ircParameters = split[1];
                    }
                    else if ((firstPart = GetSubString(ref msg, ":")) != null)
                    {
                        split = firstPart.Split(' ');
                        ircParameters = msg;

                        ircSender = split[0];
                        ircCommand = split[1];

                        if (split.Length >= 3)
                        {
                            ircDestination = split[2];

                            if (split.Length >= 4)
                                ircChannel = split[3];
                        }

                    }
                    else
                    {
                        split = msg.Split(' ');

                        if (split[0].Equals("ping", StringComparison.CurrentCultureIgnoreCase))
                        {
                            ircCommand = split[0];
                            ircSender = split[1];
                        }
                        else
                        {
                            ircSender = split[0];
                            ircCommand = split[1];
                        }

                        if (split.Length > 3)
                        {
                            ircDestination = split[2];
                            if (split.Length > 4)
                            {
                                ircChannel = split[3];
                                if (split.Length > 5)
                                    ircParameters = split[4] + " " + split[5];
                            }
                        }
                    }
                }

                createArgs:
                {
                    OsuIrcMessageArgs args = new OsuIrcMessageArgs(ircSender, ircFrom, ircDestination,
                                                                   ircChannel, ircCommand, ircParameters);
                    TriggerEvents(args);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                Console.WriteLine("—————————————");
                Console.WriteLine(ex);
                Console.WriteLine(message);
                Console.WriteLine("—————————————");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }
        }

        private void HandlePrivateMessage(OsuIrcMessageArgs args)
        {
            if (args.Sender == null || !args.Sender.Equals("BanchoBot", StringComparison.CurrentCultureIgnoreCase))
            {
                OnPrivateMessageRecieved?.Invoke(this, new OsuIrcPrivateMessageArg(args.Sender, args.Parameters, this));
                return;
            }

            if (args.Parameters.Equals("User not found", StringComparison.CurrentCultureIgnoreCase))
            {
                OnUserRequestedNotFound?.Invoke(this, new EventArgs());
                return;
            }

            bool triggerPrivMsgEvent = true;

            HandleBanchoMessage(args, ref triggerPrivMsgEvent);

            if (triggerPrivMsgEvent)
                OnPrivateMessageRecieved?.Invoke(this, new OsuIrcPrivateMessageArg(args.Sender, args.Parameters, this));
        }

        private void HandleBanchoMessage(OsuIrcMessageArgs args, ref bool triggerPrivMsgEvent)
        {
            //Handles: !stats <user>, !roll <maxValue>, !where <user>
            //[https://google.de test]
            if (_userStats.Equals(default))
            {
#pragma warning disable CA1307 // Specify StringComparison
                if (args.Parameters.StartsWith("Stats for"))
#pragma warning restore CA1307 // Specify StringComparison
                {
                    //Stats for (Skyfly)[https://osu.ppy.sh/u/5790241] is Afk:
                    int start = args.Parameters.IndexOf('(');
                    int end = args.Parameters.IndexOf(')');

                    string user = args.Parameters.Substring(start + 1, end - start - 1);

                    end = args.Parameters.IndexOf(']');
                    string status = args.Parameters.Remove(0, end + 5).TrimEnd(':');

                    _userStats = new UserStatsInfo(user, 0, 0, 0, 0, 0, status);
                    triggerPrivMsgEvent = false;
                }
            }
            else
            {
                string[] split = args.Parameters.Split(':');
                string split1Str = split[1].TrimStart(' ').TrimEnd(' ');
                string split1StrFirstPart = GetSubString(ref split1Str, " ");

#pragma warning disable CA1304 // Specify CultureInfo
                switch (split[0].ToLower())
#pragma warning restore CA1304 // Specify CultureInfo
                {
                    default:
                        return;

                    case "score":
                        string scoreStr = split1StrFirstPart.Replace(",", "");
#pragma warning disable CA1305 // Specify IFormatProvider
                        _userStats.Score = ulong.Parse(scoreStr);
                        _userStats.Rank = ulong.Parse(split1Str.Trim('(', ')', '#'));

                        triggerPrivMsgEvent = false;
                        break;

                    case "plays":
                        _userStats.PlayCount = long.Parse(split1StrFirstPart);
                        _userStats.Level = int.Parse(split1Str.Trim('(', ')', 'l', 'v'));

                        triggerPrivMsgEvent = false;
                        break;

                    case "accuracy":
                        _userStats.Accuracy = double.Parse(split1Str.Trim('%')) / 100.0;
#pragma warning restore CA1305 // Specify IFormatProvider

                        OsuIrcUserStatsArg statsArg = new OsuIrcUserStatsArg(_userStats.User, _userStats.Score, _userStats.Rank,
                                                                             _userStats.PlayCount, _userStats.Level, _userStats.Accuracy,
                                                                             _userStats.Status, this);

                        _userStats = default;

                        OnUserStatsRecieved?.Invoke(this, statsArg);

                        triggerPrivMsgEvent = false;
                        break;
                }
            }

            if (args.Parameters.Contains(" rolls "))
            {
                string[] split = args.Parameters.Split(' ');

                string user = split[0];
#pragma warning disable CA1305 // Specify IFormatProvider
                int points = int.Parse(split[2]);
#pragma warning restore CA1305 // Specify IFormatProvider

                OnUserRollRecieved?.Invoke(this, new OsuIrcUserRollArg(user, points, this));

                return;
            }

            if (args.Parameters.Contains(" is in "))
            {
                string[] split = args.Parameters.Split(' ');

                string user = split[0];
                string location = split[3];

                OnUserWhereRecieved?.Invoke(this, new OsuIrcUserWhereArg(user, location, this));
                return;
            }
        }

        private void HandlePing(object sender, OsuIrcMessageArgs args)
        {
            WriteAsync($"PONG {User} {args.Sender}").Wait();
        }

        private void AddChannel(string channel, bool privateChannel)
        {
            lock(_channelLock)
            {
                if (_channels.ContainsKey(channel))
                    return;

                _channels.Add(channel, new OsuIrcChannel(privateChannel, channel));
            }
        }

        private void RemoveChannel(string channel)
        {
            lock(_channelLock)
            {
                if (!_channels.ContainsKey(channel))
                    return;

                _channels.Remove(channel);
            }
        }

        private void TriggerEvents(OsuIrcMessageArgs args)
        {
            OnIrcMessageRecieved?.Invoke(this, args);

            if (int.TryParse(args.Command, out int cmdVal))
            {
                switch(cmdVal)
                {
                    default:
                        break;

                    case 001:
                        OnWelcomeMessageRecieved?.Invoke(this, args.Parameters);
                        break;

                    case 332:
                        OnChannelTopicRecieved?.Invoke(this, new OsuIrcChannelTopicArg(args.Channel, args.Parameters, this));
                        break;

                    //Unkown code, parameters: <user>!<from> <id>
                    case 333:
                        break;

                    case 353:

                        foreach(string user in args.Parameters.Replace("+", "").Split(' '))
                        {
                            if (string.IsNullOrEmpty(user))
                                continue;

                            _channels[args.Channel.Replace(" ", "")].AddUser(new OsuIrcUser(user));
                        }

                        OnUserListRecieved?.Invoke(this, new OsuIrcUserListArg(args.Channel, args.Parameters, this));
                        break;
                    case 366:
                        OnUserListRecieveEnd?.Invoke(this, new EventArgs());
                        break;

                    //Motd
                    case 372:
                        _motd.Add(args.Parameters);
                        break;
                    //Motd start
                    case 375:
                        _motd.Clear();
                        break;
                    //Motd end
                    case 376:
                        OnMotdRecieved?.Invoke(this, new OsuIrcMotdArg(_motd, this));
                        break;
                }
            }
            else
            {
#pragma warning disable CA1304 // Specify CultureInfo
                switch (args.Command.ToUpper())
#pragma warning restore CA1304 // Specify CultureInfo
                {
                    default: 
                        break;

                    case "QUIT":
                        if (args.Sender.Equals(User, StringComparison.CurrentCultureIgnoreCase))
                        {
                            lock (_channelLock)
                            {
                                _channels.Clear();
                            }
                        }

                        OnUserQuitRecieved?.Invoke(this, new OsuIrcUserQuitArg(args.Sender, this));
                        break;

                    case "JOIN":

                        if (args.Sender.Equals(User, StringComparison.CurrentCultureIgnoreCase))
                        {
                            AddChannel(args.Parameters, false);
                        }
                        else
                        {
                            var channel = _channels[args.Parameters];
                            channel.AddUser(new OsuIrcUser(args.Sender));
                        }

                        OnUserJoinRecieved?.Invoke(this, new OsuIrcUserJoinArg(args.Sender, args.Parameters, this));
                        break;

                    case "PART":

                        if (args.Sender.Equals(User, StringComparison.CurrentCultureIgnoreCase))
                        {
                            RemoveChannel(args.Parameters);
                        }
                        else
                        {
                            var channel = _channels[args.Parameters];
                            channel.RemoveUser(args.Sender);
                        }

                        OnUserPartRecieved?.Invoke(this, new OsuIrcUserPartArg(args.Sender, args.Parameters, this));
                        break;

                    case "PING":
                        OnPingRecieved?.Invoke(this, args);
                        break;

                    case "PRIVMSG":
                        {
                            if (args.Channel[0] != '#')
                            {
                                HandlePrivateMessage(args);
                                break;
                            }

                            OnChannelMessageRecieved?.Invoke(this, new OsuIrcChannelMessageArg(args.Sender, args.Channel, args.Parameters, this));
                        }
                        break;
                }
            }
        }

        private string GetSubString(ref string @string, string indexToFind, bool includeIndex = false, bool trimStartEndWhitespace = true)
        {
#pragma warning disable CA1307 // Specify StringComparison
            int index = @string.IndexOf(indexToFind);
#pragma warning restore CA1307 // Specify StringComparison

            if (index == -1)
                return null;

            int count;
            string result;
            if (includeIndex)
            {
                count = index + indexToFind.Length;

                result = @string.Substring(0, count);
                @string = @string.Remove(0, count);
            }
            else
            {
                count = index;

                result = @string.Substring(0, count);
                @string = @string.Remove(0, count + indexToFind.Length);
            }

            if (trimStartEndWhitespace)
                result = result.TrimStart(' ').TrimEnd(' ');

            return result;
        }

        /// <summary>
        /// Sends a private message to a user
        /// </summary>
        /// <param name="user">User_Name</param>
        /// <param name="message">Message</param>
        public void SendPrivateMessage(string user, string message)
        {
            OnBeforePrivateMessageSent?.Invoke(this, new OsuIrcPrivateMessageSendArg(user, message, this));
            WriteAsync($"PRIVMSG {user} :{message}").Wait();
            OnAfterPrivateMessageSent?.Invoke(this, new OsuIrcPrivateMessageSendArg(user, message, this));
        }

        /// <summary>
        /// Sends a message to a channel
        /// </summary>
        /// <param name="channel"># in the beginning of the channel name is optional</param>
        /// <param name="message">Message</param>
        public void SendChannelMessage(string channel, string message)
        {
            if (string.IsNullOrEmpty(channel))
                throw new ArgumentNullException(nameof(channel));
            else if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException(nameof(message));

            if (channel[0] != '#')
                channel = '#' + channel;

            OnBeforeChannelMessageSent?.Invoke(this, new OsuIrcChannelMessageSendArg(channel, message, this));
            WriteAsync($"PRIVMSG {channel} :{message}").Wait();
            OnAfterChannelMessageSent?.Invoke(this, new OsuIrcChannelMessageSendArg(channel, message, this));
        }

        /// <summary>
        /// Joins a channel
        /// </summary>
        /// <param name="channel">Channel name</param>
        public void JoinChannel(string channel)
        {
            OnBeforeJoinSent?.Invoke(this, new OsuIrcJoinSendArg(channel, this));
            WriteAsync($"JOIN {channel}").Wait();
            OnAfterJoinSent?.Invoke(this, new OsuIrcJoinSendArg(channel, this));
        }

        /// <summary>
        /// Parts a channel
        /// </summary>
        /// <param name="channel">Channel name</param>
        public void PartChannel(string channel)
        {
            OnBeforePartSent?.Invoke(this, new OsuIrcPartSendArg(channel, this));
            WriteAsync($"PART {channel}").Wait();
            OnAfterPartSent?.Invoke(this, new OsuIrcPartSendArg(channel, this));
        }

        /// <summary>
        /// Quits from the server but does not disconnect by itself
        /// </summary>
        public void Quit()
        {
            OnBeforeQuitSent?.Invoke(this, new EventArgs());
            WriteAsync($"QUIT :quit").Wait();
            OnAfterQuitSent?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Disposes the client, if connected in quits the client
        /// </summary>
        public new void Dispose()
        {
            base.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            Quit();
            base.Dispose(disposing);
        }

        /// <summary>
        /// Login to the server
        /// </summary>
        public new void Login()
        {
            base.Login();
        }

        /// <summary>
        /// Connect to the server
        /// </summary>
        public new void Connect()
        {
            base.Connect();
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public new void Disconnect()
        {
            base.Disconnect();
        }

        /// <summary>
        /// Reads the next incoming raw message
        /// </summary>
        /// <returns>irc raw message</returns>
        public new async Task<string> ReadAsync()
        {
            return await base.ReadAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Starts to read incoming raw messages
        /// </summary>
        public new void StartReadingAsync()
        {
            base.StartReadingAsync();
        }

        /// <summary>
        /// Stops reading
        /// </summary>
        public new void StopReading()
        {
            base.StopReading();
        }

        /// <summary>
        /// Writes a raw message to the server
        /// </summary>
        /// <param name="line">Raw message</param>
        /// <returns></returns>
        public new async Task WriteAsync(string line)
        {
            await base.WriteAsync(line).ConfigureAwait(false);
        }

        /// <summary>
        /// Requests !stats user
        /// </summary>
        /// <param name="user">User_Name</param>
        public void RequestUserStats(string user)
        {
#pragma warning disable CA1304 // Specify CultureInfo
#pragma warning disable CA1062 // Validate arguments of public methods
            user = user.ToLower();
#pragma warning restore CA1062 // Validate arguments of public methods
#pragma warning restore CA1304 // Specify CultureInfo

            SendBanchoCommand("!stats " + user);
        }

        /// <summary>
        /// Requests !where user
        /// </summary>
        /// <param name="user">User_Name</param>
        public void RequestUserWhere(string user)
        {
            SendBanchoCommand("!where " + user);
        }

        /// <summary>
        /// Requests the FAQ List
        /// </summary>
        public void RequestFAQList()
        {
            SendBanchoCommand("!faq list");
        }

        /// <summary>
        /// Requests a specific FAQ topic
        /// </summary>
        /// <param name="faq">topic</param>
        public void RequestFAQInfo(string faq)
        {
            SendBanchoCommand("!faq " + faq);
        }

        /// <summary>
        /// Requests !roll value
        /// </summary>
        /// <param name="maxValue">max amount, 1 to X</param>
        public void RequestRoll(int maxValue = 100)
        {
            SendBanchoCommand("!roll " + maxValue);
        }

        /// <summary>
        /// Sends a command to BanchoBot
        /// </summary>
        /// <param name="command">command</param>
        public void SendBanchoCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException(nameof(command));

            if (command[0] != '!')
                command = '!' + command;

            SendPrivateMessage("BanchoBot", command);
        }

        private struct UserStatsInfo : IEquatable<UserStatsInfo>
        {
            public string User { get; set; }
            public ulong Score { get; set; }
            public ulong Rank { get; set; }
            public long PlayCount { get; set; }
            public int Level { get; set; }
            public double Accuracy { get; set; }
            public string Status { get; set; }

            public UserStatsInfo(string user, ulong score, ulong rank, long playCount, int level, double accuracy, string status) : this()
            {
                User = user;
                Score = score;
                Rank = rank;
                PlayCount = playCount;
                Level = level;
                Accuracy = accuracy;
                Status = status;
            }

            public override bool Equals(object obj)
            {
                return obj is UserStatsInfo info && Equals(info);
            }

            public bool Equals(UserStatsInfo other)
            {
                return User == other.User &&
                       Score == other.Score &&
                       Rank == other.Rank &&
                       PlayCount == other.PlayCount &&
                       Level == other.Level &&
                       Accuracy == other.Accuracy &&
                       Status == other.Status;
            }

            public override int GetHashCode()
            {
                var hashCode = -252079384;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(User);
                hashCode = hashCode * -1521134295 + Score.GetHashCode();
                hashCode = hashCode * -1521134295 + Rank.GetHashCode();
                hashCode = hashCode * -1521134295 + PlayCount.GetHashCode();
                hashCode = hashCode * -1521134295 + Level.GetHashCode();
                hashCode = hashCode * -1521134295 + Accuracy.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Status);
                return hashCode;
            }

            public static bool operator ==(UserStatsInfo left, UserStatsInfo right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(UserStatsInfo left, UserStatsInfo right)
            {
                return !(left == right);
            }
        }
    }
}
