using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SKOsuIrc.Irc
{
    public class IrcMessageArgs : IEquatable<IrcMessageArgs>
    {
        public string Prefix { get; }
        public string Command { get; }
#pragma warning disable CA1819 // Properties should not return arrays
        public string[] Parameters { get; }
#pragma warning restore CA1819 // Properties should not return arrays

        public IrcMessageArgs(string prefix, string command, params string[] parameters)
        {
            Prefix = prefix;
            Command = command;

            if (parameters == null)
                Parameters = Array.Empty<string>();
            else
                Parameters = parameters;
        }

        public IrcMessageArgs()
        {
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IrcMessageArgs);
        }

        public bool Equals(IrcMessageArgs other)
        {
            return other != null &&
                   Prefix == other.Prefix &&
                   Command == other.Command &&
                   EqualityComparer<string[]>.Default.Equals(Parameters, other.Parameters);
        }

        public override int GetHashCode()
        {
            var hashCode = -469078252;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Prefix);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Command);
            hashCode = hashCode * -1521134295 + EqualityComparer<string[]>.Default.GetHashCode(Parameters);
            return hashCode;
        }

        public static bool operator ==(IrcMessageArgs left, IrcMessageArgs right)
        {
            return EqualityComparer<IrcMessageArgs>.Default.Equals(left, right);
        }

        public static bool operator !=(IrcMessageArgs left, IrcMessageArgs right)
        {
            return !(left == right);
        }
    }
}
