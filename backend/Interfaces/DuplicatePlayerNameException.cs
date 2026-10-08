using System;

namespace backend.Interfaces
{
    public sealed class DuplicatePlayerNameException : Exception
    {
        public DuplicatePlayerNameException(string name)
            : base($"A player named '{name}' already exists.")
        {
            Name = name;
        }

        public string Name { get; }
    }
}
