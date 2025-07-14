namespace HiperShopping.ProductAPI.Data.ValueObjects
{
    public interface IProductVO  
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string CategoryName { get; set; }
        public string ImageURL { get; set; }
    }
}
