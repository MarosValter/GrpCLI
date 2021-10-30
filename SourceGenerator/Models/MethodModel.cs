using System.Collections.Generic;

namespace SourceGenerator.Models
{
    internal class MethodModel
    {
        internal string Name { get; set; }
        internal string Description { get; set; }
        internal DataTypeModel Request { get; set; }
        internal DataTypeModel Response { get; set; }

        public override bool Equals(object obj)
        {
            return obj is MethodModel model &&
                   Name == model.Name &&
                   Description == model.Description &&
                   EqualityComparer<DataTypeModel>.Default.Equals(Request, model.Request) &&
                   EqualityComparer<DataTypeModel>.Default.Equals(Response, model.Response);
        }

        public override int GetHashCode()
        {
            var hashCode = 647983145;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Description);
            hashCode = hashCode * -1521134295 + EqualityComparer<DataTypeModel>.Default.GetHashCode(Request);
            hashCode = hashCode * -1521134295 + EqualityComparer<DataTypeModel>.Default.GetHashCode(Response);
            return hashCode;
        }
    }
}
