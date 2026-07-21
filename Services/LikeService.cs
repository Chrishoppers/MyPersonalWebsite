using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Services
{
    public class LikeService
    {
        private readonly AppDbContext _context;

        public LikeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, int NewLikeCount, bool IsLiked)> ToggleLikeAsync(int messageId, int userId)
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message == null)
                return (false, "留言不存在", 0, false);

            if (message.UserId == userId)
                return (false, "不能给自己的留言点赞", message.LikeCount, false);

            message.LikeCount++;
            await _context.SaveChangesAsync();

            return (true, "点赞成功", message.LikeCount, true);
        }
    }
}
