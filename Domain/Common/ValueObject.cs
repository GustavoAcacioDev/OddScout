namespace OddScout.Domain.Common
{
    public abstract class ValueObject : IEquatable<ValueObject>
    {
        protected abstract IEnumerable<object> GetEqualityComponents();

        public bool Equals(ValueObject? other)
        {
            return other is not null && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public override bool Equals(object? obj)
        {
            return obj is ValueObject valueObject && Equals(valueObject);
        }

        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Select(x => x?.GetHashCode() ?? 0)
                .Aggregate((x, y) => x ^ y);
        }

        public static bool operator ==(ValueObject? left, ValueObject? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ValueObject? left, ValueObject? right)
        {
            return !Equals(left, right);
        }
    }

}
