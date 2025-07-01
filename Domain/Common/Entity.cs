using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OddScout.Domain.Common
{
    public abstract class Entity<T> : IEquatable<Entity<T>>
    {
        public T Id { get; protected set; } = default!;

        public bool Equals(Entity<T>? other)
        {
            return other is not null && Id!.Equals(other.Id);
        }

        public override bool Equals(object? obj)
        {
            return obj is Entity<T> entity && Equals(entity);
        }

        public override int GetHashCode()
        {
            return Id!.GetHashCode();
        }

        public static bool operator ==(Entity<T>? left, Entity<T>? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Entity<T>? left, Entity<T>? right)
        {
            return !Equals(left, right);
        }
    }
}
