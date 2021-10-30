using System.Collections.Generic;

namespace SourceGenerator.Models
{
    internal class DataTypeModel
    {
        internal const string Empty = "Empty";

        internal string Type { get; set; }
        internal string Namespace { get; set; }
        internal string Description { get; set; }
        internal bool Repeated { get; set; }
        internal bool IsEmpty => Type.Equals(Empty, System.StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj)
        {
            return obj is DataTypeModel model &&
                   Type == model.Type &&
                   Namespace == model.Namespace &&
                   Description == model.Description &&
                   Repeated == model.Repeated;
        }

        public override int GetHashCode()
        {
            var hashCode = -1630219311;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Type);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Namespace);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Description);
            hashCode = hashCode * -1521134295 + Repeated.GetHashCode();
            return hashCode;
        }
    }
}
