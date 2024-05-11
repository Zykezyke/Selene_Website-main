using SELENE_STUDIO.Models;
using System.Collections.Generic;

namespace SELENE_STUDIO.ViewModels
{
    public class HomeViewModel
    {
        public List<Product> TopProducts { get; set; }
        public List<Feedback>? FeaturedFeedbacks { get; set; }
    }
}
