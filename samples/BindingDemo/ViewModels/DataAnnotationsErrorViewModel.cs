using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BindingDemo.ViewModels
{
    public class DataAnnotationsErrorViewModel
    {
        [Phone]
        [MaxLength(10)]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "MaxLength is safe here, as string implements ICollection.")]
        public string PhoneNumber { get; set; }

        [Range(0, 9)]
        public int LessThan10 { get; set; }
    }
}
