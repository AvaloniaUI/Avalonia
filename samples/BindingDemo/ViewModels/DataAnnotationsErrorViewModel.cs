using System.ComponentModel.DataAnnotations;

namespace BindingDemo.ViewModels
{
    public class DataAnnotationsErrorViewModel
    {
        [Phone]
        [MaxLength(10)]
        public string PhoneNumber { get; set; }

        [Range(0, 9)]
        public int LessThan10 { get; set; }
    }
}
