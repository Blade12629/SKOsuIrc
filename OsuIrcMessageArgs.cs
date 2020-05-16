using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SKOsuIrc
{
    public sealed class OsuIrcMessageArgs : IEquatable<OsuIrcMessageArgs>
    {
        public string Sender { get; }
        public string From { get; }
        public string Destination { get; }
        public string Channel { get; }
        public string Command { get; }
        public string Parameters { get; }

        public OsuIrcMessageArgs(string sender, string from, string destination, string channel, string command, string parameters)
        {
            Sender = sender;
            From = from;
            Destination = destination;
            Channel = channel;
            Command = command;
            Parameters = parameters;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as OsuIrcMessageArgs);
        }

        public bool Equals(OsuIrcMessageArgs other)
        {
            return other != null &&
                   Sender == other.Sender &&
                   From == other.From &&
                   Destination == other.Destination &&
                   Channel == other.Channel &&
                   Command == other.Command &&
                   Parameters == other.Parameters;
        }

        public override int GetHashCode()
        {
            var hashCode = -1273258849;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Sender);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(From);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Destination);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Channel);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Command);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Parameters);
            return hashCode;
        }

        public static bool operator ==(OsuIrcMessageArgs left, OsuIrcMessageArgs right)
        {
            return EqualityComparer<OsuIrcMessageArgs>.Default.Equals(left, right);
        }

        public static bool operator !=(OsuIrcMessageArgs left, OsuIrcMessageArgs right)
        {
            return !(left == right);
        }
    }
}
