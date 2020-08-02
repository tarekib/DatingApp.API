using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatingApp.API.Data
{
    public class DatingRepository : IDatingRepository
    {
        #region Private Fields
        private readonly DatingAppContext _context;
        #endregion

        #region Constructor
        public DatingRepository(DatingAppContext context)
        {
            _context = context;
        }
        #endregion

        #region Public Methods
        public void Add<T>(T entity) where T : class
        {
            _context.Add(entity);
        }

        public void Delete<T>(T entity) where T : class
        {
            _ = _context.Remove(entity);
        }

        public async Task<Like> GetLike(int userId, int recipientId)
        {
            return await _context.Likes.FirstOrDefaultAsync(u => u.LikerId == userId && u.LikeeId == recipientId);
        }

        public async Task<Photo> GetMainPhotoForUser(int id)
        {
            return await _context.Photos.Where(p => p.User.Id == id).FirstOrDefaultAsync(p => p.IsMain);
        }

        public async Task<Photo> GetPhoto(int id)
        {
            return await _context.Photos.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<User> GetUser(int id)
        {
            var user = await _context.Users.Include(p => p.Photos).FirstOrDefaultAsync(u => u.Id == id);
            return user;
        }

        public async Task<PagedList<User>> GetUsers(int pageNumber, int countPerPage, int userId, string gender, int minAge, int maxAge, bool likers = false, bool likees = false)
        {
            var users = _context.Users.Include(p => p.Photos).AsQueryable();
            users = users.Where(u => u.Id != userId);
            users = users.Where(u => u.Gender == gender);

            if (likers)
            {
                var userLikers = await GetUserLikes(userId, likers);
                users = users.Where(u => userLikers.Contains(u.Id));
            }

            if (likees)
            {
                var userLikees = await GetUserLikes(userId, likers);
                users = users.Where(u => userLikees.Contains(u.Id));
            }

            if (minAge != 18 || maxAge != 99)
            {
                var minDob = DateTime.Today.AddYears(-maxAge - 1);
                var maxDob = DateTime.Today.AddYears(-minAge);

                users = users.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);
            }
            return await PagedList<User>.CreateAsync(users, pageNumber, countPerPage);
        }

        private async Task<IEnumerable<int>> GetUserLikes(int id, bool likers)
        {
            var user = await _context.Users
                .Include(x => x.Likers)
                .Include(x => x.Likees)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (likers)
                return user.Likers.Where(u => u.LikeeId == id).Select(i => i.LikerId);

            else return user.Likees.Where(u => u.LikerId == id).Select(i => i.LikeeId);

        }

        public async Task<bool> SaveAll()
        {
            return await _context.SaveChangesAsync() > 0;
        }
        #endregion
    }
}
