using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SKOsuIrc.Args
{
    public class OsuIrcEventArg : EventArgs
    {
        public OsuIrcClient Client { get; }

        public OsuIrcEventArg(OsuIrcClient client) : base()
        {
            Client = client;
        }
    }

    public class OsuIrcChannelTopicArg : OsuIrcEventArg
    {
        public string Channel { get; }
        public string Topic { get; }

        public OsuIrcChannelTopicArg(string channel, string topic, OsuIrcClient client) : base(client)
        {
            Channel = channel;
            Topic = topic;
        }
    }

    public class OsuIrcPrivateMessageArg : OsuIrcEventArg
    {
        public string Sender { get; }
        public string Message { get; }

        public OsuIrcPrivateMessageArg(string sender, string message, OsuIrcClient client) : base(client)
        {
            Sender = sender;
            Message = message;
        }
    }

    public class OsuIrcChannelMessageArg : OsuIrcEventArg
    {
        public string Sender { get; }
        public string Channel { get; }
        public string Message { get; }

        public OsuIrcChannelMessageArg(string sender, string channel, string message, OsuIrcClient client) : base(client)
        {
            Sender = sender;
            Channel = channel;
            Message = message;
        }
    }

    public class OsuIrcPrivateMessageSendArg : OsuIrcEventArg
    {
        public string Destination { get; }
        public string Message { get; }

        public OsuIrcPrivateMessageSendArg(string destination, string message, OsuIrcClient client) : base(client)
        {
            Destination = destination;
            Message = message;
        }
    }

    public class OsuIrcChannelMessageSendArg : OsuIrcEventArg
    {
        public string Destination { get; }
        public string Message { get; }

        public OsuIrcChannelMessageSendArg(string destination, string message, OsuIrcClient client) : base(client)
        {
            Destination = destination;
            Message = message;
        }
    }

    public class OsuIrcPartSendArg : OsuIrcEventArg
    {
        public string Channel { get; }

        public OsuIrcPartSendArg(string channel, OsuIrcClient client) : base(client)
        {
            Channel = channel;
        }
    }

    public class OsuIrcJoinSendArg : OsuIrcEventArg
    {
        public string Channel { get; }

        public OsuIrcJoinSendArg(string channel, OsuIrcClient client) : base(client)
        {
            Channel = channel;
        }
    }

    public class OsuIrcUserPartArg : OsuIrcPartSendArg
    {
        public string User { get; }

        public OsuIrcUserPartArg(string user, string channel, OsuIrcClient client) : base(channel, client)
        {
            User = user;
        }
    }

    public class OsuIrcUserJoinArg : OsuIrcJoinSendArg
    {
        public string User { get; }

        public OsuIrcUserJoinArg(string user, string channel, OsuIrcClient client) : base(channel, client)
        {
            User = user;
        }
    }

    public class OsuIrcUserQuitArg : OsuIrcEventArg
    {
        public string User { get; }

        public OsuIrcUserQuitArg(string user, OsuIrcClient client) : base(client)
        {
            User = user;
        }
    }

    public class OsuIrcMotdArg : OsuIrcEventArg
    {
        public List<string> MotdLines { get; }

        public string Motd
        {
            get
            {
                if (MotdLines == null || MotdLines.Count == 0)
                    return "null";

                string result = MotdLines[0];

                for (int i = 1; i < MotdLines.Count; i++)
                    result += Environment.NewLine + MotdLines[i];

                return result;
            }
        }

        public OsuIrcMotdArg(List<string> motdLines, OsuIrcClient client) : base(client)
        {
            MotdLines = motdLines;
        }
        public OsuIrcMotdArg(string[] motdLines, OsuIrcClient client) : base(client)
        {
            MotdLines = motdLines.ToList();
        }
        public OsuIrcMotdArg(OsuIrcClient client, params string[] motdLines) : base(client)
        {
            MotdLines = motdLines.ToList();
        }
    }

    public class OsuIrcUserListArg : OsuIrcEventArg
    {
        public string Channel { get; }
        public string UserListString { get; }
        public List<string> Users
        {
            get
            {
                if (UserListString == null || UserListString.Length == 0)
                    return new List<string>();

                return UserListString.Split(' ').Where(s => !string.IsNullOrEmpty(s)).ToList();
            }
        }

        public OsuIrcUserListArg(string channel, string userListString, OsuIrcClient client) : base(client)
        {
            Channel = channel;
            UserListString = userListString;
        }
    }

    public class OsuIrcUserStatsArg : OsuIrcEventArg
    {
        public string User { get; }
        public ulong Score { get; }
        public ulong Rank { get; }
        public long PlayCount { get; }
        public int Level { get; }
        public double Accuracy { get; }
        public string Status { get; }

        public OsuIrcUserStatsArg(string user, ulong score, ulong rank, long playCount, int level, double accuracy, string status, OsuIrcClient client) : base(client)
        {
            User = user;
            Score = score;
            Rank = rank;
            PlayCount = playCount;
            Level = level;
            Accuracy = accuracy;
            Status = status;
        }
    }

    public class OsuIrcUserRollArg : OsuIrcEventArg
    {
        public string User { get; }
        public int Points { get; }

        public OsuIrcUserRollArg(string user, int points, OsuIrcClient client) : base(client)
        {
            User = user;
            Points = points;
        }
    }

    public class OsuIrcUserWhereArg : OsuIrcEventArg
    {
        public string User { get; }
        public string Location { get; }

        public OsuIrcUserWhereArg(string user, string location, OsuIrcClient client) : base(client)
        {
            User = user;
            Location = location;
        }
    }
}
