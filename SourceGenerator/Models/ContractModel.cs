using System.Collections.Generic;

namespace SourceGenerator.Models
{
    internal class ContractModel
    {
        internal string Name { get; set; }
        internal string Namespace { get; set; }
        internal ServiceModel[] Services { get; set; }

        public override bool Equals(object obj)
        {
            return obj is ContractModel model &&
                   Name == model.Name &&
                   Namespace == model.Namespace &&
                   EqualityComparer<ServiceModel[]>.Default.Equals(Services, model.Services);
        }

        public override int GetHashCode()
        {
            var hashCode = 496075515;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Namespace);
            hashCode = hashCode * -1521134295 + EqualityComparer<ServiceModel[]>.Default.GetHashCode(Services);
            return hashCode;
        }
    }
}
