namespace RoadRegistry.BackOffice
{
    using System;

    public readonly struct ExtractDescription : IEquatable<ExtractDescription>
    {
        public const int MaxLength = 256;

        private readonly string _value;

        public ExtractDescription(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "The extract description must not be null");
            }

            if (value.Length > MaxLength)
            {
                throw new ArgumentOutOfRangeException(nameof(value),
                    $"The extract description must be {MaxLength} characters or less.");
            }

            _value = value;
        }

        public static bool AcceptsValue(string value)
        {
            return value != null && value.Length <= MaxLength;
        }

        public bool Equals(ExtractDescription other) => _value == other._value;
        public override bool Equals(object obj) => obj is ExtractDescription other && Equals(other);
        public override int GetHashCode() => (_value != null ? _value.GetHashCode() : 0);
        public override string ToString() => _value;
        public static implicit operator string(ExtractDescription instance) => instance.ToString();
        public static implicit operator ExtractDescription(string instance) => new ExtractDescription(instance);
        public static bool operator ==(ExtractDescription left, ExtractDescription right) => left.Equals(right);
        public static bool operator !=(ExtractDescription left, ExtractDescription right) => !left.Equals(right);
    }
}