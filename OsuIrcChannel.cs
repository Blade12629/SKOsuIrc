using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SKOsuIrc
{
    public class OsuIrcChannel
    {
        public bool IsPrivateChannel { get; }
        public string Channel { get; }
        public IReadOnlyDictionary<string, OsuIrcUser> Users { get { return new ReadOnlyDictionary<string, OsuIrcUser>(_users); } }

        private Dictionary<string, OsuIrcUser> _users;
        private object _usersLock = new object();

        public OsuIrcChannel(bool privChannel, string channel)
        {
            IsPrivateChannel = privChannel;
            Channel = channel;
            _users = new Dictionary<string, OsuIrcUser>();
        }

        public void AddUser(OsuIrcUser user)
        {
            lock(_usersLock)
            {
                if (_users.ContainsKey(user.Name))
                    _users[user.Name] = user;
                else
                    _users.Add(user.Name, user);
            }
        }

        public void AddUsers(params OsuIrcUser[] users)
        {
            lock(_usersLock)
            {
                foreach(OsuIrcUser user in users)
                {
                    if (_users.ContainsKey(user.Name))
                        _users[user.Name] = user;
                    else
                        _users.Add(user.Name, user);
                }
            }
        }

        public void RemoveUser(OsuIrcUser user)
        {
            RemoveUser(user.Name);
        }

        public void RemoveUser(string user)
        {
            lock (_usersLock)
            {
                if (!_users.ContainsKey(user))
                    return;

                _users.Remove(user);
            }
        }

        public bool IsUserInChannel(OsuIrcUser user)
        {
            return IsUserInChannel(user.Name);
        }

        public bool IsUserInChannel(string user)
        {
            return _users.ContainsKey(user);
        }
    }
}
