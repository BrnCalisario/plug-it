// using System.Linq.Expressions;
// using Microsoft.EntityFrameworkCore;

// namespace Reddit.Repositories;
// using Model;


// public class ImageRepository : IRepository<ImageDatum>
// {
//     private RedditContext ctx;

//     public ImageRepository(RedditContext ctx)
//         => this.ctx = ctx;

//     public async void Add(ImageDatum obj)
//     {
//         await ctx.ImageData.AddAsync(obj);
//         await ctx.SaveChangesAsync();
//     }

//     public async void Delete(ImageDatum obj)
//     {
//         ctx.ImageData.Remove(obj);
//         await ctx.SaveChangesAsync();
//     }

//     public async Task<List<ImageDatum>> Filter(Expression<Func<ImageDatum, bool>> exp)
//     {
//         var query = ctx.ImageData.Where(exp);
//         return await query.ToListAsync();
//     }

//     public async void Update(ImageDatum obj)
//     {
//         ctx.ImageData.Update(obj);
//         await ctx.SaveChangesAsync();
//     }
// }