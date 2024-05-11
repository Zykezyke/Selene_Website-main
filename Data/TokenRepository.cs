using Microsoft.EntityFrameworkCore;
using SELENE_STUDIO.Models;

namespace SELENE_STUDIO.Data
{
    public interface ITokenRepository
    {
        Task AddAsync(Token token);
        Task<Token> GetTokenAsync(string tokenValue);
    }

    public class TokenRepository : ITokenRepository
    {
        private readonly LogAppDbContext _context;

        public TokenRepository(LogAppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Token token)
        {
            _context.Tokens.Add(token);
            await _context.SaveChangesAsync();
        }

        public async Task<Token> GetTokenAsync(string tokenValue)
        {
            return await _context.Tokens.FirstOrDefaultAsync(t => t.TokenValue == tokenValue);
        }
    }

}
