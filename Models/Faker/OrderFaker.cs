using SELENE_STUDIO.Data;

namespace SELENE_STUDIO.Models.Faker;

public class OrderFaker : Bogus.Faker<Order> {
    public OrderFaker(RegUser[] user, CartProduct[] products) {
        UseSeed(156782290).Rules((faker, order) => {
            order.Date = faker.Date.Past();
            order.Customer = faker.Random.ArrayElement(user);
            order.DeliveryAddress = faker.Address.FullAddress();
            order.PaymentMethod = faker.Finance.TransactionType();
            order.PaymentStatus = faker.Random.Enum<PaymentStatus>();
            order.OrderStatus = faker.Random.Enum<OrderStatus>();
            order.Products = faker.Random.ArrayElements(products).ToList();
            order.TotalPrice = faker.Random.Int(100, 1000);
        });
    }
}