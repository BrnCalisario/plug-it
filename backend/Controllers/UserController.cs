using Microsoft.AspNetCore.Mvc;
using Security.Jwt;
using Microsoft.AspNetCore.Cors;

namespace Reddit.Controllers;

using Model;
using Repositories;
using DTO;
using Services;

[ApiController]
[EnableCors("MainPolicy")]
[Route("user")]
public class UserController : RedditController
{
    private IUserRepository userRepository;
    private IGroupRepository groupRepository;

    public UserController(
        [FromServices] IUserService userService,
        [FromServices] IUserRepository userRepository,
        [FromServices] IGroupRepository groupRepository
    ) : base(userService)
    {
        this.userRepository = userRepository;
        this.groupRepository = groupRepository;
    }

    [HttpPost("single")]
    public async Task<ActionResult<UserData>> Get([FromBody] Jwt jwt)
    {
        User user = await this.ValidateJwt(jwt.Value);

        if (user is null)
            return BadRequest("Invalid User ID");

        var query = await this.groupRepository.GetUserGroups(user);

        var groupsDTO = query.Select(g => new GroupDTO
        {
            Name = g.Name,
            Description = g.Description,
            ImageId = g.Image,
            Posts = new List<PostDTO>(),
            isMember = true,
            UserQuantity = null,
        }).ToList();

        UserData result = new UserData()
        {
            Username = user.Username,
            Email = user.Email,
            ProfilePicture = user.ProfilePicture,
            Groups = groupsDTO,
        };

        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<User>>> GetAll()
    {
        var query = await this.userRepository.Filter(u => true);
        return query;
    }

    [HttpPost("register")]
    public async Task<ActionResult<int>> Register( 
        [FromServices] IPasswordHasher psh,
        [FromBody] UserRegister userData)
    {
        var query = await this.userRepository.Filter(u => u.Username == userData.Username || u.Email == userData.Email);

        if (query.Count() > 0)
            return BadRequest();

        byte[] hashPassword;
        string salt;

        (hashPassword, salt) = psh.GetHashAndSalt(userData.Password);

        User u = new User()
        {
            Username = userData.Username,
            Email = userData.Email,
            Password = hashPassword,
            Salt = salt,
            ProfilePicture = null,
            BirthDate = userData.Birthdate,
        };

        await this.userRepository.Add(u);

        return Ok(u.Id);

    }

    [HttpPost("validate")]
    public ActionResult<UserToken> ValidateJwt(
        [FromServices] IJwtService jwtService,
        [FromBody] Jwt jwt
    )
    {
        if (jwt.Value == "" || jwt.Value is null)
        {
            return Ok(new UserToken { Authenticated = false });
        }

        try
        {
            var result = jwtService.Validate<UserToken>(jwt.Value);
            return Ok(result);
        }
        catch
        {
            return Ok(new UserToken { Authenticated = false });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResult>> Login(
        [FromBody] UserLogin loginData,
        [FromServices] IPasswordHasher psh,
        [FromServices] IJwtService jwtService
    )
    {
        var result = new LoginResult();

        var userList = await this.userRepository.Filter(u => u.Email == loginData.Email);

        result.UserExists = userList.Count() > 0;
        if (!result.UserExists)
        {
            return Ok(result);
        }

        User target = userList.First();

        if (psh.Validate(loginData.Password, target.Password, target.Salt))
        {
            string token = jwtService.GetToken<UserToken>(new UserToken { UserID = target.Id, Authenticated = true });

            result.Jwt = token;
            result.Success = true;
            return Ok(result);
        }

        result.Success = false;
        return Ok(result);
    }


    // TODO
    // Update User Info

}