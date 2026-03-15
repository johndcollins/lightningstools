using System;

namespace Henkie.Common
{
    [AttributeUsage(AttributeTargets.Field)]
    public class CommandGroupAttribute:Attribute
    {
        public CommandGroupAttribute(string commandGroupName)
        {
            CommandGroupName = commandGroupName;
        }

        public string CommandGroupName { get; set; }
    }
}
