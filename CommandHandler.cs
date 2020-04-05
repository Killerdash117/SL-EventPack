﻿using Smod2.Commands;
using Smod2.EventHandlers;
using Smod2.Events;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventManager.Events
{
    public class CommandHandler : IEventHandlerAdminQuery, IEventHandlerRoundEnd, IEventHandlerRoundStart
    {
        Dictionary<string, bool> user_quered = new Dictionary<string, bool>();
        private bool once_event = false, round_ongoing = false;
        private string queue_event = null;
        public List<Event> Commands { get; } = new List<Event>();
        public void RegisterCommand(Event command)
        {
            if (Commands.Find(x => x.GetName() == command.GetName() || Array.Exists(command.GetCommands(), y => x.GetCommands().Contains(y))) == null)
            { 
                Commands.Add(command);
                PluginHandler.Shared.Info($"Added {command.GetName()} command with {command.GetCommandType()} type");
                PluginHandler.Shared.AddEventHandlers(command as IEventHandler);
            }
            else
            {
                PluginHandler.Shared.Error($"Couldn't add {command.GetName()}");   
            }
        }
        

        public void OnAdminQuery(AdminQueryEvent ev)
        {
            if (!string.IsNullOrEmpty(queue_event))
            {
                ev.Admin.PersonalBroadcast(7, "Obecnie event jest w poczekalni, spróbuj w następnej rundzie", false);
                return;
            }
            string command = null ;
            string arg = null;

            if (ev.Admin.Permissions <= 0)
                return;
            try
            {
                command = ev.Query.Split(' ')[0];
                arg = ev.Query.Split(' ')[1];
            }
            catch
            {}

            if (ev.Admin.Permissions < 3)
            {
                if (! this.user_quered.ContainsKey(ev.Admin.UserId) )
                {
                    if (this.user_quered[ev.Admin.UserId] == true)
                        arg = "once";
                    else
                        return;
                }
                else
                {
                    this.user_quered.Add(ev.Admin.UserId, true);
                    arg = "once";
                }
            }
            else
            { 
                if (arg == null)
                    arg = "once";
            }

            Event commandh = Commands.Find(x => x.GetCommands().Contains(command));
            if (commandh != null)
            {
                if (commandh.GetCommandType() == ConsoleType.RA)
                {
                    ev.Handled = true;
                    ev.Admin.SendConsoleMessage($"[{commandh.GetName()}] Tryb jest {arg}" + Environment.NewLine);
                    if (arg == "on")
                        commandh.isQueue = true;
                    else if (arg == "off")
                        commandh.isQueue = false;
                    else if (arg == "once")
                    {
                        if (round_ongoing) {
                            queue_event = commandh.GetName();
                            ev.Admin.PersonalBroadcast(7, "Event odbędzie się w następnej rundzie", false);
                        }
                        else
                            commandh.isQueue = true;
                        this.once_event = true;
                    }
                    ev.Output = "Check Console";
                    ev.Successful = true;
                    if(ev.Admin.Permissions < 3)
                        if (this.user_quered[ev.Admin.UserId] == true)
                            GetTime(ev.Admin.UserId).GetAwaiter();
                }
            }
        }

        public async Task GetTime(string userId)
        {
            this.user_quered[userId] = false;
            await Task.Delay(TimeSpan.FromHours(2));
            this.user_quered[userId] = true;
        }

        public void OnRoundEnd(RoundEndEvent ev)
        {
            if (ev.Status == Smod2.API.ROUND_END_STATUS.ON_GOING)
                this.round_ongoing = true;
            else
                this.round_ongoing = false;
            if (this.once_event)
                Commands.ForEach(x => x.isQueue = false);
            Commands.ForEach(x => x.Dispose());
            if (string.IsNullOrEmpty(queue_event))
            {
                Commands.Find(x => x.GetName() == queue_event).isQueue = true;
            }
        }

        public void OnRoundStart(RoundStartEvent ev)
        {
            this.round_ongoing = true;
            queue_event = null;
        }
    }
    public abstract class Event
    {
        public bool isQueue = false;
        public abstract string[] GetCommands();
        public abstract ConsoleType GetCommandType();
        public abstract string GetName();
        public virtual void Dispose() { return; }
        public virtual IDictionary<string, string> Translation { get; set; }
    }

    public enum ConsoleType
    {
        RA = 1,
        Client = 2,
        Server = 4
    }
}
