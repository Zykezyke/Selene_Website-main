using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SELENE_STUDIO.Models
{

    public enum Category
    {
        [Display(Name = "Notepad")]
        Notepad,

        [Display(Name = "Sticker")]
        Sticker,

        [Display(Name = "Notebook")]
        Notebook,

        [Display(Name = "Thank You Card")]
        ThankYouCard,

        [Display(Name = "Cupcake Topper")]
        CupcakeTopper,

        [Display(Name = "Business Card")]
        BusinessCard,
    }

    public enum Size
    {
        [Display(Name = "4x4")]
        FourByFour,

        [Display(Name = "A5")]
        A5,

        [Display(Name = "A4")]
        A4,

        [Display(Name = "1x1")]
        OneByOne,

        [Display(Name = "2x2")]
        TwoByTwo,

        [Display(Name = "3x3")]
        ThreeByThree,

        [Display(Name = "3x2")]
        ThreeByTwo,

        [Display(Name = "3.5 x 2.5")]
        ThreePointFiveByTwoPointFive,

        [Display(Name = "4 x 2.5")]
        FourByTwo,

        [Display(Name = "100 pcs")]
        HundredPcs,
    }

    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductID { get; set; }

        [Display(Name = "Product Images")]
        [DataType(DataType.Upload)]
        [Required(ErrorMessage = "Please select at least one image.")]
        [NotMapped] // This property won't be mapped to the database
        public ICollection<IFormFile> ImageFiles { get; set; }

        public ICollection<string> ImagePaths { get; set; }

        [Required(ErrorMessage = "Product Name is required.")]
        public string ProductName { get; set; }

        [Required(ErrorMessage = "Product Category is required.")]
        public Category ProductCategory { get; set; }

        public DateTime DateCreated { get; set; }

        public bool isHidden { get; set; }

        public Size ProductSize { get; set; }


        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500, ErrorMessage = "Description must be less than 500 characters.")]
        public string ProductDescription { get; set; }

        public ICollection<AdditionalPrice> AdditionalPrices { get; set; }
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

        public class AdditionalPrice
        {
            public int Id { get; set; }
            public int ProductId { get; set; }
            public string Size { get; set; }
            public decimal Price { get; set; }

            public Product Product { get; set; }
        }

        public Product()
        {
            AdditionalPrices = new List<AdditionalPrice>();
            ImageFiles = new List<IFormFile>();
            ImagePaths = new List<string>();
        }



        public IEnumerable<string> GetAvailableSizes()
        {
            switch (ProductCategory)
            {
                case Category.Notepad:
                    return Enum.GetValues(typeof(Size))
                               .Cast<Size>()
                               .Where(s => s == Size.FourByFour || s == Size.A5 || s == Size.A4)
                               .Select(s => GetEnumDisplayName(s));

                case Category.Sticker:
                    return Enum.GetValues(typeof(Size))
                               .Cast<Size>()
                               .Where(s => s == Size.OneByOne || s == Size.TwoByTwo || s == Size.ThreeByThree)
                               .Select(s => GetEnumDisplayName(s));

                case Category.Notebook:
                    return Enum.GetValues(typeof(Size))
                               .Cast<Size>()
                               .Where(s => s == Size.A5 || s == Size.A4)
                               .Select(s => GetEnumDisplayName(s));

                case Category.ThankYouCard:
                    return Enum.GetValues(typeof(Size))
                               .Cast<Size>()
                               .Where(s => s == Size.ThreeByTwo || s == Size.ThreePointFiveByTwoPointFive || s == Size.FourByTwo)
                               .Select(s => GetEnumDisplayName(s));

                case Category.CupcakeTopper:
                    return Enum.GetValues(typeof(Size))
                               .Cast<Size>()
                               .Where(s => s == Size.TwoByTwo)
                               .Select(s => GetEnumDisplayName(s));

                case Category.BusinessCard:
                    return Enum.GetValues(typeof(Size))
                               .Cast<Size>()
                               .Where(s => s == Size.HundredPcs)
                               .Select(s => GetEnumDisplayName(s));

                default:
                    throw new InvalidOperationException("Invalid category.");
            }
        }


        private static string GetEnumDisplayName(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = (DisplayAttribute)Attribute.GetCustomAttribute(field, typeof(DisplayAttribute));
            return attribute == null ? value.ToString() : attribute.Name;
        }
    }
}
