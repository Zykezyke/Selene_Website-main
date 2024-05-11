using SELENE_STUDIO.Data;
using Microsoft.AspNetCore.Identity;

namespace SELENE_STUDIO.Models.Faker;

public class RegUserFaker : Bogus.Faker<RegUser> {
    PasswordHasher<RegUser> passwordHasher = new PasswordHasher<RegUser>();
    public RegUserFaker() {
        UseSeed(1234890).Rules((faker, regUser) => {
            regUser.UserName = faker.Internet.UserName();
            regUser.FirstName = faker.Name.FirstName();
            regUser.LastName = faker.Name.LastName();
            regUser.Email = faker.Internet.Email();
            regUser.EmailConfirmed = faker.Random.Bool();
            regUser.Role = faker.Random.ArrayElement(new[] { "Admin", "User" });
            regUser.Address = faker.Address.FullAddress();
            regUser.PhoneNumber = faker.Phone.PhoneNumber();
            regUser._Faker_Password = faker.Internet.Password();
            regUser.PasswordHash = passwordHasher.HashPassword(regUser, regUser._Faker_Password);
        });
    }
}