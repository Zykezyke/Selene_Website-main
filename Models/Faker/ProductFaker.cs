namespace SELENE_STUDIO.Models.Faker;

public class ProductFaker : Bogus.Faker<Product> {
    public ProductFaker() {
        UseSeed(890234789).Rules((faker, product) => {
            product.ProductName = faker.Commerce.ProductName();
            product.ProductDescription = faker.Commerce.ProductDescription();
            product.ImagePaths = new List<string>
                {
                    faker.Image.LoremFlickrUrl(),
                    faker.Image.LoremFlickrUrl(), // Generate multiple image paths
                    faker.Image.LoremFlickrUrl()
                };
            product.ProductCategory = faker.Random.Enum<Category>();
        });
    }
}