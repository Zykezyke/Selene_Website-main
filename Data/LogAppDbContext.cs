using Microsoft.EntityFrameworkCore;
using SELENE_STUDIO.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using static SELENE_STUDIO.Models.Product;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;

namespace SELENE_STUDIO.Data
{
    public class LogAppDbContext : IdentityDbContext<RegUser>
    {
        public DbSet<Admin> Admins { get; set; }
        public DbSet<RegUser> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Product> Products { get; set; }

        public DbSet<CartProduct> CartProducts { get; set; }
        public DbSet<UserCart> UserCarts { get; set; }
        public DbSet<AdditionalPrice> AdditionalPrices { get; set; }

        public DbSet<Message> Messages { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Token> Tokens { get; set; }

        public LogAppDbContext(DbContextOptions<LogAppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<AdditionalPrice>()
            .HasOne(ap => ap.Product)
            .WithMany(p => p.AdditionalPrices)
            .HasForeignKey(ap => ap.ProductId);

            modelBuilder.Entity<Product>()
        .Property(p => p.ImagePaths)
        .HasConversion(
            v => string.Join(',', v),
            v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
        );

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Message>()
              .HasOne(m => m.Sender)
              .WithMany(u => u.SentMessages)
              .HasForeignKey(m => m.SenderId)
              .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Conversation>()
                .HasMany(c => c.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);


            // Seed admin role
            modelBuilder.Entity<IdentityRole>().HasData(new IdentityRole
            {
                Id = "1",
                Name = "Admin",
                NormalizedName = "ADMIN"
            });

            // Seed admin user
            var admin = new RegUser
            {
                Id = "1",
                UserName = "admin@gmail.com",
                NormalizedUserName = "ADMIN@GMAIL.COM",
                Email = "admin@gmail.com",
                NormalizedEmail = "ADMIN@GMAIL.COM",
                EmailConfirmed = true,
                Role = "Admin",
                LockoutEnabled = true,
            };

            var user = new RegUser
            {
                Id = "2",
                UserName = "user@gmail.com",
                NormalizedUserName = "USER@GMAIL.COM",
                Email = "user@gmail.com",
                NormalizedEmail = "USER@GMAIL.COM",
                FirstName = "Test",
                LastName = "User",
                Address = "Test",
                PhoneNumber = "123456789",
                EmailConfirmed = true,
                LockoutEnabled = true,

            };

            var passwordHasher = new PasswordHasher<RegUser>();
            admin.PasswordHash = passwordHasher.HashPassword(admin, "admin123");
            user.PasswordHash = passwordHasher.HashPassword(user, "user1234");

            modelBuilder.Entity<RegUser>().HasData(admin);
            modelBuilder.Entity<RegUser>().HasData(user);

            modelBuilder.Entity<IdentityRole>().HasData(new IdentityRole
            {
                Id = "2",
                Name = "User",
                NormalizedName = "USER"
            });


            // Assign user role to user
            modelBuilder.Entity<IdentityUserRole<string>>().HasData(new IdentityUserRole<string>
            {
                RoleId = "2", // Assuming the role ID for "User" is "2"
                UserId = "2" // Assuming the user ID for "user@gmail.com" is "2"
            });

            // Assign admin role to admin user
            modelBuilder.Entity<IdentityUserRole<string>>().HasData(new IdentityUserRole<string>
            {
                RoleId = "1",
                UserId = "1"
            });

            var notepadProduct = new Product
            {
                ProductID = 1,
                ProductName = "Notepad",
                ProductCategory = Category.Notepad,
                ProductDescription = "Introducing our sleek notepad, your go-to companion for jotting down thoughts, ideas, and lists on the fly. With premium paper and a durable cover, it's perfect for professionals, students, and creatives alike. Get organized and stay stylish with our Notepad today. 50 sheets printed in an 80 gsm fsc certified paper to ensure responsibility and sustainability.",
                DateCreated = DateTime.Now,
                isHidden = false,
                ImagePaths = new List<string>
    {
        "/products/images/note_pad_1.png",
        "/products/images/note_pad_2.png",
        "/products/images/note_pad_3.png",
        "/products/images/note_pad_4.png"
    }
            };

            var notepadSizesWithPrices = new Dictionary<Size, decimal>
{
    { Size.FourByFour, 60.00m },
    { Size.A5, 85.00m },
    { Size.A4, 165.00m }
};

            // Adjust the index for each product to avoid conflicts
            var notepadIndex = 1;

            modelBuilder.Entity<Product>().HasData(notepadProduct);
            modelBuilder.Entity<AdditionalPrice>().HasData(
                notepadSizesWithPrices.Select((sizePricePair, index) => new AdditionalPrice
                {
                    Id = notepadIndex + index, // Ensure unique IDs
                    ProductId = notepadProduct.ProductID,
                    Size = GetEnumDisplayName(sizePricePair.Key),
                    Price = sizePricePair.Value
                }).ToArray()
            );

            // Repeat the process for other products with multiple images

            var stickerProduct = new Product
            {
                ProductID = 2,
                ProductName = "Sticker",
                ProductCategory = Category.Sticker,
                ProductDescription = "Introducing our Customized Stickers - where creativity meets convenience. Tailored to your unique style, these stickers add a personal touch to any surface. Perfect for labeling, decorating, or branding, make a lasting impression with our customizable stickers. High quality stickers printed in waterproof vinyl sticker papers.",
                DateCreated = DateTime.Now,
                isHidden = false,
                ImagePaths = new List<string>
    {
        "/products/images/sticker_1.png",
        "/products/images/sticker_2.png",
        "/products/images/sticker_3.png",
        "/products/images/sticker_4.png"
    }
            };

            var stickerSizesWithPrices = new Dictionary<Size, decimal>
{
    { Size.OneByOne, 1.00m },
    { Size.TwoByTwo, 2.50m },
    { Size.ThreeByThree, 5.00m }
};

            // Adjust the index for each product to avoid conflicts
            var stickerIndex = notepadIndex + notepadSizesWithPrices.Count;

            modelBuilder.Entity<Product>().HasData(stickerProduct);
            modelBuilder.Entity<AdditionalPrice>().HasData(
                stickerSizesWithPrices.Select((sizePricePair, index) => new AdditionalPrice
                {
                    Id = stickerIndex + index, // Ensure unique IDs
                    ProductId = stickerProduct.ProductID,
                    Size = GetEnumDisplayName(sizePricePair.Key),
                    Price = sizePricePair.Value
                }).ToArray()
            );

            var notebookProduct = new Product
            {
                ProductID = 3,
                ProductName = "Notebook",
                ProductCategory = Category.Notebook,
                ProductDescription = "Introducing our Customized Notepad - the perfect blend of style and functionality. Tailored to your preferences, it's a personalized solution for all your note-taking needs. Whether for business or pleasure, make a statement with every jot and scribble. Available in hard cover with 50 sheets printed in an 80 gsm fsc certified paper and binded in a coiled spring.",
                DateCreated = DateTime.Now,
                isHidden = false,
                ImagePaths = new List<string>
    {
        "/products/images/notebook_1.png",
        "/products/images/notebook_2.png",
        "/products/images/notebook_3.png"
    }
            };

            var notebookSizesWithPrices = new Dictionary<Size, decimal>
{
    { Size.A5, 160.00m },
    { Size.A4, 260.00m }
};

            // Adjust the index for each product to avoid conflicts
            var notebookIndex = stickerIndex + stickerSizesWithPrices.Count;

            modelBuilder.Entity<Product>().HasData(notebookProduct);
            modelBuilder.Entity<AdditionalPrice>().HasData(
                notebookSizesWithPrices.Select((sizePricePair, index) => new AdditionalPrice
                {
                    Id = notebookIndex + index, // Ensure unique IDs
                    ProductId = notebookProduct.ProductID,
                    Size = GetEnumDisplayName(sizePricePair.Key),
                    Price = sizePricePair.Value
                }).ToArray()
            );

            var thankYouCardProduct = new Product
            {
                ProductID = 4,
                ProductName = "Thank You Card",
                ProductCategory = Category.ThankYouCard,
                ProductDescription = "Elevate your gratitude with our Customized Thank You Cards. Tailored to reflect your personal touch, these cards are perfect for expressing appreciation in style. Whether for weddings, birthdays, or business, make every 'thank you' memorable with our customizable cards.",
                DateCreated = DateTime.Now,
                isHidden = false,
                ImagePaths = new List<string>
    {
        "/products/images/thank_you_card_1.png",
        "/products/images/thank_you_card_2.png",
        "/products/images/thank_you_card_3.png"
    }
            };

            var thankYouCardSizesWithPrices = new Dictionary<Size, decimal>
{
    { Size.ThreeByTwo, 65.50m },
    { Size.ThreePointFiveByTwoPointFive, 75.50m },
    { Size.FourByTwo, 85.50m }
};

            // Adjust the index for each product to avoid conflicts
            var thankYouCardIndex = notebookIndex + notebookSizesWithPrices.Count;

            modelBuilder.Entity<Product>().HasData(thankYouCardProduct);
            modelBuilder.Entity<AdditionalPrice>().HasData(
                thankYouCardSizesWithPrices.Select((sizePricePair, index) => new AdditionalPrice
                {
                    Id = thankYouCardIndex + index, // Ensure unique IDs
                    ProductId = thankYouCardProduct.ProductID,
                    Size = GetEnumDisplayName(sizePricePair.Key),
                    Price = sizePricePair.Value
                }).ToArray()
            );

            var cupcakeTopperProduct = new Product
            {
                ProductID = 5,
                ProductName = "Cupcake Topper",
                ProductCategory = Category.CupcakeTopper,
                ProductDescription = "Introducing our Custom Cupcake Toppers – the perfect way to add a personalized touch to your sweet treats. Tailored to your theme or occasion, these toppers elevate cupcakes for birthdays, weddings, or any celebration. With customizable designs and durable materials, make your desserts stand out with our bespoke cupcake toppers.",
                DateCreated = DateTime.Now,
                isHidden = false,
                ImagePaths = new List<string>
    {
        "/products/images/cupcake_topper_1.png",
        "/products/images/cupcake_topper_2.png",
        "/products/images/cupcake_topper_3.png",
        "/products/images/cupcake_topper_4.png"
    }
            };

            var cupcakeTopperSizesWithPrices = new Dictionary<Size, decimal>
{
    { Size.TwoByTwo, 120.00m }
};

            // Adjust the index for each product to avoid conflicts
            var cupcakeTopperIndex = thankYouCardIndex + thankYouCardSizesWithPrices.Count;

            modelBuilder.Entity<Product>().HasData(cupcakeTopperProduct);
            modelBuilder.Entity<AdditionalPrice>().HasData(
                cupcakeTopperSizesWithPrices.Select((sizePricePair, index) => new AdditionalPrice
                {
                    Id = cupcakeTopperIndex + index, // Ensure unique IDs
                    ProductId = cupcakeTopperProduct.ProductID,
                    Size = GetEnumDisplayName(sizePricePair.Key),
                    Price = sizePricePair.Value
                }).ToArray()
            );

            var businessCardProduct = new Product
            {
                ProductID = 6,
                ProductName = "Business Card",
                ProductCategory = Category.BusinessCard,
                ProductDescription = "Introducing our Business Cards - your key to making a lasting impression. Sleek, professional, and customizable to reflect your brand identity, these cards are perfect for networking and leaving a memorable mark. With premium materials and attention to detail, elevate your business image with our bespoke business cards.",
                DateCreated = DateTime.Now,
                isHidden = false,
                ImagePaths = new List<string>
    {
        "/products/images/business_card_1.png",
        "/products/images/business_card_2.png",
        "/products/images/business_card_3.png",
        "/products/images/business_card_4.png"
    }
            };

            var businessCardSizesWithPrices = new Dictionary<Size, decimal>
{
    { Size.HundredPcs, 300.00m }
};

            // Adjust the index for each product to avoid conflicts
            var businessCardIndex = cupcakeTopperIndex + cupcakeTopperSizesWithPrices.Count;

            modelBuilder.Entity<Product>().HasData(businessCardProduct);
            modelBuilder.Entity<AdditionalPrice>().HasData(
                businessCardSizesWithPrices.Select((sizePricePair, index) => new AdditionalPrice
                {
                    Id = businessCardIndex + index, // Ensure unique IDs
                    ProductId = businessCardProduct.ProductID,
                    Size = GetEnumDisplayName(sizePricePair.Key),
                    Price = sizePricePair.Value
                }).ToArray()
            );




            modelBuilder.Entity<Product>().ToTable("Products");

            modelBuilder.Entity<Admin>().HasData(new Admin
            {
                AdminId = 1,
                Name = "Administrator",
                Email = "admin@gmail.com"
            });
        }
        private string GetEnumDisplayName(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = (DisplayAttribute)Attribute.GetCustomAttribute(field, typeof(DisplayAttribute));
            return attribute == null ? value.ToString() : attribute.Name;
        }
    }
}
