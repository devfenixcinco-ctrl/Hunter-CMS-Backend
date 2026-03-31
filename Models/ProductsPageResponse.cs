using System.Collections.Generic;

namespace MreHunter.Api.Models
{
    public class ProductsPageResponse
    {
        public ProductsPage? Page { get; set; }
        public List<ProductHelpCard> HelpCards { get; set; } = new();
        public List<ProductOffice> Offices { get; set; } = new();
    }
}