using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Reddit.Controllers;

using Model;
using Repositories;
using DTO;
using Services;

[ApiController]
[EnableCors("MainPolicy")]
[Route("post")]
public class PostController : RedditController
{

    private IPostRepository postRepository;
    private IGroupRepository groupRepository;
    private IUserRepository userRepository;

    public PostController(
        [FromServices] IUserService userService,
        [FromServices] IPostRepository postRepository,
        [FromServices] IGroupRepository groupRepository,
        [FromServices] IUserRepository userRepository
    ) : base(userService)
    {
        this.postRepository = postRepository;
        this.groupRepository = groupRepository;
        this.userRepository = userRepository;
    }


    [HttpPost("single")]
    public async Task<ActionResult<FeedPostDTO>> GetSingle([FromBody] CreatePostDTO postData)
    {
        User user = await this.ValidateJwt(postData.Jwt);

        if (user is null)
            return NotFound("User not found");

        Group group = await groupRepository.Find(postData.GroupID);

        if (group is null)
            return NotFound("Group not found");

        bool canView = await groupRepository.IsMember(user, group);

        if (!canView)
            return Forbid("User isn't a member");

        Post post = await postRepository.Find(postData.Id);

        if (post is null)
            return NotFound("Post not found");

        FeedPostDTO fp = new FeedPostDTO(post)
        {
            GroupName = group.Name,
            GroupId = group.Id,
            VoteValue = (int)await postRepository.GetPostVote(user, post),
            LikeCount = await postRepository.GetLikeCount(post),
            IsAuthor = post.Author.Username == user.Username,
            CanDelete = await groupRepository.HasPermission(user, group, PermissionEnum.Delete)
        };

        return Ok(fp);
    }


    [HttpPost]
    public async Task<ActionResult<int>> Post([FromBody] CreatePostDTO postData)
    {
        User user = await this.ValidateJwt(postData.Jwt);

        if (user is null)
            return NotFound();

        var groupQuery = await groupRepository.Filter(g => g.Id == postData.GroupID);
        var group = groupQuery.FirstOrDefault();

        if (group is null)
            return BadRequest();

        Post post = new Post
        {
            Title = postData.Title,
            Content = postData.Content,
            GroupId = postData.GroupID,
            AuthorId = user.Id,
            IndexedImage = null,
        };

        await postRepository.Add(post);

        return Ok(post.Id);
    }

    [HttpPost("vote")]
    public async Task<ActionResult> LikePost([FromBody] VoteDTO voteData)
    {
        User user = await this.ValidateJwt(voteData.Jwt);

        Post target = await postRepository.Find(voteData.PostId);

        if (target is null)
            return NotFound("Post não encontrado");

        bool hasVoted = await postRepository.HasVoted(user, target);

        if (hasVoted)
            await postRepository.UndoVote(user, target);

        Upvote vote = new Upvote()
        {
            UserId = user.Id,
            PostId = voteData.PostId,
            Value = voteData.Value
        };

        await postRepository.Vote(vote);

        return Ok();
    }

    [HttpPost("undo")]
    public async Task<ActionResult> UnlikePost([FromBody] VoteDTO voteData)
    {
        User user = await this.ValidateJwt(voteData.Jwt);

        Post post = await postRepository.Find(voteData.PostId);

        if (post is null)
            return NotFound("Post not found");

        await postRepository.UndoVote(user, post);

        return Ok();
    }


    [HttpPost("comment")]
    public async Task<ActionResult> Comment([FromBody] CommentDTO commentData)
    {
        if (commentData.Content.Length < 1)
            return BadRequest("Conteúdo necessário");

        User user = await this.ValidateJwt(commentData.Jwt);

        Comment c = new Comment()
        {
            AuthorId = user.Id,
            PostId = commentData.PostID,
            Content = commentData.Content,
        };

        await postRepository.AddComment(c);

        return Ok();
    }


    [HttpDelete("delete-comment")]
    public async Task<ActionResult> DeleteComment(
        [FromBody] CommentDTO commentData,
        [FromServices] IRepository<Comment> commentRepository
    )
    {
        User user = await this.ValidateJwt(commentData.Jwt);

        if (user is null)
            return BadRequest("Invalid user");

        var comment = await commentRepository.Find(commentData.Id);

        Group group = comment.Post.Group;

        bool canRemove = await groupRepository.HasPermission(user, group, PermissionEnum.Delete);

        if (!canRemove && comment.AuthorId != user.Id)
            return StatusCode(405);

        await commentRepository.Delete(comment);

        return Ok();
    }

