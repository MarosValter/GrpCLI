using System.Collections.Generic;

namespace SourceGenerator.Models
{
    internal class ServiceModel
    {
        internal string Name { get; set; }
        internal string Description { get; set; }
        internal MethodModel[] Methods { get; set; }

        public override bool Equals(object obj)
        {
            return obj is ServiceModel model &&
                   Name == model.Name &&
                   Description == model.Description &&
                   EqualityComparer<MethodModel[]>.Default.Equals(Methods, model.Methods);
        }

        public override int GetHashCode()
        {
            var hashCode = 261729008;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Description);
            hashCode = hashCode * -1521134295 + EqualityComparer<MethodModel[]>.Default.GetHashCode(Methods);
            return hashCode;
        }
    }
}
