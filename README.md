# SKOsuIrc

osu! Irc Client

---

https://www.nuget.org/packages/SKOsuIrc/1.0.0

```
Install-Package SKOsuIrc -Version 1.0.0
```

---

See https://github.com/Blade12629/SKOsuIrc/wiki/Documentation for documentation

---

If you need help, you can contact me via discord: ??????#0284

---

Usage:

```cs
SKOsuIrc.OsuIrcClient ircClient = new SKOsuIrc.OsuIrcClient(string nick, string pass);
//subscribe to any event here like channel messages
osuIrcClient.OnChannelMessageRecieved += new EventHandler<SKOsuIrc.Args.OsuIrcChannelMessageArg>((s, e) =>
{
  Console.WriteLine("————————————————————————————————————");
  Console.WriteLine($"New channel message recieved from {e.Sender} at {e.Channel}: {e.Message}");
  Console.WriteLine("————————————————————————————————————");
};
ircClient.Connect();
ircClient.StartReadingAsync();
ircClient.Login();
```