    [HttpPut]
    public async Task<ActionResult> UpdatePost([FromBody] CreatePostDTO postData)
    {
        Post post = await postRepository.Find(postData.Id);

        if (post is null)
            return NotFound();

        User user = await this.ValidateJwt(postData.Jwt);

        if (user is null)
            return BadRequest("Invalid user");

        if (post.Title != postData.Title)
            post.Title = postData.Title;

        if (post.Content != postData.Content)
            post.Content = postData.Content;

        await postRepository.Update(post);

        return Ok();
    }

    [HttpPost("remove")]
    public async Task<ActionResult> Delete([FromBody] CreatePostDTO postData)
    {
        Post post = await postRepository.Find(postData.Id);

        if (post is null)
            return NotFound();

        User user = await this.ValidateJwt(postData.Jwt);

        if (post.Group is null)
            post.Group = await groupRepository.Find(postData.GroupID);

        bool canDelete = await groupRepository.HasPermission(user, post.Group, PermissionEnum.Delete);

        if (!canDelete && post.AuthorId != user.Id)
            return BadRequest();

        await postRepository.Delete(post);

        return Ok();
    }


    [HttpPost("main-feed")]
    public async Task<ActionResult<List<FeedPostDTO>>> GetMainFeed([FromBody] Jwt jwt)
    {
        User user = await this.ValidateJwt(jwt.Value);

        if (user is null)
            return NotFound();

        List<FeedPostDTO> feedPosts = new List<FeedPostDTO>();

        var userGroups = await groupRepository.GetUserGroups(user);

        if (userGroups.Count() == 0)
            return Ok(feedPosts);

        foreach (var group in userGroups)
        {
            var posts = await postRepository.Filter(p => p.GroupId == group.Id);

            foreach (var post in posts)
            {
                FeedPostDTO fp = new FeedPostDTO(post)
                {
                    GroupName = group.Name,
                    GroupId = group.Id,
                    VoteValue = (int)await postRepository.GetPostVote(user, post),
                    LikeCount = await postRepository.GetLikeCount(post),
                    IsAuthor = post.Author.Username == user.Username,
                    CanDelete = await groupRepository.HasPermission(user, group, PermissionEnum.Delete)
                };

                feedPosts.Add(fp);
            }
        }

        return Ok(feedPosts.OrderByDescending(p => p.PostDate));
    }

    [HttpPost("group-feed/id")]
    public async Task<ActionResult<List<FeedPostDTO>>> GetGroupFeed(
        [FromBody] CreateGroupDTO groupData
        )
    {
        User user = await this.ValidateJwt(groupData.Jwt);

        if (user is null)
            return NotFound("User not found");

        List<FeedPostDTO> feedPosts = new List<FeedPostDTO>();

        Group group = await groupRepository.Find(groupData.Id);

        if (group is null)
            return NotFound("Group not found");

        var groupPosts = await postRepository.Filter(p => p.GroupId == group.Id);

        foreach (var post in groupPosts)
        {
            FeedPostDTO fp = new FeedPostDTO(post)
            {
                GroupId = group.Id,
                GroupName = group.Name,
                VoteValue = (int)await postRepository.GetPostVote(user, post),
                LikeCount = await postRepository.GetLikeCount(post),
                IsAuthor = post.Author.Username == user.Username,
                CanDelete = await groupRepository.HasPermission(user, group, PermissionEnum.Delete)
            };

            feedPosts.Add(fp);
        }

        return Ok(feedPosts.OrderByDescending(f => f.PostDate));
    }

    [HttpPost("group-feed/group-name")]
    public async Task<ActionResult<List<FeedPostDTO>>> GetGroupFeedByName(
        [FromBody] CreateGroupDTO groupData
    )
    {
        User user = await this.ValidateJwt(groupData.Jwt);

        if (user is null)
            return NotFound("User not found");

        List<FeedPostDTO> feedPosts = new List<FeedPostDTO>();

        var query = await groupRepository.Filter(g => g.Name == groupData.Name);

        Group group = query.FirstOrDefault();

        if (group is null)
            return NotFound("Group not found");

        var groupPosts = await postRepository.Filter(p => p.GroupId == group.Id);

        foreach (var post in groupPosts)
        {
            FeedPostDTO fp = new FeedPostDTO(post)
            {
                GroupId = group.Id,
                GroupName = group.Name,
                VoteValue = (int)await postRepository.GetPostVote(user, post),
                LikeCount = await postRepository.GetLikeCount(post),
                IsAuthor = post.Author.Username == user.Username,
                CanDelete = await groupRepository.HasPermission(user, group, PermissionEnum.Delete)
            };

            feedPosts.Add(fp);
        }

        return Ok(feedPosts.OrderByDescending(f => f.PostDate));
    }
}