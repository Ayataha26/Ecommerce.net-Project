namespace MarketPlaceApi.Models
{
    public class UpdateProductModel
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? Category { get; set; }
        public string? Images { get; set; }
        public int? NumberOfAvailableUnits { get; set; }

    }
}