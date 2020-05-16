using System;
using System.Collections.Generic;
using System.Text;

namespace SKOsuIrc
{
    public class OsuIrcUser
    {
        public string Name { get; }

        public OsuIrcUser(string name)
        {
            Name = name;
        }
    }
}
